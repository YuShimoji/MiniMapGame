using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.MapGen
{
    public class OrganicGenerator : IMapGenerator
    {
        public (List<MapNode> nodes, List<MapEdge> edges) Generate(SeededRng rng, Vector2 center, MapPreset preset)
        {
            var nodes = new List<MapNode>();
            var edges = new List<MapEdge>();
            float ca = preset.curveAmount;

            // 1. Center hub
            int C = MapGenUtils.AddNode(nodes, center.x, center.y, preset, "市中心", NodeType.Hub);

            // 2. Arterial count
            int numA = preset.arterialRange.x +
                       rng.Range(0, preset.arterialRange.y - preset.arterialRange.x + 1);
            var arterialTips = new List<int>();

            // 3. Arterial roads
            for (int i = 0; i < numA; i++)
            {
                float baseAng = (i / (float)numA) * Mathf.PI * 2f + rng.Next() * 0.2f;
                float angle = baseAng;
                int prev = C;
                float totalLen = 150f + rng.Next() * 130f;
                int steps = 3 + Mathf.FloorToInt(rng.Next() * 3f);
                float slen = totalLen / steps;

                for (int j = 0; j < steps; j++)
                {
                    angle += (rng.Next() - 0.5f) * 0.4f * ca;
                    float nx = nodes[prev].position.x + Mathf.Cos(angle) * slen * (0.7f + rng.Next() * 0.6f);
                    float ny = nodes[prev].position.y + Mathf.Sin(angle) * slen * (0.7f + rng.Next() * 0.6f);

                    // Snap to existing node if close
                    int snapped = -1;
                    for (int k = 0; k < nodes.Count; k++)
                    {
                        if (k != prev && MapGenUtils.Distance(nodes[k].position, new Vector2(nx, ny)) < 20f)
                        {
                            snapped = k;
                            break;
                        }
                    }

                    int n = snapped >= 0
                        ? snapped
                        : MapGenUtils.AddNode(nodes, nx, ny, preset, j == steps - 1 ? $"門{i + 1}" : "");
                    MapGenUtils.AddEdge(nodes, edges, prev, n, 0, rng, ca);
                    if (j == steps - 1) arterialTips.Add(n);
                    prev = n;
                }
            }

            // 4. Ring road
            if (preset.hasRingRoad && arterialTips.Count >= 3)
            {
                arterialTips.Sort((a, b) =>
                {
                    float angA = Mathf.Atan2(nodes[a].position.y - center.y, nodes[a].position.x - center.x);
                    float angB = Mathf.Atan2(nodes[b].position.y - center.y, nodes[b].position.x - center.x);
                    return angA.CompareTo(angB);
                });
                for (int i = 0; i < arterialTips.Count; i++)
                {
                    int j = (i + 1) % arterialTips.Count;
                    if (MapGenUtils.Distance(nodes[arterialTips[i]].position, nodes[arterialTips[j]].position) < 280f)
                        MapGenUtils.AddEdge(nodes, edges, arterialTips[i], arterialTips[j], 0, rng, ca * 0.6f);
                }
            }

            // 5. Secondary / tertiary roads
            int t0Count = edges.Count;
            for (int ei = 0; ei < t0Count; ei++)
            {
                var seg = edges[ei];
                if (seg.tier != 0) continue;
                int numB = Mathf.FloorToInt(rng.Next() * 3f);

                for (int b = 0; b < numB; b++)
                {
                    if (rng.Next() > 0.7f) continue;
                    var na = nodes[seg.nodeA];
                    var nb = nodes[seg.nodeB];
                    float t = 0.2f + rng.Next() * 0.6f;
                    float bx = Mathf.Lerp(na.position.x, nb.position.x, t) + (rng.Next() - 0.5f) * 10f;
                    float by = Mathf.Lerp(na.position.y, nb.position.y, t) + (rng.Next() - 0.5f) * 10f;

                    bool tooClose = false;
                    for (int k = 0; k < nodes.Count; k++)
                    {
                        if (MapGenUtils.Distance(nodes[k].position, new Vector2(bx, by)) < 13f)
                        { tooClose = true; break; }
                    }
                    if (tooClose) continue;

                    int bRoot = MapGenUtils.AddNode(nodes, bx, by, preset);
                    MapGenUtils.AddEdge(nodes, edges, seg.nodeA, bRoot, 1, rng, ca);

                    float brAngle = Mathf.Atan2(nb.position.y - na.position.y, nb.position.x - na.position.x)
                                    + (rng.Next() > 0.5f ? 1f : -1f) * (Mathf.PI * 0.4f + rng.Next() * 0.3f);
                    int prev = bRoot;
                    int brSteps = 2 + Mathf.FloorToInt(rng.Next() * 4f);

                    for (int j = 0; j < brSteps; j++)
                    {
                        brAngle += (rng.Next() - 0.5f) * 0.35f * ca;
                        float len = 28f + rng.Next() * 60f;
                        float nnx = nodes[prev].position.x + Mathf.Cos(brAngle) * len;
                        float nny = nodes[prev].position.y + Mathf.Sin(brAngle) * len;

                        bool ok = true;
                        for (int k = 0; k < nodes.Count; k++)
                        {
                            if (MapGenUtils.Distance(nodes[k].position, new Vector2(nnx, nny)) < 11f)
                            { ok = false; break; }
                        }
                        if (!ok) break;

                        int nn = MapGenUtils.AddNode(nodes, nnx, nny, preset);
                        MapGenUtils.AddEdge(nodes, edges, prev, nn, 1, rng, ca);

                        // Tertiary sub-branch
                        if (rng.Next() > 0.6f)
                        {
                            float aa = brAngle + (rng.Next() > 0.5f ? 1f : -1f) * (0.4f + rng.Next() * 0.5f);
                            float ax = nodes[prev].position.x + Mathf.Cos(aa) * (18f + rng.Next() * 38f);
                            float ay = nodes[prev].position.y + Mathf.Sin(aa) * (18f + rng.Next() * 38f);
                            bool ok2 = true;
                            for (int k = 0; k < nodes.Count; k++)
                            {
                                if (MapGenUtils.Distance(nodes[k].position, new Vector2(ax, ay)) < 10f)
                                { ok2 = false; break; }
                            }
                            if (ok2)
                            {
                                int an = MapGenUtils.AddNode(nodes, ax, ay, preset);
                                MapGenUtils.AddEdge(nodes, edges, prev, an, 2, rng, ca);
                            }
                        }
                        prev = nn;
                    }
                }
            }

            return (nodes, edges);
        }
    }
}
