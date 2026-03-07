using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;
using MiniMapGame.Player;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Instantiates decoration objects from MapDecoration data.
    /// Supports LOD-based visibility toggling via camera distance.
    /// </summary>
    public class DecorationSpawner : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public CameraController cameraController;

        [Header("Colors")]
        public Color streetLightColor = new(0.9f, 0.85f, 0.5f);
        public Color treeCanopyColor = new(0.12f, 0.28f, 0.10f);
        public Color treeTrunkColor = new(0.35f, 0.22f, 0.12f);
        public Color benchColor = new(0.4f, 0.3f, 0.2f);
        public Color bollardColor = new(0.45f, 0.45f, 0.5f);

        [Header("LOD Thresholds")]
        public float lodMediumDistance = 30f;
        public float lodCloseDistance = 12f;

        private readonly Dictionary<int, List<GameObject>> _lodGroups = new()
        {
            [0] = new(),
            [1] = new(),
            [2] = new()
        };

        private int _currentLOD = -1;
        private static readonly MaterialPropertyBlock _propBlock = new();

        public void Spawn(MapData data)
        {
            Clear();
            if (data.decorations == null) return;

            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            foreach (var dec in data.decorations)
            {
                float elev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    elev = mapManager.CurrentElevationMap.Sample(dec.position);

                var worldPos = MapGenUtils.ToWorldPosition(dec.position, elev, preset);
                var go = CreateDecoration(dec, worldPos);

                if (go != null)
                {
                    int lod = Mathf.Clamp(dec.lodLevel, 0, 2);
                    _lodGroups[lod].Add(go);
                }
            }

            _currentLOD = -1; // Force update
        }

        public void Clear()
        {
            foreach (var kvp in _lodGroups)
            {
                foreach (var obj in kvp.Value)
                    if (obj != null) Destroy(obj);
                kvp.Value.Clear();
            }
            _currentLOD = -1;
        }

        void LateUpdate()
        {
            if (cameraController == null) return;

            float dist = cameraController.CurrentDistance;
            int newLOD;
            if (dist < lodCloseDistance) newLOD = 2;      // Show all
            else if (dist < lodMediumDistance) newLOD = 1; // LOD 0+1 only
            else newLOD = 0;                               // LOD 0 only

            if (newLOD != _currentLOD)
            {
                _currentLOD = newLOD;
                UpdateLODVisibility();
            }
        }

        private void UpdateLODVisibility()
        {
            // LOD 0 = always visible
            SetGroupActive(0, true);
            // LOD 1 = visible when currentLOD >= 1
            SetGroupActive(1, _currentLOD >= 1);
            // LOD 2 = visible only when currentLOD >= 2
            SetGroupActive(2, _currentLOD >= 2);
        }

        private void SetGroupActive(int level, bool active)
        {
            if (!_lodGroups.TryGetValue(level, out var list)) return;
            foreach (var obj in list)
                if (obj != null) obj.SetActive(active);
        }

        private GameObject CreateDecoration(MapDecoration dec, Vector3 worldPos)
        {
            return dec.type switch
            {
                DecorationType.StreetLight => CreateStreetLight(worldPos, dec),
                DecorationType.Tree => CreateTree(worldPos, dec),
                DecorationType.Bench => CreateBench(worldPos, dec),
                DecorationType.Bollard => CreateBollard(worldPos, dec),
                _ => null
            };
        }

        private GameObject CreateStreetLight(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_Light");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform);
            pole.transform.localPosition = new Vector3(0, 2f, 0);
            pole.transform.localScale = new Vector3(0.15f, 2f, 0.15f);
            RemoveCollider(pole);
            ApplyColor(pole, bollardColor);

            // Lamp head
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.transform.SetParent(root.transform);
            lamp.transform.localPosition = new Vector3(0, 4.2f, 0);
            lamp.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
            RemoveCollider(lamp);
            ApplyColor(lamp, streetLightColor);

            return root;
        }

        private GameObject CreateTree(Vector3 pos, MapDecoration dec)
        {
            var root = new GameObject("Deco_Tree");
            root.transform.SetParent(transform);
            root.transform.position = pos;

            float s = dec.scale;

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform);
            trunk.transform.localPosition = new Vector3(0, s * 0.6f, 0);
            trunk.transform.localScale = new Vector3(0.2f * s, s * 0.6f, 0.2f * s);
            RemoveCollider(trunk);
            ApplyColor(trunk, treeTrunkColor);

            // Canopy
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(root.transform);
            canopy.transform.localPosition = new Vector3(0, s * 1.5f, 0);
            canopy.transform.localScale = Vector3.one * s * 1.2f;
            RemoveCollider(canopy);
            ApplyColor(canopy, treeCanopyColor);

            return root;
        }

        private GameObject CreateBench(Vector3 pos, MapDecoration dec)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Deco_Bench";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * 0.25f;
            go.transform.rotation = Quaternion.Euler(0, dec.angle * Mathf.Rad2Deg, 0);
            go.transform.localScale = new Vector3(2f, 0.5f, 0.6f);
            RemoveCollider(go);
            ApplyColor(go, benchColor);

            return go;
        }

        private GameObject CreateBollard(Vector3 pos, MapDecoration dec)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Deco_Bollard";
            go.transform.SetParent(transform);
            go.transform.position = pos + Vector3.up * 0.4f;
            go.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
            RemoveCollider(go);
            ApplyColor(go, bollardColor);

            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            _propBlock.SetColor("_BaseColor", color);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
