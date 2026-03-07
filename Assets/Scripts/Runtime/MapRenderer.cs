using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Renders road network using batched procedural meshes. 3 width tiers.
    /// Outer casing and inner fill are each combined into a single mesh for minimal draw calls.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;

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
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            var outerVerts = new List<Vector3>();
            var outerTris = new List<int>();
            var innerVerts = new List<Vector3>();
            var innerTris = new List<int>();

            foreach (var edge in data.edges)
            {
                var na = data.nodes[edge.nodeA];
                var nb = data.nodes[edge.nodeB];
                int ti = Mathf.Clamp(edge.tier, 0, 2);

                GenerateRoadStrip(na.position, nb.position, edge.controlPoint,
                    outerWidths[ti], preset, roadYOffset, outerVerts, outerTris);
                GenerateRoadStrip(na.position, nb.position, edge.controlPoint,
                    innerWidths[ti], preset, roadYOffset + 0.005f, innerVerts, innerTris);
            }

            CreateRoadMesh(outerVerts, outerTris, roadOuterMaterial, "Roads_Outer");
            CreateRoadMesh(innerVerts, innerTris, roadInnerMaterial, "Roads_Inner");
        }

        private void GenerateRoadStrip(Vector2 posA, Vector2 posB, Vector2 ctrl,
            float width, MapPreset preset, float yOffset, List<Vector3> verts, List<int> tris)
        {
            float halfW = width * 0.5f;
            int baseIdx = verts.Count;

            for (int i = 0; i <= bezierSegments; i++)
            {
                float t = i / (float)bezierSegments;
                var p2d = MapGenUtils.BezierPoint(posA, ctrl, posB, t);
                var worldPos = MapGenUtils.ToWorldPosition(p2d, preset) + Vector3.up * yOffset;

                // Tangent via finite difference
                float tA = Mathf.Max(t - 0.01f, 0f);
                float tB = Mathf.Min(t + 0.01f, 1f);
                var pA = MapGenUtils.ToWorldPosition(
                    MapGenUtils.BezierPoint(posA, ctrl, posB, tA), preset);
                var pB = MapGenUtils.ToWorldPosition(
                    MapGenUtils.BezierPoint(posA, ctrl, posB, tB), preset);
                var tangent = (pB - pA).normalized;
                var right = Vector3.Cross(Vector3.up, tangent).normalized;

                verts.Add(worldPos - right * halfW);
                verts.Add(worldPos + right * halfW);
            }

            for (int i = 0; i < bezierSegments; i++)
            {
                int idx = baseIdx + i * 2;
                tris.Add(idx);
                tris.Add(idx + 2);
                tris.Add(idx + 1);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
                tris.Add(idx + 3);
            }
        }

        private void CreateRoadMesh(List<Vector3> verts, List<int> tris,
            Material mat, string name)
        {
            if (verts.Count == 0) return;

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            _spawnedObjects.Add(go);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = mat;
        }

        private void RenderNodeMarkers(MapData data)
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
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
