using System;

namespace Project77.Puzzle
{
    public interface IPrototypeLevelPayload
    {
    }

    public sealed class PrototypeLevelDefinition
    {
        public const int CurrentSchemaVersion = 1;

        public PrototypeLevelDefinition(
            int schemaVersion,
            string id,
            int revision,
            string variant,
            string difficultyTag,
            IPrototypeLevelPayload payload)
        {
            SchemaVersion = schemaVersion;
            Id = id;
            Revision = revision;
            Variant = variant;
            DifficultyTag = difficultyTag;
            Payload = payload;
        }

        public int SchemaVersion { get; }
        public string Id { get; }
        public int Revision { get; }
        public string Variant { get; }
        public string DifficultyTag { get; }
        public IPrototypeLevelPayload Payload { get; }

        public override string ToString()
        {
            return $"{Id}@{Revision} ({Variant})";
        }
    }
}
