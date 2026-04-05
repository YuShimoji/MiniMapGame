# Project Context

## PROJECT CONTEXT

- プロジェクト名: MiniMapGame
- 環境: Unity 6.3 LTS (6000.3.6f1) / C# / URP 17.3.0 / InputSystem 1.18.0
- ブランチ戦略: trunk-based (master のみ)
- 現フェーズ: α (主要機能は存在するが、マップの最終ビジュアル品質は未成立)
- 直近の状態 (2026-04-06 session 11 corrective sync):
  - SP-040 は北極星として維持するが、browser-preview 上の実装は最終形に届いていない
  - browser-preview では 4 段階の試行を行ったが、いずれも「これまでの延長線上」に留まり不合格
  - relief の強化、glyph 散布、surface classification のいずれも、航空写真的な観測密度の再構成には不十分
  - 現在の browser-preview 出力は「方向性の候補」ではなく「根本的な見直しが必要な失敗例」として扱う
  - Unity Editor 手動検証は依然未実施
  - spec-index: 40エントリ (done 23 / partial 11 / deprecated 1 / legacy 1 / merged 2 / todo 3)
  - 次の作業: 実装継続ではなく、可視化方針の再定義と責務分離の是正

---

## CURRENT DEVELOPMENT AXIS

- 主軸: マップ可視化モデルの再査定 (SP-040 主導、SP-041 再定義中)
- この軸を優先する理由: いま問題なのは調整不足ではなく描画モデル自体の誤読であり、このまま実装を積んでも最終形へ近づかないため
- 今ここで避けるべき脱線: Quest拡張、新規ゲーム機能、既存 browser-preview 実装の延長調整、Google Maps 的な標高可視化への回帰

---

## CURRENT LANE

- 主レーン: Rendering Model Reassessment
- 副レーン: なし
- 今このレーンを優先する理由: relief / glyph scatter / surface classification の各案がすべて「最終形から逆算した描画方式」になっておらず、ユーザー要求と実装の間に構造的なズレがあるため
- いまは深入りしないレーン: Experience Slice (Quest等)、Runtime Core、Acceptance。ただし文脈修正後に visual 方針が固まり次第 Unity 側の確認に戻る

---

## CURRENT SLICE

- スライス名: SP-041 patch/glyph 構造転換
- 前スライス結果: corrective reframing 完了。4段階の試行 (shader直訳 / レイヤー分離 / glyph散布 / surface classification) は全て不採用として固定済み
- 不採用理由: 全てが「ピクセル単位で色を計算する」ImageData 操作に閉じており、航空写真的な多スケール・異方性・内部構造を原理的に表現できない
- 次の方向: ImageData ピクセル操作 → Canvas 2D パス描画 API (Path2D, clip, createPattern, globalCompositeOperation multiply/overlay) による patch/glyph 構造描画
- 成功状態:
  - browser-preview 上でパッチ境界が地理的に意味ある形を持つ
  - グリフ密度が均一塗りと質的に異なる出力になる
  - relief / observed surface / hard edge の3層が干渉せず読める
  - 4 preset x 2 theme で「地図として読める」出力になる
- やらないこと: ImageData ベースのピクセル単位色計算、既存 surface classification の微調整、Quest拡張、Unity への先行移植

---

## FINAL DELIVERABLE IMAGE

- 最終成果物: 手続き型マップ探索ゲーム — プロシージャル都市を5分間探索し、建物内部で断片的テキストを発見するセッション型ゲーム
- 最終的なユーザーワークフロー: タイトル → プリセット選択 → マップ生成 → 自由探索 + クエスト → 結果 → リプレイ
- 受け入れ時の使われ方: 「5分間遊んで楽しいか」が判定基準。探索→発見→報酬の閉じたループが成立するか
- ビジュアル品質確立の優先順位: browser-preview で「最終形に近づく描画モデルか」を見極める → 有効な構造だけを Unity shader / atlas / patch に反映 → Unity PlayMode で 3D 固有問題を検証。browser-preview は高速検証器であり SSOT ではない
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
| 2026-04-06 | ImageData ピクセル操作による observed surface 描画は不採用。Canvas 2D パス描画 API による patch/glyph 方式に転換 | ImageData 微調整 / Canvas 2D パス描画転換 / WebGL 導入 | 4段階の試行が全て「セル単位で色を決める」に帰着し、多スケール・異方性・内部構造を持つ航空写真的密度を原理的に表現できなかった。SP-041 の Field/Grammar/Glyph を Canvas 2D パス API で正しく実装する |
| 2026-04-06 | corrective note: 現行 browser-preview 出力は不合格として扱い、延長調整路線を停止 | 現行実装を磨く / 不合格として文脈修正 / browser-preview 自体を破棄 | ユーザー評価として「このクオリティでは使えない」が明示された。問題は調整不足ではなく描画モデルの勘違いであり、まず正本を直さないと再び同じ方向へ drift する |
| 2026-04-06 | shader 直訳 / glyph 散布 / surface classification の 3 路線はいずれも現時点では採用しない | 直訳継続 / 散布継続 / classification継続 / 一旦不採用化 | いずれも最終形から逆算したレンダリングモデルになっておらず、「航空写真に近いもの」の再現に届かないため |
| 2026-04-06 | ビジュアル品質確立をbrowser-preview (Canvas 2D) 優先に決定 | A:Unity PlayMode先行 / B:browser-preview先行 / C:WebGLビルド | Unity上の表示品質に目処が立たない。ブラウザで即時確認+PNG回収可能。色・合成の品質を先に確立してからUnityに移植する |
| 2026-04-06 | 開発主軸をVisual Quality (browser-preview品質確立) に転換 | Quest続行 / ビジュアル品質 / Unity PlayMode検証 | 前セッションでQuest実装にdrift。ユーザー指摘により本来のグラフィック修正軸に復帰。まずブラウザ上で地図可読性を実現する |
| 2026-04-06 | corrective note: browser-preview は最終品質ゲートではなく方向確認用プローブと再定義 | browser-preview をそのまま合格基準にする / 高速プローブとして使う / Unity先行に戻す | 2D直訳が延長線上の改善に閉じ、最終成果物との接続を失ったため。browser-preview は可読性・レイヤ設計の確認に限定し、Unityで最終判断する |
| 2026-04-06 | 観測密度表現は「Field / Grammar / Glyph」方式で進める | reliefの延長で押し切る / フォト模写に寄せる / Field / Grammar / Glyph | 裏山・河川帯・人工広場・住宅密集は標高可視化では足りない。画像生成に逃げず、土地の痕跡密度を決定論的に合成するため |
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
| IP-007 | observed surface archetype 拡張 (forest / rail yard / housing patch) | hold | system/SP-041 | 描画モデルの再設計が済み、「延長線上ではない」と判断できた時 |

---

## HANDOFF SNAPSHOT

- 現在の主レーン: Rendering Model Reassessment
- 現在のスライス: SP-041 patch/glyph 構造転換
- 前スライス結果: corrective reframing 完了。4段階の ImageData ベース試行は全て不採用
- 次の作業: Canvas 2D パス描画 API (Path2D, clip, globalCompositeOperation) による patch/glyph 構造描画の実装
- 次回最初に確認すべきファイル: `docs/project-context.md`, `docs/specs/observed-surface-synthesis.md`, `browser-preview/observed-surface.js`
- 詳細引き継ぎ: `docs/session-context-2026-04-06.md`
- 未確定の設計論点: patch extraction のアルゴリズム選定、archetype 別 glyph 配置ルールの詳細設計
- 今は触らない範囲: Quest拡張、Unity PlayMode検証、オーディオ、ImageData ベースの微調整
- 記録債務: なし
