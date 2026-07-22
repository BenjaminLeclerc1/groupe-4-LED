using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LedShow.Authoring;
using LedShow.Routing;
using LedShow.LED;

namespace LedShow.Editor
{
    public static class LedWallConnectMenu
    {
        [MenuItem("LED/Connect Ski Game To Real Wall")]
        public static void ConnectSkiGameToRealWall()
        {
            WireShowPipeline(markSceneDirty: true, selectRouter: true);
        }

        /// <summary>
        /// Crée / branche Bridge + Router + SkiShowLighting dans la scène active.
        /// </summary>
        public static void WireShowPipeline(bool markSceneDirty, bool selectRouter)
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

            bridge.SetWallSimulator(wallSimulator);
            EditorUtility.SetDirty(bridge);

            var router = Object.FindAnyObjectByType<LedWallArtNetRouter>();
            if (router == null)
            {
                var routerGo = new GameObject("LED Wall Router");
                router = routerGo.AddComponent<LedWallArtNetRouter>();
                Undo.RegisterCreatedObjectUndo(routerGo, "Create LED Wall Router");
            }

            router.SetStateSource(bridge);
            EditorUtility.SetDirty(router);

            var lighting = Object.FindAnyObjectByType<SkiShowLighting>();
            if (lighting == null)
            {
                var lightingGo = new GameObject("Ski Show Lighting");
                lighting = lightingGo.AddComponent<SkiShowLighting>();
                Undo.RegisterCreatedObjectUndo(lightingGo, "Create Ski Show Lighting");
            }

            EditorUtility.SetDirty(lighting);

            if (markSceneDirty && !Application.isPlaying)
            {
                var scene = wallSimulator.gameObject.scene;
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            if (selectRouter)
                Selection.activeGameObject = router.gameObject;

            Debug.Log(
                "Pipeline Art-Net branche : Ski → Bridge → LED Wall Router → " +
                string.Join(", ", LedWallLayout.ControllerIps) +
                " + Ski Show Lighting (u33). Lance Play. PC sur 192.168.1.x.",
                router);
        }
    }
}
