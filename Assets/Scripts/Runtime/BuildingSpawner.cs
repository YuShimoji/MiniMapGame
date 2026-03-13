using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;
using MiniMapGame.Interior;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Instantiates building prefabs from MapBuilding data.
    /// </summary>
    public class BuildingSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject normalBuildingPrefab;
        public GameObject landmarkBuildingPrefab;

        [Header("References")]
        public MapManager mapManager;

        [Header("Building Height")]
        public float floorHeight = 1.2f;

        [Header("Roof Fade")]
        [Tooltip("Material using MiniMapGame/BuildingFade shader. If null, buildings use prefab material without roof fade.")]
        public Material buildingFadeMaterial;

        private readonly List<GameObject> _spawnedBuildings = new();
        private Color _normalColor = new(0.22f, 0.28f, 0.38f);
        private Color _landmarkColor = new(0.10f, 0.16f, 0.25f);
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        public void SetThemeColors(Color normal, Color landmark)
        {
            _normalColor = normal;
            _landmarkColor = landmark;
            RefreshBuildingColors();
        }

        public void Spawn(MapData data)
        {
            Clear();
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            foreach (var b in data.buildings)
            {
                // Sample terrain elevation for building Y position
                float terrainElev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    terrainElev = mapManager.CurrentElevationMap.Sample(b.position);

                // Height variation: ±7.5% per building based on ID hash
                int bHash = b.id.GetHashCode();
                float heightVar = 1f + ((bHash & 0x1FF) / 511f - 0.5f) * 0.15f;
                float yHeight = Mathf.Max(b.floors, 1) * floorHeight * heightVar;
                var rotation = Quaternion.Euler(0f, b.angle * Mathf.Rad2Deg, 0f);

                GameObject go;
                switch (b.shapeType)
                {
                    case 1: // L-shape: two boxes
                        go = CreateLShape(b, terrainElev, yHeight, rotation, preset);
                        break;
                    case 2: // Cylinder
                        go = CreateCylinder(b, terrainElev, yHeight, rotation, preset);
                        break;
                    case 3: // Stepped: base + taller tower
                        go = CreateStepped(b, terrainElev, yHeight, rotation, preset);
                        break;
                    default: // 0 = Box (original)
                        go = CreateBox(b, terrainElev, yHeight, rotation, preset);
                        break;
                }

                if (go == null) continue;
                go.name = b.id;

                var interaction = go.AddComponent<BuildingInteraction>();
                interaction.buildingId = b.id;
                interaction.isLandmark = b.isLandmark;

                // Classify building for v2 interior generation
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                {
                    interaction.context = BuildingClassifier.Classify(
                        b, preset, data.terrain, mapManager.CurrentElevationMap);
                }

                // Apply roof-fade material if available
                if (buildingFadeMaterial != null)
                    ApplyFadeMaterial(go);

                ApplyBuildingVariation(go, b.id, b.isLandmark);
                _spawnedBuildings.Add(go);
            }
        }

        private GameObject CreateBox(MapBuilding b, float terrainElev, float yHeight,
            Quaternion rotation, MapPreset preset)
        {
            var prefab = b.isLandmark ? landmarkBuildingPrefab : normalBuildingPrefab;
            if (prefab == null) return null;
            var worldPos = MapGenUtils.ToWorldPosition(b.position, terrainElev, preset);
            worldPos.y = terrainElev + yHeight * 0.5f;
            var go = Instantiate(prefab, worldPos, rotation, transform);
            go.transform.localScale = new Vector3(b.width, yHeight, b.height);
            return go;
        }

        private GameObject CreateLShape(MapBuilding b, float terrainElev, float yHeight,
            Quaternion rotation, MapPreset preset)
        {
            var prefab = b.isLandmark ? landmarkBuildingPrefab : normalBuildingPrefab;
            if (prefab == null) return null;
            var worldPos = MapGenUtils.ToWorldPosition(b.position, terrainElev, preset);

            var root = new GameObject();
            root.transform.SetParent(transform);
            root.transform.position = worldPos;
            root.transform.rotation = rotation;

            // Main block
            var main = Instantiate(prefab, root.transform);
            main.transform.localPosition = new Vector3(0, yHeight * 0.5f, 0);
            main.transform.localScale = new Vector3(b.width, yHeight, b.height * 0.6f);
            main.transform.localRotation = Quaternion.identity;

            // Wing block
            var wing = Instantiate(prefab, root.transform);
            wing.transform.localPosition = new Vector3(b.width * 0.25f, yHeight * 0.35f, b.height * 0.2f);
            wing.transform.localScale = new Vector3(b.width * 0.5f, yHeight * 0.7f, b.height * 0.5f);
            wing.transform.localRotation = Quaternion.identity;

            return root;
        }

        private GameObject CreateCylinder(MapBuilding b, float terrainElev, float yHeight,
            Quaternion rotation, MapPreset preset)
        {
            var worldPos = MapGenUtils.ToWorldPosition(b.position, terrainElev, preset);
            worldPos.y = terrainElev + yHeight * 0.5f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(transform);
            go.transform.position = worldPos;
            go.transform.rotation = rotation;
            float radius = Mathf.Min(b.width, b.height) * 0.5f;
            go.transform.localScale = new Vector3(radius, yHeight * 0.5f, radius);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }

        private GameObject CreateStepped(MapBuilding b, float terrainElev, float yHeight,
            Quaternion rotation, MapPreset preset)
        {
            var prefab = b.isLandmark ? landmarkBuildingPrefab : normalBuildingPrefab;
            if (prefab == null) return null;
            var worldPos = MapGenUtils.ToWorldPosition(b.position, terrainElev, preset);

            var root = new GameObject();
            root.transform.SetParent(transform);
            root.transform.position = worldPos;
            root.transform.rotation = rotation;

            // Base (wide, shorter)
            float baseH = yHeight * 0.5f;
            var basePart = Instantiate(prefab, root.transform);
            basePart.transform.localPosition = new Vector3(0, baseH * 0.5f, 0);
            basePart.transform.localScale = new Vector3(b.width, baseH, b.height);
            basePart.transform.localRotation = Quaternion.identity;

            // Tower (narrower, taller)
            float towerH = yHeight;
            var tower = Instantiate(prefab, root.transform);
            tower.transform.localPosition = new Vector3(0, towerH * 0.5f, 0);
            tower.transform.localScale = new Vector3(b.width * 0.55f, towerH, b.height * 0.55f);
            tower.transform.localRotation = Quaternion.identity;

            return root;
        }

        private void ApplyFadeMaterial(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.sharedMaterial = buildingFadeMaterial;
        }

        private void ApplyBuildingVariation(GameObject go, string buildingId, bool isLandmark)
        {
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();

            int hash = buildingId.GetHashCode();
            float rv = ((hash & 0xFF) / 255f - 0.5f) * 0.06f;
            float gv = (((hash >> 8) & 0xFF) / 255f - 0.5f) * 0.06f;
            float bv = (((hash >> 16) & 0xFF) / 255f - 0.5f) * 0.06f;

            Color baseColor = isLandmark ? _landmarkColor : _normalColor;
            _propBlock.SetColor("_BaseColor", new Color(
                Mathf.Clamp01(baseColor.r + rv),
                Mathf.Clamp01(baseColor.g + gv),
                Mathf.Clamp01(baseColor.b + bv),
                baseColor.a));

            if (isLandmark)
                _propBlock.SetColor("_EmissionColor", _landmarkColor * 0.15f);
            else
                _propBlock.SetColor("_EmissionColor", Color.black);

            // Per-building roughness variation (smoothness 0.3–0.6)
            float roughness = 0.4f + (((hash >> 24) & 0xFF) / 255f) * 0.3f;
            _propBlock.SetFloat("_Smoothness", 1f - roughness);

            // Apply to self and all children with renderers
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.SetPropertyBlock(_propBlock);
        }

        private void RefreshBuildingColors()
        {
            foreach (var go in _spawnedBuildings)
            {
                if (go == null) continue;
                var interaction = go.GetComponent<BuildingInteraction>();
                if (interaction != null)
                    ApplyBuildingVariation(go, interaction.buildingId, interaction.isLandmark);
            }
        }

        // ===== Exploration markers =====

        /// <summary>
        /// Add or update a small sphere marker above an explored building.
        /// Green = explored, Gold = complete.
        /// </summary>
        public void SetExplorationMarker(string buildingId, bool isComplete)
        {
            foreach (var go in _spawnedBuildings)
            {
                if (go == null) continue;
                var interaction = go.GetComponent<BuildingInteraction>();
                if (interaction == null || interaction.buildingId != buildingId) continue;
                CreateOrUpdateMarker(go, isComplete);
                break;
            }
        }

        /// <summary>
        /// Refresh all exploration markers from saved records. Called after map load.
        /// </summary>
        public void RefreshAllExplorationMarkers(ExplorationProgressManager progress)
        {
            if (progress == null) return;
            foreach (var go in _spawnedBuildings)
            {
                if (go == null) continue;
                var interaction = go.GetComponent<BuildingInteraction>();
                if (interaction == null) continue;
                var record = progress.GetRecord(interaction.buildingId);
                if (record != null && record.hasEntered)
                    CreateOrUpdateMarker(go, record.IsComplete);
            }
        }

        private void CreateOrUpdateMarker(GameObject building, bool isComplete)
        {
            const string markerName = "ExploreMarker";
            var existing = building.transform.Find(markerName);
            if (existing != null) Destroy(existing.gameObject);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName;
            marker.transform.SetParent(building.transform, false);

            // Place above building using renderer bounds
            float topY = 0f;
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                float rTop = r.bounds.max.y - building.transform.position.y;
                if (rTop > topY) topY = rTop;
            }
            marker.transform.localPosition = new Vector3(0f, topY + 0.4f, 0f);
            marker.transform.localScale = Vector3.one * 0.25f;

            var col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            var mr = marker.GetComponent<Renderer>();
            Color markerColor = isComplete
                ? new Color(0.9f, 0.75f, 0.2f)
                : new Color(0.2f, 0.7f, 0.3f, 0.85f);
            _propBlock.SetColor("_BaseColor", markerColor);
            _propBlock.SetColor("_EmissionColor", isComplete ? markerColor * 0.3f : Color.black);
            mr.SetPropertyBlock(_propBlock);
        }

        public void Clear()
        {
            foreach (var obj in _spawnedBuildings)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedBuildings.Clear();
        }
    }
}
