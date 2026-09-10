using System;
using Project77.Puzzle;
using UnityEngine;

namespace Project77.Game
{
    public sealed class PrototypeVariantSelector : MonoBehaviour
    {
        private PrototypePlaytestRuntime playtestRuntime;
        private string playtestId;
        private string setupError = string.Empty;
        private Vector2 setupScroll;

        private void Awake()
        {
            playtestRuntime = GetComponent<PrototypePlaytestRuntime>();
            if (playtestRuntime == null)
            {
                playtestRuntime = gameObject.AddComponent<PrototypePlaytestRuntime>();
            }

            playtestId = "p1-" + Guid.NewGuid().ToString("N").Substring(0, 8);
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
#if UNITY_EDITOR
            var contentHeight = string.IsNullOrEmpty(setupError) ? 700f : 766f;
#else
            var contentHeight = string.IsNullOrEmpty(setupError) ? 458f : 524f;
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
