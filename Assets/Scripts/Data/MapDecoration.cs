using UnityEngine;
using MiniMapGame.Core;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public struct MapDecoration : ISpatialBounds
    {
        public Vector2 position;
        public DecorationType type;
        public float angle;
        public float scale;
        public int lodLevel; // 0=always, 1=medium, 2=close only

        // ISpatialBounds — small fixed footprint
        Vector2 ISpatialBounds.Position => position;
        float ISpatialBounds.Width => scale * 2f;
        float ISpatialBounds.Height => scale * 2f;
        float ISpatialBounds.Angle => angle;
    }
}
