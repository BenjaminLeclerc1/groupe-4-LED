using UnityEditor;
using UnityEngine;
using LedShow.Routing;
using LedShow.Simulator;
using LedShow.Authoring;

namespace LedShow.Editor
{
    public static class ArtNetRouterMenu
    {
        [MenuItem("LED Show/Create ArtNet Router")]
        public static void CreateRouter()
        {
            var go = new GameObject("LED ArtNet Router");
            var router = go.AddComponent<LedStateArtNetRouter>();

            MonoBehaviour existingSource = Object.FindAnyObjectByType<LedRenderToTextureSource>();
            if (existingSource == null)
            {
                existingSource = Object.FindAnyObjectByType<LedTestPatternGenerator>();
            }

            if (existingSource != null)
            {
                var serializedRouter = new SerializedObject(router);
                serializedRouter.FindProperty("stateSourceBehaviour").objectReferenceValue = existingSource;
                serializedRouter.ApplyModifiedProperties();
                Debug.Log($"LED ArtNet Router branche sur '{existingSource.name}'.", router);
            }
            else
            {
                Debug.Log("Aucune source de state trouvee dans la scene : glisse-en une (LedTestPatternGenerator " +
                          "ou LedRenderToTextureSource) dans le champ 'State Source Behaviour' du routeur.", router);
            }

            Selection.activeGameObject = go;
        }
    }
}
