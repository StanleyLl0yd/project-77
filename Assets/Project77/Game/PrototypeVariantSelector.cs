using System;
using System.IO;
using Project77.Puzzle;
using UnityEngine;

namespace Project77.Game
{
    public sealed class PrototypeVariantSelector : MonoBehaviour
    {
        private const string DataFolderName = "Project77Playtests";
        private const string EventsSuffix = "_events.jsonl";
        private const string MetadataSuffix = "_metadata.json";

        private PrototypePlaytestRuntime playtestRuntime;
        private string playtestId;
        private string setupError = string.Empty;
        private string previousEventsPath;
        private string previousMetadataPath;
        private string recoveryStatus = string.Empty;
        private float recoveryStatusUntil;
        private Vector2 setupScroll;

        private void Awake()
        {
            playtestRuntime = GetComponent<PrototypePlaytestRuntime>();
            if (playtestRuntime == null)
            {
                playtestRuntime = gameObject.AddComponent<PrototypePlaytestRuntime>();
            }

            playtestId = "p1-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            RefreshPreviousSessionFiles();
        }

        private void OnGUI()
        {
            PrototypeGuiLayout.Begin();
            try
            {
                DrawSetup();
            }
            finally
            {
                PrototypeGuiLayout.End();
            }
        }

        private void DrawSetup()
        {
            var width = PrototypeGuiLayout.ContentWidth(640f, 16f);
            var viewportHeight = Mathf.Max(220f, PrototypeGuiLayout.Height - 24f);
            var viewport = new Rect(
                (PrototypeGuiLayout.Width - width) * 0.5f,
                12f,
                width,
                viewportHeight);

            var contentWidth = Mathf.Max(220f, width - 20f);
            var recoveryHeight = HasPreviousSessionData ? 138f : 0f;
#if UNITY_EDITOR
            var contentHeight = (string.IsNullOrEmpty(setupError) ? 700f : 766f) + recoveryHeight;
#else
            var contentHeight = (string.IsNullOrEmpty(setupError) ? 458f : 524f) + recoveryHeight;
#endif
            setupScroll = GUI.BeginScrollView(
                viewport,
                setupScroll,
                new Rect(0f, 0f, contentWidth, contentHeight));

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };
            var captionStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 16
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };

            var y = 8f;
            GUI.Label(new Rect(0f, y, contentWidth, 48f), "Project 77", titleStyle);
            y += 48f;
            GUI.Label(new Rect(0f, y, contentWidth, 34f), "Playtest build", titleStyle);
            y += 48f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 82f),
                "Restore the island's energy network, recover resources, repair the generator and investigate what wakes up.",
                bodyStyle);
            y += 92f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 42f),
                $"Build {playtestRuntime.BuildVersion}\nSchema v{Project77.Analytics.PrototypeAnalyticsEvent.CurrentSchemaVersion}",
                captionStyle);
            y += 54f;

            GUI.Label(new Rect(0f, y, contentWidth, 28f), "Anonymous playtest ID", bodyStyle);
            y += 32f;
            playtestId = GUI.TextField(
                new Rect(8f, y, contentWidth - 16f, 48f),
                playtestId ?? string.Empty,
                64,
                textFieldStyle);
            y += 68f;

            if (HasPreviousSessionData)
            {
                DrawPreviousSessionData(contentWidth, ref y, bodyStyle);
            }

            if (GUI.Button(
                    new Rect(8f, y, contentWidth - 16f, 68f),
                    "Start playtest",
                    buttonStyle))
            {
                StartSelectedMeta();
            }
            y += 82f;

#if UNITY_EDITOR
            DrawEditorDebugVariants(contentWidth, ref y, bodyStyle);
#endif

            if (!string.IsNullOrEmpty(setupError))
            {
                var errorStyle = new GUIStyle(bodyStyle)
                {
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(new Rect(8f, y, contentWidth - 16f, 58f), setupError, errorStyle);
            }

            GUI.EndScrollView();
        }

        private bool HasPreviousSessionData =>
            IsReadableFile(previousEventsPath) && IsReadableFile(previousMetadataPath);

        private void DrawPreviousSessionData(float contentWidth, ref float y, GUIStyle bodyStyle)
        {
            var panelWidth = contentWidth - 16f;
            var titleStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            var statusStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 14
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                wordWrap = true
            };

            GUI.Box(new Rect(8f, y, panelWidth, 124f), string.Empty);
            GUI.Label(
                new Rect(18f, y + 8f, panelWidth - 20f, 26f),
                "Moderator: previous session data",
                titleStyle);

            var gap = 8f;
            var buttonWidth = (panelWidth - 28f - gap) * 0.5f;
            GUI.enabled = IsReadableFile(previousEventsPath);
            if (GUI.Button(new Rect(18f, y + 40f, buttonWidth, 44f), "Copy events", buttonStyle))
            {
                CopyPreviousFile(previousEventsPath, "Previous events copied");
            }

            GUI.enabled = IsReadableFile(previousMetadataPath);
            if (GUI.Button(
                    new Rect(18f + buttonWidth + gap, y + 40f, buttonWidth, 44f),
                    "Copy metadata",
                    buttonStyle))
            {
                CopyPreviousFile(previousMetadataPath, "Previous metadata copied");
            }
            GUI.enabled = true;

            if (Time.realtimeSinceStartup < recoveryStatusUntil)
            {
                GUI.Label(
                    new Rect(18f, y + 88f, panelWidth - 20f, 26f),
                    recoveryStatus,
                    statusStyle);
            }

            y += 138f;
        }

        private void RefreshPreviousSessionFiles()
        {
            previousEventsPath = null;
            previousMetadataPath = null;

            var directory = Path.Combine(Application.persistentDataPath, DataFolderName);
            if (!Directory.Exists(directory))
            {
                return;
            }

            FindLatestSessionPair(directory, out previousEventsPath, out previousMetadataPath);
        }

        private static void FindLatestSessionPair(
            string directory,
            out string latestEventsPath,
            out string latestMetadataPath)
        {
            latestEventsPath = null;
            latestMetadataPath = null;
            var latestWrite = DateTime.MinValue;

            var eventFiles = Directory.GetFiles(
                directory,
                "*" + EventsSuffix,
                SearchOption.TopDirectoryOnly);
            foreach (var eventPath in eventFiles)
            {
                var filename = Path.GetFileName(eventPath);
                if (string.IsNullOrEmpty(filename) || filename.Length <= EventsSuffix.Length)
                {
                    continue;
                }

                var stem = filename.Substring(0, filename.Length - EventsSuffix.Length);
                var metadataPath = Path.Combine(directory, stem + MetadataSuffix);
                if (!File.Exists(metadataPath))
                {
                    continue;
                }

                var write = File.GetLastWriteTimeUtc(eventPath);
                if (write <= latestWrite)
                {
                    continue;
                }

                latestWrite = write;
                latestEventsPath = eventPath;
                latestMetadataPath = metadataPath;
            }
        }

        private void CopyPreviousFile(string path, string successStatus)
        {
            try
            {
                if (!IsReadableFile(path))
                {
                    recoveryStatus = "Previous session file is unavailable";
                }
                else
                {
                    GUIUtility.systemCopyBuffer = File.ReadAllText(path);
                    recoveryStatus = successStatus;
                }
            }
            catch (Exception)
            {
                recoveryStatus = "Could not copy previous session data";
            }

            recoveryStatusUntil = Time.realtimeSinceStartup + 3f;
        }

        private static bool IsReadableFile(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

#if UNITY_EDITOR
        private void DrawEditorDebugVariants(float contentWidth, ref float y, GUIStyle bodyStyle)
        {
            var sectionStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            var debugButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                wordWrap = true
            };

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 32f),
                "Editor-only P0 debug variants",
                sectionStyle);
            y += 40f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 48f), "A — Energy Routing only", debugButtonStyle))
            {
                Select<EnergyRoutingPrototypeController>(PrototypeVariant.EnergyRouting);
            }
            y += 56f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 48f), "B — Path / Expedition Routing", debugButtonStyle))
            {
                Select<PathExpeditionPrototypeController>(PrototypeVariant.PathExpeditionRouting);
            }
            y += 56f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 48f), "C — Flow / Network Restoration", debugButtonStyle))
            {
                Select<FlowNetworkPrototypeController>(PrototypeVariant.FlowNetworkRestoration);
            }
            y += 58f;
        }
#endif

        private void StartSelectedMeta()
        {
            if (!playtestRuntime.TryBeginSelectedMeta(
                    playtestId,
                    PrototypeVariant.EnergyRouting,
                    out setupError))
            {
                return;
            }

            enabled = false;
            var controller = gameObject.AddComponent<EnergyRoutingPrototypeController>();
            controller.ConfigureSelectedMeta(playtestId);
        }

#if UNITY_EDITOR
        private void Select<T>(string variant) where T : MonoBehaviour
        {
            if (!playtestRuntime.TryBeginVariant(playtestId, variant, out setupError))
            {
                return;
            }

            enabled = false;
            gameObject.AddComponent<T>();
        }
#endif
    }
}
