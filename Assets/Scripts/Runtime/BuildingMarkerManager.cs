using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Interior;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Manages exploration markers for all buildings on the overworld map.
    /// Computes BuildingMarkerState from ExplorationProgressManager data
    /// and drives BuildingMarkerUI instances.
    /// SP-020 Layer 2.
    /// </summary>
    public class BuildingMarkerManager : MonoBehaviour
    {
        [Header("References")]
        public BuildingSpawner buildingSpawner;
        public ExplorationProgressManager progressManager;

        [Header("Marker Prefab")]
        [Tooltip("Prefab with BuildingMarkerUI component. Spawned above each landmark building.")]
        public GameObject markerPrefab;

        [Header("Map")]
        public MapManager mapManager;

        private readonly Dictionary<string, BuildingMarkerUI> _markers = new();
        private readonly HashSet<string> _discoveredBuildings = new();

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
        }

        private void OnMapGenerated(MapData _)
        {
            // Delay one frame to ensure all buildings are spawned
            StartCoroutine(InitializeNextFrame());
        }

        private System.Collections.IEnumerator InitializeNextFrame()
        {
            yield return null;
            InitializeMarkers();
        }

        /// <summary>
        /// Create markers for all landmark buildings after map generation.
        /// </summary>
        public void InitializeMarkers()
        {
            ClearMarkers();

            if (buildingSpawner == null || markerPrefab == null) return;

            foreach (var go in buildingSpawner.SpawnedBuildings)
            {
                if (go == null) continue;
                var interaction = go.GetComponent<BuildingInteraction>();
                if (interaction == null || !interaction.isLandmark) continue;

                var markerGo = Instantiate(markerPrefab, go.transform);
                markerGo.name = "BuildingMarker";

                // Position above building
                float topY = GetBuildingTopY(go);
                markerGo.transform.localPosition = new Vector3(0f, topY + 0.6f, 0f);

                var ui = markerGo.GetComponent<BuildingMarkerUI>();
                if (ui != null)
                {
                    ui.Initialize(interaction.buildingId, interaction.context.category,
                        interaction.context.floors);
                    _markers[interaction.buildingId] = ui;
                }
            }

            // Restore state from progress manager
            RefreshAllStates();
        }

        /// <summary>
        /// Mark a building as discovered (player approached within highlight range).
        /// Called from BuildingInteraction.SetHighlight(true).
        /// </summary>
        public void OnBuildingDiscovered(string buildingId)
        {
            if (_discoveredBuildings.Add(buildingId))
                UpdateMarkerState(buildingId);
        }

        /// <summary>
        /// Called when player enters a building.
        /// </summary>
        public void OnBuildingEntered(string buildingId)
        {
            UpdateMarkerState(buildingId);
        }

        /// <summary>
        /// Called when exploration progress changes (discovery collected, floor visited, etc.).
        /// </summary>
        public void OnProgressChanged(string buildingId)
        {
            UpdateMarkerState(buildingId);
        }

        /// <summary>
        /// Refresh all marker states from current progress data.
        /// Called after save/load.
        /// </summary>
        public void RefreshAllStates()
        {
            if (progressManager == null) return;

            // Restore discovered set from progress records
            foreach (var kvp in progressManager.GetAllRecords())
            {
                _discoveredBuildings.Add(kvp.Key);
            }

            foreach (var kvp in _markers)
            {
                var state = ComputeState(kvp.Key);
                kvp.Value.SetState(state);
            }
        }

        /// <summary>
        /// Compute the current marker state for a building.
        /// </summary>
        public BuildingMarkerState ComputeState(string buildingId)
        {
            if (progressManager == null)
                return _discoveredBuildings.Contains(buildingId)
                    ? BuildingMarkerState.Discovered
                    : BuildingMarkerState.Unknown;

            var record = progressManager.GetRecord(buildingId);

            if (record == null || !record.hasEntered)
            {
                return _discoveredBuildings.Contains(buildingId)
                    ? BuildingMarkerState.Discovered
                    : BuildingMarkerState.Unknown;
            }

            if (record.IsComplete)
                return BuildingMarkerState.Complete;

            // Has entered but not complete
            if (record.CollectedCount > 0 || record.VisitedFloorCount > 1)
                return BuildingMarkerState.InProgress;

            return BuildingMarkerState.Entered;
        }

        private void UpdateMarkerState(string buildingId)
        {
            if (!_markers.TryGetValue(buildingId, out var ui)) return;
            var state = ComputeState(buildingId);
            ui.SetState(state);
        }

        private static float GetBuildingTopY(GameObject building)
        {
            float topY = 0f;
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.gameObject.name == "BuildingMarker") continue;
                float rTop = r.bounds.max.y - building.transform.position.y;
                if (rTop > topY) topY = rTop;
            }
            return topY;
        }

        public void ClearMarkers()
        {
            foreach (var kvp in _markers)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _markers.Clear();
            _discoveredBuildings.Clear();
        }
    }
}
