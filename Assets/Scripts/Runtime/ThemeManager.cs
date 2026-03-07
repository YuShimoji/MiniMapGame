using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Applies MapTheme colors to MapRenderer, BuildingSpawner, AnalysisVisualizer, and camera.
    /// Call ApplyTheme() to update all visuals.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [Header("Theme")]
        public MapTheme activeTheme;

        [Header("References")]
        public MapManager mapManager;
        public MapRenderer mapRenderer;
        public BuildingSpawner buildingSpawner;
        public AnalysisVisualizer analysisVisualizer;
        public Camera mainCamera;

        public void ApplyTheme()
        {
            ApplyTheme(activeTheme);
        }

        public void ApplyTheme(MapTheme theme)
        {
            if (theme == null) return;
            activeTheme = theme;

            // Camera background
            if (mainCamera != null)
                mainCamera.backgroundColor = theme.backgroundColor;

            // Ground
            if (mapManager != null && mapManager.groundMaterial != null)
                mapManager.groundMaterial.color = theme.groundColor;

            // Road materials — create tier-specific materials
            if (mapRenderer != null)
            {
                ApplyRoadMaterials(theme);
            }

            // Building colors
            if (buildingSpawner != null)
            {
                ApplyBuildingColors(theme);
            }

            // Analysis visualizer colors
            if (analysisVisualizer != null)
            {
                analysisVisualizer.deadEndColor = theme.deadEndColor;
                analysisVisualizer.chokeColor = theme.chokeColor;
                analysisVisualizer.intersectionColor = theme.intersectionColor;
                analysisVisualizer.plazaColor = theme.plazaColor;
            }
        }

        private void ApplyRoadMaterials(MapTheme theme)
        {
            // Outer road material — use tier 0 color as base
            if (mapRenderer.roadOuterMaterial != null)
                mapRenderer.roadOuterMaterial.color = theme.roadOuter0;

            // Inner road material — use tier 0 fill color as base
            if (mapRenderer.roadInnerMaterial != null)
                mapRenderer.roadInnerMaterial.color = theme.roadFill0;
        }

        private void ApplyBuildingColors(MapTheme theme)
        {
            // Update prefab materials at runtime via spawned instances
            // Normal building prefab material
            if (buildingSpawner.normalBuildingPrefab != null)
            {
                var r = buildingSpawner.normalBuildingPrefab.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    r.sharedMaterial.color = theme.buildingFill;
            }

            // Landmark building prefab material
            if (buildingSpawner.landmarkBuildingPrefab != null)
            {
                var r = buildingSpawner.landmarkBuildingPrefab.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    r.sharedMaterial.color = theme.buildingFillLandmark;
            }
        }
    }
}
