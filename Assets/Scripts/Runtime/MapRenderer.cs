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

        [Header("Materials (per tier)")]
        public Material[] roadOuterMaterials = new Material[3]; // tier 0,1,2
        public Material[] roadInnerMaterials = new Material[3]; // tier 0,1,2

        [Header("Rendering")]
        public int bezierSegments = 16;
        public float roadYOffset = 0.01f;

        [Header("Bridge")]
        public Material bridgePillarMaterial;

        [Header("Node Markers")]
        public GameObject intersectionMarkerPrefab;
        public GameObject plazaMarkerPrefab;

        public ElevationMap ElevMap { get; set; }

        private readonly List<GameObject> _spawnedObjects = new();

        public void Render(MapData data)
        {
            Clear();
            RenderEdges(data);
            RenderBridgePillars(data);
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

            // Buffer key: (tier, layer) → separate mesh per tier for tier-colored materials
            var buffers = new Dictionary<(int tier, int layer),
                (List<Vector3> outerV, List<int> outerT, List<Vector3> innerV, List<int> innerT)>();

            foreach (var edge in data.edges)
            {
                int ti = Mathf.Clamp(edge.tier, 0, 2);
                int layer = Mathf.Clamp(edge.layer, -1, 1);
                var key = (ti, layer);

                if (!buffers.ContainsKey(key))
                    buffers[key] = (new(), new(), new(), new());
                var buf = buffers[key];

                GenerateRoadStrip(edge, data.nodes, preset, outerWidths[ti],
                    roadYOffset, buf.outerV, buf.outerT);
                GenerateRoadStrip(edge, data.nodes, preset, innerWidths[ti],
                    roadYOffset + 0.005f, buf.innerV, buf.innerT);
            }

            string[] layerNames = { "Tunnel", "Ground", "Bridge" };
            foreach (var kvp in buffers)
            {
                int ti = kvp.Key.tier;
                int layer = kvp.Key.layer;
                string layerName = layerNames[layer + 1];
                var outerMat = ti < roadOuterMaterials.Length && roadOuterMaterials[ti] != null
                    ? roadOuterMaterials[ti] : roadOuterMaterials[0];
                var innerMat = ti < roadInnerMaterials.Length && roadInnerMaterials[ti] != null
                    ? roadInnerMaterials[ti] : roadInnerMaterials[0];

                CreateRoadMesh(kvp.Value.outerV, kvp.Value.outerT, outerMat,
                    $"Roads_{layerName}_T{ti}_Outer");
                CreateRoadMesh(kvp.Value.innerV, kvp.Value.innerT, innerMat,
                    $"Roads_{layerName}_T{ti}_Inner");
            }
        }

        private void GenerateRoadStrip(MapEdge edge, List<MapNode> nodes,
            MapPreset preset, float width, float yOffset,
            List<Vector3> verts, List<int> tris)
        {
            float halfW = width * 0.5f;
            int baseIdx = verts.Count;
            var posA = nodes[edge.nodeA].position;
            var posB = nodes[edge.nodeB].position;
            var ctrl = edge.controlPoint;

            for (int i = 0; i <= bezierSegments; i++)
            {
                float t = i / (float)bezierSegments;
                var p2d = MapGenUtils.BezierPoint(posA, ctrl, posB, t);

                // Elevation-aware Y position
                float elev = MapGenUtils.SampleEdgeElevation(edge, nodes, t, ElevMap);
                var worldPos = MapGenUtils.ToWorldPosition(p2d, elev, preset)
                    + Vector3.up * yOffset;

                // Tangent via finite difference (also elevation-aware)
                float tA = Mathf.Max(t - 0.01f, 0f);
                float tB = Mathf.Min(t + 0.01f, 1f);
                float elevA = MapGenUtils.SampleEdgeElevation(edge, nodes, tA, ElevMap);
                float elevB = MapGenUtils.SampleEdgeElevation(edge, nodes, tB, ElevMap);
                var pA = MapGenUtils.ToWorldPosition(
                    MapGenUtils.BezierPoint(posA, ctrl, posB, tA), elevA, preset);
                var pB = MapGenUtils.ToWorldPosition(
                    MapGenUtils.BezierPoint(posA, ctrl, posB, tB), elevB, preset);
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

        private void RenderBridgePillars(MapData data)
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            foreach (var edge in data.edges)
            {
                if (edge.layer != 1) continue;

                // Place a pillar at the midpoint of the bridge
                var posA = data.nodes[edge.nodeA].position;
                var posB = data.nodes[edge.nodeB].position;
                var midPoint = MapGenUtils.BezierPoint(posA, edge.controlPoint, posB, 0.5f);
                float midElev = MapGenUtils.SampleEdgeElevation(edge, data.nodes, 0.5f, ElevMap);

                if (midElev < 0.5f) continue;

                var worldPos = MapGenUtils.ToWorldPosition(midPoint, midElev * 0.5f, preset);
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"BridgePillar_{edge.nodeA}_{edge.nodeB}";
                pillar.transform.SetParent(transform);
                pillar.transform.position = worldPos;
                pillar.transform.localScale = new Vector3(0.4f, midElev * 0.5f, 0.4f);

                // Remove collider to avoid NavMesh interference
                var col = pillar.GetComponent<Collider>();
                if (col != null) Destroy(col);

                if (bridgePillarMaterial != null)
                    pillar.GetComponent<Renderer>().sharedMaterial = bridgePillarMaterial;

                _spawnedObjects.Add(pillar);
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
                    MapGenUtils.ToWorldPosition(node.position, node.elevation, preset),
                    Quaternion.identity, transform);
                _spawnedObjects.Add(go);
            }

            foreach (int idx in data.analysis.intersectionIndices)
            {
                if (intersectionMarkerPrefab == null) break;
                if (data.analysis.plazaIndices.Contains(idx)) continue;
                var node = data.nodes[idx];
                var go = Instantiate(intersectionMarkerPrefab,
                    MapGenUtils.ToWorldPosition(node.position, node.elevation, preset),
                    Quaternion.identity, transform);
                _spawnedObjects.Add(go);
            }
        }
    }
}
