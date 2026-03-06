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
        public static MapTerrain Generate(SeededRng rng, Vector2 center, MapPreset preset)
        {
            var terrain = new MapTerrain();
            GenerateCoast(terrain, rng, preset);
            GenerateRiver(terrain, rng, center, preset);
            GenerateHills(terrain, rng, preset);
            return terrain;
        }

        private static void GenerateCoast(MapTerrain terrain, SeededRng rng, MapPreset preset)
        {
            if (!preset.hasCoast) return;

            float w = preset.worldWidth;
            float h = preset.worldHeight;

            // Initial polygon points (top-right corner water area)
            terrain.coastPoints.Add(new Vector2(w * (0.62f + rng.Next() * 0.1f), 0f));
            terrain.coastPoints.Add(new Vector2(w, 0f));
            terrain.coastPoints.Add(new Vector2(w, h));

            // Wavy coastline from bottom to top
            float y = h;
            while (y > 0f)
            {
                terrain.coastPoints.Add(new Vector2(
                    w * (0.68f + (rng.Next() - 0.5f) * 0.12f), y));
                y -= 25f + rng.Next() * 30f;
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

        private static void GenerateHills(MapTerrain terrain, SeededRng rng, MapPreset preset)
        {
            float w = preset.worldWidth;
            float h = preset.worldHeight;
            int numHills = Mathf.FloorToInt(preset.hillDensity * (8f + rng.Next() * 8f));

            for (int i = 0; i < numHills; i++)
            {
                terrain.hills.Add(new HillData
                {
                    position = new Vector2(rng.Next() * w * 0.8f, rng.Next() * h),
                    radiusX = 35f + rng.Next() * 80f,
                    radiusY = 22f + rng.Next() * 50f,
                    angle = rng.Next() * Mathf.PI,
                    layers = 2 + Mathf.FloorToInt(rng.Next() * 3f)
                });
            }
        }
    }
}
