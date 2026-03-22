# Exploration Progress Tracking (SP-061)

**Status**: partial
**実装率**: 95% (コード完了、Unity手動検証待ち)

## Overview

建物探索の進捗を記録・表示するシステム。
探索済み建物をマップ上にマーク、収集アイテム・フロア踏破・鍵/ドア状態を追跡する。

SP-060 (Interior Interaction) の上に構築される。

## Scope

- 建物単位の探索記録（永続化）
- マップ上の探索済みマーカー
- 建物内探索プログレス（フロア踏破、重要アイテム収集）
- 鍵/ドア状態のステータス表示
- メニュー展開時の進捗一覧

## Out of Scope

- 建物間のメタ進捗（地区クリア率等）
- 探索スコアランキング
- 報酬内容の定義（SP-060 と同様、基盤のみ）
- NPC / ストーリー連動

---

## 1. 探索記録データ

### 1.1 BuildingExplorationRecord

建物ごとに保持する永続的な探索記録。

```csharp
[System.Serializable]
public class BuildingExplorationRecord
{
    public string buildingId;
    public bool hasEntered;               // 一度でも入ったか
    public HashSet<int> visitedFloors;    // 訪問済みフロアインデックス
    public int totalFloors;               // 総フロア数（初回入場時に記録）
    public HashSet<string> collectedDiscoveries;  // 収集済みDiscovery ID
    public int totalDiscoveries;          // 総Discovery数（初回入場時に記録）
    public List<KeyDoorStatus> keyDoorStatuses;   // 鍵/ドア状態
}
```

### 1.2 KeyDoorStatus

```csharp
[System.Serializable]
public struct KeyDoorStatus
{
    public int doorIndex;
    public bool keyFound;      // 対応鍵を発見済み
    public bool doorOpened;    // ドアを解錠済み
}
```

### 1.3 ExplorationProgressManager

全建物の探索記録を管理する MonoBehaviour。

```
fields:
  Dictionary<string, BuildingExplorationRecord> records
  MapEventBus eventBus (Subscribe用)

Initialize():
  eventBus.Subscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected)
  eventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked)

OnBuildingEntered(buildingId, InteriorMapData):
  record がなければ新規作成
  hasEntered = true
  totalFloors = data.floors.Count
  totalDiscoveries = 全フロアのDiscovery数を計上
  鍵付きドア情報を KeyDoorStatus として登録

OnFloorVisited(buildingId, floorIndex):
  visitedFloors.Add(floorIndex)

OnDiscoveryCollected(event):
  record.collectedDiscoveries.Add(event.discoveryId)

OnDoorUnlocked(event):
  対応 KeyDoorStatus.doorOpened = true
```

### 1.4 ライフサイクル

```
建物入場 → OnBuildingEntered (record 作成/更新)
  ↓
探索中 → イベント経由で自動更新
  ↓
建物退出 → record は永続保持（リセットしない）
  ↓
セーブ → SaveManager に records を含める
```

InteriorSessionState（per-visit）とは独立。
SessionState はフレーム単位の操作判定用、ExplorationRecord は永続進捗用。

---

## 2. マップ上の探索マーカー

### 2.1 表示ルール

| 状態 | マーカー表現 (MVP/モック) |
|------|--------------------------|
| 未訪問 | マーカーなし（通常の建物表示） |
| 入場済み・未完了 | 建物上に小さな黄色ドット |
| 全Discovery収集 + 全フロア踏破 | 緑色ドット（完了） |

### 2.2 実装方針

- `BuildingSpawner` で生成済みの建物 GO の子に小さな Quad/Sphere を追加
- `ExplorationProgressManager` が `BuildingInteraction.buildingId` をキーに
  対応する建物 GO を検索し、マーカーの色を切替
- マップ再生成時は seed から buildingId が再現されるため、
  `records` を再マッピング可能

---

## 3. 探索プログレス表示

### 3.1 メニュー展開時（Tab キー等）

建物内にいるとき、メニュー展開で以下を表示:

```
── Building: Coastal Shop (Commercial) ──
Floors:      2/3
Discoveries: 4/7  [■■■■□□□]
Keys/Doors:
  [FOUND] Key → Door 2  ✓ Opened
  [    ] Key → Door 5
```

### 3.2 表示要素

| 項目 | 形式 | 説明 |
|------|------|------|
| 建物名 | `{Category} {SubType}` | BuildingClassifier から取得 |
| フロア踏破 | `n/n` | visitedFloors.Count / totalFloors |
| 重要アイテム | `n/n` + バー | collectedDiscoveries.Count / totalDiscoveries |
| 鍵/ドア | リスト | 各 KeyDoorStatus を表示 |

### 3.3 鍵/ドア状態表示

モック段階ではテキスト表記:

| 状態 | 表示 |
|------|------|
| 鍵未発見 + ドア未開 | `[ ] Key → Door N` |
| 鍵発見 + ドア未開 | `[FOUND] Key → Door N` |
| 鍵発見 + ドア開済 | `~~[FOUND] Key → Door N  ✓ Opened~~` (打ち消し線) |

将来はアイコン化:
- 鍵アイコン: グレー → 金色
- ドアアイコン: 赤(施錠) → 緑(解錠) → チェック(開放済)

### 3.4 建物外での表示

メニュー展開時に「最近訪問した建物」のサマリーを表示:

```
── Recent Explorations ──
Commercial Shop     2/3 floors  4/7 items
Residential House   1/1 floors  2/2 items  ✓ Complete
Public Office       0/4 floors  0/12 items (entered)
```

---

## 4. 追加アイデア

### 4.1 建物カテゴリバッジ

マップ上のマーカーに建物カテゴリ（Commercial / Residential / Public / Industrial / Special）を
色で区別する。カテゴリは BuildingClassifier で既に決定されているため、追加データ不要。

| カテゴリ | マーカー色 |
|---------|-----------|
| Commercial | 黄色 |
| Residential | 青色 |
| Public | 白色 |
| Industrial | オレンジ |
| Special | 紫色 |

### 4.2 Discovery レアリティ

FurnitureType に基づくレアリティ分類:

| レアリティ | 対象 | 表示 |
|-----------|------|------|
| Common | Note | 通常テキスト |
| Uncommon | Document, Photo | 太字 |
| Rare | Container (鍵以外) | 金色テキスト |
| Key | Container (鍵) | 鍵アイコン |

MVP ではレアリティ表示なし。将来、収集ログで区別する。

### 4.3 「NEW」インジケータ

未訪問の建物に近接したとき、インタラクションプロンプトに `[NEW]` を付与:
```
E: Enter Building [NEW]
```

records に buildingId が存在しなければ NEW 判定。

### 4.4 探索コンパス（将来）

最も近い未探索建物への方角を HUD に表示。
現段階では不要だが、ExplorationProgressManager に
`GetNearestUnexplored(Vector3 position)` メソッドを用意可能。

---

## 5. 永続化

### 5.1 SaveData 拡張

```csharp
[System.Serializable]
public class SaveData
{
    // 既存
    public int seed;
    public string presetName;
    public GameState gameState;
    public string timestamp;

    // 追加
    public List<BuildingExplorationRecord> explorationRecords;
}
```

### 5.2 保存タイミング

- 建物退出時（自動保存候補）
- 手動セーブ時
- マップ再生成時は records をクリアしない（seed が変わっても buildingId ベースで照合）

### 5.3 seed 再生成との整合性

同じ seed で再生成すると同じ buildingId が生成される。
異なる seed ではマッチしない records は保持するが表示されない（孤児レコード）。

---

## 6. Architecture

### 6.1 新規ファイル

| ファイル | 責務 |
|---------|------|
| ExplorationProgressManager.cs | 全建物の探索記録管理 + イベント購読 |
| BuildingExplorationRecord.cs | 永続データ構造 |
| ExplorationMenuUI.cs | メニュー展開時の進捗表示 |

### 6.2 既存ファイル変更

| ファイル | 変更内容 |
|---------|---------|
| InteriorController.cs | 入退場時に ExplorationProgressManager 通知 |
| InteriorInteractionManager.cs | フロア変更時に通知 |
| BuildingSpawner.cs | マーカー表示メソッド追加 |
| SaveManager.cs | explorationRecords の保存/読込 |
| SceneBootstrapper.cs | ExplorationProgressManager 配線 |

### 6.3 イベント追加

| イベント | 用途 |
|---------|------|
| BuildingEnteredEvent | 建物入場記録 |
| FloorVisitedEvent | フロア踏破記録 |

既存の DiscoveryCollectedEvent / DoorUnlockedEvent はそのまま活用。

### 6.4 入力キー

| キー | コンテキスト | アクション |
|------|-------------|-----------|
| Tab | 建物内 | 探索プログレスメニュー表示/非表示 |
| Tab | 建物外 | 最近の探索サマリー表示/非表示 |

---

## 7. 実装優先度

| Phase | 内容 | 見積 |
|-------|------|------|
| 1 | データ構造 + ExplorationProgressManager + イベント購読 | 小 |
| 2 | InteriorController/InteractionManager 通知配線 | 小 |
| 3 | マップ上マーカー表示 | 中 |
| 4 | 探索メニュー UI (モック: テキストベース) | 中 |
| 5 | SaveManager 永続化対応 | 小 |
| 6 | カテゴリバッジ / NEW インジケータ | 小 |
