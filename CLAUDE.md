# MiniMapGame - Project Instructions

## Overview
手続き型マップ探索ゲーム — プロシージャル都市・地形生成と探索・発見がコア体験。
React/Canvasプロトタイプから C#/Unity へ移植済み。
現フェーズ: α（地形/道路/水面/Interior/Discovery実装完了 → 体験ループ構築へ軸転換）

## PROJECT CONTEXT

→ 詳細は `docs/project-context.md` を参照

現フェーズ: α (体験ループ構築中)
直近の状態 (2026-04-05 session 10): SP-040(マップビジュアルディレクション)新規作成。SP-001 Phase 2完了(プリセット別フィルタ+30件クエスト)。全実装完了・Unity PlayMode検証待ち。

## DECISION LOG

→ 最新の決定は `docs/project-context.md` の DECISION LOG を参照
→ 過去の決定は `docs/archive/decision-log-archive.md` を参照
## Engine & Pipeline
- Unity 6.3 (6000.3.6f1)
- URP 17.3.0
- Camera: **Perspective** (3D orbit/pan/zoom, Interior時はOrthographic)
- Input: Unity InputSystem 1.18.0
- **Polybrush は削除済み** (Unity 6.2非互換)

## Architecture Principles
- Pure data/logic classes に MonoBehaviour 依存禁止
- 全データ構造に `[System.Serializable]`
- 全生成は同一 int seed で決定論的再現
- 外部パッケージ追加禁止 (Unity built-ins only)
- MapPreset / MapTheme は ScriptableObject
- Strategy パターンで IMapGenerator を実装
- MapEventBus による Pub/Sub イベントシステム

## Code Style
- C# naming: PascalCase (public), camelCase (private), _camelCase (fields)
- 1ファイル1クラス原則（小型のenum/structは例外）
- namespace: `MiniMapGame`（サブ: `.Data`, `.Core`, `.MapGen`, `.Runtime`, `.Interior`, `.GameLoop`, `.Player`, `.UI`）
- コメント: 日本語OK、ただしAPIドキュメントは英語

## Unity 6.x API 注意事項
- `TextAlignmentOptions.TopCenter` → `.Top`
- `TextAlignmentOptions.MiddleLeft` → `.MidlineLeft`
- `TextAlignmentOptions.MiddleRight` → `.MidlineRight`
- `AntialiasingMode.SubPixelMorphologicalAntiAliasing` → `.SubpixelMorphologicalAntiAliasing` (小文字p)

## Directory Structure
```
Assets/
  Scripts/
    Data/           MapData, MapNode, MapEdge, MapBuilding, MapTerrain, MapAnalysis,
                    MapPreset(SO), MapTheme(SO), RoadProfile(SO), WaterProfile(SO),
                    MapDecoration, DecorationType, WaterBodyType, WaterBodyData,
                    HillData, HillCluster, ClusterType, SlopeProfile,
                    NodeType, GeneratorType, BuildingCategory, ShopSubtype,
                    InteriorBuildingContext, BuildingMarkerState
    Core/           SeededRng, SpatialHash, MapGenUtils, MapAnalyzer,
                    TerrainGenerator, BuildingPlacer, BuildingClassifier,
                    DecorationPlacer, BridgeTunnelDetector, ElevationMap,
                    WaterGenerator, WaterTerrainInteraction, ISpatialBounds,
                    RoadCurveSampler
    MapGen/         IMapGenerator, OrganicGenerator, GridGenerator,
                    MountainGenerator, RuralGenerator
    Runtime/        MapManager, MapRenderer, BuildingSpawner, BuildingInteraction,
                    AnalysisVisualizer, ThemeManager, WaterRenderer,
                    DecorationSpawner, PostProcessingManager,
                    AmbientParticleController,
                    GroundSemanticMaskBaker, GroundSemanticMaskSet,
                    GroundSurfacePresetDefaults,
                    BuildingMarkerManager
    Interior/       InteriorMapGenerator, InteriorMapData,
                    InteriorRenderer, InteriorController,
                    InteriorVisibilityController, InteriorDebugSpawner,
                    InteriorPreset, InteriorRoomType, FurnitureType,
                    FloorNavigator, FloorPlanFactory, FloorPlanUtils,
                    IFloorPlanGenerator,
                    IInteriorInteractable, InteriorSessionState,
                    InteriorEvents, DiscoveryInteractable,
                    DoorInteractable, StairInteractable,
                    InteriorInteractionManager, InteriorFurniturePlanner,
                    DiscoveryRarity, DiscoveryTextSystem,
                    BuildingExplorationRecord,
                    ExplorationProgressManager, ExplorationMenuUI,
                    FloorPlanGenerators/ (Commercial, Industrial,
                      Residential, Special)
    GameLoop/       GameSessionManager, GameSessionUI, GameSessionEvents,
                    QuestData, QuestManager, QuestEvents,
                    SaveManager, SaveData,
                    MapEventBus, IMapEventBus
    Player/         PlayerMovement, CameraController
    UI/             MapControlUI, MiniMapController,
                    WorldPositionTrackerUI, LabelController,
                    VerificationChecklistUI, InteriorFeedbackUI,
                    BuildingMarkerUI, QuestLogUI, QuestHUD
  Editor/           SceneBootstrapper, MapPresetCreator, MapThemeCreator,
                    RoadProfileCreator, InteriorDebugPreview,
                    InteriorPresetCreator
  Shaders/          GridGround.shader, Water.shader, Road.shader,
                    BuildingFade.shader
  Resources/
    Presets/        Preset_Coastal.asset 等
    Themes/         Theme_Dark.asset, Theme_Parchment.asset
    RoadProfiles/   RoadProfile_Modern, RoadProfile_Rural, RoadProfile_Historic
```

## Generation Pipeline (MapManager.Generate)
1. SeededRng → center計算
2. IMapGenerator.Generate → nodes, edges
3. WaterGenerator.DetermineCoastSide → coastSide
4. TerrainGenerator.Generate(rng, center, preset, coastSide, nodes) → terrain (hills only)
5. ElevationMap 生成 (from terrain hills)
6. WaterGenerator.Generate → terrain.waterBodies (W-1: 勾配降下川流路 + W-5: プリセット別蛇行調整)
7. WaterTerrainInteraction.ApplyWaterCarving → ElevationMapへcarving適用
8. ElevationMap.ApplyToNodes → 最終地形形状を反映
9. BuildingPlacer.Place → buildings
10. MapAnalyzer.Analyze → analysis
11. BridgeTunnelDetector.Detect → edge.layer (waterBodies参照)
11b. GroundSemanticMaskBaker.Bake → HeightSlopeTex + SemanticTex (SP-032)
12. MapRenderer.Render → road meshes (RoadProfile駆動, Road.shader)
13. BuildingSpawner.Spawn → building GOs (4 shapes × floor-based height)
14. WaterRenderer.Render → water meshes (typed waterBodies, depth UV2, roughness vertex color)
15. DecorationPlacer.Place → decorations
16. DecorationSpawner.Spawn → decoration GOs + LOD
17. EnsureGroundPlane → elevation-following ground mesh (material instance + mask bind + preset defaults)
18. OnMapGenerated event → ThemeManager再適用 + PlayerMovement位置リセット

## Key Decisions
- カメラは Perspective に統一。Interior時のみOrthographic
- JSX座標系→Unity: `MapGenUtils.ToWorldPosition(coord, elev, preset)` → `new Vector3(coord.x, elev, preset.worldHeight - coord.y)`
- 道路: RoadProfile SO + Road.shader、UV駆動の車線標示・路面表現、1ストリップ/tier
- 建物: 4形状 (Box/L-shape/Cylinder/Stepped), floors × floorHeight (1.2f)
- 高低差: ElevationMap (SlopeProfile駆動 + CarvingData carving対応), 道路・建物・水面が高度追従
- 橋: BridgeTunnelDetector (道路交差 + 河川交差 → layer割当)
- 水面: WaterGenerator (地形追従river + coast) + WaterTerrainInteraction (carving) + WaterRenderer (WaterProfile駆動, depth/roughness vertex data)
- 装飾: DecorationPlacer/Spawner (StreetLight/Tree/Bench/Bollard) + LOD 3段階
- 海岸: 4方向ランダム, 丘陵は海岸・道路ノード回避
- セーブ/ロード: JSON → `Application.persistentDataPath/save.json`
- マップビジュアル方針: SP-040 (hybrid orthophoto relief — 上位方針、五原則、5層アーキテクチャ、モバイル予算、完了条件)
- 地表: SP-032 mask-driven compositing (CPU bake 2xRGBA8 → GridGround.shader 13段階合成, ThemeManager lifecycle管理) — SP-040の下位実装仕様

## ALERT FILTER
- CRITICAL: ビルドエラー、データ不整合、セキュリティ
- WARNING: 既存コード矛盾、未使用依存
- INFO: スタイル提案、最適化候補
- IGNORE: TMP Examples, Tutorial scaffolding
