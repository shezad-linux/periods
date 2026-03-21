using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumenfall.Data
{
    [Serializable]
    public sealed class GateRequirement
    {
        public List<AbilityType> requiredAbilities = new();
        public List<string> requiredBossIds = new();
        public List<string> requiredFlags = new();

        public bool IsSatisfied(SaveGameData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            foreach (AbilityType ability in requiredAbilities)
            {
                if (!saveData.unlockedAbilities.Contains(ability))
                {
                    return false;
                }
            }

            foreach (string bossId in requiredBossIds)
            {
                if (!saveData.defeatedBossIds.Contains(bossId))
                {
                    return false;
                }
            }

            foreach (string flagId in requiredFlags)
            {
                if (!saveData.worldFlags.Contains(flagId))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [CreateAssetMenu(menuName = "Lumenfall/World/Area Definition", fileName = "AreaDefinition")]
    public sealed class AreaDefinition : ScriptableObject
    {
        public string areaId = string.Empty;
        public string displayName = string.Empty;
        public string sceneName = string.Empty;
        public Color minimapTint = Color.cyan;
        public string entryRoomId = string.Empty;
        public List<RoomDefinition> rooms = new();
    }

    [CreateAssetMenu(menuName = "Lumenfall/World/Room Definition", fileName = "RoomDefinition")]
    public sealed class RoomDefinition : ScriptableObject
    {
        public string roomId = string.Empty;
        public string displayName = string.Empty;
        public AreaDefinition area;
        public Vector2Int mapPosition;
        public bool showOnMap = true;
        public string defaultSpawnPointId = string.Empty;
        public List<string> connectedRoomIds = new();
        public List<GateRequirement> gateRequirements = new();
    }

    [Serializable]
    public sealed class MapRoomData
    {
        public string roomId = string.Empty;
        public Vector2Int mapPosition;
        public List<string> connections = new();
        public bool visited;
    }
}
