using UnityEngine;
using MiniMapGame.Core;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public struct MapBuilding : ISpatialBounds
    {
        public Vector2 position;
        public float width;
        public float height;
        public float angle;
        public int tier;
        public bool isLandmark;
        public int floors; // 1-based floor count for height variation
        public int shapeType; // 0=box, 1=L-shape, 2=cylinder, 3=stepped
        public string id;

        // ISpatialBounds
        Vector2 ISpatialBounds.Position => position;
        float ISpatialBounds.Width => width;
        float ISpatialBounds.Height => height;
        float ISpatialBounds.Angle => angle;
    }
}
