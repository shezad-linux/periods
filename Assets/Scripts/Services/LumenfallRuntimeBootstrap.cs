using Lumenfall.Core;
using UnityEngine;

namespace Lumenfall.Services
{
    public sealed class PersistentSystemsRoot : MonoBehaviour
    {
        private static PersistentSystemsRoot _instance;

        public static PersistentSystemsRoot Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeSystems()
        {
            Application.targetFrameRate = LumenfallConstants.TargetFrameRate;
            QualitySettings.vSyncCount = 0;

            if (_instance != null)
            {
                return;
            }

            GameObject rootObject = new(LumenfallConstants.PersistentSystemsRootName);
            rootObject.AddComponent<PersistentSystemsRoot>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureService<InputService>();
            EnsureService<SaveService>();
            EnsureService<GameStateService>();
            EnsureService<SceneService>();
            EnsureService<AudioService>();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.SaveToActiveSlot();
            }
        }

        private void OnApplicationQuit()
        {
            if (ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.SaveToActiveSlot();
            }
        }

        private void EnsureService<T>() where T : Component
        {
            if (GetComponent<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }
    }
}
