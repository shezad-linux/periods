using System;
using System.Collections.Generic;
using Lumenfall.Services;
using UnityEngine;

namespace Lumenfall.Gameplay.Combat
{
    public readonly struct DamagePayload
    {
        public readonly int Damage;
        public readonly Vector2 Knockback;
        public readonly GameObject Source;
        public readonly Vector2 HitPoint;

        public DamagePayload(int damage, Vector2 knockback, GameObject source, Vector2 hitPoint)
        {
            Damage = damage;
            Knockback = knockback;
            Source = source;
            HitPoint = hitPoint;
        }
    }

    public interface IKnockbackReceiver
    {
        void ApplyKnockback(Vector2 force, float duration);
    }

    public class DamageReceiver : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilitySeconds = 0.1f;

        private float _invulnerabilityTimer;

        public event Action<int, int> HealthChanged;

        public event Action<DamagePayload> Died;

        public int CurrentHealth { get; private set; }

        public int MaxHealth => maxHealth;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        protected virtual void Update()
        {
            if (_invulnerabilityTimer > 0f)
            {
                _invulnerabilityTimer -= Time.deltaTime;
            }
        }

        public void SetMaxHealth(int health)
        {
            maxHealth = Mathf.Max(1, health);
            CurrentHealth = Mathf.Min(CurrentHealth <= 0 ? maxHealth : CurrentHealth, maxHealth);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public virtual bool CanReceiveDamage()
        {
            return enabled && gameObject.activeInHierarchy && _invulnerabilityTimer <= 0f;
        }

        public virtual void ReceiveDamage(DamagePayload payload)
        {
            if (!CanReceiveDamage())
            {
                return;
            }

            _invulnerabilityTimer = invulnerabilitySeconds;
            CurrentHealth = Mathf.Max(0, CurrentHealth - payload.Damage);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (TryGetComponent(out IKnockbackReceiver knockbackReceiver))
            {
                knockbackReceiver.ApplyKnockback(payload.Knockback, 0.12f);
            }

            if (CurrentHealth <= 0)
            {
                HandleDeath(payload);
            }
        }

        public virtual void RestoreToFull()
        {
            CurrentHealth = maxHealth;
            _invulnerabilityTimer = 0f;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        protected virtual void HandleDeath(DamagePayload payload)
        {
            Died?.Invoke(payload);
        }
    }

    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private DamageReceiver receiver;

        private void Awake()
        {
            if (receiver == null)
            {
                receiver = GetComponentInParent<DamageReceiver>();
            }
        }

        public void ReceiveDamage(DamagePayload payload)
        {
            receiver?.ReceiveDamage(payload);
        }
    }

    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(1.5f, 1f);
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private Vector2 localOffset = new(0.75f, 0f);
        [SerializeField] private bool debugDraw;

        private readonly HashSet<Hurtbox> _alreadyHit = new();
        private DamagePayload _payload;
        private float _activeUntilTime;
        private bool _isActive;
        private int _facingDirection = 1;

        public void Fire(DamagePayload payload, float activeDuration, int facingDirection)
        {
            _payload = payload;
            _facingDirection = facingDirection >= 0 ? 1 : -1;
            _alreadyHit.Clear();
            _activeUntilTime = Time.time + Mathf.Max(0.01f, activeDuration);
            _isActive = true;
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            if (Time.time >= _activeUntilTime)
            {
                _isActive = false;
                return;
            }

            Vector2 center = (Vector2)transform.position + new Vector2(localOffset.x * _facingDirection, localOffset.y);
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(center, size, 0f, hitMask);
            foreach (Collider2D overlap in overlaps)
            {
                Hurtbox hurtbox = overlap.GetComponent<Hurtbox>() ?? overlap.GetComponentInParent<Hurtbox>();
                if (hurtbox == null || _alreadyHit.Contains(hurtbox))
                {
                    continue;
                }

                if (_payload.Source != null && hurtbox.transform.root == _payload.Source.transform.root)
                {
                    continue;
                }

                _alreadyHit.Add(hurtbox);
                hurtbox.ReceiveDamage(new DamagePayload(_payload.Damage, new Vector2(_payload.Knockback.x * _facingDirection, _payload.Knockback.y), _payload.Source, center));
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw)
            {
                return;
            }

            Gizmos.color = Color.red;
            Vector2 center = (Vector2)transform.position + new Vector2(localOffset.x * (_facingDirection >= 0 ? 1 : -1), localOffset.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
