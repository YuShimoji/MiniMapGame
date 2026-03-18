using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Orchestrates interior generation using the floor plan strategy pattern.
    /// Uses context-aware API with building classification and preset.
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

    }
}
