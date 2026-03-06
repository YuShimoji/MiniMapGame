using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Generates interior room/corridor layouts from a building seed.
    /// Uses BSP-like subdivision for room placement.
    /// </summary>
    public static class InteriorMapGenerator
    {
        private const float MinRoomSize = 4f;
        private const float MaxRoomSize = 12f;
        private const int MaxRooms = 8;
        private const float CorridorWidth = 2f;

        public static InteriorMapData Generate(int seed)
        {
            var rng = new SeededRng(seed);
            var data = new InteriorMapData();

            // Entrance room
            int roomCount = 4 + Mathf.FloorToInt(rng.Next() * (MaxRooms - 4));
            data.rooms.Add(new RoomNode
            {
                position = new Vector2(0f, 0f),
                size = new Vector2(rng.Range(5f, 8f), rng.Range(5f, 8f)),
                type = RoomType.Entrance
            });

            // Generate remaining rooms in a branching layout
            for (int i = 1; i < roomCount; i++)
            {
                int parentIdx = rng.Range(0, data.rooms.Count);
                var parent = data.rooms[parentIdx];

                float angle = rng.Next() * Mathf.PI * 2f;
                float dist = parent.size.magnitude * 0.5f + rng.Range(3f, 8f);
                var pos = parent.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                var size = new Vector2(rng.Range(MinRoomSize, MaxRoomSize),
                                       rng.Range(MinRoomSize, MaxRoomSize));

                RoomType type = RoomType.Normal;
                if (i == roomCount - 1 && rng.Next() > 0.5f) type = RoomType.Boss;
                else if (rng.Next() > 0.8f) type = RoomType.Treasure;

                data.rooms.Add(new RoomNode { position = pos, size = size, type = type });
                data.corridors.Add(new CorridorEdge
                {
                    roomA = parentIdx,
                    roomB = i,
                    width = CorridorWidth
                });
            }

            // Extra corridors for loops (30% chance per pair of nearby rooms)
            for (int i = 0; i < data.rooms.Count; i++)
            {
                for (int j = i + 2; j < data.rooms.Count; j++)
                {
                    if (rng.Next() > 0.3f) continue;
                    float d = Vector2.Distance(data.rooms[i].position, data.rooms[j].position);
                    if (d < 15f)
                    {
                        data.corridors.Add(new CorridorEdge { roomA = i, roomB = j, width = CorridorWidth });
                    }
                }
            }

            // Identify alcoves (degree == 1 rooms)
            var degrees = new int[data.rooms.Count];
            foreach (var c in data.corridors)
            {
                degrees[c.roomA]++;
                degrees[c.roomB]++;
            }
            for (int i = 0; i < data.rooms.Count; i++)
            {
                if (degrees[i] == 1 && data.rooms[i].type == RoomType.Normal)
                {
                    var room = data.rooms[i];
                    room.type = RoomType.Alcove;
                    data.rooms[i] = room;
                    data.alcoveIndices.Add(i);
                }
            }

            return data;
        }
    }
}
