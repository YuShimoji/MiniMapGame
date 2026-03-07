using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

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

        private readonly List<GameObject> _spawnedBuildings = new();
        private Color _normalColor = new(0.22f, 0.28f, 0.38f);
        private Color _landmarkColor = new(0.10f, 0.16f, 0.25f);
        private static readonly MaterialPropertyBlock _propBlock = new();

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
                var prefab = b.isLandmark ? landmarkBuildingPrefab : normalBuildingPrefab;
                if (prefab == null) continue;

                // Sample terrain elevation for building Y position
                float terrainElev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    terrainElev = mapManager.CurrentElevationMap.Sample(b.position);

                var worldPos = MapGenUtils.ToWorldPosition(b.position, terrainElev, preset);
                float yHeight = Mathf.Max(b.floors, 1) * floorHeight;
                worldPos.y = terrainElev + yHeight * 0.5f;

                var rotation = Quaternion.Euler(0f, b.angle * Mathf.Rad2Deg, 0f);
                var go = Instantiate(prefab, worldPos, rotation, transform);
                go.transform.localScale = new Vector3(b.width, yHeight, b.height);
                go.name = b.id;

                var interaction = go.AddComponent<BuildingInteraction>();
                interaction.buildingId = b.id;
                interaction.isLandmark = b.isLandmark;

                ApplyBuildingVariation(go, b.id, b.isLandmark);
                _spawnedBuildings.Add(go);
            }
        }

        private void ApplyBuildingVariation(GameObject go, string buildingId, bool isLandmark)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;

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
