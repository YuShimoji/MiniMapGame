# Floor Navigation Interaction (SP-062)

## Overview

階段部屋（Stairwell）でのフロア移動をインタラクション化する。
現在の FloorNavigator は UI ボタンのみだが、空間的な「階段を使う」体験に変える。

SP-060 (Interior Interaction) の IInteriorInteractable パターンに準拠。

## Scope

- Stairwell 部屋に StairInteractable コンポーネントを付与
- プレイヤーが Stairwell に接近 → "E: Go Up" / "E: Go Down" プロンプト
- E キーで階移動（InteriorRenderer.SetActiveFloor + プレイヤーテレポート）
- FloorNavigator UI との連動維持

## Out of Scope

- エレベーター / 梯子 / 別ルート
- 階段のアニメーション演出
- 階段の物理的な傾斜表現

---

## 1. StairInteractable

### 1.1 コンポーネント

```csharp
public class StairInteractable : MonoBehaviour, IInteriorInteractable
{
    public int floorIndex;           // この階段がある階
    public int targetFloorIndex;     // 移動先の階 (+1 or -1)
    public bool goesUp;             // true=上階, false=下階
    public int stairwellRoomId;     // 所属 Stairwell の roomId

    // IInteriorInteractable
    public Vector3 WorldPosition => transform.position;
    public float InteractRadius => 2.0f;
    public bool IsAvailable => true;  // 常に使用可能
    public int FloorIndex => floorIndex;

    public string PromptMessage =>
        goesUp ? "E: Go Upstairs" : "E: Go Downstairs";

    public void Interact(InteriorInteractionManager manager)
    {
        manager.ChangeFloor(targetFloorIndex);
    }
}
```

### 1.2 付与タイミング

InteriorRenderer.CreateNewRoomFloor() 内で、
`room.type == InteriorRoomType.Stairwell` の場合に付与。

各 Stairwell には最大2つの StairInteractable（上方向 + 下方向）:
- 最下階: 上方向のみ
- 最上階: 下方向のみ
- 中間階: 両方

### 1.3 Stairwell の検出

InteriorMapData の各フロアで `InteriorRoomType.Stairwell` を持つ部屋を検出。
同じ Stairwell は隣接フロアで同じ位置（roomId ベース）に存在する設計。

---

## 2. InteriorInteractionManager 拡張

### 2.1 ChangeFloor メソッド

```csharp
public void ChangeFloor(int targetFloorIndex)
{
    // 1. InteriorRenderer.SetActiveFloor(targetFloorIndex)
    // 2. プレイヤーを移動先フロアの Stairwell 位置にテレポート
    // 3. FloorNavigator に通知（UI 更新）
    // 4. FloorVisitedEvent 発行
    // 5. _interactables リストを再スキャン（不要: 全フロア分保持済み）
}
```

### 2.2 Initialize 時の StairInteractable 収集

既存の DiscoveryInteractable / DoorInteractable と同様に
`GetComponentsInChildren<StairInteractable>(true)` で収集し、
`_interactables` リストに追加。

---

## 3. InteriorRenderer 変更

### 3.1 Stairwell 部屋への Collider + コンポーネント付与

CreateNewRoomFloor で Stairwell 判定時:

```csharp
if (room.type == InteriorRoomType.Stairwell)
{
    // Trigger collider for interaction
    var col = go.AddComponent<BoxCollider>();
    col.isTrigger = true;
    col.size = new Vector3(room.size.x * 0.6f, 2f, room.size.y * 0.6f);

    // Determine if this stairwell connects up/down
    bool hasFloorAbove = floorIndex < data.floors.Count - 1;
    bool hasFloorBelow = floorIndex > 0;

    if (hasFloorAbove)
    {
        var upStair = go.AddComponent<StairInteractable>();
        // ... or create child GO for separate up/down targets
    }
}
```

### 3.2 設計選択: 単一 GO vs 子 GO

- **単一 GO + 複数コンポーネント**: StairInteractable を2つ付けると
  FindNearestInteractable が混乱する可能性
- **子 GO 分割**: Stairwell の上半分に UpStair、下半分に DownStair を配置
  → 空間的に自然だが実装が複雑

MVP は単一 GO + `StairInteractable` 1つで、
プロンプトで "E: Go Up / Down (↑↓)" を表示し、E キーで toggle する方式を推奨。

---

## 4. FloorNavigator 連動

### 4.1 現行の FloorNavigator

UI ボタンベースのフロア切替。InteriorRenderer と直結。

### 4.2 変更方針

- FloorNavigator の UI ボタンは引き続き機能（アクセシビリティ）
- StairInteractable からの ChangeFloor 呼び出しは FloorNavigator 経由で統一
- FloorNavigator に `ChangeFloor(int targetFloorIndex)` メソッドを追加

---

## 5. Architecture

### 5.1 新規ファイル

| ファイル | 責務 |
|---------|------|
| StairInteractable.cs | Stairwell インタラクションコンポーネント |

### 5.2 既存ファイル変更

| ファイル | 変更内容 |
|---------|---------|
| InteriorRenderer.cs | Stairwell 部屋に StairInteractable 付与 |
| InteriorInteractionManager.cs | ChangeFloor() メソッド追加 |
| FloorNavigator.cs | ChangeFloor() メソッド追加 / 外部呼び出し対応 |

### 5.3 委譲適性

このタスクは独立性が高く、委譲に適している:
- 新規ファイル 1 + 既存変更 3（局所的）
- SP-060 の IInteriorInteractable パターンに準拠するだけ
- テスト: 建物に入る → Stairwell に接近 → E → 階移動
