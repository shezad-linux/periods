using System;
using System.Collections.Generic;
using Lumenfall.Data;
using UnityEngine;

namespace Lumenfall.Services
{
    public sealed class GameStateService : ServiceBehaviour
    {
        private SaveService _saveService;
        private InputService _inputService;

        protected override Type ServiceType => typeof(GameStateService);

        public SaveGameData ActiveSave { get; private set; }

        public GameSessionState SessionState { get; private set; } = new();

        public int ActiveSlotIndex { get; private set; } = -1;

        protected override void Awake()
        {
            base.Awake();
            _saveService = GetComponent<SaveService>();
            ActiveSave = SaveGameData.CreateDefault(0);
        }

        private void Update()
        {
            if (_inputService == null)
            {
                _inputService = GetComponent<InputService>();
            }

            if (_inputService != null && _inputService.Gameplay.PausePressed)
            {
                SetPaused(!SessionState.isPaused);
            }
        }

        public void BeginNewGame(int slotIndex)
        {
            ActiveSlotIndex = slotIndex;
            ActiveSave = _saveService.CreateNewGame(slotIndex);
            SessionState = new GameSessionState
            {
                currentAreaId = ActiveSave.currentAreaId,
                activeRoomId = ActiveSave.activeRoomId,
                respawnCheckpointId = ActiveSave.checkpointId
            };
            ApplyAudioSettings();
        }

        public void LoadGame(int slotIndex)
        {
            ActiveSlotIndex = slotIndex;
            ActiveSave = _saveService.LoadSlot(slotIndex);
            SessionState = new GameSessionState
            {
                currentAreaId = ActiveSave.currentAreaId,
                activeRoomId = ActiveSave.activeRoomId,
                respawnCheckpointId = ActiveSave.checkpointId
            };
            ApplyAudioSettings();
        }

        public void SaveToActiveSlot()
        {
            if (ActiveSlotIndex < 0 || ActiveSave == null)
            {
                return;
            }

            _saveService.SaveSlot(ActiveSlotIndex, ActiveSave);
        }

        public void RecordCheckpoint(string checkpointId, string roomId, string areaId, Vector3 playerPosition)
        {
            ActiveSave.checkpointId = checkpointId;
            ActiveSave.activeRoomId = roomId;
            ActiveSave.currentAreaId = areaId;
            ActiveSave.playerPosition = playerPosition;
            SessionState.respawnCheckpointId = checkpointId;
            SessionState.activeRoomId = roomId;
            SessionState.currentAreaId = areaId;
            SaveToActiveSlot();
        }

        public void RegisterAreaLoaded(string sceneName)
        {
            if (!SessionState.loadedAreaScenes.Contains(sceneName))
            {
                SessionState.loadedAreaScenes.Add(sceneName);
            }
        }

        public void RegisterAreaUnloaded(string sceneName)
        {
            SessionState.loadedAreaScenes.Remove(sceneName);
        }

        public void SetActiveRoom(RoomDefinition roomDefinition)
        {
            if (roomDefinition == null)
            {
                return;
            }

            ActiveSave.activeRoomId = roomDefinition.roomId;
            SessionState.activeRoomId = roomDefinition.roomId;
            if (roomDefinition.area != null)
            {
                ActiveSave.currentAreaId = roomDefinition.area.areaId;
                SessionState.currentAreaId = roomDefinition.area.areaId;
            }

            ActiveSave.MarkRoomVisited(roomDefinition.roomId, roomDefinition.mapPosition, roomDefinition.connectedRoomIds);
        }

        public void UnlockAbility(AbilityType abilityType)
        {
            ActiveSave.UnlockAbility(abilityType);
            SaveToActiveSlot();
        }

        public void RecordLore(string loreId)
        {
            if (!string.IsNullOrWhiteSpace(loreId) && !ActiveSave.collectedLoreIds.Contains(loreId))
            {
                ActiveSave.collectedLoreIds.Add(loreId);
                SaveToActiveSlot();
            }
        }

        public void RecordBossDefeat(BossDefinition bossDefinition)
        {
            if (bossDefinition == null)
            {
                return;
            }

            ActiveSave.MarkBossDefeated(bossDefinition.bossId);
            ActiveSave.UnlockAbility(bossDefinition.rewardAbility);
            ActiveSave.AddWorldFlag(bossDefinition.rewardFlag);
            ActiveSave.corruptionScore += bossDefinition.corruptionReward;
            SaveToActiveSlot();
        }

        public void AddWorldFlag(string flagId)
        {
            ActiveSave.AddWorldFlag(flagId);
            SaveToActiveSlot();
        }

        public bool IsGateOpen(GateRequirement gateRequirement)
        {
            return gateRequirement == null || gateRequirement.IsSatisfied(ActiveSave);
        }

        public void AddCorruption(int amount)
        {
            ActiveSave.corruptionScore = Mathf.Max(0, ActiveSave.corruptionScore + amount);
        }

        public void SetReleaseChoice(bool selected)
        {
            SessionState.releaseChoiceSelected = selected;
        }

        public void SetPaused(bool paused)
        {
            SessionState.isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public EndingType EvaluateEnding(IReadOnlyList<EndingRuleDefinition> endings)
        {
            if (endings != null)
            {
                foreach (EndingRuleDefinition endingRule in endings)
                {
                    if (endingRule != null && endingRule.Matches(ActiveSave, SessionState.releaseChoiceSelected))
                    {
                        return endingRule.endingType;
                    }
                }
            }

            if (SessionState.releaseChoiceSelected)
            {
                return EndingType.Release;
            }

            return ActiveSave.corruptionScore < 35 ? EndingType.EternalSeal : EndingType.Symbiosis;
        }

        private void ApplyAudioSettings()
        {
            if (TryGetComponent(out AudioService audioService))
            {
                audioService.ApplySettings(ActiveSave.settings);
            }
        }
    }
}
