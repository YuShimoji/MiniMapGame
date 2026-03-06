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
        public string id;

        // ISpatialBounds
        Vector2 ISpatialBounds.Position => position;
        float ISpatialBounds.Width => width;
        float ISpatialBounds.Height => height;
        float ISpatialBounds.Angle => angle;
    }
}
