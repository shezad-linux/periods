using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumenfall.Data
{
    public enum AbilityType
    {
        DashCore = 0,
        WallClimb = 1,
        LumenPulse = 2,
        PhaseShift = 3,
        CrystalGrapple = 4
    }

    public enum ContextActionType
    {
        None = 0,
        Grapple = 1,
        PhaseWall = 2,
        Interact = 3,
        Pulse = 4
    }

    [CreateAssetMenu(menuName = "Lumenfall/Abilities/Ability Definition", fileName = "AbilityDefinition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        public AbilityType abilityType;
        [Min(0f)] public float cooldownSeconds;
        [Min(0)] public int energyCost;
        public string animationTrigger = string.Empty;
        [TextArea] public string description = string.Empty;
    }

    [CreateAssetMenu(menuName = "Lumenfall/Abilities/Ability Unlock Database", fileName = "AbilityUnlockDatabase")]
    public sealed class AbilityUnlockDatabase : ScriptableObject
    {
        public List<AbilityDefinition> abilities = new();

        public AbilityDefinition GetDefinition(AbilityType abilityType)
        {
            return abilities.Find(definition => definition != null && definition.abilityType == abilityType);
        }
    }

    [Serializable]
    public sealed class AbilityRuntimeState
    {
        public AbilityType abilityType;
        public bool isUnlocked;
        public float cooldownRemaining;
        public float runtimeValue;
    }
}
