using UnityEngine;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Holds the two CPU-baked mask textures that drive GridGround.shader compositing.
    /// Owns texture lifetime — call DestroyTextures() on disposal.
    /// </summary>
    public sealed class GroundSemanticMaskSet
    {
        /// <summary>
        /// R: normalized elevation, G: normalized slope,
        /// B: curvature (0.5 = flat), A: contour jitter.
        /// </summary>
        public Texture2D HeightSlopeTexture { get; }

        /// <summary>
        /// R: moisture/shore influence, G: road influence,
        /// B: building influence, A: intersection boost.
        /// </summary>
        public Texture2D SemanticTexture { get; }

        public int Resolution { get; }

        public GroundSemanticMaskSet(Texture2D heightSlope, Texture2D semantic, int resolution)
        {
            HeightSlopeTexture = heightSlope;
            SemanticTexture = semantic;
            Resolution = resolution;
        }

        public void DestroyTextures()
        {
            if (HeightSlopeTexture != null) Object.Destroy(HeightSlopeTexture);
            if (SemanticTexture != null) Object.Destroy(SemanticTexture);
        }
    }
}
