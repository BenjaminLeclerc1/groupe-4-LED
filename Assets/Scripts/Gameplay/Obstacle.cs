using System.Collections.Generic;
using UnityEngine;

namespace LedShow.Gameplay
{
    // Marker + AABB source for the simplified collision check in
    // ObstacleCollisionDetector. Registers itself while enabled so the detector
    // never has to FindObjectsOfType or test against a destroyed/pooled obstacle.
    [RequireComponent(typeof(Collider))]
    public class Obstacle : MonoBehaviour
    {
        public static readonly List<Obstacle> Active = new List<Obstacle>();

        private Collider obstacleCollider;

        public Bounds WorldBounds => obstacleCollider.bounds;

        private void Awake()
        {
            obstacleCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }
    }
}
