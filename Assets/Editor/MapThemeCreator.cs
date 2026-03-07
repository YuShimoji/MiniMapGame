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
            if (AssetDatabase.LoadAssetAtPath<MapTheme>(path) != null) return;

            var t = ScriptableObject.CreateInstance<MapTheme>();
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

            // Buildings
            t.buildingFill = HexColor("111c28");
            t.buildingFillLandmark = HexColor("1a2840");
            t.buildingStroke = HexColor("2c3e58");

            // Terrain
            t.coastColor = HexColor("0a1520");
            t.riverColor = HexColor("142030");

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

            AssetDatabase.CreateAsset(t, path);
            Debug.Log($"[MapThemeCreator] Created {path}");
        }

        private static void CreateParchmentTheme()
        {
            string path = $"{ThemeFolder}/Theme_Parchment.asset";
            if (AssetDatabase.LoadAssetAtPath<MapTheme>(path) != null) return;

            var t = ScriptableObject.CreateInstance<MapTheme>();
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

            // Buildings
            t.buildingFill = HexColor("c8bca0");
            t.buildingFillLandmark = HexColor("b0a888");
            t.buildingStroke = HexColor("888060");

            // Terrain
            t.coastColor = HexColor("b8c8d0");
            t.riverColor = HexColor("90b0c8");

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

            AssetDatabase.CreateAsset(t, path);
            Debug.Log($"[MapThemeCreator] Created {path}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
