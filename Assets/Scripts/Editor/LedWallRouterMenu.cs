using UnityEditor;
using UnityEngine;
using LedShow.Routing;
using LedShow.Authoring;
using LedShow.LED;

namespace LedShow.Editor
{
    public static class LedWallRouterMenu
    {
        [MenuItem("LED/Create LED Wall Router (real wall)")]
        public static void CreateRouter()
        {
            var go = new GameObject("LED Wall Router");
            var router = go.AddComponent<LedWallArtNetRouter>();

            var existingSource = Object.FindAnyObjectByType<LedWallSimulatorBridgeSource>();
            if (existingSource != null)
            {
                var serializedRouter = new SerializedObject(router);
                serializedRouter.FindProperty("stateSourceBehaviour").objectReferenceValue = existingSource;
                serializedRouter.ApplyModifiedProperties();
                Debug.Log($"LED Wall Router branche sur '{existingSource.name}'. Il envoie vers les 4 IP : " +
                          string.Join(", ", LedWallLayout.ControllerIps), router);
            }
            else
            {
                Debug.Log("Aucune source de state trouvee dans la scene : glisse-en une dans le champ " +
                          "'State Source Behaviour' du routeur (ex. LedWallSimulatorBridgeSource).", router);
            }

            Selection.activeGameObject = go;
        }

        // ArtNet controllers latch the last frame they received - there is no
        // "off" in the protocol. Stopping Play (or closing Unity) leaves
        // whatever was last displayed lit up on the real wall. This sends one
        // explicit all-black frame to every universe of every controller so
        // the shared wall is left clean for the next group.
        [MenuItem("LED/Send Blackout To Wall")]
        public static void SendBlackout()
        {
            byte[] blackUniverse = new byte[ArtNetPacket.MaxDmxLength]; // zero-initialized

            for (int c = 0; c < LedWallLayout.ControllerIps.Length; c++)
            {
                using (var sender = new ArtNetSender(LedWallLayout.ControllerIps[c]))
                {
                    for (int u = 0; u < LedWallLayout.UniversesPerController; u++)
                    {
                        sender.SendUniverse((ushort)u, blackUniverse, ArtNetPacket.MaxDmxLength);
                    }
                }
            }

            // Lyres + projecteur (univers 33 sur 192.168.1.48).
            SkiShowLighting.SendBlackout();

            Debug.Log("Blackout envoye : 128 univers mur (0-31 x4) + univers 33 (lyres/statique).");
        }
    }
}
