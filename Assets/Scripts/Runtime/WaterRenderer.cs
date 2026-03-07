using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Renders river ribbons and coast polygons as procedural meshes.
    /// </summary>
    public class WaterRenderer : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;

        [Header("Materials")]
        public Material riverMaterial;
        public Material coastMaterial;

        [Header("Settings")]
        public float waterYOffset = 0.02f;
        public float coastY = 0.01f;

        private readonly List<GameObject> _spawnedObjects = new();

        public void Render(MapData data)
        {
            Clear();
            var preset = mapManager != null ? mapManager.activePreset : null;
            if (preset == null || data.terrain == null) return;

            RenderRiver(data.terrain, preset);
            RenderCoast(data.terrain, preset);
        }

        public void Clear()
        {
            foreach (var obj in _spawnedObjects)
                if (obj != null) Destroy(obj);
            _spawnedObjects.Clear();
        }

        private void RenderRiver(MapTerrain terrain, MapPreset preset)
        {
            if (terrain.riverPoints == null || terrain.riverPoints.Count < 2) return;

            float halfW = preset.riverWidth * 0.5f;
            var points = terrain.riverPoints;
            int segCount = points.Count - 1;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];

                // Sample terrain elevation, river sits slightly below
                float terrainElev = 0f;
                if (mapManager != null && mapManager.CurrentElevationMap != null)
                    terrainElev = mapManager.CurrentElevationMap.Sample(p);
                float riverY = Mathf.Max(terrainElev - 0.5f, 0f) + waterYOffset;

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

                // Perpendicular in XZ plane (Y-inversion: negate Z component)
                var right = new Vector3(-tangent.y, 0f, -tangent.x).normalized;

                verts.Add(worldPos - right * halfW);
                verts.Add(worldPos + right * halfW);

                float v = i / (float)Mathf.Max(segCount, 1);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
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

            CreateWaterMesh(verts, uvs, tris, riverMaterial, "River");
        }

        private void RenderCoast(MapTerrain terrain, MapPreset preset)
        {
            if (terrain.coastPoints == null || terrain.coastPoints.Count < 3) return;

            var points = terrain.coastPoints;

            // Compute centroid
            var centroid = Vector2.zero;
            foreach (var p in points) centroid += p;
            centroid /= points.Count;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Center vertex
            var centerWorld = new Vector3(centroid.x, coastY, preset.worldHeight - centroid.y);
            verts.Add(centerWorld);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Perimeter vertices
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var worldPos = new Vector3(p.x, coastY, preset.worldHeight - p.y);
                verts.Add(worldPos);

                float u = (p.x - centroid.x) / (preset.worldWidth * 0.5f) * 0.5f + 0.5f;
                float v = (p.y - centroid.y) / (preset.worldHeight * 0.5f) * 0.5f + 0.5f;
                uvs.Add(new Vector2(u, v));
            }

            // Fan triangulation from centroid
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                tris.Add(0);
                tris.Add(1 + i);
                tris.Add(1 + next);
            }

            CreateWaterMesh(verts, uvs, tris, coastMaterial, "Coast");
        }

        private void CreateWaterMesh(List<Vector3> verts, List<Vector2> uvs,
            List<int> tris, Material mat, string name)
        {
            if (verts.Count == 0) return;

            var go = new GameObject($"Water_{name}");
            go.transform.SetParent(transform);
            _spawnedObjects.Add(go);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.material = mat;
        }
    }
}
