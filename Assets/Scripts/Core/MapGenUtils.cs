using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    public static class MapGenUtils
    {
        public static int AddNode(List<MapNode> nodes, float x, float y,
            MapPreset preset, string label = "", NodeType type = NodeType.None)
        {
            var pos = ClampToPreset(new Vector2(x, y), preset);
            nodes.Add(new MapNode { position = pos, degree = 0, label = label, type = type });
            return nodes.Count - 1;
        }

        /// <summary>
        /// Add edge with randomized bezier control point. Updates degree on both nodes.
        /// Port of JSX addEdge().
        /// </summary>
        public static void AddEdge(List<MapNode> nodes, List<MapEdge> edges,
            int a, int b, int tier, SeededRng rng, float curveAmount = 0.5f)
        {
            var na = nodes[a];
            var nb = nodes[b];
            float mx = Mathf.Lerp(na.position.x, nb.position.x, 0.45f + (rng.Next() - 0.5f) * 0.1f)
                        + (rng.Next() - 0.5f) * 30f * curveAmount * (1f - tier * 0.25f);
            float my = Mathf.Lerp(na.position.y, nb.position.y, 0.45f + (rng.Next() - 0.5f) * 0.1f)
                        + (rng.Next() - 0.5f) * 30f * curveAmount * (1f - tier * 0.25f);

            edges.Add(new MapEdge { nodeA = a, nodeB = b, tier = tier, controlPoint = new Vector2(mx, my) });

            na.degree++;
            nb.degree++;
            nodes[a] = na;
            nodes[b] = nb;
        }

        public static float Distance(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        public static Vector2 Direction(Vector2 from, Vector2 to)
        {
            float d = Distance(from, to);
            if (d < 0.0001f) return Vector2.right;
            return (to - from) / d;
        }

        public static Vector2 Perpendicular(Vector2 dir)
        {
            return new Vector2(-dir.y, dir.x);
        }

        /// <summary>Quadratic bezier point at parameter t. Port of JSX bPt().</summary>
        public static Vector2 BezierPoint(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        /// <summary>Clamp position within preset border. Port of JSX cpt().</summary>
        public static Vector2 ClampToPreset(Vector2 p, MapPreset preset)
        {
            float pad = preset.borderPadding;
            return new Vector2(
                Mathf.Clamp(p.x, pad, preset.worldWidth - pad),
                Mathf.Clamp(p.y, pad * 0.8f, preset.worldHeight - pad * 0.8f)
            );
        }

        /// <summary>Convert JSX 2D coord to Unity world position (XZ plane, Y-inverted).</summary>
        public static Vector3 ToWorldPosition(Vector2 coord, MapPreset preset)
        {
            return new Vector3(coord.x, 0f, preset.worldHeight - coord.y);
        }

        /// <summary>Convert 2D coord to world position with elevation.</summary>
        public static Vector3 ToWorldPosition(Vector2 coord, float elevation, MapPreset preset)
        {
            return new Vector3(coord.x, elevation, preset.worldHeight - coord.y);
        }

        /// <summary>
        /// Sample elevation at parameter t along an edge, accounting for layer type.
        /// layer=0: terrain-following (lerp between node elevations or ElevationMap sample).
        /// layer=1: bridge arch (sinusoidal bulge above node elevations).
        /// layer=-1: tunnel dip (sinusoidal dip below node elevations).
        /// </summary>
        public static float SampleEdgeElevation(MapEdge edge, List<MapNode> nodes,
            float t, ElevationMap elevMap)
        {
            float elevA = nodes[edge.nodeA].elevation;
            float elevB = nodes[edge.nodeB].elevation;
            float baseElev = Mathf.Lerp(elevA, elevB, t);

            if (edge.layer == 0 && elevMap != null)
            {
                var p2d = BezierPoint(
                    nodes[edge.nodeA].position, edge.controlPoint,
                    nodes[edge.nodeB].position, t);
                baseElev = elevMap.Sample(p2d);
            }

            if (edge.layer == 1) // bridge
            {
                float arch = Mathf.Sin(t * Mathf.PI) * 4f;
                return baseElev + arch;
            }

            if (edge.layer == -1) // tunnel
            {
                float dip = Mathf.Sin(t * Mathf.PI) * 3f;
                return baseElev - dip;
            }

            return baseElev;
        }
    }
}
