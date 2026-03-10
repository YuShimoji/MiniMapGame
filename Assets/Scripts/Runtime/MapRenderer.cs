using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Renders road network using batched procedural meshes with UV-driven Road.shader.
    /// Single mesh per (tier, layer) with procedural lane markings and surface detail.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;

        [Header("Road Materials (per tier, uses Road.shader)")]
        public Material[] roadMaterials = new Material[3];

        [Header("Rendering")]
        public int bezierSegments = 16;
        public float roadYOffset = 0.01f;

        [Header("Bridge")]
        public Material bridgePillarMaterial;

        [Header("Node Markers")]
        public GameObject intersectionMarkerPrefab;
        public GameObject plazaMarkerPrefab;

        // Legacy fields — kept for serialization safety
        [HideInInspector] public float[] outerWidths = { 1.4f, 0.9f, 0.5f };
        [HideInInspector] public float[] innerWidths = { 0.9f, 0.55f, 0.28f };
        [HideInInspector] public Material[] roadOuterMaterials = new Material[3];
        [HideInInspector] public Material[] roadInnerMaterials = new Material[3];

        [Header("Intersections")]
        [Range(0f, 2f)] public float intersectionRadiusFactor = 0.7f;
        public int intersectionSegments = 12;

        public ElevationMap ElevMap { get; set; }

        private readonly List<GameObject> _spawnedObjects = new();
        private RoadProfile _fallbackProfile;
        private Material[] _intersectionMaterials;

        public void Render(MapData data)
        {
            Clear();
            RenderEdges(data);
            RenderIntersections(data);
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

            if (_intersectionMaterials != null)
            {
                foreach (var mat in _intersectionMaterials)
                    if (mat != null) Destroy(mat);
                _intersectionMaterials = null;
            }
        }

        private RoadProfile GetProfile()
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset != null && preset.roadProfile != null)
                return preset.roadProfile;

            if (_fallbackProfile == null)
                _fallbackProfile = RoadProfile.CreateDefaultFallback();
            return _fallbackProfile;
        }

        private void RenderEdges(MapData data)
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            var profile = GetProfile();

            var buffers = new Dictionary<(int tier, int layer),
                (List<Vector3> verts, List<Vector2> uvs, List<int> tris)>();

            foreach (var edge in data.edges)
            {
                int ti = Mathf.Clamp(edge.tier, 0, 2);
                int layer = Mathf.Clamp(edge.layer, -1, 1);
                var key = (ti, layer);

                if (!buffers.ContainsKey(key))
                    buffers[key] = (new(), new(), new());
                var buf = buffers[key];

                float width = profile.tiers[ti].TotalWidth;
                GenerateRoadStrip(edge, data.nodes, preset, width,
                    roadYOffset, buf.verts, buf.uvs, buf.tris);
            }

            string[] layerNames = { "Tunnel", "Ground", "Bridge" };
            foreach (var kvp in buffers)
            {
                int ti = kvp.Key.tier;
                int layer = kvp.Key.layer;
                string layerName = layerNames[layer + 1];

                var mat = GetRoadMaterial(ti);
                ApplyProfileToMaterial(mat, profile.tiers[ti]);

                CreateRoadMesh(kvp.Value.verts, kvp.Value.uvs,
                    kvp.Value.tris, mat, $"Roads_{layerName}_T{ti}");
            }
        }

        private Material GetRoadMaterial(int tier)
        {
            // New materials first
            if (roadMaterials != null && tier < roadMaterials.Length && roadMaterials[tier] != null)
                return roadMaterials[tier];

            // Legacy fallback: use outer material if new ones aren't set
            if (roadOuterMaterials != null && tier < roadOuterMaterials.Length && roadOuterMaterials[tier] != null)
                return roadOuterMaterials[tier];

            // Ultimate fallback
            if (roadMaterials != null && roadMaterials.Length > 0 && roadMaterials[0] != null)
                return roadMaterials[0];

            return null;
        }

        private void GenerateRoadStrip(MapEdge edge, List<MapNode> nodes,
            MapPreset preset, float width, float yOffset,
            List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            float halfW = width * 0.5f;
            int baseIdx = verts.Count;
            var posA = nodes[edge.nodeA].position;
            var posB = nodes[edge.nodeB].position;
            var ctrl = edge.controlPoint;

            float accumulatedDist = 0f;
            Vector3 prevWorldPos = Vector3.zero;

            for (int i = 0; i <= bezierSegments; i++)
            {
                float t = i / (float)bezierSegments;
                var p2d = MapGenUtils.BezierPoint(posA, ctrl, posB, t);

                float elev = MapGenUtils.SampleEdgeElevation(edge, nodes, t, ElevMap);
                var worldPos = MapGenUtils.ToWorldPosition(p2d, elev, preset)
                    + Vector3.up * yOffset;

                if (i > 0)
                    accumulatedDist += Vector3.Distance(prevWorldPos, worldPos);
                prevWorldPos = worldPos;

                // Tangent via finite difference
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

                // UV: x=0 left edge, x=1 right edge; y=cumulative distance
                uvs.Add(new Vector2(0f, accumulatedDist));
                uvs.Add(new Vector2(1f, accumulatedDist));
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

        private static void ApplyProfileToMaterial(Material mat, RoadProfile.RoadTierConfig config)
        {
            if (mat == null) return;

            float total = config.TotalWidth;
            if (total <= 0f) return;

            mat.SetFloat("_CurbRatio", config.curbWidth / total);
            mat.SetFloat("_ShoulderRatio", config.shoulderWidth / total);
            mat.SetFloat("_LaneCount", config.laneCount);
            mat.SetFloat("_MarkingWidthRatio", config.markingWidth / total);
            mat.SetFloat("_HasCenterLine", config.hasCenterLine ? 1f : 0f);
            mat.SetFloat("_CenterLineSolid", config.centerLineSolid ? 1f : 0f);
            mat.SetFloat("_HasLaneDividers", config.hasLaneDividers ? 1f : 0f);
            mat.SetFloat("_HasEdgeLines", config.hasEdgeLines ? 1f : 0f);
            mat.SetFloat("_DashLength", config.dashLength);
            mat.SetFloat("_DashGap", config.dashGap);
            mat.SetFloat("_Roughness", config.roughness);
            mat.SetFloat("_Wear", config.wear);
            mat.SetFloat("_CrackDensity", config.crackDensity);
        }

        private void RenderIntersections(MapData data)
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null || intersectionRadiusFactor <= 0f) return;

            var profile = GetProfile();

            // Collect best (lowest) tier per node
            var nodeBestTier = new Dictionary<int, int>();
            var nodeDegree = new Dictionary<int, int>();
            foreach (var edge in data.edges)
            {
                int ti = Mathf.Clamp(edge.tier, 0, 2);
                foreach (int ni in new[] { edge.nodeA, edge.nodeB })
                {
                    nodeDegree.TryAdd(ni, 0);
                    nodeDegree[ni]++;
                    if (!nodeBestTier.ContainsKey(ni) || ti < nodeBestTier[ni])
                        nodeBestTier[ni] = ti;
                }
            }

            // Batched buffers per tier
            var buffers = new Dictionary<int,
                (List<Vector3> verts, List<Vector2> uvs, List<int> tris)>();

            foreach (var kvp in nodeDegree)
            {
                if (kvp.Value < 3) continue;

                int nodeIdx = kvp.Key;
                int bestTier = nodeBestTier[nodeIdx];
                var config = profile.tiers[Mathf.Clamp(bestTier, 0, 2)];
                float radius = config.TotalWidth * intersectionRadiusFactor;

                if (!buffers.ContainsKey(bestTier))
                    buffers[bestTier] = (new(), new(), new());
                var buf = buffers[bestTier];

                var node = data.nodes[nodeIdx];
                var center = MapGenUtils.ToWorldPosition(node.position, node.elevation, preset)
                    + Vector3.up * (roadYOffset + 0.008f);

                GenerateDisc(center, radius, buf.verts, buf.uvs, buf.tris);
            }

            foreach (var kvp in buffers)
            {
                var mat = GetIntersectionMaterial(kvp.Key);
                CreateRoadMesh(kvp.Value.verts, kvp.Value.uvs,
                    kvp.Value.tris, mat, $"Intersections_T{kvp.Key}");
            }
        }

        private void GenerateDisc(Vector3 center, float radius,
            List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            int centerIdx = verts.Count;
            verts.Add(center);
            uvs.Add(new Vector2(0.5f, 0f));

            for (int i = 0; i <= intersectionSegments; i++)
            {
                float angle = (i / (float)intersectionSegments) * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                verts.Add(center + offset);
                uvs.Add(new Vector2(0.5f, 0f));

                if (i > 0)
                {
                    tris.Add(centerIdx);
                    tris.Add(centerIdx + i);
                    tris.Add(centerIdx + i + 1);
                }
            }
        }

        private Material GetIntersectionMaterial(int tier)
        {
            if (_intersectionMaterials == null)
                _intersectionMaterials = new Material[3];

            if (_intersectionMaterials[tier] == null)
            {
                var roadMat = GetRoadMaterial(tier);
                if (roadMat == null) return null;

                _intersectionMaterials[tier] = new Material(roadMat);
                _intersectionMaterials[tier].SetFloat("_HasCenterLine", 0f);
                _intersectionMaterials[tier].SetFloat("_HasLaneDividers", 0f);
                _intersectionMaterials[tier].SetFloat("_HasEdgeLines", 0f);
                _intersectionMaterials[tier].SetFloat("_CurbRatio", 0f);
                _intersectionMaterials[tier].SetFloat("_ShoulderRatio", 0f);
            }
            return _intersectionMaterials[tier];
        }

        private void RenderBridgePillars(MapData data)
        {
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null) return;

            foreach (var edge in data.edges)
            {
                if (edge.layer != 1) continue;

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

                var col = pillar.GetComponent<Collider>();
                if (col != null) Destroy(col);

                if (bridgePillarMaterial != null)
                    pillar.GetComponent<Renderer>().sharedMaterial = bridgePillarMaterial;

                _spawnedObjects.Add(pillar);
            }
        }

        private void CreateRoadMesh(List<Vector3> verts, List<Vector2> uvs,
            List<int> tris, Material mat, string name)
        {
            if (verts.Count == 0) return;

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            _spawnedObjects.Add(go);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().mesh = mesh;
            if (mat != null)
                go.AddComponent<MeshRenderer>().material = mat;
            else
                go.AddComponent<MeshRenderer>();
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
