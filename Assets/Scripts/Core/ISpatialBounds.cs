using UnityEngine;

namespace MiniMapGame.Core
{
    public interface ISpatialBounds
    {
        Vector2 Position { get; }
        float Width { get; }
        float Height { get; }
        float Angle { get; }
    }
}
