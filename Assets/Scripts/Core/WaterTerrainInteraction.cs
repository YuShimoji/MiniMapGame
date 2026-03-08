using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Applies water-driven terrain carving to ElevationMap.
    /// Rivers carve Gaussian valleys; coasts create gentle shore slopes.
    /// </summary>
    public static class WaterTerrainInteraction
    {
        public static void ApplyWaterCarving(
            ElevationMap elevMap,
            List<WaterBodyData> waterBodies,
            MapPreset preset)
        {
            if (elevMap == null || waterBodies == null) return;

            var profile = preset.waterProfile ?? WaterProfile.CreateDefaultFallback();

            foreach (var body in waterBodies)
            {
                switch (body.bodyType)
                {
                    case WaterBodyType.River:
                    case WaterBodyType.Stream:
                        CarveRiver(elevMap, body, profile.river);
                        break;
                    case WaterBodyType.Coast:
                        CarveCoast(elevMap, body, profile.coast, preset);
                        break;
                }
            }
        }

        private static void CarveRiver(
            ElevationMap elevMap,
            WaterBodyData river,
            WaterProfile.RiverConfig config)
        {
            if (config.terrainCarveStrength <= 0f) return;

            float radius = config.terrainCarveRadius;
            float strength = config.terrainCarveStrength;
            int count = river.pathPoints.Count;

            for (int i = 0; i < count; i++)
            {
                float t = (count > 1) ? (float)i / (count - 1) : 0f;
                float width = (i < river.widths.Count) ? river.widths[i] : config.baseWidth;
                // Carve radius scales with river width
                float carveRadius = Mathf.Max(radius, width * 1.5f);
                // Depth increases downstream (source shallow → mouth deep)
                float depth = (i < river.depths.Count) ? river.depths[i] : config.depthBase;
                float downstreamScale = 1f + t * 0.6f;
                float carveDepth = depth * strength * downstreamScale;

                elevMap.AddCarving(new CarvingData
                {
                    position = river.pathPoints[i],
                    radius = carveRadius,
                    depth = carveDepth,
                    falloffPower = 2.0f // Quadratic: smooth valley walls
                });
            }
        }

        private static void CarveCoast(
            ElevationMap elevMap,
            WaterBodyData coast,
            WaterProfile.CoastConfig config,
            MapPreset preset)
        {
            if (config.terrainCarveStrength <= 0f) return;

            float radius = config.terrainCarveRadius;
            float strength = config.terrainCarveStrength;

            // Coast carving: gentle slope toward shore edge
            // Sample inland points near the coast boundary
            float worldW = preset.worldWidth;
            float worldH = preset.worldHeight;

            // Determine shore direction from coastSide
            Vector2 shoreDir;
            switch (coast.coastSide)
            {
                case 0: shoreDir = Vector2.right; break;   // right coast
                case 1: shoreDir = Vector2.down; break;     // bottom coast
                case 2: shoreDir = Vector2.left; break;     // left coast
                case 3: shoreDir = Vector2.up; break;       // top coast
                default: return;
            }

            // Walk along coast boundary, sample every ~30 units for adequate density
            float accumDist = 0f;
            const float sampleInterval = 30f;
            Vector2 prevPt = coast.pathPoints.Count > 0 ? coast.pathPoints[0] : Vector2.zero;

            for (int i = 0; i < coast.pathPoints.Count; i++)
            {
                var pt = coast.pathPoints[i];
                accumDist += Vector2.Distance(pt, prevPt);
                prevPt = pt;

                if (i > 0 && accumDist < sampleInterval) continue;
                accumDist = 0f;

                float depth = (i < coast.depths.Count) ? coast.depths[i] : config.depthBase;

                // Shore boundary carving (gentle slope toward water)
                elevMap.AddCarving(new CarvingData
                {
                    position = pt,
                    radius = radius,
                    depth = depth * strength * 0.5f,
                    falloffPower = 1.5f // Gentle falloff
                });

                // Secondary carving further inland for gradual transition
                Vector2 inlandPt = pt - shoreDir * radius * 0.4f;
                elevMap.AddCarving(new CarvingData
                {
                    position = inlandPt,
                    radius = radius * 0.6f,
                    depth = depth * strength * 0.2f,
                    falloffPower = 1.2f // Very gentle
                });
            }
        }
    }
}
