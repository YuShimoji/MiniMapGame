using System;
using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    // ===== New Interior Data Types =====

    [Serializable]
    public class InteriorMapData
    {
        public InteriorBuildingContext context;
        public List<InteriorFloorData> floors = new();
        public int totalRoomCount;
        public int totalDiscoveryCount;
        public int totalFurnitureCount;
    }

    [Serializable]
    public class InteriorFloorData
    {
        public int floorIndex;              // 0 = ground, -1 = basement, 1+ = upper
        public List<InteriorRoom> rooms = new();
        public List<InteriorDoor> doors = new();
        public List<InteriorCorridor> corridors = new();
        public List<InteriorFurniture> furniture = new();
        public Vector2 floorBounds;         // Usable area dimensions
        public float deadSpaceRatio;        // Actual dead space ratio achieved
    }

    [Serializable]
    public struct InteriorRoom
    {
        public int id;
        public InteriorRoomType type;
        public Vector2 position;            // Center, relative to floor origin
        public Vector2 size;
        public float rotation;              // For non-axis-aligned rooms (ruins)
        public int discoverySlotCount;      // How many discovery items can spawn
        public bool isSecret;               // Hidden room
    }

    [Serializable]
    public struct InteriorDoor
    {
        public int roomA;
        public int roomB;
        public Vector2 position;            // Position on the wall
        public float width;
        public bool isHidden;               // Secret door
        public bool isLocked;               // Requires interaction to open
    }

    [Serializable]
    public struct InteriorCorridor
    {
        public int roomA;
        public int roomB;
        public float width;
        public Vector2[] waypoints;         // For L-shaped or curved corridors
    }

    [Serializable]
    public struct InteriorFurniture
    {
        public int roomId;
        public FurnitureType type;
        public Vector2 position;
        public float angle;
        public float scale;
    }

}
