# Interior Interaction System (SP-060)

## Overview

建物内部でプレイヤーが Discovery アイテムを収集し、ドアを操作する
インタラクションシステム。Interior System v2 (SP-026) の上に構築される。

## Scope

- Discovery (Document / Note / Photo / Container) の収集インタラクション
- ドア操作（解錠・開放）
- 鍵-ドア 1:1 紐づけ
- 隠しドアの近接出現
- セッション状態管理（per-visit、退出リセット）

## Out of Scope (今回実装しないもの)

- 報酬内容の定義（スコア値・テキスト等は最小スタブのみ）
- インベントリ UI
- ドア破壊 / 回り込み / 内側開放（設計のみ）
- セーブ/ロード対応

---

## 1. Discovery Interaction

### 1.1 対象 FurnitureType

| FurnitureType | プロンプト | 用途 |
|---|---|---|
| Document | "E: Read Document" | 情報取得 |
| Note | "E: Pick Up Note" | 情報取得 |
| Photo | "E: Examine Photo" | 情報取得 |
| Container | "E: Search Container" | アイテム / 鍵 |
| Container (key) | "E: Open Container (Key)" | 鍵取得 |

### 1.2 操作フロー

1. プレイヤーが Discovery 家具に接近（半径 1.5m 以内）
2. 画面下部にプロンプトテキスト表示
3. E キー押下で収集
4. 視覚フィードバック: 色変化 + 縮小 → 0.3秒後に Destroy
5. `DiscoveryCollectedEvent` を MapEventBus に発行
6. 同一アイテムの再収集不可（SessionState で管理）

### 1.3 鍵アイテム

- Container のうち、locked door と紐づいたものは鍵アイテム
- 収集時に `linkedDoorIndex` の解錠を SessionState に記録
- 鍵は汎用ではなく、対応ドアと 1:1 紐づけ

---

## 2. Door Interaction

### 2.1 ドア状態遷移

```
Locked ─(対応鍵所持 + E)─→ Unlocked ─(E)─→ Open
  │                              │
  └─(鍵なし + E)─→ "Locked" フィードバック
```

### 2.2 プロンプト

| 状態 | プロンプト |
|---|---|
| Locked | "Locked" |
| Unlocked | "E: Open Door" |
| Open | (プロンプトなし) |
| Hidden (未発見) | (非表示) |

鍵所持の有無はプロンプトに反映しない。E キー押下時に内部で鍵チェックし、
所持していれば解錠、未所持なら操作不可（"Locked" 表示のまま）。

### 2.3 視覚表現

- Locked: 赤色ドア指示器 + 移動ブロック用 Collider
- Unlocked: 緑色ドア指示器
- Open: 指示器縮小/非表示、移動ブロック Collider 除去

### 2.4 移動ブロック

locked ドアには非 trigger BoxCollider を付与し、CharacterController の通過を阻止。
解錠時に Collider を Destroy。

---

## 3. Hidden Door

### 3.1 出現仕様

- 隠しドアは初期状態で GameObject.SetActive(false)
- プレイヤーが半径 1.2m 以内に接近すると自動的に出現
- 出現時に `HiddenDoorRevealedEvent` を発行
- 出現後は通常のドアとして操作可能（locked/unlocked）

### 3.2 壁表現

MVP では、隠しドアの壁隙間は省略。ドア指示器が壁の上に重なって出現する。
将来的に壁メッシュの動的再構築で正確な開口を表現可能。

---

## 4. 鍵-ドア 1:1 紐づけルール

### 4.1 紐づけアルゴリズム

`InteriorInteractionManager.BuildKeyDoorMappings()` で Initialize 時に実行:

1. 全 locked door を順に処理
2. 各 locked door に対し、未割当の Container 型 Discovery の中から最近距離のものを選択
3. 選択された Container に `linkedDoorIndex` を設定し、1:1 紐づけ
4. `SessionState.doorKeyMap[doorIndex] = discoveryId` に登録
5. 使用済み Container は他の door に再割当されない

### 4.2 制約

- 1つの鍵は 1つのドアにのみ対応
- 同じ Container が複数のドアの鍵にはならない
- 鍵を使用してもアイテムは消費されない（セッション状態として記録のみ）

---

## 5. ドア拡張設計方針

### 5.1 DoorUnlockMethod enum

```csharp
enum DoorUnlockMethod
{
    Key,          // 対応鍵による解錠（今回実装）
    Force,        // 破壊（将来: 耐久値、武器/道具要件）
    MiniGame,     // ミニゲームクリアによる解錠（将来）
    InsideOpen,   // 内側からの開放（将来: 別ルート探索）
    Bypass        // 回り込み / 換気口等（将来）
}
```

### 5.2 拡張ポイント

- `DoorInteractable` は `DoorUnlockMethod` を保持し、イベントに含める
- 将来 `CanForceOpen()`, `CanOpenFromInside()` 等のメソッド追加可能
- サブクラス (`DoorBreakable`) またはコンポーネント追加で拡張可能
- 解錠条件は `DoorInteractable` 内で判定、Manager は条件を知らない

---

## 6. Session State

### 6.1 ライフサイクル

```
EnterBuilding → Initialize (SessionState 生成)
  ↓
  探索中 (収集・解錠を記録)
  ↓
ExitBuilding → Cleanup (SessionState リセット)
```

### 6.2 管理データ

| フィールド | 型 | 用途 |
|---|---|---|
| collectedDiscoveries | HashSet<string> | 収集済み Discovery ID |
| unlockedDoors | HashSet<int> | 解錠済みドアインデックス |
| revealedHiddenDoors | HashSet<int> | 出現済み隠しドア |
| doorKeyMap | Dictionary<int, string> | doorIndex → keyDiscoveryId |
| discoveryCount | int | 累計収集数 |
| totalDiscoveryValue | int | 累計スコア |

### 6.3 セーブ/ロード

現段階では未対応。建物を出ると全リセット。
将来的に per-building persistent state として保存可能な構造。

---

## 7. Events

### 7.1 DiscoveryCollectedEvent

```csharp
struct DiscoveryCollectedEvent
{
    string discoveryId;
    FurnitureType furnitureType;
    int value;
    string buildingId;
}
```

### 7.2 DoorUnlockedEvent

```csharp
struct DoorUnlockedEvent
{
    int doorIndex;
    int roomA;
    int roomB;
    DoorUnlockMethod unlockMethod;
    string buildingId;
}
```

### 7.3 HiddenDoorRevealedEvent

```csharp
struct HiddenDoorRevealedEvent
{
    int doorIndex;
    string buildingId;
}
```

---

## 8. Architecture

### 8.1 新規ファイル

| ファイル | 責務 |
|---|---|
| IInteriorInteractable.cs | インタラクション対象インターフェース |
| InteriorSessionState.cs | Per-visit 状態管理 |
| InteriorEvents.cs | MapEventBus 用イベント構造体 |
| DiscoveryInteractable.cs | Discovery 家具コンポーネント |
| DoorInteractable.cs | ドア操作コンポーネント |
| InteriorInteractionManager.cs | 中央管理（近接スキャン・E キー・プロンプト） |

### 8.2 既存ファイル変更

| ファイル | 変更内容 |
|---|---|
| InteriorRenderer.cs | Door/Furniture に Collider + コンポーネント付与 |
| InteriorController.cs | InteractionManager の Initialize/Cleanup 呼出 |
| SceneBootstrapper.cs | InteractionManager GO 作成・配線 |

### 8.3 入力キー

| キー | コンテキスト | アクション |
|---|---|---|
| E | Discovery 近接 | 収集 |
| E | Door 近接 | 解錠 / 開放 |
| (自動) | Hidden door 近接 | 出現 |

PlayerMovement の E キーは建物内で `_currentBuilding == null` のため衝突しない。
