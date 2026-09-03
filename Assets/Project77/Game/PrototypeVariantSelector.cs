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
            var width = Mathf.Min(560f, Screen.width - 40f);
            var x = (Screen.width - width) * 0.5f;
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true
            };

            GUI.Label(new Rect(x, 36f, width, 40f), "Project 77 — P0 Playtest Setup", titleStyle);
            GUI.Label(
                new Rect(x, 82f, width, 52f),
                "Moderator setup. Record variant order, select the assigned mechanic, then hand the device to the tester.",
                bodyStyle);
            GUI.Label(
                new Rect(x, 136f, width, 24f),
                $"Build: {playtestRuntime.BuildVersion} · schema v{Project77.Analytics.PrototypeAnalyticsEvent.CurrentSchemaVersion}",
                bodyStyle);

            GUI.Label(new Rect(x, 166f, width, 22f), "Anonymous playtest ID", bodyStyle);
            playtestId = GUI.TextField(new Rect(x, 190f, width, 40f), playtestId ?? string.Empty, 64);

            if (GUI.Button(new Rect(x, 250f, width, 54f), "A — Energy Routing"))
                Select<EnergyRoutingPrototypeController>(PrototypeVariant.EnergyRouting);
            if (GUI.Button(new Rect(x, 316f, width, 54f), "B — Path / Expedition Routing"))
                Select<PathExpeditionPrototypeController>(PrototypeVariant.PathExpeditionRouting);
            if (GUI.Button(new Rect(x, 382f, width, 54f), "C — Flow / Network Restoration"))
                Select<FlowNetworkPrototypeController>(PrototypeVariant.FlowNetworkRestoration);

            if (!string.IsNullOrEmpty(setupError))
            {
                GUI.Label(new Rect(x, 446f, width, 48f), setupError, bodyStyle);
            }
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
