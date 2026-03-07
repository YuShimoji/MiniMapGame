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

        [Header("Lighting")]
        public Color directionalLightColor = Color.white;
        public float directionalLightIntensity = 0.8f;
        public Color ambientColor = new Color(0.04f, 0.06f, 0.1f);
        public float shadowStrength = 0.4f;

        [Header("Post-Processing")]
        public float bloomIntensity = 0.3f;
        public float bloomThreshold = 0.9f;
        public float vignetteIntensity = 0.25f;
        public Color vignetteColor = Color.black;
        public float contrast = 8f;
        public float saturation = -10f;

        [Header("Fog")]
        public bool enableFog = true;
        public Color fogColor;
        public float fogStartDistance = 100f;
        public float fogEndDistance = 400f;

        [Header("Ambient Particles")]
        public Color ambientParticleColor = new Color(0.5f, 0.7f, 1f, 0.15f);

        [Header("Ground")]
        public Color gridLineColor;
        public float gridSize = 20f;
        public float gridOpacity = 0.15f;
    }
}
