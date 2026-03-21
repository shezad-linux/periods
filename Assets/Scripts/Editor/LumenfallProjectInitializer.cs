#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Lumenfall.Core;
using Lumenfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lumenfall.Editor
{
    [InitializeOnLoad]
    public static class LumenfallProjectInitializer
    {
        private const string SessionKey = "Lumenfall.ProjectInitialized";
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        static LumenfallProjectInitializer()
        {
            EditorApplication.delayCall += EnsureProjectInitialized;
        }

        [MenuItem("Lumenfall/Initialize Project")]
        public static void InitializeFromMenu()
        {
            EnsureProjectInitialized(true);
        }

        private static void EnsureProjectInitialized(bool force = false)
        {
            if (!force && SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureLayers();
                EnsurePlayerSettings();
                EnsureScenes();
                EnsureBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        private static void EnsureScenes()
        {
            EnsureScene("Assets/Scenes/System/Boot.unity", includeAreaRoot: false);
            EnsureScene("Assets/Scenes/System/MainMenu.unity", includeAreaRoot: false);
            EnsureScene("Assets/Scenes/System/PersistentSystems.unity", includeAreaRoot: false);
            EnsureScene("Assets/Scenes/Areas/SilentGate.unity", includeAreaRoot: true);
            EnsureScene("Assets/Scenes/Areas/ForgottenMarket.unity", includeAreaRoot: true);
            EnsureScene("Assets/Scenes/Areas/DeepArchives.unity", includeAreaRoot: true);
            EnsureScene("Assets/Scenes/Areas/RootCaverns.unity", includeAreaRoot: true);
            EnsureScene("Assets/Scenes/Areas/CoreOfLumenfall.unity", includeAreaRoot: true);
        }

        private static void EnsureScene(string scenePath, bool includeAreaRoot)
        {
            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? "Assets/Scenes");
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            EnsureCameraExists();

            if (includeAreaRoot)
            {
                EnsureAreaRootExists();
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void EnsureCameraExists()
        {
            if (Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureAreaRootExists()
        {
            if (Object.FindFirstObjectByType<AreaSceneRoot>() == null)
            {
                new GameObject("AreaSceneRoot").AddComponent<AreaSceneRoot>();
            }
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                CreateBuildScene("Assets/Scenes/System/Boot.unity"),
                CreateBuildScene("Assets/Scenes/System/MainMenu.unity"),
                CreateBuildScene("Assets/Scenes/System/PersistentSystems.unity"),
                CreateBuildScene("Assets/Scenes/Areas/SilentGate.unity"),
                CreateBuildScene("Assets/Scenes/Areas/ForgottenMarket.unity"),
                CreateBuildScene("Assets/Scenes/Areas/DeepArchives.unity"),
                CreateBuildScene("Assets/Scenes/Areas/RootCaverns.unity"),
                CreateBuildScene("Assets/Scenes/Areas/CoreOfLumenfall.unity"),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static EditorBuildSettingsScene CreateBuildScene(string path)
        {
            return new EditorBuildSettingsScene(path, true);
        }

        private static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = LumenfallConstants.CompanyName;
            PlayerSettings.productName = LumenfallConstants.ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.lumenfall.ashesoflumenfall");
        }

        private static void EnsureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (assets.Length == 0)
            {
                return;
            }

            SerializedObject tagManager = new(assets[0]);
            SerializedProperty layersProperty = tagManager.FindProperty("layers");
            if (layersProperty == null || !layersProperty.isArray)
            {
                return;
            }

            foreach (string layerName in LumenfallLayerNames.Recommended)
            {
                EnsureLayer(layersProperty, layerName);
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureLayer(SerializedProperty layersProperty, string layerName)
        {
            for (int index = 8; index < layersProperty.arraySize; index++)
            {
                SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(index);
                if (layerProperty != null && layerProperty.stringValue == layerName)
                {
                    return;
                }
            }

            for (int index = 8; index < layersProperty.arraySize; index++)
            {
                SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(index);
                if (layerProperty != null && string.IsNullOrEmpty(layerProperty.stringValue))
                {
                    layerProperty.stringValue = layerName;
                    return;
                }
            }
        }
    }
}
#endif
