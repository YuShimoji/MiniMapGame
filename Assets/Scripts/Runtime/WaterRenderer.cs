using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Renders water bodies (rivers, coasts, lakes, etc.) as procedural meshes.
    /// Supports per-point width and depth data from WaterBodyData.
    /// </summary>
    public class WaterRenderer : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;

        [Header("Materials")]
        public Material waterMaterial;

        [Header("Settings")]
        [Tooltip("Height above carved terrain for water surfaces (must be >> groundYOffset to avoid Z-fighting)")]
        public float waterYOffset = 0.15f;

        private readonly List<GameObject> _spawnedObjects = new();

        public void Render(MapData data)
        {
            Clear();
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null || data.terrain == null) return;

            foreach (var water in data.terrain.waterBodies)
            {
                switch (water.bodyType)
                {
                    case WaterBodyType.River:
                    case WaterBodyType.Stream:
                    case WaterBodyType.Canal:
                        RenderRibbon(water, preset);
                        break;

                    case WaterBodyType.Coast:
                        RenderCoastPolygon(water, preset);
                        break;

                    case WaterBodyType.Lake:
                    case WaterBodyType.Pond:
                        RenderClosedPolygon(water, preset);
                        break;
                }
            }
        }

        public void Clear()
        {
            foreach (var obj in _spawnedObjects)
                if (obj != null) Destroy(obj);
            _spawnedObjects.Clear();
        }

        private void RenderRibbon(WaterBodyData water, MapPreset preset)
        {
            if (water.pathPoints == null || water.pathPoints.Count < 2) return;

            var profile = preset.waterProfile != null
                ? preset.waterProfile
                : WaterProfile.CreateDefaultFallback();

            var points = water.pathPoints;
            int segCount = points.Count - 1;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var colors = new List<Color32>();
            var tris = new List<int>();

            float roughness = profile.river.roughness;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                float halfW = (i < water.widths.Count ? water.widths[i] : 12f) * 0.5f;
                float depth = i < water.depths.Count ? water.depths[i] : 2f;

                // Sample terrain elevation, river sits below terrain
                float terrainElev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    terrainElev = mapManager.CurrentElevationMap.Sample(p);
                float riverY = Mathf.Max(terrainElev - depth * 0.2f, 0f) + waterYOffset;

                // World position (Y-inverted like MapGenUtils)
                var worldPos = new Vector3(p.x, riverY, preset.worldHeight - p.y);

                // Perpendicular direction
                Vector2 tangent;
                if (i == 0)
                    tangent = (points[1] - points[0]).normalized;
                else if (i == points.Count - 1)
                    tangent = (points[i] - points[i - 1]).normalized;
                else
                    tangent = (points[i + 1] - points[i - 1]).normalized;

                var right = new Vector3(-tangent.y, 0f, -tangent.x).normalized;

                verts.Add(worldPos - right * halfW);
                verts.Add(worldPos + right * halfW);

                float v = segCount > 0 ? i / (float)segCount : 0f;
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));

                // Depth normalized for shader (0-1 range over 5 units)
                float depthNorm = Mathf.Clamp01(depth / 5f);
                uv2.Add(new Vector2(depthNorm, 0f));
                uv2.Add(new Vector2(depthNorm, 1f));

                byte r = (byte)(roughness * 255);
                colors.Add(new Color32(r, 0, 0, 255));
                colors.Add(new Color32(r, 0, 0, 255));
            }

            for (int i = 0; i < segCount; i++)
            {
                int idx = i * 2;
                tris.Add(idx);
                tris.Add(idx + 2);
                tris.Add(idx + 1);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
                tris.Add(idx + 3);
            }

            CreateWaterMesh(verts, uvs, uv2, colors, tris, $"Water_{water.bodyType}");
        }

        private void RenderCoastPolygon(WaterBodyData water, MapPreset preset)
        {
            if (water.pathPoints == null || water.pathPoints.Count < 3) return;

            var profile = preset.waterProfile != null
                ? preset.waterProfile
                : WaterProfile.CreateDefaultFallback();

            var points = water.pathPoints;

            // Sample minimum terrain elevation along the coast perimeter
            // to place the water surface consistently above the carved shore
            float minTerrainElev = 0f;
            if (mapManager != null && mapManager.CurrentElevationMap != null)
            {
                minTerrainElev = float.MaxValue;
                foreach (var p in points)
                {
                    float elev = mapManager.CurrentElevationMap.Sample(p);
                    if (elev < minTerrainElev) minTerrainElev = elev;
                }
                if (minTerrainElev == float.MaxValue) minTerrainElev = 0f;
            }
            float coastY = Mathf.Max(minTerrainElev, 0f) + waterYOffset;

            // Compute centroid
            var centroid = Vector2.zero;
            foreach (var p in points) centroid += p;
            centroid /= points.Count;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var colors = new List<Color32>();
            var tris = new List<int>();

            float roughness = profile.coast.roughness;
            float depthBase = profile.coast.depthBase;

            // Center vertex (deep)
            var centerWorld = new Vector3(centroid.x, coastY, preset.worldHeight - centroid.y);
            verts.Add(centerWorld);
            uvs.Add(new Vector2(0.5f, 0.5f));
            uv2.Add(new Vector2(Mathf.Clamp01(depthBase / 5f), 0.5f));
            colors.Add(new Color32((byte)(roughness * 255), 0, 0, 255));

            // Perimeter vertices (shallow near shore)
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var worldPos = new Vector3(p.x, coastY, preset.worldHeight - p.y);
                verts.Add(worldPos);

                float u = (p.x - centroid.x) / (preset.worldWidth * 0.5f) * 0.5f + 0.5f;
                float v = (p.y - centroid.y) / (preset.worldHeight * 0.5f) * 0.5f + 0.5f;
                uvs.Add(new Vector2(u, v));

                float edgeDepth = i < water.depths.Count ? water.depths[i] : depthBase * 0.3f;
                uv2.Add(new Vector2(Mathf.Clamp01(edgeDepth / 5f), 0f));
                colors.Add(new Color32((byte)(roughness * 255), 0, 0, 255));
            }

            // Fan triangulation from centroid
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                tris.Add(0);
                tris.Add(1 + i);
                tris.Add(1 + next);
            }

            CreateWaterMesh(verts, uvs, uv2, colors, tris, "Water_Coast");
        }

        private void RenderClosedPolygon(WaterBodyData water, MapPreset preset)
        {
            // Reuses coast polygon rendering for lakes/ponds
            RenderCoastPolygon(water, preset);
        }

        private void CreateWaterMesh(List<Vector3> verts, List<Vector2> uvs,
            List<Vector2> uv2, List<Color32> colors, List<int> tris, string name)
        {
            if (verts.Count == 0) return;

            var go = new GameObject($"Water_{name}");
            go.transform.SetParent(transform);
            _spawnedObjects.Add(go);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            if (uv2.Count == verts.Count)
                mesh.SetUVs(1, uv2);
            if (colors.Count == verts.Count)
                mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (waterMaterial != null) mr.material = waterMaterial;
        }
    }
}
