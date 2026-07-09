using System;
using UnityEngine;

namespace LedShow.Gameplay
{
    // Manual AABB overlap test rather than OnCollisionEnter/OnTriggerEnter: the
    // skier has no Rigidbody, so there is no PhysX collision resolution to hook
    // into, only a bounds intersection against the registered obstacles.
    [RequireComponent(typeof(Collider))]
    public class ObstacleCollisionDetector : MonoBehaviour
    {
        public event Action OnHitObstacle;

        private Collider playerCollider;

        private void Awake()
        {
            playerCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            // Only a live run can end in death: this also stops the same
            // overlapping obstacle from re-firing every frame during the Dead delay.
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            Bounds playerBounds = playerCollider.bounds;

            foreach (Obstacle obstacle in Obstacle.Active)
            {
                if (!playerBounds.Intersects(obstacle.WorldBounds))
                {
                    continue;
                }

                OnHitObstacle?.Invoke();
                GameManager.Instance?.NotifyCollision();
                return;
            }
        }
    }
}
