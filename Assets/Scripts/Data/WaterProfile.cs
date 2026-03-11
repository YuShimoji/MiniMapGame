using UnityEngine;

namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewWaterProfile", menuName = "MiniMapGame/WaterProfile")]
    public class WaterProfile : ScriptableObject
    {
        [System.Serializable]
        public struct RiverConfig
        {
            [Header("Geometry")]
            public float baseWidth;
            [Range(1f, 3f)] public float widthGrowth;
            public float depthBase;
            [Range(0f, 1f)] public float depthVariation;

            [Header("Path Generation")]
            public float swayAmount;
            public float stepSizeMin;
            public float stepSizeMax;
            [Range(0f, 1f)] public float meanderFrequency;

            [Header("Terrain Interaction")]
            [Range(0f, 2f)] public float terrainCarveStrength;
            public float terrainCarveRadius;
            [Range(0f, 1f)] public float flowResponsiveness;

            [Header("Sandbank")]
            [Range(0f, 1f)] public float sandbankStrength;

            [Header("Visual")]
            [Range(0f, 1f)] public float roughness;
            [Range(0f, 1f)] public float transparency;
            [Range(0f, 0.5f)] public float foamThreshold;
        }

        [System.Serializable]
        public struct CoastConfig
        {
            [Header("Geometry")]
            [Range(0.2f, 0.5f)] public float inlandReach;
            [Range(0f, 1f)] public float coastlineRoughness;
            public float stepSizeMin;
            public float stepSizeMax;

            [Header("Bay/Cape")]
            [Range(0f, 0.5f)] public float bayAmplitude;
            public float baySpacing;

            [Header("Depth")]
            public float depthBase;
            [Range(0f, 1f)] public float depthVariation;

            [Header("Terrain Interaction")]
            [Range(0f, 2f)] public float terrainCarveStrength;
            public float terrainCarveRadius;

            [Header("Visual")]
            [Range(0f, 1f)] public float roughness;
            [Range(0f, 1f)] public float transparency;
            [Range(0f, 0.5f)] public float foamThreshold;
        }

        public RiverConfig river;
        public CoastConfig coast;

        public static WaterProfile CreateDefaultFallback()
        {
            var profile = CreateInstance<WaterProfile>();

            profile.river = new RiverConfig
            {
                baseWidth = 12f,
                widthGrowth = 1.8f,
                depthBase = 2.5f,
                depthVariation = 0.3f,
                swayAmount = 55f,
                stepSizeMin = 20f,
                stepSizeMax = 55f,
                meanderFrequency = 0.5f,
                terrainCarveStrength = 1.0f,
                terrainCarveRadius = 25f,
                flowResponsiveness = 0.5f,
                sandbankStrength = 0.4f,
                roughness = 0.3f,
                transparency = 0.25f,
                foamThreshold = 0.15f
            };

            profile.coast = new CoastConfig
            {
                inlandReach = 0.35f,
                coastlineRoughness = 0.5f,
                stepSizeMin = 25f,
                stepSizeMax = 55f,
                bayAmplitude = 0.3f,
                baySpacing = 120f,
                depthBase = 1.5f,
                depthVariation = 0.2f,
                terrainCarveStrength = 0.3f,
                terrainCarveRadius = 40f,
                roughness = 0.5f,
                transparency = 0.2f,
                foamThreshold = 0.08f
            };

            return profile;
        }
    }
}
