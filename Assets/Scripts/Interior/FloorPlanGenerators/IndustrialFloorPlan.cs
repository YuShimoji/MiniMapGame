using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Generates industrial building floor plans (warehouses, factories, workshops).
    /// Characterized by: large open spaces, loading docks, minimal subdivision, high dead space.
    /// </summary>
    public class IndustrialFloorPlan : IFloorPlanGenerator
    {
        public InteriorFloorData Generate(SeededRng rng, InteriorBuildingContext context, InteriorPreset preset, int floorIndex)
        {
            var data = new InteriorFloorData
            {
                floorIndex = floorIndex,
                floorBounds = FloorPlanUtils.CalculateFootprint(context).size
            };

            if (floorIndex == 0)
            {
                GenerateGroundFloor(data, rng, context, preset);
            }
            else if (floorIndex > 0)
            {
                GenerateUpperFloor(data, rng, context, preset);
            }
            else // basement
            {
                GenerateBasement(data, rng, context, preset);
            }

            // Industrial buildings have higher dead space ratio
            float industrialDeadSpace = Mathf.Max(preset.deadSpaceRatio, 0.25f);
            FloorPlanUtils.InsertDeadSpace(data.rooms, rng, industrialDeadSpace, preset.wallVoidProbability);
            data.deadSpaceRatio = industrialDeadSpace;

            // Connect rooms
            var pairs = FloorPlanUtils.FindAdjacentPairs(data.rooms, preset.doorWidth, 0.5f);
            data.doors = FloorPlanUtils.PlaceDoors(data.rooms, pairs, rng, preset.doorWidth);
            data.corridors = FloorPlanUtils.EnsureConnectivity(data.rooms, pairs, preset.corridorWidth);

            return data;
        }

        private void GenerateGroundFloor(InteriorFloorData data, SeededRng rng, InteriorBuildingContext context, InteriorPreset preset)
        {
            Rect footprint = FloorPlanUtils.CalculateFootprint(context);
            int roomId = 0;

            // Loading dock at one edge (full width, ~20% depth)
            bool dockAtNorth = rng.Next() > 0.5f;
            float dockDepth = footprint.height * 0.2f;
            Rect dockArea = dockAtNorth
                ? new Rect(footprint.x, footprint.yMax - dockDepth, footprint.width, dockDepth)
                : new Rect(footprint.x, footprint.y, footprint.width, dockDepth);

            data.rooms.Add(new InteriorRoom
            {
                id = roomId++,
                type = InteriorRoomType.LoadingDock,
                position = dockArea.center,
                size = dockArea.size,
                rotation = 0f,
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 2f),
                isSecret = false
            });

            // Entrance at loading dock
            data.rooms.Add(new InteriorRoom
            {
                id = roomId++,
                type = InteriorRoomType.Entrance,
                position = new Vector2(dockArea.center.x, dockArea.yMin + 1f),
                size = new Vector2(3f, 2f),
                rotation = 0f,
                discoverySlotCount = 0,
                isSecret = false
            });

            // Main workshop/warehouse area (~60% of remaining space)
            float remainingHeight = footprint.height - dockDepth;
            float workshopHeight = remainingHeight * 0.6f;
            Rect workshopArea = dockAtNorth
                ? new Rect(footprint.x, footprint.y, footprint.width, workshopHeight)
                : new Rect(footprint.x, footprint.y + dockDepth, footprint.width, workshopHeight);

            InteriorRoomType mainType = rng.Next() > 0.5f ? InteriorRoomType.Workshop : InteriorRoomType.MachineryRoom;
            if (preset.decayLevel > 0.7f && rng.Next() > 0.6f)
            {
                mainType = InteriorRoomType.Storage; // Abandoned warehouse
            }

            data.rooms.Add(new InteriorRoom
            {
                id = roomId++,
                type = mainType,
                position = workshopArea.center,
                size = workshopArea.size,
                rotation = 0f,
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 5f),
                isSecret = false
            });

            // Small utility zone at opposite edge (subdivided)
            float utilityHeight = remainingHeight - workshopHeight;
            Rect utilityArea = dockAtNorth
                ? new Rect(footprint.x, workshopArea.yMin, footprint.width, utilityHeight)
                : new Rect(footprint.x, workshopArea.yMax, footprint.width, utilityHeight);

            // Subdivide utility area into 2-3 rooms
            var utilityRects = FloorPlanUtils.Subdivide(
                utilityArea,
                rng,
                preset.minRoomSize * 1.5f,
                preset.maxRoomSize,
                1, // shallow depth
                preset.irregularity * 0.5f
            );

            for (int i = 0; i < utilityRects.Count && i < 3; i++)
            {
                InteriorRoomType utilType = i == 0 ? InteriorRoomType.Office
                    : i == 1 ? InteriorRoomType.Restroom
                    : InteriorRoomType.Utility;

                data.rooms.Add(new InteriorRoom
                {
                    id = roomId++,
                    type = utilType,
                    position = utilityRects[i].center,
                    size = utilityRects[i].size,
                    rotation = 0f,
                    discoverySlotCount = utilType == InteriorRoomType.Office ? 1 : 0,
                    isSecret = false
                });
            }

            // Secret room chance
            if (rng.Next() < preset.secretRoomProbability)
            {
                Vector2 secretPos = new Vector2(
                    footprint.x + rng.Range(2f, footprint.width - 2f),
                    footprint.y + rng.Range(2f, footprint.height - 2f)
                );
                data.rooms.Add(new InteriorRoom
                {
                    id = roomId++,
                    type = InteriorRoomType.SecretRoom,
                    position = secretPos,
                    size = new Vector2(preset.minRoomSize, preset.minRoomSize),
                    rotation = 0f,
                    discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 3f),
                    isSecret = true
                });
            }
        }

        private void GenerateUpperFloor(InteriorFloorData data, SeededRng rng, InteriorBuildingContext context, InteriorPreset preset)
        {
            Rect footprint = FloorPlanUtils.CalculateFootprint(context);
            int roomId = 0;

            // Large open workshop or control area (~70% of space)
            float mainWidth = footprint.width * 0.7f;
            Rect mainArea = new Rect(footprint.x, footprint.y, mainWidth, footprint.height);

            data.rooms.Add(new InteriorRoom
            {
                id = roomId++,
                type = rng.Next() > 0.5f ? InteriorRoomType.Workshop : InteriorRoomType.Office,
                position = mainArea.center,
                size = mainArea.size,
                rotation = 0f,
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 3f),
                isSecret = false
            });

            // Small utility/stairwell area
            Rect utilArea = new Rect(footprint.xMax - footprint.width * 0.3f, footprint.y, footprint.width * 0.3f, footprint.height);
            var utilRects = FloorPlanUtils.Subdivide(utilArea, rng, preset.minRoomSize, preset.maxRoomSize, 1, 0.1f);

            for (int i = 0; i < utilRects.Count && i < 2; i++)
            {
                data.rooms.Add(new InteriorRoom
                {
                    id = roomId++,
                    type = i == 0 ? InteriorRoomType.Stairwell : InteriorRoomType.Utility,
                    position = utilRects[i].center,
                    size = utilRects[i].size,
                    rotation = 0f,
                    discoverySlotCount = 0,
                    isSecret = false
                });
            }
        }

        private void GenerateBasement(InteriorFloorData data, SeededRng rng, InteriorBuildingContext context, InteriorPreset preset)
        {
            Rect footprint = FloorPlanUtils.CalculateFootprint(context);
            int roomId = 0;

            // Large storage or machinery room (~80% of space)
            float mainWidth = footprint.width * 0.8f;
            Rect mainArea = new Rect(footprint.x, footprint.y, mainWidth, footprint.height);

            InteriorRoomType mainType = rng.Next() > 0.5f ? InteriorRoomType.Storage : InteriorRoomType.MachineryRoom;
            if (preset.decayLevel > 0.8f && rng.Next() > 0.7f)
            {
                mainType = InteriorRoomType.Basement; // Generic basement/ruin
            }

            data.rooms.Add(new InteriorRoom
            {
                id = roomId++,
                type = mainType,
                position = mainArea.center,
                size = mainArea.size,
                rotation = 0f,
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 4f),
                isSecret = false
            });

            // Utility + stairwell
            Rect utilArea = new Rect(footprint.xMax - footprint.width * 0.2f, footprint.y, footprint.width * 0.2f, footprint.height);
            var utilRects = FloorPlanUtils.Subdivide(utilArea, rng, preset.minRoomSize, preset.maxRoomSize, 1, 0.1f);

            for (int i = 0; i < utilRects.Count && i < 2; i++)
            {
                data.rooms.Add(new InteriorRoom
                {
                    id = roomId++,
                    type = i == 0 ? InteriorRoomType.Stairwell : InteriorRoomType.Utility,
                    position = utilRects[i].center,
                    size = utilRects[i].size,
                    rotation = 0f,
                    discoverySlotCount = 0,
                    isSecret = false
                });
            }
        }
    }
}
