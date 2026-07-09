using UnityEditor;
using UnityEngine;
using LedShow.Simulator;

namespace LedShow.Editor
{
    public static class LedSimulatorMenu
    {
        [MenuItem("LED Show/Create LED Simulator")]
        public static void CreateSimulator()
        {
            var go = new GameObject("LED Simulator");
            // Offset from the origin so the preview quad doesn't overlap whatever
            // demo content (e.g. LED Render-To-Texture Demo) also sits at (0,0,0).
            go.transform.position = new Vector3(0f, 3f, 0f);
            var patternGenerator = go.AddComponent<LedTestPatternGenerator>();
            var display = go.AddComponent<LedSimulatorDisplay>();

            var serializedDisplay = new SerializedObject(display);
            serializedDisplay.FindProperty("stateSourceBehaviour").objectReferenceValue = patternGenerator;
            serializedDisplay.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }
    }
}
