# MiniMapGame プロジェクト状態監査
> 作成日: 2026-03-26 / 監査セッション: session 8 (nightshift)

---

## 1. 懸念点

| # | 懸念 | 重大度 | 詳細 |
|---|------|--------|------|
| C-001 | Unity 再コンパイル未検証 | 高 | 旧GameLoop 11クラス削除 + 新GameSession 3クラス追加後、Unity Editor での再コンパイル確認が未実施。ビルドエラーの可能性あり |
| C-002 | 未コミット変更が大量に滞留 | 高 | 削除14ファイル + 新規3ファイル + 修正8ファイル。前セッションの作業が未コミット |
| C-003 | GameState.cs がデッドコード化 | 中 | `collectedValue` / `collectedItemIds` は外部から一切参照されていない。SaveManager が空の GameState を保存するのみ |
| C-004 | MiniGame 7ファイルが孤立 | 中 | MiniGameManager / RoomTrigger / 3ゲーム実装がゲームフローに未接続。SP-017 で「位置づけ再検討予定」のまま放置 |
| C-005 | spec-index と spec ファイルの pct 不整合 | 低 | SP-001: index=30 vs spec=15 |
| C-006 | project-context.md のエントリ数/draft 表記が古い | 低 | 「38エントリ (done 23 / partial 9 / draft 1)」→ 実際は partial 10、draft 0 |
| C-007 | SceneBootstrapper に #if false ブロック残存 | 低 | 旧 GameLoopUI + PlayerHUD の約150行が死蔵。削除条件 (Unity再コンパイル確認後) は未達成だが、コードとして不要 |

---

## 2. 機能状態一覧

### 2a. 実装済み機能 (done: 23件)

| SP | 機能 | カテゴリ | 検証状態 | 備考 |
|----|------|----------|----------|------|
| SP-002 | プリセット定義 (7種) | data | 実装時検証済み | Coastal/Rural/Grid/Mountain/Island/Downtown/Valley |
| SP-003 | データ構造 | data | 実装時検証済み | MapNode/Edge/Building/Terrain/Data/Analysis/Decoration |
| SP-004 | SeededRng (決定論的乱数) | core | 実装時検証済み | XOR-shift PRNG |
| SP-005 | マップジェネレータ (4種) | core | 実装時検証済み | Organic/Grid/Mountain/Rural |
| SP-006 | SpatialHash (衝突検出) | core | 実装時検証済み | |
| SP-007 | BuildingPlacer | core | 実装時検証済み | |
| SP-008 | MapAnalyzer | core | 実装時検証済み | |
| SP-009 | 地形生成 | core | 実装時検証済み | 丘陵+川+海岸+Carving |
| SP-010 | Unityシーンアーキテクチャ | system | 実装時検証済み | 18ステップ生成パイプライン |
| SP-012 | ゲームループ (旧: 削除済み) | core | 削除確認済み | 11クラス削除。SP-001で再設計 |
| SP-013 | 座標系変換 | core | 実装時検証済み | |
| SP-014 | 高低差システム | core | 実装時検証済み | ElevationMap + 橋/トンネル |
| SP-015 | 水面レンダリング | system | 実装時検証済み | WaterProfile SO駆動 |
| SP-016 | 装飾・LODシステム | system | 実装時検証済み | 12種+LOD3段階 |
| SP-017 | ミニゲームシステム (凍結中) | core | 実装時検証済み | 3種実装済みだがフロー未接続 |
| SP-018 | テーマシステム | ui | 実装時検証済み | Dark/Parchment |
| SP-023 | RoadProfile (SO) | data | 実装時検証済み | Modern/Rural/Historic |
| SP-024 | Road.shader | system | 実装時検証済み | URP HLSL |
| SP-027 | デバッグセットアップガイド | infra | ドキュメント | |
| SP-030 | 道路幅テーブル監査 | infra | 監査完了 | |
| SP-031 | Git認証トラブルシュート | infra | ドキュメント | |
| SP-034 | Gate-1 実行ランブック | infra | ドキュメント | |
| SP-035 | プリセット拡充・バランス調整 | data | 実装時検証済み | 7プリセット完成 |

### 2b. 確認未完了の機能 (partial: 10件)

| SP | 機能 | pct | 確認手段 | 未完了の内容 |
|----|------|-----|----------|--------------|
| SP-001 | ゲームループ Sandbox+Quest | 30% | Unity手動検証 + 実装 | Phase 0 (セッション基盤) 実装済みだが未検証。Phase 1後半 (Quest基盤: QuestData/QuestManager/QuestLogUI) 未実装 |
| SP-020 | 探索体験ポリッシュ | 50% | Unity手動検証 | Layer 2: BuildingMarkerManager実装済み、Prefab未作成+手動検証待ち。Layer 3: 報酬システム未着手 |
| SP-025 | 水系生成強化 | 57% | Unity手動検証 | W-6河口デルタ、W-7水辺配置 未実装 |
| SP-026 | Interior System v2 | 98% | Unity手動検証 | style拡張 (InteriorStyle→生成バイアス) 残り |
| SP-029 | Pre-Gate仕様整理 | 70% | 人間判断 (HUMAN_AUTHORITY) | SG-02/03/04/06 が設計判断待ち |
| SP-032 | 地表合成レンダリング | 85% | Unity手動検証 | Slice 5: 4preset x 2theme 統合手動検証 |
| SP-033 | SP-032 MVP実装計画 | 85% | Unity手動検証 | SP-032と同じ (Slice 5) |
| SP-060 | Interior Interaction | 95% | Unity手動検証 | Blocker修正済み。Unity実機検証のみ残 |
| SP-061 | Exploration Progress | 95% | Unity手動検証 | Blocker修正済み。Unity実機検証のみ残 |
| SP-062 | Floor Navigation | 95% | Unity手動検証 | Blocker修正済み。Unity実機検証のみ残 |

### 2c. 未実装機能 (todo: 3件)

| SP | 機能 | カテゴリ | 前提条件 | 優先度 |
|----|------|----------|----------|--------|
| SP-019 | パフォーマンス最適化 | infra | メッシュ結合, GPU instancing, プール | 低 (体験完成後) |
| SP-021 | オーディオシステム | system | BGM, 環境音, SE | 中 (体験ループ確立後) |
| SP-022 | UIシステム拡張 | ui | ポーズ, 設定, カメラプリセット | 中 (GameSessionUI確立後) |

### 2d. その他ステータス

| SP | 機能 | ステータス | 備考 |
|----|------|------------|------|
| SP-011 | Interior v1 (legacy) | legacy | SP-026 v2 で置換だが InteriorController 自体はまだ現役 |
| SP-028 | P4道路検証ゲート | merged | SP-032 Slice 5 に統合 |

---

## 3. 検証手段別の未確認機能

### Unity Editor 手動検証が必要 (8件)
これらは全てコード実装済みだが、Unity Editor でのプレイモード確認が未実施。

| SP | 機能 | 検証内容 |
|----|------|----------|
| SP-001 Phase 0 | GameSessionManager | Title→Play→Timer→Results ライフサイクル |
| SP-020 L2 | BuildingMarkerManager | マップ上の探索可視化マーカー |
| SP-032 Slice 5 | 地表合成 | 4preset x 2theme の統合表示確認 |
| SP-033 Slice 5 | (SP-032と同一) | 同上 |
| SP-060 | Interior Interaction | Discovery収集+ドア操作+フィードバックUI |
| SP-061 | Exploration Progress | 建物探索記録+マーカー+メニュー |
| SP-062 | Floor Navigation | 階段移動+フロア切替 |

### 人間の設計判断が必要 (1件)
| SP | 機能 | 待ち項目 |
|----|------|----------|
| SP-029 | Pre-Gate仕様整理 | SG-02/03/04/06 (HUMAN_AUTHORITY) |

### 追加実装が必要 (4件)
| SP | 機能 | 残りの実装 |
|----|------|------------|
| SP-001 | ゲームループ | Phase 1後半: QuestData/QuestManager/QuestLogUI |
| SP-025 | 水系生成強化 | W-6河口デルタ, W-7水辺配置 |
| SP-026 | Interior v2 | style拡張 (2%残) |
| SP-020 | 探索体験ポリッシュ | Layer 2 Prefab作成, Layer 3 報酬システム |

---

## 4. デッドコード / レガシー堆積物

| # | 対象 | ファイル | 行数 | 状態 | 対応 |
|---|------|----------|------|------|------|
| D-001 | `#if false` 旧GameLoopUI/Controller | Editor/SceneBootstrapper.cs:542-691 | ~150行 | 削除可能 | 本セッションで削除 |
| D-002 | `#if false` 旧PlayerHUD | Editor/SceneBootstrapper.cs:1029-1109 | ~80行 | 削除可能 | 本セッションで削除 |
| D-003 | GameState.collectedValue/collectedItemIds | Scripts/GameLoop/GameState.cs | 全34行 | 外部参照なし | 本セッションで整理 |
| D-004 | MiniGame 7ファイル (孤立) | Scripts/MiniGame/*.cs | 7ファイル | フロー未接続 | HUMAN_AUTHORITY (削除 vs Quest統合) |
| D-005 | SaveManager のコメント参照 | Scripts/GameLoop/SaveManager.cs:14 | 1行 | コメントのみ | 本セッションで削除 |
| D-006 | GameState のコメント参照 | Scripts/GameLoop/GameState.cs:7 | 1行 | コメントのみ | 本セッションで削除 |

### 判断保留 (HUMAN_AUTHORITY)
- **MiniGame 7ファイル**: SP-017 で「位置づけ再検討予定」。削除するか Quest システムに統合するかは設計判断。コード自体は動作するため急ぎの削除は不要
- **VerificationChecklistUI**: デバッグ用だが SceneBootstrapper で生成・Scene で使用中。開発中は有用
- **InteriorController (SP-011 legacy)**: v2 の名前だが実態は現役。SP-026 は生成/レンダリング改善で、Controller 自体は置き換わっていない。legacy ラベルが不正確

---

## 5. 定量指標

| 指標 | 値 |
|------|-----|
| 総 .cs ファイル | ~110 |
| namespace | 9 (Core, Data, GameLoop, Interior, MapGen, MiniGame, Player, Runtime, UI) |
| spec-index エントリ | 38 |
| done | 23 (60.5%) |
| partial | 10 (26.3%) |
| todo | 3 (7.9%) |
| legacy | 1 (2.6%) |
| merged | 1 (2.6%) |
| テストファイル | 0 |
| #if false ブロック | 2 (SceneBootstrapper.cs) |
| TODO/FIXME/HACK | 0 |
| 未コミット削除ファイル | 14 |
| 未コミット新規ファイル | 3 + docs |
| 未 push コミット | 6 (前セッション時点) |
