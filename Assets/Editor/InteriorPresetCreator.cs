using UnityEngine;
using UnityEditor;
using MiniMapGame.Interior;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to auto-create the 6 default InteriorPreset ScriptableObject assets.
    /// Menu: MiniMapGame > Create Default Interior Presets
    /// </summary>
    public static class InteriorPresetCreator
    {
        [MenuItem("MiniMapGame/Create Default Interior Presets")]
        public static void CreateDefaultInteriorPresets()
        {
            string folder = "Assets/Resources/InteriorPresets";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "InteriorPresets");

            CreatePreset(folder, "InteriorPreset_Modern", new PresetDef
            {
                displayName = "Modern Urban",
                style = InteriorStyle.Modern,
                minRoomSize = 4f,
                maxRoomSize = 12f,
                maxRoomsPerFloor = 8,
                corridorWidth = 2f,
                wallHeight = 3f,
                doorWidth = 1.2f,
                irregularity = 0.1f,
                basementFloors = 1,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.05f,
                wallVoidProbability = 0.1f,
                discoveryDensity = 0.3f,
                secretRoomProbability = 0.08f,
                lockedDoorProbability = 0.05f,
                furnitureDensity = 0.6f,
                decayLevel = 0f,
                floorColor = new Color(0.7f, 0.72f, 0.75f),
                wallColor = new Color(0.4f, 0.42f, 0.45f),
                corridorColor = new Color(0.6f, 0.6f, 0.62f),
                secretRoomColor = new Color(0.3f, 0.15f, 0.5f),
                description = "Modern urban architecture. Orderly room layout with functional circulation."
            });

            CreatePreset(folder, "InteriorPreset_Urban", new PresetDef
            {
                displayName = "Downtown Office",
                style = InteriorStyle.Urban,
                minRoomSize = 3.5f,
                maxRoomSize = 10f,
                maxRoomsPerFloor = 10,
                corridorWidth = 1.8f,
                wallHeight = 2.8f,
                doorWidth = 1.0f,
                irregularity = 0.15f,
                basementFloors = 2,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.08f,
                wallVoidProbability = 0.15f,
                discoveryDensity = 0.4f,
                secretRoomProbability = 0.12f,
                lockedDoorProbability = 0.1f,
                furnitureDensity = 0.7f,
                decayLevel = 0.05f,
                floorColor = new Color(0.55f, 0.58f, 0.62f),
                wallColor = new Color(0.35f, 0.37f, 0.4f),
                corridorColor = new Color(0.48f, 0.48f, 0.52f),
                secretRoomColor = new Color(0.35f, 0.18f, 0.55f),
                description = "Dense downtown building. Many small rooms and complex structure."
            });

            CreatePreset(folder, "InteriorPreset_Suburban", new PresetDef
            {
                displayName = "Suburban Home",
                style = InteriorStyle.Suburban,
                minRoomSize = 4.5f,
                maxRoomSize = 14f,
                maxRoomsPerFloor = 6,
                corridorWidth = 2.2f,
                wallHeight = 2.8f,
                doorWidth = 1.3f,
                irregularity = 0.2f,
                basementFloors = 0,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.03f,
                wallVoidProbability = 0.05f,
                discoveryDensity = 0.2f,
                secretRoomProbability = 0.05f,
                lockedDoorProbability = 0.02f,
                furnitureDensity = 0.65f,
                decayLevel = 0f,
                floorColor = new Color(0.75f, 0.73f, 0.68f),
                wallColor = new Color(0.45f, 0.42f, 0.38f),
                corridorColor = new Color(0.62f, 0.6f, 0.55f),
                secretRoomColor = new Color(0.5f, 0.3f, 0.6f),
                description = "Spacious suburban home. Larger rooms and open layout."
            });

            CreatePreset(folder, "InteriorPreset_Rural", new PresetDef
            {
                displayName = "Rural Cottage",
                style = InteriorStyle.Rural,
                minRoomSize = 5f,
                maxRoomSize = 16f,
                maxRoomsPerFloor = 4,
                corridorWidth = 2.5f,
                wallHeight = 2.5f,
                doorWidth = 1.4f,
                irregularity = 0.35f,
                basementFloors = 0,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.15f,
                wallVoidProbability = 0.2f,
                discoveryDensity = 0.15f,
                secretRoomProbability = 0.03f,
                lockedDoorProbability = 0.01f,
                furnitureDensity = 0.4f,
                decayLevel = 0.1f,
                floorColor = new Color(0.65f, 0.6f, 0.5f),
                wallColor = new Color(0.4f, 0.35f, 0.28f),
                corridorColor = new Color(0.55f, 0.5f, 0.42f),
                secretRoomColor = new Color(0.4f, 0.25f, 0.5f),
                description = "Simple rural building. Irregular layout with modest furnishing."
            });

            CreatePreset(folder, "InteriorPreset_Ruin", new PresetDef
            {
                displayName = "Ruin",
                style = InteriorStyle.Mixed,
                minRoomSize = 3f,
                maxRoomSize = 10f,
                maxRoomsPerFloor = 12,
                corridorWidth = 1.5f,
                wallHeight = 2.2f,
                doorWidth = 1.0f,
                irregularity = 0.8f,
                basementFloors = 1,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.3f,
                wallVoidProbability = 0.4f,
                discoveryDensity = 0.7f,
                secretRoomProbability = 0.2f,
                lockedDoorProbability = 0.15f,
                furnitureDensity = 0.2f,
                decayLevel = 0.85f,
                floorColor = new Color(0.4f, 0.38f, 0.35f),
                wallColor = new Color(0.28f, 0.26f, 0.24f),
                corridorColor = new Color(0.35f, 0.33f, 0.3f),
                secretRoomColor = new Color(0.3f, 0.15f, 0.4f),
                description = "Crumbling ruin. Collapsed walls and eerie spatial layout."
            });

            CreatePreset(folder, "InteriorPreset_Bizarre", new PresetDef
            {
                displayName = "Bizarre",
                style = InteriorStyle.Bizarre,
                minRoomSize = 3f,
                maxRoomSize = 18f,
                maxRoomsPerFloor = 14,
                corridorWidth = 1.5f,
                wallHeight = 4.5f,
                doorWidth = 0.8f,
                irregularity = 0.95f,
                basementFloors = 3,
                useExteriorFloorCount = true,
                overrideFloorCount = 1,
                deadSpaceRatio = 0.25f,
                wallVoidProbability = 0.35f,
                discoveryDensity = 0.9f,
                secretRoomProbability = 0.3f,
                lockedDoorProbability = 0.2f,
                furnitureDensity = 0.3f,
                decayLevel = 0.4f,
                floorColor = new Color(0.5f, 0.45f, 0.55f),
                wallColor = new Color(0.3f, 0.25f, 0.35f),
                corridorColor = new Color(0.42f, 0.38f, 0.48f),
                secretRoomColor = new Color(0.6f, 0.1f, 0.6f),
                description = "Non-Euclidean bizarre architecture. Defies spatial logic."
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InteriorPresetCreator] Created 6 default interior presets in " + folder);
        }

        private struct PresetDef
        {
            public string displayName;
            public InteriorStyle style;
            public float minRoomSize;
            public float maxRoomSize;
            public int maxRoomsPerFloor;
            public float corridorWidth;
            public float wallHeight;
            public float doorWidth;
            public float irregularity;
            public int basementFloors;
            public bool useExteriorFloorCount;
            public int overrideFloorCount;
            public float deadSpaceRatio;
            public float wallVoidProbability;
            public float discoveryDensity;
            public float secretRoomProbability;
            public float lockedDoorProbability;
            public float furnitureDensity;
            public float decayLevel;
            public Color floorColor;
            public Color wallColor;
            public Color corridorColor;
            public Color secretRoomColor;
            public string description;
        }

        private static void CreatePreset(string folder, string fileName, PresetDef def)
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<InteriorPreset>(path);
            if (existing != null)
            {
                Debug.Log($"[InteriorPresetCreator] {fileName} already exists, skipping.");
                return;
            }

            var preset = ScriptableObject.CreateInstance<InteriorPreset>();
            preset.displayName = def.displayName;
            preset.style = def.style;
            preset.minRoomSize = def.minRoomSize;
            preset.maxRoomSize = def.maxRoomSize;
            preset.maxRoomsPerFloor = def.maxRoomsPerFloor;
            preset.corridorWidth = def.corridorWidth;
            preset.wallHeight = def.wallHeight;
            preset.doorWidth = def.doorWidth;
            preset.irregularity = def.irregularity;
            preset.basementFloors = def.basementFloors;
            preset.useExteriorFloorCount = def.useExteriorFloorCount;
            preset.overrideFloorCount = def.overrideFloorCount;
            preset.deadSpaceRatio = def.deadSpaceRatio;
            preset.wallVoidProbability = def.wallVoidProbability;
            preset.discoveryDensity = def.discoveryDensity;
            preset.secretRoomProbability = def.secretRoomProbability;
            preset.lockedDoorProbability = def.lockedDoorProbability;
            preset.furnitureDensity = def.furnitureDensity;
            preset.decayLevel = def.decayLevel;
            preset.floorColor = def.floorColor;
            preset.wallColor = def.wallColor;
            preset.corridorColor = def.corridorColor;
            preset.secretRoomColor = def.secretRoomColor;
            preset.description = def.description;
            preset.roomColorOverrides = null; // Designers will customize per-preset if needed

            AssetDatabase.CreateAsset(preset, path);
            Debug.Log($"[InteriorPresetCreator] Created {path}");
        }
    }
}
