using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Places decorations along roads and at notable nodes.
    /// Uses SpatialHash to avoid overlap with buildings and other decorations.
    /// </summary>
    public static class DecorationPlacer
    {
        public static List<MapDecoration> Place(
            List<MapNode> nodes, List<MapEdge> edges,
            MapAnalysis analysis, List<MapBuilding> buildings,
            SeededRng rng, MapPreset preset)
        {
            var decorations = new List<MapDecoration>();
            var hash = new SpatialHash<MapDecoration>(20f);

            // Pre-populate spatial hash with buildings for collision avoidance
            var buildingHash = new SpatialHash<MapBuilding>(40f);
            foreach (var b in buildings) buildingHash.Insert(b);

            PlaceAlongRoads(nodes, edges, rng, preset, decorations, hash, buildingHash);
            PlaceAtNodes(nodes, analysis, rng, preset, decorations, hash, buildingHash);

            return decorations;
        }

        private static void PlaceAlongRoads(
            List<MapNode> nodes, List<MapEdge> edges,
            SeededRng rng, MapPreset preset,
            List<MapDecoration> decorations,
            SpatialHash<MapDecoration> hash,
            SpatialHash<MapBuilding> buildingHash)
        {
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
                        var dec = new MapDecoration
                        {
                            position = pos,
                            type = DecorationType.StreetLight,
                            angle = Mathf.Atan2(dir.y, dir.x),
                            scale = 1.5f,
                            lodLevel = 1
                        };

                        if (!hash.Overlaps(dec) && !OverlapsBuilding(dec, buildingHash))
                        {
                            hash.Insert(dec);
                            decorations.Add(dec);
                        }
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
                        var dec = new MapDecoration
                        {
                            position = pos,
                            type = DecorationType.Tree,
                            angle = rng.Next() * Mathf.PI * 2f,
                            scale = 1.8f + rng.Next() * 1.2f,
                            lodLevel = 2
                        };

                        if (!hash.Overlaps(dec) && !OverlapsBuilding(dec, buildingHash))
                        {
                            hash.Insert(dec);
                            decorations.Add(dec);
                        }
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

                    var dec = new MapDecoration
                    {
                        position = pos,
                        type = DecorationType.Bollard,
                        angle = angle,
                        scale = 0.8f,
                        lodLevel = 2
                    };

                    if (!hash.Overlaps(dec) && !OverlapsBuilding(dec, buildingHash))
                    {
                        hash.Insert(dec);
                        decorations.Add(dec);
                    }
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

                var dec = new MapDecoration
                {
                    position = pos,
                    type = DecorationType.Bench,
                    angle = angle,
                    scale = 1.2f,
                    lodLevel = 2
                };

                if (!hash.Overlaps(dec) && !OverlapsBuilding(dec, buildingHash))
                {
                    hash.Insert(dec);
                    decorations.Add(dec);
                }
            }
        }

        private static bool OverlapsBuilding(MapDecoration dec, SpatialHash<MapBuilding> buildingHash)
        {
            // Check if decoration position falls inside any building's AABB
            var testBuilding = new MapBuilding
            {
                position = dec.position,
                width = dec.scale * 2f,
                height = dec.scale * 2f,
                angle = dec.angle
            };
            return buildingHash.Overlaps(testBuilding);
        }
    }
}
