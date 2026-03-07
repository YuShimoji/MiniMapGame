using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.MiniGame;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Renders InteriorMapData as 3D primitives (Quad floors + LineRenderer walls + corridor floors).
    /// Room types color-coded: Entrance=green, Boss=red, Treasure=gold, Alcove=grey, Normal=white.
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

        private readonly List<GameObject> _spawnedObjects = new();
        private readonly Dictionary<Color, Material> _floorMaterialCache = new();
        private Material _cachedWallMaterial;
        private Mesh _sharedQuadMesh;

        private static readonly Dictionary<RoomType, Color> RoomColors = new()
        {
            { RoomType.Entrance, new Color(0.2f, 0.7f, 0.3f) },
            { RoomType.Boss, new Color(0.8f, 0.2f, 0.2f) },
            { RoomType.Treasure, new Color(0.9f, 0.75f, 0.2f) },
            { RoomType.Alcove, new Color(0.45f, 0.45f, 0.5f) },
            { RoomType.Normal, new Color(0.6f, 0.65f, 0.7f) }
        };

        private static readonly Color WallColor = new(0.3f, 0.35f, 0.4f);
        private static readonly Color CorridorColor = new(0.5f, 0.5f, 0.55f);

        private string _currentBuildingId;
        private int _currentSeed;

        public void Render(InteriorMapData data, Vector3 worldOrigin,
            string buildingId = null, int seed = 0)
        {
            Clear();
            _currentBuildingId = buildingId;
            _currentSeed = seed;

            for (int i = 0; i < data.rooms.Count; i++)
            {
                CreateRoomFloor(data.rooms[i], worldOrigin);
                AttachRoomTrigger(data.rooms[i], i, worldOrigin);
            }

            foreach (var room in data.rooms)
                CreateRoomWalls(room, worldOrigin);

            foreach (var corridor in data.corridors)
                CreateCorridorFloor(corridor, data, worldOrigin);
        }

        public void Clear()
        {
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
        }

        private void CreateRoomFloor(RoomNode room, Vector3 origin)
        {
            var go = new GameObject($"RoomFloor_{room.type}");
            go.transform.SetParent(transform);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GetCachedFloorMaterial(RoomColors.GetValueOrDefault(room.type, Color.white));

            var pos = InteriorToWorld(room.position, origin);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(room.size.x, room.size.y, 1f);

            _spawnedObjects.Add(go);
        }

        private void CreateRoomWalls(RoomNode room, Vector3 origin)
        {
            var go = new GameObject($"RoomWalls_{room.type}");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.startWidth = wallHeight;
            lr.endWidth = wallHeight;
            lr.material = wallMaterial != null ? wallMaterial : GetCachedWallMaterial();
            lr.startColor = WallColor;
            lr.endColor = WallColor;

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

        private void CreateCorridorFloor(CorridorEdge corridor, InteriorMapData data, Vector3 origin)
        {
            var roomA = data.rooms[corridor.roomA];
            var roomB = data.rooms[corridor.roomB];

            var posA = InteriorToWorld(roomA.position, origin);
            var posB = InteriorToWorld(roomB.position, origin);

            var midpoint = (posA + posB) * 0.5f;
            float length = Vector3.Distance(posA, posB);
            float angle = Mathf.Atan2(posB.x - posA.x, posB.z - posA.z) * Mathf.Rad2Deg;

            var go = new GameObject($"Corridor_{corridor.roomA}_{corridor.roomB}");
            go.transform.SetParent(transform);

            go.AddComponent<MeshFilter>().sharedMesh = GetSharedQuadMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = GetCachedFloorMaterial(CorridorColor);

            go.transform.position = new Vector3(midpoint.x, floorY - 0.001f, midpoint.z);
            go.transform.rotation = Quaternion.Euler(90f, angle, 0f);
            go.transform.localScale = new Vector3(corridor.width, length, 1f);

            _spawnedObjects.Add(go);
        }

        private void AttachRoomTrigger(RoomNode room, int roomIndex, Vector3 origin)
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

        private Vector3 InteriorToWorld(Vector2 interiorPos, Vector3 origin)
        {
            return origin + new Vector3(interiorPos.x, floorY, interiorPos.y);
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
            _cachedWallMaterial.color = WallColor;
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
    }
}
