using System.Collections.Generic;
using UnityEngine;

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

        [Header("Settings")]
        public float floorY = 0.02f;
        public float wallHeight = 0.2f;

        private readonly List<GameObject> _spawnedObjects = new();

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

        public void Render(InteriorMapData data, Vector3 worldOrigin)
        {
            Clear();

            foreach (var room in data.rooms)
                CreateRoomFloor(room, worldOrigin);

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
        }

        private void CreateRoomFloor(RoomNode room, Vector3 origin)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"RoomFloor_{room.type}";
            go.transform.SetParent(transform);

            // Quad faces +Y when rotated to lay flat
            var pos = InteriorToWorld(room.position, origin);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(room.size.x, room.size.y, 1f);

            // Disable collider from primitive (NavMesh will re-bake)
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = CreateFloorMaterial(RoomColors.GetValueOrDefault(room.type, Color.white));
            }

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
            lr.material = wallMaterial != null ? wallMaterial : CreateWallMaterial();
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

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Corridor_{corridor.roomA}_{corridor.roomB}";
            go.transform.SetParent(transform);

            go.transform.position = new Vector3(midpoint.x, floorY - 0.001f, midpoint.z);
            go.transform.rotation = Quaternion.Euler(90f, angle, 0f);
            go.transform.localScale = new Vector3(corridor.width, length, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.material = CreateFloorMaterial(CorridorColor);

            _spawnedObjects.Add(go);
        }

        private Vector3 InteriorToWorld(Vector2 interiorPos, Vector3 origin)
        {
            return origin + new Vector3(interiorPos.x, floorY, interiorPos.y);
        }

        private static Material CreateFloorMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private static Material CreateWallMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = WallColor;
            return mat;
        }
    }
}
