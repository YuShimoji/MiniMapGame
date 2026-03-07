using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Places buildings along road edges with collision avoidance. Port of JSX placeBuildings().
    /// </summary>
    public static class BuildingPlacer
    {
        // Road half-widths per tier [min, max]
        private static readonly float[][] TierRoadWidth = {
            new[] { 12f, 8f },
            new[] { 8f, 5f },
            new[] { 5f, 3f }
        };

        // Building width range per tier [min, max]
        private static readonly float[][] TierBuildingWidth = {
            new[] { 16f, 12f },
            new[] { 11f, 8f },
            new[] { 7f, 5f }
        };

        public static List<MapBuilding> Place(
            List<MapNode> nodes, List<MapEdge> edges, SeededRng rng, MapPreset preset)
        {
            var buildings = new List<MapBuilding>();
            var hash = new SpatialHash<MapBuilding>(40f);
            int bldId = 0;

            foreach (var seg in edges)
            {
                var na = nodes[seg.nodeA];
                var nb = nodes[seg.nodeB];
                float d = MapGenUtils.Distance(na.position, nb.position);
                if (d < 15f) continue;

                var dir = MapGenUtils.Direction(na.position, nb.position);
                var pd = MapGenUtils.Perpendicular(dir);

                int ti = Mathf.Clamp(seg.tier, 0, 2);
                float[] hw = TierRoadWidth[ti];
                float[] bw = TierBuildingWidth[ti];

                for (int side = -1; side <= 1; side += 2)
                {
                    float baseOff = (hw[0] + 3f) * side;
                    float spacing = 7f + rng.Next() * 8f;
                    int count = Mathf.FloorToInt(d / spacing);
                    int ci = Mathf.FloorToInt(rng.Next() * 2f);

                    while (ci < count)
                    {
                        if (rng.Next() > preset.buildingDensity)
                        {
                            ci++;
                            continue;
                        }

                        float t = (ci + 0.5f) / count;
                        var bp = MapGenUtils.BezierPoint(na.position, seg.controlPoint, nb.position, t);
                        float bx = bp.x + pd.x * baseOff + (rng.Next() - 0.5f) * 2.5f;
                        float by = bp.y + pd.y * baseOff + (rng.Next() - 0.5f) * 2.5f;
                        float buildW = bw[0] + rng.Next() * (bw[1] - bw[0]);
                        float buildH = bw[0] * 0.65f + rng.Next() * bw[1] * 0.5f;
                        float angle = Mathf.Atan2(dir.y, dir.x) + (rng.Next() - 0.5f) * 0.15f;
                        bool isLm = rng.Next() > 0.95f && seg.tier == 0;

                        // Floor count: tier 0 = 2-6, tier 1 = 1-4, tier 2 = 1-2, landmark = 5-10
                        int floors;
                        if (isLm)
                            floors = 5 + Mathf.FloorToInt(rng.Next() * 6f);
                        else if (ti == 0)
                            floors = 2 + Mathf.FloorToInt(rng.Next() * 5f);
                        else if (ti == 1)
                            floors = 1 + Mathf.FloorToInt(rng.Next() * 4f);
                        else
                            floors = 1 + Mathf.FloorToInt(rng.Next() * 2f);

                        // Shape type: landmarks=3(stepped), tier0=0-2 weighted, tier1-2=mostly 0(box)
                        int shapeType;
                        if (isLm)
                            shapeType = 3;
                        else if (ti == 0)
                            shapeType = Mathf.FloorToInt(rng.Next() * 3f); // 0,1,2
                        else
                            shapeType = rng.Next() > 0.85f ? Mathf.FloorToInt(rng.Next() * 2f) : 0;

                        var b = new MapBuilding
                        {
                            position = new Vector2(bx, by),
                            width = isLm ? buildW * 1.9f : buildW,
                            height = isLm ? buildH * 1.9f : buildH,
                            angle = angle,
                            tier = seg.tier,
                            isLandmark = isLm,
                            floors = floors,
                            shapeType = shapeType,
                            id = $"B{bldId++}"
                        };

                        if (!hash.Overlaps(b))
                        {
                            hash.Insert(b);
                            buildings.Add(b);
                        }

                        ci += 1 + (rng.Next() > 0.65f ? 1 : 0);
                    }
                }
            }

            return buildings;
        }
    }
}
