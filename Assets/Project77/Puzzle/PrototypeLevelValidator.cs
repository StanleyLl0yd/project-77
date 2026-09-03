using System;
using System.Collections.Generic;

namespace Project77.Puzzle
{
    public sealed class PrototypeLevelValidationResult
    {
        private readonly List<string> errors = new List<string>();

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;

        internal void Add(string message)
        {
            errors.Add(message);
        }
    }

    public static class PrototypeLevelValidator
    {
        public static PrototypeLevelValidationResult Validate(PrototypeLevelDefinition level)
        {
            var result = new PrototypeLevelValidationResult();

            if (level == null)
            {
                result.Add("Level definition is null.");
                return result;
            }

            if (level.SchemaVersion != PrototypeLevelDefinition.CurrentSchemaVersion)
            {
                result.Add($"Unsupported schema version: {level.SchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(level.Id))
            {
                result.Add("Level id is required.");
            }
            else if (!IsStableId(level.Id))
            {
                result.Add($"Level id '{level.Id}' contains unsupported characters.");
            }

            if (level.Revision < 1)
            {
                result.Add("Level revision must be >= 1.");
            }

            if (!PrototypeVariant.IsCoreVariant(level.Variant))
            {
                result.Add($"Unknown core prototype variant: '{level.Variant ?? "<null>"}'.");
            }

            if (string.IsNullOrWhiteSpace(level.DifficultyTag))
            {
                result.Add("Difficulty tag is required.");
            }

            if (level.Payload == null)
            {
                result.Add("Variant payload is required.");
            }

            return result;
        }

        public static PrototypeLevelValidationResult ValidateSet(IEnumerable<PrototypeLevelDefinition> levels)
        {
            var result = new PrototypeLevelValidationResult();
            if (levels == null)
            {
                result.Add("Level set is null.");
                return result;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var level in levels)
            {
                var single = Validate(level);
                foreach (var error in single.Errors)
                {
                    result.Add(level == null ? error : $"{level}: {error}");
                }

                if (level == null || string.IsNullOrWhiteSpace(level.Id))
                {
                    continue;
                }

                if (!ids.Add(level.Id))
                {
                    result.Add($"Duplicate active level id: '{level.Id}'. Keep one active revision per id in a test set.");
                }
            }

            return result;
        }

        private static bool IsStableId(string id)
        {
            for (var index = 0; index < id.Length; index++)
            {
                var character = id[index];
                if ((character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '-' || character == '_')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
