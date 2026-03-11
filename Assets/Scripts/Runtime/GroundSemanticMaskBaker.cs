using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// CPU bakes two packed RGBA8 textures from map data for ground compositing.
    /// Deterministic: same seed + preset produces identical masks.
    /// </summary>
    public static class GroundSemanticMaskBaker
    {
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

            // Pre-compute road polylines in map-space for distance queries
            var roadSegments = PrecomputeRoadSegments(map, bezierSegments, roadProfile);

            // Pre-compute intersection positions and radii
            var intersections = PrecomputeIntersections(map, roadProfile, intersectionRadiusFactor);

            // Pre-compute water proximity data
            var waterPoints = PrecomputeWaterPoints(map);

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

                    // --- Semantic texture ---
                    float moisture = ComputeMoisture(mapPos, waterPoints);
                    float roadInf = ComputeRoadInfluence(mapPos, roadSegments);
                    float buildingInf = ComputeBuildingInfluence(mapPos, map.buildings);
                    float intersectionBoost = ComputeIntersectionBoost(mapPos, intersections);

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

        // ─── Road pre-computation ──────────────────────────────────────

        private struct RoadSegment2D
        {
            public Vector2 a, b;
            public float halfWidth;
        }

        private static List<RoadSegment2D> PrecomputeRoadSegments(
            MapData map, int bezierSegments, RoadProfile profile)
        {
            var segments = new List<RoadSegment2D>(map.edges.Count * bezierSegments);
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
                    segments.Add(new RoadSegment2D
                    {
                        a = points[i],
                        b = points[i + 1],
                        halfWidth = influenceHalfW
                    });
                }
            }
            return segments;
        }

        private struct IntersectionData
        {
            public Vector2 position;
            public float radius;
        }

        private static List<IntersectionData> PrecomputeIntersections(
            MapData map, RoadProfile profile, float radiusFactor)
        {
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

            var result = new List<IntersectionData>();
            foreach (var kvp in nodeDegree)
            {
                if (kvp.Value < 3) continue;
                int bestTier = nodeBestTier[kvp.Key];
                float totalW = profile != null && bestTier < profile.tiers.Length
                    ? profile.tiers[bestTier].TotalWidth
                    : 12f;
                result.Add(new IntersectionData
                {
                    position = map.nodes[kvp.Key].position,
                    radius = totalW * radiusFactor * 2f
                });
            }
            return result;
        }

        // ─── Water pre-computation ─────────────────────────────────────

        private static List<Vector2> PrecomputeWaterPoints(MapData map)
        {
            var points = new List<Vector2>();
            if (map.terrain?.waterBodies == null) return points;

            foreach (var wb in map.terrain.waterBodies)
            {
                if (wb.pathPoints == null) continue;
                // Sample every other point to reduce density
                for (int i = 0; i < wb.pathPoints.Count; i += 2)
                    points.Add(wb.pathPoints[i]);
            }
            return points;
        }

        // ─── Per-texel computations ────────────────────────────────────

        private static float ComputeMoisture(Vector2 pos, List<Vector2> waterPoints)
        {
            const float maxDist = 40f;
            float minDistSq = maxDist * maxDist;

            for (int i = 0; i < waterPoints.Count; i++)
            {
                float dSq = (pos - waterPoints[i]).sqrMagnitude;
                if (dSq < minDistSq) minDistSq = dSq;
            }

            float dist = Mathf.Sqrt(minDistSq);
            return Mathf.Clamp01(1f - dist / maxDist);
        }

        private static float ComputeRoadInfluence(Vector2 pos, List<RoadSegment2D> segments)
        {
            float minDist = float.MaxValue;

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                float dist = PointToSegmentDistance(pos, seg.a, seg.b);
                if (dist < seg.halfWidth && dist < minDist)
                    minDist = dist;
            }

            if (minDist >= float.MaxValue) return 0f;

            // Find the halfWidth of the closest segment for normalization
            float normHalfW = 15f;
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                float dist = PointToSegmentDistance(pos, seg.a, seg.b);
                if (Mathf.Approximately(dist, minDist))
                {
                    normHalfW = seg.halfWidth;
                    break;
                }
            }

            return Mathf.Clamp01(1f - minDist / normHalfW);
        }

        private static float ComputeBuildingInfluence(Vector2 pos, List<MapBuilding> buildings)
        {
            const float haloRadius = 15f;
            float maxInfluence = 0f;

            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
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

        private static float ComputeIntersectionBoost(Vector2 pos, List<IntersectionData> intersections)
        {
            float maxBoost = 0f;
            for (int i = 0; i < intersections.Count; i++)
            {
                float dist = Vector2.Distance(pos, intersections[i].position);
                if (dist < intersections[i].radius)
                {
                    float boost = 1f - dist / intersections[i].radius;
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
