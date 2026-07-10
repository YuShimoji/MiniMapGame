# 監修AIから開発AIへ渡す Development Packet

Status: supplemental template

This file is not a resume entrypoint, task queue, or current delegation
authority. It defines a reusable packet shape only. Every packet must be
regenerated from the current normal resume chain:

1. `docs/ai/AGENT_RULES.md`
2. `docs/project-context.md`
3. `docs/runtime-state.md`
4. `docs/spec-index.json`
5. The task-specific spec file referenced by `docs/spec-index.json`

If a generated packet conflicts with that chain, the packet is stale. Never
resume an old packet by replacing only its date, commit, or task title.

## 適切な作業単位

Development Packet は、長い Prompt と短い Prompt の中間を狙うものではない。
単位は **ユーザーが一つの判断を下せる成果物** とする。

| 粒度 | 例 | 扱い |
|------|----|------|
| 小さすぎる | import 追加、docs 更新、単独の色調整 | 承認済み slice 内の関連修正として同じ batch に含める |
| 適切 | 同一 seed の forest grammar 3案を比較可能にする | 一つの方向判断に必要な実装・証拠・同期が閉じる |
| 大きすぎる | observed surface 全 archetype を完成させる | 低コスト比較と方向選択を先に切り出す |

## Packet テンプレート

以下を埋めて、そのまま開発AIへ渡す。ファイル一覧や実装手順を過剰に固定せず、
成果・境界・証拠を固定する。

```text
MiniMapGame Development Packet: <短い packet 名>

現在地
- source commit: <監修時の commit>
- branch: <branch>
- active lane / slice: <project-context と一致>
- task spec: <SP 番号と path>
- 解く bottleneck: <今回の成果が直接変える一つの詰まり>

今回、判断可能にするもの
<ユーザーが何を見て、何を選べる／合否判断できるようになるかを1段落で書く>

対象範囲
- 含む: <実装、関連修正、比較、必要な正本更新>
- 含まない: <非ゴール、別の creative direction、将来拡張>
- 触らない境界: <保存互換、API、既存 SSOT など>

開発AIの裁量
- 承認なしで継続: slice 内の可逆な技術判断、発見した関連バグ修正、
  リファクタ、ローカル検証、証拠生成、正本同期
- 停止して確認: 破壊的変更、依存追加、保存・認証・API契約変更、
  正本競合、未承認の最終成果物定義、方向を変える creative judgment
- 合理的な既定値: <未指定時に採る可逆な既定値と理由>

比較と証拠
- 同条件: <seed、preset、theme、viewport など>
- 比較対象: <2〜3案。単なる強度違いではなく構造的に異なる案>
- 自動検証: <tools/validate-project.ps1 など>
- 視覚・実行証拠: <スクリーンショット、PlayMode、数値、ログ>
- 合否軸: <spec の acceptance criteria に接続する観察可能な条件>

同じ batch で閉じること
- 選んだ成果物または比較プローブ
- 発見した in-scope blocker の修正
- ローカル自動検証
- 必要な手動確認を一つの coherent gate に集約
- project-context / runtime-state / task spec / spec-index のうち、
  実際に意味が変わった正本だけを同期

完了時に返す内容
ファイルを開かなくても、何が変わり、何が判断可能になり、どの証拠で
確認し、何が未確定かが伝わる自然文にする。次の入口は異なる bottleneck を
解く2〜4案とし、各案で次に何が可能になるかを添える。
```

## Creative checkpoint

layout、visual grammar、色、font、motion、language、content expansion のように
ユーザーの感性判断が支配的な作業では、production 実装前に checkpoint を置く。

- 同じ seed / preset / viewport で 2〜3 の異なる方向を比べる。
- 推奨案を一つ示すが、推奨理由、失うもの、実装コストを併記する。
- mock、thumbnail、one-archetype probe、token sample など、捨てやすい忠実度にする。
- ユーザー選択後にだけ、選択された方向の production batch を作る。
- 選択は、明記した方向と忠実度だけを承認する。最終製品全体の承認へ拡張しない。

同じ抽象モデルが 2 回連続で NG なら、そのモデルの微調整を止める。色、半径、
opacity だけを動かす次案ではなく、入力データ、構造、評価軸のどれを変えるかを
比較してから再開する。

## 監修AI・開発AI・自動化の担当

| 担当 | 主な責任 | 担当しないこと |
|------|----------|----------------|
| 監修AI | bottleneck と判断可能な成果を定義し、高曖昧度では低コスト比較を設計する | reversible な実装詳細を一問ずつ承認すること |
| 開発AI | slice を実装し、関連修正、検証、証拠、正本同期まで閉じる | 未承認の最終製品像や感性判断を既成事実にすること |
| ユーザー | 優先順位、感性、不可逆な方向、最終受入を決める | ローカルで自動検証できる技術詳細を逐次判断すること |
| 自動化 | canonical docs の検証・公開、反復可能な smoke check を担う | 独立した status 正本を持つこと |

## Prompt を分割し直す条件

次の場合だけ、新しい Packet に分ける。

- ユーザーが判断する成果物が別になる。
- 別 spec / 別 active lane へ移る。
- 依存、契約、保存互換、最終製品像など新しい権限が必要になる。
- 比較 checkpoint で方向が選ばれ、probe から production へ fidelity が変わる。

compile error、同じ slice の軽微な UI 修正、検証、スクリーンショット、docs 同期を
理由に Packet を細切れにしない。
