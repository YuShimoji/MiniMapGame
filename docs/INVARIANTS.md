# Invariants
# 破ってはいけない条件・責務境界・UX不変量を保持する正本。

## Systemic Diagnosis
- [例] 重要なのは個々の症状でなく、全体の傾向。

## UX / Algorithmic Invariants
- [例] スキップ時は本文表示完了後に次バブルへ進む。
- [例] モードごとに状態が分裂しない。

## Responsibility Boundaries
- [例] 音声合成は YMM4 側の責務。
- [例] `.ymmp` 直接編集では正常な音声読み上げを保証できない。

## Prohibited Interpretations / Shortcuts
- [例] rejected を「工程不要」と解釈しない。
- [例] ユーザー未指定の固有名詞・方式を勝手に採用しない。

## 運用ルール
- ユーザーが一度説明した非交渉条件は、同一ブロック内でここへ固定する。
- `project-context.md` の DECISION LOG には理由を短く残し、ここには条件そのものを残す。
