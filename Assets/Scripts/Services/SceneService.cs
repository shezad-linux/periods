using System;
using System.Collections;
using Lumenfall.Core;
using Lumenfall.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lumenfall.Services
{
    public sealed class SceneService : ServiceBehaviour
    {
        private GameStateService _gameStateService;

        protected override Type ServiceType => typeof(SceneService);

        public string ActiveAreaSceneName { get; private set; } = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            _gameStateService = GetComponent<GameStateService>();
        }

        public void LoadMainMenu()
        {
            if (Application.CanStreamedLevelBeLoaded(LumenfallSceneNames.MainMenu))
            {
                SceneManager.LoadScene(LumenfallSceneNames.MainMenu, LoadSceneMode.Single);
            }
        }

        public void LoadArea(AreaDefinition areaDefinition, string spawnPointId = "")
        {
            if (areaDefinition == null)
            {
                return;
            }

            StartCoroutine(LoadAreaRoutine(areaDefinition, spawnPointId));
        }

        private IEnumerator LoadAreaRoutine(AreaDefinition areaDefinition, string spawnPointId)
        {
            if (!string.IsNullOrWhiteSpace(ActiveAreaSceneName) && ActiveAreaSceneName != areaDefinition.sceneName)
            {
                yield return UnloadSceneIfLoaded(ActiveAreaSceneName);
            }

            if (!Application.CanStreamedLevelBeLoaded(areaDefinition.sceneName))
            {
                Debug.LogWarning($"Scene '{areaDefinition.sceneName}' is not in the build settings. Staying in the current scene.");
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(areaDefinition.sceneName, LoadSceneMode.Additive);
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            ActiveAreaSceneName = areaDefinition.sceneName;
            _gameStateService.RegisterAreaLoaded(areaDefinition.sceneName);
            _gameStateService.SessionState.currentAreaId = areaDefinition.areaId;
            _gameStateService.ActiveSave.currentAreaId = areaDefinition.areaId;
            _gameStateService.ActiveSave.checkpointId = spawnPointId;
        }

        private IEnumerator UnloadSceneIfLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
            while (!unloadOperation.isDone)
            {
                yield return null;
            }

            _gameStateService.RegisterAreaUnloaded(sceneName);
            if (ActiveAreaSceneName == sceneName)
            {
                ActiveAreaSceneName = string.Empty;
            }
        }
    }
}
