using Lumenfall.Data;
using Lumenfall.Gameplay.Abilities;
using Lumenfall.Gameplay.Combat;
using Lumenfall.Services;
using UnityEngine;

namespace Lumenfall.Gameplay.Player
{
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour, IKnockbackReceiver
    {
        [Header("Movement")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float maxMoveSpeed = 7f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float groundDeceleration = 55f;
        [SerializeField] private float airAcceleration = 28f;
        [SerializeField] private float gravity = 45f;
        [SerializeField] private float jumpVelocity = 14f;
        [SerializeField] private float terminalVelocity = 25f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float skinWidth = 0.02f;
        [SerializeField] private float maxSlopeAngle = 50f;
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.12f;

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];
        private ContactFilter2D _contactFilter;
        private CapsuleCollider2D _capsuleCollider;
        private InputService _inputService;
        private GameStateService _gameStateService;
        private AbilityController _abilityController;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _dashTimer;
        private float _knockbackTimer;
        private Vector2 _knockbackVelocity;

        public Vector2 Velocity { get; private set; }

        public bool IsGrounded { get; private set; }

        public int FacingDirection { get; private set; } = 1;

        public bool IsDashing => _dashTimer > 0f;

        private void Reset()
        {
            CapsuleCollider2D colliderComponent = GetComponent<CapsuleCollider2D>();
            colliderComponent.direction = CapsuleDirection2D.Vertical;
            colliderComponent.size = new Vector2(0.8f, 1.8f);
        }

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            _abilityController = GetComponent<AbilityController>();
            _contactFilter.useLayerMask = true;
            _contactFilter.layerMask = collisionMask;
            _contactFilter.useTriggers = false;
        }

        private void Update()
        {
            if (_inputService == null)
            {
                ServiceRegistry.TryGet(out _inputService);
            }

            if (_gameStateService == null)
            {
                ServiceRegistry.TryGet(out _gameStateService);
            }

            if (_inputService == null || (_gameStateService != null && _gameStateService.SessionState.isPaused))
            {
                return;
            }

            Simulate(Time.deltaTime, _inputService.Gameplay);
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            Velocity = Vector2.zero;
            _dashTimer = 0f;
            _knockbackTimer = 0f;
        }

        public void ApplyKnockback(Vector2 force, float duration)
        {
            _knockbackVelocity = force;
            _knockbackTimer = duration;
        }

        private void Simulate(float deltaTime, GameplayInputSnapshot input)
        {
            ProbeGround();

            if (IsGrounded)
            {
                _coyoteTimer = coyoteTime;
                if (Velocity.y < 0f)
                {
                    Velocity = new Vector2(Velocity.x, -2f);
                }
            }
            else
            {
                _coyoteTimer -= deltaTime;
            }

            _jumpBufferTimer = input.JumpPressed ? jumpBufferTime : _jumpBufferTimer - deltaTime;

            if (Mathf.Abs(input.Move.x) > 0.01f)
            {
                FacingDirection = input.Move.x > 0f ? 1 : -1;
            }

            float targetSpeed = input.Move.x * maxMoveSpeed;
            float acceleration = IsGrounded ? (Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration) : airAcceleration;

            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= deltaTime;
                Velocity = _knockbackVelocity;
            }
            else if (_dashTimer > 0f)
            {
                _dashTimer -= deltaTime;
                Velocity = new Vector2(FacingDirection * dashSpeed, 0f);
            }
            else
            {
                Velocity = new Vector2(Mathf.MoveTowards(Velocity.x, targetSpeed, acceleration * deltaTime), Velocity.y);

                if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
                {
                    Velocity = new Vector2(Velocity.x, jumpVelocity);
                    _jumpBufferTimer = 0f;
                    _coyoteTimer = 0f;
                    IsGrounded = false;
                }

                if (!IsGrounded)
                {
                    Velocity = new Vector2(Velocity.x, Mathf.Max(Velocity.y - gravity * deltaTime, -terminalVelocity));
                }

                if (input.DashPressed && _abilityController != null)
                {
                    _abilityController.TryDash();
                }
            }

            MoveCharacter(Velocity * deltaTime);
            ProbeGround();
        }

        public bool BeginDash()
        {
            _dashTimer = dashDuration;
            Velocity = new Vector2(FacingDirection * dashSpeed, 0f);
            return true;
        }

        public bool CanWallClimb()
        {
            if (_abilityController == null || !_abilityController.IsUnlocked(AbilityType.WallClimb))
            {
                return false;
            }

            Vector2 direction = new(FacingDirection, 0f);
            int hitCount = _capsuleCollider.Cast(direction, _contactFilter, _castHits, 0.15f);
            for (int index = 0; index < hitCount; index++)
            {
                if (_castHits[index].normal.y < 0.3f)
                {
                    return true;
                }
            }

            return false;
        }

        private void MoveCharacter(Vector2 movement)
        {
            Vector2 remaining = movement;
            Vector3 position = transform.position;

            for (int iteration = 0; iteration < 3 && remaining.sqrMagnitude > 0.0001f; iteration++)
            {
                Vector2 direction = remaining.normalized;
                float distance = remaining.magnitude;
                int hitCount = _capsuleCollider.Cast(direction, _contactFilter, _castHits, distance + skinWidth);
                if (hitCount == 0)
                {
                    position += (Vector3)remaining;
                    break;
                }

                RaycastHit2D nearestHit = _castHits[0];
                for (int index = 1; index < hitCount; index++)
                {
                    if (_castHits[index].distance < nearestHit.distance)
                    {
                        nearestHit = _castHits[index];
                    }
                }

                float travelDistance = Mathf.Max(0f, nearestHit.distance - skinWidth);
                position += (Vector3)(direction * travelDistance);
                remaining -= direction * travelDistance;

                float slopeAngle = Vector2.Angle(nearestHit.normal, Vector2.up);
                bool walkableSlope = slopeAngle <= maxSlopeAngle && nearestHit.normal.y > 0.01f;
                if (walkableSlope && Mathf.Abs(remaining.x) > 0.01f && remaining.y <= 0f)
                {
                    Vector2 tangent = new(nearestHit.normal.y, -nearestHit.normal.x);
                    if (Mathf.Sign(tangent.x) != Mathf.Sign(remaining.x))
                    {
                        tangent = -tangent;
                    }

                    remaining = tangent.normalized * remaining.magnitude;
                    Vector2 projectedVelocity = ProjectOntoTangent(Velocity, nearestHit.normal);
                    Velocity = new Vector2(projectedVelocity.x, Mathf.Max(projectedVelocity.y, Velocity.y));
                    continue;
                }

                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    Velocity = new Vector2(0f, Velocity.y);
                }
                else
                {
                    Velocity = new Vector2(Velocity.x, Mathf.Min(0f, Velocity.y));
                }

                remaining = Vector2.zero;
            }

            transform.position = position;
        }

        private void ProbeGround()
        {
            int hitCount = _capsuleCollider.Cast(Vector2.down, _contactFilter, _castHits, 0.08f);
            IsGrounded = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _castHits[index];
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (hit.normal.y > 0.2f && slopeAngle <= maxSlopeAngle)
                {
                    IsGrounded = true;
                    if (Velocity.y < 0f)
                    {
                        Velocity = new Vector2(Velocity.x, 0f);
                    }

                    break;
                }
            }
        }

        private static Vector2 ProjectOntoTangent(Vector2 vector, Vector2 normal)
        {
            Vector2 tangent = new(normal.y, -normal.x).normalized;
            return tangent * Vector2.Dot(vector, tangent);
        }
    }
}
