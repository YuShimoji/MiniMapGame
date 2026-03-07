using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Detects crossing edges in 2D and assigns layer values (bridge/tunnel).
    /// Lower-tier (wider) roads stay at ground level; higher-tier roads become bridges.
    /// </summary>
    public static class BridgeTunnelDetector
    {
        private const int SegmentsPerEdge = 8;
        private const float BridgeHeight = 4f;

        public static void Detect(MapData data, MapPreset preset)
        {
            if (!preset.enableBridges && !preset.enableTunnels) return;

            var crossings = FindCrossings(data);

            foreach (var (edgeA, edgeB) in crossings)
            {
                var eA = data.edges[edgeA];
                var eB = data.edges[edgeB];

                // Already assigned?
                if (eA.layer != 0 || eB.layer != 0) continue;

                // Lower tier (wider road) stays on ground
                int bridgeEdge, groundEdge;
                if (eA.tier <= eB.tier)
                {
                    groundEdge = edgeA;
                    bridgeEdge = edgeB;
                }
                else
                {
                    groundEdge = edgeB;
                    bridgeEdge = edgeA;
                }

                if (preset.enableBridges)
                {
                    var bridge = data.edges[bridgeEdge];
                    bridge.layer = 1;
                    data.edges[bridgeEdge] = bridge;

                    // Raise bridge endpoint nodes
                    RaiseNode(data.nodes, bridge.nodeA, BridgeHeight);
                    RaiseNode(data.nodes, bridge.nodeB, BridgeHeight);
                }
                else if (preset.enableTunnels)
                {
                    var tunnel = data.edges[bridgeEdge];
                    tunnel.layer = -1;
                    data.edges[bridgeEdge] = tunnel;
                }
            }
        }

        private static List<(int, int)> FindCrossings(MapData data)
        {
            var results = new List<(int, int)>();
            var edgeSegments = new List<List<Vector2>>(data.edges.Count);

            // Approximate bezier curves as line segments
            for (int i = 0; i < data.edges.Count; i++)
            {
                var edge = data.edges[i];
                var posA = data.nodes[edge.nodeA].position;
                var posB = data.nodes[edge.nodeB].position;
                var segments = new List<Vector2>(SegmentsPerEdge + 1);

                for (int s = 0; s <= SegmentsPerEdge; s++)
                {
                    float t = s / (float)SegmentsPerEdge;
                    segments.Add(MapGenUtils.BezierPoint(posA, edge.controlPoint, posB, t));
                }
                edgeSegments.Add(segments);
            }

            // Check all edge pairs for crossing
            for (int i = 0; i < data.edges.Count; i++)
            {
                for (int j = i + 1; j < data.edges.Count; j++)
                {
                    // Skip edges sharing a node
                    var eI = data.edges[i];
                    var eJ = data.edges[j];
                    if (eI.nodeA == eJ.nodeA || eI.nodeA == eJ.nodeB ||
                        eI.nodeB == eJ.nodeA || eI.nodeB == eJ.nodeB)
                        continue;

                    if (SegmentsCross(edgeSegments[i], edgeSegments[j]))
                        results.Add((i, j));
                }
            }

            return results;
        }

        private static bool SegmentsCross(List<Vector2> segsA, List<Vector2> segsB)
        {
            for (int i = 0; i < segsA.Count - 1; i++)
            {
                for (int j = 0; j < segsB.Count - 1; j++)
                {
                    if (LineSegmentsIntersect(segsA[i], segsA[i + 1], segsB[j], segsB[j + 1]))
                        return true;
                }
            }
            return false;
        }

        private static bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float d1 = Cross(b2 - b1, a1 - b1);
            float d2 = Cross(b2 - b1, a2 - b1);
            float d3 = Cross(a2 - a1, b1 - a1);
            float d4 = Cross(a2 - a1, b2 - a1);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            return false;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static void RaiseNode(List<MapNode> nodes, int index, float minElevation)
        {
            var node = nodes[index];
            if (node.elevation < minElevation)
                node.elevation = minElevation;
            nodes[index] = node;
        }
    }
}
