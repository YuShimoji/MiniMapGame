using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.MapGen
{
    /// <summary>
    /// Grid-based map generator. Direct port of JavaScript buildGrid() function.
    /// Creates a regular grid layout with horizontal, vertical, and diagonal connections.
    /// </summary>
    public class GridGenerator : IMapGenerator
    {
        public (List<MapNode> nodes, List<MapEdge> edges) Generate(
            SeededRng rng,
            Vector2 center,
            MapPreset preset)
        {
            var nodes = new List<MapNode>();
            var edges = new List<MapEdge>();

            // Calculate spacing and grid dimensions
            float spacing = 42f + rng.Next() * 10f;
            int cols = Mathf.FloorToInt((preset.worldWidth - 100f) / spacing);
            int rows = Mathf.FloorToInt((preset.worldHeight - 80f) / spacing);

            // Calculate starting position to center the grid
            float startX = (preset.worldWidth - cols * spacing) / 2f;
            float startY = (preset.worldHeight - rows * spacing) / 2f;

            // Create grid of nodes
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // Add jitter to break up perfect grid alignment
                    float jitterX = (rng.Next() - 0.5f) * preset.curveAmount * 12f;
                    float jitterY = (rng.Next() - 0.5f) * preset.curveAmount * 12f;

                    float x = startX + c * spacing + jitterX;
                    float y = startY + r * spacing + jitterY;

                    // Label center node
                    string label = "";
                    if (r == rows / 2 && c == cols / 2)
                    {
                        label = "中心街";
                    }

                    // Add node directly to list (not using MapGenUtils.AddNode to avoid clamping)
                    nodes.Add(new MapNode
                    {
                        position = new Vector2(x, y),
                        degree = 0,
                        label = label,
                        type = NodeType.None
                    });
                }
            }

            // Create horizontal edges (connecting adjacent columns in each row)
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int nodeA = r * cols + c;
                    int nodeB = r * cols + (c + 1);

                    // Determine tier: main streets (middle row, edge rows) = tier 0,
                    // every 4th column = tier 1, rest = tier 2
                    int tier;
                    if (r == rows / 2 || r == 1 || r == rows - 2)
                    {
                        tier = 0;
                    }
                    else if (c % 4 == 0)
                    {
                        tier = 1;
                    }
                    else
                    {
                        tier = 2;
                    }

                    MapGenUtils.AddEdge(nodes, edges, nodeA, nodeB, tier, rng, preset.curveAmount);
                }
            }

            // Create vertical edges (connecting adjacent rows in each column)
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows - 1; r++)
                {
                    int nodeA = r * cols + c;
                    int nodeB = (r + 1) * cols + c;

                    // Determine tier: main streets (middle col, edge cols) = tier 0,
                    // every 4th row = tier 1, rest = tier 2
                    int tier;
                    if (c == cols / 2 || c == 1 || c == cols - 2)
                    {
                        tier = 0;
                    }
                    else if (r % 4 == 0)
                    {
                        tier = 1;
                    }
                    else
                    {
                        tier = 2;
                    }

                    MapGenUtils.AddEdge(nodes, edges, nodeA, nodeB, tier, rng, preset.curveAmount);
                }
            }

            // Create diagonal avenue (Broadway-style) if grid is large enough
            if (rows > 4 && cols > 4)
            {
                int row = 0;
                int col = Mathf.FloorToInt(cols * 0.4f);

                while (row < rows - 1)
                {
                    int currentNode = row * cols + col;

                    // Move down one row
                    int nextRow = row + 1;

                    // Randomly move right or stay in same column
                    int nextCol = col;
                    if (rng.Next() > 0.5f && col < cols - 1)
                    {
                        nextCol = col + 1;
                    }
                    nextCol = Mathf.Clamp(nextCol, 0, cols - 1);

                    int nextNode = nextRow * cols + nextCol;

                    // Add diagonal edge with tier 0 and less curve
                    MapGenUtils.AddEdge(nodes, edges, currentNode, nextNode, 0, rng, 0.12f);

                    // Move to next position
                    row = nextRow;
                    col = nextCol;
                }
            }

            return (nodes, edges);
        }
    }
}
