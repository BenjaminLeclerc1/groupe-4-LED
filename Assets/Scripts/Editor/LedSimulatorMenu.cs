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
            var patternGenerator = go.AddComponent<LedTestPatternGenerator>();
            var display = go.AddComponent<LedSimulatorDisplay>();

            var serializedDisplay = new SerializedObject(display);
            serializedDisplay.FindProperty("stateSourceBehaviour").objectReferenceValue = patternGenerator;
            serializedDisplay.ApplyModifiedProperties();

            Selection.activeGameObject = go;
        }
    }
}
