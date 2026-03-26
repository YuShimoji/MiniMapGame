# SP-001: ゲームループ再設計 — Sandbox + Quest

status: partial
pct: 30
cat: core

## 概要

ゲームのコアループを「Sandbox型 + クエストシステム」として再定義する。
旧来のタクティカル/脱出ゲーム用GameLoop (HP, エンカウント, 脱出) を廃止し、
自由探索を軸にクエスト駆動の目的・報酬を段階的に追加していく設計。

### 設計哲学

- **自由探索が基本**: 終了条件なし。好きなだけ歩き、好きな建物に入り、好きなタイミングでやめる
- **クエストは動機の供給**: 「次にどこへ行くか」のヒントと、発見への報酬を与える
- **クエストは強制しない**: クエストを無視して自由に探索しても問題ない
- **量で勝負**: 少数の深いクエストではなく、大量の小さなクエストで多様性を出す

---

## 1. コアフロー

```
起動
  ↓
プリセット選択 (7種 + シード指定)
  ↓
マップ生成
  ↓
自由探索 ← ←
  │        ↑
  ├→ 建物入場 → Interior探索 → Discovery収集 → 退出 → ←
  │
  ├→ クエスト受注 → クエスト進行 → クエスト完了 → 報酬
  │
  ├→ セーブ / ロード
  │
  └→ 終了 (任意タイミング)
```

### 1.1 セッション構造

- **1セッション = 1マップ**: プリセット + シードで生成される1枚のマップ
- **セーブは1スロット**: 現在のマップ + 探索進捗 + クエスト状態を保存
- **New Game**: 新しいシード or 新しいプリセットでマップを再生成
- **永続アンロック**: セッション間で累積する要素 (将来: SP-020 Layer 3)

---

## 2. クエストシステム

### 2.1 クエストの基本構造

```
Quest (ScriptableObject or JSON)
├── id: string
├── title: string (EN/JA)
├── description: string (EN/JA)
├── questType: QuestType
├── objectives: List<QuestObjective>
│   ├── type: ObjectiveType
│   ├── target: string (建物カテゴリ, FurnitureType, etc.)
│   ├── count: int
│   └── isCompleted: bool
├── reward: QuestReward
│   ├── value: int
│   └── unlockId: string (optional)
├── prerequisites: List<string> (先行クエストID)
└── isRepeatable: bool
```

### 2.2 QuestType (クエスト種別)

| 種別 | 説明 | 例 |
| --- | --- | --- |
| Exploration | 指定条件の建物に入る / フロアを踏破する | 「商業施設を3つ探索せよ」 |
| Collection | 指定種類のDiscoveryを集める | 「Documentを10個収集せよ」 |
| Survey | 指定エリア / カテゴリの建物を完全探索 | 「Industrial地区を完全踏破せよ」 |
| Discovery | 特定の条件でRareアイテムを発見する | 「隠し部屋でRareを見つけよ」 |
| Traversal | マップ上の指定地点を巡回する | 「全dead endを訪問せよ」 |

### 2.3 ObjectiveType (目標種別)

| 種別 | target の意味 | 例 |
| --- | --- | --- |
| EnterBuilding | BuildingCategory名 | "Commercial" |
| CollectDiscovery | FurnitureType名 | "Document" |
| VisitFloor | 階数 | "3" (3階以上の建物) |
| CompleteBuilding | BuildingCategory名 or "*" | "Industrial" |
| FindRare | DiscoveryRarity名 | "Rare" |
| VisitNode | ノード条件 | "deadEnd" |

### 2.4 クエスト生成方式

**Phase 1 (MVP)**: 手書きJSON + プリセット別フィルタ
- 各プリセットで成立するクエストを事前定義 (20-30件)
- マップ生成時にプリセットに合うクエストをフィルタしてアクティブ化
- 順序なし、同時に複数受注可能

**Phase 2**: プロシージャル生成
- マップ分析結果 (MapAnalysis) からクエストを自動生成
- 建物カテゴリ分布、dead end数、フロア数、Discovery密度に基づく
- テンプレート + パラメータ化で多様なクエストを量産

**Phase 3**: チェーンクエスト
- クエスト完了で次のクエストが出現
- Rareテキストの手がかりに基づく連鎖
- 世界観の断片的な発見体験

---

## 3. 報酬システム

### 3.1 即時報酬

- **value加算**: クエスト完了時にvalueを獲得
- **Toast通知**: クエスト完了メッセージ
- **進捗可視化**: クエストログに完了マーク

### 3.2 累積報酬 (将来: SP-020 Layer 3)

- **累積valueによるアンロック**: 新テーマ, 特殊建物タイプ
- **クエスト完了数によるランク**: 探索者ランク表示
- **コレクション完成ボーナス**: 全Rareテキスト収集 etc.

---

## 4. UI設計

### 4.1 クエストログ

- **表示トリガー**: Q キーでトグル (or Tabメニュー内タブ)
- **表示内容**:
  - アクティブクエスト一覧 (タイトル + 進捗バー)
  - 各クエストの詳細展開 (description + objectives + 報酬)
  - 完了済みクエスト (折りたたみ)

### 4.2 クエストHUD

- **画面端にミニ表示**: 現在追跡中のクエスト1件
- **目標進捗**: 「Document 3/10」のようなカウンター
- **完了時フラッシュ**: Toast通知と連動

### 4.3 クエスト受注

- **自動受注**: マップ生成時に全クエストがアクティブ
- **明示的な受注UI は Phase 1 では不要**: Sandbox型なので「やりたければやる」

---

## 5. 旧GameLoopからの移行

### 5.1 削除対象

| クラス | 理由 |
| --- | --- |
| PlayerStats | HP/ダメージは不要。探索にリスク要素なし |
| EncounterZone | チョークポイントダメージは廃止 |
| ExtractionPoint | 脱出メカニクスは廃止 |
| ValueObjectBehaviour | dead endアイテムはInterior Discoveryで代替 |
| GameLoopUI | 旧HUD。クエストUIに置換 |
| GameLoopEvents | 旧イベント群。クエストイベントに置換 |
| IEncounterTrigger / IValueObject / IExtractDecision | 旧インターフェース |

### 5.2 改修対象

| クラス | 変更内容 |
| --- | --- |
| GameLoopController | → ExplorationController にリネーム。クエスト管理を担当 |
| GameState | encounterCount/PlayerStats削除。クエスト状態を追加 |
| SaveData | クエスト状態の保存を追加 |
| SaveManager | GameLoopController参照を ExplorationController に変更 |

### 5.3 新規作成

| クラス | 責務 |
| --- | --- |
| QuestData | クエスト定義データ (SO or JSON) |
| QuestObjective | 目標定義 |
| QuestState | クエストの進行状態 |
| QuestManager | クエストの受注/進行/完了管理。MapEventBus購読 |
| QuestLogUI | クエストログ表示 |
| QuestHUD | ミニ表示 |
| QuestEvents | クエスト関連イベント定義 |

### 5.4 維持

| クラス | 理由 |
| --- | --- |
| MapEventBus | 変更なし。全システムの基盤 |
| IMapEventBus | 変更なし |
| SaveManager | 参照先変更のみ |

---

## 6. 実装フェーズ

### Phase 0: タイマーセッション基盤 [DONE] ✓

5分間の閉じた体験ループ。クエストシステムなしで「探索→発見→スコア」の最小ループを成立させる。

実装済みコンポーネント:
- `GameSessionManager` — セッションライフサイクル (Title/Playing/Paused/Results)
- `GameSessionUI` — プログラマティック4パネル UI
- `GameSessionEvents` — SessionStartedEvent / SessionEndedEvent
- `SceneBootstrapper.SetupGameSession()` — シーン統合

フロー:
```
タイトル画面 → PLAY → マップ生成 → 5分タイマー開始
  → WASD探索 → 建物進入 → Discovery収集 → HUDカウンタ更新
  → ESC → ポーズ → Resume / Restart / Quit
  → タイマー0 → 結果画面 (入場建物数/完全探索数/発見数)
  → Play Again → タイトルへ
```

受け入れ条件:
1. タイトル画面からPLAYで開始できる
2. HUDにタイマー(M:SS)と進捗カウンタが表示される
3. ESCでポーズ/リジューム/リスタート/終了ができる
4. タイマー0で結果画面にスコアが表示される
5. Play Againでタイトルに戻り新しいマップで再開できる

### Phase 1: 旧GameLoop整理 + 最小クエスト基盤

1. ~~旧クラス削除 (PlayerStats, EncounterZone, ExtractionPoint, ValueObjectBehaviour, GameLoopUI, GameLoopEvents, GameLoopController, PlayerHUD, IEncounterTrigger, IValueObject, IExtractDecision)~~ [DONE]
2. ~~GameState 簡素化 (encounterCount/PlayerStats除去)~~ [DONE]
3. ~~SaveManager から GameLoopController 参照除去~~ [DONE]
4. ~~SceneBootstrapper から旧GameLoop関連コード除去~~ [DONE]
5. ~~MiniGameCompletedEvent を MiniGame namespace に移動~~ [DONE] → MiniGame全削除済み
6. ~~QuestData / QuestObjective / QuestState データクラス作成~~ [DONE]
7. ~~QuestManager 最小実装 (MapEventBus購読 → 目標進捗追跡)~~ [DONE]
8. ~~手書きクエスト10件 (JSON)~~ [DONE]
9. ~~QuestLogUI (Qキートグル)~~ [DONE]
10. ~~SaveData にクエスト状態保存~~ [DONE]
11. ~~GameSessionManager にクエスト統計連携~~ [DONE]
12. ~~InteriorController から BuildingEnteredEvent 発行~~ [DONE]

### Phase 2: クエスト拡充 + HUD

1. クエスト20-30件に拡充
2. QuestHUD (画面端ミニ表示)
3. プリセット別クエストフィルタ
4. SaveData にクエスト状態保存

### Phase 3: プロシージャルクエスト + チェーン

1. MapAnalysis からのクエスト自動生成
2. チェーンクエスト (先行条件)
3. 累積報酬システム (SP-020 Layer 3 統合)

---

## 7. 追跡者について

DECISION LOG 2026-03-08: 「追跡者(対処不可能な敵)は将来候補として保持」

Sandbox + クエスト設計との関係:
- **追跡者はクエストシステムとは独立**。導入する場合は別仕様 (SP-XXX) として策定
- **追跡者がいる場合のゲーム感の変化**: 「自由に歩ける」→「常に緊張感がある」
- **Phase 3以降の検討事項**: クエスト完了条件に「追跡者から逃げながら」を追加できる設計余地を残す
- **現時点では導入しない**: まずSandbox + クエストの基本体験を固める

---

## 8. 受け入れ条件

### Phase 1

1. 起動 → プリセット選択 → マップ生成 → 自由探索が一気通貫で動作する
2. 旧GameLoop要素 (HP, エンカウント, 脱出) が完全に除去されている
3. 最低10件のクエストがアクティブ化される
4. クエスト進捗が自動追跡される (建物入場/Discovery収集をトリガー)
5. クエストログでアクティブ/完了クエストを確認できる
6. セーブ/ロードでクエスト状態が保持される

---

## 9. やらないこと (Out of Scope)

- NPC / 会話システム
- アイテム装備 / インベントリ管理
- 戦闘 / HP / ダメージ
- マルチプレイヤー
- プロシージャルクエスト生成 (Phase 2以降)
- チェーンクエスト (Phase 3以降)
- 追跡者 (別仕様)
- オーディオ (SP-021)
