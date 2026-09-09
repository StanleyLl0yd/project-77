using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Project77.Analytics;
using Project77.Puzzle;
using UnityEngine;

namespace Project77.Game
{
    public sealed class PrototypePlaytestRuntime : MonoBehaviour
    {
        private const int FirstLevel = 1;
        private const int LastLevel = 10;
        private const string DataFolderName = "Project77Playtests";

        private JsonLinesPrototypeAnalyticsSink fileSink;
        private PrototypeBuildInfo buildInfo;
        private string selectedVariant;
        private string selectedCoreVariant;
        private string playtestId;
        private string sessionId;
        private string eventsPath;
        private string metadataPath;
        private string latestEventsPath;
        private string latestMetadataPath;
        private string activeLevelId;
        private int? activeLevelRevision;
        private int activeAttemptIndex;
        private long activeAttemptStartMs;
        private long sessionStartMs;
        private int levelsStarted;
        private int levelsCompleted;
        private bool activeAttemptTerminal;
        private bool sessionActive;
        private bool sessionEnded;
        private bool restartPreludePending;
        private bool tutorialRecorded;
        private bool writingGeneratedEvent;
        private string lastVisibleStep;
        private string clipboardStatus;
        private float clipboardStatusUntil;

        public string BuildVersion => buildInfo?.build_version ?? Application.version;
        public string CommitSha => buildInfo?.commit_sha ?? "local";
        public string UnityVersion => buildInfo?.unity_version ?? Application.unityVersion;
        public bool HasActiveSession => sessionActive;

        private void Awake()
        {
            buildInfo = LoadBuildInfo();
            FindLatestFiles();
            PrototypeAnalyticsRuntimeHooks.Configure(PrepareEvent, ObserveEvent);
        }

        private void OnDestroy()
        {
            PrototypeAnalyticsRuntimeHooks.Reset();
        }

        private void OnApplicationQuit()
        {
            EndForExit();
        }

        private void OnGUI()
        {
            if (sessionActive)
            {
                return;
            }

            var selector = GetComponent<PrototypeVariantSelector>();
            if (selector != null && selector.enabled)
            {
                return;
            }

            PrototypeGuiLayout.Begin();
            try
            {
                DrawExportControls();
            }
            finally
            {
                PrototypeGuiLayout.End();
            }
        }

        public bool TryBeginVariant(string requestedPlaytestId, string variant, out string error)
        {
            error = null;
            if (sessionActive)
            {
                error = "A playtest session is already active.";
                return false;
            }

            if (!PrototypeAnalyticsValidator.IsKnownCoreVariant(variant))
            {
                error = "Unknown prototype variant.";
                return false;
            }

            var normalizedId = NormalizePlaytestId(requestedPlaytestId);
            if (normalizedId == null)
            {
                error = "Playtest ID must be 1-64 characters and contain no line breaks.";
                return false;
            }

            playtestId = normalizedId;
            selectedVariant = variant;
            selectedCoreVariant = variant;
            StartSession(writeMetadata: true);
            return true;
        }

        public bool TryBeginSelectedMeta(
            string requestedPlaytestId,
            string coreVariant,
            out string error)
        {
            error = null;
            if (sessionActive)
            {
                error = "A playtest session is already active.";
                return false;
            }

            if (!PrototypeAnalyticsValidator.IsKnownCoreVariant(coreVariant))
            {
                error = "Unknown selected core variant.";
                return false;
            }

            var normalizedId = NormalizePlaytestId(requestedPlaytestId);
            if (normalizedId == null)
            {
                error = "Playtest ID must be 1-64 characters and contain no line breaks.";
                return false;
            }

            playtestId = normalizedId;
            selectedVariant = PrototypeVariant.SelectedMeta;
            selectedCoreVariant = coreVariant;
            StartSession(writeMetadata: true);
            return true;
        }

        private PrototypeAnalyticsEvent PrepareEvent(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                return null;
            }

            if (!sessionActive)
            {
                if (sessionEnded && IsFirstLevelStart(analyticsEvent))
                {
                    StartSession(writeMetadata: true);
                    restartPreludePending = true;
                }
                else
                {
                    throw new InvalidOperationException("Prototype analytics event arrived outside an active playtest session.");
                }
            }

            if (!string.Equals(analyticsEvent.Context.PrototypeVariant, selectedVariant, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Controller variant '{analyticsEvent.Context.PrototypeVariant}' does not match selected variant '{selectedVariant}'.");
            }

            var properties = analyticsEvent.Properties;
            if (analyticsEvent.EventName == PrototypeAnalyticsEventName.PrototypeStart)
            {
                var corrected = new Dictionary<string, object>(analyticsEvent.Properties)
                {
                    ["entry_point"] = "variant_select"
                };
                properties = corrected;
            }

            var context = new PrototypeAnalyticsContext(
                sessionId,
                playtestId,
                BuildVersion,
                selectedVariant,
                "unknown",
                CurrentOrientation());

            return new PrototypeAnalyticsEvent(
                analyticsEvent.EventName,
                analyticsEvent.TimestampUtcMs,
                context,
                analyticsEvent.LevelId,
                analyticsEvent.LevelRevision,
                properties);
        }

        private void ObserveEvent(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (writingGeneratedEvent)
            {
                return;
            }

            if (restartPreludePending && analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelStart)
            {
                restartPreludePending = false;
                WritePrototypeStart("restart");
                WriteTutorialExposure();
            }

            fileSink.Track(analyticsEvent);
            latestEventsPath = eventsPath;
            lastVisibleStep = analyticsEvent.EventName;
            ObserveState(analyticsEvent);

            if (!tutorialRecorded &&
                ((analyticsEvent.EventName == PrototypeAnalyticsEventName.PrototypeStart &&
                  selectedVariant != PrototypeVariant.SelectedMeta) ||
                 (analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelStart &&
                  selectedVariant == PrototypeVariant.SelectedMeta)))
            {
                WriteTutorialExposure();
            }

            if (analyticsEvent.EventName == PrototypeAnalyticsEventName.NextPuzzleClicked &&
                analyticsEvent.Properties.TryGetValue("next_level_id", out var nextLevelRaw) &&
                nextLevelRaw is string nextLevelId &&
                nextLevelId.EndsWith("-END", StringComparison.Ordinal))
            {
                CompleteSession();
            }
        }

        private void ObserveState(PrototypeAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelStart)
            {
                if (PrototypeAnalyticsValidator.TryGetIntegerProperty(
                        analyticsEvent.Properties,
                        "attempt_index",
                        out var attemptIndex))
                {
                    activeAttemptIndex = (int)attemptIndex;
                }

                if (activeAttemptIndex == 1)
                {
                    levelsStarted++;
                }

                activeLevelId = analyticsEvent.LevelId;
                activeLevelRevision = analyticsEvent.LevelRevision;
                activeAttemptStartMs = analyticsEvent.TimestampUtcMs;
                activeAttemptTerminal = false;
                return;
            }

            if (analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelComplete)
            {
                levelsCompleted++;
                activeAttemptTerminal = true;
                return;
            }

            if (analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelFail ||
                analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelQuit)
            {
                activeAttemptTerminal = true;
            }
        }

        private void StartSession(bool writeMetadata)
        {
            sessionId = Guid.NewGuid().ToString("N");
            sessionStartMs = NowMs();
            levelsStarted = 0;
            levelsCompleted = 0;
            activeLevelId = null;
            activeLevelRevision = null;
            activeAttemptIndex = 0;
            activeAttemptStartMs = 0;
            activeAttemptTerminal = true;
            lastVisibleStep = null;
            tutorialRecorded = false;
            sessionActive = true;
            sessionEnded = false;

            var directory = Path.Combine(Application.persistentDataPath, DataFolderName);
            Directory.CreateDirectory(directory);
            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var prefix = $"{stamp}_{sessionId}_{VariantCode(selectedVariant)}";
            eventsPath = Path.Combine(directory, prefix + "_events.jsonl");
            metadataPath = Path.Combine(directory, prefix + "_metadata.json");
            fileSink = new JsonLinesPrototypeAnalyticsSink(eventsPath);
            latestEventsPath = eventsPath;
            latestMetadataPath = metadataPath;

            if (writeMetadata)
            {
                WriteMetadata();
            }
        }

        private void WriteMetadata()
        {
            var metadata = new PlaytestMetadata
            {
                metadata_schema_version = 1,
                event_schema_version = PrototypeAnalyticsEvent.CurrentSchemaVersion,
                session_id = sessionId,
                playtest_id = playtestId,
                build_version = BuildVersion,
                commit_sha = CommitSha,
                app_version = Application.version,
                unity_version = UnityVersion,
                prototype_variant = selectedVariant,
                device_model = SystemInfo.deviceModel,
                operating_system = SystemInfo.operatingSystem,
                android_api = AndroidApiLevel(),
                screen_orientation = CurrentOrientation(),
                session_started_utc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                core_variant = selectedCoreVariant,
                levels = LoadLevelManifest(selectedVariant)
            };

            File.WriteAllText(
                metadataPath,
                JsonUtility.ToJson(metadata, true),
                new UTF8Encoding(false));
            latestMetadataPath = metadataPath;
        }

        private LevelRevisionRecord[] LoadLevelManifest(string variant)
        {
            var records = new LevelRevisionRecord[LastLevel - FirstLevel + 1];
            for (var number = FirstLevel; number <= LastLevel; number++)
            {
                PrototypeLevelDefinition level;
                if (variant == PrototypeVariant.EnergyRouting ||
                    (variant == PrototypeVariant.SelectedMeta && selectedCoreVariant == PrototypeVariant.EnergyRouting))
                {
                    level = EnergyRoutingJsonLoader.LoadResource($"A-{number:000}");
                }
                else if (variant == PrototypeVariant.PathExpeditionRouting)
                {
                    level = PathExpeditionJsonLoader.LoadResource($"B-{number:000}");
                }
                else if (variant == PrototypeVariant.FlowNetworkRestoration)
                {
                    level = FlowNetworkJsonLoader.LoadResource($"C-{number:000}");
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported prototype variant '{variant}' with core '{selectedCoreVariant}'.");
                }

                records[number - FirstLevel] = new LevelRevisionRecord
                {
                    id = level.Id,
                    revision = level.Revision
                };
            }

            return records;
        }

        private void WriteTutorialExposure()
        {
            if (tutorialRecorded)
            {
                return;
            }

            tutorialRecorded = true;
            WriteGeneratedEvent(new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.TutorialExposed,
                NowMs(),
                CurrentContext(),
                null,
                null,
                new Dictionary<string, object>
                {
                    ["tutorial_step_id"] = TutorialStepId(selectedCoreVariant),
                    ["exposure_index"] = 1,
                    ["presentation_type"] = "text",
                    ["auto_advance_ms"] = null
                }));
        }

        private void WritePrototypeStart(string entryPoint)
        {
            WriteGeneratedEvent(new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.PrototypeStart,
                NowMs(),
                CurrentContext(),
                null,
                null,
                new Dictionary<string, object>
                {
                    ["entry_point"] = entryPoint,
                    ["core_variant"] = selectedCoreVariant
                }));
        }

        private void CompleteSession()
        {
            if (!sessionActive)
            {
                return;
            }

            WriteSessionEnd("prototype_complete");
            sessionActive = false;
            sessionEnded = true;
            FindLatestFiles();
        }

        private void EndForExit()
        {
            if (!sessionActive)
            {
                return;
            }

            if (!activeAttemptTerminal &&
                !string.IsNullOrWhiteSpace(activeLevelId) &&
                activeLevelRevision.HasValue &&
                activeAttemptIndex >= 1)
            {
                WriteGeneratedEvent(new PrototypeAnalyticsEvent(
                    PrototypeAnalyticsEventName.LevelQuit,
                    NowMs(),
                    CurrentContext(),
                    activeLevelId,
                    activeLevelRevision,
                    new Dictionary<string, object>
                    {
                        ["attempt_index"] = activeAttemptIndex,
                        ["duration_ms"] = (int)Math.Max(0, NowMs() - activeAttemptStartMs),
                        ["quit_destination"] = "app_exit"
                    }));
                activeAttemptTerminal = true;
            }

            WriteSessionEnd("user_exit");
            sessionActive = false;
            sessionEnded = true;
        }

        private void WriteSessionEnd(string reason)
        {
            WriteGeneratedEvent(new PrototypeAnalyticsEvent(
                PrototypeAnalyticsEventName.SessionEnd,
                NowMs(),
                CurrentContext(),
                null,
                null,
                new Dictionary<string, object>
                {
                    ["duration_ms"] = (int)Math.Max(0, NowMs() - sessionStartMs),
                    ["levels_started"] = levelsStarted,
                    ["levels_completed"] = levelsCompleted,
                    ["end_reason"] = reason,
                    ["last_visible_step"] = lastVisibleStep
                }));
        }

        private void WriteGeneratedEvent(PrototypeAnalyticsEvent analyticsEvent)
        {
            var errors = PrototypeAnalyticsValidator.Validate(analyticsEvent);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }

            writingGeneratedEvent = true;
            try
            {
                fileSink.Track(analyticsEvent);
                latestEventsPath = eventsPath;
                lastVisibleStep = analyticsEvent.EventName;
            }
            finally
            {
                writingGeneratedEvent = false;
            }
        }

        private PrototypeAnalyticsContext CurrentContext()
        {
            return new PrototypeAnalyticsContext(
                sessionId,
                playtestId,
                BuildVersion,
                selectedVariant,
                "unknown",
                CurrentOrientation());
        }

        private void FindLatestFiles()
        {
            var directory = Path.Combine(Application.persistentDataPath, DataFolderName);
            if (!Directory.Exists(directory))
            {
                return;
            }

            latestEventsPath = FindLatest(directory, "*_events.jsonl");
            latestMetadataPath = FindLatest(directory, "*_metadata.json");
        }

        private static string FindLatest(string directory, string pattern)
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
            string latest = null;
            var latestWrite = DateTime.MinValue;
            foreach (var file in files)
            {
                var write = File.GetLastWriteTimeUtc(file);
                if (write > latestWrite)
                {
                    latestWrite = write;
                    latest = file;
                }
            }
            return latest;
        }

        private void DrawExportControls()
        {
            var width = PrototypeGuiLayout.ContentWidth(480f, 16f);
            var x = (PrototypeGuiLayout.Width - width) * 0.5f;
            var y = Mathf.Max(96f, PrototypeGuiLayout.Height - 214f);
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };

            GUI.Box(new Rect(x, y, width, 142f), string.Empty);
            GUI.Label(
                new Rect(x + 10f, y + 8f, width - 20f, 30f),
                $"Prototype data · schema v{PrototypeAnalyticsEvent.CurrentSchemaVersion}",
                titleStyle);

            var gap = 10f;
            var buttonWidth = (width - 30f - gap) * 0.5f;
            GUI.enabled = !string.IsNullOrEmpty(latestEventsPath) && File.Exists(latestEventsPath);
            if (GUI.Button(new Rect(x + 15f, y + 46f, buttonWidth, 50f), "Copy events"))
            {
                CopyFile(latestEventsPath, "Events copied");
            }

            GUI.enabled = !string.IsNullOrEmpty(latestMetadataPath) && File.Exists(latestMetadataPath);
            if (GUI.Button(new Rect(x + 15f + buttonWidth + gap, y + 46f, buttonWidth, 50f), "Copy metadata"))
            {
                CopyFile(latestMetadataPath, "Metadata copied");
            }
            GUI.enabled = true;

            if (Time.realtimeSinceStartup < clipboardStatusUntil)
            {
                GUI.Label(new Rect(x + 10f, y + 104f, width - 20f, 28f), clipboardStatus, statusStyle);
            }
        }

        private void CopyFile(string path, string status)
        {
            GUIUtility.systemCopyBuffer = File.ReadAllText(path, Encoding.UTF8);
            clipboardStatus = status;
            clipboardStatusUntil = Time.realtimeSinceStartup + 3f;
        }

        private static PrototypeBuildInfo LoadBuildInfo()
        {
            var asset = Resources.Load<TextAsset>("PrototypeBuildInfo");
            if (asset != null)
            {
                var info = JsonUtility.FromJson<PrototypeBuildInfo>(asset.text);
                if (info != null && !string.IsNullOrWhiteSpace(info.build_version))
                {
                    return info;
                }
            }

            return new PrototypeBuildInfo
            {
                schema_version = 1,
                build_version = Application.version + "+local",
                commit_sha = "local",
                unity_version = Application.unityVersion
            };
        }

        private static string NormalizePlaytestId(string value)
        {
            if (value == null)
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.Length < 1 || normalized.Length > 64 || normalized.IndexOf('\n') >= 0 || normalized.IndexOf('\r') >= 0)
            {
                return null;
            }
            return normalized;
        }

        private static bool IsFirstLevelStart(PrototypeAnalyticsEvent analyticsEvent)
        {
            return analyticsEvent.EventName == PrototypeAnalyticsEventName.LevelStart &&
                PrototypeAnalyticsValidator.TryGetIntegerProperty(analyticsEvent.Properties, "attempt_index", out var attempt) &&
                attempt == 1 &&
                PrototypeAnalyticsValidator.TryGetIntegerProperty(analyticsEvent.Properties, "level_sequence_index", out var sequence) &&
                sequence == 1;
        }

        private static string TutorialStepId(string variant)
        {
            if (variant == PrototypeVariant.EnergyRouting) return "energy_routing_core_instruction";
            if (variant == PrototypeVariant.PathExpeditionRouting) return "path_expedition_core_instruction";
            if (variant == PrototypeVariant.FlowNetworkRestoration) return "flow_network_core_instruction";
            return "unknown_core_instruction";
        }

        private static string VariantCode(string variant)
        {
            if (variant == PrototypeVariant.EnergyRouting) return "A";
            if (variant == PrototypeVariant.PathExpeditionRouting) return "B";
            if (variant == PrototypeVariant.FlowNetworkRestoration) return "C";
            if (variant == PrototypeVariant.SelectedMeta) return "P1";
            return "X";
        }

        private static string CurrentOrientation()
        {
            if (Screen.width == Screen.height)
            {
                return "unknown";
            }
            return Screen.width > Screen.height ? "landscape" : "portrait";
        }

        private static int AndroidApiLevel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
            }
            catch
            {
                return -1;
            }
#else
            return -1;
#endif
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [Serializable]
        private sealed class PrototypeBuildInfo
        {
            public int schema_version;
            public string build_version;
            public string commit_sha;
            public string unity_version;
        }

        [Serializable]
        private sealed class PlaytestMetadata
        {
            public int metadata_schema_version;
            public int event_schema_version;
            public string session_id;
            public string playtest_id;
            public string build_version;
            public string commit_sha;
            public string app_version;
            public string unity_version;
            public string prototype_variant;
            public string core_variant;
            public string device_model;
            public string operating_system;
            public int android_api;
            public string screen_orientation;
            public string session_started_utc;
            public LevelRevisionRecord[] levels;
        }

        [Serializable]
        private sealed class LevelRevisionRecord
        {
            public string id;
            public int revision;
        }
    }
}
