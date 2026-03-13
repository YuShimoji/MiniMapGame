using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.MiniGame;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Renders InteriorMapData as 3D primitives.
    /// Supports multi-floor rendering with per-floor visibility toggle.
    /// New data path: InteriorFloorData with InteriorRoom/InteriorDoor/InteriorCorridor/InteriorFurniture.
    /// Legacy data path: RoomNode/CorridorEdge (single floor, backward compatible).
    /// </summary>
    public class InteriorRenderer : MonoBehaviour
    {
        [Header("Materials")]
        public Material wallMaterial;

        [Header("MiniGame")]
        public MiniGameManager miniGameManager;

        [Header("Settings")]
        public float floorY = 0.02f;
        public float wallHeight = 0.2f;
        public float floorSpacing = 0.5f; // Y offset between floors (for stacked rendering)

        private readonly List<FloorRenderGroup> _floorGroups = new();
        private readonly List<GameObject> _spawnedObjects = new();
        private readonly Dictionary<Color, Material> _floorMaterialCache = new();
        private Material _cachedWallMaterial;
        private Mesh _sharedQuadMesh;
        private Mesh _sharedCubeMesh;

        private int _currentFloorIndex;
        private InteriorPreset _activePreset;
        private Vector3 _currentWorldOrigin;

        // Default room type → color mapping
        private static readonly Dictionary<InteriorRoomType, Color> DefaultRoomColors = new()
        {
            // Structural
            { InteriorRoomType.Entrance, new Color(0.2f, 0.7f, 0.3f) },
            { InteriorRoomType.Hallway, new Color(0.5f, 0.5f, 0.55f) },
            { InteriorRoomType.Stairwell, new Color(0.45f, 0.55f, 0.65f) },
            { InteriorRoomType.Corridor, new Color(0.5f, 0.5f, 0.55f) },
            // Residential
            { InteriorRoomType.LivingRoom, new Color(0.65f, 0.68f, 0.6f) },
            { InteriorRoomType.Bedroom, new Color(0.6f, 0.6f, 0.7f) },
            { InteriorRoomType.Kitchen, new Color(0.7f, 0.65f, 0.55f) },
            { InteriorRoomType.Bathroom, new Color(0.55f, 0.7f, 0.72f) },
            { InteriorRoomType.DiningRoom, new Color(0.68f, 0.65f, 0.58f) },
            { InteriorRoomType.Storage, new Color(0.5f, 0.48f, 0.45f) },
            // Commercial
            { InteriorRoomType.Shopfront, new Color(0.75f, 0.7f, 0.55f) },
            { InteriorRoomType.Backroom, new Color(0.5f, 0.48f, 0.46f) },
            { InteriorRoomType.Counter, new Color(0.7f, 0.6f, 0.5f) },
            { InteriorRoomType.DisplayArea, new Color(0.72f, 0.68f, 0.6f) },
            { InteriorRoomType.SeatingArea, new Color(0.65f, 0.62f, 0.55f) },
            { InteriorRoomType.Bar, new Color(0.55f, 0.42f, 0.35f) },
            // Industrial
            { InteriorRoomType.Workshop, new Color(0.58f, 0.55f, 0.5f) },
            { InteriorRoomType.LoadingDock, new Color(0.55f, 0.55f, 0.52f) },
            { InteriorRoomType.MachineryRoom, new Color(0.52f, 0.52f, 0.5f) },
            // Public
            { InteriorRoomType.Lobby, new Color(0.7f, 0.72f, 0.68f) },
            { InteriorRoomType.Office, new Color(0.62f, 0.65f, 0.68f) },
            { InteriorRoomType.MeetingRoom, new Color(0.6f, 0.62f, 0.65f) },
            { InteriorRoomType.Archive, new Color(0.55f, 0.52f, 0.48f) },
            // Special
            { InteriorRoomType.Laboratory, new Color(0.6f, 0.72f, 0.65f) },
            { InteriorRoomType.ServerRoom, new Color(0.5f, 0.55f, 0.65f) },
            { InteriorRoomType.SecretRoom, new Color(0.4f, 0.2f, 0.6f) },
            { InteriorRoomType.Vault, new Color(0.9f, 0.75f, 0.2f) },
            { InteriorRoomType.Ruin, new Color(0.4f, 0.38f, 0.35f) },
            { InteriorRoomType.Rooftop, new Color(0.6f, 0.65f, 0.7f) },
            { InteriorRoomType.Basement, new Color(0.42f, 0.4f, 0.38f) },
            // Dead space
            { InteriorRoomType.WallVoid, new Color(0.2f, 0.2f, 0.22f) },
            { InteriorRoomType.Shaft, new Color(0.18f, 0.18f, 0.2f) },
            // Utility
            { InteriorRoomType.Restroom, new Color(0.6f, 0.65f, 0.7f) },
            { InteriorRoomType.Utility, new Color(0.48f, 0.48f, 0.5f) },
        };

        // Legacy colors for backward compat
#pragma warning disable CS0618
        private static readonly Dictionary<RoomType, Color> LegacyRoomColors = new()
        {
            { RoomType.Entrance, new Color(0.2f, 0.7f, 0.3f) },
            { RoomType.Boss, new Color(0.8f, 0.2f, 0.2f) },
            { RoomType.Treasure, new Color(0.9f, 0.75f, 0.2f) },
            { RoomType.Alcove, new Color(0.45f, 0.45f, 0.5f) },
            { RoomType.Normal, new Color(0.6f, 0.65f, 0.7f) }
        };
#pragma warning restore CS0618

        private static readonly Color DefaultWallColor = new(0.3f, 0.35f, 0.4f);
        private static readonly Color DefaultCorridorColor = new(0.5f, 0.5f, 0.55f);

        private string _currentBuildingId;
        private int _currentSeed;

        public int CurrentFloorIndex => _currentFloorIndex;
        public int FloorCount => _floorGroups.Count;

        /// <summary>
        /// New multi-floor render path. Uses InteriorFloorData.
        /// </summary>
        public void Render(InteriorMapData data, Vector3 worldOrigin, InteriorPreset preset,
            string buildingId = null, int seed = 0)
        {
            Clear();
            _currentBuildingId = buildingId;
            _currentSeed = seed;
            _activePreset = preset;
            _currentWorldOrigin = worldOrigin;
            _currentFloorIndex = 0;

            for (int fi = 0; fi < data.floors.Count; fi++)
            {
                var floor = data.floors[fi];
                var group = new FloorRenderGroup
                {
                    floorIndex = floor.floorIndex,
                    root = new GameObject($"Floor_{floor.floorIndex}")
                };
                group.root.transform.SetParent(transform);

                float yOffset = floorY + fi * floorSpacing;

                // Rooms
                for (int ri = 0; ri < floor.rooms.Count; ri++)
                {
                    var room = floor.rooms[ri];
                    CreateNewRoomFloor(room, worldOrigin, yOffset, group.root.transform, preset);
                    CreateNewRoomWalls(room, ri, floor.doors, worldOrigin, yOffset, group.root.transform, preset);
                }

                // Doors (rendered as floor gaps in walls — visual indicators)
                for (int di = 0; di < floor.doors.Count; di++)
                {
                    var door = floor.doors[di];
                    CreateDoorIndicator(door, di, fi, worldOrigin, yOffset, group.root.transform, preset);
                }

                // Corridors
                foreach (var corridor in floor.corridors)
                {
                    CreateNewCorridorFloor(corridor, floor, worldOrigin, yOffset, group.root.transform, preset);
                }

                // Furniture and discovery props
                foreach (var furniture in floor.furniture)
                {
                    CreateFurniture(furniture, fi, worldOrigin, yOffset, group.root.transform, preset);
                }

                _floorGroups.Add(group);
            }

            // Show only ground floor initially
            SetActiveFloor(0);
        }

        /// <summary>
        /// Legacy single-floor render path. Backward compatible with old InteriorMapData.
        /// </summary>
#pragma warning disable CS0618
        public void Render(InteriorMapData data, Vector3 worldOrigin,
            string buildingId = null, int seed = 0)
        {
            Clear();
            _currentBuildingId = buildingId;
            _currentSeed = seed;
            _activePreset = null;
            _currentWorldOrigin = worldOrigin;

            // Legacy path: use rooms/corridors properties (triggers Obsolete warning suppressed here)
            var rooms = data.rooms;
            var corridors = data.corridors;

            for (int i = 0; i < rooms.Count; i++)
            {
                CreateLegacyRoomFloor(rooms[i], worldOrigin);
                AttachLegacyRoomTrigger(rooms[i], i, worldOrigin);
            }

            foreach (var room in rooms)
                CreateLegacyRoomWalls(room, worldOrigin);

            foreach (var corridor in corridors)
                CreateLegacyCorridorFloor(corridor, rooms, worldOrigin);
        }
#pragma warning restore CS0618

        public void Clear()
        {
            foreach (var group in _floorGroups)
            {
                if (group.root != null) Destroy(group.root);
            }
            _floorGroups.Clear();

            foreach (var obj in _spawnedObjects)
                if (obj != null) Destroy(obj);
            _spawnedObjects.Clear();

            foreach (var mat in _floorMaterialCache.Values)
                if (mat != null) Destroy(mat);
            _floorMaterialCache.Clear();
            if (_cachedWallMaterial != null)
            {
                Destroy(_cachedWallMaterial);
                _cachedWallMaterial = null;
            }

            _activePreset = null;
            _currentFloorIndex = 0;
            _currentWorldOrigin = Vector3.zero;
        }

        /// <summary>
        /// Show only the specified floor. Hides all others.
        /// </summary>
        public void SetActiveFloor(int index)
        {
            index = Mathf.Clamp(index, 0, _floorGroups.Count - 1);
            _currentFloorIndex = index;

            for (int i = 0; i < _floorGroups.Count; i++)
            {
                if (_floorGroups[i].root != null)
                    _floorGroups[i].root.SetActive(i == index);
            }
        }

        /// <summary>
        /// Move to next floor (up). Returns new floor index.
        /// </summary>
        public int GoUpFloor()
        {
            if (_currentFloorIndex < _floorGroups.Count - 1)
                SetActiveFloor(_currentFloorIndex + 1);
            return _currentFloorIndex;
        }

        /// <summary>
        /// Move to previous floor (down). Returns new floor index.
        /// </summary>
        public int GoDownFloor()
        {
            if (_currentFloorIndex > 0)
                SetActiveFloor(_currentFloorIndex - 1);
            return _currentFloorIndex;
        }

        /// <summary>
        /// Get the display label for the current floor (B2, B1, 1F, 2F, ...).
        /// </summary>
        public string GetFloorLabel(int index)
        {
            if (index < 0 || index >= _floorGroups.Count) return "";
            int fi = _floorGroups[index].floorIndex;
            if (fi < 0) return $"B{-fi}";
            return $"{fi + 1}F";
        }

        public string GetCurrentFloorLabel() => GetFloorLabel(_currentFloorIndex);

        // ===== New data path rendering =====

        private void CreateNewRoomFloor(InteriorRoom room, Vector3 origin, float yOffset,
            Transform parent, InteriorPreset preset)
        {
            var go = new GameObject($"RoomFloor_{room.type}_{room.id}");
            go.transform.SetParent(parent);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GetCachedFloorMaterial(GetRoomColor(room.type, preset));

            var pos = new Vector3(origin.x + room.position.x, yOffset, origin.z + room.position.y);

            if (room.rotation != 0f)
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(90f, room.rotation, 0f);
            }
            else
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            go.transform.localScale = new Vector3(room.size.x, room.size.y, 1f);
        }

        private void CreateNewRoomWalls(InteriorRoom room, int roomIndex, List<InteriorDoor> doors,
            Vector3 origin, float yOffset, Transform parent, InteriorPreset preset)
        {
            float hw = room.size.x * 0.5f;
            float hh = room.size.y * 0.5f;
            var center = new Vector3(origin.x + room.position.x, yOffset + 0.05f, origin.z + room.position.y);

            // Collect door gaps on this room's walls
            var doorGaps = new List<DoorGap>();
            foreach (var door in doors)
            {
                if (door.roomA == roomIndex || door.roomB == roomIndex)
                {
                    doorGaps.Add(new DoorGap
                    {
                        position = door.position,
                        halfWidth = door.width * 0.5f,
                        isHidden = door.isHidden
                    });
                }
            }

            Color wallColor = preset != null ? preset.wallColor : DefaultWallColor;
            float wh = preset != null ? Mathf.Min(preset.wallHeight * 0.06f, 0.3f) : wallHeight;

            // 4 wall segments, each potentially split by door gaps
            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector3(center.x - hw, center.y, center.z - hh);
            corners[1] = new Vector3(center.x + hw, center.y, center.z - hh);
            corners[2] = new Vector3(center.x + hw, center.y, center.z + hh);
            corners[3] = new Vector3(center.x - hw, center.y, center.z + hh);

            for (int w = 0; w < 4; w++)
            {
                var wallStart = corners[w];
                var wallEnd = corners[(w + 1) % 4];
                var segments = SplitWallByDoors(wallStart, wallEnd, doorGaps);

                foreach (var seg in segments)
                {
                    CreateWallSegment(seg.start, seg.end, wh, wallColor, parent);
                }
            }
        }

        private void CreateWallSegment(Vector3 start, Vector3 end, float width, Color color, Transform parent)
        {
            if (Vector3.Distance(start, end) < 0.1f) return;

            var go = new GameObject("WallSeg");
            go.transform.SetParent(parent);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = wallMaterial != null ? wallMaterial : GetCachedWallMaterial();
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        private void CreateDoorIndicator(InteriorDoor door, int doorIndex, int floorIndex,
            Vector3 origin, float yOffset, Transform parent, InteriorPreset preset)
        {
            // Small colored indicator at door position
            var go = new GameObject($"Door_{door.roomA}_{door.roomB}");
            go.transform.SetParent(parent);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            Color doorColor = door.isLocked
                ? new Color(0.8f, 0.3f, 0.2f, 0.8f) // Red = locked
                : new Color(0.3f, 0.7f, 0.4f, 0.8f); // Green = open
            go.AddComponent<MeshRenderer>().sharedMaterial = GetCachedFloorMaterial(doorColor);

            go.transform.position = new Vector3(origin.x + door.position.x, yOffset + 0.01f, origin.z + door.position.y);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(door.width, door.width * 0.3f, 1f);

            // Interaction collider (trigger)
            var triggerCol = go.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1f, 1f, 5f); // Scaled by transform; Z extends upward

            // DoorInteractable component
            var interactable = go.AddComponent<DoorInteractable>();
            interactable.doorIndex = doorIndex;
            interactable.roomA = door.roomA;
            interactable.roomB = door.roomB;
            interactable.isLocked = door.isLocked;
            interactable.isHidden = door.isHidden;
            interactable.floorIndex = floorIndex;

            // Movement-blocking collider for locked doors (non-trigger)
            if (door.isLocked)
            {
                var blockCol = go.AddComponent<BoxCollider>();
                blockCol.isTrigger = false;
                blockCol.size = new Vector3(1f, 1f, 5f);
                interactable.blockingCollider = blockCol;
            }

            // Hidden doors start inactive
            if (door.isHidden)
            {
                go.SetActive(false);
            }
        }

        private void CreateNewCorridorFloor(InteriorCorridor corridor, InteriorFloorData floor,
            Vector3 origin, float yOffset, Transform parent, InteriorPreset preset)
        {
            Color color = preset != null ? preset.corridorColor : DefaultCorridorColor;

            if (corridor.waypoints != null && corridor.waypoints.Length >= 2)
            {
                // Waypoint-based corridor: render segments between consecutive waypoints
                for (int i = 0; i < corridor.waypoints.Length - 1; i++)
                {
                    var posA = new Vector3(origin.x + corridor.waypoints[i].x, yOffset - 0.001f,
                        origin.z + corridor.waypoints[i].y);
                    var posB = new Vector3(origin.x + corridor.waypoints[i + 1].x, yOffset - 0.001f,
                        origin.z + corridor.waypoints[i + 1].y);

                    CreateCorridorSegment(posA, posB, corridor.width, color, parent);
                }
            }
            else if (corridor.roomA >= 0 && corridor.roomA < floor.rooms.Count
                     && corridor.roomB >= 0 && corridor.roomB < floor.rooms.Count)
            {
                // Fallback: room-center to room-center
                var roomA = floor.rooms[corridor.roomA];
                var roomB = floor.rooms[corridor.roomB];
                var posA = new Vector3(origin.x + roomA.position.x, yOffset - 0.001f, origin.z + roomA.position.y);
                var posB = new Vector3(origin.x + roomB.position.x, yOffset - 0.001f, origin.z + roomB.position.y);

                CreateCorridorSegment(posA, posB, corridor.width, color, parent);
            }
        }

        private void CreateFurniture(InteriorFurniture furniture, int floorIndex,
            Vector3 origin, float yOffset, Transform parent, InteriorPreset preset)
        {
            var go = new GameObject($"Furniture_{furniture.type}_{furniture.roomId}");
            go.transform.SetParent(parent);

            Vector3 size = GetFurnitureSize(furniture.type, furniture.scale);
            go.AddComponent<MeshFilter>().sharedMesh = GetSharedCubeMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GetCachedFloorMaterial(GetFurnitureColor(furniture.type, preset));

            go.transform.position = new Vector3(
                origin.x + furniture.position.x,
                yOffset + size.y * 0.5f + 0.03f,
                origin.z + furniture.position.y);
            go.transform.rotation = Quaternion.Euler(0f, furniture.angle, 0f);
            go.transform.localScale = size;

            // Attach DiscoveryInteractable for collectible furniture types
            if (DiscoveryInteractable.IsDiscoveryType(furniture.type))
            {
                var triggerCol = go.AddComponent<BoxCollider>();
                triggerCol.isTrigger = true;
                triggerCol.size = Vector3.one; // Already scaled by transform

                var discovery = go.AddComponent<DiscoveryInteractable>();
                discovery.discoveryId = $"{_currentBuildingId}_{floorIndex}_{furniture.roomId}_{furniture.type}";
                discovery.furnitureType = furniture.type;
                discovery.value = DiscoveryInteractable.GetDefaultValue(furniture.type);
                discovery.floorIndex = floorIndex;
            }
        }

        private void CreateCorridorSegment(Vector3 posA, Vector3 posB, float width, Color color, Transform parent)
        {
            var midpoint = (posA + posB) * 0.5f;
            float length = Vector3.Distance(posA, posB);
            float angle = Mathf.Atan2(posB.x - posA.x, posB.z - posA.z) * Mathf.Rad2Deg;

            var go = new GameObject("CorridorSeg");
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = GetCachedFloorMaterial(color);
            go.transform.position = midpoint;
            go.transform.rotation = Quaternion.Euler(90f, angle, 0f);
            go.transform.localScale = new Vector3(width, length, 1f);
        }

        // ===== Legacy rendering (backward compat) =====

#pragma warning disable CS0618
        private void CreateLegacyRoomFloor(RoomNode room, Vector3 origin)
        {
            var go = new GameObject($"RoomFloor_{room.type}");
            go.transform.SetParent(transform);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GetCachedFloorMaterial(LegacyRoomColors.GetValueOrDefault(room.type, Color.white));

            var pos = InteriorToWorld(room.position, origin);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(room.size.x, room.size.y, 1f);

            _spawnedObjects.Add(go);
        }

        private void CreateLegacyRoomWalls(RoomNode room, Vector3 origin)
        {
            var go = new GameObject($"RoomWalls_{room.type}");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = wallHeight;
            lr.endWidth = wallHeight;
            lr.material = wallMaterial != null ? wallMaterial : GetCachedWallMaterial();
            lr.startColor = DefaultWallColor;
            lr.endColor = DefaultWallColor;

            var center = InteriorToWorld(room.position, origin);
            float hw = room.size.x * 0.5f;
            float hh = room.size.y * 0.5f;
            float wy = center.y + 0.05f;

            lr.positionCount = 4;
            lr.SetPosition(0, new Vector3(center.x - hw, wy, center.z - hh));
            lr.SetPosition(1, new Vector3(center.x + hw, wy, center.z - hh));
            lr.SetPosition(2, new Vector3(center.x + hw, wy, center.z + hh));
            lr.SetPosition(3, new Vector3(center.x - hw, wy, center.z + hh));

            _spawnedObjects.Add(go);
        }

        private void CreateLegacyCorridorFloor(CorridorEdge corridor, List<RoomNode> rooms, Vector3 origin)
        {
            var roomA = rooms[corridor.roomA];
            var roomB = rooms[corridor.roomB];

            var posA = InteriorToWorld(roomA.position, origin);
            var posB = InteriorToWorld(roomB.position, origin);

            var midpoint = (posA + posB) * 0.5f;
            float length = Vector3.Distance(posA, posB);
            float angle = Mathf.Atan2(posB.x - posA.x, posB.z - posA.z) * Mathf.Rad2Deg;

            var go = new GameObject($"Corridor_{corridor.roomA}_{corridor.roomB}");
            go.transform.SetParent(transform);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = GetCachedFloorMaterial(DefaultCorridorColor);

            go.transform.position = new Vector3(midpoint.x, floorY - 0.001f, midpoint.z);
            go.transform.rotation = Quaternion.Euler(90f, angle, 0f);
            go.transform.localScale = new Vector3(corridor.width, length, 1f);

            _spawnedObjects.Add(go);
        }

        private void AttachLegacyRoomTrigger(RoomNode room, int roomIndex, Vector3 origin)
        {
            if (miniGameManager == null) return;
            var gameType = RoomTrigger.GetGameType(room.type);
            if (gameType == null) return;

            var go = new GameObject($"RoomTrigger_{room.type}_{roomIndex}");
            go.transform.SetParent(transform);
            go.transform.position = InteriorToWorld(room.position, origin);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(room.size.x * 0.6f, 2f, room.size.y * 0.6f);

            var trigger = go.AddComponent<RoomTrigger>();
            trigger.miniGameManager = miniGameManager;
            trigger.gameType = gameType.Value;
            trigger.roomIndex = roomIndex;
            trigger.buildingId = _currentBuildingId ?? "";
            trigger.seed = _currentSeed + roomIndex;

            _spawnedObjects.Add(go);
        }
#pragma warning restore CS0618

        // ===== Helpers =====

        private Color GetRoomColor(InteriorRoomType type, InteriorPreset preset)
        {
            // 1. Check preset overrides
            if (preset != null && preset.roomColorOverrides != null)
            {
                foreach (var entry in preset.roomColorOverrides)
                {
                    if (entry.roomType == type) return entry.color;
                }
            }

            // 2. Preset-level colors for special types
            if (preset != null && type == InteriorRoomType.SecretRoom)
                return preset.secretRoomColor;

            // 3. Default color map
            if (DefaultRoomColors.TryGetValue(type, out var color))
                return color;

            // 4. Fallback to preset floor color or white
            return preset != null ? preset.floorColor : Color.white;
        }

        private List<WallSegment> SplitWallByDoors(Vector3 wallStart, Vector3 wallEnd,
            List<DoorGap> doorGaps)
        {
            var segments = new List<WallSegment>();
            var wallDir = (wallEnd - wallStart).normalized;
            float wallLen = Vector3.Distance(wallStart, wallEnd);

            // Project each door gap onto this wall segment
            var gaps = new List<(float start, float end)>();
            foreach (var gap in doorGaps)
            {
                if (gap.isHidden) continue; // Hidden doors don't create visual gaps
                var doorWorld = new Vector3(
                    _currentWorldOrigin.x + gap.position.x,
                    wallStart.y,
                    _currentWorldOrigin.z + gap.position.y);

                // Calculate perpendicular distance from door to wall line
                var toGap = doorWorld - wallStart;
                float proj = Vector3.Dot(toGap, wallDir);
                float perpDist = Vector3.Distance(wallStart + wallDir * proj, doorWorld);

                // Only affect this wall if door is close enough
                if (perpDist < 0.5f && proj > -gap.halfWidth && proj < wallLen + gap.halfWidth)
                {
                    float gapStart = Mathf.Max(0f, proj - gap.halfWidth);
                    float gapEnd = Mathf.Min(wallLen, proj + gap.halfWidth);
                    if (gapEnd > gapStart)
                        gaps.Add((gapStart, gapEnd));
                }
            }

            if (gaps.Count == 0)
            {
                segments.Add(new WallSegment { start = wallStart, end = wallEnd });
                return segments;
            }

            // Sort gaps by start position
            gaps.Sort((a, b) => a.start.CompareTo(b.start));

            float cursor = 0f;
            foreach (var (gStart, gEnd) in gaps)
            {
                if (gStart > cursor + 0.1f)
                {
                    segments.Add(new WallSegment
                    {
                        start = wallStart + wallDir * cursor,
                        end = wallStart + wallDir * gStart
                    });
                }
                cursor = gEnd;
            }
            if (cursor < wallLen - 0.1f)
            {
                segments.Add(new WallSegment
                {
                    start = wallStart + wallDir * cursor,
                    end = wallEnd
                });
            }

            return segments;
        }

        private Vector3 InteriorToWorld(Vector2 interiorPos, Vector3 origin)
        {
            return origin + new Vector3(interiorPos.x, floorY, interiorPos.y);
        }

        private Color GetFurnitureColor(FurnitureType type, InteriorPreset preset)
        {
            Color baseColor = type switch
            {
                FurnitureType.Table or FurnitureType.Chair or FurnitureType.Bed or FurnitureType.Sofa
                    or FurnitureType.Cabinet or FurnitureType.Shelf or FurnitureType.Desk
                    or FurnitureType.Bookshelf or FurnitureType.ShopCounter
                    => new Color(0.53f, 0.39f, 0.27f),
                FurnitureType.Fridge or FurnitureType.Stove or FurnitureType.Sink
                    or FurnitureType.Register or FurnitureType.DisplayCase or FurnitureType.FileCabinet
                    or FurnitureType.Computer or FurnitureType.Machine
                    => new Color(0.63f, 0.67f, 0.72f),
                FurnitureType.Crate or FurnitureType.Pallet or FurnitureType.Container
                    => new Color(0.44f, 0.33f, 0.24f),
                FurnitureType.Barrel or FurnitureType.Workbench
                    => new Color(0.47f, 0.42f, 0.33f),
                FurnitureType.Lamp or FurnitureType.Document or FurnitureType.Photo or FurnitureType.Note
                    => new Color(0.85f, 0.8f, 0.62f),
                FurnitureType.Mannequin
                    => new Color(0.76f, 0.72f, 0.68f),
                FurnitureType.Rubble or FurnitureType.Debris or FurnitureType.Cobweb or FurnitureType.Vine
                    => new Color(0.34f, 0.31f, 0.28f),
                _ => new Color(0.6f, 0.62f, 0.65f)
            };

            if (preset == null)
            {
                return baseColor;
            }

            float decayTint = Mathf.Clamp01(preset.decayLevel * 0.45f);
            return Color.Lerp(baseColor, preset.wallColor, decayTint);
        }

        private Vector3 GetFurnitureSize(FurnitureType type, float scale)
        {
            Vector3 baseSize = type switch
            {
                FurnitureType.Bed => new Vector3(1.4f, 0.45f, 2.2f),
                FurnitureType.Sofa => new Vector3(1.8f, 0.55f, 0.9f),
                FurnitureType.Table => new Vector3(1.2f, 0.5f, 1.2f),
                FurnitureType.Chair => new Vector3(0.6f, 0.65f, 0.6f),
                FurnitureType.Shelf or FurnitureType.Bookshelf => new Vector3(0.45f, 1.35f, 1.5f),
                FurnitureType.Cabinet or FurnitureType.FileCabinet => new Vector3(0.7f, 1.05f, 0.65f),
                FurnitureType.Fridge => new Vector3(0.85f, 1.5f, 0.85f),
                FurnitureType.Stove or FurnitureType.Sink => new Vector3(0.9f, 0.95f, 0.65f),
                FurnitureType.ShopCounter => new Vector3(2.1f, 1f, 0.8f),
                FurnitureType.Register => new Vector3(0.55f, 0.35f, 0.45f),
                FurnitureType.DisplayCase => new Vector3(1.4f, 0.9f, 0.7f),
                FurnitureType.Mannequin => new Vector3(0.45f, 1.7f, 0.45f),
                FurnitureType.Crate or FurnitureType.Container => new Vector3(0.8f, 0.65f, 0.8f),
                FurnitureType.Barrel => new Vector3(0.72f, 0.9f, 0.72f),
                FurnitureType.Machine => new Vector3(1.25f, 1.15f, 1.1f),
                FurnitureType.Workbench or FurnitureType.Desk => new Vector3(1.4f, 0.9f, 0.7f),
                FurnitureType.Pallet => new Vector3(1.2f, 0.2f, 1.2f),
                FurnitureType.Computer => new Vector3(0.45f, 0.3f, 0.35f),
                FurnitureType.Rubble or FurnitureType.Debris => new Vector3(0.9f, 0.35f, 0.9f),
                FurnitureType.Cobweb or FurnitureType.Vine => new Vector3(0.7f, 0.05f, 0.7f),
                FurnitureType.Document or FurnitureType.Photo or FurnitureType.Note => new Vector3(0.45f, 0.05f, 0.35f),
                FurnitureType.Lamp => new Vector3(0.35f, 1.1f, 0.35f),
                _ => new Vector3(0.8f, 0.8f, 0.8f)
            };

            return baseSize * Mathf.Max(0.45f, scale);
        }

        private Material GetCachedFloorMaterial(Color color)
        {
            if (_floorMaterialCache.TryGetValue(color, out var cached))
                return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            _floorMaterialCache[color] = mat;
            return mat;
        }

        private Material GetCachedWallMaterial()
        {
            if (_cachedWallMaterial != null) return _cachedWallMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            _cachedWallMaterial = new Material(shader);
            _cachedWallMaterial.color = DefaultWallColor;
            return _cachedWallMaterial;
        }

        private Mesh GetSharedQuadMesh()
        {
            if (_sharedQuadMesh != null) return _sharedQuadMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _sharedQuadMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return _sharedQuadMesh;
        }

        private Mesh GetSharedCubeMesh()
        {
            if (_sharedCubeMesh != null) return _sharedCubeMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _sharedCubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return _sharedCubeMesh;
        }

        // ===== Internal types =====

        private class FloorRenderGroup
        {
            public int floorIndex;
            public GameObject root;
        }

        private struct WallSegment
        {
            public Vector3 start;
            public Vector3 end;
        }

        private struct DoorGap
        {
            public Vector2 position; // World-relative 2D
            public float halfWidth;
            public bool isHidden;
        }
    }
}
