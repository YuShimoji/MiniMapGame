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

        // Legacy compatibility: flat room/corridor access for single-floor use
        [Obsolete("Use floors[0].rooms instead")]
        public List<RoomNode> rooms
        {
            get
            {
                if (floors.Count == 0) return _legacyRooms;
                var list = new List<RoomNode>();
                foreach (var r in floors[0].rooms)
                {
                    list.Add(new RoomNode
                    {
                        position = r.position,
                        size = r.size,
                        type = MapLegacyRoomType(r.type)
                    });
                }
                return list;
            }
        }

        [Obsolete("Use floors[0].corridors instead")]
        public List<CorridorEdge> corridors
        {
            get
            {
                if (floors.Count == 0) return _legacyCorridors;
                var list = new List<CorridorEdge>();
                foreach (var c in floors[0].corridors)
                {
                    list.Add(new CorridorEdge
                    {
                        roomA = c.roomA,
                        roomB = c.roomB,
                        width = c.width
                    });
                }
                return list;
            }
        }

        [Obsolete("Use discovery system instead")]
        public List<int> alcoveIndices
        {
            get
            {
                if (floors.Count == 0) return _legacyAlcoves;
                var list = new List<int>();
                if (floors.Count > 0)
                {
                    for (int i = 0; i < floors[0].rooms.Count; i++)
                    {
                        if (floors[0].rooms[i].type == InteriorRoomType.SecretRoom)
                            list.Add(i);
                    }
                }
                return list;
            }
        }

        // Backing fields for legacy Generate(int seed) path
        internal List<RoomNode> _legacyRooms = new();
        internal List<CorridorEdge> _legacyCorridors = new();
        internal List<int> _legacyAlcoves = new();

        private static RoomType MapLegacyRoomType(InteriorRoomType type)
        {
            return type switch
            {
                InteriorRoomType.Entrance => RoomType.Entrance,
                InteriorRoomType.Vault => RoomType.Treasure,
                InteriorRoomType.SecretRoom => RoomType.Alcove,
                _ => RoomType.Normal
            };
        }
    }

    [Serializable]
    public class InteriorFloorData
    {
        public int floorIndex;              // 0 = ground, -1 = basement, 1+ = upper
        public List<InteriorRoom> rooms = new();
        public List<InteriorDoor> doors = new();
        public List<InteriorCorridor> corridors = new();
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

    // ===== Legacy Types (kept for backward compatibility) =====

    [Obsolete("Use InteriorRoom with InteriorRoomType instead")]
    [Serializable]
    public struct RoomNode
    {
        public Vector2 position;
        public Vector2 size;
        public RoomType type;
    }

    [Obsolete("Use InteriorCorridor instead")]
    [Serializable]
    public struct CorridorEdge
    {
        public int roomA;
        public int roomB;
        public float width;
    }

    [Obsolete("Use InteriorRoomType instead")]
    public enum RoomType
    {
        Normal,
        Entrance,
        Boss,
        Treasure,
        Alcove
    }
}
