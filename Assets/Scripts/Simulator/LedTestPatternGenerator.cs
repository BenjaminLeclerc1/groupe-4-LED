using UnityEngine;
using LedShow.Core;

namespace LedShow.Simulator
{
    public enum TestPattern
    {
        SolidColor,
        Checkerboard,
        HorizontalGradient,
        ChaseX,
    }

    // Fakes a "state" so the simulator and (later) the ArtNet sender have something
    // to display/send before the real render-to-texture pipeline (INFRA-03) exists.
    [ExecuteAlways]
    public class LedTestPatternGenerator : MonoBehaviour, ILedStateSource
    {
        // 128x128 matches the real GroupeLaps LED wall (see LedWallLayout).
        [SerializeField] private int width = 128;
        [SerializeField] private int height = 128;
        [SerializeField] private TestPattern pattern = TestPattern.Checkerboard;
        [SerializeField] private Color solidColor = Color.red;
        [SerializeField] private float chaseSpeed = 2f;

        public LedState State { get; private set; }

        private void OnEnable()
        {
            State = new LedState(width, height);
        }

        private void Update()
        {
            if (State == null || State.Width != width || State.Height != height)
            {
                State = new LedState(width, height);
            }

            switch (pattern)
            {
                case TestPattern.SolidColor:
                    State.Fill(solidColor);
                    break;

                case TestPattern.Checkerboard:
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        State.Set(x, y, (x + y) % 2 == 0 ? Color.white : Color.black);
                    break;

                case TestPattern.HorizontalGradient:
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        State.Set(x, y, Color.Lerp(Color.black, Color.white, x / (float)Mathf.Max(1, width - 1)));
                    break;

                case TestPattern.ChaseX:
                    int lit = Mathf.FloorToInt(Time.realtimeSinceStartup * chaseSpeed) % width;
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        State.Set(x, y, x == lit ? Color.cyan : Color.black);
                    break;
            }
        }
    }
}
