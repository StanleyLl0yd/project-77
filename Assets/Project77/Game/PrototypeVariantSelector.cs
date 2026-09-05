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

            playtestId = "p0-" + Guid.NewGuid().ToString("N").Substring(0, 8);
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
            var contentHeight = string.IsNullOrEmpty(setupError) ? 500f : 566f;
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
            GUI.Label(new Rect(0f, y, contentWidth, 34f), "P0 Playtest Setup", titleStyle);
            y += 42f;

            GUI.Label(
                new Rect(8f, y, contentWidth - 16f, 60f),
                "Moderator setup: record the assigned order, choose the mechanic, then hand the device to the tester.",
                bodyStyle);
            y += 68f;

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

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 62f), "A — Energy Routing", buttonStyle))
            {
                Select<EnergyRoutingPrototypeController>(PrototypeVariant.EnergyRouting);
            }
            y += 72f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 62f), "B — Path / Expedition Routing", buttonStyle))
            {
                Select<PathExpeditionPrototypeController>(PrototypeVariant.PathExpeditionRouting);
            }
            y += 72f;

            if (GUI.Button(new Rect(8f, y, contentWidth - 16f, 62f), "C — Flow / Network Restoration", buttonStyle))
            {
                Select<FlowNetworkPrototypeController>(PrototypeVariant.FlowNetworkRestoration);
            }
            y += 72f;

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
