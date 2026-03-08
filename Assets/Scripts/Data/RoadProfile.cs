using UnityEngine;

namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewRoadProfile", menuName = "MiniMapGame/RoadProfile")]
    public class RoadProfile : ScriptableObject
    {
        [System.Serializable]
        public struct RoadTierConfig
        {
            [Header("Geometry")]
            public string tierName;
            [Range(1, 6)] public int laneCount;
            public float laneWidth;
            public float shoulderWidth;
            public float curbWidth;

            [Header("Markings")]
            public bool hasCenterLine;
            public bool centerLineSolid;
            public bool hasLaneDividers;
            public bool hasEdgeLines;
            public float markingWidth;
            public float dashLength;
            public float dashGap;

            [Header("Surface")]
            [Range(0f, 1f)] public float roughness;
            [Range(0f, 1f)] public float wear;
            [Range(0f, 1f)] public float crackDensity;

            public float TotalWidth => 2f * (curbWidth + shoulderWidth) + laneCount * laneWidth;
        }

        public RoadTierConfig[] tiers = new RoadTierConfig[3];

        public static RoadProfile CreateDefaultFallback()
        {
            var profile = CreateInstance<RoadProfile>();
            profile.tiers = new RoadTierConfig[3];

            profile.tiers[0] = new RoadTierConfig
            {
                tierName = "Arterial",
                laneCount = 4,
                laneWidth = 0.225f,
                shoulderWidth = 0.15f,
                curbWidth = 0.1f,
                hasCenterLine = true,
                centerLineSolid = true,
                hasLaneDividers = true,
                hasEdgeLines = true,
                markingWidth = 0.02f,
                dashLength = 2f,
                dashGap = 1.5f,
                roughness = 0.2f,
                wear = 0.1f,
                crackDensity = 0.05f
            };

            profile.tiers[1] = new RoadTierConfig
            {
                tierName = "Secondary",
                laneCount = 2,
                laneWidth = 0.275f,
                shoulderWidth = 0.1f,
                curbWidth = 0.075f,
                hasCenterLine = true,
                centerLineSolid = false,
                hasLaneDividers = false,
                hasEdgeLines = true,
                markingWidth = 0.015f,
                dashLength = 1.5f,
                dashGap = 1.5f,
                roughness = 0.35f,
                wear = 0.2f,
                crackDensity = 0.1f
            };

            profile.tiers[2] = new RoadTierConfig
            {
                tierName = "Alley",
                laneCount = 1,
                laneWidth = 0.28f,
                shoulderWidth = 0.05f,
                curbWidth = 0.06f,
                hasCenterLine = false,
                centerLineSolid = false,
                hasLaneDividers = false,
                hasEdgeLines = false,
                markingWidth = 0f,
                dashLength = 0f,
                dashGap = 0f,
                roughness = 0.5f,
                wear = 0.4f,
                crackDensity = 0.25f
            };

            return profile;
        }
    }
}
