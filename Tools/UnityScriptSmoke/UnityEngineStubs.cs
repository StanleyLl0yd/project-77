using System;

namespace UnityEngine
{
    public class Object
    {
    }

    public class Component : Object
    {
        public GameObject gameObject { get; } = new GameObject();

        public T GetComponent<T>() where T : Component
        {
            return default;
        }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public sealed class GameObject : Object
    {
        public T AddComponent<T>() where T : Component
        {
            return default;
        }
    }

    public sealed class TextAsset : Object
    {
        public string text { get; set; }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object
        {
            return default;
        }
    }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json)
        {
            return default;
        }

        public static string ToJson(object obj, bool prettyPrint)
        {
            return string.Empty;
        }
    }

    public static class Application
    {
        public static string version => "stub";
        public static string unityVersion => "stub";
        public static string persistentDataPath => ".";
    }

    public static class SystemInfo
    {
        public static string deviceModel => "stub";
        public static string operatingSystem => "stub";
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
    }

    public static class Time
    {
        public static float realtimeSinceStartup => 0f;
    }

    public static class GUIUtility
    {
        public static string systemCopyBuffer { get; set; }
    }

    public static class GUI
    {
        public static Color backgroundColor { get; set; }
        public static bool enabled { get; set; } = true;
        public static GUISkin skin { get; } = new GUISkin();

        public static void Label(Rect position, string text)
        {
        }

        public static void Label(Rect position, string text, GUIStyle style)
        {
        }

        public static void Box(Rect position, string text)
        {
        }

        public static bool Button(Rect position, string text)
        {
            return false;
        }

        public static string TextField(Rect position, string text, int maxLength)
        {
            return text;
        }
    }

    public sealed class GUISkin
    {
        public GUIStyle label { get; } = new GUIStyle();
    }

    public sealed class GUIStyle
    {
        public GUIStyle()
        {
        }

        public GUIStyle(GUIStyle other)
        {
        }

        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
    }

    public enum FontStyle
    {
        Normal,
        Bold
    }

    public enum TextAnchor
    {
        UpperLeft,
        MiddleCenter
    }

    public struct Vector2 : IEquatable<Vector2>
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;

        public bool Equals(Vector2 other)
        {
            return x.Equals(other.x) && y.Equals(other.y);
        }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float x;
        public float y;
        public float width;
        public float height;

        public bool Contains(Vector2 point)
        {
            return point.x >= x && point.x <= x + width && point.y >= y && point.y <= y + height;
        }
    }

    public struct Color
    {
        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            a = 1f;
        }

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public float r;
        public float g;
        public float b;
        public float a;
    }

    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
    }

    public static class Input
    {
        public static int touchCount => 0;
        public static Vector2 mousePosition => default;
        public static Touch GetTouch(int index) => default;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
    }

    public struct Touch
    {
        public Vector2 position { get; set; }
        public TouchPhase phase { get; set; }
    }

    public enum TouchPhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled
    }
}
