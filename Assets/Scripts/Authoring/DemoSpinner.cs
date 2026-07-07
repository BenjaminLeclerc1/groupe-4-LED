using UnityEngine;

namespace LedShow.Authoring
{
    // Minimal proof-of-life for the render-to-texture demo scene: if this object
    // visibly spins in the LED simulator, the capture pipeline is truly live,
    // not just displaying a cached frame.
    [ExecuteAlways]
    public class DemoSpinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
