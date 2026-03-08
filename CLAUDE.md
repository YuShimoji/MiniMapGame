# MiniMapGame - Project Instructions

## Overview
手続き型マップ探索ゲーム — プロシージャル都市・地形生成と探索・発見がコア体験。
React/Canvasプロトタイプから C#/Unity へ移植済み。
現フェーズ: 地形生成の視覚品質向上 → 発見物配置 → ゲームループ再設計

## PROJECT CONTEXT
現フェーズ: α（地形品質向上の実装完了・検証待ち）
直近の状態: P5地形品質向上を実装完了。H1丘クラスタ/H2装飾12種/H3傾斜5プロファイル + Q1-Q6視覚改善。デバッグ可視化(AnalysisVisualizer Terrainモード)追加。SPEC.md/spec-index.json同期済み。
次の作業: Unity Editorで4プリセット×複数シードの手動検証 → steepnessBias調整 → 発見物配置設計

## DECISION LOG
| 日付 | 決定事項 | 選択肢 | 決定理由 |
|------|----------|--------|----------|
| 2026-03-08 | ジャンルを「探索・発見ゲーム」に再定義 | タクティカル / 探索・発見 / リスク管理 | 実装にタクティカル要素なし。コア体験は未知マップの探索 |
| 2026-03-08 | エンカウント(自動ダメージ)を削除方向 | 削除 / 発見イベント化 / 軽量チャレンジ / 維持 | 元は「追跡する敵」構想の名残。現実装はコアと無関係 |
| 2026-03-08 | 脱出判断・HP・リプレイ構造は保留 | 各種オプション | まず単一マップの探索品質を固めることが先決 |
| 2026-03-08 | 追跡者(対処不可能な敵)は将来候補として保持 | 即実装 / 保留 / 却下 | 地形品質が固まってから検討。探索の緊張感源として有望 |
| 2026-03-08 | GameLoop関連コードは凍結(コード残存/開発対象外) | 削除 / 凍結 / 継続開発 | 方向性が変わる可能性あり。削除は早計 |
| 2026-03-09 | P5地形品質向上: H1/H2/H3全実装 + Q1-Q6全実装 | 段階的 / 全部 | 探索体験の基盤として地形品質が最優先 |
| 2026-03-09 | デバッグ可視化はAnalysisVisualizerに統合(Tab切替) | 統合 / 別コンポーネント / Editorウィンドウ | 既存インフラ活用、追加依存なし |

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
- namespace: `MiniMapGame`（サブ: `.Data`, `.Core`, `.MapGen`, `.Runtime`, `.Interior`, `.GameLoop`, `.Player`, `.UI`, `.MiniGame`）
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
                    NodeType, GeneratorType
    Core/           SeededRng, SpatialHash, MapGenUtils, MapAnalyzer,
                    TerrainGenerator, BuildingPlacer, DecorationPlacer,
                    BridgeTunnelDetector, ElevationMap, WaterGenerator,
                    WaterTerrainInteraction, ISpatialBounds
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
  Editor/           SceneBootstrapper, MapPresetCreator, MapThemeCreator,
                    RoadProfileCreator
  Shaders/          GridGround.shader, Water.shader, Road.shader
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
6. WaterGenerator.Generate → terrain.waterBodies (ElevationMap参照で谷追従)
7. WaterTerrainInteraction.ApplyWaterCarving → ElevationMapへcarving適用
8. ElevationMap.ApplyToNodes → 最終地形形状を反映
9. BuildingPlacer.Place → buildings
10. MapAnalyzer.Analyze → analysis
11. BridgeTunnelDetector.Detect → edge.layer (waterBodies参照)
12. MapRenderer.Render → road meshes (RoadProfile駆動, Road.shader)
13. BuildingSpawner.Spawn → building GOs (4 shapes × floor-based height)
14. WaterRenderer.Render → water meshes (typed waterBodies, depth UV2, roughness vertex color)
15. DecorationPlacer.Place → decorations
16. DecorationSpawner.Spawn → decoration GOs + LOD
17. EnsureGroundPlane → elevation-following ground mesh
18. BakeNavMesh

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

## ALERT FILTER
- CRITICAL: ビルドエラー、データ不整合、セキュリティ
- WARNING: 既存コード矛盾、未使用依存
- INFO: スタイル提案、最適化候補
- IGNORE: TMP Examples, Tutorial scaffolding
