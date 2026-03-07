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

            // Create diagonal avenues (2 crossing avenues for larger grids)
            if (rows > 4 && cols > 4)
            {
                // Avenue 1: top-left → bottom-right
                CreateDiagonalAvenue(nodes, edges, rng, rows, cols,
                    0, Mathf.FloorToInt(cols * 0.3f), 1, 1);

                // Avenue 2: top-right → bottom-left (if grid large enough)
                if (rows > 6 && cols > 6)
                {
                    CreateDiagonalAvenue(nodes, edges, rng, rows, cols,
                        0, Mathf.FloorToInt(cols * 0.7f), 1, -1);
                }
            }

            // Mark plaza nodes (avenue intersections / center area)
            int centerNode = (rows / 2) * cols + (cols / 2);
            if (centerNode < nodes.Count)
            {
                var cn = nodes[centerNode];
                cn.type = NodeType.Hub;
                nodes[centerNode] = cn;
            }

            return (nodes, edges);
        }

        private static void CreateDiagonalAvenue(
            List<MapNode> nodes, List<MapEdge> edges, SeededRng rng,
            int rows, int cols, int startRow, int startCol, int rowDir, int colDir)
        {
            int row = startRow;
            int col = startCol;

            while (row >= 0 && row < rows - 1 && col >= 0 && col < cols)
            {
                int currentNode = row * cols + col;
                int nextRow = row + rowDir;
                int nextCol = col;

                if (rng.Next() > 0.4f)
                    nextCol = col + colDir;
                nextCol = Mathf.Clamp(nextCol, 0, cols - 1);
                nextRow = Mathf.Clamp(nextRow, 0, rows - 1);
                if (nextRow == row && nextCol == col) break;

                int nextNode = nextRow * cols + nextCol;
                MapGenUtils.AddEdge(nodes, edges, currentNode, nextNode, 0, rng, 0.12f);

                row = nextRow;
                col = nextCol;
            }
        }
    }
}
