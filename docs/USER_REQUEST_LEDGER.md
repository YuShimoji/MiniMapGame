# User Request Ledger

ユーザーの継続要望・差分要求・backlog を保持する台帳。

## 現在有効な要求

- ブラウザ上で確認できる鳥瞰図マップ画像が欲しい (session 9 で着手)
- 開発プレビュー用途: Unity 起動不要でマップ生成結果をブラウザ確認
- 全要素 (道路/建物/水域/地形/装飾) を描画対象とする
- シンプルな方針で差し当たり使えるクオリティの成果物を優先
- マップの見え方から逆算して構造を再構築する過程に耐える設計にする

## 未反映の是正要求

- 「5分間遊んで楽しいか」は AI が session 7 drift 診断で定式化した表現。ユーザー本来の意図か要確認。refresh-2026-03-19.md → project-context.md FINAL DELIVERABLE IMAGE に転記されている
- project-context.md の FINAL DELIVERABLE IMAGE の受け入れ基準はユーザー未承認のまま

## Backlog Delta

- Phase C: DecorationPlacer 移植 (木・街灯・岩・花の地形適応配置)
- パラメータ UI: Preset のスライダー調整 UI (ビジュアル反復の高速化)
- canonical docs 残り2本 (USER_REQUEST_LEDGER / OPERATOR_WORKFLOW) の実内容化
- SP-001 Phase 1 後半 (Quest基盤) — ブラウザ方向転換により優先度要再確認

## 今後明文化すべきこと

- ブラウザプレビューと Unity 版の関係 (開発ツール / 独立成果物 / Unity の代替)
- 最終成果物像の再定義 (「5分セッション型ゲーム」はユーザー未承認)

## 運用ルール

- 会話で一度出た要求のうち、次回以降も効くものをここへ残す
- 単なる感想ではなく、仕様・設計・backlog に効くものを優先する
