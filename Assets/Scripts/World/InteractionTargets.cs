using System.Collections;
using Lumenfall.Data;
using Lumenfall.Gameplay.Player;
using Lumenfall.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Lumenfall.World
{
    public enum RespawnRule
    {
        OnRoomReenter = 0,
        CheckpointOnly = 1,
        Never = 2
    }

    [System.Serializable]
    public sealed class SpawnGroup
    {
        public GameObject prefab;
        public Transform[] spawnPoints;
    }

    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private SpawnGroup[] spawnGroups = { };
        [SerializeField] private RespawnRule respawnRule;
        [SerializeField] private bool disableSpawnedOnRoomExit = true;

        private readonly System.Collections.Generic.List<GameObject> _spawnedInstances = new();
        private bool _hasSpawnedOnce;

        public void SetRoomActive(bool isActive)
        {
            if (isActive)
            {
                SpawnIfNeeded();
                foreach (GameObject instance in _spawnedInstances)
                {
                    if (instance != null)
                    {
                        instance.SetActive(true);
                    }
                }

                return;
            }

            if (!disableSpawnedOnRoomExit)
            {
                return;
            }

            foreach (GameObject instance in _spawnedInstances)
            {
                if (instance != null)
                {
                    instance.SetActive(false);
                }
            }
        }

        private void SpawnIfNeeded()
        {
            if (_hasSpawnedOnce && respawnRule == RespawnRule.Never)
            {
                return;
            }

            if (_hasSpawnedOnce && respawnRule == RespawnRule.CheckpointOnly)
            {
                return;
            }

            if (_spawnedInstances.Count == 0 || respawnRule == RespawnRule.OnRoomReenter)
            {
                ClearMissing();
                if (_spawnedInstances.Count == 0)
                {
                    foreach (SpawnGroup group in spawnGroups)
                    {
                        if (group?.prefab == null || group.spawnPoints == null)
                        {
                            continue;
                        }

                        foreach (Transform spawnPoint in group.spawnPoints)
                        {
                            if (spawnPoint == null)
                            {
                                continue;
                            }

                            GameObject spawned = Instantiate(group.prefab, spawnPoint.position, Quaternion.identity, transform);
                            _spawnedInstances.Add(spawned);
                        }
                    }
                }
            }

            _hasSpawnedOnce = true;
        }

        private void ClearMissing()
        {
            _spawnedInstances.RemoveAll(instance => instance == null);
        }
    }

    public sealed class WorldGate : MonoBehaviour
    {
        [SerializeField] private GateRequirement gateRequirement = new();
        [SerializeField] private Collider2D blockerCollider;
        [SerializeField] private GameObject blockerVisual;
        [SerializeField] private bool refreshEveryFrame;

        private void Awake()
        {
            RefreshGate();
        }

        private void Update()
        {
            if (refreshEveryFrame)
            {
                RefreshGate();
            }
        }

        public void RefreshGate()
        {
            if (!ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                return;
            }

            bool isOpen = gameStateService.IsGateOpen(gateRequirement);
            if (blockerCollider != null)
            {
                blockerCollider.enabled = !isOpen;
            }

            if (blockerVisual != null)
            {
                blockerVisual.SetActive(!isOpen);
            }
        }
    }

    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        public string spawnPointId = string.Empty;
    }

    public sealed class AmbientZone : MonoBehaviour
    {
        [SerializeField] private AudioClip ambientClip;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (ServiceRegistry.TryGet(out AudioService audioService))
            {
                audioService.SetAmbientClip(ambientClip);
            }
        }
    }

    public sealed class CheckpointShrine : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private string checkpointId = "checkpoint";
        [SerializeField] private Rooms.RoomRoot roomRoot;
        [SerializeField] private UnityEvent onActivated;

        public ContextActionType ActionType => ContextActionType.Interact;

        public int Priority => 3;

        public Transform TargetTransform => transform;

        public void Configure(Rooms.RoomRoot root)
        {
            roomRoot = root;
        }

        public bool IsAvailable(SaveGameData saveData)
        {
            return true;
        }

        public void Perform(GameObject actor)
        {
            if (roomRoot == null)
            {
                roomRoot = GetComponentInParent<Rooms.RoomRoot>();
            }

            if (ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                string roomId = roomRoot != null && roomRoot.Definition != null ? roomRoot.Definition.roomId : string.Empty;
                string areaId = roomRoot != null && roomRoot.Definition != null && roomRoot.Definition.area != null ? roomRoot.Definition.area.areaId : string.Empty;
                gameStateService.RecordCheckpoint(checkpointId, roomId, areaId, actor.transform.position);
            }

            onActivated?.Invoke();
        }
    }

    public sealed class LoreTrigger : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private LoreEntryDefinition loreEntry;
        [SerializeField] private bool requiresInteraction = true;
        [SerializeField] private UnityEvent onLoreCollected;

        public ContextActionType ActionType => ContextActionType.Interact;

        public int Priority => 3;

        public Transform TargetTransform => transform;

        public bool IsAvailable(SaveGameData saveData)
        {
            return loreEntry != null && saveData != null && !saveData.collectedLoreIds.Contains(loreEntry.loreId);
        }

        public void Perform(GameObject actor)
        {
            if (loreEntry == null || !ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                return;
            }

            gameStateService.RecordLore(loreEntry.loreId);
            onLoreCollected?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (requiresInteraction || !other.CompareTag("Player"))
            {
                return;
            }

            Perform(other.gameObject);
        }
    }

    public sealed class GrappleAnchorTarget : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private Transform anchorPoint;

        public ContextActionType ActionType => ContextActionType.Grapple;

        public int Priority => 1;

        public Transform TargetTransform => anchorPoint != null ? anchorPoint : transform;

        public bool IsAvailable(SaveGameData saveData)
        {
            return saveData != null && saveData.HasAbility(AbilityType.CrystalGrapple);
        }

        public void Perform(GameObject actor)
        {
            if (actor.TryGetComponent(out PlayerMotor2D playerMotor))
            {
                playerMotor.TeleportTo(TargetTransform.position);
            }
            else
            {
                actor.transform.position = TargetTransform.position;
            }
        }
    }

    public sealed class PhaseWallTarget : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private Collider2D wallCollider;
        [SerializeField] private float disabledDuration = 1.5f;

        public ContextActionType ActionType => ContextActionType.PhaseWall;

        public int Priority => 2;

        public Transform TargetTransform => transform;

        public bool IsAvailable(SaveGameData saveData)
        {
            return saveData != null && saveData.HasAbility(AbilityType.PhaseShift);
        }

        public void Configure(Collider2D colliderToToggle, float duration = -1f)
        {
            wallCollider = colliderToToggle;
            if (duration >= 0f)
            {
                disabledDuration = duration;
            }
        }

        public void Perform(GameObject actor)
        {
            StartCoroutine(DisableWallRoutine());
        }

        private IEnumerator DisableWallRoutine()
        {
            if (wallCollider == null)
            {
                yield break;
            }

            wallCollider.enabled = false;
            yield return new WaitForSeconds(disabledDuration);
            wallCollider.enabled = true;
        }
    }

    public sealed class PulseTarget : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private bool disableObjectOnPulse;
        [SerializeField] private UnityEvent onPulse;

        public ContextActionType ActionType => ContextActionType.Pulse;

        public int Priority => 4;

        public Transform TargetTransform => transform;

        public bool IsAvailable(SaveGameData saveData)
        {
            return saveData != null && saveData.HasAbility(AbilityType.LumenPulse);
        }

        public void Configure(bool disableOnPulse)
        {
            disableObjectOnPulse = disableOnPulse;
        }

        public void Perform(GameObject actor)
        {
            onPulse?.Invoke();
            if (disableObjectOnPulse)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public sealed class WorldInteractable : MonoBehaviour, IContextActionTarget
    {
        [SerializeField] private int priority = 3;
        [SerializeField] private UnityEvent onInteracted;

        public ContextActionType ActionType => ContextActionType.Interact;

        public int Priority => priority;

        public Transform TargetTransform => transform;

        public bool IsAvailable(SaveGameData saveData)
        {
            return true;
        }

        public void Perform(GameObject actor)
        {
            onInteracted?.Invoke();
        }
    }
}
