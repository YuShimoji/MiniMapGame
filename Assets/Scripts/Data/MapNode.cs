using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public struct MapNode
    {
        public Vector2 position;
        public int degree;
        public string label;
        public NodeType type;
    }
}
