using System;
using UnityEngine;

namespace LedShow.LED
{
    [Serializable]
    public struct SkiTimelineObstacleEvent
    {
        public float timeSeconds;
        public SkiObstacleType type;
        public SkiObstacleLane lane;
    }
}
