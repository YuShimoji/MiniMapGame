using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Generates floor plans for commercial buildings (shops, restaurants, services).
    /// Layout varies by ShopSubtype: front customer zone + back staff zone on ground floor,
    /// office-like layout on upper floors.
    /// </summary>
    public class CommercialFloorPlan : IFloorPlanGenerator
    {
        public InteriorFloorData Generate(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            int floorIndex)
        {
            var floorData = new InteriorFloorData
            {
                floorIndex = floorIndex,
                floorBounds = FloorPlanUtils.CalculateFootprint(context).size
            };

            Rect footprint = FloorPlanUtils.CalculateFootprint(context);

            if (floorIndex == 0)
            {
                GenerateGroundFloor(rng, context, preset, footprint, floorData);
            }
            else
            {
                GenerateUpperFloor(rng, preset, footprint, floorData);
            }

            // Place doors between adjacent rooms
            var adjacentPairs = FloorPlanUtils.FindAdjacentPairs(floorData.rooms, preset.doorWidth, 0.2f);
            floorData.doors = FloorPlanUtils.PlaceDoors(floorData.rooms, adjacentPairs, rng, preset.doorWidth);

            // Ensure connectivity with corridors if needed
            floorData.corridors = FloorPlanUtils.EnsureConnectivity(floorData.rooms, adjacentPairs, preset.corridorWidth);

            // Insert dead space (voids, gaps between walls)
            FloorPlanUtils.InsertDeadSpace(floorData.rooms, rng, preset.deadSpaceRatio, preset.wallVoidProbability);
            floorData.deadSpaceRatio = preset.deadSpaceRatio;

            return floorData;
        }

        private void GenerateGroundFloor(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            Rect footprint,
            InteriorFloorData floorData)
        {
            // Zone split ratio based on tier (street position)
            float frontZoneRatio = context.tier == 0 ? 0.75f : (context.tier == 2 ? 0.5f : 0.65f);
            float frontDepth = footprint.height * frontZoneRatio;
            float backDepth = footprint.height * (1f - frontZoneRatio);

            Rect frontZone = new Rect(footprint.x, footprint.y, footprint.width, frontDepth);
            Rect backZone = new Rect(footprint.x, footprint.y + frontDepth, footprint.width, backDepth);

            int roomId = 0;
            var rooms = floorData.rooms;

            // Generate front zone (customer area) based on subtype
            GenerateFrontZone(rng, context, preset, frontZone, rooms, ref roomId);

            // Generate back zone (staff area)
            GenerateBackZone(rng, context, preset, backZone, rooms, ref roomId);

            // Add entrance at front edge
            AddEntrance(context, footprint, rooms, ref roomId);

            // Add restroom if building is large enough
            if (footprint.width * footprint.height > 40f)
            {
                AddRestroom(rng, backZone, rooms, ref roomId);
            }
        }

        private void GenerateFrontZone(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            Rect zone,
            List<InteriorRoom> rooms,
            ref int roomId)
        {
            ShopSubtype subtype = context.shopSubtype;

            switch (subtype)
            {
                case ShopSubtype.Grocery:
                case ShopSubtype.Pharmacy:
                case ShopSubtype.ArcadeShop:
                    // Large shopfront + counter near entrance
                    CreateLargeShopfrontLayout(zone, rooms, ref roomId, preset.discoveryDensity);
                    break;

                case ShopSubtype.Restaurant:
                case ShopSubtype.Cafe:
                    // Seating area (dominant) + counter/bar
                    CreateRestaurantLayout(zone, rooms, ref roomId, preset.discoveryDensity, subtype == ShopSubtype.Cafe);
                    break;

                case ShopSubtype.Bar:
                    // Counter (dominant) + small seating
                    CreateBarLayout(zone, rooms, ref roomId, preset.discoveryDensity);
                    break;

                case ShopSubtype.Department:
                case ShopSubtype.Hotel:
                    // Large lobby + display areas
                    CreateLobbyLayout(zone, rooms, ref roomId, preset.discoveryDensity, subtype == ShopSubtype.Hotel);
                    break;

                case ShopSubtype.Bookstore:
                case ShopSubtype.Pawnshop:
                    // Multiple display areas (subdivided)
                    CreateDisplayLayout(zone, rooms, ref roomId, preset, rng);
                    break;

                case ShopSubtype.Stall:
                case ShopSubtype.Vendor:
                    // Single shopfront (tiny)
                    CreateStallLayout(zone, rooms, ref roomId, preset.discoveryDensity);
                    break;

                case ShopSubtype.Laundry:
                case ShopSubtype.Tattoo:
                case ShopSubtype.Clinic:
                    // Shopfront + specialized service rooms
                    CreateServiceLayout(zone, rooms, ref roomId, preset, rng, subtype);
                    break;

                default:
                    // Generic shop layout
                    CreateLargeShopfrontLayout(zone, rooms, ref roomId, preset.discoveryDensity);
                    break;
            }
        }

        private void CreateLargeShopfrontLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, float discoveryDensity)
        {
            // 70% shopfront, 30% counter
            float counterWidth = zone.width * 0.3f;
            Rect shopRect = new Rect(zone.x, zone.y, zone.width - counterWidth, zone.height);
            Rect counterRect = new Rect(zone.x + shopRect.width, zone.y, counterWidth, zone.height);

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Shopfront, shopRect, discoveryDensity));
            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Counter, counterRect, discoveryDensity * 0.5f));
        }

        private void CreateRestaurantLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, float discoveryDensity, bool isCafe)
        {
            // 65% seating, 35% counter/bar
            float counterWidth = zone.width * 0.35f;
            Rect seatingRect = new Rect(zone.x, zone.y, zone.width - counterWidth, zone.height);
            Rect counterRect = new Rect(zone.x + seatingRect.width, zone.y, counterWidth, zone.height);

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.SeatingArea, seatingRect, discoveryDensity));
            rooms.Add(CreateRoom(roomId++, isCafe ? InteriorRoomType.Counter : InteriorRoomType.SeatingArea, counterRect, discoveryDensity * 0.3f));
        }

        private void CreateBarLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, float discoveryDensity)
        {
            // 60% counter/bar, 40% seating
            float seatingWidth = zone.width * 0.4f;
            Rect barRect = new Rect(zone.x, zone.y, zone.width - seatingWidth, zone.height);
            Rect seatingRect = new Rect(zone.x + barRect.width, zone.y, seatingWidth, zone.height);

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Counter, barRect, discoveryDensity * 0.5f));
            rooms.Add(CreateRoom(roomId++, InteriorRoomType.SeatingArea, seatingRect, discoveryDensity));
        }

        private void CreateLobbyLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, float discoveryDensity, bool isHotel)
        {
            // 50% lobby, 50% display/check-in
            float split = zone.width * 0.5f;
            Rect lobbyRect = new Rect(zone.x, zone.y, split, zone.height);
            Rect displayRect = new Rect(zone.x + split, zone.y, zone.width - split, zone.height);

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Lobby, lobbyRect, discoveryDensity * 0.6f));
            rooms.Add(CreateRoom(roomId++, isHotel ? InteriorRoomType.Counter : InteriorRoomType.DisplayArea, displayRect, discoveryDensity));
        }

        private void CreateDisplayLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, InteriorPreset preset, SeededRng rng)
        {
            // Subdivide into 3-6 display areas
            var rects = FloorPlanUtils.Subdivide(zone, rng, preset.minRoomSize, preset.maxRoomSize, 2, preset.irregularity);
            foreach (var rect in rects)
            {
                rooms.Add(CreateRoom(roomId++, InteriorRoomType.DisplayArea, rect, preset.discoveryDensity));
            }
        }

        private void CreateStallLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, float discoveryDensity)
        {
            // Single shopfront
            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Shopfront, zone, discoveryDensity));
        }

        private void CreateServiceLayout(Rect zone, List<InteriorRoom> rooms, ref int roomId, InteriorPreset preset, SeededRng rng, ShopSubtype subtype)
        {
            // 40% shopfront, 60% service rooms
            float shopWidth = zone.width * 0.4f;
            Rect shopRect = new Rect(zone.x, zone.y, shopWidth, zone.height);
            Rect serviceZone = new Rect(zone.x + shopWidth, zone.y, zone.width - shopWidth, zone.height);

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Shopfront, shopRect, preset.discoveryDensity));

            // Subdivide service zone
            var serviceRects = FloorPlanUtils.Subdivide(serviceZone, rng, preset.minRoomSize, preset.maxRoomSize, 1, preset.irregularity * 0.5f);
            InteriorRoomType serviceType = subtype == ShopSubtype.Tattoo ? InteriorRoomType.Shopfront
                                                                            : InteriorRoomType.Backroom;

            foreach (var rect in serviceRects)
            {
                rooms.Add(CreateRoom(roomId++, serviceType, rect, preset.discoveryDensity * 0.7f));
            }
        }

        private void GenerateBackZone(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            Rect zone,
            List<InteriorRoom> rooms,
            ref int roomId)
        {
            // Back zone: Storage, Backroom, Office, (Kitchen for restaurants)
            bool needsKitchen = context.shopSubtype == ShopSubtype.Restaurant ||
                                context.shopSubtype == ShopSubtype.Cafe ||
                                context.shopSubtype == ShopSubtype.Bar;

            if (zone.width * zone.height < preset.minRoomSize * 2f)
            {
                // Too small to subdivide
                var type = needsKitchen ? InteriorRoomType.Kitchen : InteriorRoomType.Storage;
                rooms.Add(CreateRoom(roomId++, type, zone, preset.discoveryDensity * 0.3f));
                return;
            }

            // Subdivide back zone
            var rects = FloorPlanUtils.Subdivide(zone, rng, preset.minRoomSize, preset.maxRoomSize, 1, preset.irregularity * 0.3f);

            for (int i = 0; i < rects.Count; i++)
            {
                InteriorRoomType type;
                if (i == 0 && needsKitchen)
                    type = InteriorRoomType.Kitchen;
                else if (i == 1)
                    type = InteriorRoomType.Backroom;
                else if (i == 2 && rng.Next() > 0.5f)
                    type = InteriorRoomType.Office;
                else
                    type = InteriorRoomType.Storage;

                rooms.Add(CreateRoom(roomId++, type, rects[i], preset.discoveryDensity * 0.4f));
            }
        }

        private void AddEntrance(InteriorBuildingContext context, Rect footprint, List<InteriorRoom> rooms, ref int roomId)
        {
            // Entrance at front center, sized proportionally
            float entranceWidth = Mathf.Clamp(footprint.width * 0.15f, 1.5f, 3f);
            float entranceDepth = 1.5f;
            Rect entranceRect = new Rect(
                footprint.x + (footprint.width - entranceWidth) * 0.5f,
                footprint.y,
                entranceWidth,
                entranceDepth
            );

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Entrance, entranceRect, 0f));
        }

        private void AddRestroom(SeededRng rng, Rect backZone, List<InteriorRoom> rooms, ref int roomId)
        {
            // Small restroom in back corner
            float size = rng.Range(2f, 3f);
            Rect restroomRect = new Rect(
                backZone.xMax - size,
                backZone.yMax - size,
                size,
                size
            );

            rooms.Add(CreateRoom(roomId++, InteriorRoomType.Restroom, restroomRect, 0f));
        }

        private void GenerateUpperFloor(
            SeededRng rng,
            InteriorPreset preset,
            Rect footprint,
            InteriorFloorData floorData)
        {
            // Upper floors: office-like layout (offices, meeting rooms, storage)
            var rects = FloorPlanUtils.Subdivide(footprint, rng, preset.minRoomSize, preset.maxRoomSize, 2, preset.irregularity);

            int roomId = 0;
            for (int i = 0; i < rects.Count; i++)
            {
                InteriorRoomType type;
                float roll = rng.Next();
                if (roll < 0.5f)
                    type = InteriorRoomType.Office;
                else if (roll < 0.75f)
                    type = InteriorRoomType.MeetingRoom;
                else if (roll < 0.9f)
                    type = InteriorRoomType.Storage;
                else
                    type = InteriorRoomType.Restroom;

                floorData.rooms.Add(CreateRoom(roomId++, type, rects[i], preset.discoveryDensity * 0.6f));
            }
        }

        private InteriorRoom CreateRoom(int id, InteriorRoomType type, Rect rect, float discoveryDensity)
        {
            float area = rect.width * rect.height;
            int slotCount = Mathf.Max(1, Mathf.RoundToInt(area * discoveryDensity));

            return new InteriorRoom
            {
                id = id,
                type = type,
                position = rect.center,
                size = rect.size,
                rotation = 0f,
                discoverySlotCount = slotCount,
                isSecret = false
            };
        }
    }
}
