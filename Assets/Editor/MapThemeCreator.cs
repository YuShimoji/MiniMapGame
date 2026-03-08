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

            t.displayName = "ダーク";

            // Background
            t.backgroundColor = HexColor("06090d");
            t.groundColor = HexColor("090c12");

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
            t.gridLineColor = HexColor("0e1520");
            t.gridSize = 20f;
            t.gridOpacity = 0.15f;

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

            t.displayName = "パーチメント";

            // Background
            t.backgroundColor = HexColor("e8e0cc");
            t.groundColor = HexColor("dfd7be");

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
            t.gridLineColor = HexColor("c8c0a8");
            t.gridSize = 20f;
            t.gridOpacity = 0.12f;

            EditorUtility.SetDirty(t);
            Debug.Log($"[MapThemeCreator] Updated {path}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
