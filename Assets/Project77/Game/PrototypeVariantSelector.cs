using UnityEngine;

namespace Project77.Game
{
    public sealed class PrototypeVariantSelector : MonoBehaviour
    {
        private void OnGUI()
        {
            var width = Mathf.Min(520f, Screen.width - 40f);
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

            GUI.Label(new Rect(x, 50f, width, 40f), "Project 77 — P0 Core Comparison", titleStyle);
            GUI.Label(new Rect(x, 96f, width, 62f), "Choose a greybox mechanic. Variant order is a playtest variable and must be recorded.", bodyStyle);

            if (GUI.Button(new Rect(x, 180f, width, 56f), "A — Energy Routing"))
                Select<EnergyRoutingPrototypeController>();
            if (GUI.Button(new Rect(x, 252f, width, 56f), "B — Path / Expedition Routing"))
                Select<PathExpeditionPrototypeController>();
            if (GUI.Button(new Rect(x, 324f, width, 56f), "C — Flow / Network Restoration"))
                Select<FlowNetworkPrototypeController>();
        }

        private void Select<T>() where T : MonoBehaviour
        {
            enabled = false;
            gameObject.AddComponent<T>();
        }
    }
}
