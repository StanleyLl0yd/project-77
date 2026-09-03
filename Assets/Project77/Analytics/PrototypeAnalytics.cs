using System;
using System.Collections.Generic;

namespace Project77.Analytics
{
    public static class PrototypeAnalyticsEventName
    {
        public const string PrototypeStart = "prototype_start";
        public const string TutorialExposed = "tutorial_exposed";
        public const string LevelStart = "level_start";
        public const string LevelComplete = "level_complete";
        public const string LevelFail = "level_fail";
        public const string LevelRetry = "level_retry";
        public const string LevelQuit = "level_quit";
        public const string InvalidInteraction = "invalid_interaction";
        public const string RewardShown = "reward_shown";
        public const string RewardClaimed = "reward_claimed";
        public const string ResourceSpend = "resource_spend";
        public const string GeneratorRepair = "generator_repair";
        public const string IslandChange = "island_change";
        public const string AreaUnlock = "area_unlock";
        public const string Robot77Discovered = "robot_77_discovered";
        public const string NextPuzzleOffered = "next_puzzle_offered";
        public const string NextPuzzleClicked = "next_puzzle_clicked";
        public const string SessionEnd = "session_end";

        private static readonly HashSet<string> KnownNames = new HashSet<string>(StringComparer.Ordinal)
        {
            PrototypeStart,
            TutorialExposed,
            LevelStart,
            LevelComplete,
            LevelFail,
            LevelRetry,
            LevelQuit,
            InvalidInteraction,
            RewardShown,
            RewardClaimed,
            ResourceSpend,
            GeneratorRepair,
            IslandChange,
            AreaUnlock,
            Robot77Discovered,
            NextPuzzleOffered,
            NextPuzzleClicked,
            SessionEnd
        };

        public static bool IsKnown(string value)
        {
            return value != null && KnownNames.Contains(value);
        }
    }

    public sealed class PrototypeAnalyticsContext
    {
        public PrototypeAnalyticsContext(
            string sessionId,
            string playtestId,
            string buildVersion,
            string prototypeVariant,
            string deviceTier,
            string screenOrientation)
        {
            SessionId = sessionId;
            PlaytestId = playtestId;
            BuildVersion = buildVersion;
            PrototypeVariant = prototypeVariant;
            DeviceTier = deviceTier;
            ScreenOrientation = screenOrientation;
        }

        public string SessionId { get; }
        public string PlaytestId { get; }
        public string BuildVersion { get; }
        public string PrototypeVariant { get; }
        public string DeviceTier { get; }
        public string ScreenOrientation { get; }
    }

    public sealed class PrototypeAnalyticsEvent
    {
        public const int CurrentSchemaVersion = 1;

        public PrototypeAnalyticsEvent(
            string eventName,
            long timestampUtcMs,
            PrototypeAnalyticsContext context,
            string levelId,
            int? levelRevision,
            IReadOnlyDictionary<string, object> properties)
        {
            EventSchemaVersion = CurrentSchemaVersion;
            EventName = eventName;
            TimestampUtcMs = timestampUtcMs;
            Context = context;
            LevelId = levelId;
            LevelRevision = levelRevision;
            Properties = properties ?? EmptyProperties;
        }

        private static readonly IReadOnlyDictionary<string, object> EmptyProperties =
            new Dictionary<string, object>();

        public int EventSchemaVersion { get; }
        public string EventName { get; }
        public long TimestampUtcMs { get; }
        public PrototypeAnalyticsContext Context { get; }
        public string LevelId { get; }
        public int? LevelRevision { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
    }

    public interface IPrototypeAnalyticsSink
    {
        void Track(PrototypeAnalyticsEvent analyticsEvent);
    }

    public sealed class InMemoryPrototypeAnalyticsSink : IPrototypeAnalyticsSink
    {
        private readonly List<PrototypeAnalyticsEvent> events = new List<PrototypeAnalyticsEvent>();

        public IReadOnlyList<PrototypeAnalyticsEvent> Events => events;

        public void Track(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                throw new ArgumentNullException(nameof(analyticsEvent));
            }

            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(analyticsEvent));
            }

            events.Add(analyticsEvent);
        }
    }

    public static class PrototypeAnalyticsValidator
    {
        private static readonly HashSet<string> DeviceTiers = new HashSet<string>(StringComparer.Ordinal)
        {
            "low", "mid", "high", "unknown"
        };

        private static readonly HashSet<string> Orientations = new HashSet<string>(StringComparer.Ordinal)
        {
            "portrait", "landscape", "unknown"
        };

        private static readonly HashSet<string> Variants = new HashSet<string>(StringComparer.Ordinal)
        {
            "energy_routing",
            "path_expedition_routing",
            "flow_network_restoration",
            "selected_meta",
            "unknown"
        };

        public static IReadOnlyList<string> Validate(PrototypeAnalyticsEvent analyticsEvent)
        {
            var errors = new List<string>();
            if (analyticsEvent == null)
            {
                errors.Add("Event is null.");
                return errors;
            }

            if (analyticsEvent.EventSchemaVersion != PrototypeAnalyticsEvent.CurrentSchemaVersion)
            {
                errors.Add("Unsupported event schema version.");
            }

            if (!PrototypeAnalyticsEventName.IsKnown(analyticsEvent.EventName))
            {
                errors.Add($"Unknown event name: '{analyticsEvent.EventName ?? "<null>"}'.");
            }

            if (analyticsEvent.TimestampUtcMs < 0)
            {
                errors.Add("Timestamp must be a non-negative UTC Unix millisecond value.");
            }

            var context = analyticsEvent.Context;
            if (context == null)
            {
                errors.Add("Analytics context is required.");
                return errors;
            }

            RequireNonEmpty(context.SessionId, "session_id", errors);
            RequireNonEmpty(context.PlaytestId, "playtest_id", errors);
            RequireNonEmpty(context.BuildVersion, "build_version", errors);

            if (!Variants.Contains(context.PrototypeVariant ?? string.Empty))
            {
                errors.Add($"Unknown prototype_variant: '{context.PrototypeVariant ?? "<null>"}'.");
            }

            if (!DeviceTiers.Contains(context.DeviceTier ?? string.Empty))
            {
                errors.Add($"Unknown device_tier: '{context.DeviceTier ?? "<null>"}'.");
            }

            if (!Orientations.Contains(context.ScreenOrientation ?? string.Empty))
            {
                errors.Add($"Unknown screen_orientation: '{context.ScreenOrientation ?? "<null>"}'.");
            }

            if (analyticsEvent.LevelRevision.HasValue && analyticsEvent.LevelRevision.Value < 1)
            {
                errors.Add("level_revision must be >= 1 when present.");
            }

            if (analyticsEvent.LevelRevision.HasValue && string.IsNullOrWhiteSpace(analyticsEvent.LevelId))
            {
                errors.Add("level_id is required when level_revision is present.");
            }

            return errors;
        }

        private static void RequireNonEmpty(string value, string fieldName, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{fieldName} is required.");
            }
        }
    }
}
