# MiniMapGame - Project Instructions

## Overview
手続き型マップ探索ゲーム — プロシージャル都市・地形生成と探索・発見がコア体験。
React/Canvasプロトタイプから C#/Unity へ移植済み。
現フェーズ: 地形生成の視覚品質向上 → 発見物配置 → ゲームループ再設計

## PROJECT CONTEXT
現フェーズ: α（3系統実装完了 → Unity手動検証待ち）
直近の状態 (2026-03-18 session 6 nightshift):

- SP-026レガシークリーンアップ完了 (94%→98%): RoomNode/CorridorEdge/RoomType削除、旧Generate(int seed)廃止、MiniGame参照除去 (-427行)
- SPEC.md同期: section 11 v2 API更新、section 18に Interior v2/Interaction/Discovery追加
- SP-020 specヘッダー修正 (draft/0%→partial/30%)
- spec-index整合性監査: 全partial/todoスペックのpct妥当性確認済み
- origin/master: 11448f2、ローカル+2 commits (未push)

次の作業:
1. Unity Bootstrap → 手動検証3系統 (Interior統合 / SP-032 Slice 5 / SP-020 Layer 1)
2. 検証結果に基づく修正
3. SP-001 Phase 1実装 or SP-020 Layer 2実装

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
| 2026-03-09 | W-1: 川流路を勾配降下方式に変更 | 固定方向 / 勾配降下 / ランダム角度 | メタ地理（標高場）から自然に流れが決まる。丘陵配置→川方向の因果関係が明確 |
| 2026-03-09 | W-5: プリセット別蛇行をGenerate内で自動調整 | CreateDefaultFallback引数追加 / Generate内調整 | RiverConfig structコピーで非破壊的。カスタムWaterProfile時はスキップ |
| 2026-03-09 | W-7: 水辺配置は「排除」ではなく「出現率制御」 | 完全排除 / 出現率制御 / 条件付き配置 | 入り江にレストラン等の配置を許容。マクロ(WaterfrontCharacter) × ミクロ(BuildingContext)の2層制御 |
| 2026-03-09 | P4: 道路をRoadProfile SO + Road.shader駆動に刷新 | マテリアル手動設定 / SO+シェーダー統合 | デザイナー調整可能性・draw call削減・プリセット別表現が必要 |
| 2026-03-09 | プリセット→プロファイル: Coastal/Grid→Modern, Rural/Mountain→Rural | 個別指定 / GeneratorType自動マッピング | Bootstrap時自動化、手動設定は上書きしない設計 |
| 2026-03-09 | E仕様: 道路幅テーブル表記を `max,min` に統一 | `min,max` 維持 / `max,min` 統一 | 実装配列が降順値(例:12→8)のため、読み誤りを防止 |
| 2026-03-09 | B実装方針: 実装コスト優先はモック、見た目優先を最終段階で採用 | コスト優先を本実装 / モック化 / 見た目優先先行 | 手戻り抑制しつつ最終品質を見た目重視で確保するため |
| 2026-03-10 | SP-032地表表現は「carrier mesh + CPU semantic masks + compositing shader」で進める | Unity Terrain移行 / 地面メッシュ維持 + mask合成 | 道路・水・建物のエッジを保ったまま、疑似オルソフォト風の可読性を上げるため |
| 2026-03-10 | SP-032のMVPは単一Ground mesh + 2枚maskで開始し、chunkingはIdeal段階へ後送り | 先にchunking / 先に単一meshMVP | Gate-1完了後に最小リスクで導入し、後段で高解像度化へ繋げるため |
| 2026-03-11 | SO displayNameを英語化（CJKフォント不採用） | A:英語化 / B:CJKフォント追加 / C:バイリンガル | LiberationSans SDFがCJK非対応。フォント追加はアセットサイズ増大。UIテキストは英語で十分 |
| 2026-03-11 | W-4(浜辺遷移帯)をSP-032完了後に後送り | 先行実装 / SP-032後 | GridGround.shaderへの頂点カラー追加がSP-032の地表合成パイプライン刷新と競合するため |
| 2026-03-11 | 操作モデルをクリック移動→WASD三人称に変更 | WASD / クリック維持 / 両対応 | クリック移動は追跡者逃走設計の名残。探索ゲームにはWASD/スティックが自然 |
| 2026-03-11 | アートディレクション仕様はSP-032(地表)以外が未定義→後日SP新規策定 | 先に仕様 / プレイヤブル先行 | プレイヤブル先行を選択。画風仕様は別途策定 |
| 2026-03-11 | NavMesh完全削除→CharacterController化 | NavMesh維持/軽量化 / CharacterController / Rigidbody | WASD移動にNavMesh不要。228秒のフリーズ原因を根本除去 |
| 2026-03-11 | GameLoop UI/Controller/PlayerHUDをSceneBootstrapperで無効化 | 削除 / コメントアウト / 非表示 | DECISION LOG 2026-03-08「GameLoop凍結」の反映。コード残存・セットアップ停止 |
| 2026-03-11 | MapControlUIの独自レスポンシブスケーリング削除 | 修正 / 削除 / CanvasScaler無効化 | CanvasScaler(ScaleWithScreenSize)と二重適用が原因。CanvasScalerに委ねる |
| 2026-03-12 | Gate-1(P4道路検証)をSP-032統合検証に吸収 | SP-032統合 / Gate-1合格扱い / 個別実施 | blocker5件修正済み。SP-032 Slice 5で4preset x 2theme一括検証し道路+地表を同時カバー |
| 2026-03-13 | 水辺ビジュアル基準: 全既定プリセットで水辺を見せる。Gridはriver+bridge既定ON、Dark/Parchment水色は深浅差を読みやすく調整 | river追加なし / riverのみ / river+bridge+palette調整 | 全presetで水辺体験を担保しつつ、Gridの道路-水面交差破綻を避け、SP-032の読図性を上げるため |
| 2026-03-13 | Task B FB修正: coast深浅の実装整合・ThemeCreator同期・Grid表示名互換維持 | docsのみ修正 / 実装修正 / save migration追加 | docs期待値との不整合と再生成巻き戻しを解消しつつ、displayName保存方式で既存saveを壊さないため |
| 2026-03-13 | SP-060: 鍵-ドア1:1紐づけ（汎用鍵にしない）、ドア操作は拡張可能設計(DoorUnlockMethod enum) | 汎用鍵 / 1:1紐づけ | 探索の深さを出すため。将来の破壊/ミニゲーム/回り込みも受容可能 |
| 2026-03-13 | SP-061: 探索記録は永続(建物退出で消えない)、セーブ連携あり | per-visit / 永続 | プレイヤーの進捗を可視化し、再訪問時に前回の状態を維持 |
| 2026-03-13 | SP-062: フロア移動をFloorNavigatorキー操作からStairInteractable(E key)に変更 | キー操作 / インタラクタブル | SP-060のIInteriorInteractableパターンに統一。没入感向上 |
| 2026-03-18 | 建物近接フィードバックを色変化+emission方式に決定 | A:アウトライン / B:色変化 / C:パーティクル / D:テキストのみ | shader追加不要で低コスト。MaterialPropertyBlockで既存パイプラインに統合 |
| 2026-03-18 | Discoveryテキスト方針を全面変更: 空間客観描写 | 旧:ポストアポカリプス陰謀 / 新:空間環境断片 | 人間要素・固有名詞・時間軸・主観知覚を排除。カテゴリ別に空間質を変え、レアリティ=解像度。文体はグラック/マルケス参照。docs/specs/discovery-text-policy.md に方針文書化 |

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
                    NodeType, GeneratorType, BuildingCategory, ShopSubtype,
                    InteriorBuildingContext
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
                    GroundSurfacePresetDefaults
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
    GameLoop/       GameLoopController, GameState, PlayerStats,
                    EncounterZone, ExtractionPoint, ValueObjectBehaviour,
                    MapEventBus, GameLoopEvents, SaveManager, SaveData, GameLoopUI,
                    IEncounterTrigger, IValueObject, IExtractDecision, IMapEventBus
    Player/         PlayerMovement, CameraController
    UI/             MapControlUI, PlayerHUD, MiniMapController,
                    WorldPositionTrackerUI, LabelController,
                    VerificationChecklistUI, InteriorFeedbackUI
    MiniGame/       MiniGameManager, MiniGameTypes, IMiniGame, RoomTrigger,
                    TimingCombatGame, MemoryMatchGame, TrapDodgeGame
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
- 地表: SP-032 mask-driven compositing (CPU bake 2xRGBA8 → GridGround.shader 13段階合成, ThemeManager lifecycle管理)

## ALERT FILTER
- CRITICAL: ビルドエラー、データ不整合、セキュリティ
- WARNING: 既存コード矛盾、未使用依存
- INFO: スタイル提案、最適化候補
- IGNORE: TMP Examples, Tutorial scaffolding
