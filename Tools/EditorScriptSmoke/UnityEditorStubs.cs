using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitializeOnLoadAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string itemName)
        {
        }
    }

    public static class EditorApplication
    {
        public static bool isCompiling => false;
        public static bool isUpdating => false;
        public static Action delayCall;
    }

    [Flags]
    public enum ImportAssetOptions
    {
        Default = 0,
        ForceSynchronousImport = 1
    }

    public static class AssetDatabase
    {
        public static bool IsValidFolder(string path) => true;
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => default;
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static void Refresh(ImportAssetOptions options) { }
    }

    public enum BuildTargetGroup
    {
        Android
    }

    public enum ScriptingImplementation
    {
        IL2CPP
    }

    public enum AndroidSdkVersions
    {
        AndroidApiLevel26 = 26,
        AndroidApiLevel36 = 36
    }

    [Flags]
    public enum AndroidArchitecture
    {
        None = 0,
        ARM64 = 1
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }

        public static void SetApplicationIdentifier(BuildTargetGroup buildTargetGroup, string identifier) { }
        public static void SetScriptingBackend(BuildTargetGroup buildTargetGroup, ScriptingImplementation implementation) { }

        public static class Android
        {
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
            public static AndroidArchitecture targetArchitectures { get; set; }
        }
    }

    public sealed class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled)
        {
            this.path = path;
            this.enabled = enabled;
        }

        public string path { get; }
        public bool enabled { get; }
    }

    public static class EditorBuildSettings
    {
        private static EditorBuildSettingsScene[] value = Array.Empty<EditorBuildSettingsScene>();

        public static EditorBuildSettingsScene[] scenes
        {
            get => value;
            set => EditorBuildSettings.value = value ?? Array.Empty<EditorBuildSettingsScene>();
        }
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup
    {
        EmptyScene
    }

    public enum NewSceneMode
    {
        Single
    }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => default;
        public static bool SaveScene(Scene scene, string path) => true;
    }
}
