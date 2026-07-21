using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkiLevel", menuName = "LED/Niveau Timeline")]
public class SkiLevelTimeline : ScriptableObject
{
    public AudioClip music;
    [Tooltip("Début de la musique dans le clip (secondes).")]
    public float musicStartTime;
    [Tooltip("Fin de la musique (secondes). 0 = fin du clip.")]
    public float musicEndTime;
    public float runSpeed = 34f;
    public List<SkiTimelineObstacleEvent> obstacles = new();

    public float MusicStart => music != null ? Mathf.Clamp(musicStartTime, 0f, music.length) : 0f;

    public float MusicEnd
    {
        get
        {
            if (music == null)
                return 30f;

            if (musicEndTime <= 0f)
                return music.length;

            return Mathf.Clamp(musicEndTime, MusicStart + 0.1f, music.length);
        }
    }

    public float MusicDuration => Mathf.Max(0.1f, MusicEnd - MusicStart);

    public float Duration
    {
        get
        {
            var max = music != null ? MusicDuration : 30f;
            foreach (var obstacle in obstacles)
                max = Mathf.Max(max, obstacle.timeSeconds + 2f);

            return Mathf.Max(max, 5f);
        }
    }

    public float ClipTimeToLevelTime(float clipTime)
    {
        return Mathf.Clamp(clipTime - MusicStart, 0f, MusicDuration);
    }

    public float LevelTimeToClipTime(float levelTime)
    {
        return MusicStart + Mathf.Clamp(levelTime, 0f, MusicDuration);
    }

    public void ClampMusicRange()
    {
        if (music == null)
        {
            musicStartTime = 0f;
            musicEndTime = 0f;
            return;
        }

        musicStartTime = Mathf.Clamp(musicStartTime, 0f, music.length - 0.1f);

        if (musicEndTime <= 0f)
            return;

        musicEndTime = Mathf.Clamp(musicEndTime, musicStartTime + 0.1f, music.length);
    }

    public static float TimeToWorldX(float timeSeconds, int playerColumn, float speed)
    {
        return timeSeconds * speed + playerColumn;
    }

    public static float WorldXToTime(float worldX, int playerColumn, float speed)
    {
        return (worldX - playerColumn) / speed;
    }

    public void AddObstacle(
        float timeSeconds,
        SkiObstacleType type = SkiObstacleType.Rock,
        SkiObstacleLane lane = SkiObstacleLane.Ground)
    {
        if (type == SkiObstacleType.Piaf)
            lane = SkiObstacleLane.Air;

        obstacles.Add(new SkiTimelineObstacleEvent
        {
            timeSeconds = Mathf.Max(0f, timeSeconds),
            type = type,
            lane = lane
        });
        SortObstacles();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= obstacles.Count)
            return;

        obstacles.RemoveAt(index);
    }

    public void SortObstacles()
    {
        obstacles.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
    }

    public IReadOnlyList<SkiTimelineObstacleEvent> GetSortedEvents()
    {
        SortObstacles();
        return obstacles;
    }
}
