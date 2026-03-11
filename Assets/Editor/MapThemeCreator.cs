using UnityEngine;
using UnityEditor;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to auto-create dark and parchment MapTheme ScriptableObject assets.
    /// Menu: MiniMapGame > Create Default Themes
    /// </summary>
    public static class MapThemeCreator
    {
        private const string ThemeFolder = "Assets/Resources/Themes";

        // Dark theme ground palette: cool, muted earth tones
        private static readonly Color DarkGroundBaseColor = new Color(0.28f, 0.33f, 0.24f, 1f);
        private static readonly Color DarkGroundMidColor = new Color(0.38f, 0.34f, 0.26f, 1f);
        private static readonly Color DarkGroundHighColor = new Color(0.48f, 0.44f, 0.38f, 1f);
        private static readonly Color DarkGroundSlopeColor = new Color(0.34f, 0.30f, 0.26f, 1f);
        private static readonly Color DarkGroundMoistureTint = new Color(0.18f, 0.28f, 0.32f, 1f);
        private static readonly Color DarkGroundRoadTint = new Color(0.38f, 0.36f, 0.33f, 1f);
        private static readonly Color DarkGroundBuildingTint = new Color(0.36f, 0.33f, 0.30f, 1f);
        private static readonly Color DarkGroundContourColor = new Color(0.18f, 0.22f, 0.16f, 1f);
        private static readonly Color DarkGroundGridColor = new Color(0.22f, 0.26f, 0.20f, 1f);

        // Parchment theme ground palette: warm, light beige-green tones
        private static readonly Color ParchGroundBaseColor = new Color(0.62f, 0.64f, 0.48f, 1f);
        private static readonly Color ParchGroundMidColor = new Color(0.68f, 0.66f, 0.52f, 1f);
        private static readonly Color ParchGroundHighColor = new Color(0.74f, 0.70f, 0.58f, 1f);
        private static readonly Color ParchGroundSlopeColor = new Color(0.60f, 0.56f, 0.46f, 1f);
        private static readonly Color ParchGroundMoistureTint = new Color(0.48f, 0.58f, 0.55f, 1f);
        private static readonly Color ParchGroundRoadTint = new Color(0.70f, 0.66f, 0.54f, 1f);
        private static readonly Color ParchGroundBuildingTint = new Color(0.68f, 0.64f, 0.54f, 1f);
        private static readonly Color ParchGroundContourColor = new Color(0.50f, 0.48f, 0.38f, 1f);
        private static readonly Color ParchGroundGridColor = new Color(0.56f, 0.54f, 0.42f, 1f);

        [MenuItem("MiniMapGame/Create Default Themes")]
        public static void CreateDefaultThemes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ThemeFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "Themes");

            CreateDarkTheme();
            CreateParchmentTheme();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MapThemeCreator] Created 2 default themes in " + ThemeFolder);
        }

        private static void CreateDarkTheme()
        {
            string path = $"{ThemeFolder}/Theme_Dark.asset";
            var t = AssetDatabase.LoadAssetAtPath<MapTheme>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<MapTheme>();
                AssetDatabase.CreateAsset(t, path);
            }

            t.displayName = "Dark";

            // Background
            t.backgroundColor = HexColor("06090d");
            t.groundColor = DarkGroundBaseColor;

            // Roads (Tier 0/1/2)
            t.roadOuter0 = HexColor("2c3e58");
            t.roadOuter1 = HexColor("3a5070");
            t.roadOuter2 = HexColor("1e2c40");
            t.roadFill0 = HexColor("384e68");
            t.roadFill1 = HexColor("486080");
            t.roadFill2 = HexColor("283848");
            // Road markings
            t.markingColor = HexColor("a0a898");
            t.curbColor = HexColor("1a2838");

            // Buildings
            t.buildingFill = HexColor("111c28");
            t.buildingFillLandmark = HexColor("1a2840");
            t.buildingStroke = HexColor("2c3e58");

            // Terrain
            t.coastColor = HexColor("0a1520");
            t.riverColor = HexColor("142030");
            t.shallowWaterColor = new Color(0.10f, 0.22f, 0.32f, 0.45f);
            t.deepWaterColor = new Color(0.02f, 0.06f, 0.16f, 0.92f);
            t.foamColor = new Color(0.5f, 0.6f, 0.7f, 0.5f);

            // Nodes
            t.nodeColor = HexColor("4a6a8a");
            t.plazaNodeColor = HexColor("5a80a8");

            // UI
            t.textColor = HexColor("c0d8f0");

            // Analysis
            t.deadEndColor = new Color(0.9f, 0.3f, 0.3f, 0.8f);
            t.chokeColor = new Color(0.9f, 0.6f, 0.2f, 0.8f);
            t.intersectionColor = new Color(0.3f, 0.8f, 0.4f, 0.8f);
            t.plazaColor = new Color(0.3f, 0.5f, 0.9f, 0.8f);

            // Lighting
            t.directionalLightColor = new Color(0.85f, 0.9f, 1.0f);
            t.directionalLightIntensity = 0.8f;
            t.ambientColor = new Color(0.04f, 0.06f, 0.1f);
            t.shadowStrength = 0.4f;

            // Post-Processing
            t.bloomIntensity = 0.3f;
            t.bloomThreshold = 0.9f;
            t.vignetteIntensity = 0.25f;
            t.vignetteColor = Color.black;
            t.contrast = 8f;
            t.saturation = -10f;

            // Fog
            t.enableFog = true;
            t.fogColor = HexColor("06090d");
            t.fogStartDistance = 100f;
            t.fogEndDistance = 400f;

            // Ambient Particles
            t.ambientParticleColor = new Color(0.5f, 0.7f, 1f, 0.15f);

            // Ground
            t.gridLineColor = DarkGroundGridColor;
            t.gridSize = 20f;
            t.gridOpacity = 0.12f;
            ApplyDarkGroundPalette(t);

            EditorUtility.SetDirty(t);
            Debug.Log($"[MapThemeCreator] Updated {path}");
        }

        private static void CreateParchmentTheme()
        {
            string path = $"{ThemeFolder}/Theme_Parchment.asset";
            var t = AssetDatabase.LoadAssetAtPath<MapTheme>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<MapTheme>();
                AssetDatabase.CreateAsset(t, path);
            }

            t.displayName = "Parchment";

            // Background
            t.backgroundColor = HexColor("e8e0cc");
            t.groundColor = ParchGroundBaseColor;

            // Roads (Tier 0/1/2)
            t.roadOuter0 = HexColor("888060");
            t.roadOuter1 = HexColor("9a9070");
            t.roadOuter2 = HexColor("706848");
            t.roadFill0 = HexColor("c8b870");
            t.roadFill1 = HexColor("d8c880");
            t.roadFill2 = HexColor("b0a060");
            // Road markings
            t.markingColor = HexColor("504830");
            t.curbColor = HexColor("605840");

            // Buildings
            t.buildingFill = HexColor("c8bca0");
            t.buildingFillLandmark = HexColor("b0a888");
            t.buildingStroke = HexColor("888060");

            // Terrain
            t.coastColor = HexColor("b8c8d0");
            t.riverColor = HexColor("90b0c8");
            t.shallowWaterColor = new Color(0.45f, 0.58f, 0.65f, 0.40f);
            t.deepWaterColor = new Color(0.25f, 0.40f, 0.55f, 0.85f);
            t.foamColor = new Color(0.90f, 0.92f, 0.88f, 0.55f);

            // Nodes
            t.nodeColor = HexColor("706040");
            t.plazaNodeColor = HexColor("605030");

            // UI
            t.textColor = HexColor("2a2418");

            // Analysis
            t.deadEndColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            t.chokeColor = new Color(0.8f, 0.5f, 0.1f, 0.8f);
            t.intersectionColor = new Color(0.2f, 0.6f, 0.3f, 0.8f);
            t.plazaColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);

            // Lighting
            t.directionalLightColor = new Color(1.0f, 0.95f, 0.85f);
            t.directionalLightIntensity = 1.0f;
            t.ambientColor = new Color(0.5f, 0.48f, 0.4f);
            t.shadowStrength = 0.3f;

            // Post-Processing
            t.bloomIntensity = 0.2f;
            t.bloomThreshold = 1.2f;
            t.vignetteIntensity = 0.15f;
            t.vignetteColor = HexColor("3a2a1a");
            t.contrast = 5f;
            t.saturation = -5f;

            // Fog
            t.enableFog = true;
            t.fogColor = HexColor("e8e0cc");
            t.fogStartDistance = 120f;
            t.fogEndDistance = 450f;

            // Ambient Particles
            t.ambientParticleColor = new Color(0.7f, 0.6f, 0.4f, 0.12f);

            // Ground
            t.gridLineColor = ParchGroundGridColor;
            t.gridSize = 20f;
            t.gridOpacity = 0.10f;
            ApplyParchmentGroundPalette(t);

            EditorUtility.SetDirty(t);
            Debug.Log($"[MapThemeCreator] Updated {path}");
        }

        private static void ApplyDarkGroundPalette(MapTheme theme)
        {
            theme.groundMidColor = DarkGroundMidColor;
            theme.groundHighColor = DarkGroundHighColor;
            theme.groundSlopeColor = DarkGroundSlopeColor;
            theme.groundMoistureTint = DarkGroundMoistureTint;
            theme.groundRoadTint = DarkGroundRoadTint;
            theme.groundBuildingTint = DarkGroundBuildingTint;
            theme.groundContourColor = DarkGroundContourColor;
        }

        private static void ApplyParchmentGroundPalette(MapTheme theme)
        {
            theme.groundMidColor = ParchGroundMidColor;
            theme.groundHighColor = ParchGroundHighColor;
            theme.groundSlopeColor = ParchGroundSlopeColor;
            theme.groundMoistureTint = ParchGroundMoistureTint;
            theme.groundRoadTint = ParchGroundRoadTint;
            theme.groundBuildingTint = ParchGroundBuildingTint;
            theme.groundContourColor = ParchGroundContourColor;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
