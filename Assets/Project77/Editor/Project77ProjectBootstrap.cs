#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Project77.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Project77.Editor
{
    [InitializeOnLoad]
    public static class Project77ProjectBootstrap
    {
        private const string Root = "Assets/Project77";
        private const string SettingsRoot = Root + "/Settings";
        private const string ScenesRoot = Root + "/Scenes";
        private const string ResourcesRoot = Root + "/Content/Resources";
        private const string RendererPath = SettingsRoot + "/Project77Renderer.asset";
        private const string PipelinePath = SettingsRoot + "/Project77URP.asset";
        private const string ScenePath = ScenesRoot + "/Bootstrap.unity";
        private const string BuildInfoPath = ResourcesRoot + "/PrototypeBuildInfo.json";
        private const string PrototypeApplicationId = "com.sl.project77.prototype";

        static Project77ProjectBootstrap()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += ApplyBaseline;
            }
        }

        [MenuItem("Project 77/Validate and Apply Prototype Baseline")]
        private static void ApplyBaseline()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ApplyBaseline;
                return;
            }

            try
            {
                ApplyBaselineInternal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void ApplyBaselineForBuild()
        {
            ApplyBaselineInternal();
            WritePrototypeBuildInfo();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyBaselineInternal()
        {
            EnsureDirectory(SettingsRoot);
            EnsureDirectory(ScenesRoot);
            ConfigurePlayerSettings();
            ConfigureUrp();
            EnsureBootstrapScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Project 77 prototype baseline is configured. Verify Console, Android module availability, and an actual Android build before marking P77-001/P77-003 complete.");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "SL";
            PlayerSettings.productName = "Project77";
            PlayerSettings.bundleVersion = "0.0.1-prototype";
#pragma warning disable CS0618
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PrototypeApplicationId);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#pragma warning restore CS0618
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        private static void ConfigureUrp()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "Project77Renderer";
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                pipelineAsset.name = "Project77URP";
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
        }

        private static void EnsureBootstrapScene()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);

                var entryPoint = new GameObject("Prototype Entry Point");
                entryPoint.AddComponent<PrototypeEntryPoint>();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            var existingScenes = EditorBuildSettings.scenes;
            var bootstrapPresent = false;
            foreach (var scene in existingScenes)
            {
                if (scene.path == ScenePath)
                {
                    bootstrapPresent = true;
                    break;
                }
            }

            if (!bootstrapPresent)
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            }
        }

        private static void WritePrototypeBuildInfo()
        {
            EnsureDirectory(ResourcesRoot);
            var revision = Environment.GetEnvironmentVariable("BUILD_REVISION");
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = "local";
            }

            var compactRevision = revision.Length > 12 ? revision.Substring(0, 12) : revision;
            var info = new PrototypeBuildInfoData
            {
                schema_version = 1,
                build_version = PlayerSettings.bundleVersion + "+" + compactRevision,
                commit_sha = revision,
                unity_version = Application.unityVersion
            };

            File.WriteAllText(
                BuildInfoPath,
                JsonUtility.ToJson(info, true),
                new UTF8Encoding(false));
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            Directory.CreateDirectory(assetPath);
            AssetDatabase.Refresh();
        }

        [Serializable]
        private sealed class PrototypeBuildInfoData
        {
            public int schema_version;
            public string build_version;
            public string commit_sha;
            public string unity_version;
        }
    }
}
#endif
