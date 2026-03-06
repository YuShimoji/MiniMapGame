using System.Collections.Generic;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Analyzes graph structure for tactical features. Port of JSX analyze().
    /// </summary>
    public static class MapAnalyzer
    {
        public static MapAnalysis Analyze(List<MapNode> nodes, List<MapEdge> edges)
        {
            var analysis = new MapAnalysis();

            for (int i = 0; i < nodes.Count; i++)
            {
                int deg = nodes[i].degree;
                if (deg == 1) analysis.deadEndIndices.Add(i);
                if (deg >= 3) analysis.intersectionIndices.Add(i);
                if (deg >= 4) analysis.plazaIndices.Add(i);
            }

            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (e.tier > 1) continue;
                if (nodes[e.nodeA].degree > 2 || nodes[e.nodeB].degree > 2) continue;
                if (MapGenUtils.Distance(nodes[e.nodeA].position, nodes[e.nodeB].position) <= 32f) continue;
                analysis.chokeEdgeIndices.Add(i);
            }

            return analysis;
        }
    }
}
