# SP-026: Interior System v2

## 概要
建物内部の間取り生成・レンダリング・探索システムの全面刷新。
建物カテゴリ・ShopSubtype・環境コンテキストに基づく多様な間取りを生成し、
マルチフロア対応・カメラ距離LOD・デバッグプレビューを提供する。

## ステータス
- Phase 1 (データ層+生成ロジック): **完了**
- Phase 2 (レンダリング+フロアナビ+可視性制御): **完了**
- Phase 3 (統合パイプライン): **完了**（レガシークリーンアップは未実施）

---

## Phase 1: データ層+生成ロジック (完了)

### 1.1 新規データ型

| ファイル | 型 | 説明 |
|---|---|---|
| BuildingCategory.cs | enum | Residential / Commercial / Industrial / Public / Special |
| ShopSubtype.cs | enum | 17種。tier別にグループ化 (tier0: Department等, tier1: Grocery等, tier2: Pawnshop等) |
| InteriorBuildingContext.cs | struct | 外部→内部のコンテキスト橋渡し (buildingId, footprint, tier, floors, category, shopSubtype, elevation, nearCoast/River/Hill, mapType) |
| InteriorRoomType.cs | enum | 34種の部屋タイプ (Entrance, Hallway, Stairwell, Corridor, LivingRoom, Bedroom, Kitchen, Bathroom, DiningRoom, Storage, Shopfront, Backroom, Counter, DisplayArea, SeatingArea, Bar, Workshop, LoadingDock, MachineryRoom, Lobby, Office, MeetingRoom, Archive, Laboratory, ServerRoom, SecretRoom, Vault, Ruin, Rooftop, Basement, WallVoid, Shaft, Restroom, Utility) |
| FurnitureType.cs | enum | 31種 (Phase 2用、家具配置) |

### 1.2 InteriorPreset (ScriptableObject)

`[CreateAssetMenu(menuName = "MiniMapGame/InteriorPreset")]`

| カテゴリ | パラメータ | デフォルト |
|---|---|---|
| Layout | minRoomSize / maxRoomSize | 3.0 / 8.0 |
| | maxRoomsPerFloor / corridorWidth / wallHeight / doorWidth | 8 / 1.5 / 3.0 / 1.2 |
| | irregularity (0=規則的, 1=不規則) | 0.2 |
| Floor | basementFloors / useExteriorFloorCount / overrideFloorCount | 0 / true / 3 |
| Dead Space | deadSpaceRatio / wallVoidProbability | 0.05 / 0.1 |
| Discovery | discoveryDensity / secretRoomProbability / lockedDoorProbability | 0.3 / 0.1 / 0.05 |
| Furniture | furnitureDensity | 0.5 |
| Decay | decayLevel (0=pristine, 1=ruined) | 0.0 |
| Style | InteriorStyle enum | Modern |
| Visual | floorColor / wallColor / corridorColor / secretRoomColor | 白系 |
| | roomColorOverrides[] | (部屋タイプ別色指定) |

### 1.3 BuildingClassifier

`BuildingClassifier.Classify(MapBuilding, MapData, SeededRng) → InteriorBuildingContext`

- 決定論的: building.id基準のSeededRngで分類
- Tier別カテゴリ確率分布:
  - Tier 0 (幹線): 商業40% / 公共25% / 住居20% / 特殊15%
  - Tier 1 (二次): 商業50% / 住居35% / 公共10% / 産業5%
  - Tier 2 (裏道): 住居30% / 商業30% / 産業25% / 特殊15%
- GeneratorType補正: Rural→住居+20%, Mountain→産業+15%, Grid→商業+10%
- ランドマーク→Special固定
- 環境コンテキスト検出: nearCoast/nearRiver/nearHill (道路セグメントから20f以内)

### 1.4 InteriorMapData拡張

新構造:
- `InteriorMapData` → `List<InteriorFloorData> floors` + `InteriorBuildingContext context`
- `InteriorFloorData` → `floorIndex` + `List<InteriorRoom> rooms` + `List<InteriorDoor> doors` + `List<InteriorCorridor> corridors`
- `InteriorRoom` struct: id, type, position, size, rotation, discoverySlotCount, isSecret
- `InteriorDoor` struct: roomA, roomB, position, width, isHidden, isLocked
- `InteriorCorridor` struct: roomA, roomB, width, waypoints

後方互換: レガシー型 (RoomNode, CorridorEdge, RoomType) を `[Obsolete]` で残存。旧 `Generate(int seed)` パスも動作。

### 1.5 間取り生成ストラテジー

`IFloorPlanGenerator.Generate(rng, context, preset, floorIndex) → InteriorFloorData`

| 実装 | 対象カテゴリ | 特徴 |
|---|---|---|
| ResidentialFloorPlan | Residential | BSP分割、フロア別部屋タイプ (1F: LivingRoom/Kitchen, 2F+: Bedroom)、中央廊下パターン |
| CommercialFloorPlan | Commercial, Public | ShopSubtype別17パターン、frontZone/backZone分割 (tier依存比率)、SeatingArea/Counter/DisplayArea |
| IndustrialFloorPlan | Industrial | 少数大部屋 (3-5)、LoadingDock端配置、高デッドスペース (25%下限) |
| SpecialFloorPlan | Special | 3サブモード: Landmark (グランドロビー+最上階Vault) / Ruin (高irregularity+30-50%崩壊) / ResearchFacility (中央廊下+Lab/Archive翼) |

共通ユーティリティ (FloorPlanUtils):
- BSP `Subdivide()` — 再帰二分割
- `FindAdjacentPairs()` — 隣接部屋ペア検出
- `PlaceDoors()` — 隣接ペア間にドア配置
- `EnsureConnectivity()` — Union-Findで全部屋到達可能性保証
- `InsertDeadSpace()` — WallVoid/Shaft挿入
- `CalculateFootprint()` — shapeType別フットプリント計算

### 1.6 MapPreset連携

`MapPreset.defaultInteriorPreset` フィールド追加。各プリセットアセットにInteriorPresetを紐づけ可能。

---

## Phase 2: レンダリング+フロアナビ+可視性制御 (完了)

### 2.1 マルチフロアレンダリング

**現状**: InteriorRendererは単一フロア（レガシーrooms/corridors）のみ描画。
**目標**: 新InteriorMapDataのfloors[]を階層別に描画し、現在階のみ表示。

設計:
- `InteriorRenderer.Render(InteriorMapData, Vector3, ...)` を新データ対応にオーバーロード
- 各フロアを独立したGameObject (`Floor_N`) 配下に生成
- `currentFloorIndex` でアクティブフロアのみ表示、他は非表示
- 部屋色: InteriorPresetの `floorColor/wallColor` + `roomColorOverrides` 適用
- ドア: 壁のLineRendererに開口部（gap）を設ける
- InteriorRoomType別デフォルト色マッピング追加

### 2.2 FloorNavigator（階数移動）

プレイヤーがStairwell部屋に到達したときにフロア移動を提供する。

設計:
- `FloorNavigator` コンポーネント: InteriorControllerの子として動作
- Stairwell部屋にトリガーを配置 → 上下移動のUIボタン or 自動移動
- フロア切替時: 現フロアを非表示、次フロアを表示、プレイヤー位置を対応Stairwellに移動
- 階数表示: UIに現在フロア番号 (B1, 1F, 2F, ...) を表示

### 2.3 ワールドマップ視認性制御

**課題**: 建物内部がワールドマップのカメラ引き時に全て見えると情報過多。
**方針**: カメラ距離に応じたInterior描画物のLOD/カリング。

設計:
- `InteriorVisibilityController` コンポーネント
- CameraController.CurrentDistance を参照
- 閾値ベースの段階制御:
  - **Near** (distance < nearThreshold): 全詳細表示（部屋・壁・ドア・家具）
  - **Mid** (nearThreshold ≤ distance < farThreshold): 壁・ドアのみ、家具非表示
  - **Far** (distance ≥ farThreshold): Interior全体非表示
- 閾値はInspectorで調整可能
- Interior状態（IsInside）時は常にNear扱い

### 2.4 デバッグプレビューモード

**課題**: プレイヤー操作なしでインテリア生成結果を確認したい。他の開発作業との並行のため、ゲームプレイ不要の確認手段が必要。

設計:
- `InteriorDebugPreview` エディタウィンドウ (`EditorWindow`)
  - seed入力 / buildingId指定 / ランダム生成ボタン
  - InteriorPreset選択フィールド
  - BuildingCategory / ShopSubtype / floors / tier のオーバーライド
  - 「Generate」ボタン → InteriorMapGenerator.Generate() をEditor上で実行
  - 結果をSceneViewにGizmo描画（部屋矩形 + ドア位置 + 部屋タイプラベル）
  - フロア切替スライダー
- ランタイム確認用: `InteriorDebugSpawner` MonoBehaviour
  - Inspectorでパラメータ設定、Play時に指定位置にインテリアを即生成
  - プレイヤー不要、カメラのみで確認可能

---

## Phase 3: 統合パイプライン (完了)

### 3.1 コンテキスト受け渡し

```
BuildingPlacer.Place → MapBuilding[]
    ↓
BuildingSpawner.Spawn → GameObject + BuildingInteraction
    ↓ (BuildingClassifier.Classify)
BuildingInteraction.context = InteriorBuildingContext
    ↓ (player interaction)
InteriorController.EnterBuilding(BuildingInteraction)
    ↓
InteriorMapGenerator.Generate(context, preset, seed)
    ↓
InteriorRenderer.Render(data)
```

### 3.2 InteriorController更新
- `EnterBuilding` で新 `Generate(context, preset, seed)` を使用
- MapPreset.defaultInteriorPreset を参照
- フロアナビゲーション初期化

### 3.3 レガシークリーンアップ (未実施)
- `[Obsolete]` 型の削除（RoomNode, CorridorEdge, RoomType）
- 旧 `Generate(int seed)` パスの削除
- InteriorRendererの旧 `Render` シグネチャ削除
- RoomTriggerの新InteriorRoomType対応

### 3.4 実装済みファイル一覧

**Phase 2 新規ファイル:**
- `InteriorRenderer.cs` — マルチフロア対応リライト (FloorRenderGroup, ドアgap, 34色マッピング, レガシー互換)
- `FloorNavigator.cs` — Stairwell近接検出 + PageUp/PageDown移動 + 階数ラベル (B1/1F/2F)
- `InteriorVisibilityController.cs` — カメラ距離3段階LOD (Full/Minimal/Hidden)
- `InteriorDebugSpawner.cs` — ランタイムプレビュー (F5=再生成, PageUp/Down=フロア切替)
- `InteriorDebugPreview.cs` — Editorウィンドウ (SceneView Gizmo描画, パラメータ調整)
- `InteriorPresetCreator.cs` — 6プリセットアセット生成エディタ

**Phase 3 更新ファイル:**
- `InteriorController.cs` — v2 context-aware生成 + FloorNavigator/VisibilityController統合
- `BuildingInteraction.cs` — `InteriorBuildingContext context` フィールド追加
- `BuildingSpawner.cs` — `BuildingClassifier.Classify()` 呼び出し追加

---

## 暗黙仕様 (Phase 1で決定済み)

| 項目 | 値 | 根拠 |
|---|---|---|
| Tier 0 商業frontZone比率 | 75% | 幹線沿い=来客メイン |
| Tier 2 商業frontZone比率 | 50% | 裏道=作業スペース重視 |
| 産業デッドスペース下限 | 25% | 倉庫・工場の未利用空間 |
| 廃墟irregularity乗数 | 2.0x | 崩壊した構造の不規則さ |
| 廃墟Ruin/WallVoid変換率 | 30-50% | 部屋の相当部分が崩壊 |
| 研究施設lockedDoorProbability | 0.5 | セキュリティの高い施設 |
| ランドマーク最上階 | Vault配置 | 探索の到達報酬 |
| ランドマークsecretRoom倍率 | 1.5x | 探索密度の高い重要建物 |

## 未決事項

- [ ] InteriorStyleの各値 (Modern/Natural/Urban/Suburban/Rural/Mixed/Bizarre) が生成にどう影響するか → Phase 2でPresetパラメータへの自動バイアスとして実装予定
- [ ] 家具配置の詳細仕様 (FurnitureType → 部屋タイプ別配置ルール)
- [ ] ミニゲームシステムとの接続: 新InteriorRoomTypeベースのトリガー条件
- [ ] 双方向テレイン影響 (interior ↔ exterior): Phase 3以降で設計
