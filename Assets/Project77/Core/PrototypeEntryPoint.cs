using UnityEngine;

namespace Project77.Core
{
    public sealed class PrototypeEntryPoint : MonoBehaviour
    {
        private static readonly Rect PanelRect = new Rect(24f, 24f, 560f, 170f);

        private void OnGUI()
        {
            GUI.Box(PanelRect, string.Empty);

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };

            var body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };

            GUI.Label(new Rect(44f, 42f, 520f, 40f), "Project 77 — Prototype 0.1", title);
            GUI.Label(
                new Rect(44f, 88f, 520f, 90f),
                "Unity bootstrap scene. Placeholder presentation is intentional.\nNext implementation target: the three P0 puzzle variants defined in Docs/28_PROTOTYPE_01_SPEC.md.",
                body);
        }
    }
}
