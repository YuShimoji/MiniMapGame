using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public struct MapEdge
    {
        public int nodeA;
        public int nodeB;
        public int tier;
        public Vector2 controlPoint;
    }
}
