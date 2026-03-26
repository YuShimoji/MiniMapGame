using UnityEngine;

using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TMPro;
using MiniMapGame.Runtime;
using MiniMapGame.Player;
using MiniMapGame.Data;
using MiniMapGame.GameLoop;
using MiniMapGame.UI;
using MiniMapGame.Interior;


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
            // Ensure prefabs, presets, and profiles exist
            CreateBuildingPrefabs();
            RoadProfileCreator.CreateDefaultProfiles();
            MapPresetCreator.CreateDefaultPresets();
            AutoBindRoadProfiles();
            CreateMapEventBusAsset();

            // 1. Map Manager root
            var mapManagerGo = FindOrCreate("MapManager");
            var mapManager = EnsureComponent<MapManager>(mapManagerGo);

            // 2. MapRenderer
            var mapRendererGo = FindOrCreate("MapRenderer", mapManagerGo.transform);
            var mapRenderer = EnsureComponent<MapRenderer>(mapRendererGo);
            mapManager.mapRenderer = mapRenderer;
            mapRenderer.mapManager = mapManager;

            // Per-tier road materials
            if (mapRenderer.roadOuterMaterials == null || mapRenderer.roadOuterMaterials.Length < 3)
                mapRenderer.roadOuterMaterials = new Material[3];
            if (mapRenderer.roadInnerMaterials == null || mapRenderer.roadInnerMaterials.Length < 3)
                mapRenderer.roadInnerMaterials = new Material[3];

            Color[] defaultOuter = { new(0.17f, 0.24f, 0.35f), new(0.20f, 0.27f, 0.36f), new(0.24f, 0.30f, 0.38f) };
            Color[] defaultInner = { new(0.22f, 0.31f, 0.41f), new(0.26f, 0.34f, 0.42f), new(0.30f, 0.37f, 0.44f) };
            for (int i = 0; i < 3; i++)
            {
                if (mapRenderer.roadOuterMaterials[i] == null)
                    mapRenderer.roadOuterMaterials[i] = CreateUnlitMaterial($"RoadOuter_T{i}", defaultOuter[i]);
                if (mapRenderer.roadInnerMaterials[i] == null)
                    mapRenderer.roadInnerMaterials[i] = CreateUnlitMaterial($"RoadInner_T{i}", defaultInner[i]);
            }
            if (mapRenderer.bridgePillarMaterial == null)
                mapRenderer.bridgePillarMaterial = CreateLitMaterial("BridgePillar", new Color(0.35f, 0.35f, 0.38f));

            // New Road.shader materials
            if (mapRenderer.roadMaterials == null || mapRenderer.roadMaterials.Length < 3)
                mapRenderer.roadMaterials = new Material[3];

            var roadShader = Shader.Find("MiniMapGame/Road");
            Color[] defaultBase = { new(0.22f, 0.31f, 0.41f), new(0.26f, 0.34f, 0.42f), new(0.30f, 0.37f, 0.44f) };
            Color[] defaultCasing = { new(0.17f, 0.24f, 0.35f), new(0.20f, 0.27f, 0.36f), new(0.24f, 0.30f, 0.38f) };

            for (int i = 0; i < 3; i++)
            {
                if (mapRenderer.roadMaterials[i] == null)
                {
                    var mat = roadShader != null
                        ? new Material(roadShader)
                        : new Material(Shader.Find("Universal Render Pipeline/Lit"));

                    mat.SetColor("_BaseColor", defaultBase[i]);
                    mat.SetColor("_CasingColor", defaultCasing[i]);
                    mat.SetColor("_MarkingColor", new Color(0.85f, 0.85f, 0.75f));
                    mat.SetColor("_CurbColor", new Color(0.3f, 0.3f, 0.3f));

                    EnsureFolder("Assets/Resources/Materials");
                    AssetDatabase.CreateAsset(mat, $"Assets/Resources/Materials/Road_T{i}_Mat.mat");
                    mapRenderer.roadMaterials[i] = mat;
                }
            }

            // 3. BuildingSpawner
            var buildingSpawnerGo = FindOrCreate("BuildingSpawner", mapManagerGo.transform);
            var buildingSpawner = EnsureComponent<BuildingSpawner>(buildingSpawnerGo);
            mapManager.buildingSpawner = buildingSpawner;
            buildingSpawner.mapManager = mapManager;

            var normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/NormalBuilding.prefab");
            var landmarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/LandmarkBuilding.prefab");
            buildingSpawner.normalBuildingPrefab = normalPrefab;
            buildingSpawner.landmarkBuildingPrefab = landmarkPrefab;

            // BuildingFade material (roof dissolve near player)
            if (buildingSpawner.buildingFadeMaterial == null)
            {
                var fadeShader = Shader.Find("MiniMapGame/BuildingFade");
                if (fadeShader != null)
                {
                    EnsureFolder("Assets/Resources/Materials");
                    string fadePath = "Assets/Resources/Materials/BuildingFade_Mat.mat";
                    var fadeMat = AssetDatabase.LoadAssetAtPath<Material>(fadePath);
                    if (fadeMat == null)
                    {
                        fadeMat = new Material(fadeShader);
                        fadeMat.SetColor("_BaseColor", new Color(0.22f, 0.28f, 0.38f, 1f));
                        fadeMat.SetFloat("_FadeStartDist", 20f);
                        fadeMat.SetFloat("_FadeEndDist", 8f);
                        AssetDatabase.CreateAsset(fadeMat, fadePath);
                    }
                    buildingSpawner.buildingFadeMaterial = fadeMat;
                }
            }

            // 3b. WaterRenderer
            var waterRendererGo = FindOrCreate("WaterRenderer", mapManagerGo.transform);
            var waterRenderer = EnsureComponent<WaterRenderer>(waterRendererGo);
            mapManager.waterRenderer = waterRenderer;
            waterRenderer.mapManager = mapManager;

            var waterShader = Shader.Find("MiniMapGame/Water");
            if (waterShader != null)
            {
                if (waterRenderer.waterMaterial == null)
                {
                    waterRenderer.waterMaterial = new Material(waterShader);
                    waterRenderer.waterMaterial.SetColor("_BaseColor", new Color(0.08f, 0.18f, 0.35f, 0.75f));
                }
            }

            // 3c. DecorationSpawner
            var decorationSpawnerGo = FindOrCreate("DecorationSpawner", mapManagerGo.transform);
            var decorationSpawner = EnsureComponent<DecorationSpawner>(decorationSpawnerGo);
            mapManager.decorationSpawner = decorationSpawner;
            decorationSpawner.mapManager = mapManager;

            // Assign preset
            var preset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Coastal.asset");
            if (preset != null)
                mapManager.activePreset = preset;

            // Ground material — use GridGround shader for procedural grid
            var gridShader = Shader.Find("MiniMapGame/GridGround");
            if (gridShader != null)
            {
                mapManager.groundMaterial = new Material(gridShader);
                mapManager.groundMaterial.SetColor("_BaseColor", new Color(0.28f, 0.33f, 0.24f));
                mapManager.groundMaterial.SetColor("_GridColor", new Color(0.22f, 0.26f, 0.20f));
                mapManager.groundMaterial.SetFloat("_GridSize", 20f);
                mapManager.groundMaterial.SetFloat("_GridOpacity", 0.12f);
            }
            else
            {
                mapManager.groundMaterial = CreateLitMaterial("Ground", new Color(0.28f, 0.33f, 0.24f));
            }

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
            camCtrl.initialDistance = 30f;
            camCtrl.distanceMinMax = new Vector2(8f, 300f);

            // Camera anti-aliasing (SMAA Low)
            var urpCamData = cam.GetUniversalAdditionalCameraData();
            if (urpCamData != null)
            {
                urpCamData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                urpCamData.antialiasingQuality = AntialiasingQuality.Low;
            }

            // 5. Player
            var playerGo = FindOrCreate("Player");
            playerGo.tag = "Player";
            // Remove legacy NavMeshAgent if present
            var existingAgent = playerGo.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (existingAgent != null)
                Object.DestroyImmediate(existingAgent);
            var playerMovement = EnsureComponent<PlayerMovement>(playerGo);
            camCtrl.playerTarget = playerGo.transform;

            // CharacterController handles collision + movement; also acts as a collider
            var cc = EnsureComponent<CharacterController>(playerGo);
            cc.height = 2f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 1f, 0);
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.8f;
            // Remove redundant CapsuleCollider/Rigidbody if present (CC has its own collider)
            var oldCol = playerGo.GetComponent<CapsuleCollider>();
            if (oldCol != null) Object.DestroyImmediate(oldCol);
            var oldRb = playerGo.GetComponent<Rigidbody>();
            if (oldRb != null) Object.DestroyImmediate(oldRb);

            playerGo.transform.position = new Vector3(430f, 0f, 290f);

            // Player visual: Capsule mesh child (placeholder until art direction finalised)
            var playerVisual = FindOrCreate("PlayerVisual", playerGo.transform);
            var pvMeshFilter = EnsureComponent<MeshFilter>(playerVisual);
            pvMeshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            var pvRenderer = EnsureComponent<MeshRenderer>(playerVisual);
            var pvMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pvMat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 0.9f));
            pvMat.name = "PlayerCapsule_Mat";
            pvRenderer.sharedMaterial = pvMat;
            playerVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
            playerVisual.transform.localScale = Vector3.one;

            // Wire camera to decoration spawner for LOD
            decorationSpawner.cameraController = camCtrl;

            // 6. Ensure Ground layer
            EnsureLayerNote("Ground");

            // 7. UI Canvas
            var canvas = SetupCanvas();
            SetupInteractionUI(playerMovement, canvas);

            // 8. MiniMap
            SetupMiniMap(canvas, mapManager, playerGo.transform);

            // 10. Analysis Visualizer (Tab to toggle)
            var vizGo = FindOrCreate("AnalysisVisualizer");
            var viz = EnsureComponent<AnalysisVisualizer>(vizGo);
            viz.mapManager = mapManager;
            EditorUtility.SetDirty(viz);

            // 11. Map Control UI (F1 to toggle)
            SetupMapControlUI(canvas, mapManager, viz);

            // 12. Theme System
            MapThemeCreator.CreateDefaultThemes();
            SetupThemeManager(mapManager, mapRenderer, buildingSpawner, waterRenderer, viz, cam);
            SetupVerificationChecklistUI(canvas, mapManager);

            // 13. Interior System
            SetupInteriorSystem(mapManager, camCtrl, playerGo.transform, canvas);

            // 14b. Interior Feedback UI (toast + floor indicator)
            SetupInteriorFeedbackUI(canvas);

            // 15. Save Manager
            SetupSaveManager(mapManager);

            // 17. Lighting
            var dirLight = SetupLighting();

            // 18. Post-Processing Volume
            var ppManager = SetupPostProcessing();

            // 19. Ambient Particles
            var ambientParticles = SetupAmbientParticles();

            // Wire visual systems into ThemeManager
            var themeManager = Object.FindAnyObjectByType<ThemeManager>();
            if (themeManager != null)
            {
                themeManager.directionalLight = dirLight;
                themeManager.postProcessingManager = ppManager;
                themeManager.ambientParticles = ambientParticles;
                EditorUtility.SetDirty(themeManager);
            }

            // 19. Game Session (5-min prototype)
            SetupGameSession(canvas, mapManager);

            EditorUtility.SetDirty(mapManager);
            EditorUtility.SetDirty(mapRenderer);
            EditorUtility.SetDirty(buildingSpawner);
            EditorUtility.SetDirty(waterRenderer);
            EditorUtility.SetDirty(decorationSpawner);
            Debug.Log("[SceneBootstrapper] Test scene bootstrapped. Press Play to generate map.");
            Debug.Log("[SceneBootstrapper] NOTE: Ensure 'Ground' layer and 'Player' tag exist in Tags & Layers.");
        }

        // ── Interior Feedback UI ──

        private static void SetupInteriorFeedbackUI(Canvas canvas)
        {
            var feedbackGo = FindOrCreate("InteriorFeedbackUI", canvas.transform);
            var feedback = EnsureComponent<InteriorFeedbackUI>(feedbackGo);

            // Wire references
            var eventBus = AssetDatabase.LoadAssetAtPath<MapEventBus>("Assets/Resources/MapEventBus.asset");
            feedback.eventBus = eventBus;
            feedback.interiorController = Object.FindAnyObjectByType<InteriorController>();

            // Toast panel (top-center)
            var toastPanel = FindOrCreate("ToastPanel", feedbackGo.transform);
            var toastRect = EnsureComponent<RectTransform>(toastPanel);
            toastRect.anchorMin = new Vector2(0.3f, 0.85f);
            toastRect.anchorMax = new Vector2(0.7f, 0.92f);
            toastRect.offsetMin = Vector2.zero;
            toastRect.offsetMax = Vector2.zero;

            var toastBg = EnsureComponent<Image>(toastPanel);
            toastBg.color = new Color(0.03f, 0.05f, 0.08f, 0.80f);

            var toastCg = EnsureComponent<CanvasGroup>(toastPanel);
            toastCg.alpha = 0f;
            toastPanel.SetActive(false);
            feedback.toastCanvasGroup = toastCg;

            var toastTextGo = FindOrCreate("ToastText", toastPanel.transform);
            var toastTmp = EnsureComponent<TextMeshProUGUI>(toastTextGo);
            toastTmp.text = "";
            toastTmp.fontSize = 18;
            toastTmp.fontStyle = FontStyles.Bold;
            toastTmp.color = new Color(0.92f, 0.97f, 1f, 0.96f);
            toastTmp.alignment = TextAlignmentOptions.Center;
            var toastTmpRect = toastTmp.GetComponent<RectTransform>();
            toastTmpRect.anchorMin = Vector2.zero;
            toastTmpRect.anchorMax = Vector2.one;
            toastTmpRect.offsetMin = new Vector2(10f, 2f);
            toastTmpRect.offsetMax = new Vector2(-10f, -2f);
            feedback.toastText = toastTmp;

            // Floor indicator (top-left)
            var floorRoot = FindOrCreate("FloorIndicator", feedbackGo.transform);
            var floorRect = EnsureComponent<RectTransform>(floorRoot);
            floorRect.anchorMin = new Vector2(0.02f, 0.90f);
            floorRect.anchorMax = new Vector2(0.18f, 0.96f);
            floorRect.offsetMin = Vector2.zero;
            floorRect.offsetMax = Vector2.zero;

            var floorBg = EnsureComponent<Image>(floorRoot);
            floorBg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            floorRoot.SetActive(false);
            feedback.floorIndicatorRoot = floorRoot;

            var floorTextGo = FindOrCreate("FloorText", floorRoot.transform);
            var floorTmp = EnsureComponent<TextMeshProUGUI>(floorTextGo);
            floorTmp.text = "F1";
            floorTmp.fontSize = 16;
            floorTmp.fontStyle = FontStyles.Bold;
            floorTmp.color = new Color(0.7f, 0.85f, 1f, 0.9f);
            floorTmp.alignment = TextAlignmentOptions.Center;
            var floorTmpRect = floorTmp.GetComponent<RectTransform>();
            floorTmpRect.anchorMin = Vector2.zero;
            floorTmpRect.anchorMax = Vector2.one;
            floorTmpRect.offsetMin = new Vector2(4f, 2f);
            floorTmpRect.offsetMax = new Vector2(-4f, -2f);
            feedback.floorIndicatorText = floorTmp;

            EditorUtility.SetDirty(feedback);
            Debug.Log("[SceneBootstrapper] Interior feedback UI created.");
        }

        // ── Game Session (5-min prototype) ──

        private static void SetupGameSession(Canvas canvas, MapManager mapManager)
        {
            var eventBus = AssetDatabase.LoadAssetAtPath<MapEventBus>("Assets/Resources/MapEventBus.asset");
            var explorationProgress = Object.FindAnyObjectByType<ExplorationProgressManager>();

            var sessionGo = FindOrCreate("GameSessionManager");
            var sessionMgr = EnsureComponent<GameSessionManager>(sessionGo);
            sessionMgr.mapManager = mapManager;
            sessionMgr.eventBus = eventBus;
            sessionMgr.explorationProgress = explorationProgress;
            sessionMgr.mapControlUI = Object.FindAnyObjectByType<MapControlUI>();

            var uiGo = FindOrCreate("GameSessionUI", canvas.transform);
            var sessionUI = EnsureComponent<GameSessionUI>(uiGo);
            sessionMgr.sessionUI = sessionUI;

            EditorUtility.SetDirty(sessionMgr);
            Debug.Log("[SceneBootstrapper] GameSession system set up.");
        }

        // ── Road Profile Auto-Binding ──

        private static void AutoBindRoadProfiles()
        {
            // Explicit mapping keeps preset-specific art direction even in recovery flows.
            var presetProfileMap = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Assets/Resources/Presets/Preset_Coastal.asset", "RoadProfile_Modern" },
                { "Assets/Resources/Presets/Preset_Rural.asset", "RoadProfile_Rural" },
                { "Assets/Resources/Presets/Preset_Grid.asset", "RoadProfile_Modern" },
                { "Assets/Resources/Presets/Preset_Mountain.asset", "RoadProfile_Rural" },
                { "Assets/Resources/Presets/Preset_Island.asset", "RoadProfile_Historic" },
                { "Assets/Resources/Presets/Preset_Downtown.asset", "RoadProfile_Modern" },
                { "Assets/Resources/Presets/Preset_Valley.asset", "RoadProfile_Rural" }
            };

            foreach (var entry in presetProfileMap)
            {
                var preset = AssetDatabase.LoadAssetAtPath<MapPreset>(entry.Key);
                if (preset == null || preset.roadProfile != null) continue;

                var profile = AssetDatabase.LoadAssetAtPath<RoadProfile>(
                    $"Assets/Resources/RoadProfiles/{entry.Value}.asset");

                if (profile != null)
                {
                    preset.roadProfile = profile;
                    EditorUtility.SetDirty(preset);
                    Debug.Log($"[SceneBootstrapper] Bound {entry.Value} to {preset.displayName}");
                }
            }
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
            // EventSystem is required for all UI interaction (buttons, inputs, sliders).
            // Must run before the early return so it's always ensured regardless of Canvas state.
            var existingES = Object.FindAnyObjectByType<EventSystem>();
            {
                var esGo = existingES != null ? existingES.gameObject : FindOrCreate("EventSystem");
                EnsureComponent<EventSystem>(esGo);
                // Remove legacy StandaloneInputModule if present (incompatible with new Input System)
                var legacyModule = esGo.GetComponent<StandaloneInputModule>();
                if (legacyModule != null) Object.DestroyImmediate(legacyModule);
                // InputSystemUIInputModule is required when using the new Input System package.
                EnsureComponent<InputSystemUIInputModule>(esGo);
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) return canvas;

            var canvasGo = FindOrCreate("UICanvas");
            canvas = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(canvasGo);

            return canvas;
        }

        private static void SetupInteractionUI(PlayerMovement playerMovement, Canvas canvas)
        {
            var msgGo = FindOrCreate("InteractionMessage", canvas.transform);
            RemoveComponentIfPresent<TextMeshProUGUI>(msgGo);
            RemoveComponentIfPresent<Image>(msgGo);
            RemoveComponentIfPresent<Outline>(msgGo);
            SetRectFromBottomCenter(msgGo, 0f, 28f, 560f, 52f);

            var bgGo = FindOrCreate("Background", msgGo.transform);
            var bg = EnsureComponent<Image>(bgGo);
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.84f);
            var outline = EnsureComponent<Outline>(bgGo);
            outline.effectColor = new Color(0.25f, 0.42f, 0.58f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);
            SetRect(bgGo, Vector2.zero, Vector2.one);

            var textGo = FindOrCreate("Text", msgGo.transform);
            var tmp = EnsureComponent<TextMeshProUGUI>(textGo);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.92f, 0.97f, 1f, 0.96f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            SetRect(textGo, Vector2.zero, Vector2.one);

            playerMovement.interactionMessageText = tmp;
            msgGo.SetActive(false);
        }


        // ── Map Control UI ──

        private static void SetupMapControlUI(Canvas canvas, MapManager mapManager, AnalysisVisualizer viz)
        {
            // MapControlUI lives on a separate GO so F1 toggle can hide the panel
            // without disabling the MonoBehaviour that listens for F1.
            var controllerGo = FindOrCreate("MapControlUIController", canvas.transform);
            var controlUI = EnsureComponent<MiniMapGame.UI.MapControlUI>(controllerGo);
            // Controller GO needs a RectTransform but is invisible (no Image/Graphic)
            var controllerRect = EnsureComponent<RectTransform>(controllerGo);
            controllerRect.anchorMin = Vector2.zero;
            controllerRect.anchorMax = Vector2.zero;
            controllerRect.sizeDelta = Vector2.zero;

            var panelGo = FindOrCreate("MapControlPanel", canvas.transform);
            controlUI.controlPanel = panelGo;
            controlUI.mapManager = mapManager;
            controlUI.analysisVisualizer = viz;

            var panelBg = EnsureComponent<Image>(panelGo);
            panelBg.color = new Color(0.04f, 0.07f, 0.11f, 0.94f);
            SetRectFromTopLeft(panelGo, 24f, 88f, 360f, 760f);

            float cursorY = 24f;

            var titleTmp = CreateTMPChild(panelGo.transform, "CtrlTitle", "Bootstrap Controls",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            titleTmp.fontSize = 27f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.93f, 0.97f, 1f, 0.98f);
            SetRectFromTopStretch(titleTmp.gameObject, 22f, cursorY, 338f, 34f);
            cursorY += 34f;

            var subtitleTmp = CreateTMPChild(panelGo.transform, "CtrlSubtitle", "F1: toggle panel  |  Fast visual verification workspace",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            subtitleTmp.fontSize = 12f;
            subtitleTmp.color = new Color(0.56f, 0.71f, 0.82f, 0.9f);
            SetRectFromTopStretch(subtitleTmp.gameObject, 22f, cursorY, 338f, 22f);
            cursorY += 38f;

            var selectedBg = CreateCard(panelGo.transform, "SelectedPresetCard", 18f, cursorY, 324f, 78f,
                new Color(0.08f, 0.12f, 0.17f, 0.96f));
            var selectedLabel = CreateTMPChild(selectedBg.transform, "SelectedPresetLabel", "ACTIVE PRESET",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            selectedLabel.fontSize = 11f;
            selectedLabel.color = new Color(0.55f, 0.68f, 0.77f, 0.9f);
            SetRectFromTopStretch(selectedLabel.gameObject, 16f, 12f, 292f, 18f);

            controlUI.presetNameText = CreateTMPChild(selectedBg.transform, "PresetName", "---",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            controlUI.presetNameText.fontSize = 24f;
            controlUI.presetNameText.fontStyle = FontStyles.Bold;
            controlUI.presetNameText.color = new Color(0.9f, 0.85f, 0.6f);
            SetRectFromTopStretch(controlUI.presetNameText.gameObject, 16f, 30f, 292f, 34f);
            cursorY += 96f;

            CreateSectionLabel(panelGo.transform, "PresetsLabel", "Map Presets", cursorY);
            cursorY += 24f;

            controlUI.coastalPreset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Coastal.asset");
            controlUI.ruralPreset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Rural.asset");
            controlUI.gridPreset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Grid.asset");
            controlUI.mountainPreset = AssetDatabase.LoadAssetAtPath<MapPreset>("Assets/Resources/Presets/Preset_Mountain.asset");

            const float buttonHeight = 44f;
            const float buttonWidth = 324f;
            controlUI.coastalButton = CreateStyledButton(panelGo.transform, "BtnCoastal", "Coastal", 18f, cursorY, buttonWidth, buttonHeight,
                new Color(0.13f, 0.36f, 0.54f)).GetComponent<Button>();
            cursorY += 52f;
            controlUI.ruralButton = CreateStyledButton(panelGo.transform, "BtnRural", "Rural", 18f, cursorY, buttonWidth, buttonHeight,
                new Color(0.29f, 0.41f, 0.2f)).GetComponent<Button>();
            cursorY += 52f;
            controlUI.gridButton = CreateStyledButton(panelGo.transform, "BtnGrid", "NYC Grid", 18f, cursorY, buttonWidth, buttonHeight,
                new Color(0.43f, 0.31f, 0.18f)).GetComponent<Button>();
            cursorY += 52f;
            controlUI.mountainButton = CreateStyledButton(panelGo.transform, "BtnMountain", "Mountain", 18f, cursorY, buttonWidth, buttonHeight,
                new Color(0.28f, 0.31f, 0.38f)).GetComponent<Button>();
            cursorY += 64f;

            CreateSectionLabel(panelGo.transform, "GenerationLabel", "Generation", cursorY);
            cursorY += 26f;

            var seedLabel = CreateTMPChild(panelGo.transform, "SeedLabel", "Seed",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            seedLabel.fontSize = 13f;
            seedLabel.color = new Color(0.74f, 0.82f, 0.9f, 0.95f);
            SetRectFromTopStretch(seedLabel.gameObject, 18f, cursorY, 120f, 18f);
            cursorY += 22f;

            var seedGo = FindOrCreate("SeedInput", panelGo.transform);
            var seedInput = EnsureComponent<TMP_InputField>(seedGo);
            seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            SetRectFromTopStretch(seedGo, 18f, cursorY, 324f, 42f);
            EnsureInputFieldChildren(seedGo);
            controlUI.seedInput = seedInput;
            cursorY += 58f;

            controlUI.densityLabel = CreateTMPChild(panelGo.transform, "DensityLabel", "Building Density: 80%",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            controlUI.densityLabel.fontSize = 13f;
            controlUI.densityLabel.color = new Color(0.74f, 0.82f, 0.9f, 0.95f);
            SetRectFromTopStretch(controlUI.densityLabel.gameObject, 18f, cursorY, 324f, 18f);
            cursorY += 24f;

            var sliderGo = FindOrCreate("DensitySlider", panelGo.transform);
            var slider = EnsureComponent<Slider>(sliderGo);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            SetRectFromTopStretch(sliderGo, 18f, cursorY, 324f, 28f);
            EnsureSliderChildren(sliderGo);
            controlUI.buildingDensitySlider = slider;
            cursorY += 44f;

            controlUI.regenerateButton = CreateStyledButton(panelGo.transform, "BtnRegenerate", "Regenerate", 18f, cursorY, 156f, 44f,
                new Color(0.23f, 0.49f, 0.35f)).GetComponent<Button>();
            controlUI.randomButton = CreateStyledButton(panelGo.transform, "BtnRandom", "Random", 186f, cursorY, 156f, 44f,
                new Color(0.39f, 0.3f, 0.5f)).GetComponent<Button>();
            cursorY += 60f;

            CreateSectionLabel(panelGo.transform, "ThemeLabel", "Theme", cursorY);
            cursorY += 26f;

            controlUI.darkThemeButton = CreateStyledButton(panelGo.transform, "BtnDark", "Dark", 18f, cursorY, 156f, 42f,
                new Color(0.08f, 0.1f, 0.16f)).GetComponent<Button>();
            controlUI.parchmentThemeButton = CreateStyledButton(panelGo.transform, "BtnParchment", "Parchment", 186f, cursorY, 156f, 42f,
                new Color(0.72f, 0.68f, 0.55f)).GetComponent<Button>();
            // Set parchment button text to dark color for readability
            var parchBtnText = controlUI.parchmentThemeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (parchBtnText != null) parchBtnText.color = new Color(0.2f, 0.18f, 0.12f);
            cursorY += 58f;

            CreateSectionLabel(panelGo.transform, "SaveLoadLabel", "Persistence", cursorY);
            cursorY += 26f;

            controlUI.saveButton = CreateStyledButton(panelGo.transform, "BtnSave", "Save", 18f, cursorY, 156f, 42f,
                new Color(0.2f, 0.45f, 0.3f)).GetComponent<Button>();
            controlUI.loadButton = CreateStyledButton(panelGo.transform, "BtnLoad", "Load", 186f, cursorY, 156f, 42f,
                new Color(0.3f, 0.3f, 0.5f)).GetComponent<Button>();
            cursorY += 60f;

            var statsCard = CreateCard(panelGo.transform, "StatsCard", 18f, cursorY, 324f, 184f,
                new Color(0.07f, 0.1f, 0.14f, 0.95f));
            var statsHeader = CreateTMPChild(statsCard.transform, "StatsHeader", "Analysis Snapshot",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            statsHeader.fontSize = 13f;
            statsHeader.fontStyle = FontStyles.Bold;
            statsHeader.color = new Color(0.8f, 0.88f, 0.95f, 0.96f);
            SetRectFromTopStretch(statsHeader.gameObject, 16f, 14f, 292f, 18f);

            controlUI.statsText = CreateTMPChild(statsCard.transform, "StatsText", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            controlUI.statsText.fontSize = 12f;
            controlUI.statsText.color = new Color(0.5f, 0.6f, 0.7f, 0.8f);
            SetRectFromTopStretch(controlUI.statsText.gameObject, 16f, 40f, 292f, 130f);
            controlUI.statsText.overflowMode = TextOverflowModes.Ellipsis;

            EditorUtility.SetDirty(controlUI);
        }

        private static void EnsureInputFieldChildren(GameObject inputGo)
        {
            // TMP_InputField requires TextArea > Text children
            var textAreaGo = FindOrCreate("Text Area", inputGo.transform);
            SetRect(textAreaGo, Vector2.zero, Vector2.one);
            var textGo = FindOrCreate("Text", textAreaGo.transform);
            var tmp = EnsureComponent<TextMeshProUGUI>(textGo);
            tmp.fontSize = 14f;
            tmp.color = new Color(0.8f, 0.9f, 1f);
            SetRect(textGo, Vector2.zero, Vector2.one);

            var placeholderGo = FindOrCreate("Placeholder", textAreaGo.transform);
            var phTmp = EnsureComponent<TextMeshProUGUI>(placeholderGo);
            phTmp.text = "12345";
            phTmp.fontSize = 14f;
            phTmp.color = new Color(0.4f, 0.5f, 0.6f, 0.5f);
            SetRect(placeholderGo, Vector2.zero, Vector2.one);

            var inputField = inputGo.GetComponent<TMP_InputField>();
            if (inputField != null)
            {
                inputField.textViewport = textAreaGo.GetComponent<RectTransform>();
                inputField.textComponent = tmp;
                inputField.placeholder = phTmp;
            }

            // Background
            var bg = EnsureComponent<Image>(inputGo);
            bg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
        }

        private static void EnsureSliderChildren(GameObject sliderGo)
        {
            // Background
            var bgGo = FindOrCreate("Background", sliderGo.transform);
            var bgImg = EnsureComponent<Image>(bgGo);
            bgImg.color = new Color(0.1f, 0.12f, 0.18f);
            SetRect(bgGo, Vector2.zero, Vector2.one);

            // Fill Area
            var fillAreaGo = FindOrCreate("Fill Area", sliderGo.transform);
            SetRect(fillAreaGo, new Vector2(0, 0.25f), new Vector2(1, 0.75f));

            var fillGo = FindOrCreate("Fill", fillAreaGo.transform);
            var fillImg = EnsureComponent<Image>(fillGo);
            fillImg.color = new Color(0.3f, 0.5f, 0.7f);
            SetRect(fillGo, Vector2.zero, Vector2.one);

            // Handle Slide Area
            var handleAreaGo = FindOrCreate("Handle Slide Area", sliderGo.transform);
            SetRect(handleAreaGo, Vector2.zero, Vector2.one);

            var handleGo = FindOrCreate("Handle", handleAreaGo.transform);
            var handleImg = EnsureComponent<Image>(handleGo);
            handleImg.color = new Color(0.7f, 0.8f, 0.9f);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(12, 0);

            // Wire slider references
            var slider = sliderGo.GetComponent<Slider>();
            if (slider != null)
            {
                slider.fillRect = fillGo.GetComponent<RectTransform>();
                slider.handleRect = handleGo.GetComponent<RectTransform>();
                slider.targetGraphic = handleImg;
            }
        }

        // ── MiniMap ──

        private static void SetupMiniMap(Canvas canvas, MapManager mapManager, Transform playerTransform)
        {
            EnsureFolder("Assets/Resources/RenderTextures");

            // RenderTexture (860:580 aspect ratio → 512x346)
            string rtPath = "Assets/Resources/RenderTextures/MiniMapRT.renderTexture";
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt == null)
            {
                rt = new RenderTexture(512, 346, 16, RenderTextureFormat.ARGB32);
                rt.name = "MiniMapRT";
                rt.filterMode = FilterMode.Bilinear;
                AssetDatabase.CreateAsset(rt, rtPath);
            }

            // MiniMap Camera
            var camGo = FindOrCreate("MiniMapCamera");
            var miniCam = EnsureComponent<Camera>(camGo);
            miniCam.orthographic = true;
            miniCam.orthographicSize = 290f; // worldHeight / 2
            miniCam.transform.position = new Vector3(430f, 500f, 290f);
            miniCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            miniCam.clearFlags = CameraClearFlags.SolidColor;
            miniCam.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            miniCam.depth = -1;
            miniCam.targetTexture = rt;

            // RawImage in Canvas (bottom-right corner)
            var frameGo = CreateCard(canvas.transform, "MiniMapFrame", 0f, 0f, 268f, 198f,
                new Color(0.04f, 0.06f, 0.09f, 0.88f));
            SetRectFromBottomRight(frameGo, 24f, 24f, 268f, 198f);

            var mapTitle = CreateTMPChild(frameGo.transform, "MiniMapTitle", "Mini Map",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            mapTitle.fontSize = 13f;
            mapTitle.fontStyle = FontStyles.Bold;
            mapTitle.color = new Color(0.84f, 0.9f, 0.97f, 0.95f);
            SetRectFromTopStretch(mapTitle.gameObject, 16f, 12f, 236f, 18f);

            var mapImageGo = FindOrCreate("MiniMapImage", frameGo.transform);
            RemoveComponentIfPresent<Image>(mapImageGo);
            var rawImage = EnsureComponent<RawImage>(mapImageGo);
            rawImage.texture = rt;
            rawImage.color = Color.white;
            SetRectFromTopStretch(mapImageGo, 16f, 40f, 236f, 142f);

            // Border outline
            var outline = EnsureComponent<Outline>(mapImageGo);
            outline.effectColor = new Color(0.3f, 0.45f, 0.6f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);

            // Player indicator (small triangle/arrow)
            var indicatorGo = FindOrCreate("MiniMapPlayerIndicator", mapImageGo.transform);
            var indicatorImg = EnsureComponent<Image>(indicatorGo);
            indicatorImg.color = new Color(0.2f, 1f, 0.4f, 1f);

            var indicatorRect = indicatorGo.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(10, 14);
            indicatorRect.anchoredPosition = Vector2.zero;

            // MiniMapController
            var controllerGo = FindOrCreate("MiniMapSystem");
            var controller = EnsureComponent<MiniMapController>(controllerGo);
            controller.miniMapCamera = miniCam;
            controller.miniMapImage = rawImage;
            controller.playerIndicator = indicatorRect;
            controller.playerTransform = playerTransform;
            controller.mapManager = mapManager;

            EditorUtility.SetDirty(controller);
        }

        // ── Theme Manager ──

        private static void SetupThemeManager(MapManager mapManager, MapRenderer mapRenderer,
            BuildingSpawner buildingSpawner, WaterRenderer waterRenderer,
            AnalysisVisualizer viz, Camera mainCam)
        {
            var go = FindOrCreate("ThemeManager");
            var tm = EnsureComponent<ThemeManager>(go);
            tm.mapManager = mapManager;
            tm.mapRenderer = mapRenderer;
            tm.buildingSpawner = buildingSpawner;
            tm.waterRenderer = waterRenderer;
            tm.analysisVisualizer = viz;
            tm.mainCamera = mainCam;

            var darkTheme = AssetDatabase.LoadAssetAtPath<MapTheme>("Assets/Resources/Themes/Theme_Dark.asset");
            if (darkTheme != null)
                tm.activeTheme = darkTheme;

            // Wire themes into MapControlUI
            var controlUI = Object.FindAnyObjectByType<MapControlUI>();
            if (controlUI != null)
            {
                controlUI.themeManager = tm;
                controlUI.darkTheme = darkTheme;
                controlUI.parchmentTheme =
                    AssetDatabase.LoadAssetAtPath<MapTheme>("Assets/Resources/Themes/Theme_Parchment.asset");
                EditorUtility.SetDirty(controlUI);
            }

            EditorUtility.SetDirty(tm);
        }

        private static void SetupVerificationChecklistUI(Canvas canvas, MapManager mapManager)
        {
            var systemGo = FindOrCreate("VerificationChecklistSystem");
            var checklist = EnsureComponent<VerificationChecklistUI>(systemGo);
            var panelGo = FindOrCreate("VerificationChecklistPanel", canvas.transform);
            var panelChecklist = panelGo.GetComponent<VerificationChecklistUI>();
            if (panelChecklist != null)
                Object.DestroyImmediate(panelChecklist);
            checklist.mapManager = mapManager;
            checklist.themeManager = Object.FindAnyObjectByType<ThemeManager>();
            checklist.panelRoot = panelGo;

            var bg = EnsureComponent<Image>(panelGo);
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.84f);
            var outline = EnsureComponent<Outline>(panelGo);
            outline.effectColor = new Color(0.25f, 0.42f, 0.58f, 0.35f);
            outline.effectDistance = new Vector2(2f, -2f);
            SetRectFromTopRight(panelGo, 24f, 96f, 320f, 420f);

            var title = CreateTMPChild(panelGo.transform, "VerificationTitle", "Gate-1 Checklist",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.93f, 0.97f, 1f, 0.98f);
            SetRectFromTopStretch(title.gameObject, 18f, 18f, 284f, 24f);

            var subtitle = CreateTMPChild(panelGo.transform, "VerificationSubtitle", "Road manual verification helper",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            subtitle.fontSize = 12f;
            subtitle.color = new Color(0.6f, 0.74f, 0.84f, 0.9f);
            SetRectFromTopStretch(subtitle.gameObject, 18f, 44f, 284f, 18f);

            var summaryText = CreateTMPChild(panelGo.transform, "VerificationSummary", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            summaryText.fontSize = 13f;
            summaryText.color = new Color(0.91f, 0.95f, 1f, 0.95f);
            summaryText.textWrappingMode = TextWrappingModes.Normal;
            SetRectFromTopStretch(summaryText.gameObject, 18f, 82f, 284f, 72f);
            checklist.summaryText = summaryText;

            var body = CreateTMPChild(panelGo.transform, "VerificationBody", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            body.fontSize = 12f;
            body.color = new Color(0.78f, 0.86f, 0.94f, 0.9f);
            body.textWrappingMode = TextWrappingModes.Normal;
            SetRectFromTopStretch(body.gameObject, 18f, 160f, 284f, 242f);
            checklist.checklistText = body;

            EditorUtility.SetDirty(checklist);
        }

        // ── Interior System ──

        private static void SetupInteriorSystem(MapManager mapManager,
            CameraController cameraController, Transform playerTransform, Canvas canvas)
        {
            var rendererGo = FindOrCreate("InteriorRenderer");
            var renderer = EnsureComponent<InteriorRenderer>(rendererGo);

            // Create wall material
            EnsureFolder("Assets/Resources/Materials");
            string wallMatPath = "Assets/Resources/Materials/InteriorWall_Mat.mat";
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            if (wallMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                wallMat = new Material(shader);
                wallMat.color = new Color(0.3f, 0.35f, 0.4f);
                AssetDatabase.CreateAsset(wallMat, wallMatPath);
            }
            renderer.wallMaterial = wallMat;

            var controllerGo = FindOrCreate("InteriorController");
            var controller = EnsureComponent<InteriorController>(controllerGo);
            controller.mapManager = mapManager;
            controller.interiorRenderer = renderer;
            controller.cameraController = cameraController;
            controller.playerTransform = playerTransform;

            // InteriorInteractionManager
            var interactionGo = FindOrCreate("InteriorInteractionManager");
            var interactionMgr = EnsureComponent<InteriorInteractionManager>(interactionGo);
            interactionMgr.interiorController = controller;
            interactionMgr.interiorRenderer = renderer;
            interactionMgr.playerTransform = playerTransform;
            var eventBus = AssetDatabase.LoadAssetAtPath<MapEventBus>("Assets/Resources/MapEventBus.asset");
            interactionMgr.eventBus = eventBus;
            controller.interactionManager = interactionMgr;

            // Interior interaction prompt UI (bottom-center, similar to exterior InteractionMessage)
            var promptPanelGo = FindOrCreate("InteriorPrompt", canvas.transform);
            RemoveComponentIfPresent<TextMeshProUGUI>(promptPanelGo);
            RemoveComponentIfPresent<Image>(promptPanelGo);
            SetRectFromBottomCenter(promptPanelGo, 0f, 90f, 480f, 48f);

            var promptBgGo = FindOrCreate("Background", promptPanelGo.transform);
            var promptBg = EnsureComponent<Image>(promptBgGo);
            promptBg.color = new Color(0.03f, 0.05f, 0.08f, 0.84f);
            var promptOutline = EnsureComponent<Outline>(promptBgGo);
            promptOutline.effectColor = new Color(0.25f, 0.42f, 0.58f, 0.45f);
            promptOutline.effectDistance = new Vector2(2f, -2f);
            SetRect(promptBgGo, Vector2.zero, Vector2.one);

            var promptTextGo = FindOrCreate("Text", promptPanelGo.transform);
            var promptTmp = EnsureComponent<TextMeshProUGUI>(promptTextGo);
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.fontSize = 20f;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.color = new Color(0.92f, 0.97f, 1f, 0.96f);
            promptTmp.textWrappingMode = TextWrappingModes.Normal;
            SetRect(promptTextGo, Vector2.zero, Vector2.one);

            interactionMgr.promptPanel = promptPanelGo;
            interactionMgr.promptText = promptTmp;
            promptPanelGo.SetActive(false);

            // FloorNavigator
            var floorNavGo = FindOrCreate("FloorNavigator");
            var floorNav = EnsureComponent<FloorNavigator>(floorNavGo);
            floorNav.interiorRenderer = renderer;
            floorNav.playerTransform = playerTransform;
            controller.floorNavigator = floorNav;

            // ExplorationProgressManager
            var explorationGo = FindOrCreate("ExplorationProgressManager");
            var explorationMgr = EnsureComponent<ExplorationProgressManager>(explorationGo);
            explorationMgr.eventBus = eventBus;
            controller.explorationProgress = explorationMgr;

            // BuildingMarkerManager (SP-020 Layer 2)
            var markerMgrGo = FindOrCreate("BuildingMarkerManager");
            var markerMgr = EnsureComponent<BuildingMarkerManager>(markerMgrGo);
            markerMgr.buildingSpawner = buildingSpawner;
            markerMgr.progressManager = explorationMgr;
            markerMgr.mapManager = mapManager;

            // ExplorationMenuUI (Tab key toggle)
            var explorationMenuGo = FindOrCreate("ExplorationMenuUI");
            var explorationMenu = EnsureComponent<ExplorationMenuUI>(explorationMenuGo);
            explorationMenu.explorationProgress = explorationMgr;

            var menuPanelGo = FindOrCreate("ExplorationMenuPanel", canvas.transform);
            RemoveComponentIfPresent<TextMeshProUGUI>(menuPanelGo);
            RemoveComponentIfPresent<Image>(menuPanelGo);
            SetRectFromCenter(menuPanelGo, 0f, 0f, 600f, 420f);

            var menuBgGo = FindOrCreate("Background", menuPanelGo.transform);
            var menuBg = EnsureComponent<Image>(menuBgGo);
            menuBg.color = new Color(0.03f, 0.05f, 0.08f, 0.92f);
            var menuOutline = EnsureComponent<Outline>(menuBgGo);
            menuOutline.effectColor = new Color(0.25f, 0.42f, 0.58f, 0.35f);
            menuOutline.effectDistance = new Vector2(2f, -2f);
            SetRect(menuBgGo, Vector2.zero, Vector2.one);

            var menuTextGo = FindOrCreate("Text", menuPanelGo.transform);
            var menuTmp = EnsureComponent<TextMeshProUGUI>(menuTextGo);
            menuTmp.alignment = TextAlignmentOptions.TopLeft;
            menuTmp.fontSize = 16f;
            menuTmp.color = new Color(0.92f, 0.97f, 1f, 0.96f);
            menuTmp.richText = true;
            menuTmp.textWrappingMode = TextWrappingModes.Normal;
            SetRect(menuTextGo, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));

            explorationMenu.menuPanel = menuPanelGo;
            explorationMenu.menuText = menuTmp;
            menuPanelGo.SetActive(false);

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(interactionMgr);
            EditorUtility.SetDirty(floorNav);
            EditorUtility.SetDirty(explorationMgr);
            EditorUtility.SetDirty(explorationMenu);
        }

        // ── Save Manager ──

        private static void SetupSaveManager(MapManager mapManager)
        {
            var go = FindOrCreate("SaveManager");
            var sm = EnsureComponent<SaveManager>(go);
            sm.mapManager = mapManager;

            var explorationMgr = Object.FindAnyObjectByType<ExplorationProgressManager>();
            if (explorationMgr != null)
                sm.explorationProgress = explorationMgr;

            // Wire into MapControlUI
            var controlUI = Object.FindAnyObjectByType<MapControlUI>();
            if (controlUI != null)
            {
                controlUI.saveManager = sm;
                EditorUtility.SetDirty(controlUI);
            }

            EditorUtility.SetDirty(sm);
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

        private static GameObject CreateStyledButton(Transform parent, string name, string label,
            float left, float top, float width, float height, Color color)
        {
            var go = FindOrCreate(name, parent);
            var img = EnsureComponent<Image>(go);
            img.color = color;
            EnsureComponent<Button>(go);
            SetRectFromTopLeft(go, left, top, width, height);

            var outline = EnsureComponent<Outline>(go);
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textGo = FindOrCreate($"{name}_Text", go.transform);
            var tmp = EnsureComponent<TextMeshProUGUI>(textGo);
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            SetRect(textGo, Vector2.zero, Vector2.one);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            return go;
        }

        private static GameObject CreateCard(Transform parent, string name, float left, float top, float width, float height, Color color)
        {
            var go = FindOrCreate(name, parent);
            var image = EnsureComponent<Image>(go);
            image.color = color;
            SetRectFromTopLeft(go, left, top, width, height);
            return go;
        }

        private static TextMeshProUGUI CreateSectionLabel(Transform parent, string name, string text, float top)
        {
            var label = CreateTMPChild(parent, name, text,
                new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft);
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.57f, 0.72f, 0.82f, 0.94f);
            SetRectFromTopStretch(label.gameObject, 18f, top, 324f, 18f);
            return label;
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

        private static void SetRectFromTopLeft(GameObject go, float left, float top, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRectFromTopRight(GameObject go, float right, float top, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRectFromBottomRight(GameObject go, float right, float bottom, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-right, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRectFromBottomCenter(GameObject go, float offsetX, float bottom, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(offsetX, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRectFromCenter(GameObject go, float offsetX, float offsetY, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(offsetX, offsetY);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRectFromTopStretch(GameObject go, float left, float top, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
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
            if (parent != null)
            {
                var child = parent.Find(name);
                if (child != null) return child.gameObject;
            }
            else
            {
                var existing = GameObject.Find(name);
                if (existing != null) return existing;
            }

            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void RemoveComponentIfPresent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp != null)
                Object.DestroyImmediate(comp);
        }

        // ── Lighting ──

        private static Light SetupLighting()
        {
            var lightGo = FindOrCreate("Directional Light");
            var light = EnsureComponent<Light>(lightGo);
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.color = new Color(0.85f, 0.9f, 1.0f);
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.4f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.06f, 0.1f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.024f, 0.035f, 0.05f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 400f;

            return light;
        }

        // ── Post-Processing ──

        private static PostProcessingManager SetupPostProcessing()
        {
            var go = FindOrCreate("PostProcessingVolume");
            var volume = EnsureComponent<Volume>(go);
            volume.isGlobal = true;
            volume.priority = 0;

            if (volume.profile == null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();

                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.3f);
                bloom.threshold.Override(0.9f);
                bloom.scatter.Override(0.65f);

                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.25f);
                vignette.smoothness.Override(0.4f);
                vignette.rounded.Override(true);

                var colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.postExposure.Override(0.1f);
                colorAdj.contrast.Override(8f);
                colorAdj.saturation.Override(-10f);

                var tonemap = profile.Add<Tonemapping>(true);
                tonemap.mode.Override(TonemappingMode.ACES);

                volume.profile = profile;
            }

            var ppManager = EnsureComponent<PostProcessingManager>(go);
            ppManager.volume = volume;

            EditorUtility.SetDirty(go);
            return ppManager;
        }

        // ── Ambient Particles ──

        private static AmbientParticleController SetupAmbientParticles()
        {
            var go = FindOrCreate("AmbientDust");
            var ps = EnsureComponent<ParticleSystem>(go);
            var controller = EnsureComponent<AmbientParticleController>(go);
            controller.dustSystem = ps;

            var main = ps.main;
            main.maxParticles = 50;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.5f, 0.7f, 1f, 0.15f);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(860f, 20f, 580f);
            shape.position = new Vector3(430f, 10f, 290f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.3f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Use Particles/Standard Unlit material
            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
                if (particleShader != null)
                {
                    var mat = new Material(particleShader);
                    mat.SetFloat("_Surface", 1); // Transparent
                    particleRenderer.material = mat;
                }
            }

            EditorUtility.SetDirty(go);
            return controller;
        }

        // ── Helpers ──

        private static Material CreateLitMaterial(string name, Color color)
        {
            EnsureFolder("Assets/Resources/Materials");
            string path = $"Assets/Resources/Materials/{name}_Lit_Mat.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureLayerNote(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                // Try to auto-create the layer in an empty slot (8-31 are user layers)
                var tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layersProp = tagManager.FindProperty("layers");
                bool created = false;
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    var slot = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(slot.stringValue))
                    {
                        slot.stringValue = layerName;
                        tagManager.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log($"[SceneBootstrapper] Created layer '{layerName}' at index {i}.");
                        created = true;
                        break;
                    }
                }
                if (!created)
                {
                    Debug.LogWarning($"[SceneBootstrapper] Layer '{layerName}' not found and no empty slots available. " +
                        $"Please create it in Edit > Project Settings > Tags and Layers.");
                }
            }
        }
    }
}
