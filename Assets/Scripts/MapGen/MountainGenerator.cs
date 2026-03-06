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

            // 2. Dead-end branches
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
                MapGenUtils.AddEdge(nodes, edges, si, bn, 1, rng, ca);

                // Sub-branch
                if (rng.Next() > 0.6f)
                {
                    float bx2 = bx + (rng.Next() - 0.5f) * 45f;
                    float by2 = by + (rng.Next() - 0.5f) * 45f;
                    if (bx2 > 50f && bx2 < w - 50f && by2 > 30f && by2 < h - 30f)
                    {
                        int bn2 = MapGenUtils.AddNode(nodes, bx2, by2, preset);
                        MapGenUtils.AddEdge(nodes, edges, bn, bn2, 2, rng, ca);
                    }
                }
            }

            return (nodes, edges);
        }
    }
}
