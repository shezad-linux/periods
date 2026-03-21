using System.Collections.Generic;
using Lumenfall.Data;
using Lumenfall.Services;
using Lumenfall.World;
using UnityEngine;

namespace Lumenfall.World.Rooms
{
    public sealed class RoomRoot : MonoBehaviour
    {
        [SerializeField] private RoomDefinition definition;
        [SerializeField] private Collider2D activationVolume;
        [SerializeField] private BoxCollider2D cameraBounds;
        [SerializeField] private List<GameObject> managedContent = new();
        [SerializeField] private List<EnemySpawner> enemySpawners = new();
        [SerializeField] private List<LoreTrigger> loreTriggers = new();

        private AreaSceneRoot _areaSceneRoot;

        public RoomDefinition Definition => definition;

        public BoxCollider2D CameraBounds => cameraBounds;

        public void Initialize(RoomDefinition roomDefinition, Collider2D roomActivationVolume)
        {
            definition = roomDefinition;
            activationVolume = roomActivationVolume;
        }

        private void Awake()
        {
            if (activationVolume == null)
            {
                activationVolume = GetComponent<Collider2D>();
            }

            if (activationVolume is BoxCollider2D boxCollider)
            {
                boxCollider.isTrigger = true;
            }

            if (enemySpawners.Count == 0)
            {
                enemySpawners.AddRange(GetComponentsInChildren<EnemySpawner>(true));
            }

            if (loreTriggers.Count == 0)
            {
                loreTriggers.AddRange(GetComponentsInChildren<LoreTrigger>(true));
            }
        }

        public void BindToArea(AreaSceneRoot areaSceneRoot)
        {
            _areaSceneRoot = areaSceneRoot;
            _areaSceneRoot.RegisterRoom(this);
        }

        public void SetRoomActive(bool isActive)
        {
            foreach (GameObject content in managedContent)
            {
                if (content != null)
                {
                    content.SetActive(isActive);
                }
            }

            foreach (EnemySpawner enemySpawner in enemySpawners)
            {
                if (enemySpawner != null)
                {
                    enemySpawner.SetRoomActive(isActive);
                }
            }

            if (isActive && ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.SetActiveRoom(definition);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _areaSceneRoot?.EnterRoom(this);
        }
    }
}
