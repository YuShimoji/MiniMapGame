# MiniMapGame - Project Instructions

## Overview
ミニマリスト抽象タクティカルゲーム — 手続き型マップ生成が中核。
React/Canvasプロトタイプ (map-generator-v3.jsx) をC#/Unityへ移植し、
ゲームプレイ基盤を構築する。

## Engine & Pipeline
- Unity 6.2 beta (6000.2.0b2)
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
- namespace: `MiniMapGame`（サブ: `.Data`, `.Core`, `.MapGen`, `.Runtime`, `.Interior`, `.GameLoop`, `.Player`, `.UI`, `.MiniGame`）
- コメント: 日本語OK、ただしAPIドキュメントは英語

## Unity 6.2 API 注意事項
- `TextAlignmentOptions.TopCenter` → `.Top`
- `TextAlignmentOptions.MiddleLeft` → `.MidlineLeft`
- `TextAlignmentOptions.MiddleRight` → `.MidlineRight`
- `AntialiasingMode.SubPixelMorphologicalAntiAliasing` → `.SubpixelMorphologicalAntiAliasing` (小文字p)

## Directory Structure
```
Assets/
  Scripts/
    Data/           MapData, MapNode, MapEdge, MapBuilding, MapTerrain, MapAnalysis,
                    MapPreset(SO), MapTheme(SO), MapDecoration, DecorationType,
                    HillData, NodeType, GeneratorType
    Core/           SeededRng, SpatialHash, MapGenUtils, MapAnalyzer,
                    TerrainGenerator, BuildingPlacer, DecorationPlacer,
                    BridgeTunnelDetector, ElevationMap, ISpatialBounds
    MapGen/         IMapGenerator, OrganicGenerator, GridGenerator,
                    MountainGenerator, RuralGenerator
    Runtime/        MapManager, MapRenderer, BuildingSpawner, BuildingInteraction,
                    AnalysisVisualizer, ThemeManager, WaterRenderer,
                    DecorationSpawner, PostProcessingManager,
                    AmbientParticleController
    Interior/       InteriorMapGenerator, InteriorMapData,
                    InteriorRenderer, InteriorController
    GameLoop/       GameLoopController, GameState, PlayerStats,
                    EncounterZone, ExtractionPoint, ValueObjectBehaviour,
                    MapEventBus, GameLoopEvents, SaveManager, SaveData, GameLoopUI,
                    IEncounterTrigger, IValueObject, IExtractDecision, IMapEventBus
    Player/         PlayerMovement, CameraController
    UI/             MapControlUI, PlayerHUD, MiniMapController,
                    WorldPositionTrackerUI, LabelController
    MiniGame/       MiniGameManager, MiniGameTypes, IMiniGame, RoomTrigger,
                    TimingCombatGame, MemoryMatchGame, TrapDodgeGame
  Editor/           SceneBootstrapper, MapPresetCreator, MapThemeCreator
  Shaders/          GridGround.shader, Water.shader
  Resources/
    Presets/        Preset_Coastal.asset 等
    Themes/         Theme_Dark.asset, Theme_Parchment.asset
```

## Generation Pipeline (MapManager.Generate)
1. SeededRng → center計算
2. IMapGenerator.Generate → nodes, edges
3. TerrainGenerator.Generate(rng, center, preset, nodes) → terrain
4. ElevationMap → ApplyToNodes
5. BuildingPlacer.Place(nodes, edges, rng, preset, terrain) → buildings
6. MapAnalyzer.Analyze → analysis
7. BridgeTunnelDetector.Detect → edge.layer
8. MapRenderer.Render → road meshes (per-tier materials)
9. BuildingSpawner.Spawn → building GOs (4 shapes × floor-based height)
10. WaterRenderer.Render → water meshes (river + coast)
11. DecorationPlacer.Place → decorations
12. DecorationSpawner.Spawn → decoration GOs + LOD
13. EnsureGroundPlane → elevation-following ground mesh
14. BakeNavMesh

## Key Decisions
- カメラは Perspective に統一。Interior時のみOrthographic
- JSX座標系→Unity: `MapGenUtils.ToWorldPosition(coord, elev, preset)` → `new Vector3(coord.x, elev, preset.worldHeight - coord.y)`
- 道路: プロシージャルMeshバッチ (tier×layer 別メッシュ, 3 outer + 3 inner materials)
- 建物: 4形状 (Box/L-shape/Cylinder/Stepped), floors × floorHeight (1.2f)
- 高低差: ElevationMap (Gaussian falloff), 道路・建物・水面が高度追従
- 橋: BridgeTunnelDetector (道路交差 + 河川交差 → layer割当)
- 水面: WaterRenderer (river ribbon + coast fan polygon + elevation追従)
- 装飾: DecorationPlacer/Spawner (StreetLight/Tree/Bench/Bollard) + LOD 3段階
- 海岸: 4方向ランダム, 丘陵は海岸・道路ノード回避
- セーブ/ロード: JSON → `Application.persistentDataPath/save.json`

## ALERT FILTER
- CRITICAL: ビルドエラー、データ不整合、セキュリティ
- WARNING: 既存コード矛盾、未使用依存
- INFO: スタイル提案、最適化候補
- IGNORE: TMP Examples, Tutorial scaffolding
