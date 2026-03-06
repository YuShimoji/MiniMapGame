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

        [Header("Building Height")]
        public float normalHeight = 2f;
        public float landmarkHeight = 4f;

        private readonly List<GameObject> _spawnedBuildings = new();

        public void Spawn(MapData data)
        {
            Clear();
            var preset = FindAnyObjectByType<MapManager>()?.activePreset;
            if (preset == null) return;

            foreach (var b in data.buildings)
            {
                var prefab = b.isLandmark ? landmarkBuildingPrefab : normalBuildingPrefab;
                if (prefab == null) continue;

                var worldPos = MapGenUtils.ToWorldPosition(b.position, preset);
                float yHeight = b.isLandmark ? landmarkHeight : normalHeight;
                worldPos.y = yHeight * 0.5f;

                var rotation = Quaternion.Euler(0f, b.angle * Mathf.Rad2Deg, 0f);
                var go = Instantiate(prefab, worldPos, rotation, transform);
                go.transform.localScale = new Vector3(b.width, yHeight, b.height);
                go.name = b.id;

                var interaction = go.AddComponent<BuildingInteraction>();
                interaction.buildingId = b.id;
                interaction.isLandmark = b.isLandmark;

                _spawnedBuildings.Add(go);
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
