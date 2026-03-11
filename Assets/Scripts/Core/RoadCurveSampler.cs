using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Samples road edge Bezier curves into 2D polylines for mask baking.
    /// Operates in map-space (not world-space) to match ElevationMap coordinates.
    /// </summary>
    public static class RoadCurveSampler
    {
        /// <summary>
        /// Sample the Bezier curve of an edge into 2D map-space points.
        /// </summary>
        public static void Sample2D(MapEdge edge, List<MapNode> nodes, int segments,
            List<Vector2> outPoints)
        {
            var posA = nodes[edge.nodeA].position;
            var posB = nodes[edge.nodeB].position;
            var ctrl = edge.controlPoint;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                outPoints.Add(MapGenUtils.BezierPoint(posA, ctrl, posB, t));
            }
        }
    }
}
