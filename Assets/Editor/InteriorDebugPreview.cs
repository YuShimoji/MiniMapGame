using UnityEngine;
using UnityEditor;
using MiniMapGame.Interior;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    public class InteriorDebugPreview : EditorWindow
    {
        private int _seed = 12345;
        private InteriorPreset _preset;
        private BuildingCategory _category = BuildingCategory.Residential;
        private ShopSubtype _shopSubtype = ShopSubtype.None;
        private int _tier = 1;
        private int _floors = 3;
        private float _footprintWidth = 15f;
        private float _footprintHeight = 12f;
        private int _shapeType = 0;
        private bool _isLandmark = false;
        private GeneratorType _mapType = GeneratorType.Organic;

        private InteriorMapData _generatedData;
        private int _currentFloorIndex = 0;

        [MenuItem("MiniMapGame/Interior Debug Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<InteriorDebugPreview>("Interior Preview");
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            GUILayout.Label("Interior Generator Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (GUILayout.Button("Randomize", GUILayout.Width(100)))
            {
                _seed = Random.Range(0, 999999);
            }
            EditorGUILayout.EndHorizontal();

            _preset = (InteriorPreset)EditorGUILayout.ObjectField("Interior Preset", _preset, typeof(InteriorPreset), false);

            EditorGUILayout.Space();
            GUILayout.Label("Building Context", EditorStyles.boldLabel);

            _category = (BuildingCategory)EditorGUILayout.EnumPopup("Category", _category);

            EditorGUI.BeginDisabledGroup(_category != BuildingCategory.Commercial);
            _shopSubtype = (ShopSubtype)EditorGUILayout.EnumPopup("Shop Subtype", _shopSubtype);
            EditorGUI.EndDisabledGroup();

            _tier = EditorGUILayout.IntSlider("Tier", _tier, 0, 2);
            _floors = EditorGUILayout.IntSlider("Floors", _floors, 1, 10);
            _footprintWidth = EditorGUILayout.FloatField("Footprint Width", _footprintWidth);
            _footprintHeight = EditorGUILayout.FloatField("Footprint Height", _footprintHeight);
            _shapeType = EditorGUILayout.IntSlider("Shape Type", _shapeType, 0, 3);
            _isLandmark = EditorGUILayout.Toggle("Is Landmark", _isLandmark);
            _mapType = (GeneratorType)EditorGUILayout.EnumPopup("Map Type", _mapType);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_preset == null);
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                GenerateInterior();
            }
            EditorGUI.EndDisabledGroup();

            if (_generatedData != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Generated Data", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Total Room Count", _generatedData.totalRoomCount.ToString());
                EditorGUILayout.LabelField("Total Discovery Count", _generatedData.totalDiscoveryCount.ToString());

                if (_generatedData.floors != null && _generatedData.floors.Count > 0)
                {
                    _currentFloorIndex = EditorGUILayout.IntSlider("Floor Index", _currentFloorIndex, 0, _generatedData.floors.Count - 1);

                    var currentFloor = _generatedData.floors[_currentFloorIndex];
                    EditorGUILayout.LabelField("Dead Space Ratio", currentFloor.deadSpaceRatio.ToString("F2"));
                    EditorGUILayout.LabelField("Rooms on Floor", currentFloor.rooms?.Count.ToString() ?? "0");
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Preview is shown in Scene View. Select a SceneView to see the generated interior layout.", MessageType.Info);
            }
        }

        private void GenerateInterior()
        {
            if (_preset == null)
            {
                Debug.LogWarning("InteriorPreset is required to generate interior.");
                return;
            }

            var context = new InteriorBuildingContext
            {
                buildingId = "debug_preview",
                footprintWidth = _footprintWidth,
                footprintHeight = _footprintHeight,
                angle = 0f,
                tier = _tier,
                floors = _floors,
                shapeType = _shapeType,
                isLandmark = _isLandmark,
                category = _category,
                shopSubtype = _shopSubtype,
                elevation = 0f,
                nearCoast = false,
                nearRiver = false,
                nearHill = false,
                mapType = _mapType
            };

            _generatedData = InteriorMapGenerator.Generate(context, _preset, _seed);
            _currentFloorIndex = 0;

            SceneView.RepaintAll();
            Repaint();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_generatedData == null || _generatedData.floors == null || _generatedData.floors.Count == 0)
                return;

            if (_currentFloorIndex < 0 || _currentFloorIndex >= _generatedData.floors.Count)
                return;

            var floor = _generatedData.floors[_currentFloorIndex];
            if (floor.rooms == null)
                return;

            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 300, 20), $"Floor {_currentFloorIndex} - {floor.rooms.Count} rooms", EditorStyles.whiteLargeLabel);
            Handles.EndGUI();

            // Draw rooms
            foreach (var room in floor.rooms)
            {
                DrawRoom(room);
            }

            // Draw doors
            if (floor.doors != null)
            {
                foreach (var door in floor.doors)
                {
                    DrawDoor(door);
                }
            }
        }

        private void DrawRoom(InteriorRoom room)
        {
            Vector3 center = new Vector3(room.position.x, _currentFloorIndex * 4f, room.position.y);
            Vector3 size3D = new Vector3(room.size.x, 0, room.size.y);

            // Apply rotation
            Quaternion rotation = Quaternion.Euler(0, room.rotation, 0);

            // Calculate corners
            Vector3[] corners = new Vector3[4];
            corners[0] = center + rotation * new Vector3(-size3D.x * 0.5f, 0, -size3D.z * 0.5f);
            corners[1] = center + rotation * new Vector3(size3D.x * 0.5f, 0, -size3D.z * 0.5f);
            corners[2] = center + rotation * new Vector3(size3D.x * 0.5f, 0, size3D.z * 0.5f);
            corners[3] = center + rotation * new Vector3(-size3D.x * 0.5f, 0, size3D.z * 0.5f);

            // Get color based on room type
            Color roomColor = GetRoomTypeColor(room.type);
            roomColor.a = room.isSecret ? 0.5f : 0.3f;

            Color outlineColor = room.isSecret ? Color.magenta : Color.black;
            outlineColor.a = 0.8f;

            // Draw filled rectangle
            Handles.DrawSolidRectangleWithOutline(corners, roomColor, outlineColor);

            // Draw label
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            string label = room.type.ToString();
            if (room.isSecret)
                label += " (Secret)";
            if (room.discoverySlotCount > 0)
                label += $"\n{room.discoverySlotCount} discoveries";

            Handles.Label(center, label, labelStyle);
        }

        private void DrawDoor(InteriorDoor door)
        {
            Vector3 position = new Vector3(door.position.x, _currentFloorIndex * 4f, door.position.y);
            float radius = door.width * 0.5f;

            Color doorColor;
            if (door.isLocked)
                doorColor = Color.red;
            else if (door.isHidden)
                doorColor = Color.yellow;
            else
                doorColor = Color.green;

            doorColor.a = 0.8f;

            Handles.color = doorColor;
            Handles.DrawSolidDisc(position, Vector3.up, radius);
            Handles.color = Color.white;
        }

        private Color GetRoomTypeColor(InteriorRoomType type)
        {
            switch (type)
            {
                case InteriorRoomType.Entrance:
                    return new Color(0.2f, 0.8f, 0.2f); // Green
                case InteriorRoomType.Corridor:
                    return new Color(0.6f, 0.6f, 0.6f); // Gray
                case InteriorRoomType.Storage:
                    return new Color(0.8f, 0.6f, 0.2f); // Orange
                case InteriorRoomType.Office:
                    return new Color(0.2f, 0.4f, 0.8f); // Blue
                case InteriorRoomType.Shop:
                    return new Color(0.8f, 0.2f, 0.8f); // Magenta
                case InteriorRoomType.Utility:
                    return new Color(0.5f, 0.3f, 0.1f); // Brown
                case InteriorRoomType.Special:
                    return new Color(0.8f, 0.8f, 0.2f); // Yellow
                case InteriorRoomType.Living:
                    return new Color(0.2f, 0.6f, 0.6f); // Cyan
                case InteriorRoomType.Kitchen:
                    return new Color(0.9f, 0.4f, 0.2f); // Red-Orange
                case InteriorRoomType.Bathroom:
                    return new Color(0.4f, 0.7f, 0.9f); // Light Blue
                case InteriorRoomType.Bedroom:
                    return new Color(0.6f, 0.3f, 0.7f); // Purple
                case InteriorRoomType.Production:
                    return new Color(0.7f, 0.5f, 0.3f); // Tan
                default:
                    return new Color(0.5f, 0.5f, 0.5f); // Default Gray
            }
        }
    }
}
