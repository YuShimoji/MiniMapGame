using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.MapGen
{
    public class RuralGenerator : IMapGenerator
    {
        public (List<MapNode> nodes, List<MapEdge> edges) Generate(SeededRng rng, Vector2 center, MapPreset preset)
        {
            var nodes = new List<MapNode>();
            var edges = new List<MapEdge>();
            float ca = preset.curveAmount;
            float w = preset.worldWidth;
            float h = preset.worldHeight;

            // 1. Center hub
            int C = MapGenUtils.AddNode(nodes, center.x, center.y, preset, "村", NodeType.Hub);

            // 2. Arterial count
            int numA = preset.arterialRange.x +
                       Mathf.FloorToInt(rng.Next() * (preset.arterialRange.y - preset.arterialRange.x + 1));

            // 3. Radial roads
            for (int i = 0; i < numA; i++)
            {
                float baseAngle = (i / (float)numA) * Mathf.PI * 2f + rng.Next() * 0.4f;
                int prev = C;
                int steps = 4 + Mathf.FloorToInt(rng.Next() * 4f);
                float len = (200f + rng.Next() * 120f) / steps;

                for (int j = 0; j < steps; j++)
                {
                    float na = baseAngle + (rng.Next() - 0.5f) * 0.25f * ca;
                    float nx = nodes[prev].position.x + Mathf.Cos(na) * len * (0.6f + rng.Next() * 0.8f);
                    float ny = nodes[prev].position.y + Mathf.Sin(na) * len * (0.6f + rng.Next() * 0.8f);

                    // Skip if too close to existing node
                    bool ok = true;
                    for (int k = 0; k < nodes.Count; k++)
                    {
                        if (MapGenUtils.Distance(nodes[k].position, new Vector2(nx, ny)) < 18f)
                        { ok = false; break; }
                    }
                    if (!ok) break;

                    string label = (j == steps - 1 && rng.Next() > 0.4f) ? "農場" : "";
                    int tier = j < 2 ? 0 : 1;
                    int n = MapGenUtils.AddNode(nodes, nx, ny, preset, label,
                        label == "農場" ? NodeType.Farm : NodeType.None);
                    MapGenUtils.AddEdge(nodes, edges, prev, n, tier, rng, ca);

                    // Side branch
                    if (j > 0 && rng.Next() > 0.75f)
                    {
                        float bAng = na + (rng.Next() > 0.5f ? 1f : -1f) * (0.5f + rng.Next() * 0.6f);
                        float bLen = 25f + rng.Next() * 55f;
                        float bx = nodes[n].position.x + Mathf.Cos(bAng) * bLen;
                        float by = nodes[n].position.y + Mathf.Sin(bAng) * bLen;

                        if (bx > 50f && bx < w - 50f && by > 40f && by < h - 40f)
                        {
                            int bn = MapGenUtils.AddNode(nodes, bx, by, preset);
                            MapGenUtils.AddEdge(nodes, edges, n, bn, 2, rng, ca);
                        }
                    }

                    prev = n;
                }
            }

            return (nodes, edges);
        }
    }
}
