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

        [Header("Road Markings")]
        public Color markingColor = new Color(0.85f, 0.85f, 0.75f, 1f);
        public Color curbColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Header("Buildings")]
        public Color buildingFill;
        public Color buildingFillLandmark;
        public Color buildingStroke;

        [Header("Terrain")]
        public Color coastColor;
        public Color riverColor;
        public Color shallowWaterColor = new Color(0.15f, 0.35f, 0.45f, 0.45f);
        public Color deepWaterColor = new Color(0.02f, 0.08f, 0.22f, 0.90f);
        public Color foamColor = new Color(0.8f, 0.9f, 1.0f, 0.6f);

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
        public Color gridLineColor = new Color(0.22f, 0.26f, 0.20f, 1f);
        public float gridSize = 20f;
        public float gridOpacity = 0.12f;

        [Header("Ground Surface Compositing")]
        public Color groundMidColor = new Color(0.38f, 0.34f, 0.26f, 1f);
        public Color groundHighColor = new Color(0.48f, 0.44f, 0.38f, 1f);
        public Color groundSlopeColor = new Color(0.34f, 0.30f, 0.26f, 1f);
        public Color groundMoistureTint = new Color(0.18f, 0.28f, 0.32f, 1f);
        public Color groundRoadTint = new Color(0.38f, 0.36f, 0.33f, 1f);
        public Color groundBuildingTint = new Color(0.36f, 0.33f, 0.30f, 1f);
        public Color groundContourColor = new Color(0.18f, 0.22f, 0.16f, 1f);
    }
}
