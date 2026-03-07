using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public class MapTerrain
    {
        public List<Vector2> coastPoints = new();
        public List<Vector2> riverPoints = new();
        public List<HillData> hills = new();
        public int coastSide = -1; // 0=right, 1=bottom, 2=left, 3=top, -1=none
    }
}
