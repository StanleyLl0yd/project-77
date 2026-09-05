using UnityEngine;

namespace Project77.Game
{
    public static class PrototypeGuiLayout
    {
        private const float ReferenceDpi = 160f;
        private const float MinScale = 1f;
        private const float MaxScale = 2.5f;
        private static Matrix4x4 previousMatrix;
        private static Rect safeArea;
        private static float topInset;
        private static float scale = 1f;
        private static bool active;

        public static float Scale
        {
            get
            {
                Refresh();
                return scale;
            }
        }

        public static float Width
        {
            get
            {
                Refresh();
                return safeArea.width / scale;
            }
        }

        public static float Height
        {
            get
            {
                Refresh();
                return safeArea.height / scale;
            }
        }

        public static bool IsCompact => Mathf.Min(Width, Height) < 360f;

        public static void Begin()
        {
            Refresh();
            if (active)
            {
                return;
            }

            previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(safeArea.x, topInset, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            GUI.skin.label.fontSize = 18;
            GUI.skin.button.fontSize = 18;
            GUI.skin.box.fontSize = 18;
            GUI.skin.textField.fontSize = 20;
            active = true;
        }

        public static void End()
        {
            if (!active)
            {
                return;
            }

            GUI.matrix = previousMatrix;
            active = false;
        }

        public static Vector2 ScreenToGui(Vector2 screenPosition)
        {
            Refresh();
            return new Vector2(
                (screenPosition.x - safeArea.x) / scale,
                ((Screen.height - screenPosition.y) - topInset) / scale);
        }

        public static float ContentWidth(float maxWidth, float horizontalMargin = 20f)
        {
            return Mathf.Max(0f, Mathf.Min(maxWidth, Width - horizontalMargin * 2f));
        }

        public static float CalculateScale(int width, int height, float dpi)
        {
            if (width <= 0 || height <= 0)
            {
                return MinScale;
            }

            var shortestSide = Mathf.Min(width, height);
            var candidate = dpi >= 120f && dpi <= 1000f
                ? dpi / ReferenceDpi
                : shortestSide / 480f;

            if (width > height)
            {
                candidate = Mathf.Min(candidate, 1.85f);
            }

            return Mathf.Clamp(candidate, MinScale, MaxScale);
        }

        private static void Refresh()
        {
            scale = CalculateScale(Screen.width, Screen.height, Screen.dpi);
            safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            topInset = Screen.height - (safeArea.y + safeArea.height);
        }
    }
}
