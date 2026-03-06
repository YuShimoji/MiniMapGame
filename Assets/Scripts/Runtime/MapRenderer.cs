using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Renders road network using LineRenderers. 3 width tiers.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Road Width by Tier (outer casing)")]
        public float[] outerWidths = { 1.4f, 0.9f, 0.5f };
        [Header("Road Width by Tier (inner fill)")]
        public float[] innerWidths = { 0.9f, 0.55f, 0.28f };

        [Header("Materials")]
        public Material roadOuterMaterial;
        public Material roadInnerMaterial;

        [Header("Rendering")]
        public int bezierSegments = 16;
        public float roadYOffset = 0.01f;

        [Header("Node Markers")]
        public GameObject intersectionMarkerPrefab;
        public GameObject plazaMarkerPrefab;

        private readonly List<GameObject> _spawnedObjects = new();

        public void Render(MapData data)
        {
            Clear();
            RenderEdges(data);
            RenderNodeMarkers(data);
        }

        public void Clear()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedObjects.Clear();
        }

        private void RenderEdges(MapData data)
        {
            var preset = FindAnyObjectByType<MapManager>()?.activePreset;
            if (preset == null) return;

            foreach (var edge in data.edges)
            {
                var na = data.nodes[edge.nodeA];
                var nb = data.nodes[edge.nodeB];
                int ti = Mathf.Clamp(edge.tier, 0, 2);

                // Outer casing
                CreateRoadLine(na.position, nb.position, edge.controlPoint,
                    outerWidths[ti], roadOuterMaterial, preset, $"Road_Outer_T{ti}");

                // Inner fill
                CreateRoadLine(na.position, nb.position, edge.controlPoint,
                    innerWidths[ti], roadInnerMaterial, preset, $"Road_Inner_T{ti}");
            }
        }

        private void CreateRoadLine(Vector2 posA, Vector2 posB, Vector2 ctrl,
            float width, Material mat, MapPreset preset, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            _spawnedObjects.Add(go);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = bezierSegments + 1;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;

            for (int i = 0; i <= bezierSegments; i++)
            {
                float t = i / (float)bezierSegments;
                var p2d = MapGenUtils.BezierPoint(posA, ctrl, posB, t);
                lr.SetPosition(i, MapGenUtils.ToWorldPosition(p2d, preset)
                    + Vector3.up * roadYOffset);
            }
        }

        private void RenderNodeMarkers(MapData data)
        {
            var preset = FindAnyObjectByType<MapManager>()?.activePreset;
            if (preset == null) return;

            foreach (int idx in data.analysis.plazaIndices)
            {
                if (plazaMarkerPrefab == null) break;
                var node = data.nodes[idx];
                var go = Instantiate(plazaMarkerPrefab,
                    MapGenUtils.ToWorldPosition(node.position, preset),
                    Quaternion.identity, transform);
                _spawnedObjects.Add(go);
            }

            foreach (int idx in data.analysis.intersectionIndices)
            {
                if (intersectionMarkerPrefab == null) break;
                if (data.analysis.plazaIndices.Contains(idx)) continue;
                var node = data.nodes[idx];
                var go = Instantiate(intersectionMarkerPrefab,
                    MapGenUtils.ToWorldPosition(node.position, preset),
                    Quaternion.identity, transform);
                _spawnedObjects.Add(go);
            }
        }
    }
}
