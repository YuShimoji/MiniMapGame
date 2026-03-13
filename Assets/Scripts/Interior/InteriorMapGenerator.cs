using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Orchestrates interior generation using the floor plan strategy pattern.
    /// Supports both the new context-aware API and legacy single-seed API.
    /// </summary>
    public static class InteriorMapGenerator
    {
        /// <summary>
        /// New API: context-aware generation with preset and building classification.
        /// </summary>
        public static InteriorMapData Generate(
            InteriorBuildingContext context, InteriorPreset preset, int seed)
        {
            var rng = new SeededRng(seed);
            var data = new InteriorMapData { context = context };

            int floorCount = preset.useExteriorFloorCount
                ? Mathf.Max(1, context.floors)
                : preset.overrideFloorCount;

            var generator = FloorPlanFactory.Create(context.category);

            // Generate basement floors
            for (int f = -preset.basementFloors; f < 0; f++)
            {
                var floorData = generator.Generate(rng, context, preset, f);
                var furnitureRng = new SeededRng(DeriveFurnitureSeed(seed, floorData.floorIndex, context.category));
                floorData.furniture = InteriorFurniturePlanner.Generate(furnitureRng, context, preset, floorData);
                data.floors.Add(floorData);
                data.totalRoomCount += floorData.rooms.Count;
                data.totalFurnitureCount += floorData.furniture.Count;
            }

            // Generate above-ground floors
            for (int f = 0; f < floorCount; f++)
            {
                var floorData = generator.Generate(rng, context, preset, f);
                var furnitureRng = new SeededRng(DeriveFurnitureSeed(seed, floorData.floorIndex, context.category));
                floorData.furniture = InteriorFurniturePlanner.Generate(furnitureRng, context, preset, floorData);
                data.floors.Add(floorData);
                data.totalRoomCount += floorData.rooms.Count;
                data.totalFurnitureCount += floorData.furniture.Count;
            }

            // Count discovery slots
            foreach (var floor in data.floors)
                foreach (var room in floor.rooms)
                    data.totalDiscoveryCount += room.discoverySlotCount;

            return data;
        }

        private static int DeriveFurnitureSeed(int baseSeed, int floorIndex, BuildingCategory category)
        {
            unchecked
            {
                int hash = baseSeed;
                hash = (hash * 397) ^ floorIndex;
                hash = (hash * 397) ^ (int)category;
                hash ^= 0x5bd1e995;
                if (hash == 0) hash = 1;
                return hash;
            }
        }

        // ===== Legacy API (backward compatibility) =====

        private const float MinRoomSize = 4f;
        private const float MaxRoomSize = 12f;
        private const int MaxRooms = 8;
        private const float CorridorWidth = 2f;

        /// <summary>
        /// Legacy API: generates a simple branching room layout from a seed.
        /// Used by existing InteriorController.EnterBuilding when no context is available.
        /// </summary>
        public static InteriorMapData Generate(int seed)
        {
            var rng = new SeededRng(seed);
            var data = new InteriorMapData();

            // Legacy branching room generation
            int roomCount = 4 + Mathf.FloorToInt(rng.Next() * (MaxRooms - 4));

            #pragma warning disable CS0618 // Suppress Obsolete warnings for legacy types
            data._legacyRooms.Add(new RoomNode
            {
                position = new Vector2(0f, 0f),
                size = new Vector2(rng.Range(5f, 8f), rng.Range(5f, 8f)),
                type = RoomType.Entrance
            });

            for (int i = 1; i < roomCount; i++)
            {
                int parentIdx = rng.Range(0, data._legacyRooms.Count);
                var parent = data._legacyRooms[parentIdx];

                float angle = rng.Next() * Mathf.PI * 2f;
                float dist = parent.size.magnitude * 0.5f + rng.Range(3f, 8f);
                var pos = parent.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                var size = new Vector2(rng.Range(MinRoomSize, MaxRoomSize),
                                       rng.Range(MinRoomSize, MaxRoomSize));

                RoomType type = RoomType.Normal;
                if (i == roomCount - 1 && rng.Next() > 0.5f) type = RoomType.Boss;
                else if (rng.Next() > 0.8f) type = RoomType.Treasure;

                data._legacyRooms.Add(new RoomNode { position = pos, size = size, type = type });
                data._legacyCorridors.Add(new CorridorEdge
                {
                    roomA = parentIdx,
                    roomB = i,
                    width = CorridorWidth
                });
            }

            // Extra corridors
            for (int i = 0; i < data._legacyRooms.Count; i++)
            {
                for (int j = i + 2; j < data._legacyRooms.Count; j++)
                {
                    if (rng.Next() > 0.3f) continue;
                    float d = Vector2.Distance(data._legacyRooms[i].position, data._legacyRooms[j].position);
                    if (d < 15f)
                    {
                        data._legacyCorridors.Add(new CorridorEdge { roomA = i, roomB = j, width = CorridorWidth });
                    }
                }
            }

            // Identify alcoves
            var degrees = new int[data._legacyRooms.Count];
            foreach (var c in data._legacyCorridors)
            {
                degrees[c.roomA]++;
                degrees[c.roomB]++;
            }
            for (int i = 0; i < data._legacyRooms.Count; i++)
            {
                if (degrees[i] == 1 && data._legacyRooms[i].type == RoomType.Normal)
                {
                    var room = data._legacyRooms[i];
                    room.type = RoomType.Alcove;
                    data._legacyRooms[i] = room;
                    data._legacyAlcoves.Add(i);
                }
            }
            #pragma warning restore CS0618

            return data;
        }
    }
}
