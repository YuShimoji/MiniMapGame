using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Generates all water features (rivers, coasts, and future water body types).
    /// Extracted from TerrainGenerator to enable terrain-responsive placement
    /// and resolve H1 conflict (TerrainGenerator now handles hills only).
    /// </summary>
    public static class WaterGenerator
    {
        /// <summary>
        /// Determine which side the coast is on. Consumes exactly 1 rng call.
        /// Called before hill generation so hills can avoid the coast side.
        /// Returns -1 if preset has no coast.
        /// </summary>
        public static int DetermineCoastSide(SeededRng rng, MapPreset preset)
        {
            if (!preset.hasCoast) return -1;
            return Mathf.FloorToInt(rng.Next() * 4f);
        }

        /// <summary>
        /// Generate all water bodies for the map.
        /// Called AFTER ElevationMap is created from hills, enabling terrain-responsive placement.
        /// </summary>
        public static List<WaterBodyData> Generate(SeededRng rng, Vector2 center,
            MapPreset preset, int coastSide, List<MapNode> nodes = null,
            ElevationMap elevMap = null)
        {
            var waterBodies = new List<WaterBodyData>();
            var profile = preset.waterProfile != null
                ? preset.waterProfile
                : WaterProfile.CreateDefaultFallback();

            if (preset.hasCoast && coastSide >= 0)
            {
                var coast = GenerateCoast(rng, preset, profile.coast, coastSide);
                waterBodies.Add(coast);
            }

            if (preset.hasRiver)
            {
                // W-5: Auto-tune meander when using default fallback profile
                var riverConfig = profile.river;
                if (preset.waterProfile == null)
                    riverConfig = ApplyPresetMeanderTuning(riverConfig, preset.generatorType);

                var river = GenerateRiver(rng, center, preset, riverConfig,
                    nodes, elevMap, waterBodies);
                waterBodies.Add(river);
            }

            return waterBodies;
        }

        // ─── Coast generation ─────────────────────────────────────────────

        private static WaterBodyData GenerateCoast(SeededRng rng, MapPreset preset,
            WaterProfile.CoastConfig config, int side)
        {
            var coast = new WaterBodyData
            {
                bodyType = WaterBodyType.Coast,
                coastSide = side
            };

            float w = preset.worldWidth;
            float h = preset.worldHeight;
            float reach = config.inlandReach;
            float roughness = config.coastlineRoughness;

            switch (side)
            {
                case 0: GenerateCoastRight(coast, rng, w, h, reach, roughness, config); break;
                case 1: GenerateCoastBottom(coast, rng, w, h, reach, roughness, config); break;
                case 2: GenerateCoastLeft(coast, rng, w, h, reach, roughness, config); break;
                case 3: GenerateCoastTop(coast, rng, w, h, reach, roughness, config); break;
            }

            // Add depth data for each coast point
            for (int i = 0; i < coast.pathPoints.Count; i++)
            {
                float depthNoise = Mathf.Sin(i * 0.3f + rng.Next() * 6.28f) * 0.5f + 0.5f;
                float depth = config.depthBase + depthNoise * config.depthVariation * config.depthBase;
                coast.depths.Add(depth);
                coast.widths.Add(0f);
            }

            coast.ComputeBounds();
            return coast;
        }

        private static void GenerateCoastRight(WaterBodyData coast, SeededRng rng,
            float w, float h, float reach, float roughness,
            WaterProfile.CoastConfig config)
        {
            float baseX = w * (1f - reach);
            float jitter = roughness * 0.12f;

            // W-2: Bay/cape pre-computation
            float coastLength = h;
            float amplitude = reach * w * config.bayAmplitude;
            float[] baySigns = null;
            if (config.bayAmplitude > 0f && config.baySpacing > 1f)
                baySigns = PrecomputeBaySigns(rng, coastLength, config.baySpacing);

            coast.pathPoints.Add(new Vector2(baseX + rng.Next() * w * 0.1f, 0f));
            coast.pathPoints.Add(new Vector2(w, 0f));
            coast.pathPoints.Add(new Vector2(w, h));

            float y = h;
            while (y > 0f)
            {
                float bayDisp = 0f;
                if (baySigns != null)
                {
                    float along = h - y;
                    bayDisp = ComputeBayOffset(along, coastLength, amplitude,
                        config.baySpacing, baySigns);
                }
                float x = baseX + w * (rng.Next() - 0.5f) * jitter + bayDisp;
                coast.pathPoints.Add(new Vector2(x, y));
                y -= config.stepSizeMin + rng.Next() * (config.stepSizeMax - config.stepSizeMin);
            }
        }

        private static void GenerateCoastBottom(WaterBodyData coast, SeededRng rng,
            float w, float h, float reach, float roughness,
            WaterProfile.CoastConfig config)
        {
            float baseY = h * (1f - reach);
            float jitter = roughness * 0.12f;

            // W-2: Bay/cape pre-computation
            float coastLength = w;
            float amplitude = reach * h * config.bayAmplitude;
            float[] baySigns = null;
            if (config.bayAmplitude > 0f && config.baySpacing > 1f)
                baySigns = PrecomputeBaySigns(rng, coastLength, config.baySpacing);

            coast.pathPoints.Add(new Vector2(0f, baseY + rng.Next() * h * 0.1f));
            coast.pathPoints.Add(new Vector2(0f, h));
            coast.pathPoints.Add(new Vector2(w, h));

            float x = w;
            while (x > 0f)
            {
                float bayDisp = 0f;
                if (baySigns != null)
                {
                    float along = w - x;
                    bayDisp = ComputeBayOffset(along, coastLength, amplitude,
                        config.baySpacing, baySigns);
                }
                float cy = baseY + h * (rng.Next() - 0.5f) * jitter + bayDisp;
                coast.pathPoints.Add(new Vector2(x, cy));
                x -= config.stepSizeMin + rng.Next() * (config.stepSizeMax - config.stepSizeMin);
            }
        }

        private static void GenerateCoastLeft(WaterBodyData coast, SeededRng rng,
            float w, float h, float reach, float roughness,
            WaterProfile.CoastConfig config)
        {
            float baseX = w * reach;
            float jitter = roughness * 0.12f;

            // W-2: Bay/cape pre-computation (left coast: negate displacement)
            float coastLength = h;
            float amplitude = reach * w * config.bayAmplitude;
            float[] baySigns = null;
            if (config.bayAmplitude > 0f && config.baySpacing > 1f)
                baySigns = PrecomputeBaySigns(rng, coastLength, config.baySpacing);

            coast.pathPoints.Add(new Vector2(baseX - rng.Next() * w * 0.1f, 0f));
            coast.pathPoints.Add(new Vector2(0f, 0f));
            coast.pathPoints.Add(new Vector2(0f, h));

            float y = h;
            while (y > 0f)
            {
                float bayDisp = 0f;
                if (baySigns != null)
                {
                    float along = h - y;
                    bayDisp = ComputeBayOffset(along, coastLength, amplitude,
                        config.baySpacing, baySigns) * -1f; // invert for left coast
                }
                float x = baseX + w * (rng.Next() - 0.5f) * jitter + bayDisp;
                coast.pathPoints.Add(new Vector2(x, y));
                y -= config.stepSizeMin + rng.Next() * (config.stepSizeMax - config.stepSizeMin);
            }
        }

        private static void GenerateCoastTop(WaterBodyData coast, SeededRng rng,
            float w, float h, float reach, float roughness,
            WaterProfile.CoastConfig config)
        {
            float baseY = h * reach;
            float jitter = roughness * 0.12f;

            // W-2: Bay/cape pre-computation (top coast: negate displacement)
            float coastLength = w;
            float amplitude = reach * h * config.bayAmplitude;
            float[] baySigns = null;
            if (config.bayAmplitude > 0f && config.baySpacing > 1f)
                baySigns = PrecomputeBaySigns(rng, coastLength, config.baySpacing);

            coast.pathPoints.Add(new Vector2(0f, baseY - rng.Next() * h * 0.1f));
            coast.pathPoints.Add(new Vector2(0f, 0f));
            coast.pathPoints.Add(new Vector2(w, 0f));

            float x = w;
            while (x > 0f)
            {
                float bayDisp = 0f;
                if (baySigns != null)
                {
                    float along = w - x;
                    bayDisp = ComputeBayOffset(along, coastLength, amplitude,
                        config.baySpacing, baySigns) * -1f; // invert for top coast
                }
                float cy = baseY + h * (rng.Next() - 0.5f) * jitter + bayDisp;
                coast.pathPoints.Add(new Vector2(x, cy));
                x -= config.stepSizeMin + rng.Next() * (config.stepSizeMax - config.stepSizeMin);
            }
        }

        // ─── W-1: Meta-geography gradient descent river routing ─────────

        private static WaterBodyData GenerateRiver(SeededRng rng, Vector2 center,
            MapPreset preset, WaterProfile.RiverConfig config,
            List<MapNode> nodes, ElevationMap elevMap,
            List<WaterBodyData> existingWaterBodies = null)
        {
            var river = new WaterBodyData { bodyType = WaterBodyType.River };

            float w = preset.worldWidth;
            float h = preset.worldHeight;
            float pad = preset.borderPadding;

            float sway = config.swayAmount;

            // ─── Source: highest elevation outside coast ──────────────────
            Vector2 source = FindRiverSource(rng, elevMap, preset, existingWaterBodies);

            // ─── Initial momentum from gradient ──────────────────────────
            Vector2 momentum = ComputeGradientFlow(elevMap, source);
            if (momentum.sqrMagnitude < 0.0001f)
            {
                // Flat terrain fallback: aim toward map center
                momentum = (new Vector2(w * 0.5f, h * 0.5f) - source).normalized;
                if (momentum.sqrMagnitude < 0.0001f)
                    momentum = Vector2.down;
            }
            river.flowDirection = Mathf.Atan2(momentum.y, momentum.x);

            // ─── Gradient descent walk ───────────────────────────────────
            Vector2 pos = source;
            float meanderPhase = rng.Next() * Mathf.PI * 2f;
            int stepCount = 0;
            const int maxSteps = 500;
            float maxRiverLen = Mathf.Sqrt(w * w + h * h) * 0.8f;

            while (stepCount < maxSteps)
            {
                // Stop at map bounds (small overshoot allowed for smooth exit)
                bool atEdge = pos.x < -5f || pos.x > w + 5f
                           || pos.y < -5f || pos.y > h + 5f;

                // Stop if entering coast polygon (min 4 steps for short-river guard)
                bool enteredCoast = !atEdge && stepCount > 3
                    && existingWaterBodies != null
                    && IsInsideCoast(pos, existingWaterBodies);

                // Record point (including final point at edge/coast for visual continuity)
                river.pathPoints.Add(new Vector2(
                    Mathf.Clamp(pos.x, pad, w - pad),
                    Mathf.Clamp(pos.y, pad, h - pad)));

                float distFromSource = Vector2.Distance(pos, source);
                float t = Mathf.Clamp01(distFromSource / maxRiverLen);

                river.widths.Add(config.baseWidth * Mathf.Lerp(1f, config.widthGrowth, t));

                float depthNoise = Mathf.Sin(stepCount * 0.2f + rng.Next() * 3.14f)
                    * 0.5f + 0.5f;
                float downstreamFactor = 1f + t * 0.5f;
                river.depths.Add(config.depthBase * downstreamFactor
                    * (1f + depthNoise * config.depthVariation));

                if (atEdge || enteredCoast) break;

                // ─── Direction for next step ─────────────────────────────
                Vector2 gradient = ComputeGradientFlow(elevMap, pos);

                Vector2 desiredDir;
                if (gradient.sqrMagnitude > 0.0001f)
                {
                    float gradWeight = Mathf.Lerp(0.3f, 0.8f, config.flowResponsiveness);
                    desiredDir = Vector2.Lerp(momentum, gradient.normalized, gradWeight)
                        .normalized;
                }
                else
                {
                    desiredDir = momentum; // Flat terrain: maintain momentum
                }

                // Loop detection: if near earlier path, force momentum
                if (river.pathPoints.Count > 10)
                {
                    int checkEnd = river.pathPoints.Count - 8;
                    for (int i = 0; i < checkEnd; i += 3)
                    {
                        if (Vector2.SqrMagnitude(pos - river.pathPoints[i]) < 400f)
                        {
                            desiredDir = momentum;
                            break;
                        }
                    }
                }

                // Clamp turn angle to 45 deg per step
                float angleDiff = Vector2.SignedAngle(momentum, desiredDir);
                if (Mathf.Abs(angleDiff) > 45f)
                {
                    float clampedRad = Mathf.Sign(angleDiff) * 45f * Mathf.Deg2Rad;
                    desiredDir = RotateVector(momentum, clampedRad);
                }

                // Step length
                float stepLen = config.stepSizeMin
                    + rng.Next() * (config.stepSizeMax - config.stepSizeMin);

                // Meander (perpendicular to flow)
                meanderPhase += stepLen * config.meanderFrequency * 0.02f;
                Vector2 perp = new Vector2(-desiredDir.y, desiredDir.x);
                float meanderBias = Mathf.Sin(meanderPhase) * sway * 0.6f;
                float jitter = (rng.Next() - 0.5f) * sway * 0.4f;

                // Advance
                Vector2 advance = desiredDir * stepLen + perp * (meanderBias + jitter);
                pos += advance;
                momentum = advance.normalized;
                stepCount++;
            }

            // W-3: Apply sandbanks at meander bends
            if (config.sandbankStrength > 0f)
                ApplySandbanks(river, config.sandbankStrength);

            river.ComputeBounds();
            return river;
        }

        /// <summary>
        /// Find river source: grid-sample ElevationMap for highest point outside coast.
        /// Consumes 1 rng call (pick from top candidates). Flat terrain falls back to edge.
        /// </summary>
        private static Vector2 FindRiverSource(SeededRng rng, ElevationMap elevMap,
            MapPreset preset, List<WaterBodyData> waterBodies)
        {
            float w = preset.worldWidth;
            float h = preset.worldHeight;
            float pad = preset.borderPadding;

            const int gridSize = 8;
            var candidates = new List<(Vector2 pos, float elev)>();

            for (int gy = 0; gy < gridSize; gy++)
            {
                for (int gx = 0; gx < gridSize; gx++)
                {
                    float x = pad + (w - 2f * pad) * (gx + 0.5f) / gridSize;
                    float y = pad + (h - 2f * pad) * (gy + 0.5f) / gridSize;
                    var pos = new Vector2(x, y);

                    // Exclude points inside coast polygon
                    if (IsInsideCoast(pos, waterBodies)) continue;

                    float elev = elevMap != null ? elevMap.Sample(pos) : 0f;
                    candidates.Add((pos, elev));
                }
            }

            // Sort by elevation descending
            candidates.Sort((a, b) => b.elev.CompareTo(a.elev));

            int topN = Mathf.Min(3, candidates.Count);
            // Flat terrain or no candidates: pick random edge point
            if (topN == 0 || candidates[0].elev < 0.1f)
            {
                float along = rng.Next();
                int side = Mathf.FloorToInt(rng.Next() * 4f);
                return side switch
                {
                    0 => new Vector2(w - pad, pad + along * (h - 2f * pad)),
                    1 => new Vector2(pad + along * (w - 2f * pad), h - pad),
                    2 => new Vector2(pad, pad + along * (h - 2f * pad)),
                    _ => new Vector2(pad + along * (w - 2f * pad), pad),
                };
            }

            int pick = Mathf.FloorToInt(rng.Next() * topN);
            return candidates[pick].pos;
        }

        /// <summary>
        /// Compute downhill flow direction at a position using central differences.
        /// Returns zero vector on flat terrain.
        /// </summary>
        private static Vector2 ComputeGradientFlow(ElevationMap elevMap, Vector2 pos)
        {
            if (elevMap == null) return Vector2.zero;

            const float delta = 10f;
            float elevE = elevMap.Sample(pos + new Vector2(delta, 0f));
            float elevW = elevMap.Sample(pos - new Vector2(delta, 0f));
            float elevN = elevMap.Sample(pos + new Vector2(0f, delta));
            float elevS = elevMap.Sample(pos - new Vector2(0f, delta));

            var gradient = new Vector2(elevE - elevW, elevN - elevS) / (2f * delta);
            return -gradient; // Downhill = negative gradient
        }

        private static Vector2 RotateVector(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ─── W-2: Bay/cape coastline patterns ──────────────────────────────

        /// <summary>
        /// Pre-compute random sign for each bay/cape feature along the coastline.
        /// +1 = cape (land protrudes), -1 = bay (water indents).
        /// </summary>
        private static float[] PrecomputeBaySigns(SeededRng rng, float coastLength, float baySpacing)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(coastLength / baySpacing));
            var signs = new float[count];
            for (int i = 0; i < count; i++)
                signs[i] = rng.Next() > 0.5f ? 1f : -1f;
            return signs;
        }

        /// <summary>
        /// Compute bay/cape displacement at a position along the coastline.
        /// Returns positive for cape, negative for bay.
        /// Uses smooth cosine bumps centered on each segment.
        /// </summary>
        private static float ComputeBayOffset(float along, float coastLength,
            float amplitude, float baySpacing, float[] baySigns)
        {
            int count = baySigns.Length;
            float segLen = coastLength / count;
            float offset = 0f;

            for (int i = 0; i < count; i++)
            {
                float segCenter = (i + 0.5f) * segLen;
                float localT = (along - segCenter) / (segLen * 0.5f);
                if (Mathf.Abs(localT) <= 1f)
                {
                    float shape = 0.5f + 0.5f * Mathf.Cos(localT * Mathf.PI);
                    offset += baySigns[i] * amplitude * shape;
                }
            }

            return offset;
        }

        // ─── W-3: Sandbanks at river meanders ──────────────────────────────

        /// <summary>
        /// Reduce river depth at high-curvature bends to create sandbank visuals.
        /// The shader's ShallowColor renders reduced-depth areas lighter.
        /// </summary>
        private static void ApplySandbanks(WaterBodyData river, float strength)
        {
            var points = river.pathPoints;
            if (points.Count < 3) return;

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 d1 = (points[i] - points[i - 1]).normalized;
                Vector2 d2 = (points[i + 1] - points[i]).normalized;
                float cross = Mathf.Abs(d1.x * d2.y - d1.y * d2.x);

                const float curvatureThreshold = 0.1f;
                if (cross > curvatureThreshold)
                {
                    float intensity = Mathf.Clamp01((cross - curvatureThreshold) / 0.5f);
                    float factor = 1f - strength * intensity;
                    factor = Mathf.Max(factor, 0.2f);
                    river.depths[i] *= factor;
                }
            }
        }

        // ─── W-5: Per-preset meander auto-tuning ─────────────────────────

        /// <summary>
        /// Adjust meander/sway for generator type when using default WaterProfile.
        /// Custom WaterProfile values are never overridden.
        /// </summary>
        private static WaterProfile.RiverConfig ApplyPresetMeanderTuning(
            WaterProfile.RiverConfig config, GeneratorType type)
        {
            switch (type)
            {
                case GeneratorType.Rural:
                    config.meanderFrequency *= 0.6f;
                    config.swayAmount *= 0.65f;
                    break;
                case GeneratorType.Mountain:
                    config.meanderFrequency *= 1.6f;
                    config.swayAmount *= 0.4f;
                    break;
                case GeneratorType.Grid:
                    config.meanderFrequency *= 0.2f;
                    config.swayAmount *= 0.3f;
                    break;
                // Organic: 1.0x (no adjustment)
            }
            return config;
        }

        // ─── Utility methods ──────────────────────────────────────────────

        /// <summary>
        /// Get the path points from the first water body of a given type.
        /// Convenience for legacy integration (BridgeTunnelDetector etc.).
        /// </summary>
        public static List<Vector2> GetPathPoints(List<WaterBodyData> waterBodies,
            WaterBodyType type)
        {
            if (waterBodies == null) return new List<Vector2>();

            foreach (var wb in waterBodies)
            {
                if (wb.bodyType == type)
                    return wb.pathPoints;
            }
            return new List<Vector2>();
        }

        /// <summary>
        /// Get the first water body of a given type, or null.
        /// </summary>
        public static WaterBodyData GetFirstOfType(List<WaterBodyData> waterBodies,
            WaterBodyType type)
        {
            if (waterBodies == null) return null;

            foreach (var wb in waterBodies)
            {
                if (wb.bodyType == type)
                    return wb;
            }
            return null;
        }

        /// <summary>
        /// Check if a point is inside any coast polygon.
        /// Uses ray-casting point-in-polygon test.
        /// </summary>
        public static bool IsInsideCoast(Vector2 point, List<WaterBodyData> waterBodies)
        {
            if (waterBodies == null) return false;

            foreach (var wb in waterBodies)
            {
                if (wb.bodyType != WaterBodyType.Coast) continue;
                if (!wb.BoundsContains(point)) continue;
                if (PointInPolygon(point, wb.pathPoints)) return true;
            }
            return false;
        }

        /// <summary>
        /// Compute minimum distance from point to any water body.
        /// </summary>
        public static float MinDistToWater(Vector2 pos, List<WaterBodyData> waterBodies)
        {
            if (waterBodies == null) return float.MaxValue;

            float minDistSq = float.MaxValue;

            foreach (var wb in waterBodies)
            {
                // AABB early-out: skip water bodies far from query point
                float expandedMargin = 50f;
                if (pos.x < wb.boundsMin.x - expandedMargin ||
                    pos.x > wb.boundsMax.x + expandedMargin ||
                    pos.y < wb.boundsMin.y - expandedMargin ||
                    pos.y > wb.boundsMax.y + expandedMargin)
                    continue;

                foreach (var p in wb.pathPoints)
                {
                    float d = Vector2.SqrMagnitude(pos - p);
                    if (d < minDistSq) minDistSq = d;
                }
            }

            return minDistSq < float.MaxValue ? Mathf.Sqrt(minDistSq) : float.MaxValue;
        }

        /// <summary>
        /// Approximate check: is position on the water side of a coast?
        /// Uses average coastline position per axis.
        /// </summary>
        public static bool IsOnWaterSide(Vector2 pos, int coastSide,
            List<WaterBodyData> waterBodies)
        {
            if (coastSide < 0 || waterBodies == null) return false;

            var coast = GetFirstOfType(waterBodies, WaterBodyType.Coast);
            if (coast == null || coast.pathPoints.Count < 3) return false;

            float avgCoast = 0f;
            int count = 0;
            foreach (var cp in coast.pathPoints)
            {
                switch (coastSide)
                {
                    case 0: case 2: avgCoast += cp.x; break;
                    case 1: case 3: avgCoast += cp.y; break;
                }
                count++;
            }
            if (count == 0) return false;
            avgCoast /= count;

            switch (coastSide)
            {
                case 0: return pos.x > avgCoast;
                case 1: return pos.y > avgCoast;
                case 2: return pos.x < avgCoast;
                case 3: return pos.y < avgCoast;
            }
            return false;
        }

        /// <summary>
        /// Ray-casting point-in-polygon test.
        /// </summary>
        public static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;

            bool inside = false;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                        / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
