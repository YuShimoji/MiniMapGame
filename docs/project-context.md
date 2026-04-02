# Project Context

## PROJECT CONTEXT

- プロジェクト名: MiniMapGame
- 環境: Unity 6.3 LTS (6000.3.6f1) / C# / URP 17.3.0 / InputSystem 1.18.0
- ブランチ戦略: trunk-based (master のみ)
- 現フェーズ: α (地形/道路/水面/Interior 実装完了 → ブラウザプレビュー構築中)
- 直近の状態 (2026-04-02 session 9):
  - 方向転換: Unity の高度な体験ループ構築 → シンプルなブラウザ鳥瞰図プレビューを優先
  - browser-preview/ 新設: C# パイプラインを JS に移植した開発プレビューツール
  - Phase A (道路網+建物) + Phase B (地形+水域) 完了。4 Generator + 建物 + 丘陵 + 川 + 海岸
  - Phase C (装飾) 未着手
  - INVARIANTS.md を実内容化 (Architecture / Visual Iteration 原則)
  - 「5分間遊んで楽しいか」が AI session 7 起源と判明。ユーザー未承認の可能性
  - spec-index: 38エントリ (done 23 / partial 10 / legacy 1 / merged 1 / todo 3)
  - 次の作業: Phase C (装飾移植) or パラメータ UI or 最終成果物像の再定義

---

## CURRENT DEVELOPMENT AXIS

- 主軸: 体験ループ構築 (SP-001 体験プロトタイプ → クエスト基盤)
- この軸を優先する理由: 3セッション整備偏重 (refresh-2026-03-19 で drift 検出)。エンジン基盤は成熟。「なぜ探索するか」の回答が最大の欠落
- 今ここで避けるべき脱線: docs 整備のみの作業ブロック、新規エンジン機能追加、コンテンツ執筆

---

## CURRENT LANE

- 主レーン: Experience Slice + Runtime Core
- 副レーン: なし (旧GameLoop整理は完了)
- 今このレーンを優先する理由: SP-001 Phase 1 後半 (Quest基盤) で体験を閉じる
- いまは深入りしないレーン: Acceptance (手動検証は Unity Editor 依存)

---

## CURRENT SLICE

- スライス名: SP-001 Phase 1 後半 (最小クエスト基盤)
- ユーザー操作列: 起動 → タイトル → PLAY → マップ生成 → WASD探索 → 建物進入 → Discovery収集 → クエスト進捗自動追跡 → Qキーでクエストログ → タイマー0 → 結果画面
- 成功状態: Phase 0 の受け入れ条件5項目 + 最低10件クエストがアクティブ + クエストログで確認可能
- このスライスで必要な基盤能力: GameSessionManager (実装済み), QuestManager/QuestData (未実装), MapEventBus連携 (既存)
- このスライスから抽出されるツール要求: クエスト定義 JSON のオーサリング支援
- 今回はやらないこと: プロシージャルクエスト生成 (Phase 3), オーディオ (SP-021), 追跡者

---

## FINAL DELIVERABLE IMAGE

- 最終成果物: 手続き型マップ探索ゲーム — プロシージャル都市を5分間探索し、建物内部で断片的テキストを発見するセッション型ゲーム
- 最終的なユーザーワークフロー: タイトル → プリセット選択 → マップ生成 → 自由探索 + クエスト → 結果 → リプレイ
- 受け入れ時の使われ方: 「5分間遊んで楽しいか」が判定基準。探索→発見→報酬の閉じたループが成立するか
- 現時点で未確定な要素:
  - 動画制作者向け Pipeline (撮影用カメラ、シーン制御、書き出し) — 要件未定義
  - 手動介入 vs 自動化の境界 — 要件未定義
  - 追跡者システムの導入判断
  - 累積報酬 / メタ進行の設計

---

## DECISION LOG

最新の決定のみ保持。古い決定は `docs/archive/decision-log-archive.md` を参照。

| 日付 | 決定事項 | 選択肢 | 決定理由 |
|------|----------|--------|----------|
| 2026-04-02 | ブラウザ鳥瞰図プレビューを開発ツールとして構築 | Unity のみ / ブラウザ追加 / ブラウザ移行 | Unity 起動が重く反復が遅い。C# パイプラインの JS 移植でブラウザ即時確認を実現 |
| 2026-04-02 | 「5分間遊んで楽しいか」はユーザー未承認の可能性を特定 | 維持 / 再定義 / 保留 | session 7 で AI が drift 診断中に定式化。project-context.md に転記されていた。ユーザー判断待ち |
| 2026-03-26 | GameState.cs 完全削除 (デッドコード) | 削除 / 簡素化維持 | collectedValue/collectedItemIds は外部から一切参照なし。DiscoveryはInteriorSessionState経由。SaveDataからもフィールド削除 |
| 2026-03-26 | SceneBootstrapper #if false ブロック完全削除 (旧GameLoopUI+PlayerHUD ~230行) | 削除 / 凍結維持 | Unity再コンパイル不要。参照先クラス (GameLoopUI/GameLoopController/PlayerHUD) は既に削除済み |
| 2026-03-23 | 旧GameLoop 11クラス完全削除 (SP-001 Phase 1) | 削除 / 凍結維持 / 段階的移行 | GameSessionManagerが完全に代替。凍結コードの保守負債を解消 |
| 2026-03-22 | 体験プロトタイプ方針D採択: 5分タイマーセッション | A:SP-001設計 / B:手動検証117 / C:探索FBループ / D:体験プロトタイプ | 3セッション整備偏重からの脱却。最小閉じた体験ループを最優先 |
| 2026-03-18 | Discoveryテキスト方針を全面変更: 空間客観描写 | 旧:ポストアポカリプス陰謀 / 新:空間環境断片 | 人間要素・固有名詞・時間軸・主観知覚を排除 |
| 2026-03-18 | 建物近接フィードバックを色変化+emission方式に決定 | A:アウトライン / B:色変化 / C:パーティクル / D:テキストのみ | shader追加不要で低コスト |
| 2026-03-11 | 操作モデルをWASD三人称に変更 + NavMesh削除 | WASD / クリック維持 / 両対応 | 探索ゲームにはWASD/スティックが自然。228秒フリーズ原因を根本除去 |
| 2026-03-08 | ジャンルを「探索・発見ゲーム」に再定義 | タクティカル / 探索・発見 / リスク管理 | コア体験は未知マップの探索 |

---

## IDEA POOL

| ID | アイデア | 状態 | 関連領域 | 再訪トリガー |
|----|----------|------|----------|--------------|
| IP-001 | 追跡者 (対処不可能な敵) | hold | core/SP-001 | Sandbox+クエストの基本体験が安定した時 |
| IP-002 | プロシージャルクエスト生成 | hold | core/SP-001 Phase 3 | Phase 2 のクエスト拡充が完了した時 |
| IP-003 | 動画制作者向け Pipeline (撮影カメラ・シーン制御) | hold | tooling | 最終成果物像の「動画制作者」要件が定義された時 |
| IP-004 | 累積報酬 / メタ進行 | hold | core/SP-020 L3 | SP-001 Phase 2 完了時 |
| IP-005 | SP-032 Ideal段階 (MacroNoiseTex・GridMode) | hold | system | MVP手動検証完了時 |
| IP-006 | MiniGame 7ファイルの処遇 (削除 vs Quest統合) | hold | core/SP-017 | SP-001 Quest基盤完成時に判断 |

---

## HANDOFF SNAPSHOT

- 現在の主レーン: Advance (ブラウザ鳥瞰図プレビュー構築)
- 現在のスライス: browser-preview Phase A+B 完了、Phase C (装飾) 未着手
- 今回変更した対象: browser-preview/ 新設 (map-gen.js, renderer.js, index.html), INVARIANTS.md 実内容化, USER_REQUEST_LEDGER.md 実内容化
- 次回最初に確認すべきファイル: browser-preview/index.html (ブラウザで開いて動作確認)
- 未確定の設計論点:
  - 「5分間遊んで楽しいか」がユーザー本来の意図か (AI session 7 起源)
  - ブラウザプレビューと Unity 版の最終的な関係
  - SP-001 Quest 基盤の優先度 (方向転換後)
- 今は触らない範囲: Unity 側の Quest 基盤、SP-032手動検証、オーディオ
- 記録債務:
  - SPEC.md §12 (旧ゲームループインターフェース) の本文が旧内容のまま → 削除済みを反映する
  - OPERATOR_WORKFLOW.md がテンプレのまま
