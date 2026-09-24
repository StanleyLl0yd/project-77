using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Project77.Save
{
    public sealed class VerticalSliceSaveState
    {
        public const int CurrentSchemaVersion = 1;

        private readonly IReadOnlyList<string> claimedRewardIds;

        public VerticalSliceSaveState(
            int schemaVersion,
            string playerId,
            int energyRoutingLevelIndex,
            int scrap,
            int energy,
            bool generatorRepaired,
            bool areaUnlocked,
            bool robot77Discovered,
            int narrativeStep,
            bool hapticsEnabled,
            bool reducedMotionEnabled,
            IEnumerable<string> claimedRewardIds)
        {
            SchemaVersion = schemaVersion;
            PlayerId = playerId;
            EnergyRoutingLevelIndex = energyRoutingLevelIndex;
            Scrap = scrap;
            Energy = energy;
            GeneratorRepaired = generatorRepaired;
            AreaUnlocked = areaUnlocked;
            Robot77Discovered = robot77Discovered;
            NarrativeStep = narrativeStep;
            HapticsEnabled = hapticsEnabled;
            ReducedMotionEnabled = reducedMotionEnabled;
            this.claimedRewardIds = new List<string>(
                claimedRewardIds ?? Array.Empty<string>()).AsReadOnly();
        }

        public int SchemaVersion { get; }
        public string PlayerId { get; }
        public int EnergyRoutingLevelIndex { get; }
        public int Scrap { get; }
        public int Energy { get; }
        public bool GeneratorRepaired { get; }
        public bool AreaUnlocked { get; }
        public bool Robot77Discovered { get; }
        public int NarrativeStep { get; }
        public bool HapticsEnabled { get; }
        public bool ReducedMotionEnabled { get; }
        public IReadOnlyList<string> ClaimedRewardIds => claimedRewardIds;

        public static VerticalSliceSaveState CreateNew(string playerId)
        {
            return new VerticalSliceSaveState(
                CurrentSchemaVersion,
                playerId,
                energyRoutingLevelIndex: 0,
                scrap: 0,
                energy: 0,
                generatorRepaired: false,
                areaUnlocked: false,
                robot77Discovered: false,
                narrativeStep: 0,
                hapticsEnabled: true,
                reducedMotionEnabled: false,
                claimedRewardIds: Array.Empty<string>());
        }
    }

    public static class VerticalSliceSaveValidator
    {
        public static IReadOnlyList<string> Validate(VerticalSliceSaveState state)
        {
            var errors = new List<string>();
            if (state == null)
            {
                errors.Add("save state is required");
                return errors;
            }

            if (state.SchemaVersion != VerticalSliceSaveState.CurrentSchemaVersion)
            {
                errors.Add(
                    $"schemaVersion must be {VerticalSliceSaveState.CurrentSchemaVersion}");
            }

            if (string.IsNullOrWhiteSpace(state.PlayerId) ||
                !Guid.TryParseExact(state.PlayerId, "N", out _))
            {
                errors.Add("playerId must be a 32-character GUID in N format");
            }

            if (state.EnergyRoutingLevelIndex < 0)
            {
                errors.Add("energyRoutingLevelIndex cannot be negative");
            }

            if (state.Scrap < 0 || state.Energy < 0)
            {
                errors.Add("resource balances cannot be negative");
            }

            if (state.NarrativeStep < 0)
            {
                errors.Add("narrativeStep cannot be negative");
            }

            if (state.AreaUnlocked && !state.GeneratorRepaired)
            {
                errors.Add("area unlock requires generator repair");
            }

            if (state.Robot77Discovered && !state.AreaUnlocked)
            {
                errors.Add("robot 77 discovery requires area unlock");
            }

            if (state.NarrativeStep > 0 && !state.Robot77Discovered)
            {
                errors.Add("narrative progression requires robot 77 discovery");
            }

            var seenRewards = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rewardId in state.ClaimedRewardIds)
            {
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    errors.Add("claimed reward ids cannot be blank");
                    continue;
                }

                if (!seenRewards.Add(rewardId))
                {
                    errors.Add($"duplicate claimed reward id: {rewardId}");
                }
            }

            return errors;
        }
    }

    public static class VerticalSliceSaveCodec
    {
        private const string Magic = "P77SAVE";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static string Serialize(VerticalSliceSaveState state)
        {
            var errors = VerticalSliceSaveValidator.Validate(state);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }

            var rewardIds = new string[state.ClaimedRewardIds.Count];
            for (var index = 0; index < rewardIds.Length; index++)
            {
                rewardIds[index] = state.ClaimedRewardIds[index];
            }
            Array.Sort(rewardIds, StringComparer.Ordinal);

            var encodedRewards = new string[rewardIds.Length];
            for (var index = 0; index < rewardIds.Length; index++)
            {
                encodedRewards[index] = EncodeString(rewardIds[index]);
            }

            var builder = new StringBuilder();
            builder.AppendLine(Magic);
            builder.Append("schemaVersion=").Append(state.SchemaVersion.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.Append("playerId=").Append(EncodeString(state.PlayerId)).AppendLine();
            builder.Append("energyRoutingLevelIndex=").Append(state.EnergyRoutingLevelIndex.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.Append("scrap=").Append(state.Scrap.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.Append("energy=").Append(state.Energy.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.Append("generatorRepaired=").Append(BoolValue(state.GeneratorRepaired)).AppendLine();
            builder.Append("areaUnlocked=").Append(BoolValue(state.AreaUnlocked)).AppendLine();
            builder.Append("robot77Discovered=").Append(BoolValue(state.Robot77Discovered)).AppendLine();
            builder.Append("narrativeStep=").Append(state.NarrativeStep.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.Append("hapticsEnabled=").Append(BoolValue(state.HapticsEnabled)).AppendLine();
            builder.Append("reducedMotionEnabled=").Append(BoolValue(state.ReducedMotionEnabled)).AppendLine();
            builder.Append("claimedRewardIds=").Append(string.Join(",", encodedRewards)).AppendLine();
            return builder.ToString();
        }

        public static bool TryDeserialize(
            string text,
            out VerticalSliceSaveState state,
            out string error)
        {
            state = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "save payload is empty";
                return false;
            }

            var normalized = text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .TrimEnd('\n');
            var lines = normalized.Split('\n');
            if (lines.Length < 2 || !string.Equals(lines[0], Magic, StringComparison.Ordinal))
            {
                error = "save magic header is invalid";
                return false;
            }

            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 1; index < lines.Length; index++)
            {
                var separator = lines[index].IndexOf('=');
                if (separator <= 0)
                {
                    error = $"save line {index + 1} is malformed";
                    return false;
                }

                var key = lines[index].Substring(0, separator);
                var value = lines[index].Substring(separator + 1);
                if (!IsKnownKey(key))
                {
                    error = $"unknown save field: {key}";
                    return false;
                }
                if (!fields.TryAdd(key, value))
                {
                    error = $"duplicate save field: {key}";
                    return false;
                }
            }

            if (!TryInt(fields, "schemaVersion", out var schemaVersion, out error))
            {
                return false;
            }
            if (schemaVersion != VerticalSliceSaveState.CurrentSchemaVersion)
            {
                error = $"unsupported schemaVersion {schemaVersion}";
                return false;
            }

            if (!TryString(fields, "playerId", out var playerId, out error) ||
                !TryInt(fields, "energyRoutingLevelIndex", out var levelIndex, out error) ||
                !TryInt(fields, "scrap", out var scrap, out error) ||
                !TryInt(fields, "energy", out var energy, out error) ||
                !TryBool(fields, "generatorRepaired", out var generatorRepaired, out error) ||
                !TryBool(fields, "areaUnlocked", out var areaUnlocked, out error) ||
                !TryBool(fields, "robot77Discovered", out var robot77Discovered, out error) ||
                !TryInt(fields, "narrativeStep", out var narrativeStep, out error) ||
                !TryBool(fields, "hapticsEnabled", out var hapticsEnabled, out error) ||
                !TryBool(fields, "reducedMotionEnabled", out var reducedMotionEnabled, out error) ||
                !TryRewards(fields, out var claimedRewardIds, out error))
            {
                return false;
            }

            state = new VerticalSliceSaveState(
                schemaVersion,
                playerId,
                levelIndex,
                scrap,
                energy,
                generatorRepaired,
                areaUnlocked,
                robot77Discovered,
                narrativeStep,
                hapticsEnabled,
                reducedMotionEnabled,
                claimedRewardIds);

            var validationErrors = VerticalSliceSaveValidator.Validate(state);
            if (validationErrors.Count > 0)
            {
                error = string.Join("; ", validationErrors);
                state = null;
                return false;
            }

            return true;
        }

        private static bool IsKnownKey(string key)
        {
            return key == "schemaVersion" ||
                key == "playerId" ||
                key == "energyRoutingLevelIndex" ||
                key == "scrap" ||
                key == "energy" ||
                key == "generatorRepaired" ||
                key == "areaUnlocked" ||
                key == "robot77Discovered" ||
                key == "narrativeStep" ||
                key == "hapticsEnabled" ||
                key == "reducedMotionEnabled" ||
                key == "claimedRewardIds";
        }

        private static string BoolValue(bool value)
        {
            return value ? "1" : "0";
        }

        private static string EncodeString(string value)
        {
            return Convert.ToBase64String(Utf8.GetBytes(value));
        }

        private static bool TryString(
            Dictionary<string, string> fields,
            string key,
            out string value,
            out string error)
        {
            value = null;
            error = null;
            if (!fields.TryGetValue(key, out var encoded))
            {
                error = $"missing save field: {key}";
                return false;
            }

            try
            {
                value = Utf8.GetString(Convert.FromBase64String(encoded));
                return true;
            }
            catch (FormatException)
            {
                error = $"save field {key} is not valid base64";
                return false;
            }
            catch (DecoderFallbackException)
            {
                error = $"save field {key} is not valid UTF-8";
                return false;
            }
        }

        private static bool TryInt(
            Dictionary<string, string> fields,
            string key,
            out int value,
            out string error)
        {
            value = 0;
            error = null;
            if (!fields.TryGetValue(key, out var raw))
            {
                error = $"missing save field: {key}";
                return false;
            }

            if (!int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                error = $"save field {key} is not a valid integer";
                return false;
            }
            return true;
        }

        private static bool TryBool(
            Dictionary<string, string> fields,
            string key,
            out bool value,
            out string error)
        {
            value = false;
            error = null;
            if (!fields.TryGetValue(key, out var raw))
            {
                error = $"missing save field: {key}";
                return false;
            }

            if (raw == "1")
            {
                value = true;
                return true;
            }
            if (raw == "0")
            {
                return true;
            }

            error = $"save field {key} must be 0 or 1";
            return false;
        }

        private static bool TryRewards(
            Dictionary<string, string> fields,
            out string[] rewardIds,
            out string error)
        {
            rewardIds = Array.Empty<string>();
            error = null;
            if (!fields.TryGetValue("claimedRewardIds", out var raw))
            {
                error = "missing save field: claimedRewardIds";
                return false;
            }

            if (raw.Length == 0)
            {
                return true;
            }

            var encoded = raw.Split(',');
            rewardIds = new string[encoded.Length];
            for (var index = 0; index < encoded.Length; index++)
            {
                try
                {
                    rewardIds[index] = Utf8.GetString(Convert.FromBase64String(encoded[index]));
                }
                catch (FormatException)
                {
                    error = "claimedRewardIds contains invalid base64";
                    rewardIds = Array.Empty<string>();
                    return false;
                }
                catch (DecoderFallbackException)
                {
                    error = "claimedRewardIds contains invalid UTF-8";
                    rewardIds = Array.Empty<string>();
                    return false;
                }
            }
            return true;
        }
    }

    public static class VerticalSliceSaveMigrator
    {
        public static bool TryNormalizeToCurrent(
            string source,
            out string normalized,
            out string error)
        {
            normalized = null;
            if (!VerticalSliceSaveCodec.TryDeserialize(source, out var state, out error))
            {
                return false;
            }

            normalized = VerticalSliceSaveCodec.Serialize(state);
            return true;
        }
    }

    public sealed class LocalSaveLoadResult
    {
        private LocalSaveLoadResult(
            bool found,
            VerticalSliceSaveState state,
            string source,
            string error)
        {
            Found = found;
            State = state;
            Source = source;
            Error = error;
        }

        public bool Found { get; }
        public VerticalSliceSaveState State { get; }
        public string Source { get; }
        public string Error { get; }

        public static LocalSaveLoadResult Success(
            VerticalSliceSaveState state,
            string source)
        {
            return new LocalSaveLoadResult(true, state, source, null);
        }

        public static LocalSaveLoadResult Missing(string error)
        {
            return new LocalSaveLoadResult(false, null, null, error);
        }
    }

    public sealed class AtomicLocalSaveStore
    {
        public const string PrimaryFileName = "vertical-slice.save";
        public const string BackupFileName = "vertical-slice.save.bak";
        public const string TemporaryFileName = "vertical-slice.save.tmp";

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public AtomicLocalSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Save directory is required.", nameof(directory));
            }

            DirectoryPath = directory;
            PrimaryPath = Path.Combine(directory, PrimaryFileName);
            BackupPath = Path.Combine(directory, BackupFileName);
            TemporaryPath = Path.Combine(directory, TemporaryFileName);
        }

        public string DirectoryPath { get; }
        public string PrimaryPath { get; }
        public string BackupPath { get; }
        public string TemporaryPath { get; }

        public void Save(VerticalSliceSaveState state)
        {
            var payload = VerticalSliceSaveCodec.Serialize(state);
            Directory.CreateDirectory(DirectoryPath);

            File.WriteAllText(TemporaryPath, payload, Utf8);
            if (!TryReadValid(TemporaryPath, out _, out var tempError))
            {
                throw new InvalidOperationException(
                    $"temporary save validation failed: {tempError}");
            }

            if (File.Exists(PrimaryPath) &&
                TryReadValid(PrimaryPath, out _, out _))
            {
                File.Copy(PrimaryPath, BackupPath, overwrite: true);
            }

            if (File.Exists(PrimaryPath))
            {
                File.Delete(PrimaryPath);
            }

            File.Move(TemporaryPath, PrimaryPath);
        }

        public LocalSaveLoadResult Load()
        {
            if (TryReadValid(PrimaryPath, out var primary, out var primaryError))
            {
                return LocalSaveLoadResult.Success(primary, "primary");
            }

            if (TryReadValid(TemporaryPath, out var temporary, out var temporaryError))
            {
                return LocalSaveLoadResult.Success(temporary, "temporary");
            }

            if (TryReadValid(BackupPath, out var backup, out var backupError))
            {
                return LocalSaveLoadResult.Success(backup, "backup");
            }

            if (!File.Exists(PrimaryPath) &&
                !File.Exists(TemporaryPath) &&
                !File.Exists(BackupPath))
            {
                return LocalSaveLoadResult.Missing("no local save exists");
            }

            return LocalSaveLoadResult.Missing(
                $"no valid local save found; primary={primaryError}; " +
                $"temporary={temporaryError}; backup={backupError}");
        }

        private static bool TryReadValid(
            string path,
            out VerticalSliceSaveState state,
            out string error)
        {
            state = null;
            error = "file does not exist";
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                return VerticalSliceSaveCodec.TryDeserialize(text, out state, out error);
            }
            catch (IOException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
