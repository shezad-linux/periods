using System.Collections;
using Lumenfall.Gameplay.Combat;
using Lumenfall.Services;
using UnityEngine;

namespace Lumenfall.Gameplay.Player
{
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackActiveWindow = 0.12f;
        [SerializeField] private float attackRecoverySeconds = 0.2f;
        [SerializeField] private Vector2 attackKnockback = new(8f, 2f);
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private Hitbox hitbox;

        private InputService _inputService;
        private PlayerMotor2D _motor;
        private bool _isRecovering;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor2D>();
            if (hitbox == null)
            {
                GameObject hitboxObject = new("PlayerHitbox");
                hitboxObject.transform.SetParent(transform, false);
                hitbox = hitboxObject.AddComponent<Hitbox>();
                hitboxObject.layer = gameObject.layer;
            }
        }

        private void Update()
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            if (_inputService == null || _isRecovering)
            {
                return;
            }

            if (_inputService.Gameplay.AttackPressed)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isRecovering = true;
            DamagePayload payload = new(attackDamage, attackKnockback, gameObject, transform.position);
            hitbox.Fire(payload, attackActiveWindow, _motor != null ? _motor.FacingDirection : 1);
            yield return new WaitForSeconds(attackRecoverySeconds);
            _isRecovering = false;
        }
    }
}
