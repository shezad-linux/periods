using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumenfall.Data
{
    [Serializable]
    public sealed class AttackPatternStep
    {
        public string animationTrigger = string.Empty;
        [Min(0f)] public float windUpSeconds = 0.15f;
        [Min(0f)] public float activeSeconds = 0.1f;
        [Min(0f)] public float recoverySeconds = 0.2f;
        public int damage = 1;
        public Vector2 knockback = new(6f, 3f);
    }

    [CreateAssetMenu(menuName = "Lumenfall/Combat/Attack Pattern Set", fileName = "AttackPatternSet")]
    public sealed class AttackPatternSet : ScriptableObject
    {
        public string patternId = string.Empty;
        public bool loop;
        public List<AttackPatternStep> steps = new();
    }

    [CreateAssetMenu(menuName = "Lumenfall/Combat/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string enemyId = string.Empty;
        public string displayName = string.Empty;
        public int maxHealth = 3;
        public float moveSpeed = 2f;
        public float chaseRadius = 5f;
        public bool respawnOnRoomReset = true;
        public AttackPatternSet defaultAttackPattern;
    }

    [CreateAssetMenu(menuName = "Lumenfall/Combat/Boss Definition", fileName = "BossDefinition")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string bossId = string.Empty;
        public string displayName = string.Empty;
        public int maxHealth = 20;
        public AbilityType rewardAbility = AbilityType.DashCore;
        public string rewardFlag = string.Empty;
        public int corruptionReward = 5;
        public List<AttackPatternSet> phases = new();
    }
}
