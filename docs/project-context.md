# Project Context

## PROJECT CONTEXT

- プロジェクト名: MiniMapGame
- 環境: Unity 6.3 LTS (6000.3.6f1) / C# / URP 17.3.0 / InputSystem 1.18.0
- ブランチ戦略: trunk-based (master のみ)
- 現フェーズ: α (地形/道路/水面/Interior 実装完了 → 体験ループ構築中)
- 直近の状態 (2026-04-05 session 10):
  - SP-040 (マップビジュアルディレクション) 新規作成: 北極星(hybrid orthophoto relief)、五原則、5層アーキテクチャ、モバイル予算、完了条件
  - SP-001 Phase 2 完了: プリセット別フィルタ実装 (allowedPresets + MapManager参照)、クエスト30件に拡充 (20件universal + 10件preset-specific)
  - SP-040 → SP-032 上位参照リンク確立、SPEC.md/CLAUDE.md/spec-index 同期
  - Unity Editor 手動検証未実施
  - spec-index: 39エントリ (done 23 / partial 10 / deprecated 1 / legacy 1 / merged 2 / todo 3)
  - 次の作業: Unity PlayMode検証 (SP-001 Phase 2 + SP-032 Slice 5 + SP-060/061/062)

---

## CURRENT DEVELOPMENT AXIS

- 主軸: 体験ループ構築 (SP-001 体験プロトタイプ → クエスト基盤)
- この軸を優先する理由: 3セッション整備偏重 (refresh-2026-03-19 で drift 検出)。エンジン基盤は成熟。「なぜ探索するか」の回答が最大の欠落
- 今ここで避けるべき脱線: docs 整備のみの作業ブロック、新規エンジン機能追加、コンテンツ執筆

---

## CURRENT LANE

- 主レーン: Experience Slice + Runtime Core
- 副レーン: なし
- 今このレーンを優先する理由: SP-001 Phase 1 実装完了。Phase 2 (クエスト拡充+HUD) で体験深化
- いまは深入りしないレーン: Acceptance (手動検証は Unity Editor 依存)

---

## CURRENT SLICE

- スライス名: Unity PlayMode 検証ゲート
- ユーザー操作列: Unity Bootstrap → マップ生成 → WASD探索 → 建物進入 → クエスト進捗確認 → SP-032地表検証 → Interior統合検証
- 成功状態: SP-001 Phase 2 / SP-032 Slice 5 / SP-060/061/062 の統合検証完了
- このスライスで必要な基盤能力: 全て実装済み。検証のみ
- このスライスから抽出されるツール要求: なし (Unity Editor 手動操作)
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
| 2026-03-26 | MiniGame 7ファイル完全削除 | 削除 / Quest統合 | フローから完全に切断済み。InteriorRenderer/InteriorControllerにminiGameManagerフィールドも存在せず不整合。1186行根絶 |
| 2026-03-26 | Quest基盤をJSON+MapEventBus方式で実装 | JSON定義+EventBus購読 / SO定義 / Procedural | Phase 1 MVP。手書き10件で最小検証。JsonUtility互換性。Phase 2でプリセット別フィルタ追加予定 |
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
| IP-006 | ~~MiniGame 7ファイルの処遇~~ | resolved | core/SP-017 | 削除済み (session 9) |

---

## HANDOFF SNAPSHOT

- 現在の主レーン: Experience Slice + Runtime Core
- 現在のスライス: Unity PlayMode 検証ゲート
- 今回変更した対象: SP-040新規作成(430行), プリセット別フィルタ実装(QuestData.allowedPresets+QuestManager.mapManager), クエスト30件に拡充(+10件), SP-032逆リンク追加, SPEC.md/CLAUDE.md/spec-index/runtime-state同期
- 次回最初に確認すべきファイル: Unity Editor PlayMode (BootstrapTestScene)
- 未確定の設計論点: 最終成果物像 (ゲーム単体 vs 動画制作Pipeline)、SP-040北極星用語の最終承認
- 今は触らない範囲: 木漏れ日/建物陰影の実装(方向性メモ止まり)、オーディオ、パフォーマンス最適化
- 記録債務: なし
