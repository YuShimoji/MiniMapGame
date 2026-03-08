using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public class MapTerrain
    {
        public List<WaterBodyData> waterBodies = new();
        public List<HillData> hills = new();
        public List<HillCluster> hillClusters = new();
        public int coastSide = -1; // 0=right, 1=bottom, 2=left, 3=top, -1=none
    }
}
