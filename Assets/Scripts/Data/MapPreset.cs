using UnityEngine;

namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewMapPreset", menuName = "MiniMapGame/MapPreset")]
    public class MapPreset : ScriptableObject
    {
        public string displayName;
        public GeneratorType generatorType;
        public Vector2Int arterialRange;
        public bool hasRingRoad;
        [Range(0f, 1f)] public float curveAmount;
        [Range(0f, 1f)] public float buildingDensity;
        public bool hasCoast;
        public bool hasRiver;
        [Range(0f, 1f)] public float hillDensity;
        [TextArea] public string description;

        public float worldWidth = 860f;
        public float worldHeight = 580f;
        public float borderPadding = 50f;
    }
}
