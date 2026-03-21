using UnityEngine;

namespace Lumenfall.Gameplay.Player
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothing = 8f;
        [SerializeField] private Vector3 offset = new(0f, 1f, -10f);

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, target.position + offset, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}
