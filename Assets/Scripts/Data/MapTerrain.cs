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
    }
}
