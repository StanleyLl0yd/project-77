using System;
using System.Collections.Generic;
using System.Globalization;

namespace Project77.Localization
{
    public static class PlayerTextKey
    {
        public const string AppTitle = "app.title";
        public const string SliceSubtitle = "slice.subtitle";
        public const string SliceSetupDescription = "slice.setup.description";
        public const string SliceBuildInfo = "slice.setup.build_info";
        public const string SlicePlaytestIdLabel = "slice.setup.playtest_id";
        public const string SliceStart = "slice.setup.start";
        public const string RecoveryTitle = "slice.recovery.title";
        public const string RecoveryCopyEvents = "slice.recovery.copy_events";
        public const string RecoveryCopyMetadata = "slice.recovery.copy_metadata";
        public const string RecoveryEventsCopied = "slice.recovery.events_copied";
        public const string RecoveryMetadataCopied = "slice.recovery.metadata_copied";
        public const string RecoveryUnavailable = "slice.recovery.unavailable";
        public const string RecoveryCopyFailed = "slice.recovery.copy_failed";
        public const string SaveRecoveredRepairFailed = "slice.save.recovered_repair_failed";
        public const string SaveLoadFailed = "slice.save.load_failed";
        public const string SaveCreateFailed = "slice.save.create_failed";
        public const string SaveUnavailable = "slice.save.unavailable";
        public const string SaveWriteFailed = "slice.save.write_failed";

        public const string IntroBody = "slice.intro.body";
        public const string IntroPrompt = "slice.intro.prompt";
        public const string IntroBegin = "slice.intro.begin";

        public const string RewardTitle = "slice.reward.title";
        public const string RewardRecovered = "slice.reward.recovered";
        public const string RewardAmounts = "slice.reward.amounts";
        public const string RewardExplanation = "slice.reward.explanation";
        public const string RewardTake = "slice.reward.take";

        public const string IslandTitle = "slice.island.title";
        public const string IslandResources = "slice.island.resources";
        public const string GeneratorOnline = "slice.island.generator_online";
        public const string GeneratorOffline = "slice.island.generator_offline";
        public const string AnnexOpen = "slice.island.annex_open";
        public const string AnnexLocked = "slice.island.annex_locked";
        public const string SignalFound = "slice.island.signal_found";
        public const string SignalNone = "slice.island.signal_none";
        public const string IslandInitialNotice = "slice.island.notice.initial";
        public const string IslandResourcesReady = "slice.island.notice.resources_ready";
        public const string IslandResourcesNeedMore = "slice.island.notice.resources_need_more";
        public const string IslandPowerRestored = "slice.island.notice.power_restored";
        public const string IslandAnnexOpenNotice = "slice.island.notice.annex_open";
        public const string IslandSignalFoundNotice = "slice.island.notice.signal_found";
        public const string IslandResumeRepair = "slice.island.resume.repair";
        public const string IslandResumeNeedMore = "slice.island.resume.need_more";
        public const string IslandResumePowered = "slice.island.resume.powered";
        public const string IslandResumeAnnex = "slice.island.resume.annex";
        public const string IslandResumeSignal = "slice.island.resume.signal";
        public const string IslandRepair = "slice.island.action.repair";
        public const string IslandRepairNeeds = "slice.island.action.repair_needs";
        public const string IslandOpenAnnex = "slice.island.action.open_annex";
        public const string IslandInvestigate = "slice.island.action.investigate";
        public const string IslandAnotherRoute = "slice.island.another_route";
        public const string IslandNeedMoreRoute = "slice.island.need_more_route";
        public const string IslandRestoreAnother = "slice.island.action.restore_another";
        public const string IslandFinish = "slice.island.action.finish";

        public const string SliceComplete = "slice.complete";
    }

    public sealed class TextCatalog
    {
        private readonly Dictionary<string, string> entries;
        private readonly TextCatalog fallback;
        private readonly CultureInfo culture;

        public TextCatalog(
            CultureInfo culture,
            IEnumerable<KeyValuePair<string, string>> entries,
            TextCatalog fallback = null)
        {
            this.culture = culture ?? throw new ArgumentNullException(nameof(culture));
            this.entries = new Dictionary<string, string>(StringComparer.Ordinal);
            this.fallback = fallback;

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new ArgumentException("Localization keys cannot be blank.", nameof(entries));
                }
                if (entry.Value == null)
                {
                    throw new ArgumentException(
                        $"Localization value for '{entry.Key}' cannot be null.",
                        nameof(entries));
                }
                if (!this.entries.TryAdd(entry.Key, entry.Value))
                {
                    throw new ArgumentException(
                        $"Duplicate localization key '{entry.Key}'.",
                        nameof(entries));
                }
            }
        }

        public CultureInfo Culture => culture;

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "[missing:<blank>]";
            }

            if (entries.TryGetValue(key, out var value))
            {
                return value;
            }

            if (fallback != null)
            {
                var fallbackValue = fallback.Get(key);
                if (!IsMissingMarker(fallbackValue))
                {
                    return fallbackValue;
                }
            }

            return $"[missing:{key}]";
        }

        public string Format(string key, params object[] args)
        {
            var template = Get(key);
            if (IsMissingMarker(template))
            {
                return template;
            }

            try
            {
                return string.Format(culture, template, args ?? Array.Empty<object>());
            }
            catch (FormatException)
            {
                return $"[format-error:{key}]";
            }
        }

        private static bool IsMissingMarker(string value)
        {
            return value != null &&
                value.StartsWith("[missing:", StringComparison.Ordinal);
        }
    }

    public static class PlayerText
    {
        private static readonly TextCatalog EnglishCatalog = new TextCatalog(
            CultureInfo.GetCultureInfo("en-US"),
            BuildEnglishEntries());

        public static TextCatalog English => EnglishCatalog;

        public static string Get(string key)
        {
            return EnglishCatalog.Get(key);
        }

        public static string Format(string key, params object[] args)
        {
            return EnglishCatalog.Format(key, args);
        }

        private static IEnumerable<KeyValuePair<string, string>> BuildEnglishEntries()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PlayerTextKey.AppTitle] = "Project 77",
                [PlayerTextKey.SliceSubtitle] = "Vertical Slice",
                [PlayerTextKey.SliceSetupDescription] =
                    "Restore the island's energy network, recover resources, repair the generator and investigate what wakes up.",
                [PlayerTextKey.SliceBuildInfo] = "Build {0}\nSchema v{1}",
                [PlayerTextKey.SlicePlaytestIdLabel] = "Anonymous playtest ID",
                [PlayerTextKey.SliceStart] = "Start",
                [PlayerTextKey.RecoveryTitle] = "Moderator: previous session data",
                [PlayerTextKey.RecoveryCopyEvents] = "Copy events",
                [PlayerTextKey.RecoveryCopyMetadata] = "Copy metadata",
                [PlayerTextKey.RecoveryEventsCopied] = "Previous events copied",
                [PlayerTextKey.RecoveryMetadataCopied] = "Previous metadata copied",
                [PlayerTextKey.RecoveryUnavailable] = "Previous session file is unavailable",
                [PlayerTextKey.RecoveryCopyFailed] = "Could not copy previous session data",
                [PlayerTextKey.SaveRecoveredRepairFailed] =
                    "Recovered local progress, but the primary save could not be repaired.",
                [PlayerTextKey.SaveLoadFailed] =
                    "Local progress could not be loaded. Existing save files were left untouched.",
                [PlayerTextKey.SaveCreateFailed] =
                    "Local progress could not be created. Check device storage and try again.",
                [PlayerTextKey.SaveUnavailable] =
                    "Local progress is unavailable. Existing save data was not reset.",
                [PlayerTextKey.SaveWriteFailed] =
                    "SAVE ERROR: progress could not be written. Do not close the app.",

                [PlayerTextKey.IntroBody] =
                    "The island is silent. Its old generator is dead.\n\nA weak energy network still responds beneath the surface.",
                [PlayerTextKey.IntroPrompt] = "Restore a route and see what wakes up.",
                [PlayerTextKey.IntroBegin] = "Begin restoration",

                [PlayerTextKey.RewardTitle] = "Route restored",
                [PlayerTextKey.RewardRecovered] = "Recovered resources",
                [PlayerTextKey.RewardAmounts] = "SCRAP +{0}\nENERGY +{1}",
                [PlayerTextKey.RewardExplanation] =
                    "Scrap is repair material. Energy is stored power. Both can restore island machinery.",
                [PlayerTextKey.RewardTake] = "Take resources",

                [PlayerTextKey.IslandTitle] = "Abandoned Island",
                [PlayerTextKey.IslandResources] = "SCRAP {0}/{1}   ·   ENERGY {2}/{3}",
                [PlayerTextKey.GeneratorOnline] = "[GENERATOR] ONLINE",
                [PlayerTextKey.GeneratorOffline] = "[GENERATOR] OFFLINE — NEEDS REPAIR",
                [PlayerTextKey.AnnexOpen] = "[ANNEX] OPEN",
                [PlayerTextKey.AnnexLocked] = "[ANNEX] LOCKED — NO POWER",
                [PlayerTextKey.SignalFound] = "[SIGNAL] 77 FOUND",
                [PlayerTextKey.SignalNone] = "[SIGNAL] NONE",
                [PlayerTextKey.IslandInitialNotice] =
                    "The generator is dark. The annex has no power.",
                [PlayerTextKey.IslandResourcesReady] =
                    "You now have enough material and stored power to repair the generator.",
                [PlayerTextKey.IslandResourcesNeedMore] =
                    "The recovered resources can be used to repair the island generator.",
                [PlayerTextKey.IslandPowerRestored] =
                    "POWER RESTORED. Lights come on and the sealed annex receives power.",
                [PlayerTextKey.IslandAnnexOpenNotice] =
                    "ANNEX OPEN. A weak signal is now detectable inside.",
                [PlayerTextKey.IslandSignalFoundNotice] =
                    "SIGNAL FOUND: 77. The damaged robot reacts to the restored power.",
                [PlayerTextKey.IslandResumeRepair] =
                    "Recovered progress. The generator can now be repaired.",
                [PlayerTextKey.IslandResumeNeedMore] =
                    "Recovered progress. More resources are needed for the generator.",
                [PlayerTextKey.IslandResumePowered] =
                    "Recovered progress. The generator is online and the annex has power.",
                [PlayerTextKey.IslandResumeAnnex] =
                    "Recovered progress. The annex is open and a weak signal is waiting.",
                [PlayerTextKey.IslandResumeSignal] =
                    "Recovered progress. Signal 77 is active.",
                [PlayerTextKey.IslandRepair] =
                    "Repair generator — spend {0} Scrap + {1} Energy",
                [PlayerTextKey.IslandRepairNeeds] =
                    "Generator repair needs {0} Scrap and {1} Energy.",
                [PlayerTextKey.IslandOpenAnnex] = "Open the powered annex",
                [PlayerTextKey.IslandInvestigate] = "Investigate the signal",
                [PlayerTextKey.IslandAnotherRoute] = "Another energy route is available.",
                [PlayerTextKey.IslandNeedMoreRoute] =
                    "You need more resources. Another energy route is available.",
                [PlayerTextKey.IslandRestoreAnother] = "Restore another route",
                [PlayerTextKey.IslandFinish] = "Finish slice",

                [PlayerTextKey.SliceComplete] =
                    "Vertical Slice route complete. More island work is coming next."
            };
        }
    }
}
