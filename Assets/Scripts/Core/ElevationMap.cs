using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Data for a terrain depression caused by water bodies.
    /// </summary>
    [System.Serializable]
    public struct CarvingData
    {
        public Vector2 position;
        public float radius;
        public float depth;
        public float falloffPower; // 1 = linear, 2 = quadratic, higher = sharper edge
    }

    /// <summary>
    /// Samples terrain elevation from HillData using variable falloff profiles.
    /// Supports water-driven carving depressions subtracted from hill elevation.
    /// Used by MapRenderer and BuildingSpawner for Y-positioning.
    /// </summary>
    public class ElevationMap
    {
        private readonly List<HillData> _hills;
        private readonly float _scale;
        private readonly float _maxElevation;
        private readonly List<CarvingData> _carvings = new();

        public ElevationMap(MapTerrain terrain, MapPreset preset)
        {
            _hills = terrain?.hills ?? new List<HillData>();
            _scale = preset != null ? preset.elevationScale : 1f;
            _maxElevation = preset != null ? preset.maxElevation : 15f;
        }

        /// <summary>
        /// Add a terrain carving (depression) at the given position.
        /// Carvings are subtracted from hill elevation during sampling.
        /// </summary>
        public void AddCarving(CarvingData carving)
        {
            _carvings.Add(carving);
        }

        /// <summary>
        /// Sample elevation at a 2D map position.
        /// Each hill contributes elevation using its assigned slope profile.
        /// Carvings are subtracted to create valleys and shore slopes.
        /// </summary>
        public float Sample(Vector2 pos)
        {
            float totalElev = 0f;

            foreach (var hill in _hills)
            {
                // Rotate position into hill's local space
                float cos = Mathf.Cos(-hill.angle);
                float sin = Mathf.Sin(-hill.angle);
                float dx = pos.x - hill.position.x;
                float dy = pos.y - hill.position.y;
                float lx = dx * cos - dy * sin;
                float ly = dx * sin + dy * cos;

                // Normalized distance in elliptical space
                float nx = lx / Mathf.Max(hill.radiusX, 1f);
                float ny = ly / Mathf.Max(hill.radiusY, 1f);
                float distSq = nx * nx + ny * ny;

                if (distSq > 4f) continue; // Skip hills too far away

                float influence = ComputeFalloff(distSq, hill.profile);

                // Height proportional to hill size and layer count
                float hillHeight = hill.layers * 2f * _scale;
                totalElev += influence * hillHeight;
            }

            totalElev = Mathf.Min(totalElev, _maxElevation);

            // Subtract water carvings
            if (_carvings.Count > 0)
            {
                float totalCarve = 0f;
                foreach (var carving in _carvings)
                {
                    float dist = Vector2.Distance(pos, carving.position);
                    if (dist >= carving.radius) continue;

                    float t = dist / carving.radius;
                    float falloff = 1f - Mathf.Pow(t, carving.falloffPower);
                    totalCarve += carving.depth * falloff;
                }
                totalElev = Mathf.Max(0f, totalElev - totalCarve);
            }

            return totalElev;
        }

        /// <summary>
        /// Estimate slope magnitude at a position using central differences.
        /// Returns 0 for flat terrain, higher values for steeper slopes.
        /// </summary>
        public float SampleSlope(Vector2 pos)
        {
            const float delta = 2.0f;
            float ex = Sample(new Vector2(pos.x + delta, pos.y));
            float wx = Sample(new Vector2(pos.x - delta, pos.y));
            float ny = Sample(new Vector2(pos.x, pos.y + delta));
            float sy = Sample(new Vector2(pos.x, pos.y - delta));

            float dzdx = (ex - wx) / (2f * delta);
            float dzdy = (ny - sy) / (2f * delta);
            return Mathf.Sqrt(dzdx * dzdx + dzdy * dzdy);
        }

        /// <summary>
        /// Apply terrain elevation to all nodes.
        /// Nodes with existing non-zero elevation (e.g. from MountainGenerator) are preserved.
        /// </summary>
        public void ApplyToNodes(List<MapNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.elevation == 0f)
                    node.elevation = Sample(node.position);
                nodes[i] = node;
            }
        }

        private static float ComputeFalloff(float distSq, SlopeProfile profile)
        {
            switch (profile)
            {
                case SlopeProfile.Steep:
                    return Mathf.Exp(-distSq * 3.0f);

                case SlopeProfile.Gentle:
                    return Mathf.Exp(-distSq * 0.7f);

                case SlopeProfile.Plateau:
                    // Flat top within normalized dist 0.3, then steep falloff
                    if (distSq < 0.09f) return 1.0f;
                    float platDist = (distSq - 0.09f) / (4.0f - 0.09f);
                    return Mathf.Max(0f, 1.0f - platDist * platDist * 3.0f);

                case SlopeProfile.Mesa:
                    // Hard flat top within 0.4 radius, then near-vertical drop
                    if (distSq < 0.16f) return 1.0f;
                    float mesaDist = Mathf.Sqrt(distSq) - 0.4f;
                    return Mathf.Max(0f, Mathf.Exp(-mesaDist * mesaDist * 20f));

                case SlopeProfile.Gaussian:
                default:
                    return Mathf.Exp(-distSq * 1.5f);
            }
        }
    }
}
