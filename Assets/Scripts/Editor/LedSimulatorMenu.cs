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
            go.AddComponent<LedTestPatternGenerator>();
            go.AddComponent<LedSimulatorDisplay>();
            Selection.activeGameObject = go;
        }
    }
}
