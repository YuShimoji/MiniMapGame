# Interaction Notes
# 報告UI・手動確認・質問形式に関する project-local メモ。

## 手動確認の出し方
- 手動確認項目は本文で提示する。
- AskUserQuestion では `OK / NG番号` だけを聞く。
- 手動確認依頼と次アクション選択を同じ質問に混ぜない。

## 禁止パターン
- AskUserQuestion の `question` に Markdown テーブルを入れる
- 選択肢を commit / しない の yes/no で埋める
- 既知文脈を「詳細を教えてください」で再質問する

## ユーザーが嫌う形式
- [例] 進路選択を狭める二択
- [例] 読みにくい崩れた表

## 報告メモ
- [例] BLOCK SUMMARY では先に原因分析を示す
