using UnityEditor;
using UnityEngine;
using LedShow.Authoring;
using LedShow.Simulator;
using LedShow.Routing;

namespace LedShow.Editor
{
    public static class LedWallSimulatorBridgeMenu
    {
        [MenuItem("LED Show/Bridge Ski Game To Our Pipeline")]
        public static void CreateBridge()
        {
            // Reuses the ski game's own bootstrap so we don't have to know how
            // to build its scene ourselves - it creates a LEDWallSimulator
            // (+ sprites) if one doesn't already exist.
            LEDWallBootstrap.EnsureWallExists();

            var wallSimulator = Object.FindAnyObjectByType<LEDWallSimulator>();
            if (wallSimulator == null)
            {
                Debug.LogError("Aucun LEDWallSimulator trouve ou cree - verifie que le code du jeu de ski est present dans le projet.");
                return;
            }

            var go = new GameObject("LED Wall Simulator Bridge");
            var bridge = go.AddComponent<LedWallSimulatorBridgeSource>();

            var serializedBridge = new SerializedObject(bridge);
            serializedBridge.FindProperty("wallSimulator").objectReferenceValue = wallSimulator;
            serializedBridge.ApplyModifiedProperties();

            int rewiredCount = 0;

            var display = Object.FindAnyObjectByType<LedSimulatorDisplay>();
            if (display != null)
            {
                var serializedDisplay = new SerializedObject(display);
                serializedDisplay.FindProperty("stateSourceBehaviour").objectReferenceValue = bridge;
                serializedDisplay.ApplyModifiedProperties();
                rewiredCount++;
            }

            var router = Object.FindAnyObjectByType<LedWallArtNetRouter>();
            if (router != null)
            {
                var serializedRouter = new SerializedObject(router);
                serializedRouter.FindProperty("stateSourceBehaviour").objectReferenceValue = bridge;
                serializedRouter.ApplyModifiedProperties();
                rewiredCount++;
            }

            Debug.Log($"Pont cree, branche sur '{wallSimulator.name}'. {rewiredCount} composant(s) " +
                      "(simulateur et/ou routeur) rebranches automatiquement dessus.", bridge);

            Selection.activeGameObject = go;
        }
    }
}
