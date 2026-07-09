using System;
using UnityEngine;

namespace LedShow.Gameplay
{
    // Scrolls each layer's UV offset instead of moving/repositioning geometry:
    // the background never runs out of mesh to show, so there is nothing to
    // seam between tiles as long as the source texture wraps (Repeat mode).
    // Scroll speed itself is fixed here - the Timeline drives when things
    // happen in the level, not how fast the background moves.
    public class ParallaxBackground : MonoBehaviour
    {
        [Serializable]
        private struct Layer
        {
            public Renderer renderer;
            public float parallaxFactor;
        }

        [SerializeField] private float scrollSpeed = 2f;

        [SerializeField] private Layer sky = new Layer { parallaxFactor = 0.1f };
        [SerializeField] private Layer mountains = new Layer { parallaxFactor = 0.4f };
        [SerializeField] private Layer ground = new Layer { parallaxFactor = 1f };

        private Material skyMaterial;
        private Material mountainsMaterial;
        private Material groundMaterial;

        private void Awake()
        {
            skyMaterial = CacheMaterial(sky);
            mountainsMaterial = CacheMaterial(mountains);
            groundMaterial = CacheMaterial(ground);
        }

        private void Update()
        {
            float distance = scrollSpeed * Time.deltaTime;
            Scroll(skyMaterial, sky.parallaxFactor, distance);
            Scroll(mountainsMaterial, mountains.parallaxFactor, distance);
            Scroll(groundMaterial, ground.parallaxFactor, distance);
        }

        // Cached once so Update never touches Renderer.material - repeated
        // access there instantiates a fresh material copy every call, which
        // would allocate every frame and is a common source of stutter.
        private static Material CacheMaterial(Layer layer)
        {
            if (layer.renderer == null)
            {
                return null;
            }

            Material material = layer.renderer.material;
            WarnIfNotSeamless(material);
            return material;
        }

        private static void WarnIfNotSeamless(Material material)
        {
            Texture texture = material.mainTexture;
            if (texture != null && texture.wrapMode != TextureWrapMode.Repeat)
            {
                Debug.LogWarning(
                    $"Parallax texture '{texture.name}' is not set to Repeat wrap mode; it will show a tiling seam while scrolling.",
                    texture);
            }
        }

        // Wrapped into [0, 1) every frame so the offset never grows large
        // enough for float precision loss to introduce jitter on long runs.
        private static void Scroll(Material material, float parallaxFactor, float distance)
        {
            if (material == null)
            {
                return;
            }

            Vector2 offset = material.mainTextureOffset;
            offset.x = (offset.x + distance * parallaxFactor) % 1f;
            material.mainTextureOffset = offset;
        }
    }
}
