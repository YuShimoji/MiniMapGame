using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MiniMapGame.Runtime;
using MiniMapGame.Player;
using MiniMapGame.Data;
using MiniMapGame.GameLoop;

namespace MiniMapGame.EditorTools
{
    /// <summary>
    /// Editor utility to set up a playable test scene.
    /// Menu: MiniMapGame > Bootstrap Test Scene
    /// Creates building prefabs, game loop prefabs, assembles required GameObjects, and wires references.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string PrefabFolder = "Assets/Resources/Prefabs";
        private const string GameLoopPrefabFolder = "Assets/Resources/Prefabs/GameLoop";

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

        [MenuItem("MiniMapGame/Create GameLoop Prefabs")]
        public static void CreateGameLoopPrefabs()
        {
            EnsureFolder(GameLoopPrefabFolder);

            CreateValueObjectPrefab();
            CreateEncounterZonePrefab();
            CreateExtractionPointPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBootstrapper] GameLoop prefabs created in " + GameLoopPrefabFolder);
        }

        [MenuItem("MiniMapGame/Bootstrap Test Scene")]
        public static void BootstrapTestScene()
        {
            // Ensure prefabs and presets exist
            CreateBuildingPrefabs();
            CreateGameLoopPrefabs();
            MapPresetCreator.CreateDefaultPresets();
            CreateMapEventBusAsset();

            // 1. Map Manager root
            var mapManagerGo = FindOrCreate("MapManager");
            var mapManager = EnsureComponent<MapManager>(mapManagerGo);

            // 2. MapRenderer
            var mapRendererGo = FindOrCreate("MapRenderer", mapManagerGo.transform);
            var mapRenderer = EnsureComponent<MapRenderer>(mapRendererGo);
            mapManager.mapRenderer = mapRenderer;

            if (mapRenderer.roadOuterMaterial == null)
                mapRenderer.roadOuterMaterial = CreateUnlitMaterial("RoadOuter", new Color(0.17f, 0.24f, 0.35f));
            if (mapRenderer.roadInnerMaterial == null)
                mapRenderer.roadInnerMaterial = CreateUnlitMaterial("RoadInner", new Color(0.22f, 0.31f, 0.41f));

            // 3. BuildingSpawner
            var buildingSpawnerGo = FindOrCreate("BuildingSpawner", mapManagerGo.transform);
            var buildingSpawner = EnsureComponent<BuildingSpawner>(buildingSpawnerGo);
            mapManager.buildingSpawner = buildingSpawner;

            var normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/NormalBuilding.prefab");
            var landmarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/LandmarkBuilding.prefab");
            buildingSpawner.normalBuildingPrefab = normalPrefab;
            buildingSpawner.landmarkBuildingPrefab = landmarkPrefab;

            // Assign preset
            var preset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Coastal.asset");
            if (preset != null)
                mapManager.activePreset = preset;

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
            playerGo.tag = "Player";
            var agent = EnsureComponent<NavMeshAgent>(playerGo);
            agent.speed = 5f;
            agent.angularSpeed = 360f;
            var playerMovement = EnsureComponent<PlayerMovement>(playerGo);
            camCtrl.playerTarget = playerGo.transform;

            // Ensure player has collider + kinematic rigidbody for trigger detection
            var playerCol = EnsureComponent<CapsuleCollider>(playerGo);
            playerCol.height = 2f;
            playerCol.center = new Vector3(0, 1f, 0);
            var playerRb = EnsureComponent<Rigidbody>(playerGo);
            playerRb.isKinematic = true;

            playerGo.transform.position = new Vector3(430f, 0f, 290f);

            // 6. Ensure Ground layer
            EnsureLayerNote("Ground");

            // 7. UI Canvas
            var canvas = SetupCanvas();
            SetupInteractionUI(playerMovement, canvas);

            // 8. GameLoop system
            var eventBus = AssetDatabase.LoadAssetAtPath<MapEventBus>("Assets/Resources/MapEventBus.asset");
            var gameLoopUI = SetupGameLoopUI(canvas);
            SetupGameLoopController(mapManager, eventBus, gameLoopUI);

            EditorUtility.SetDirty(mapManager);
            EditorUtility.SetDirty(mapRenderer);
            EditorUtility.SetDirty(buildingSpawner);
            Debug.Log("[SceneBootstrapper] Test scene bootstrapped. Press Play to generate map.");
            Debug.Log("[SceneBootstrapper] NOTE: Ensure 'Ground' layer and 'Player' tag exist in Tags & Layers.");
        }

        // ── Building Prefabs ──

        private static void CreateCubePrefab(string name, Color color)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, $"{PrefabFolder}/{name}_Mat.mat");
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var triggerCol = go.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1.5f, 2f, 1.5f);

            go.isStatic = true;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ── GameLoop Prefabs ──

        private static void CreateValueObjectPrefab()
        {
            string path = $"{GameLoopPrefabFolder}/ValueObject.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ValueObject";
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.9f, 0.75f, 0.2f);
            mat.SetFloat("_Smoothness", 0.8f);
            AssetDatabase.CreateAsset(mat, $"{GameLoopPrefabFolder}/ValueObject_Mat.mat");
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3f, 3f, 3f); // Larger trigger zone

            go.AddComponent<ValueObjectBehaviour>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateEncounterZonePrefab()
        {
            string path = $"{GameLoopPrefabFolder}/EncounterZone.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = new GameObject("EncounterZone");
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 5f;

            go.AddComponent<EncounterZone>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateExtractionPointPrefab()
        {
            string path = $"{GameLoopPrefabFolder}/ExtractionPoint.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ExtractionPoint";
            go.transform.localScale = new Vector3(2f, 3f, 2f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.7f, 0.5f, 0.7f);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(mat, $"{GameLoopPrefabFolder}/ExtractionPoint_Mat.mat");
            go.GetComponent<Renderer>().sharedMaterial = mat;

            // Replace MeshCollider with SphereCollider trigger
            var meshCol = go.GetComponent<MeshCollider>();
            if (meshCol != null) Object.DestroyImmediate(meshCol);
            var capsuleCol = go.GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Object.DestroyImmediate(capsuleCol);

            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2f;

            go.AddComponent<ExtractionPoint>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ── MapEventBus Asset ──

        private static void CreateMapEventBusAsset()
        {
            EnsureFolder("Assets/Resources");
            string path = "Assets/Resources/MapEventBus.asset";
            if (AssetDatabase.LoadAssetAtPath<MapEventBus>(path) != null) return;

            var bus = ScriptableObject.CreateInstance<MapEventBus>();
            AssetDatabase.CreateAsset(bus, path);
            Debug.Log("[SceneBootstrapper] Created MapEventBus asset.");
        }

        // ── UI Setup ──

        private static Canvas SetupCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var canvasGo = FindOrCreate("UICanvas");
            canvas = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureComponent<CanvasScaler>(canvasGo);
            EnsureComponent<GraphicRaycaster>(canvasGo);
            return canvas;
        }

        private static void SetupInteractionUI(PlayerMovement playerMovement, Canvas canvas)
        {
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

        private static GameLoopUI SetupGameLoopUI(Canvas canvas)
        {
            var uiGo = FindOrCreate("GameLoopUI", canvas.transform);
            var ui = EnsureComponent<GameLoopUI>(uiGo);

            // HUD - top left
            var hudGo = FindOrCreate("HUD", uiGo.transform);
            SetRect(hudGo, new Vector2(0, 0.85f), new Vector2(0.25f, 1f));

            ui.valueText = CreateTMPChild(hudGo.transform, "ValueText", "Value: 0",
                new Vector2(0, 0.66f), new Vector2(1, 1f), TextAlignmentOptions.TopLeft);
            ui.encounterText = CreateTMPChild(hudGo.transform, "EncounterText", "Encounters: 0",
                new Vector2(0, 0.33f), new Vector2(1, 0.66f), TextAlignmentOptions.TopLeft);
            ui.itemCountText = CreateTMPChild(hudGo.transform, "ItemCountText", "Items: 0",
                new Vector2(0, 0), new Vector2(1, 0.33f), TextAlignmentOptions.TopLeft);

            // Center message overlay
            var msgGo = FindOrCreate("GameLoopMessage", uiGo.transform);
            ui.messageText = EnsureComponent<TextMeshProUGUI>(msgGo);
            ui.messageText.alignment = TextAlignmentOptions.Center;
            ui.messageText.fontSize = 32f;
            ui.messageText.color = new Color(1f, 0.4f, 0.3f, 0.95f);
            SetRect(msgGo, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.6f));
            msgGo.SetActive(false);

            // Extraction decision panel
            var extractPanel = FindOrCreate("ExtractionPanel", uiGo.transform);
            ui.extractionPanel = extractPanel;
            SetRect(extractPanel, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));

            var bgImg = EnsureComponent<Image>(extractPanel);
            bgImg.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);

            ui.extractionInfoText = CreateTMPChild(extractPanel.transform, "ExtractInfo", "Extract?",
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Center);

            // Buttons
            var extractBtnGo = CreateButton(extractPanel.transform, "ExtractButton", "EXTRACT",
                new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.35f), new Color(0.2f, 0.7f, 0.4f));
            ui.extractButton = extractBtnGo.GetComponent<Button>();

            var continueBtnGo = CreateButton(extractPanel.transform, "ContinueButton", "CONTINUE",
                new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.35f), new Color(0.5f, 0.5f, 0.6f));
            ui.continueButton = continueBtnGo.GetComponent<Button>();

            extractPanel.SetActive(false);

            // Result panel
            var resultPanel = FindOrCreate("ResultPanel", uiGo.transform);
            ui.resultPanel = resultPanel;
            SetRect(resultPanel, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));

            var resultBg = EnsureComponent<Image>(resultPanel);
            resultBg.color = new Color(0.03f, 0.06f, 0.1f, 0.95f);

            ui.resultText = CreateTMPChild(resultPanel.transform, "ResultText", "",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), TextAlignmentOptions.Center);
            ui.resultText.fontSize = 24f;

            resultPanel.SetActive(false);

            EditorUtility.SetDirty(ui);
            return ui;
        }

        private static void SetupGameLoopController(MapManager mapManager, MapEventBus eventBus, GameLoopUI gameLoopUI)
        {
            var go = FindOrCreate("GameLoopController");
            var controller = EnsureComponent<GameLoopController>(go);
            controller.mapManager = mapManager;
            controller.eventBus = eventBus;
            controller.gameLoopUI = gameLoopUI;

            // Assign game loop prefabs
            controller.valueObjectPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{GameLoopPrefabFolder}/ValueObject.prefab");
            controller.encounterZonePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{GameLoopPrefabFolder}/EncounterZone.prefab");
            controller.extractionPointPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{GameLoopPrefabFolder}/ExtractionPoint.prefab");

            EditorUtility.SetDirty(controller);
        }

        // ── Helpers ──

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

        private static TextMeshProUGUI CreateTMPChild(Transform parent, string name,
            string text, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align)
        {
            var go = FindOrCreate(name, parent);
            var tmp = EnsureComponent<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.alignment = align;
            tmp.fontSize = 16f;
            tmp.color = new Color(0.7f, 0.85f, 1f, 0.9f);
            SetRect(go, anchorMin, anchorMax);
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = FindOrCreate(name, parent);
            var img = EnsureComponent<Image>(go);
            img.color = color;
            EnsureComponent<Button>(go);
            SetRect(go, anchorMin, anchorMax);

            var textGo = FindOrCreate($"{name}_Text", go.transform);
            var tmp = EnsureComponent<TextMeshProUGUI>(textGo);
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18f;
            tmp.color = Color.white;
            SetRect(textGo, Vector2.zero, Vector2.one);

            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
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
