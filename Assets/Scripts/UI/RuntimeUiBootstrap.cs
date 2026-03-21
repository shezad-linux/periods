using Lumenfall.Core;
using Lumenfall.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Lumenfall.UI
{
    public static class RuntimeUiBootstrap
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHooks()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureUi()
        {
            EnsureEventSystem();
            EnsurePersistentHud();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            EnsureEventSystem();
            EnsurePersistentHud();

            if (scene.name == LumenfallSceneNames.Boot)
            {
                if (ServiceRegistry.TryGet(out SceneService sceneService))
                {
                    sceneService.LoadMainMenu();
                }

                return;
            }

            if (scene.name == LumenfallSceneNames.MainMenu && Object.FindFirstObjectByType<MainMenuController>() == null)
            {
                new GameObject("MainMenuController").AddComponent<MainMenuController>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(eventSystemObject);
        }

        private static void EnsurePersistentHud()
        {
            if (Object.FindFirstObjectByType<MobileHudController>() != null)
            {
                return;
            }

            GameObject hudObject = new("MobileHudController");
            hudObject.AddComponent<MobileHudController>();
            Object.DontDestroyOnLoad(hudObject);
        }
    }
}
