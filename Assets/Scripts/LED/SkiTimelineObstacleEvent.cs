using System;
using UnityEngine;

[Serializable]
public struct SkiTimelineObstacleEvent
{
    public float timeSeconds;
    public SkiObstacleType type;
    public SkiObstacleLane lane;
}
