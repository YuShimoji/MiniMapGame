using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.MapGen
{
    public class MountainGenerator : IMapGenerator
    {
        public (List<MapNode> nodes, List<MapEdge> edges) Generate(SeededRng rng, Vector2 center, MapPreset preset)
        {
            var nodes = new List<MapNode>();
            var edges = new List<MapEdge>();
            float ca = preset.curveAmount;
            float w = preset.worldWidth;
            float h = preset.worldHeight;

            // 1. Winding spine top→bottom
            float x = center.x + (rng.Next() - 0.5f) * 80f;
            float y = 30f;
            var spine = new List<int>();

            while (y < h - 30f)
            {
                string label = y < 60f ? "登山口" : (y > h - 60f ? "山頂" : "");
                spine.Add(MapGenUtils.AddNode(nodes, x, y, preset, label));
                x = Mathf.Clamp(x + (rng.Next() - 0.5f) * 80f * ca, 80f, w - 80f);
                y += 38f + rng.Next() * 28f;
            }

            for (int i = 0; i < spine.Count - 1; i++)
                MapGenUtils.AddEdge(nodes, edges, spine[i], spine[i + 1], 0, rng, ca);

            // 1b. Elevation profile along spine
            float maxElev = preset.maxElevation;
            float peakPos = 0.5f + (rng.Next() - 0.5f) * 0.3f; // Peak at 35-65% along spine
            for (int i = 0; i < spine.Count; i++)
            {
                float t = spine.Count > 1 ? i / (float)(spine.Count - 1) : 0f;
                // Smooth bell curve peaking at peakPos
                float dist = Mathf.Abs(t - peakPos) / Mathf.Max(peakPos, 1f - peakPos);
                float elev = maxElev * Mathf.Exp(-dist * dist * 3f);
                var node = nodes[spine[i]];
                node.elevation = elev;
                nodes[spine[i]] = node;
            }

            // 2. Secondary ridges (1-2 branching from spine mid-section)
            int numRidges = 1 + Mathf.FloorToInt(rng.Next() * 2f);
            for (int r = 0; r < numRidges; r++)
            {
                // Pick a branch point from the middle 40-80% of spine
                int branchIdx = Mathf.FloorToInt(spine.Count * (0.4f + rng.Next() * 0.4f));
                branchIdx = Mathf.Clamp(branchIdx, 1, spine.Count - 2);
                int branchFrom = spine[branchIdx];
                float parentElev = nodes[branchFrom].elevation;

                // Ridge direction: perpendicular-ish from spine direction
                float spineAngle = Mathf.Atan2(
                    nodes[spine[Mathf.Min(branchIdx + 1, spine.Count - 1)]].position.y - nodes[branchFrom].position.y,
                    nodes[spine[Mathf.Min(branchIdx + 1, spine.Count - 1)]].position.x - nodes[branchFrom].position.x);
                float ridgeAngle = spineAngle + (rng.Next() > 0.5f ? 1f : -1f) * (Mathf.PI * 0.3f + rng.Next() * 0.4f);

                var ridge = new List<int> { branchFrom };
                int ridgeSteps = 3 + Mathf.FloorToInt(rng.Next() * 3f);
                int prev = branchFrom;

                for (int s = 0; s < ridgeSteps; s++)
                {
                    ridgeAngle += (rng.Next() - 0.5f) * 0.4f * ca;
                    float stepLen = 30f + rng.Next() * 40f;
                    float rx = nodes[prev].position.x + Mathf.Cos(ridgeAngle) * stepLen;
                    float ry = nodes[prev].position.y + Mathf.Sin(ridgeAngle) * stepLen;

                    if (rx < 50f || rx > w - 50f || ry < 30f || ry > h - 30f) break;

                    string rLabel = s == ridgeSteps - 1 ? "副峰" : "";
                    int rn = MapGenUtils.AddNode(nodes, rx, ry, preset, rLabel);

                    // Ridge elevation: descends from parent, peaks at 70% of parent
                    float ridgeT = (s + 1) / (float)ridgeSteps;
                    float ridgeElev = parentElev * (0.7f - ridgeT * 0.3f);
                    var rNode = nodes[rn];
                    rNode.elevation = ridgeElev;
                    nodes[rn] = rNode;

                    MapGenUtils.AddEdge(nodes, edges, prev, rn, 1, rng, ca);
                    ridge.Add(rn);
                    prev = rn;
                }
            }

            // 3. Dead-end branches (from both spine and ridge nodes)
            foreach (int si in spine)
            {
                if (rng.Next() >= 0.4f) continue;

                float angle = (rng.Next() - 0.5f) * Mathf.PI * 0.7f + Mathf.PI / 2f;
                float len = 35f + rng.Next() * 65f;
                float bx = nodes[si].position.x + Mathf.Cos(angle) * len;
                float by = nodes[si].position.y + Mathf.Sin(angle) * len;

                if (bx < 50f || bx > w - 50f || by < 30f || by > h - 30f) continue;

                string bLabel = rng.Next() > 0.8f ? "避難小屋" : "";
                int bn = MapGenUtils.AddNode(nodes, bx, by, preset, bLabel, NodeType.Shelter);
                var branchNode = nodes[bn];
                branchNode.elevation = nodes[si].elevation * (0.6f + rng.Next() * 0.3f);
                nodes[bn] = branchNode;
                MapGenUtils.AddEdge(nodes, edges, si, bn, 1, rng, ca);

                // Sub-branch
                if (rng.Next() > 0.6f)
                {
                    float bx2 = bx + (rng.Next() - 0.5f) * 45f;
                    float by2 = by + (rng.Next() - 0.5f) * 45f;
                    if (bx2 > 50f && bx2 < w - 50f && by2 > 30f && by2 < h - 30f)
                    {
                        int bn2 = MapGenUtils.AddNode(nodes, bx2, by2, preset);
                        var subNode = nodes[bn2];
                        subNode.elevation = branchNode.elevation * (0.5f + rng.Next() * 0.3f);
                        nodes[bn2] = subNode;
                        MapGenUtils.AddEdge(nodes, edges, bn, bn2, 2, rng, ca);
                    }
                }
            }

            return (nodes, edges);
        }
    }
}
