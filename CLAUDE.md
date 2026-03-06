# MiniMapGame - Project Instructions

## Overview
ミニマリスト抽象タクティカルゲーム — 手続き型マップ生成が中核。
React/Canvasプロトタイプ (map-generator-v3.jsx) をC#/Unityへ移植し、
ゲームプレイ基盤を構築する。

## Engine & Pipeline
- Unity 6.2 beta (6000.2.0b2)
- URP 17.2.0
- Camera: **Perspective** (3D orbit/pan/zoom)
- Input: Unity InputSystem 1.14.0

## Architecture Principles
- Pure data/logic classes に MonoBehaviour 依存禁止
- 全データ構造に `[System.Serializable]`
- 全生成は同一 int seed で決定論的再現
- 外部パッケージ追加禁止 (Unity built-ins only)
- MapPreset は ScriptableObject
- Strategy パターンで IMapGenerator を実装

## Code Style
- C# naming: PascalCase (public), camelCase (private), _camelCase (fields)
- 1ファイル1クラス原則（小型のenum/structは例外）
- namespace: `MiniMapGame`（サブ: `.MapGen`, `.Data`, `.UI`, `.GameLoop`）
- コメント: 日本語OK、ただしAPIドキュメントは英語

## Directory Structure (Target)
```
Assets/
  Scripts/
    Data/           MapData, MapNode, MapEdge, MapBuilding, MapTerrain, MapAnalysis
    Generation/     IMapGenerator, OrganicGenerator, GridGenerator, MountainGenerator, RuralGenerator
    Core/           SeededRng, SpatialHash, BuildingPlacer, MapAnalyzer
    Runtime/        MapManager, MapRenderer, BuildingSpawner, BuildingInteraction
    Interior/       InteriorMapGenerator
    GameLoop/       IEncounterTrigger, IValueObject, IExtractDecision, IMapEventBus
    Player/         PlayerMovement, CameraController (既存リファクタ)
    UI/             WorldPositionTrackerUI, LabelController
  ScriptableObjects/
    MapPreset.asset (per preset type)
  Prefabs/
    Buildings/
    Player/
```

## Key Decisions
- カメラは Perspective に統一。既存のOrthographic依存コード(PlayerMovement, LabelController)は要リファクタ
- 既存の InteractionPointController は BuildingInteraction に吸収予定
- JSXのCW/CH座標系 → Unityワールド座標への変換スケールはMapPresetで定義

## ALERT FILTER
- 🔴 CRITICAL: ビルドエラー、データ不整合、セキュリティ
- 🟡 WARNING: 既存コード矛盾、未使用依存
- 🟢 INFO: スタイル提案、最適化候補
- ⚪ IGNORE: TMP Examples, Tutorial scaffolding
