using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using TMPro;
using MiniMapGame.Runtime;
using MiniMapGame.Player;
using MiniMapGame.Data;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to set up a playable test scene.
    /// Menu: MiniMapGame > Bootstrap Test Scene
    /// Creates building prefabs, assembles required GameObjects, and wires references.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string PrefabFolder = "Assets/Resources/Prefabs";

        [MenuItem("MiniMapGame/Create Building Prefabs")]
        public static void CreateBuildingPrefabs()
        {
            EnsureFolder(PrefabFolder);

            CreateCubePrefab("NormalBuilding", new Color(0.22f, 0.28f, 0.38f, 1f));
            CreateCubePrefab("LandmarkBuilding", new Color(0.10f, 0.16f, 0.25f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBootstrapper] Building prefabs created in " + PrefabFolder);
        }

        [MenuItem("MiniMapGame/Bootstrap Test Scene")]
        public static void BootstrapTestScene()
        {
            // Ensure prefabs and presets exist
            CreateBuildingPrefabs();
            MapPresetCreator.CreateDefaultPresets();

            // 1. Map Manager root
            var mapManagerGo = FindOrCreate("MapManager");
            var mapManager = EnsureComponent<MapManager>(mapManagerGo);

            // 2. MapRenderer
            var mapRendererGo = FindOrCreate("MapRenderer", mapManagerGo.transform);
            var mapRenderer = EnsureComponent<MapRenderer>(mapRendererGo);
            mapManager.mapRenderer = mapRenderer;

            // Create default road materials if not assigned
            if (mapRenderer.roadOuterMaterial == null)
                mapRenderer.roadOuterMaterial = CreateUnlitMaterial("RoadOuter", new Color(0.17f, 0.24f, 0.35f));
            if (mapRenderer.roadInnerMaterial == null)
                mapRenderer.roadInnerMaterial = CreateUnlitMaterial("RoadInner", new Color(0.22f, 0.31f, 0.41f));

            // 3. BuildingSpawner
            var buildingSpawnerGo = FindOrCreate("BuildingSpawner", mapManagerGo.transform);
            var buildingSpawner = EnsureComponent<BuildingSpawner>(buildingSpawnerGo);
            mapManager.buildingSpawner = buildingSpawner;

            // Assign prefabs
            var normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/NormalBuilding.prefab");
            var landmarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/LandmarkBuilding.prefab");
            buildingSpawner.normalBuildingPrefab = normalPrefab;
            buildingSpawner.landmarkBuildingPrefab = landmarkPrefab;

            // Assign first available preset
            var preset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Coastal.asset");
            if (preset != null)
                mapManager.activePreset = preset;

            // Ground material
            mapManager.groundMaterial = CreateUnlitMaterial("Ground", new Color(0.035f, 0.047f, 0.07f));

            // 4. Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = FindOrCreate("Main Camera");
                cam = EnsureComponent<Camera>(camGo);
                camGo.tag = "MainCamera";
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.024f, 0.035f, 0.05f);
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            var camCtrl = EnsureComponent<CameraController>(cam.gameObject);

            // 5. Player
            var playerGo = FindOrCreate("Player");
            var agent = EnsureComponent<NavMeshAgent>(playerGo);
            agent.speed = 5f;
            agent.angularSpeed = 360f;
            var playerMovement = EnsureComponent<PlayerMovement>(playerGo);
            camCtrl.playerTarget = playerGo.transform;

            // Place player at approximate map center
            playerGo.transform.position = new Vector3(430f, 0f, 290f);

            // 6. Ensure Ground layer exists (user may need to manually create it)
            EnsureLayerNote("Ground");

            // 7. UI Canvas for interaction text
            SetupInteractionUI(playerMovement);

            EditorUtility.SetDirty(mapManager);
            EditorUtility.SetDirty(mapRenderer);
            EditorUtility.SetDirty(buildingSpawner);
            Debug.Log("[SceneBootstrapper] Test scene bootstrapped. Press Play to generate map.");
            Debug.Log("[SceneBootstrapper] NOTE: Ensure 'Ground' layer exists in Tags & Layers settings.");
        }

        private static void CreateCubePrefab(string name, Color color)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;

            // Material
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            string matPath = $"{PrefabFolder}/{name}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            // Add BoxCollider as trigger for interaction
            var col = go.GetComponent<BoxCollider>();
            // Add a larger trigger collider for interaction zone
            var triggerCol = go.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1.5f, 2f, 1.5f);

            // Static flag for NavMesh obstacle
            go.isStatic = true;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static Material CreateUnlitMaterial(string name, Color color)
        {
            EnsureFolder("Assets/Resources/Materials");
            string path = $"Assets/Resources/Materials/{name}_Mat.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SetupInteractionUI(PlayerMovement playerMovement)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = FindOrCreate("UICanvas");
                canvas = EnsureComponent<Canvas>(canvasGo);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                EnsureComponent<UnityEngine.UI.CanvasScaler>(canvasGo);
                EnsureComponent<UnityEngine.UI.GraphicRaycaster>(canvasGo);
            }

            // Interaction message
            var msgGo = FindOrCreate("InteractionMessage", canvas.transform);
            var tmp = EnsureComponent<TextMeshProUGUI>(msgGo);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18f;
            tmp.color = new Color(0.8f, 0.9f, 1f, 0.9f);

            var rect = msgGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0f);
            rect.anchorMax = new Vector2(0.7f, 0.08f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            playerMovement.interactionMessageText = tmp;
            msgGo.SetActive(false);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static GameObject FindOrCreate(string name, Transform parent = null)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void EnsureLayerNote(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                Debug.LogWarning($"[SceneBootstrapper] Layer '{layerName}' not found. " +
                    $"Please create it in Edit > Project Settings > Tags and Layers.");
            }
        }
    }
}
