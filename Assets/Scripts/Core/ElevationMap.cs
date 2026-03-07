using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Samples terrain elevation from HillData using Gaussian falloff.
    /// Used by MapRenderer and BuildingSpawner for Y-positioning.
    /// </summary>
    public class ElevationMap
    {
        private readonly List<HillData> _hills;
        private readonly float _scale;
        private readonly float _maxElevation;

        public ElevationMap(MapTerrain terrain, MapPreset preset)
        {
            _hills = terrain?.hills ?? new List<HillData>();
            _scale = preset != null ? preset.elevationScale : 1f;
            _maxElevation = preset != null ? preset.maxElevation : 15f;
        }

        /// <summary>
        /// Sample elevation at a 2D map position.
        /// Each hill contributes elevation using Gaussian falloff based on distance.
        /// </summary>
        public float Sample(Vector2 pos)
        {
            if (_hills.Count == 0) return 0f;

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

                // Gaussian falloff: peak at center, zero at ~2 radii
                float influence = Mathf.Exp(-distSq * 1.5f);

                // Height proportional to hill size and layer count
                float hillHeight = hill.layers * 2f * _scale;
                totalElev += influence * hillHeight;
            }

            return Mathf.Min(totalElev, _maxElevation);
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
    }
}
