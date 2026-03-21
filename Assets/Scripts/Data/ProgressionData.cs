using System;
using System.Collections.Generic;
using Lumenfall.Core;
using UnityEngine;

namespace Lumenfall.Data
{
    [Serializable]
    public sealed class UserSettingsData
    {
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float ambienceVolume = 0.8f;
        public bool vibrationEnabled = true;
        public bool showTouchControls = true;
    }

    [Serializable]
    public sealed class BossProgressData
    {
        public string bossId = string.Empty;
        public bool defeated;
    }

    [Serializable]
    public sealed class SaveGameData
    {
        public int saveVersion = LumenfallConstants.SaveVersion;
        public int slotIndex;
        public string currentAreaId = string.Empty;
        public string activeRoomId = string.Empty;
        public string checkpointId = string.Empty;
        public Vector3 playerPosition = Vector3.zero;
        public int maxHealth = 5;
        public int currentHealth = 5;
        public int corruptionScore;
        public UserSettingsData settings = new();
        public List<AbilityType> unlockedAbilities = new();
        public List<string> defeatedBossIds = new();
        public List<string> worldFlags = new();
        public List<string> collectedLoreIds = new();
        public List<string> visitedRoomIds = new();
        public List<MapRoomData> mapDiscovery = new();

        public bool HasAbility(AbilityType abilityType)
        {
            return unlockedAbilities.Contains(abilityType);
        }

        public void UnlockAbility(AbilityType abilityType)
        {
            if (!unlockedAbilities.Contains(abilityType))
            {
                unlockedAbilities.Add(abilityType);
            }
        }

        public void AddWorldFlag(string flagId)
        {
            if (!string.IsNullOrWhiteSpace(flagId) && !worldFlags.Contains(flagId))
            {
                worldFlags.Add(flagId);
            }
        }

        public void MarkBossDefeated(string bossId)
        {
            if (!string.IsNullOrWhiteSpace(bossId) && !defeatedBossIds.Contains(bossId))
            {
                defeatedBossIds.Add(bossId);
            }
        }

        public void MarkRoomVisited(string roomId, Vector2Int mapPosition, IList<string> connections)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return;
            }

            if (!visitedRoomIds.Contains(roomId))
            {
                visitedRoomIds.Add(roomId);
            }

            MapRoomData mapRoom = mapDiscovery.Find(entry => entry.roomId == roomId);
            if (mapRoom == null)
            {
                mapRoom = new MapRoomData
                {
                    roomId = roomId,
                    mapPosition = mapPosition,
                    visited = true
                };

                if (connections != null)
                {
                    mapRoom.connections.AddRange(connections);
                }

                mapDiscovery.Add(mapRoom);
                return;
            }

            mapRoom.mapPosition = mapPosition;
            mapRoom.visited = true;
            mapRoom.connections.Clear();
            if (connections != null)
            {
                mapRoom.connections.AddRange(connections);
            }
        }

        public static SaveGameData CreateDefault(int slotIndex)
        {
            return new SaveGameData
            {
                slotIndex = slotIndex,
                currentHealth = 5,
                maxHealth = 5
            };
        }
    }

    [Serializable]
    public sealed class GameSessionState
    {
        public string currentAreaId = string.Empty;
        public string activeRoomId = string.Empty;
        public string respawnCheckpointId = string.Empty;
        public bool isPaused;
        public bool isBossEncounterActive;
        public bool releaseChoiceSelected;
        public List<string> loadedAreaScenes = new();
    }
}
