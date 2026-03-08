using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Special building floor plan generator for landmarks, ruins, research facilities.
    /// Provides the most varied and exploration-focused layouts.
    /// </summary>
    public class SpecialFloorPlan : IFloorPlanGenerator
    {
        private enum SpecialMode { Landmark, Ruin, ResearchFacility }

        public InteriorFloorData Generate(SeededRng rng, InteriorBuildingContext context, InteriorPreset preset, int floorIndex)
        {
            var mode = DetermineMode(context, preset);
            var floorData = new InteriorFloorData { floorIndex = floorIndex };

            switch (mode)
            {
                case SpecialMode.Landmark:
                    GenerateLandmark(rng, context, preset, floorIndex, floorData);
                    break;
                case SpecialMode.Ruin:
                    GenerateRuin(rng, context, preset, floorIndex, floorData);
                    break;
                case SpecialMode.ResearchFacility:
                    GenerateResearchFacility(rng, context, preset, floorIndex, floorData);
                    break;
            }

            return floorData;
        }

        private static SpecialMode DetermineMode(InteriorBuildingContext context, InteriorPreset preset)
        {
            if (context.isLandmark) return SpecialMode.Landmark;
            if (preset.decayLevel > 0.6f) return SpecialMode.Ruin;
            return SpecialMode.ResearchFacility;
        }

        // ── LANDMARK ──────────────────────────────────────────────────────────

        private void GenerateLandmark(SeededRng rng, InteriorBuildingContext context,
            InteriorPreset preset, int floorIndex, InteriorFloorData floorData)
        {
            var footprint = FloorPlanUtils.CalculateFootprint(context);
            floorData.floorBounds = footprint.size;

            if (floorIndex == 0)
                GenerateLandmarkGroundFloor(rng, footprint, preset, floorData);
            else if (floorIndex >= context.floors - 1)
                GenerateLandmarkTopFloor(rng, footprint, preset, floorData);
            else
                GenerateLandmarkMidFloor(rng, footprint, preset, floorData);

            FinalizeFloorPlan(rng, preset, floorData, preset.secretRoomProbability * 1.5f);
        }

        private void GenerateLandmarkGroundFloor(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rooms = floorData.rooms;

            // Grand Lobby ~40% of area
            rooms.Add(new InteriorRoom
            {
                id = 0,
                type = InteriorRoomType.Lobby,
                position = footprint.center,
                size = footprint.size * 0.63f,
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 4f)
            });

            // 2-3 Stairwells around perimeter
            int stairCount = rng.Range(2, 4);
            for (int i = 0; i < stairCount; i++)
            {
                float angle = (i / (float)stairCount) * Mathf.PI * 2f;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * footprint.size.magnitude * 0.35f;
                rooms.Add(new InteriorRoom
                {
                    id = rooms.Count,
                    type = InteriorRoomType.Stairwell,
                    position = footprint.center + offset,
                    size = new Vector2(preset.minRoomSize * 1.2f, preset.minRoomSize * 1.2f)
                });
            }

            AddPerimeterRooms(rng, footprint, preset, rooms,
                new[] { InteriorRoomType.Office, InteriorRoomType.MeetingRoom }, 3, 5);
        }

        private void GenerateLandmarkTopFloor(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rooms = floorData.rooms;

            // Central Vault
            rooms.Add(new InteriorRoom
            {
                id = 0,
                type = InteriorRoomType.Vault,
                position = footprint.center,
                size = new Vector2(preset.maxRoomSize * 0.8f, preset.maxRoomSize * 0.8f),
                discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 6f)
            });

            // 1-2 SecretRooms in corners
            int secretCount = rng.Range(1, 3);
            for (int i = 0; i < secretCount; i++)
            {
                var cornerOffset = new Vector2(
                    (i % 2 == 0 ? -1 : 1) * footprint.size.x * 0.35f,
                    (i < 2 ? -1 : 1) * footprint.size.y * 0.35f);

                rooms.Add(new InteriorRoom
                {
                    id = rooms.Count,
                    type = InteriorRoomType.SecretRoom,
                    position = footprint.center + cornerOffset,
                    size = new Vector2(preset.minRoomSize, preset.minRoomSize),
                    isSecret = true,
                    discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 4f)
                });
            }

            AddPerimeterRooms(rng, footprint, preset, rooms,
                new[] { InteriorRoomType.Stairwell }, 1, 2);
        }

        private void GenerateLandmarkMidFloor(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rects = FloorPlanUtils.Subdivide(footprint, rng, preset.minRoomSize,
                preset.maxRoomSize, 3, preset.irregularity);
            var rooms = FloorPlanUtils.RectsToRooms(rects, 0);

            var types = new[] { InteriorRoomType.Office, InteriorRoomType.Archive, InteriorRoomType.MeetingRoom };
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                r.type = types[rng.Range(0, types.Length)];
                r.discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 2f);
                rooms[i] = r;
            }

            floorData.rooms = rooms;
        }

        // ── RUIN ──────────────────────────────────────────────────────────────

        private void GenerateRuin(SeededRng rng, InteriorBuildingContext context,
            InteriorPreset preset, int floorIndex, InteriorFloorData floorData)
        {
            var footprint = FloorPlanUtils.CalculateFootprint(context);
            floorData.floorBounds = footprint.size;

            float irregularity = preset.irregularity + 0.3f;
            var rects = FloorPlanUtils.Subdivide(footprint, rng,
                preset.minRoomSize * 0.7f, preset.maxRoomSize * 1.2f, 4, irregularity);
            var rooms = FloorPlanUtils.RectsToRooms(rects, 0);

            // Apply rotation to 40% of rooms (collapsed/tilted)
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (rng.Next() < 0.4f)
                    r.rotation = rng.Range(-15f, 15f);
                rooms[i] = r;
            }

            // 30-50% → Ruin, rest → WallVoid
            int ruinCount = Mathf.RoundToInt(rooms.Count * rng.Range(0.3f, 0.5f));
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (i < ruinCount)
                {
                    r.type = InteriorRoomType.Ruin;
                    r.discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 3f);
                }
                else
                {
                    r.type = InteriorRoomType.WallVoid;
                    r.discoverySlotCount = 0;
                }
                rooms[i] = r;
            }

            // Ground floor entrance
            if (floorIndex == 0 && rooms.Count > 0)
            {
                var r = rooms[0];
                r.type = InteriorRoomType.Entrance;
                r.rotation = 0f;
                rooms[0] = r;
            }

            floorData.rooms = rooms;
            floorData.deadSpaceRatio = preset.deadSpaceRatio * 1.8f;

            FinalizeFloorPlan(rng, preset, floorData, preset.secretRoomProbability * 2f,
                hiddenDoorProb: 0.5f);
        }

        // ── RESEARCH FACILITY ─────────────────────────────────────────────────

        private void GenerateResearchFacility(SeededRng rng, InteriorBuildingContext context,
            InteriorPreset preset, int floorIndex, InteriorFloorData floorData)
        {
            var footprint = FloorPlanUtils.CalculateFootprint(context);
            floorData.floorBounds = footprint.size;

            if (floorIndex == 0)
                GenerateFacilityGroundFloor(rng, footprint, preset, floorData);
            else if (floorIndex < 0)
                GenerateFacilityBasement(rng, footprint, preset, floorData);
            else
                GenerateFacilityUpperFloor(rng, footprint, preset, floorData);

            FinalizeFloorPlan(rng, preset, floorData, preset.secretRoomProbability * 2f,
                lockedDoorProb: 0.3f);
        }

        private void GenerateFacilityGroundFloor(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rooms = floorData.rooms;

            // Entrance lobby
            rooms.Add(new InteriorRoom
            {
                id = 0,
                type = InteriorRoomType.Entrance,
                position = new Vector2(footprint.xMin + preset.maxRoomSize * 0.3f, footprint.center.y),
                size = new Vector2(preset.maxRoomSize * 0.6f, preset.minRoomSize * 1.5f),
                discoverySlotCount = 1
            });

            // Central Corridor
            rooms.Add(new InteriorRoom
            {
                id = 1,
                type = InteriorRoomType.Corridor,
                position = footprint.center,
                size = new Vector2(footprint.width * 0.8f, preset.corridorWidth * 2f)
            });

            // Wings: Laboratory, Office, Archive
            var wingTypes = new[] { InteriorRoomType.Laboratory, InteriorRoomType.Office, InteriorRoomType.Archive };
            for (int i = 0; i < 3; i++)
            {
                rooms.Add(new InteriorRoom
                {
                    id = rooms.Count,
                    type = wingTypes[i],
                    position = footprint.center + new Vector2(0, (i - 1) * footprint.height * 0.3f),
                    size = new Vector2(preset.maxRoomSize, preset.minRoomSize * 1.2f),
                    discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * 3f)
                });
            }

            AddPerimeterRooms(rng, footprint, preset, rooms,
                new[] { InteriorRoomType.Stairwell }, 2, 3);
        }

        private void GenerateFacilityBasement(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rects = FloorPlanUtils.Subdivide(footprint, rng, preset.minRoomSize,
                preset.maxRoomSize, 2, preset.irregularity);
            var rooms = FloorPlanUtils.RectsToRooms(rects, 0);

            var types = new[] { InteriorRoomType.Vault, InteriorRoomType.Storage,
                InteriorRoomType.Utility, InteriorRoomType.SecretRoom };

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                r.type = types[rng.Range(0, types.Length)];
                bool isHighValue = r.type == InteriorRoomType.Vault || r.type == InteriorRoomType.SecretRoom;
                r.discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity * (isHighValue ? 5f : 2f));
                r.isSecret = r.type == InteriorRoomType.SecretRoom;
                rooms[i] = r;
            }

            floorData.rooms = rooms;
        }

        private void GenerateFacilityUpperFloor(SeededRng rng, Rect footprint,
            InteriorPreset preset, InteriorFloorData floorData)
        {
            var rects = FloorPlanUtils.Subdivide(footprint, rng, preset.minRoomSize,
                preset.maxRoomSize, 3, preset.irregularity);
            var rooms = FloorPlanUtils.RectsToRooms(rects, 0);

            var types = new[] { InteriorRoomType.Laboratory, InteriorRoomType.ServerRoom,
                InteriorRoomType.Archive, InteriorRoomType.Office };

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                r.type = types[rng.Range(0, types.Length)];
                r.discoverySlotCount = Mathf.RoundToInt(preset.discoveryDensity *
                    (r.type == InteriorRoomType.ServerRoom ? 4f : 2f));
                rooms[i] = r;
            }

            floorData.rooms = rooms;
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private static void AddPerimeterRooms(SeededRng rng, Rect footprint,
            InteriorPreset preset, List<InteriorRoom> rooms,
            InteriorRoomType[] types, int minCount, int maxCount)
        {
            int count = rng.Range(minCount, maxCount + 1);
            bool isStairwell = types.Contains(InteriorRoomType.Stairwell);

            for (int i = 0; i < count; i++)
            {
                float angle = rng.Range(0f, Mathf.PI * 2f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * footprint.size.magnitude * 0.4f;
                rooms.Add(new InteriorRoom
                {
                    id = rooms.Count,
                    type = types[rng.Range(0, types.Length)],
                    position = footprint.center + offset,
                    size = new Vector2(preset.minRoomSize, preset.minRoomSize),
                    discoverySlotCount = isStairwell ? 0 : Mathf.RoundToInt(preset.discoveryDensity * 1.5f)
                });
            }
        }

        private static void FinalizeFloorPlan(SeededRng rng, InteriorPreset preset,
            InteriorFloorData floorData, float secretProb,
            float lockedDoorProb = 0f, float hiddenDoorProb = 0f)
        {
            var rooms = floorData.rooms;
            if (rooms.Count == 0) return;

            var pairs = FloorPlanUtils.FindAdjacentPairs(rooms, preset.doorWidth, 0.5f);
            var doors = FloorPlanUtils.PlaceDoors(rooms, pairs, rng, preset.doorWidth);

            // Apply locked/hidden status (struct copy-modify-assign)
            for (int i = 0; i < doors.Count; i++)
            {
                var d = doors[i];
                if (lockedDoorProb > 0f && rng.Next() < lockedDoorProb)
                    d.isLocked = true;
                if (hiddenDoorProb > 0f && rng.Next() < hiddenDoorProb)
                    d.isHidden = true;
                doors[i] = d;
            }

            floorData.doors = doors;
            floorData.corridors = FloorPlanUtils.EnsureConnectivity(rooms, pairs, preset.corridorWidth);
            FloorPlanUtils.InsertDeadSpace(rooms, rng, preset.deadSpaceRatio, preset.wallVoidProbability);

            if (floorData.deadSpaceRatio == 0f)
                floorData.deadSpaceRatio = preset.deadSpaceRatio;
        }
    }
}
