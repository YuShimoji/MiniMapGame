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

        private void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += HandleMapGenerated;
        }

        private void Start()
        {
            ApplyTheme(activeTheme);
        }

        private void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= HandleMapGenerated;
        }

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
            if (mapManager == null) return;

            ApplyGroundToMaterial(mapManager.groundMaterial, theme);

            if (mapManager.groundPlane == null) return;

            var mr = mapManager.groundPlane.GetComponent<MeshRenderer>();
            if (mr == null) return;

            var runtimeMat = mr.sharedMaterial;
            if (runtimeMat != null && runtimeMat != mapManager.groundMaterial)
                ApplyGroundToMaterial(runtimeMat, theme);
        }

        private static void ApplyGroundToMaterial(Material mat, MapTheme theme)
        {
            if (mat == null) return;

            mat.SetColor("_BaseColor", theme.groundColor);
            mat.SetColor("_GridColor", theme.gridLineColor);
            mat.SetFloat("_GridSize", theme.gridSize);
            mat.SetFloat("_GridOpacity", theme.gridOpacity);

            mat.SetColor("_MidColor", theme.groundMidColor);
            mat.SetColor("_HighColor", theme.groundHighColor);
            mat.SetColor("_SlopeColor", theme.groundSlopeColor);
            mat.SetColor("_MoistureTint", theme.groundMoistureTint);
            mat.SetColor("_RoadTint", theme.groundRoadTint);
            mat.SetColor("_BuildingTint", theme.groundBuildingTint);
            mat.SetColor("_ContourColor", theme.groundContourColor);
        }

        private void HandleMapGenerated(MapData _)
        {
            ApplyTheme(activeTheme);
        }

        private void ApplyRoadMaterials(MapTheme theme)
        {
            if (mapRenderer == null) return;

            Color[] baseColors = { theme.roadFill0, theme.roadFill1, theme.roadFill2 };
            Color[] casingColors = { theme.roadOuter0, theme.roadOuter1, theme.roadOuter2 };

            // New Road.shader materials
            if (mapRenderer.roadMaterials != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < mapRenderer.roadMaterials.Length && mapRenderer.roadMaterials[i] != null)
                    {
                        var mat = mapRenderer.roadMaterials[i];
                        mat.SetColor("_BaseColor", baseColors[i]);
                        mat.SetColor("_CasingColor", casingColors[i]);
                        mat.SetColor("_MarkingColor", theme.markingColor);
                        mat.SetColor("_CurbColor", theme.curbColor);
                    }
                }
            }

            // Legacy materials
            if (mapRenderer.roadOuterMaterials != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < mapRenderer.roadOuterMaterials.Length && mapRenderer.roadOuterMaterials[i] != null)
                        mapRenderer.roadOuterMaterials[i].color = casingColors[i];
                }
            }
            if (mapRenderer.roadInnerMaterials != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < mapRenderer.roadInnerMaterials.Length && mapRenderer.roadInnerMaterials[i] != null)
                        mapRenderer.roadInnerMaterials[i].color = baseColors[i];
                }
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
            if (waterRenderer == null || waterRenderer.waterMaterial == null) return;
            var mat = waterRenderer.waterMaterial;
            mat.SetColor("_BaseColor", theme.riverColor);
            mat.SetColor("_ShallowColor", theme.shallowWaterColor);
            mat.SetColor("_DeepColor", theme.deepWaterColor);
            mat.SetColor("_FoamColor", theme.foamColor);
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
