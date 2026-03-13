using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Generates floor plans for residential buildings (apartments, houses, studios).
    /// </summary>
    public class ResidentialFloorPlan : IFloorPlanGenerator
    {
        public InteriorFloorData Generate(SeededRng rng, InteriorBuildingContext context, InteriorPreset preset, int floorIndex)
        {
            var data = new InteriorFloorData
            {
                floorIndex = floorIndex
            };

            Rect footprint = FloorPlanUtils.CalculateFootprint(context);
            data.floorBounds = new Vector2(footprint.width, footprint.height);

            List<Rect> availableRects = new List<Rect> { footprint };
            List<InteriorRoom> rooms = new List<InteriorRoom>();
            int roomIdCounter = 0;

            // 1. Carve entrance on ground floor
            if (floorIndex == 0)
            {
                var entrance = CarveEntrance(ref availableRects, rng, preset.doorWidth, ref roomIdCounter);
                rooms.Add(entrance);
            }

            // 2. Optionally carve central hallway
            if (EstimateRoomCount(availableRects, preset) > 4 && rng.Next() < 0.6f)
            {
                var hallway = CarveHallway(ref availableRects, rng, preset.corridorWidth, ref roomIdCounter);
                if (hallway.HasValue)
                {
                    rooms.Add(hallway.Value);
                }
            }

            // 3. Add stairwell if not ground floor
            if (floorIndex != 0)
            {
                var stairwell = CarveStairwell(ref availableRects, rng, preset.minRoomSize, ref roomIdCounter);
                rooms.Add(stairwell);
            }

            // 4. BSP subdivide remaining space
            var subdivided = new List<Rect>();
            foreach (var rect in availableRects)
            {
                subdivided.AddRange(FloorPlanUtils.Subdivide(
                    rect, rng, preset.minRoomSize, preset.maxRoomSize,
                    maxDepth: 3, preset.irregularity));
            }

            // 5. Convert to rooms and assign types
            var newRooms = FloorPlanUtils.RectsToRooms(subdivided, roomIdCounter);
            AssignResidentialTypes(newRooms, floorIndex, rng, preset);
            rooms.AddRange(newRooms);

            data.rooms = rooms;

            // 6. Place doors
            var adjacentPairs = FloorPlanUtils.FindAdjacentPairs(rooms, preset.doorWidth);
            data.doors = FloorPlanUtils.PlaceDoors(rooms, adjacentPairs, rng, preset.doorWidth);

            // 7. Ensure connectivity
            data.corridors = FloorPlanUtils.EnsureConnectivity(rooms, adjacentPairs, preset.corridorWidth);

            // 8. Insert dead space
            FloorPlanUtils.InsertDeadSpace(rooms, rng, preset.deadSpaceRatio, preset.wallVoidProbability);
            data.deadSpaceRatio = CalculateActualDeadSpace(rooms, data.floorBounds);

            // 9. Apply secret rooms
            ApplySecretRooms(rooms, adjacentPairs, data.doors, rng, preset.secretRoomProbability);

            // 10. Set discovery slots
            AssignDiscoverySlots(rooms, preset.discoveryDensity, rng);

            return data;
        }

        private InteriorRoom CarveEntrance(ref List<Rect> available, SeededRng rng, float doorWidth, ref int idCounter)
        {
            var largest = available.OrderByDescending(r => r.width * r.height).First();
            available.Remove(largest);

            // Place entrance at one edge
            bool horizontal = largest.width > largest.height;
            float entranceSize = doorWidth * 3f;

            Rect entranceRect, remainder;
            if (horizontal)
            {
                float x = rng.Next() < 0.5f ? largest.xMin : largest.xMax - entranceSize;
                entranceRect = new Rect(x, largest.yMin, entranceSize, largest.height);
                remainder = new Rect(
                    x == largest.xMin ? largest.xMin + entranceSize : largest.xMin,
                    largest.yMin,
                    largest.width - entranceSize,
                    largest.height);
            }
            else
            {
                float y = rng.Next() < 0.5f ? largest.yMin : largest.yMax - entranceSize;
                entranceRect = new Rect(largest.xMin, y, largest.width, entranceSize);
                remainder = new Rect(
                    largest.xMin,
                    y == largest.yMin ? largest.yMin + entranceSize : largest.yMin,
                    largest.width,
                    largest.height - entranceSize);
            }

            available.Add(remainder);
            return RectToRoom(entranceRect, InteriorRoomType.Entrance, idCounter++);
        }

        private InteriorRoom? CarveHallway(ref List<Rect> available, SeededRng rng, float corridorWidth, ref int idCounter)
        {
            var largest = available.OrderByDescending(r => r.width * r.height).FirstOrDefault();
            if (largest.width < corridorWidth * 4f && largest.height < corridorWidth * 4f) return null;

            available.Remove(largest);
            bool horizontal = largest.width > largest.height;

            Rect hallwayRect, remainder1, remainder2;
            if (horizontal)
            {
                float centerY = largest.center.y;
                hallwayRect = new Rect(largest.xMin, centerY - corridorWidth * 0.5f, largest.width, corridorWidth);
                remainder1 = new Rect(largest.xMin, largest.yMin, largest.width, centerY - corridorWidth * 0.5f - largest.yMin);
                remainder2 = new Rect(largest.xMin, centerY + corridorWidth * 0.5f, largest.width, largest.yMax - (centerY + corridorWidth * 0.5f));
            }
            else
            {
                float centerX = largest.center.x;
                hallwayRect = new Rect(centerX - corridorWidth * 0.5f, largest.yMin, corridorWidth, largest.height);
                remainder1 = new Rect(largest.xMin, largest.yMin, centerX - corridorWidth * 0.5f - largest.xMin, largest.height);
                remainder2 = new Rect(centerX + corridorWidth * 0.5f, largest.yMin, largest.xMax - (centerX + corridorWidth * 0.5f), largest.height);
            }

            if (remainder1.width > 0.5f && remainder1.height > 0.5f) available.Add(remainder1);
            if (remainder2.width > 0.5f && remainder2.height > 0.5f) available.Add(remainder2);

            return RectToRoom(hallwayRect, InteriorRoomType.Hallway, idCounter++);
        }

        private InteriorRoom CarveStairwell(ref List<Rect> available, SeededRng rng, float minSize, ref int idCounter)
        {
            var largest = available.OrderByDescending(r => r.width * r.height).First();
            available.Remove(largest);

            float stairSize = Mathf.Max(minSize, 2f);
            Rect stairRect = new Rect(largest.xMin, largest.yMin, stairSize, stairSize);
            Rect remainder = new Rect(largest.xMin + stairSize, largest.yMin, largest.width - stairSize, largest.height);

            if (remainder.width > 0.5f && remainder.height > 0.5f) available.Add(remainder);
            return RectToRoom(stairRect, InteriorRoomType.Stairwell, idCounter++);
        }

        private void AssignResidentialTypes(List<InteriorRoom> rooms, int floorIndex, SeededRng rng, InteriorPreset preset)
        {
            if (rooms.Count == 0) return;

            // Preserve room list order so room.id and room index remain aligned for doors/corridors.
            var sortedIds = rooms
                .OrderByDescending(r => r.size.x * r.size.y)
                .Select(r => r.id)
                .ToList();

            if (floorIndex == 0)
            {
                // Ground floor: LivingRoom (largest), Kitchen, Bathroom, DiningRoom/Storage
                SetRoomType(rooms, sortedIds[0], InteriorRoomType.LivingRoom);
                if (sortedIds.Count > 1) SetRoomType(rooms, sortedIds[1], InteriorRoomType.Kitchen);
                if (sortedIds.Count > 2) SetRoomType(rooms, sortedIds[2], InteriorRoomType.Bathroom);
                for (int i = 3; i < sortedIds.Count; i++)
                {
                    SetRoomType(
                        rooms,
                        sortedIds[i],
                        rng.Next() < 0.5f ? InteriorRoomType.DiningRoom : InteriorRoomType.Storage);
                }
            }
            else if (floorIndex > 0)
            {
                // Upper floors: Bedrooms, Bathroom, Storage
                SetRoomType(rooms, sortedIds[0], InteriorRoomType.Bedroom);
                if (sortedIds.Count > 1) SetRoomType(rooms, sortedIds[1], InteriorRoomType.Bathroom);
                for (int i = 2; i < sortedIds.Count; i++)
                {
                    SetRoomType(
                        rooms,
                        sortedIds[i],
                        rng.Next() < 0.6f ? InteriorRoomType.Bedroom : InteriorRoomType.Storage);
                }
            }
            else
            {
                // Basement: Storage, Utility, Basement
                for (int i = 0; i < sortedIds.Count; i++)
                {
                    float roll = rng.Next();
                    if (roll < 0.5f) SetRoomType(rooms, sortedIds[i], InteriorRoomType.Storage);
                    else if (roll < 0.8f) SetRoomType(rooms, sortedIds[i], InteriorRoomType.Utility);
                    else SetRoomType(rooms, sortedIds[i], InteriorRoomType.Basement);
                }
            }
        }

        private void ApplySecretRooms(List<InteriorRoom> rooms, List<(int, int)> pairs, List<InteriorDoor> doors, SeededRng rng, float probability)
        {
            if (probability <= 0f) return;

            var connectivity = BuildConnectivityMap(rooms.Count, pairs);
            for (int i = 0; i < rooms.Count; i++)
            {
                if (connectivity[i].Count == 1 && rooms[i].size.magnitude < 6f && rng.Next() < probability)
                {
                    var room = rooms[i];
                    room.isSecret = true;
                    rooms[i] = room;

                    // Hide door to this room
                    for (int d = 0; d < doors.Count; d++)
                    {
                        if (doors[d].roomA == i || doors[d].roomB == i)
                        {
                            var door = doors[d];
                            door.isHidden = true;
                            doors[d] = door;
                        }
                    }
                }
            }
        }

        private void AssignDiscoverySlots(List<InteriorRoom> rooms, float density, SeededRng rng)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                float area = room.size.x * room.size.y;
                int baseSlots = Mathf.FloorToInt(area * density * 0.1f);
                room.discoverySlotCount = Mathf.Max(0, baseSlots + rng.Range(-1, 2));
                rooms[i] = room;
            }
        }

        private int EstimateRoomCount(List<Rect> rects, InteriorPreset preset)
        {
            float totalArea = rects.Sum(r => r.width * r.height);
            float avgRoomSize = (preset.minRoomSize + preset.maxRoomSize) * 0.5f;
            return Mathf.FloorToInt(totalArea / (avgRoomSize * avgRoomSize));
        }

        private InteriorRoom RectToRoom(Rect rect, InteriorRoomType type, int id)
        {
            return new InteriorRoom
            {
                id = id,
                type = type,
                position = rect.center,
                size = rect.size,
                rotation = 0f,
                discoverySlotCount = 0,
                isSecret = false
            };
        }

        private void SetRoomType(List<InteriorRoom> rooms, int roomId, InteriorRoomType type)
        {
            int index = rooms.FindIndex(r => r.id == roomId);
            if (index < 0) return;

            var room = rooms[index];
            room.type = type;
            rooms[index] = room;
        }

        private float CalculateActualDeadSpace(List<InteriorRoom> rooms, Vector2 floorBounds)
        {
            float totalArea = floorBounds.x * floorBounds.y;
            float usedArea = rooms.Where(r => r.type != InteriorRoomType.WallVoid).Sum(r => r.size.x * r.size.y);
            return 1f - (usedArea / totalArea);
        }

        private Dictionary<int, List<int>> BuildConnectivityMap(int roomCount, List<(int, int)> pairs)
        {
            var map = new Dictionary<int, List<int>>();
            for (int i = 0; i < roomCount; i++) map[i] = new List<int>();
            foreach (var (a, b) in pairs)
            {
                map[a].Add(b);
                map[b].Add(a);
            }
            return map;
        }
    }
}
