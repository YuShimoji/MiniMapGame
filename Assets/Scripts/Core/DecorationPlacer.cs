using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Places decorations along roads, at notable nodes, and across terrain.
    /// Uses SpatialHash to avoid overlap with buildings and other decorations.
    /// </summary>
    public static class DecorationPlacer
    {
        public static List<MapDecoration> Place(
            List<MapNode> nodes, List<MapEdge> edges,
            MapAnalysis analysis, List<MapBuilding> buildings,
            SeededRng rng, MapPreset preset,
            ElevationMap elevationMap = null,
            MapTerrain terrain = null)
        {
            var decorations = new List<MapDecoration>();
            var hash = new SpatialHash<MapDecoration>(20f);

            // Pre-populate spatial hash with buildings for collision avoidance
            var buildingHash = new SpatialHash<MapBuilding>(40f);
            foreach (var b in buildings) buildingHash.Insert(b);

            PlaceAlongRoads(nodes, edges, rng, preset, decorations, hash, buildingHash);
            PlaceAtNodes(nodes, analysis, rng, preset, decorations, hash, buildingHash);

            if (elevationMap != null && terrain != null)
                PlaceOnTerrain(rng, preset, elevationMap, terrain, decorations, hash, buildingHash);

            return decorations;
        }

        private static void PlaceAlongRoads(
            List<MapNode> nodes, List<MapEdge> edges,
            SeededRng rng, MapPreset preset,
            List<MapDecoration> decorations,
            SpatialHash<MapDecoration> hash,
            SpatialHash<MapBuilding> buildingHash)
        {
            bool isRuralOrMountain = preset.generatorType == GeneratorType.Rural
                || preset.generatorType == GeneratorType.Mountain;

            foreach (var edge in edges)
            {
                if (edge.layer != 0) continue; // Skip bridges/tunnels

                int ti = Mathf.Clamp(edge.tier, 0, 2);
                var na = nodes[edge.nodeA];
                var nb = nodes[edge.nodeB];
                float d = MapGenUtils.Distance(na.position, nb.position);
                if (d < 25f) continue;

                var dir = MapGenUtils.Direction(na.position, nb.position);
                var perp = MapGenUtils.Perpendicular(dir);

                // Street lights: tier 0-1, spacing 35-45
                if (ti <= 1)
                {
                    float spacing = 35f + rng.Next() * 10f;
                    int count = Mathf.FloorToInt(d / spacing);
                    for (int i = 0; i < count; i++)
                    {
                        if (rng.Next() > preset.decorationDensity) continue;

                        float t = (i + 0.5f) / count;
                        var bp = MapGenUtils.BezierPoint(na.position, edge.controlPoint, nb.position, t);
                        int side = rng.Next() > 0.5f ? 1 : -1;
                        float offset = (ti == 0 ? 10f : 6f) * side;

                        var pos = new Vector2(bp.x + perp.x * offset, bp.y + perp.y * offset);
                        TryPlace(decorations, hash, buildingHash, new MapDecoration
                        {
                            position = pos,
                            type = DecorationType.StreetLight,
                            angle = Mathf.Atan2(dir.y, dir.x),
                            scale = 1.5f,
                            lodLevel = 1
                        });
                    }
                }

                // Trees: tier 0 only, spacing 20-30
                if (ti == 0)
                {
                    float spacing = 20f + rng.Next() * 10f;
                    int count = Mathf.FloorToInt(d / spacing);
                    for (int i = 0; i < count; i++)
                    {
                        if (rng.Next() > preset.decorationDensity * 0.8f) continue;

                        float t = (i + 0.3f) / count;
                        var bp = MapGenUtils.BezierPoint(na.position, edge.controlPoint, nb.position, t);
                        int side = rng.Next() > 0.5f ? 1 : -1;
                        float offset = 8f * side;

                        var pos = new Vector2(bp.x + perp.x * offset, bp.y + perp.y * offset);
                        bool placed = TryPlace(decorations, hash, buildingHash, new MapDecoration
                        {
                            position = pos,
                            type = DecorationType.Tree,
                            angle = rng.Next() * Mathf.PI * 2f,
                            scale = 1.8f + rng.Next() * 1.2f,
                            lodLevel = 2
                        });

                        // Stump near trees (Rural only, 20% chance)
                        if (placed && preset.generatorType == GeneratorType.Rural && rng.Next() < 0.2f)
                        {
                            float stumpAngle = rng.Next() * Mathf.PI * 2f;
                            float stumpOffset = 4f + rng.Next() * 3f;
                            var stumpPos = new Vector2(
                                pos.x + Mathf.Cos(stumpAngle) * stumpOffset,
                                pos.y + Mathf.Sin(stumpAngle) * stumpOffset);
                            TryPlace(decorations, hash, buildingHash, new MapDecoration
                            {
                                position = stumpPos,
                                type = DecorationType.Stump,
                                angle = rng.Next() * Mathf.PI * 2f,
                                scale = 0.5f + rng.Next() * 0.3f,
                                lodLevel = 2
                            });
                        }
                    }
                }

                // Fence: tier 1-2 roads, Rural/Mountain only
                if (ti >= 1 && isRuralOrMountain && rng.Next() < preset.decorationDensity * 0.4f)
                {
                    float spacing = 25f + rng.Next() * 10f;
                    int count = Mathf.FloorToInt(d / spacing);
                    int fenceSide = rng.Next() > 0.5f ? 1 : -1;

                    for (int i = 0; i < count; i++)
                    {
                        if (rng.Next() > preset.decorationDensity * 0.6f) continue;

                        float t = (i + 0.5f) / count;
                        var bp = MapGenUtils.BezierPoint(na.position, edge.controlPoint, nb.position, t);
                        float offset = (ti == 1 ? 8f : 5f) * fenceSide;

                        var pos = new Vector2(bp.x + perp.x * offset, bp.y + perp.y * offset);
                        TryPlace(decorations, hash, buildingHash, new MapDecoration
                        {
                            position = pos,
                            type = DecorationType.Fence,
                            angle = Mathf.Atan2(dir.y, dir.x),
                            scale = 1.0f,
                            lodLevel = 1
                        });
                    }
                }
            }
        }

        private static void PlaceAtNodes(
            List<MapNode> nodes, MapAnalysis analysis,
            SeededRng rng, MapPreset preset,
            List<MapDecoration> decorations,
            SpatialHash<MapDecoration> hash,
            SpatialHash<MapBuilding> buildingHash)
        {
            bool isRuralOrMountain = preset.generatorType == GeneratorType.Rural
                || preset.generatorType == GeneratorType.Mountain;

            // Bollards at intersections and plazas
            foreach (int idx in analysis.intersectionIndices)
            {
                if (rng.Next() > preset.decorationDensity) continue;
                var node = nodes[idx];

                int bollardCount = analysis.plazaIndices.Contains(idx) ? 6 : 3;
                float radius = analysis.plazaIndices.Contains(idx) ? 8f : 5f;

                for (int i = 0; i < bollardCount; i++)
                {
                    float angle = (i / (float)bollardCount) * Mathf.PI * 2f
                        + rng.Next() * 0.5f;
                    var pos = new Vector2(
                        node.position.x + Mathf.Cos(angle) * radius,
                        node.position.y + Mathf.Sin(angle) * radius);

                    TryPlace(decorations, hash, buildingHash, new MapDecoration
                    {
                        position = pos,
                        type = DecorationType.Bollard,
                        angle = angle,
                        scale = 0.8f,
                        lodLevel = 2
                    });
                }

                // SignPost at Mountain/Rural intersections
                if (isRuralOrMountain && rng.Next() < preset.decorationDensity * 0.5f)
                {
                    float spAngle = rng.Next() * Mathf.PI * 2f;
                    var spPos = new Vector2(
                        node.position.x + Mathf.Cos(spAngle) * (radius + 2f),
                        node.position.y + Mathf.Sin(spAngle) * (radius + 2f));
                    TryPlace(decorations, hash, buildingHash, new MapDecoration
                    {
                        position = spPos,
                        type = DecorationType.SignPost,
                        angle = spAngle,
                        scale = 1.2f,
                        lodLevel = 1
                    });
                }
            }

            // Benches at dead ends
            foreach (int idx in analysis.deadEndIndices)
            {
                if (rng.Next() > preset.decorationDensity * 1.2f) continue;
                var node = nodes[idx];

                float angle = rng.Next() * Mathf.PI * 2f;
                var pos = new Vector2(
                    node.position.x + Mathf.Cos(angle) * 5f,
                    node.position.y + Mathf.Sin(angle) * 5f);

                TryPlace(decorations, hash, buildingHash, new MapDecoration
                {
                    position = pos,
                    type = DecorationType.Bench,
                    angle = angle,
                    scale = 1.2f,
                    lodLevel = 2
                });
            }
        }

        private static void PlaceOnTerrain(
            SeededRng rng, MapPreset preset,
            ElevationMap elevationMap, MapTerrain terrain,
            List<MapDecoration> decorations,
            SpatialHash<MapDecoration> hash,
            SpatialHash<MapBuilding> buildingHash)
        {
            float w = preset.worldWidth;
            float h = preset.worldHeight;
            float density = Mathf.Max(preset.decorationDensity, 0.1f);
            float cellSize = 15f / density;
            int xCells = Mathf.CeilToInt(w / cellSize);
            int yCells = Mathf.CeilToInt(h / cellSize);

            for (int gx = 0; gx < xCells; gx++)
            {
                for (int gy = 0; gy < yCells; gy++)
                {
                    float px = (gx + rng.Next()) * cellSize;
                    float py = (gy + rng.Next()) * cellSize;
                    if (px < 20f || px > w - 20f || py < 20f || py > h - 20f) continue;

                    var pos = new Vector2(px, py);

                    // Skip if inside coast water area
                    if (WaterGenerator.IsOnWaterSide(pos, terrain.coastSide, terrain.waterBodies)) continue;

                    float elev = elevationMap.Sample(pos);
                    float slope = elevationMap.SampleSlope(pos);
                    float waterDist = MinDistToWater(pos, terrain);

                    var result = SelectTerrainDecoration(rng, elev, slope, waterDist, density);
                    if (result == null) continue;

                    var (type, scale, lodLevel) = result.Value;

                    TryPlace(decorations, hash, buildingHash, new MapDecoration
                    {
                        position = pos,
                        type = type,
                        angle = rng.Next() * Mathf.PI * 2f,
                        scale = scale,
                        lodLevel = lodLevel
                    });
                }
            }
        }

        private static (DecorationType type, float scale, int lodLevel)? SelectTerrainDecoration(
            SeededRng rng, float elev, float slope, float waterDist, float density)
        {
            float roll = rng.Next();

            // High elevation + steep slope -> Rock/Boulder
            if (elev > 5f && slope > 0.5f)
            {
                if (roll < 0.4f * density)
                {
                    if (slope > 1.0f)
                        return (DecorationType.Boulder, 1.5f + rng.Next() * 1.5f, 1);
                    return (DecorationType.Rock, 0.8f + rng.Next() * 0.7f, 2);
                }
                return null;
            }

            // Hill edges (moderate elevation, moderate slope) -> Shrub
            if (elev > 2f && elev < 8f && slope > 0.2f && slope < 0.8f)
            {
                if (roll < 0.3f * density)
                    return (DecorationType.Shrub, 1.0f + rng.Next() * 1.0f, 1);
                return null;
            }

            // Lowlands near water -> Wildflower
            if (elev < 2f && waterDist < 60f && waterDist > 10f)
            {
                if (roll < 0.35f * density)
                    return (DecorationType.Wildflower, 0.6f + rng.Next() * 0.6f, 2);
                return null;
            }

            // Flat lowlands -> GrassClump
            if (elev < 3f && slope < 0.15f)
            {
                if (roll < 0.25f * density)
                    return (DecorationType.GrassClump, 0.5f + rng.Next() * 0.5f, 2);
                return null;
            }

            return null;
        }

        private static bool TryPlace(List<MapDecoration> decorations,
            SpatialHash<MapDecoration> hash, SpatialHash<MapBuilding> buildingHash,
            MapDecoration dec)
        {
            if (hash.Overlaps(dec) || OverlapsBuilding(dec, buildingHash))
                return false;
            hash.Insert(dec);
            decorations.Add(dec);
            return true;
        }

        private static bool OverlapsBuilding(MapDecoration dec, SpatialHash<MapBuilding> buildingHash)
        {
            var testBuilding = new MapBuilding
            {
                position = dec.position,
                width = dec.scale * 2f,
                height = dec.scale * 2f,
                angle = dec.angle
            };
            return buildingHash.Overlaps(testBuilding);
        }

        private static float MinDistToWater(Vector2 pos, MapTerrain terrain)
        {
            return WaterGenerator.MinDistToWater(pos, terrain.waterBodies);
        }
    }
}
