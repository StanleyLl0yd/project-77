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

        private static readonly HashSet<string> KnownNames = Set(
            PrototypeStart, TutorialExposed, LevelStart, LevelComplete, LevelFail,
            LevelRetry, LevelQuit, InvalidInteraction, RewardShown, RewardClaimed,
            ResourceSpend, GeneratorRepair, IslandChange, AreaUnlock, Robot77Discovered,
            NextPuzzleOffered, NextPuzzleClicked, SessionEnd);

        public static bool IsKnown(string value)
        {
            return value != null && KnownNames.Contains(value);
        }

        private static HashSet<string> Set(params string[] values)
        {
            return new HashSet<string>(values, StringComparer.Ordinal);
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
        private static readonly IReadOnlyDictionary<string, object> EmptyProperties =
            new Dictionary<string, object>();

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

    public static class PrototypeAnalyticsRuntimeHooks
    {
        private static Func<PrototypeAnalyticsEvent, PrototypeAnalyticsEvent> transform;
        private static Action<PrototypeAnalyticsEvent> observer;

        public static void Configure(
            Func<PrototypeAnalyticsEvent, PrototypeAnalyticsEvent> eventTransform,
            Action<PrototypeAnalyticsEvent> eventObserver)
        {
            transform = eventTransform;
            observer = eventObserver;
        }

        public static void Reset()
        {
            transform = null;
            observer = null;
        }

        internal static PrototypeAnalyticsEvent Prepare(PrototypeAnalyticsEvent analyticsEvent)
        {
            var prepared = transform == null ? analyticsEvent : transform(analyticsEvent);
            if (prepared == null)
            {
                throw new InvalidOperationException("Prototype analytics transform returned null.");
            }
            return prepared;
        }

        internal static void Notify(PrototypeAnalyticsEvent analyticsEvent)
        {
            observer?.Invoke(analyticsEvent);
        }
    }

    public sealed class InMemoryPrototypeAnalyticsSink : IPrototypeAnalyticsSink
    {
        private readonly List<PrototypeAnalyticsEvent> events = new List<PrototypeAnalyticsEvent>();
        private readonly HashSet<string> terminalAttempts = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<PrototypeAnalyticsEvent> Events => events;

        public void Track(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                throw new ArgumentNullException(nameof(analyticsEvent));
            }

            analyticsEvent = PrototypeAnalyticsRuntimeHooks.Prepare(analyticsEvent);
            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);
            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", errors), nameof(analyticsEvent));
            }

            PreventDuplicateTerminal(analyticsEvent);
            events.Add(analyticsEvent);
            PrototypeAnalyticsRuntimeHooks.Notify(analyticsEvent);
        }

        private void PreventDuplicateTerminal(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent.EventName != PrototypeAnalyticsEventName.LevelComplete &&
                analyticsEvent.EventName != PrototypeAnalyticsEventName.LevelFail &&
                analyticsEvent.EventName != PrototypeAnalyticsEventName.LevelQuit)
            {
                return;
            }

            if (!PrototypeAnalyticsValidator.TryGetIntegerProperty(
                    analyticsEvent.Properties, "attempt_index", out var attemptIndex))
            {
                return;
            }

            var key = string.Concat(
                analyticsEvent.Context.SessionId, "\n",
                analyticsEvent.LevelId, "\n",
                analyticsEvent.LevelRevision.GetValueOrDefault().ToString(), "\n",
                attemptIndex.ToString());
            if (!terminalAttempts.Add(key))
            {
                throw new InvalidOperationException(
                    $"Attempt {attemptIndex} for level '{analyticsEvent.LevelId}' already has a terminal analytics event.");
            }
        }
    }

    public static class PrototypeAnalyticsValidator
    {
        private static readonly HashSet<string> DeviceTiers = Set("low", "mid", "high", "unknown");
        private static readonly HashSet<string> Orientations = Set("portrait", "landscape", "unknown");
        private static readonly HashSet<string> Variants = Set(
            "energy_routing", "path_expedition_routing", "flow_network_restoration", "selected_meta", "unknown");
        private static readonly HashSet<string> CoreVariants = Set(
            "energy_routing", "path_expedition_routing", "flow_network_restoration");
        private static readonly HashSet<string> EntryPoints = Set("fresh_launch", "restart", "variant_select");
        private static readonly HashSet<string> PresentationTypes = Set("text", "gesture", "highlight", "animation", "other");
        private static readonly HashSet<string> RetrySources = Set("fail_screen", "manual_restart", "other");
        private static readonly HashSet<string> QuitDestinations = Set("prototype_menu", "app_exit", "island", "other");
        private static readonly HashSet<string> InteractionTypes = Set("tap", "drag", "path_start", "path_end", "other");
        private static readonly HashSet<string> InvalidReasons = Set(
            "out_of_bounds", "blocked", "wrong_target", "rule_violation", "no_effect", "other");
        private static readonly HashSet<string> RewardSources = Set("level_complete", "meta_step", "other");
        private static readonly HashSet<string> ClaimModes = Set("explicit", "auto");
        private static readonly HashSet<string> ResourceTypes = Set("scrap", "energy", "other");
        private static readonly HashSet<string> ChangeTypes = Set("repair", "power_on", "reveal", "unlock_visual", "other");
        private static readonly HashSet<string> ChangeCauses = Set("generator_repair", "area_unlock", "story_step", "other");
        private static readonly HashSet<string> UnlockSources = Set("generator_repair", "story_step", "other");
        private static readonly HashSet<string> OfferContexts = Set(
            "post_level", "post_reward", "post_island_change", "post_77_discovery");
        private static readonly HashSet<string> EndReasons = Set(
            "user_exit", "prototype_complete", "moderator_end", "app_background_timeout", "other");

        public static IReadOnlyList<string> Validate(PrototypeAnalyticsEvent analyticsEvent)
        {
            var errors = new List<string>();
            if (analyticsEvent == null)
            {
                errors.Add("Event is null.");
                return errors;
            }

            if (analyticsEvent.EventSchemaVersion != PrototypeAnalyticsEvent.CurrentSchemaVersion)
                errors.Add("Unsupported event schema version.");
            if (!PrototypeAnalyticsEventName.IsKnown(analyticsEvent.EventName))
                errors.Add($"Unknown event name: '{analyticsEvent.EventName ?? "<null>"}'.");
            if (analyticsEvent.TimestampUtcMs < 0)
                errors.Add("Timestamp must be a non-negative UTC Unix millisecond value.");

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
                errors.Add($"Unknown prototype_variant: '{context.PrototypeVariant ?? "<null>"}'.");
            if (!DeviceTiers.Contains(context.DeviceTier ?? string.Empty))
                errors.Add($"Unknown device_tier: '{context.DeviceTier ?? "<null>"}'.");
            if (!Orientations.Contains(context.ScreenOrientation ?? string.Empty))
                errors.Add($"Unknown screen_orientation: '{context.ScreenOrientation ?? "<null>"}'.");

            if (analyticsEvent.LevelRevision.HasValue && analyticsEvent.LevelRevision.Value < 1)
                errors.Add("level_revision must be >= 1 when present.");
            if (analyticsEvent.LevelRevision.HasValue != !string.IsNullOrWhiteSpace(analyticsEvent.LevelId))
                errors.Add("level_id and level_revision must either both be present or both be null.");

            ValidateProperties(analyticsEvent, errors);
            return errors;
        }

        public static bool IsKnownCoreVariant(string value)
        {
            return value != null && CoreVariants.Contains(value);
        }

        public static bool TryGetIntegerProperty(
            IReadOnlyDictionary<string, object> properties,
            string propertyName,
            out long value)
        {
            value = 0;
            if (properties == null || !properties.TryGetValue(propertyName, out var raw) || raw == null)
                return false;
            if (raw is int intValue) { value = intValue; return true; }
            if (raw is long longValue) { value = longValue; return true; }
            if (raw is short shortValue) { value = shortValue; return true; }
            if (raw is byte byteValue) { value = byteValue; return true; }
            return false;
        }

        private static void ValidateProperties(PrototypeAnalyticsEvent e, ICollection<string> errors)
        {
            switch (e.EventName)
            {
                case PrototypeAnalyticsEventName.PrototypeStart:
                    Enum(e, "entry_point", EntryPoints, errors); NullableCore(e, "core_variant", errors); break;
                case PrototypeAnalyticsEventName.TutorialExposed:
                    Text(e, "tutorial_step_id", errors); Int(e, "exposure_index", 1, errors);
                    Enum(e, "presentation_type", PresentationTypes, errors); OptionalInt(e, "auto_advance_ms", 0, errors); break;
                case PrototypeAnalyticsEventName.LevelStart:
                    Level(e, errors); Int(e, "attempt_index", 1, errors); Core(e, "core_variant", errors);
                    Int(e, "level_sequence_index", 1, errors); break;
                case PrototypeAnalyticsEventName.LevelComplete:
                    Level(e, errors); Int(e, "attempt_index", 1, errors); Int(e, "duration_ms", 0, errors);
                    Int(e, "valid_interaction_count", 0, errors); Int(e, "invalid_interaction_count", 0, errors);
                    Core(e, "core_variant", errors); OptionalInt(e, "move_count", 0, errors); OptionalInt(e, "path_action_count", 0, errors); break;
                case PrototypeAnalyticsEventName.LevelFail:
                    Level(e, errors); Int(e, "attempt_index", 1, errors); Int(e, "duration_ms", 0, errors);
                    Snake(e, "fail_reason", errors); Core(e, "core_variant", errors); break;
                case PrototypeAnalyticsEventName.LevelRetry:
                    Level(e, errors); Int(e, "previous_attempt_index", 1, errors); Int(e, "new_attempt_index", 1, errors);
                    Enum(e, "retry_source", RetrySources, errors); break;
                case PrototypeAnalyticsEventName.LevelQuit:
                    Level(e, errors); Int(e, "attempt_index", 1, errors); Int(e, "duration_ms", 0, errors);
                    Enum(e, "quit_destination", QuitDestinations, errors); break;
                case PrototypeAnalyticsEventName.InvalidInteraction:
                    Level(e, errors); Enum(e, "interaction_type", InteractionTypes, errors); Enum(e, "invalid_reason", InvalidReasons, errors);
                    Int(e, "attempt_index", 1, errors); OptionalText(e, "board_element_id", errors); break;
                case PrototypeAnalyticsEventName.RewardShown:
                    Text(e, "reward_id", errors); Enum(e, "reward_source", RewardSources, errors);
                    Int(e, "scrap_amount", 0, errors); Int(e, "energy_amount", 0, errors); break;
                case PrototypeAnalyticsEventName.RewardClaimed:
                    Text(e, "reward_id", errors); Enum(e, "claim_mode", ClaimModes, errors);
                    Int(e, "scrap_amount", 0, errors); Int(e, "energy_amount", 0, errors); break;
                case PrototypeAnalyticsEventName.ResourceSpend:
                    Enum(e, "resource_type", ResourceTypes, errors); Int(e, "amount", 1, errors);
                    Text(e, "sink_id", errors); Int(e, "balance_after", 0, errors); break;
                case PrototypeAnalyticsEventName.GeneratorRepair:
                    Int(e, "repair_stage", 1, errors); Int(e, "scrap_spent", 0, errors); Int(e, "energy_spent", 0, errors);
                    Int(e, "time_since_session_start_ms", 0, errors); break;
                case PrototypeAnalyticsEventName.IslandChange:
                    Text(e, "change_id", errors); Enum(e, "change_type", ChangeTypes, errors); Enum(e, "caused_by", ChangeCauses, errors); break;
                case PrototypeAnalyticsEventName.AreaUnlock:
                    Text(e, "area_id", errors); Enum(e, "unlock_source", UnlockSources, errors); break;
                case PrototypeAnalyticsEventName.Robot77Discovered:
                    Text(e, "discovery_id", errors); Int(e, "time_since_session_start_ms", 0, errors);
                    Int(e, "levels_completed_before_discovery", 0, errors); break;
                case PrototypeAnalyticsEventName.NextPuzzleOffered:
                    Level(e, errors); Enum(e, "offer_context", OfferContexts, errors); Text(e, "next_level_id", errors);
                    Int(e, "offer_sequence_index", 1, errors); break;
                case PrototypeAnalyticsEventName.NextPuzzleClicked:
                    Level(e, errors); Enum(e, "offer_context", OfferContexts, errors); Text(e, "next_level_id", errors);
                    Int(e, "ms_since_offer", 0, errors); Int(e, "offer_sequence_index", 1, errors); break;
                case PrototypeAnalyticsEventName.SessionEnd:
                    Int(e, "duration_ms", 0, errors); Int(e, "levels_started", 0, errors); Int(e, "levels_completed", 0, errors);
                    Enum(e, "end_reason", EndReasons, errors); OptionalText(e, "last_visible_step", errors); break;
            }
        }

        private static void Level(PrototypeAnalyticsEvent e, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(e.LevelId) || !e.LevelRevision.HasValue)
                errors.Add($"{e.EventName} requires level_id and level_revision.");
        }

        private static void Int(PrototypeAnalyticsEvent e, string name, long minimum, ICollection<string> errors)
        {
            if (!TryGetIntegerProperty(e.Properties, name, out var value))
            {
                errors.Add($"{name} must be an integer.");
                return;
            }
            if (value < minimum) errors.Add($"{name} must be >= {minimum}.");
        }

        private static void OptionalInt(PrototypeAnalyticsEvent e, string name, long minimum, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || raw == null) return;
            if (!TryGetIntegerProperty(e.Properties, name, out var value))
            {
                errors.Add($"{name} must be an integer or null.");
                return;
            }
            if (value < minimum) errors.Add($"{name} must be >= {minimum} when present.");
        }

        private static void Core(PrototypeAnalyticsEvent e, string name, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || !(raw is string value) || !CoreVariants.Contains(value))
                errors.Add($"{name} must be a canonical core variant.");
        }

        private static void NullableCore(PrototypeAnalyticsEvent e, string name, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw)) { errors.Add($"{name} is required."); return; }
            if (raw != null && (!(raw is string value) || !CoreVariants.Contains(value)))
                errors.Add($"{name} must be a canonical core variant or null.");
        }

        private static void Enum(PrototypeAnalyticsEvent e, string name, ISet<string> allowed, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || !(raw is string value) || !allowed.Contains(value))
                errors.Add($"{name} has an unknown or missing enum value.");
        }

        private static void Text(PrototypeAnalyticsEvent e, string name, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || !(raw is string value) || string.IsNullOrWhiteSpace(value))
                errors.Add($"{name} must be a non-empty string.");
        }

        private static void OptionalText(PrototypeAnalyticsEvent e, string name, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || raw == null) return;
            if (!(raw is string)) errors.Add($"{name} must be a string or null.");
        }

        private static void Snake(PrototypeAnalyticsEvent e, string name, ICollection<string> errors)
        {
            if (!e.Properties.TryGetValue(name, out var raw) || !(raw is string value) || !IsSnake(value))
                errors.Add($"{name} must be a non-empty lowercase snake_case string.");
        }

        private static bool IsSnake(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var c in value)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_') continue;
                return false;
            }
            return true;
        }

        private static HashSet<string> Set(params string[] values)
        {
            return new HashSet<string>(values, StringComparer.Ordinal);
        }

        private static void RequireNonEmpty(string value, string name, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        }
    }
}
