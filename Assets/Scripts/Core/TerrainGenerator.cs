using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Generates terrain features (coast, river, hills). Port of JSX genTerrain().
    /// </summary>
    public static class TerrainGenerator
    {
        public static MapTerrain Generate(SeededRng rng, Vector2 center, MapPreset preset,
            List<MapNode> nodes = null)
        {
            var terrain = new MapTerrain();
            GenerateCoast(terrain, rng, preset);
            GenerateRiver(terrain, rng, center, preset);
            GenerateHills(terrain, rng, preset, nodes);
            return terrain;
        }

        private static void GenerateCoast(MapTerrain terrain, SeededRng rng, MapPreset preset)
        {
            if (!preset.hasCoast) return;

            float w = preset.worldWidth;
            float h = preset.worldHeight;

            // Randomly choose coast side: 0=right, 1=bottom, 2=left, 3=top
            int side = Mathf.FloorToInt(rng.Next() * 4f);
            terrain.coastSide = side;

            switch (side)
            {
                case 0: // Right edge (original)
                    GenerateCoastRight(terrain, rng, w, h);
                    break;
                case 1: // Bottom edge
                    GenerateCoastBottom(terrain, rng, w, h);
                    break;
                case 2: // Left edge
                    GenerateCoastLeft(terrain, rng, w, h);
                    break;
                case 3: // Top edge
                    GenerateCoastTop(terrain, rng, w, h);
                    break;
            }
        }

        private static void GenerateCoastRight(MapTerrain terrain, SeededRng rng, float w, float h)
        {
            terrain.coastPoints.Add(new Vector2(w * (0.62f + rng.Next() * 0.1f), 0f));
            terrain.coastPoints.Add(new Vector2(w, 0f));
            terrain.coastPoints.Add(new Vector2(w, h));
            float y = h;
            while (y > 0f)
            {
                terrain.coastPoints.Add(new Vector2(
                    w * (0.68f + (rng.Next() - 0.5f) * 0.12f), y));
                y -= 25f + rng.Next() * 30f;
            }
        }

        private static void GenerateCoastBottom(MapTerrain terrain, SeededRng rng, float w, float h)
        {
            terrain.coastPoints.Add(new Vector2(0f, h * (0.62f + rng.Next() * 0.1f)));
            terrain.coastPoints.Add(new Vector2(0f, h));
            terrain.coastPoints.Add(new Vector2(w, h));
            float x = w;
            while (x > 0f)
            {
                terrain.coastPoints.Add(new Vector2(
                    x, h * (0.68f + (rng.Next() - 0.5f) * 0.12f)));
                x -= 25f + rng.Next() * 30f;
            }
        }

        private static void GenerateCoastLeft(MapTerrain terrain, SeededRng rng, float w, float h)
        {
            terrain.coastPoints.Add(new Vector2(w * (0.38f - rng.Next() * 0.1f), 0f));
            terrain.coastPoints.Add(new Vector2(0f, 0f));
            terrain.coastPoints.Add(new Vector2(0f, h));
            float y = h;
            while (y > 0f)
            {
                terrain.coastPoints.Add(new Vector2(
                    w * (0.32f + (rng.Next() - 0.5f) * 0.12f), y));
                y -= 25f + rng.Next() * 30f;
            }
        }

        private static void GenerateCoastTop(MapTerrain terrain, SeededRng rng, float w, float h)
        {
            terrain.coastPoints.Add(new Vector2(0f, h * (0.38f - rng.Next() * 0.1f)));
            terrain.coastPoints.Add(new Vector2(0f, 0f));
            terrain.coastPoints.Add(new Vector2(w, 0f));
            float x = w;
            while (x > 0f)
            {
                terrain.coastPoints.Add(new Vector2(
                    x, h * (0.32f + (rng.Next() - 0.5f) * 0.12f)));
                x -= 25f + rng.Next() * 30f;
            }
        }

        private static void GenerateRiver(MapTerrain terrain, SeededRng rng, Vector2 center, MapPreset preset)
        {
            if (!preset.hasRiver) return;

            float w = preset.worldWidth;
            float h = preset.worldHeight;
            float rx = center.x * (0.5f + rng.Next() * 0.4f);
            float ry = -5f;
            float sway = preset.generatorType == GeneratorType.Rural ? 35f : 55f;

            while (ry <= h + 10f)
            {
                terrain.riverPoints.Add(new Vector2(
                    Mathf.Clamp(rx, 40f, w * 0.78f), ry));
                rx += (rng.Next() - 0.5f) * sway;
                ry += 20f + rng.Next() * 35f;
            }
        }

        private static void GenerateHills(MapTerrain terrain, SeededRng rng, MapPreset preset,
            List<MapNode> nodes = null)
        {
            float w = preset.worldWidth;
            float h = preset.worldHeight;
            int numHills = Mathf.FloorToInt(preset.hillDensity * (8f + rng.Next() * 8f));
            float minNodeDist = 30f; // Minimum distance from hill center to any road node

            for (int i = 0; i < numHills; i++)
            {
                // Generate hill position, avoiding coast side
                float px, py;
                switch (terrain.coastSide)
                {
                    case 0: // right coast → hills on left 60%
                        px = rng.Next() * w * 0.6f;
                        py = rng.Next() * h;
                        break;
                    case 1: // bottom coast → hills on top 60%
                        px = rng.Next() * w;
                        py = rng.Next() * h * 0.6f;
                        break;
                    case 2: // left coast → hills on right 60%
                        px = w * 0.4f + rng.Next() * w * 0.6f;
                        py = rng.Next() * h;
                        break;
                    case 3: // top coast → hills on bottom 60%
                        px = rng.Next() * w;
                        py = h * 0.4f + rng.Next() * h * 0.6f;
                        break;
                    default:
                        px = rng.Next() * w * 0.8f;
                        py = rng.Next() * h;
                        break;
                }

                // Push hill away from dense node clusters
                if (nodes != null && nodes.Count > 0)
                {
                    var hillPos = new Vector2(px, py);
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        float closestDist = float.MaxValue;
                        Vector2 pushDir = Vector2.zero;
                        foreach (var node in nodes)
                        {
                            float dist = Vector2.Distance(hillPos, node.position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                if (dist > 0.01f)
                                    pushDir = (hillPos - node.position).normalized;
                            }
                        }

                        if (closestDist >= minNodeDist) break;

                        // Nudge hill away from nearest node
                        float nudge = minNodeDist - closestDist + rng.Next() * 15f;
                        hillPos += pushDir * nudge;
                        hillPos.x = Mathf.Clamp(hillPos.x, 20f, w - 20f);
                        hillPos.y = Mathf.Clamp(hillPos.y, 20f, h - 20f);
                    }
                    px = hillPos.x;
                    py = hillPos.y;
                }

                terrain.hills.Add(new HillData
                {
                    position = new Vector2(px, py),
                    radiusX = 35f + rng.Next() * 80f,
                    radiusY = 22f + rng.Next() * 50f,
                    angle = rng.Next() * Mathf.PI,
                    layers = 2 + Mathf.FloorToInt(rng.Next() * 3f)
                });
            }
        }
    }
}
