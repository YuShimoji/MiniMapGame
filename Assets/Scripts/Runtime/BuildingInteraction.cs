using UnityEngine;
using MiniMapGame.Interior;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Attached to buildings. Triggers interior map generation on interaction.
    /// Holds InteriorBuildingContext for v2 context-aware generation.
    /// Provides proximity highlight via MaterialPropertyBlock color shift.
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        [HideInInspector] public string buildingId;
        [HideInInspector] public bool isLandmark;
        [HideInInspector] public InteriorBuildingContext context;

        private static readonly Color HighlightEmission = new(0.18f, 0.25f, 0.4f);
        private const float HighlightBrighten = 0.12f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private bool _highlighted;

        public string GetInteractionMessage()
        {
            if (!isLandmark) return buildingId;

            string cat = context.category.ToString();
            int floors = context.floors;

            // Check exploration progress for richer hint
            var progress = Object.FindAnyObjectByType<Interior.ExplorationProgressManager>();
            if (progress != null)
            {
                var record = progress.GetRecord(buildingId);
                if (record != null && record.hasEntered)
                    return $"[E] {cat} - {record.VisitedFloorCount}/{floors}F, {record.CollectedCount}/{record.totalDiscoveries} Items";
            }

            return $"[E] {cat} - {floors} Floors";
        }

        public void Interact()
        {
            if (!isLandmark) return;

            var controller = Object.FindAnyObjectByType<InteriorController>();
            if (controller != null)
            {
                controller.EnterBuilding(this);
            }
            else
            {
                Debug.LogWarning("[BuildingInteraction] InteriorController not found in scene.");
            }
        }

        /// <summary>
        /// Brightens the building color to indicate it is interactable.
        /// </summary>
        public void SetHighlight(bool on)
        {
            if (!isLandmark || on == _highlighted) return;
            _highlighted = on;

            // Notify marker manager that this building was discovered (approached)
            if (on)
            {
                var markerMgr = Object.FindAnyObjectByType<BuildingMarkerManager>();
                markerMgr?.OnBuildingDiscovered(buildingId);
            }

            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>();
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();

            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(_propBlock);

                if (on)
                {
                    // Read current base color, brighten it
                    Color baseColor = _propBlock.GetColor("_BaseColor");
                    if (baseColor == default)
                        baseColor = new Color(0.10f, 0.16f, 0.25f);
                    _propBlock.SetColor("_BaseColor", new Color(
                        Mathf.Clamp01(baseColor.r + HighlightBrighten),
                        Mathf.Clamp01(baseColor.g + HighlightBrighten),
                        Mathf.Clamp01(baseColor.b + HighlightBrighten),
                        baseColor.a));
                    _propBlock.SetColor("_EmissionColor", HighlightEmission);
                }
                else
                {
                    // Restore: read brightened color, subtract
                    Color cur = _propBlock.GetColor("_BaseColor");
                    _propBlock.SetColor("_BaseColor", new Color(
                        Mathf.Clamp01(cur.r - HighlightBrighten),
                        Mathf.Clamp01(cur.g - HighlightBrighten),
                        Mathf.Clamp01(cur.b - HighlightBrighten),
                        cur.a));
                    // Restore landmark emission (set by BuildingSpawner)
                    Color landmarkBase = new(0.10f, 0.16f, 0.25f);
                    _propBlock.SetColor("_EmissionColor", landmarkBase * 0.15f);
                }

                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
