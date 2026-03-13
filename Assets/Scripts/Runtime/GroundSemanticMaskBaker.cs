using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// CPU bakes two packed RGBA8 textures from map data for ground compositing.
    /// Deterministic: same seed + preset produces identical masks.
    /// Uses grid-based spatial indexing (fat insertion / thin query) for
    /// O(~1) per-texel proximity queries instead of brute-force O(n).
    /// </summary>
    public static class GroundSemanticMaskBaker
    {
        // ─── Spatial Grid (fat insertion / thin query) ────────────────

        /// <summary>
        /// Uniform grid where each element is inserted into ALL cells it can
        /// influence. Query returns the single cell the probe point falls in.
        /// </summary>
        private struct SpatialGrid<T>
        {
            public readonly List<T>[] cells;
            public readonly int cols, rows;
            public readonly float cellSize;

            public SpatialGrid(float worldW, float worldH, float cellSize)
            {
                this.cellSize = cellSize;
                cols = Mathf.Max(1, Mathf.CeilToInt(worldW / cellSize));
                rows = Mathf.Max(1, Mathf.CeilToInt(worldH / cellSize));
                cells = new List<T>[cols * rows];
            }

            /// <summary>Insert item into all cells within radius of pos.</summary>
            public void Insert(Vector2 pos, float radius, T item)
            {
                int minCx = Mathf.Max(0, (int)((pos.x - radius) / cellSize));
                int maxCx = Mathf.Min(cols - 1, (int)((pos.x + radius) / cellSize));
                int minCy = Mathf.Max(0, (int)((pos.y - radius) / cellSize));
                int maxCy = Mathf.Min(rows - 1, (int)((pos.y + radius) / cellSize));

                for (int cy = minCy; cy <= maxCy; cy++)
                {
                    int rowBase = cy * cols;
                    for (int cx = minCx; cx <= maxCx; cx++)
                    {
                        int idx = rowBase + cx;
                        cells[idx] ??= new List<T>();
                        cells[idx].Add(item);
                    }
                }
            }

            /// <summary>Insert item into all cells overlapping the given AABB.</summary>
            public void InsertAABB(float minX, float minY, float maxX, float maxY, T item)
            {
                int minCx = Mathf.Max(0, (int)(minX / cellSize));
                int maxCx = Mathf.Min(cols - 1, (int)(maxX / cellSize));
                int minCy = Mathf.Max(0, (int)(minY / cellSize));
                int maxCy = Mathf.Min(rows - 1, (int)(maxY / cellSize));

                for (int cy = minCy; cy <= maxCy; cy++)
                {
                    int rowBase = cy * cols;
                    for (int cx = minCx; cx <= maxCx; cx++)
                    {
                        int idx = rowBase + cx;
                        cells[idx] ??= new List<T>();
                        cells[idx].Add(item);
                    }
                }
            }

            /// <summary>Get list of items in the cell containing pos (single cell lookup).</summary>
            public List<T> Query(Vector2 pos)
            {
                int cx = Mathf.Clamp((int)(pos.x / cellSize), 0, cols - 1);
                int cy = Mathf.Clamp((int)(pos.y / cellSize), 0, rows - 1);
                return cells[cy * cols + cx];
            }
        }

        // ─── Data structs ─────────────────────────────────────────────

        private struct RoadSegment2D
        {
            public Vector2 a, b;
            public float halfWidth;
        }

        private struct IntersectionData
        {
            public Vector2 position;
            public float radius;
        }

        // ─── Main Bake ────────────────────────────────────────────────

        public static GroundSemanticMaskSet Bake(
            MapData map,
            ElevationMap elevationMap,
            MapPreset preset,
            RoadProfile roadProfile,
            int bezierSegments,
            float intersectionRadiusFactor,
            int maskResolution)
        {
            int res = maskResolution;
            float worldW = preset.worldWidth;
            float worldH = preset.worldHeight;
            float maxElev = preset.maxElevation;

            var hsPixels = new Color32[res * res];
            var semPixels = new Color32[res * res];

            // Build per-type spatial grids (fat insertion)
            var waterGrid = BuildWaterGrid(map, worldW, worldH);
            var roadGrid = BuildRoadGrid(map, bezierSegments, roadProfile, worldW, worldH);
            var buildingGrid = BuildBuildingGrid(map.buildings, worldW, worldH);
            var intersectionGrid = BuildIntersectionGrid(
                map, roadProfile, intersectionRadiusFactor, worldW, worldH);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    // Map-space position (Y-inverted: texel y=0 is mapY=worldH)
                    float mapX = (x + 0.5f) / res * worldW;
                    float mapY = (1f - (y + 0.5f) / res) * worldH;
                    var mapPos = new Vector2(mapX, mapY);

                    int idx = y * res + x;

                    // --- HeightSlope texture ---
                    float elev = elevationMap != null ? elevationMap.Sample(mapPos) : 0f;
                    float elevNorm = Mathf.Clamp01(elev / Mathf.Max(maxElev, 0.01f));

                    float slope = elevationMap != null ? elevationMap.SampleSlope(mapPos) : 0f;
                    float slopeNorm = Mathf.Clamp01(slope / 2f); // 2.0 = very steep

                    // Curvature: second derivative approximation (signed, 0.5 = flat)
                    float curvature = 0.5f;
                    if (elevationMap != null)
                    {
                        const float d = 4f;
                        float ec = elev;
                        float eN = elevationMap.Sample(mapPos + new Vector2(0, d));
                        float eS = elevationMap.Sample(mapPos - new Vector2(0, d));
                        float eE = elevationMap.Sample(mapPos + new Vector2(d, 0));
                        float eW = elevationMap.Sample(mapPos - new Vector2(d, 0));
                        float laplacian = (eN + eS + eE + eW - 4f * ec) / (d * d);
                        curvature = Mathf.Clamp01(laplacian * 5f + 0.5f);
                    }

                    // Contour jitter: low-frequency hash for natural variation
                    float jitter = Hash01(mapX * 0.037f, mapY * 0.041f);

                    hsPixels[idx] = new Color32(
                        ToByte(elevNorm),
                        ToByte(slopeNorm),
                        ToByte(curvature),
                        ToByte(jitter));

                    // --- Semantic texture (grid-accelerated) ---
                    float moisture = ComputeMoisture(mapPos, waterGrid);
                    float roadInf = ComputeRoadInfluence(mapPos, roadGrid);
                    float buildingInf = ComputeBuildingInfluence(mapPos, buildingGrid);
                    float intersectionBoost = ComputeIntersectionBoost(mapPos, intersectionGrid);

                    semPixels[idx] = new Color32(
                        ToByte(moisture),
                        ToByte(roadInf),
                        ToByte(buildingInf),
                        ToByte(intersectionBoost));
                }
            }

            var hsTex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                name = "GroundHeightSlope",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            hsTex.SetPixels32(hsPixels);
            hsTex.Apply(false, true); // makeNoLongerReadable for GPU-only

            var semTex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                name = "GroundSemantic",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            semTex.SetPixels32(semPixels);
            semTex.Apply(false, true);

            return new GroundSemanticMaskSet(hsTex, semTex, res);
        }

        // ─── Grid construction ────────────────────────────────────────

        private static SpatialGrid<Vector2> BuildWaterGrid(
            MapData map, float worldW, float worldH)
        {
            const float maxDist = 40f;
            var grid = new SpatialGrid<Vector2>(worldW, worldH, maxDist);

            if (map.terrain?.waterBodies == null) return grid;

            foreach (var wb in map.terrain.waterBodies)
            {
                if (wb.pathPoints == null) continue;
                // Sample every other point to reduce density
                for (int i = 0; i < wb.pathPoints.Count; i += 2)
                {
                    var pt = wb.pathPoints[i];
                    grid.Insert(pt, maxDist, pt);
                }
            }
            return grid;
        }

        private static SpatialGrid<RoadSegment2D> BuildRoadGrid(
            MapData map, int bezierSegments, RoadProfile profile,
            float worldW, float worldH)
        {
            const float cellSize = 30f;
            var grid = new SpatialGrid<RoadSegment2D>(worldW, worldH, cellSize);
            var points = new List<Vector2>(bezierSegments + 1);

            foreach (var edge in map.edges)
            {
                points.Clear();
                RoadCurveSampler.Sample2D(edge, map.nodes, bezierSegments, points);

                int ti = Mathf.Clamp(edge.tier, 0, 2);
                float halfW = profile != null && ti < profile.tiers.Length
                    ? profile.tiers[ti].TotalWidth * 0.5f
                    : 6f;
                // Extend influence beyond pavement for soft shoulder
                float influenceHalfW = halfW * 2.5f;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    var seg = new RoadSegment2D
                    {
                        a = points[i],
                        b = points[i + 1],
                        halfWidth = influenceHalfW
                    };

                    // AABB of segment + influence padding
                    float minX = Mathf.Min(seg.a.x, seg.b.x) - influenceHalfW;
                    float maxX = Mathf.Max(seg.a.x, seg.b.x) + influenceHalfW;
                    float minY = Mathf.Min(seg.a.y, seg.b.y) - influenceHalfW;
                    float maxY = Mathf.Max(seg.a.y, seg.b.y) + influenceHalfW;

                    grid.InsertAABB(minX, minY, maxX, maxY, seg);
                }
            }
            return grid;
        }

        private static SpatialGrid<MapBuilding> BuildBuildingGrid(
            List<MapBuilding> buildings, float worldW, float worldH)
        {
            const float haloRadius = 15f;
            const float cellSize = 25f;
            var grid = new SpatialGrid<MapBuilding>(worldW, worldH, cellSize);

            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                float halfDiag = Mathf.Sqrt(b.width * b.width + b.height * b.height) * 0.5f;
                float radius = halfDiag + haloRadius;
                grid.Insert(b.position, radius, b);
            }
            return grid;
        }

        private static SpatialGrid<IntersectionData> BuildIntersectionGrid(
            MapData map, RoadProfile profile, float radiusFactor,
            float worldW, float worldH)
        {
            const float cellSize = 50f;
            var grid = new SpatialGrid<IntersectionData>(worldW, worldH, cellSize);

            var nodeBestTier = new Dictionary<int, int>();
            var nodeDegree = new Dictionary<int, int>();
            foreach (var edge in map.edges)
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

            foreach (var kvp in nodeDegree)
            {
                if (kvp.Value < 3) continue;
                int bestTier = nodeBestTier[kvp.Key];
                float totalW = profile != null && bestTier < profile.tiers.Length
                    ? profile.tiers[bestTier].TotalWidth
                    : 12f;
                var data = new IntersectionData
                {
                    position = map.nodes[kvp.Key].position,
                    radius = totalW * radiusFactor * 2f
                };
                grid.Insert(data.position, data.radius, data);
            }
            return grid;
        }

        // ─── Per-texel computations (grid-accelerated) ────────────────

        private static float ComputeMoisture(Vector2 pos, SpatialGrid<Vector2> grid)
        {
            const float maxDist = 40f;

            var cell = grid.Query(pos);
            if (cell == null) return 0f;

            float minDistSq = maxDist * maxDist;
            for (int i = 0; i < cell.Count; i++)
            {
                float dSq = (pos - cell[i]).sqrMagnitude;
                if (dSq < minDistSq) minDistSq = dSq;
            }

            float dist = Mathf.Sqrt(minDistSq);
            return Mathf.Clamp01(1f - dist / maxDist);
        }

        private static float ComputeRoadInfluence(Vector2 pos, SpatialGrid<RoadSegment2D> grid)
        {
            var cell = grid.Query(pos);
            if (cell == null) return 0f;

            float minDist = float.MaxValue;
            float closestHalfW = 15f;

            for (int i = 0; i < cell.Count; i++)
            {
                var seg = cell[i];
                float dist = PointToSegmentDistance(pos, seg.a, seg.b);
                if (dist < seg.halfWidth && dist < minDist)
                {
                    minDist = dist;
                    closestHalfW = seg.halfWidth;
                }
            }

            if (minDist >= float.MaxValue) return 0f;
            return Mathf.Clamp01(1f - minDist / closestHalfW);
        }

        private static float ComputeBuildingInfluence(Vector2 pos, SpatialGrid<MapBuilding> grid)
        {
            const float haloRadius = 15f;

            var cell = grid.Query(pos);
            if (cell == null) return 0f;

            float maxInfluence = 0f;
            for (int i = 0; i < cell.Count; i++)
            {
                var b = cell[i];
                float halfDiag = Mathf.Sqrt(b.width * b.width + b.height * b.height) * 0.5f;
                float dist = Vector2.Distance(pos, b.position);

                // Hard footprint
                if (dist < halfDiag)
                {
                    maxInfluence = 1f;
                    break;
                }

                // Soft halo
                float haloDist = dist - halfDiag;
                if (haloDist < haloRadius)
                {
                    float inf = 0.6f * (1f - haloDist / haloRadius);
                    if (inf > maxInfluence) maxInfluence = inf;
                }
            }

            return maxInfluence;
        }

        private static float ComputeIntersectionBoost(Vector2 pos, SpatialGrid<IntersectionData> grid)
        {
            var cell = grid.Query(pos);
            if (cell == null) return 0f;

            float maxBoost = 0f;
            for (int i = 0; i < cell.Count; i++)
            {
                float dist = Vector2.Distance(pos, cell[i].position);
                if (dist < cell[i].radius)
                {
                    float boost = 1f - dist / cell[i].radius;
                    if (boost > maxBoost) maxBoost = boost;
                }
            }
            return maxBoost;
        }

        // ─── Utility ───────────────────────────────────────────────────

        private static float PointToSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float abLenSq = ab.sqrMagnitude;
            if (abLenSq < 0.001f) return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSq);
            var closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        private static float Hash01(float x, float y)
        {
            // Simple deterministic hash returning [0,1]
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        private static byte ToByte(float v)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
        }
    }
}
