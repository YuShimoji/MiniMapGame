using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public struct HillData
    {
        public Vector2 position;
        public float radiusX;
        public float radiusY;
        public float angle;
        public int layers;
    }
}
