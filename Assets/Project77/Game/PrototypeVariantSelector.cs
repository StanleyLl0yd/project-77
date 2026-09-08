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
            var contentHeight = string.IsNullOrEmpty(setupError) ? 690f : 756f;
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
            var sectionStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            var debugButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
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
            GUI.Label(new Rect(0f, y, contentWidth, 34f), "Prototype 0.1 — P1", titleStyle);
            y += 44f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 70f),
                "Selected core + meta loop: puzzle → reward → generator repair → island change → discover 77 → choose whether to continue.",
                bodyStyle);
            y += 78f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 42f),
                $"Build {playtestRuntime.BuildVersion}\nSchema v{Project77.Analytics.PrototypeAnalyticsEvent.CurrentSchemaVersion}",
                captionStyle);
            y += 50f;

            GUI.Label(new Rect(0f, y, contentWidth, 28f), "Anonymous playtest ID", bodyStyle);
            y += 32f;
            playtestId = GUI.TextField(
                new Rect(8f, y, contentWidth - 16f, 48f),
                playtestId ?? string.Empty,
                64,
                textFieldStyle);
            y += 66f;

            if (GUI.Button(
                    new Rect(8f, y, contentWidth - 16f, 68f),
                    "Start P1 — Energy Routing + Island",
                    buttonStyle))
            {
                StartSelectedMeta();
            }
            y += 88f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 34f),
                "P0 debug variants",
                sectionStyle);
            y += 42f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 50f), "A — Energy Routing only", debugButtonStyle))
            {
                Select<EnergyRoutingPrototypeController>(PrototypeVariant.EnergyRouting);
            }
            y += 58f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 50f), "B — Path / Expedition Routing", debugButtonStyle))
            {
                Select<PathExpeditionPrototypeController>(PrototypeVariant.PathExpeditionRouting);
            }
            y += 58f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 50f), "C — Flow / Network Restoration", debugButtonStyle))
            {
                Select<FlowNetworkPrototypeController>(PrototypeVariant.FlowNetworkRestoration);
            }
            y += 62f;

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

        private void Select<T>(string variant) where T : MonoBehaviour
        {
            if (!playtestRuntime.TryBeginVariant(playtestId, variant, out setupError))
            {
                return;
            }

            enabled = false;
            gameObject.AddComponent<T>();
        }
    }
}
