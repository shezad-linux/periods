using System;
using UnityEngine;

namespace Lumenfall.Data
{
    public enum LoreEntryType
    {
        Mural = 0,
        MemoryCrystal = 1,
        GhostEcho = 2,
        BossMemory = 3
    }

    public enum EndingType
    {
        EternalSeal = 0,
        Symbiosis = 1,
        Release = 2
    }

    [CreateAssetMenu(menuName = "Lumenfall/Lore/Lore Entry", fileName = "LoreEntryDefinition")]
    public sealed class LoreEntryDefinition : ScriptableObject
    {
        public string loreId = string.Empty;
        public string title = string.Empty;
        [TextArea(4, 12)] public string body = string.Empty;
        public LoreEntryType entryType;
    }

    [CreateAssetMenu(menuName = "Lumenfall/Lore/Ending Rule", fileName = "EndingRuleDefinition")]
    public sealed class EndingRuleDefinition : ScriptableObject
    {
        public EndingType endingType;
        public int minCorruption;
        public int maxCorruption = 100;
        public string requiredFlag = string.Empty;
        public bool requireReleaseChoice;

        public bool Matches(SaveGameData saveData, bool releaseChoiceSelected)
        {
            if (saveData == null)
            {
                return false;
            }

            if (saveData.corruptionScore < minCorruption || saveData.corruptionScore > maxCorruption)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredFlag) && !saveData.worldFlags.Contains(requiredFlag))
            {
                return false;
            }

            return !requireReleaseChoice || releaseChoiceSelected;
        }
    }
}
