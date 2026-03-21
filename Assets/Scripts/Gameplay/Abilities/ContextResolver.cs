using System.Collections.Generic;
using Lumenfall.Data;
using Lumenfall.Gameplay.Player;
using Lumenfall.Services;
using UnityEngine;

namespace Lumenfall.Gameplay.Abilities
{
    public sealed class ContextResolver : MonoBehaviour
    {
        [SerializeField] private LayerMask contextMask = ~0;
        [SerializeField] private float scanRadius = 1.5f;
        [SerializeField] private Vector2 scanOffset = new(0.6f, 0.4f);
        [SerializeField] private bool debugDraw;

        public IContextActionTarget CurrentTarget { get; private set; }

        public ContextActionType CurrentActionType => CurrentTarget?.ActionType ?? ContextActionType.None;

        private void Update()
        {
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (!ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                CurrentTarget = null;
                return;
            }

            int facingDirection = 1;
            if (TryGetComponent(out PlayerMotor2D playerMotor))
            {
                facingDirection = playerMotor.FacingDirection;
            }

            Vector2 center = (Vector2)transform.position + new Vector2(scanOffset.x * facingDirection, scanOffset.y);
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, scanRadius, contextMask);
            IContextActionTarget bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (Collider2D overlap in overlaps)
            {
                MonoBehaviour[] candidates = overlap.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour candidate in candidates)
                {
                    if (candidate is not IContextActionTarget target || !target.IsAvailable(gameStateService.ActiveSave))
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(center, target.TargetTransform.position);
                    if (bestTarget == null || target.Priority < bestTarget.Priority || (target.Priority == bestTarget.Priority && distance < bestDistance))
                    {
                        bestTarget = target;
                        bestDistance = distance;
                    }
                }
            }

            CurrentTarget = bestTarget;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDraw)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + (Vector3)scanOffset, scanRadius);
        }
    }
}
