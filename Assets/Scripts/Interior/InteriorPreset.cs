using UnityEngine;

namespace MiniMapGame.Interior
{
    [CreateAssetMenu(fileName = "NewInteriorPreset", menuName = "MiniMapGame/InteriorPreset")]
    public class InteriorPreset : ScriptableObject
    {
        public string displayName;

        [Header("General Layout")]
        [Range(3f, 8f)] public float minRoomSize = 4f;
        [Range(6f, 20f)] public float maxRoomSize = 12f;
        [Range(2, 16)] public int maxRoomsPerFloor = 8;
        [Range(1.5f, 4f)] public float corridorWidth = 2f;
        [Range(2f, 5f)] public float wallHeight = 3f;
        [Range(0.8f, 2f)] public float doorWidth = 1.2f;
        [Range(0f, 1f)] public float irregularity = 0.2f;

        [Header("Floor Configuration")]
        [Range(0, 3)] public int basementFloors = 0;
        public bool useExteriorFloorCount = true;
        [Range(1, 10)] public int overrideFloorCount = 1;

        [Header("Dead Space")]
        [Tooltip("Ratio of total floor area that becomes dead space (WallVoid/Shaft)")]
        [Range(0f, 0.4f)] public float deadSpaceRatio = 0.1f;
        [Range(0f, 1f)] public float wallVoidProbability = 0.15f;

        [Header("Discovery")]
        [Range(0f, 1f)] public float discoveryDensity = 0.5f;
        [Range(0f, 1f)] public float secretRoomProbability = 0.1f;
        [Range(0f, 1f)] public float lockedDoorProbability = 0.05f;

        [Header("Furniture")]
        [Range(0f, 1f)] public float furnitureDensity = 0.5f;

        [Header("Decay / Condition")]
        [Tooltip("0 = pristine, 0.5 = worn, 1.0 = ruined")]
        [Range(0f, 1f)] public float decayLevel = 0f;

        [Header("Style")]
        public InteriorStyle style;

        [Header("Visual")]
        public Color floorColor = new Color(0.6f, 0.65f, 0.7f);
        public Color wallColor = new Color(0.3f, 0.35f, 0.4f);
        public Color corridorColor = new Color(0.5f, 0.5f, 0.55f);
        public Color secretRoomColor = new Color(0.4f, 0.2f, 0.6f);

        [Header("Room Type Color Overrides")]
        public RoomColorEntry[] roomColorOverrides;

        [TextArea] public string description;
    }

    public enum InteriorStyle
    {
        Modern,
        Natural,
        Urban,
        Suburban,
        Rural,
        Mixed,
        Bizarre
    }

    [System.Serializable]
    public struct RoomColorEntry
    {
        public InteriorRoomType roomType;
        public Color color;
    }
}
