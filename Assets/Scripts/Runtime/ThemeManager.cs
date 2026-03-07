using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Applies MapTheme to all visual systems: rendering, lighting, fog, post-processing, particles.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [Header("Theme")]
        public MapTheme activeTheme;

        [Header("Rendering References")]
        public MapManager mapManager;
        public MapRenderer mapRenderer;
        public BuildingSpawner buildingSpawner;
        public AnalysisVisualizer analysisVisualizer;
        public Camera mainCamera;
        public WaterRenderer waterRenderer;

        [Header("Visual Systems")]
        public Light directionalLight;
        public PostProcessingManager postProcessingManager;
        public AmbientParticleController ambientParticles;

        public void ApplyTheme()
        {
            ApplyTheme(activeTheme);
        }

        public void ApplyTheme(MapTheme theme)
        {
            if (theme == null) return;
            activeTheme = theme;

            ApplyCamera(theme);
            ApplyGround(theme);
            ApplyRoadMaterials(theme);
            ApplyBuildingColors(theme);
            ApplyAnalysisColors(theme);
            ApplyLighting(theme);
            ApplyFog(theme);
            ApplyWater(theme);
            ApplyPostProcessing(theme);
            ApplyParticles(theme);
        }

        private void ApplyCamera(MapTheme theme)
        {
            if (mainCamera != null)
                mainCamera.backgroundColor = theme.backgroundColor;
        }

        private void ApplyGround(MapTheme theme)
        {
            if (mapManager == null || mapManager.groundMaterial == null) return;
            mapManager.groundMaterial.SetColor("_BaseColor", theme.groundColor);
            mapManager.groundMaterial.SetColor("_GridColor", theme.gridLineColor);
            mapManager.groundMaterial.SetFloat("_GridSize", theme.gridSize);
            mapManager.groundMaterial.SetFloat("_GridOpacity", theme.gridOpacity);
        }

        private void ApplyRoadMaterials(MapTheme theme)
        {
            if (mapRenderer == null) return;
            Color[] outerColors = { theme.roadOuter0, theme.roadOuter1, theme.roadOuter2 };
            Color[] innerColors = { theme.roadFill0, theme.roadFill1, theme.roadFill2 };

            for (int i = 0; i < 3; i++)
            {
                if (i < mapRenderer.roadOuterMaterials.Length && mapRenderer.roadOuterMaterials[i] != null)
                    mapRenderer.roadOuterMaterials[i].color = outerColors[i];
                if (i < mapRenderer.roadInnerMaterials.Length && mapRenderer.roadInnerMaterials[i] != null)
                    mapRenderer.roadInnerMaterials[i].color = innerColors[i];
            }
        }

        private void ApplyBuildingColors(MapTheme theme)
        {
            if (buildingSpawner == null) return;

            if (buildingSpawner.normalBuildingPrefab != null)
            {
                var r = buildingSpawner.normalBuildingPrefab.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    r.sharedMaterial.color = theme.buildingFill;
            }

            if (buildingSpawner.landmarkBuildingPrefab != null)
            {
                var r = buildingSpawner.landmarkBuildingPrefab.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    r.sharedMaterial.color = theme.buildingFillLandmark;
            }

            buildingSpawner.SetThemeColors(theme.buildingFill, theme.buildingFillLandmark);
        }

        private void ApplyAnalysisColors(MapTheme theme)
        {
            if (analysisVisualizer == null) return;
            analysisVisualizer.deadEndColor = theme.deadEndColor;
            analysisVisualizer.chokeColor = theme.chokeColor;
            analysisVisualizer.intersectionColor = theme.intersectionColor;
            analysisVisualizer.plazaColor = theme.plazaColor;
        }

        private void ApplyLighting(MapTheme theme)
        {
            if (directionalLight != null)
            {
                directionalLight.color = theme.directionalLightColor;
                directionalLight.intensity = theme.directionalLightIntensity;
                directionalLight.shadowStrength = theme.shadowStrength;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = theme.ambientColor;
        }

        private void ApplyFog(MapTheme theme)
        {
            RenderSettings.fog = theme.enableFog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogStartDistance = theme.fogStartDistance;
            RenderSettings.fogEndDistance = theme.fogEndDistance;
        }

        private void ApplyWater(MapTheme theme)
        {
            if (waterRenderer == null) return;
            if (waterRenderer.riverMaterial != null)
                waterRenderer.riverMaterial.SetColor("_BaseColor", theme.riverColor);
            if (waterRenderer.coastMaterial != null)
                waterRenderer.coastMaterial.SetColor("_BaseColor", theme.coastColor);
        }

        private void ApplyPostProcessing(MapTheme theme)
        {
            if (postProcessingManager != null)
                postProcessingManager.ApplyTheme(theme);
        }

        private void ApplyParticles(MapTheme theme)
        {
            if (ambientParticles != null)
                ambientParticles.ApplyTheme(theme);
        }
    }
}
