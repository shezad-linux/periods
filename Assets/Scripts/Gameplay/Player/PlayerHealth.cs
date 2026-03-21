using Lumenfall.Gameplay.Combat;
using Lumenfall.Services;
using UnityEngine;
using Lumenfall.Gameplay.Abilities;

namespace Lumenfall.Gameplay.Player
{
    public sealed class PlayerHealth : DamageReceiver
    {
        [SerializeField] private float respawnDelaySeconds = 0.4f;

        private PlayerMotor2D _motor;
        private PlayerCombatController _combatController;
        private AbilityController _abilityController;
        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private float _respawnAtTime = -1f;

        protected override void Awake()
        {
            base.Awake();
            _motor = GetComponent<PlayerMotor2D>();
            _combatController = GetComponent<PlayerCombatController>();
            _abilityController = GetComponent<AbilityController>();
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        protected override void Update()
        {
            base.Update();
            if (_respawnAtTime > 0f && Time.time >= _respawnAtTime)
            {
                _respawnAtTime = -1f;
                Respawn();
            }
        }

        protected override void HandleDeath(DamagePayload payload)
        {
            _respawnAtTime = Time.time + respawnDelaySeconds;
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            if (_motor != null)
            {
                _motor.enabled = false;
            }

            if (_combatController != null)
            {
                _combatController.enabled = false;
            }

            if (_abilityController != null)
            {
                _abilityController.enabled = false;
            }
        }

        private void Respawn()
        {
            if (!ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameObject.SetActive(true);
                RestoreToFull();
                return;
            }

            transform.position = gameStateService.ActiveSave.playerPosition;
            _motor?.TeleportTo(transform.position);
            RestoreToFull();
            gameStateService.ActiveSave.currentHealth = MaxHealth;
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_renderer != null)
            {
                _renderer.enabled = true;
            }

            if (_motor != null)
            {
                _motor.enabled = true;
            }

            if (_combatController != null)
            {
                _combatController.enabled = true;
            }

            if (_abilityController != null)
            {
                _abilityController.enabled = true;
            }
        }
    }
}
