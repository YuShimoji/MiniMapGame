using UnityEngine;
using UnityEditor;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to auto-create the 3 default RoadProfile ScriptableObject assets.
    /// Menu: MiniMapGame > Create Default Road Profiles
    /// </summary>
    public static class RoadProfileCreator
    {
        [MenuItem("MiniMapGame/Create Default Road Profiles")]
        public static void CreateDefaultProfiles()
        {
            string folder = "Assets/Resources/RoadProfiles";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "RoadProfiles");

            // Modern profile
            CreateProfile(folder, "RoadProfile_Modern", "Modern", new RoadProfile.RoadTierConfig[]
            {
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Highway",
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
                    roughness = 0.15f,
                    wear = 0.05f,
                    crackDensity = 0.02f
                },
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Street",
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
                    roughness = 0.25f,
                    wear = 0.15f,
                    crackDensity = 0.08f
                },
                new RoadProfile.RoadTierConfig
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
                    roughness = 0.4f,
                    wear = 0.3f,
                    crackDensity = 0.15f
                }
            });

            // Rural profile
            CreateProfile(folder, "RoadProfile_Rural", "Rural", new RoadProfile.RoadTierConfig[]
            {
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Country Road",
                    laneCount = 2,
                    laneWidth = 0.35f,
                    shoulderWidth = 0.05f,
                    curbWidth = 0.05f,
                    hasCenterLine = true,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0.012f,
                    dashLength = 3f,
                    dashGap = 3f,
                    roughness = 0.5f,
                    wear = 0.4f,
                    crackDensity = 0.2f
                },
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Farm Track",
                    laneCount = 1,
                    laneWidth = 0.4f,
                    shoulderWidth = 0.04f,
                    curbWidth = 0.03f,
                    hasCenterLine = false,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0f,
                    dashLength = 0f,
                    dashGap = 0f,
                    roughness = 0.7f,
                    wear = 0.6f,
                    crackDensity = 0.35f
                },
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Path",
                    laneCount = 1,
                    laneWidth = 0.22f,
                    shoulderWidth = 0.02f,
                    curbWidth = 0.02f,
                    hasCenterLine = false,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0f,
                    dashLength = 0f,
                    dashGap = 0f,
                    roughness = 0.85f,
                    wear = 0.75f,
                    crackDensity = 0.5f
                }
            });

            // Historic profile
            CreateProfile(folder, "RoadProfile_Historic", "Historic", new RoadProfile.RoadTierConfig[]
            {
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Boulevard",
                    laneCount = 2,
                    laneWidth = 0.35f,
                    shoulderWidth = 0.1f,
                    curbWidth = 0.1f,
                    hasCenterLine = false,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0f,
                    dashLength = 0f,
                    dashGap = 0f,
                    roughness = 0.6f,
                    wear = 0.35f,
                    crackDensity = 0.15f
                },
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Cobblestone",
                    laneCount = 1,
                    laneWidth = 0.35f,
                    shoulderWidth = 0.08f,
                    curbWidth = 0.08f,
                    hasCenterLine = false,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0f,
                    dashLength = 0f,
                    dashGap = 0f,
                    roughness = 0.8f,
                    wear = 0.25f,
                    crackDensity = 0.1f
                },
                new RoadProfile.RoadTierConfig
                {
                    tierName = "Passage",
                    laneCount = 1,
                    laneWidth = 0.2f,
                    shoulderWidth = 0.04f,
                    curbWidth = 0.04f,
                    hasCenterLine = false,
                    centerLineSolid = false,
                    hasLaneDividers = false,
                    hasEdgeLines = false,
                    markingWidth = 0f,
                    dashLength = 0f,
                    dashGap = 0f,
                    roughness = 0.9f,
                    wear = 0.5f,
                    crackDensity = 0.3f
                }
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoadProfileCreator] Created 3 default road profiles in " + folder);
        }

        private static void CreateProfile(string folder, string fileName, string profileName, RoadProfile.RoadTierConfig[] tiers)
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoadProfile>(path);
            if (existing != null)
            {
                Debug.Log($"[RoadProfileCreator] {fileName} already exists, skipping.");
                return;
            }

            var profile = ScriptableObject.CreateInstance<RoadProfile>();
            profile.name = profileName;
            profile.tiers = tiers;

            AssetDatabase.CreateAsset(profile, path);
            Debug.Log($"[RoadProfileCreator] Created {path}");
        }
    }
}
