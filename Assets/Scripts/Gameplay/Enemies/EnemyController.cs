using System.Collections;
using Lumenfall.Data;
using Lumenfall.Gameplay.Combat;
using Lumenfall.Gameplay.Player;
using UnityEngine;

namespace Lumenfall.Gameplay.Enemies
{
    public enum EnemyState
    {
        Idle = 0,
        Chase = 1,
        Attack = 2,
        Dead = 3
    }

    public sealed class EnemyStateMachine
    {
        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

        public void TransitionTo(EnemyState nextState)
        {
            CurrentState = nextState;
        }
    }

    public class EnemyController : MonoBehaviour
    {
        [SerializeField] protected EnemyDefinition definition;
        [SerializeField] protected float attackRange = 1.4f;
        [SerializeField] protected float attackCooldown = 1.2f;
        [SerializeField] protected Hitbox attackHitbox;
        [SerializeField] protected DamageReceiver health;

        protected readonly EnemyStateMachine StateMachine = new();
        protected PlayerMotor2D Player;

        private float _attackCooldownTimer;
        private bool _isAttacking;

        protected virtual void Awake()
        {
            if (health == null)
            {
                health = GetComponent<DamageReceiver>();
            }

            if (health == null)
            {
                health = gameObject.AddComponent<DamageReceiver>();
            }

            if (definition != null)
            {
                health.SetMaxHealth(definition.maxHealth);
            }

            health.Died += HandleDeath;
        }

        protected virtual void Update()
        {
            if (StateMachine.CurrentState == EnemyState.Dead)
            {
                return;
            }

            if (Player == null)
            {
                Player = Object.FindFirstObjectByType<PlayerMotor2D>();
                if (Player == null)
                {
                    return;
                }
            }

            _attackCooldownTimer -= Time.deltaTime;
            float distance = Vector2.Distance(transform.position, Player.transform.position);
            if (_isAttacking)
            {
                return;
            }

            if (distance <= attackRange)
            {
                StateMachine.TransitionTo(EnemyState.Attack);
                if (_attackCooldownTimer <= 0f)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
            else if (definition != null && distance <= definition.chaseRadius)
            {
                StateMachine.TransitionTo(EnemyState.Chase);
                Vector3 direction = (Player.transform.position - transform.position).normalized;
                transform.position += direction * (definition.moveSpeed * Time.deltaTime);
            }
            else
            {
                StateMachine.TransitionTo(EnemyState.Idle);
            }
        }

        protected virtual IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _attackCooldownTimer = attackCooldown;
            AttackPatternStep attackStep = GetCurrentAttackStep();
            if (attackHitbox != null)
            {
                yield return new WaitForSeconds(attackStep.windUpSeconds);
                int facingDirection = Player != null && Player.transform.position.x < transform.position.x ? -1 : 1;
                attackHitbox.Fire(new DamagePayload(attackStep.damage, attackStep.knockback, gameObject, transform.position), attackStep.activeSeconds, facingDirection);
                yield return new WaitForSeconds(attackStep.recoverySeconds);
            }
            else
            {
                yield return new WaitForSeconds(attackCooldown);
            }

            _isAttacking = false;
        }

        protected virtual AttackPatternStep GetCurrentAttackStep()
        {
            if (definition != null && definition.defaultAttackPattern != null && definition.defaultAttackPattern.steps.Count > 0)
            {
                return definition.defaultAttackPattern.steps[0];
            }

            return new AttackPatternStep();
        }

        protected virtual void HandleDeath(DamagePayload payload)
        {
            StateMachine.TransitionTo(EnemyState.Dead);
            gameObject.SetActive(false);
        }
    }
}
