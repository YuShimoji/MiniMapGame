# MiniMapGame - Project Instructions

## Overview
ミニマリスト抽象タクティカルゲーム — 手続き型マップ生成が中核。
React/Canvasプロトタイプ (map-generator-v3.jsx) をC#/Unityへ移植し、
ゲームプレイ基盤を構築する。

## Engine & Pipeline
- Unity 6.2 beta (6000.2.0b2)
- URP 17.2.0
- Camera: **Perspective** (3D orbit/pan/zoom, Interior時はOrthographic)
- Input: Unity InputSystem 1.14.0

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

## Directory Structure
```
Assets/
  Scripts/
    Data/           MapData, MapNode, MapEdge, MapBuilding, MapTerrain, MapAnalysis,
                    MapPreset(SO), HillData, NodeType, GeneratorType
    Core/           SeededRng, SpatialHash, MapGenUtils, MapAnalyzer,
                    TerrainGenerator, ISpatialBounds
    MapGen/         IMapGenerator, OrganicGenerator, GridGenerator,
                    MountainGenerator, RuralGenerator
    Runtime/        MapManager, MapRenderer, BuildingSpawner, BuildingInteraction,
                    AnalysisVisualizer, ThemeManager
    Interior/       InteriorMapGenerator, InteriorMapData,
                    InteriorRenderer, InteriorController
    GameLoop/       GameLoopController, GameState, PlayerStats,
                    EncounterZone, ExtractionPoint, ValueObjectBehaviour,
                    MapEventBus, GameLoopEvents, SaveManager, SaveData, GameLoopUI,
                    IEncounterTrigger, IValueObject, IExtractDecision, IMapEventBus
    Player/         PlayerMovement, CameraController
    UI/             MapControlUI, PlayerHUD, MiniMapController,
                    WorldPositionTrackerUI, LabelController
  Editor/           SceneBootstrapper, MapPresetCreator, MapThemeCreator
  Resources/
    Presets/        Preset_Coastal.asset 等
    Themes/         Theme_Dark.asset, Theme_Parchment.asset
  Prefabs/
    Buildings/      NormalBuilding, LandmarkBuilding
    Player/
```

## Key Decisions
- カメラは Perspective に統一。Interior時のみOrthographic (`CameraController.SetInteriorMode`)
- BuildingInteraction が建物インタラクションを担当 (InteriorController と連携)
- JSX CW/CH座標系 → Unity変換: `MapGenUtils.ToWorldPosition(coord, preset)` → `new Vector3(coord.x, 0f, preset.worldHeight - coord.y)`
- 道路レンダリングはプロシージャルMeshバッチ (outer/inner 各1メッシュ)
- セーブ/ロード: JSON via JsonUtility → `Application.persistentDataPath/save.json`
- マテリアルは色別Dictionary キャッシュで再利用 (リーク防止)

## ALERT FILTER
- CRITICAL: ビルドエラー、データ不整合、セキュリティ
- WARNING: 既存コード矛盾、未使用依存
- INFO: スタイル提案、最適化候補
- IGNORE: TMP Examples, Tutorial scaffolding
