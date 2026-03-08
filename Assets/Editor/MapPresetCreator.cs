using UnityEngine;
using UnityEditor;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to auto-create the 4 default MapPreset ScriptableObject assets.
    /// Menu: MiniMapGame > Create Default Presets
    /// </summary>
    public static class MapPresetCreator
    {
        [MenuItem("MiniMapGame/Create Default Presets")]
        public static void CreateDefaultPresets()
        {
            string folder = "Assets/Resources/Presets";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Presets");

            CreatePreset(folder, "Preset_Coastal", new PresetDef
            {
                displayName = "港湾都市",
                generatorType = GeneratorType.Organic,
                arterialRange = new Vector2Int(6, 8),
                hasRingRoad = true,
                curveAmount = 0.50f,
                buildingDensity = 0.80f,
                hasCoast = true,
                hasRiver = true,
                hillDensity = 0.30f,
                maxElevation = 10f,
                elevationScale = 0.8f,
                enableBridges = true,
                waterProfile = null, // Uses WaterProfile.CreateDefaultFallback()
                decorationDensity = 0.6f,
                description = "有機的な港町。入り組んだ路地と港湾区域。"
            });

            CreatePreset(folder, "Preset_Rural", new PresetDef
            {
                displayName = "田舎町",
                generatorType = GeneratorType.Rural,
                arterialRange = new Vector2Int(3, 4),
                hasRingRoad = false,
                curveAmount = 0.70f,
                buildingDensity = 0.25f,
                hasCoast = false,
                hasRiver = true,
                hillDensity = 0.75f,
                maxElevation = 12f,
                elevationScale = 1.0f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.3f,
                description = "疎らな集落。広い農村地帯と川。"
            });

            CreatePreset(folder, "Preset_Grid", new PresetDef
            {
                displayName = "NYCグリッド",
                generatorType = GeneratorType.Grid,
                arterialRange = new Vector2Int(5, 7),
                hasRingRoad = false,
                curveAmount = 0.06f,
                buildingDensity = 0.95f,
                hasCoast = false,
                hasRiver = false,
                hillDensity = 0.00f,
                maxElevation = 0f,
                elevationScale = 0f,
                enableBridges = false,
                waterProfile = null,
                decorationDensity = 0.7f,
                description = "整然としたブロック構造。高密度市街地。"
            });

            CreatePreset(folder, "Preset_Mountain", new PresetDef
            {
                displayName = "山道",
                generatorType = GeneratorType.Mountain,
                arterialRange = new Vector2Int(2, 3),
                hasRingRoad = false,
                curveAmount = 0.88f,
                buildingDensity = 0.18f,
                hasCoast = false,
                hasRiver = false,
                hillDensity = 0.95f,
                maxElevation = 40f,
                elevationScale = 1.5f,
                enableBridges = true,
                waterProfile = null,
                decorationDensity = 0.2f,
                description = "険しい山岳路。行き止まりと分岐が多い。"
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MapPresetCreator] Created 4 default presets in " + folder);
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
            // Per-preset tuning
            public float maxElevation;
            public float elevationScale;
            public bool enableBridges;
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
            preset.maxElevation = def.maxElevation;
            preset.elevationScale = def.elevationScale;
            preset.enableBridges = def.enableBridges;
            preset.waterProfile = def.waterProfile;
            preset.decorationDensity = def.decorationDensity;
            preset.roadProfile = def.roadProfile;
            // worldWidth, worldHeight, borderPadding use defaults (860, 580, 50)

            AssetDatabase.CreateAsset(preset, path);
            Debug.Log($"[MapPresetCreator] Created {path}");
        }
    }
}
