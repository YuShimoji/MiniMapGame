using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Data
{
    [System.Serializable]
    public class MapData
    {
        public List<MapNode> nodes = new();
        public List<MapEdge> edges = new();
        public List<MapBuilding> buildings = new();
        public MapTerrain terrain = new();
        public MapAnalysis analysis = new();
        public Vector2 center;
        public int seed;
    }
}
