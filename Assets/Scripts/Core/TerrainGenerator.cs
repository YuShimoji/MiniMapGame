using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Generates terrain features (hills). Port of JSX genTerrain().
    /// Hills are now placed in clusters (ridges, mound groups, valley framers) for natural terrain.
    /// Coast and river generation moved to WaterGenerator.
    /// </summary>
    public static class TerrainGenerator
    {
        private const float MinNodeDist = 30f;
        private const float MinClusterSpacing = 60f;

        public static MapTerrain Generate(SeededRng rng, Vector2 center, MapPreset preset,
            int coastSide, List<MapNode> nodes = null)
        {
            var terrain = new MapTerrain();
            terrain.coastSide = coastSide;
            GenerateHills(terrain, rng, preset, nodes);
            return terrain;
        }

        // ─── Hill generation (H1 cluster-based + H3 profile assignment) ─

        private static void GenerateHills(MapTerrain terrain, SeededRng rng, MapPreset preset,
            List<MapNode> nodes = null)
        {
            float w = preset.worldWidth;
            float h = preset.worldHeight;
            int numClusters = Mathf.FloorToInt(preset.hillDensity * (3f + rng.Next() * 4f));
            if (numClusters == 0) return;

            var clusterCenters = new List<Vector2>();
            int clusterId = 0;

            for (int c = 0; c < numClusters; c++)
            {
                // Pick coast-aware position with inter-cluster spacing
                var center = PickCoastAwarePosition(rng, w, h, terrain.coastSide);

                bool placed = false;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    bool tooClose = false;
                    foreach (var existing in clusterCenters)
                    {
                        if (Vector2.Distance(center, existing) < MinClusterSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) { placed = true; break; }
                    center = PickCoastAwarePosition(rng, w, h, terrain.coastSide);
                }

                // Fallback: if can't place with spacing, degrade to solitary
                ClusterType type = placed
                    ? PickClusterType(rng, preset.generatorType)
                    : ClusterType.Solitary;

                SlopeProfile dominantProfile = PickDominantProfile(rng, preset);
                float orientation = rng.Next() * Mathf.PI;

                // Bias orientation away from coast if applicable
                if (terrain.coastSide >= 0)
                {
                    float coastAngle = terrain.coastSide * Mathf.PI * 0.5f;
                    orientation = Mathf.Lerp(orientation, coastAngle + Mathf.PI * 0.5f, 0.3f);
                }

                var cluster = new HillCluster
                {
                    id = clusterId,
                    type = type,
                    center = center,
                    orientationAngle = orientation,
                    dominantProfile = dominantProfile
                };
                terrain.hillClusters.Add(cluster);
                clusterCenters.Add(center);

                switch (type)
                {
                    case ClusterType.Ridge:
                        GenerateRidgeHills(terrain, rng, cluster, preset, nodes);
                        break;
                    case ClusterType.MoundGroup:
                        GenerateMoundGroupHills(terrain, rng, cluster, preset, nodes);
                        break;
                    case ClusterType.ValleyFramer:
                        GenerateValleyFramerHills(terrain, rng, cluster, preset, nodes);
                        break;
                    case ClusterType.Solitary:
                        GenerateSolitaryHill(terrain, rng, cluster, preset, nodes);
                        break;
                }

                clusterId++;
            }
        }

        // ─── Cluster type generation ────────────────────────────────────

        private static void GenerateRidgeHills(MapTerrain terrain, SeededRng rng,
            HillCluster cluster, MapPreset preset, List<MapNode> nodes)
        {
            int count = 3 + Mathf.FloorToInt(rng.Next() * 4f); // 3-6 hills
            float spacing = 30f + rng.Next() * 20f;
            float halfLen = (count - 1) * spacing * 0.5f;
            float cosA = Mathf.Cos(cluster.orientationAngle);
            float sinA = Mathf.Sin(cluster.orientationAngle);

            for (int i = 0; i < count; i++)
            {
                float along = -halfLen + i * spacing;
                float perpJitter = (rng.Next() - 0.5f) * 20f;

                float px = cluster.center.x + cosA * along + sinA * perpJitter;
                float py = cluster.center.y + sinA * along - cosA * perpJitter;

                // Hills elongated along ridge axis
                float radiusAlong = 40f + rng.Next() * 40f;
                float radiusCross = 20f + rng.Next() * 25f;

                // Center hills taller than ends
                float centeredness = 1f - Mathf.Abs(2f * i / (count - 1f) - 1f);
                int layers = 2 + Mathf.FloorToInt(centeredness * 2f + rng.Next());

                // Slight profile variation within cluster
                SlopeProfile profile = (rng.Next() < 0.7f)
                    ? cluster.dominantProfile
                    : PickVariantProfile(rng, cluster.dominantProfile);

                var pos = ApplyNodeAvoidance(new Vector2(px, py), nodes, rng, preset);
                terrain.hills.Add(new HillData
                {
                    position = pos,
                    radiusX = radiusAlong,
                    radiusY = radiusCross,
                    angle = cluster.orientationAngle + (rng.Next() - 0.5f) * 0.15f,
                    layers = layers,
                    profile = profile,
                    clusterId = cluster.id
                });
            }
        }

        private static void GenerateMoundGroupHills(MapTerrain terrain, SeededRng rng,
            HillCluster cluster, MapPreset preset, List<MapNode> nodes)
        {
            int count = 3 + Mathf.FloorToInt(rng.Next() * 3f); // 3-5 hills
            float clusterRadius = 40f + rng.Next() * 40f;

            // Central hill (largest)
            {
                float rx = 45f + rng.Next() * 50f;
                float ry = 35f + rng.Next() * 35f;
                var pos = ApplyNodeAvoidance(cluster.center, nodes, rng, preset);
                terrain.hills.Add(new HillData
                {
                    position = pos,
                    radiusX = rx,
                    radiusY = ry,
                    angle = rng.Next() * Mathf.PI,
                    layers = 3 + Mathf.FloorToInt(rng.Next() * 2f),
                    profile = cluster.dominantProfile,
                    clusterId = cluster.id
                });
            }

            // Surrounding hills
            for (int i = 1; i < count; i++)
            {
                float angle = (i / (float)(count - 1)) * Mathf.PI * 2f + rng.Next() * 0.8f;
                float dist = clusterRadius * (0.5f + rng.Next() * 0.5f);

                float px = cluster.center.x + Mathf.Cos(angle) * dist;
                float py = cluster.center.y + Mathf.Sin(angle) * dist;

                float scale = 0.6f + rng.Next() * 0.2f; // 60-80% of central size
                float rx = (35f + rng.Next() * 40f) * scale;
                float ry = (22f + rng.Next() * 30f) * scale;

                SlopeProfile profile = (rng.Next() < 0.6f)
                    ? cluster.dominantProfile
                    : PickVariantProfile(rng, cluster.dominantProfile);

                var pos = ApplyNodeAvoidance(new Vector2(px, py), nodes, rng, preset);
                terrain.hills.Add(new HillData
                {
                    position = pos,
                    radiusX = rx,
                    radiusY = ry,
                    angle = rng.Next() * Mathf.PI,
                    layers = 2 + Mathf.FloorToInt(rng.Next() * 2f),
                    profile = profile,
                    clusterId = cluster.id
                });
            }
        }

        private static void GenerateValleyFramerHills(MapTerrain terrain, SeededRng rng,
            HillCluster cluster, MapPreset preset, List<MapNode> nodes)
        {
            float gapWidth = 60f + rng.Next() * 40f; // 60-100 unit valley
            float cosA = Mathf.Cos(cluster.orientationAngle);
            float sinA = Mathf.Sin(cluster.orientationAngle);

            // Two sides of the valley
            for (int side = -1; side <= 1; side += 2)
            {
                int hillsPerSide = 2 + Mathf.FloorToInt(rng.Next() * 2f); // 2-3
                float spacing = 35f + rng.Next() * 15f;
                float halfLen = (hillsPerSide - 1) * spacing * 0.5f;

                for (int i = 0; i < hillsPerSide; i++)
                {
                    float along = -halfLen + i * spacing;
                    float perp = gapWidth * 0.5f * side + (rng.Next() - 0.5f) * 10f;

                    float px = cluster.center.x + cosA * along + sinA * perp;
                    float py = cluster.center.y + sinA * along - cosA * perp;

                    // Hills elongated parallel to valley axis
                    float radiusAlong = 35f + rng.Next() * 35f;
                    float radiusCross = 20f + rng.Next() * 20f;

                    // Valley walls tend to be steep
                    SlopeProfile profile = (rng.Next() < 0.6f)
                        ? SlopeProfile.Steep
                        : cluster.dominantProfile;

                    var pos = ApplyNodeAvoidance(new Vector2(px, py), nodes, rng, preset);
                    terrain.hills.Add(new HillData
                    {
                        position = pos,
                        radiusX = radiusAlong,
                        radiusY = radiusCross,
                        angle = cluster.orientationAngle + (rng.Next() - 0.5f) * 0.2f,
                        layers = 2 + Mathf.FloorToInt(rng.Next() * 2f),
                        profile = profile,
                        clusterId = cluster.id
                    });
                }
            }
        }

        private static void GenerateSolitaryHill(MapTerrain terrain, SeededRng rng,
            HillCluster cluster, MapPreset preset, List<MapNode> nodes)
        {
            var pos = ApplyNodeAvoidance(cluster.center, nodes, rng, preset);
            terrain.hills.Add(new HillData
            {
                position = pos,
                radiusX = 35f + rng.Next() * 80f,
                radiusY = 22f + rng.Next() * 50f,
                angle = rng.Next() * Mathf.PI,
                layers = 2 + Mathf.FloorToInt(rng.Next() * 3f),
                profile = cluster.dominantProfile,
                clusterId = cluster.id
            });
        }

        // ─── Helpers ────────────────────────────────────────────────────

        private static Vector2 PickCoastAwarePosition(SeededRng rng, float w, float h, int coastSide)
        {
            float px, py;
            switch (coastSide)
            {
                case 0: // right coast: hills on left 60%
                    px = rng.Next() * w * 0.6f;
                    py = rng.Next() * h;
                    break;
                case 1: // bottom coast: hills on top 60%
                    px = rng.Next() * w;
                    py = rng.Next() * h * 0.6f;
                    break;
                case 2: // left coast: hills on right 60%
                    px = w * 0.4f + rng.Next() * w * 0.6f;
                    py = rng.Next() * h;
                    break;
                case 3: // top coast: hills on bottom 60%
                    px = rng.Next() * w;
                    py = h * 0.4f + rng.Next() * h * 0.6f;
                    break;
                default:
                    px = 20f + rng.Next() * (w - 40f);
                    py = 20f + rng.Next() * (h - 40f);
                    break;
            }
            return new Vector2(px, py);
        }

        private static Vector2 ApplyNodeAvoidance(Vector2 hillPos, List<MapNode> nodes,
            SeededRng rng, MapPreset preset)
        {
            if (nodes == null || nodes.Count == 0) return hillPos;

            float w = preset.worldWidth;
            float h = preset.worldHeight;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                float closestDist = float.MaxValue;
                Vector2 pushDir = Vector2.zero;
                foreach (var node in nodes)
                {
                    float dist = Vector2.Distance(hillPos, node.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        if (dist > 0.01f)
                            pushDir = (hillPos - node.position).normalized;
                    }
                }

                if (closestDist >= MinNodeDist) break;

                float nudge = MinNodeDist - closestDist + rng.Next() * 15f;
                hillPos += pushDir * nudge;
                hillPos.x = Mathf.Clamp(hillPos.x, 20f, w - 20f);
                hillPos.y = Mathf.Clamp(hillPos.y, 20f, h - 20f);
            }

            return hillPos;
        }

        private static ClusterType PickClusterType(SeededRng rng, GeneratorType genType)
        {
            float roll = rng.Next();
            switch (genType)
            {
                case GeneratorType.Mountain:
                    // Mountains: more ridges and valleys
                    if (roll < 0.40f) return ClusterType.Ridge;
                    if (roll < 0.60f) return ClusterType.MoundGroup;
                    if (roll < 0.85f) return ClusterType.ValleyFramer;
                    return ClusterType.Solitary;

                case GeneratorType.Rural:
                    // Rural: more gentle mound groups, some solitary
                    if (roll < 0.15f) return ClusterType.Ridge;
                    if (roll < 0.55f) return ClusterType.MoundGroup;
                    if (roll < 0.70f) return ClusterType.ValleyFramer;
                    return ClusterType.Solitary;

                case GeneratorType.Grid:
                    // Grid: balanced, slightly fewer ridges
                    if (roll < 0.20f) return ClusterType.Ridge;
                    if (roll < 0.45f) return ClusterType.MoundGroup;
                    if (roll < 0.70f) return ClusterType.ValleyFramer;
                    return ClusterType.Solitary;

                default: // Organic
                    if (roll < 0.25f) return ClusterType.Ridge;
                    if (roll < 0.55f) return ClusterType.MoundGroup;
                    if (roll < 0.75f) return ClusterType.ValleyFramer;
                    return ClusterType.Solitary;
            }
        }

        private static SlopeProfile PickDominantProfile(SeededRng rng, MapPreset preset)
        {
            float bias = preset.steepnessBias;
            float roll = rng.Next();

            switch (preset.generatorType)
            {
                case GeneratorType.Mountain:
                    // Mountains favor steep and plateau
                    if (roll < 0.30f + bias * 0.15f) return SlopeProfile.Steep;
                    if (roll < 0.50f + bias * 0.10f) return SlopeProfile.Plateau;
                    if (roll < 0.65f) return SlopeProfile.Mesa;
                    if (roll < 0.85f) return SlopeProfile.Gaussian;
                    return SlopeProfile.Gentle;

                case GeneratorType.Rural:
                    // Rural favors gentle rolling hills
                    if (roll < 0.50f - bias * 0.2f) return SlopeProfile.Gentle;
                    if (roll < 0.75f) return SlopeProfile.Gaussian;
                    if (roll < 0.90f) return SlopeProfile.Plateau;
                    return SlopeProfile.Steep;

                default: // Organic / Grid
                    if (roll < 0.35f) return SlopeProfile.Gaussian;
                    if (roll < 0.55f) return SlopeProfile.Gentle;
                    if (roll < 0.75f + bias * 0.1f) return SlopeProfile.Steep;
                    if (roll < 0.90f) return SlopeProfile.Plateau;
                    return SlopeProfile.Mesa;
            }
        }

        private static SlopeProfile PickVariantProfile(SeededRng rng, SlopeProfile dominant)
        {
            // Pick a different profile for intra-cluster variety
            float roll = rng.Next();
            switch (dominant)
            {
                case SlopeProfile.Steep:
                    return roll < 0.5f ? SlopeProfile.Gaussian : SlopeProfile.Plateau;
                case SlopeProfile.Gentle:
                    return roll < 0.5f ? SlopeProfile.Gaussian : SlopeProfile.Plateau;
                case SlopeProfile.Plateau:
                    return roll < 0.5f ? SlopeProfile.Gaussian : SlopeProfile.Mesa;
                case SlopeProfile.Mesa:
                    return roll < 0.5f ? SlopeProfile.Plateau : SlopeProfile.Steep;
                default: // Gaussian
                    return roll < 0.5f ? SlopeProfile.Gentle : SlopeProfile.Steep;
            }
        }
    }
}
