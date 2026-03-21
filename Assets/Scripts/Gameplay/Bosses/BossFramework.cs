using System.Collections;
using Lumenfall.Data;
using Lumenfall.Gameplay.Combat;
using Lumenfall.Gameplay.Enemies;
using Lumenfall.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Lumenfall.Gameplay.Bosses
{
    public sealed class PhaseManager : MonoBehaviour
    {
        [SerializeField] private BossDefinition bossDefinition;

        public int CurrentPhaseIndex { get; private set; }

        public AttackPatternSet GetPatternForHealth(float normalizedHealth)
        {
            if (bossDefinition == null || bossDefinition.phases.Count == 0)
            {
                return null;
            }

            int desiredPhase = Mathf.Clamp(Mathf.FloorToInt((1f - normalizedHealth) * bossDefinition.phases.Count), 0, bossDefinition.phases.Count - 1);
            CurrentPhaseIndex = desiredPhase;
            return bossDefinition.phases[CurrentPhaseIndex];
        }
    }

    public sealed class BossHealth : DamageReceiver
    {
        public float NormalizedHealth => MaxHealth <= 0 ? 0f : (float)CurrentHealth / MaxHealth;
    }

    public sealed class BossDeathSequence : MonoBehaviour
    {
        [SerializeField] private UnityEvent onBossDefeated;

        public void Play()
        {
            onBossDefeated?.Invoke();
        }
    }

    public sealed class BossController : EnemyController
    {
        [SerializeField] private BossDefinition bossDefinition;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private PhaseManager phaseManager;
        [SerializeField] private BossDeathSequence deathSequence;

        protected override void Awake()
        {
            if (bossHealth == null)
            {
                bossHealth = GetComponent<BossHealth>();
            }

            if (bossHealth == null)
            {
                bossHealth = gameObject.AddComponent<BossHealth>();
            }

            health = bossHealth;
            if (bossDefinition != null)
            {
                bossHealth.SetMaxHealth(bossDefinition.maxHealth);
            }

            base.Awake();
        }

        protected override AttackPatternStep GetCurrentAttackStep()
        {
            if (phaseManager == null || bossHealth == null)
            {
                return base.GetCurrentAttackStep();
            }

            AttackPatternSet pattern = phaseManager.GetPatternForHealth(bossHealth.NormalizedHealth);
            if (pattern != null && pattern.steps.Count > 0)
            {
                return pattern.steps[0];
            }

            return base.GetCurrentAttackStep();
        }

        protected override void HandleDeath(DamagePayload payload)
        {
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            StateMachine.TransitionTo(EnemyState.Dead);
            deathSequence?.Play();
            yield return new WaitForSeconds(0.4f);
            if (ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.RecordBossDefeat(bossDefinition);
            }

            gameObject.SetActive(false);
        }
    }
}
