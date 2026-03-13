using UnityEngine;
using UnityEditor;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to auto-create the 7 default MapPreset ScriptableObject assets.
    /// Menu: MiniMapGame > Create Default Presets
    /// </summary>
    public static class MapPresetCreator
    {
        [MenuItem("MiniMapGame/Create Default Presets")]
        public static void CreateDefaultPresets()
        {
            string folder = "Assets/Resources/Presets";
            var modernRoad = AssetDatabase.LoadAssetAtPath<RoadProfile>(
                "Assets/Resources/RoadProfiles/RoadProfile_Modern.asset");
            var ruralRoad = AssetDatabase.LoadAssetAtPath<RoadProfile>(
                "Assets/Resources/RoadProfiles/RoadProfile_Rural.asset");
            var historicRoad = AssetDatabase.LoadAssetAtPath<RoadProfile>(
                "Assets/Resources/RoadProfiles/RoadProfile_Historic.asset");

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Presets");

            CreatePreset(folder, "Preset_Coastal", new PresetDef
            {
                displayName = "Coastal",
                generatorType = GeneratorType.Organic,
                arterialRange = new Vector2Int(7, 8),
                hasRingRoad = true,
                curveAmount = 0.62f,
                buildingDensity = 0.68f,
                hasCoast = true,
                hasRiver = true,
                hillDensity = 0.38f,
                maxElevation = 12f,
                elevationScale = 0.9f,
                steepnessBias = 0.45f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.62f,
                roadProfile = modernRoad,
                description = "Organic harbor town with a readable waterfront, ring road, and low coastal hills."
            });

            CreatePreset(folder, "Preset_Rural", new PresetDef
            {
                displayName = "Rural",
                generatorType = GeneratorType.Rural,
                arterialRange = new Vector2Int(4, 5),
                hasRingRoad = false,
                curveAmount = 0.58f,
                buildingDensity = 0.20f,
                hasCoast = false,
                hasRiver = true,
                hillDensity = 0.55f,
                maxElevation = 10f,
                elevationScale = 0.85f,
                steepnessBias = 0.28f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.48f,
                roadProfile = ruralRoad,
                description = "Open farmland with light road loops, a river corridor, and gentle rolling hills."
            });

            CreatePreset(folder, "Preset_Grid", new PresetDef
            {
                displayName = "NYC Grid",
                generatorType = GeneratorType.Grid,
                arterialRange = new Vector2Int(5, 7),
                hasRingRoad = false,
                curveAmount = 0.02f,
                buildingDensity = 0.82f,
                hasCoast = false,
                hasRiver = true,
                hillDensity = 0.02f,
                maxElevation = 1.5f,
                elevationScale = 0.10f,
                steepnessBias = 0.15f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.42f,
                roadProfile = modernRoad,
                description = "Orderly canal grid with mid-density blocks and only slight grade changes."
            });

            CreatePreset(folder, "Preset_Mountain", new PresetDef
            {
                displayName = "Mountain",
                generatorType = GeneratorType.Mountain,
                arterialRange = new Vector2Int(2, 3),
                hasRingRoad = false,
                curveAmount = 0.94f,
                buildingDensity = 0.08f,
                hasCoast = false,
                hasRiver = true,
                hillDensity = 1.0f,
                maxElevation = 48f,
                elevationScale = 1.8f,
                steepnessBias = 0.88f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.16f,
                roadProfile = ruralRoad,
                description = "High-relief mountain trail cut by a narrow stream and sharp switchbacks."
            });

            CreatePreset(folder, "Preset_Island", new PresetDef
            {
                displayName = "Island",
                generatorType = GeneratorType.Organic,
                arterialRange = new Vector2Int(4, 6),
                hasRingRoad = true,
                curveAmount = 0.67f,
                buildingDensity = 0.52f,
                hasCoast = true,
                hasRiver = false,
                hillDensity = 0.22f,
                worldWidth = 620f,
                worldHeight = 620f,
                borderPadding = 40f,
                maxElevation = 8f,
                elevationScale = 0.70f,
                steepnessBias = 0.35f,
                enableBridges = false,
                waterProfile = null,
                decorationDensity = 0.58f,
                roadProfile = historicRoad,
                description = "Compact island-like port with a dominant shoreline and a tight circular core."
            });

            CreatePreset(folder, "Preset_Downtown", new PresetDef
            {
                displayName = "Downtown",
                generatorType = GeneratorType.Grid,
                arterialRange = new Vector2Int(6, 8),
                hasRingRoad = false,
                curveAmount = 0.01f,
                buildingDensity = 0.98f,
                hasCoast = false,
                hasRiver = false,
                hillDensity = 0.01f,
                worldWidth = 720f,
                worldHeight = 520f,
                borderPadding = 40f,
                maxElevation = 1.2f,
                elevationScale = 0.06f,
                steepnessBias = 0.10f,
                enableBridges = false,
                waterProfile = null,
                decorationDensity = 0.28f,
                roadProfile = modernRoad,
                description = "Compressed downtown core with rigid blocks, dense building walls, and near-flat grade."
            });

            CreatePreset(folder, "Preset_Valley", new PresetDef
            {
                displayName = "Valley",
                generatorType = GeneratorType.Mountain,
                arterialRange = new Vector2Int(2, 3),
                hasRingRoad = false,
                curveAmount = 0.74f,
                buildingDensity = 0.07f,
                hasCoast = false,
                hasRiver = true,
                hillDensity = 1.0f,
                worldWidth = 640f,
                worldHeight = 720f,
                borderPadding = 50f,
                maxElevation = 34f,
                elevationScale = 1.35f,
                steepnessBias = 0.94f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.22f,
                roadProfile = ruralRoad,
                description = "River-cut mountain corridor with repeated chokepoints, sparse structures, and steep walls."
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MapPresetCreator] Created 7 default presets in " + folder);
        }

        private struct PresetDef
        {
            public string displayName;
            public GeneratorType generatorType;
            public Vector2Int arterialRange;
            public bool hasRingRoad;
            public float curveAmount;
            public float buildingDensity;
            public bool hasCoast;
            public bool hasRiver;
            public float hillDensity;
            public string description;
            public float worldWidth;
            public float worldHeight;
            public float borderPadding;
            public float maxElevation;
            public float elevationScale;
            public float steepnessBias;
            public bool enableBridges;
            public bool enableTunnels;
            public WaterProfile waterProfile;
            public float decorationDensity;
            public RoadProfile roadProfile;
        }

        private static void CreatePreset(string folder, string fileName, PresetDef def)
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MapPreset>(path);
            if (existing != null)
            {
                Debug.Log($"[MapPresetCreator] {fileName} already exists, skipping.");
                return;
            }

            var preset = ScriptableObject.CreateInstance<MapPreset>();
            preset.displayName = def.displayName;
            preset.generatorType = def.generatorType;
            preset.arterialRange = def.arterialRange;
            preset.hasRingRoad = def.hasRingRoad;
            preset.curveAmount = def.curveAmount;
            preset.buildingDensity = def.buildingDensity;
            preset.hasCoast = def.hasCoast;
            preset.hasRiver = def.hasRiver;
            preset.hillDensity = def.hillDensity;
            preset.description = def.description;
            preset.worldWidth = def.worldWidth > 0f ? def.worldWidth : preset.worldWidth;
            preset.worldHeight = def.worldHeight > 0f ? def.worldHeight : preset.worldHeight;
            preset.borderPadding = def.borderPadding > 0f ? def.borderPadding : preset.borderPadding;
            preset.maxElevation = def.maxElevation;
            preset.elevationScale = def.elevationScale;
            preset.steepnessBias = def.steepnessBias;
            preset.enableBridges = def.enableBridges;
            preset.enableTunnels = def.enableTunnels;
            preset.waterProfile = def.waterProfile;
            preset.decorationDensity = def.decorationDensity;
            preset.roadProfile = def.roadProfile;

            AssetDatabase.CreateAsset(preset, path);
            Debug.Log($"[MapPresetCreator] Created {path}");
        }
    }
}
