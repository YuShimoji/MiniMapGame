using UnityEngine;

namespace MiniMapGame.Data
{
    /// <summary>
    /// Color theme for map rendering. Matches JSX THEMES (dark/parchment).
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapTheme", menuName = "MiniMapGame/MapTheme")]
    public class MapTheme : ScriptableObject
    {
        public string displayName;

        [Header("Background")]
        public Color backgroundColor;
        public Color groundColor;

        [Header("Roads (Tier 0/1/2)")]
        public Color roadOuter0;
        public Color roadOuter1;
        public Color roadOuter2;
        public Color roadFill0;
        public Color roadFill1;
        public Color roadFill2;

        [Header("Buildings")]
        public Color buildingFill;
        public Color buildingFillLandmark;
        public Color buildingStroke;

        [Header("Terrain")]
        public Color coastColor;
        public Color riverColor;

        [Header("Nodes")]
        public Color nodeColor;
        public Color plazaNodeColor;

        [Header("UI")]
        public Color textColor;

        [Header("Analysis")]
        public Color deadEndColor;
        public Color chokeColor;
        public Color plazaColor;
        public Color intersectionColor;
    }
}
