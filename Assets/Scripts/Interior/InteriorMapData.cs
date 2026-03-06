using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Interior
{
    [System.Serializable]
    public class InteriorMapData
    {
        public List<RoomNode> rooms = new();
        public List<CorridorEdge> corridors = new();
        public List<int> alcoveIndices = new();
    }

    [System.Serializable]
    public struct RoomNode
    {
        public Vector2 position;
        public Vector2 size;
        public RoomType type;
    }

    [System.Serializable]
    public struct CorridorEdge
    {
        public int roomA;
        public int roomB;
        public float width;
    }

    public enum RoomType
    {
        Normal,
        Entrance,
        Boss,
        Treasure,
        Alcove
    }
}
