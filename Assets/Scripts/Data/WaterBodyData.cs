using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public class WaterBodyData
    {
        public WaterBodyType bodyType;
        public List<Vector2> pathPoints = new();
        public List<float> widths = new();
        public List<float> depths = new();
        public int coastSide = -1; // For coast only: 0=right, 1=bottom, 2=left, 3=top
        public float flowDirection; // Angle in radians (for rivers/streams)
        public float baseElevation;

        /// <summary>
        /// Quick AABB check for spatial queries. Computed after generation.
        /// </summary>
        public Vector2 boundsMin;
        public Vector2 boundsMax;

        public void ComputeBounds()
        {
            if (pathPoints.Count == 0) return;

            boundsMin = new Vector2(float.MaxValue, float.MaxValue);
            boundsMax = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < pathPoints.Count; i++)
            {
                var p = pathPoints[i];
                float expand = (i < widths.Count) ? widths[i] * 0.5f : 0f;
                if (p.x - expand < boundsMin.x) boundsMin.x = p.x - expand;
                if (p.y - expand < boundsMin.y) boundsMin.y = p.y - expand;
                if (p.x + expand > boundsMax.x) boundsMax.x = p.x + expand;
                if (p.y + expand > boundsMax.y) boundsMax.y = p.y + expand;
            }
        }

        public bool BoundsContains(Vector2 pos)
        {
            return pos.x >= boundsMin.x && pos.x <= boundsMax.x &&
                   pos.y >= boundsMin.y && pos.y <= boundsMax.y;
        }
    }
}
