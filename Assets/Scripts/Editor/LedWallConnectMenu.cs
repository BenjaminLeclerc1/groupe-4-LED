using UnityEditor;
using UnityEngine;
using LedShow.Authoring;
using LedShow.Routing;

namespace LedShow.Editor
{
    public static class LedWallConnectMenu
    {
        [MenuItem("LED Show/Connect Ski Game To Real Wall")]
        public static void ConnectSkiGameToRealWall()
        {
            LEDWallBootstrap.EnsureWallExists();

            var wallSimulator = Object.FindAnyObjectByType<LEDWallSimulator>();
            if (wallSimulator == null)
            {
                Debug.LogError("Aucun LEDWallSimulator trouve. Ouvre SampleScene avec le jeu de ski.");
                return;
            }

            var bridge = Object.FindAnyObjectByType<LedWallSimulatorBridgeSource>();
            if (bridge == null)
            {
                var bridgeGo = new GameObject("LED Wall Simulator Bridge");
                bridge = bridgeGo.AddComponent<LedWallSimulatorBridgeSource>();
                Undo.RegisterCreatedObjectUndo(bridgeGo, "Create LED Wall Bridge");
            }

            var serializedBridge = new SerializedObject(bridge);
            serializedBridge.FindProperty("wallSimulator").objectReferenceValue = wallSimulator;
            serializedBridge.ApplyModifiedProperties();

            var router = Object.FindAnyObjectByType<LedWallArtNetRouter>();
            if (router == null)
            {
                var routerGo = new GameObject("LED Wall Router");
                router = routerGo.AddComponent<LedWallArtNetRouter>();
                Undo.RegisterCreatedObjectUndo(routerGo, "Create LED Wall Router");
            }

            router.SetStateSource(bridge);
            EditorUtility.SetDirty(router);
            EditorUtility.SetDirty(bridge);

            Selection.activeGameObject = router.gameObject;
            Debug.Log(
                "Pipeline Art-Net branche : Ski → Bridge → LED Wall Router → " +
                string.Join(", ", LedWallLayout.ControllerIps) +
                ". Lance Play. Ton PC doit etre sur le reseau 192.168.1.x.",
                router);
        }
    }
}
