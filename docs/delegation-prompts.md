# MiniMapGame 委譲タスク用プロンプト

Status: archived utility examples

This file is not a resume entrypoint, not a task queue, and not current
delegation authority. The prompt bodies below are retained only as examples of
older delegation shape.

Do not hand these prompts to a new session verbatim. Regenerate any delegation
prompt from the current normal resume chain:

1. `docs/ai/AGENT_RULES.md`
2. `docs/project-context.md`
3. `docs/runtime-state.md`
4. `docs/spec-index.json`
5. The task-specific spec file referenced by `docs/spec-index.json`

If a prompt below conflicts with the current chain, the prompt is stale.
If a prompt names a task that `docs/project-context.md` lists as a non-goal or
postponed area, do not run it without a fresh explicit user request.

---

## Archived Example A: 建物内部 (Interior) 開発

```
MiniMapGame (Unity 6.3 / URP) の建物内部表示を開発してください。

### 現状
- Interior系は Assets/Scripts/Interior/ に独立実装済み
- InteriorMapGenerator: 間取り生成 (Commercial/Industrial/Residential/Special)
- InteriorRenderer: 2Dオルソグラフィック描画
- InteriorController: 建物クリック → カメラモード切替
- FloorPlanFactory + IFloorPlanGenerator: 建物カテゴリ別の間取り戦略
- InteriorDebugSpawner: テスト用の即時インテリア生成

### 改善方向
1. 間取り品質の向上 (部屋配置の自然さ、通路接続)
2. 家具/オブジェクト配置の改善 (FurnitureType enum は定義済み)
3. 描画品質 (壁・床のテクスチャ、ドアの視覚表現)
4. カテゴリ別の個性 (商業/住宅/工業で見た目が異なる)

### 制約
- `docs/ai/AGENT_RULES.md` と task-specific spec の制約に従うこと
- namespace: MiniMapGame.Interior
- 外部パッケージ追加禁止 (Unity built-ins only)
- 全生成は int seed で決定論的
- InteriorPreset は ScriptableObject

### 接続ポイント
- InteriorController.EnterInterior(MapBuilding) が外部からの呼び出し口
- CameraController.SetInteriorMode() / ResetToFollowMode() でカメラ切替
- BuildingInteraction.cs が建物クリック検出を担当
```

---

## Archived Example B: 水辺ビジュアル改善

```
MiniMapGame (Unity 6.3 / URP) の水辺レンダリングを改善してください。

### 現状
- WaterRenderer.cs: 河川 (リボンメッシュ), 海岸/湖 (ファンメッシュ)
- Water.shader: Transparent queue, depth/roughness対応, テーマ色駆動
- WaterGenerator.cs: 勾配降下方式の川流路 + 海岸4方向
- WaterTerrainInteraction.cs: 河川→地形彫り込み (carving)
- ThemeManager.cs: ApplyWater() でシェーダーパラメータ設定

### 確認済みの課題
1. Dark テーマで水面色が暗すぎる (riverColor 0.08,0.13,0.19)
   → 地面色 (0.28,0.33,0.24) との差別化が不十分
2. Grid/Mountain プリセットに水体がない (hasRiver=0, hasCoast=0)
   → 全プリセットで水辺を楽しめるようにしたい
3. Water.shader の depth 補間が正しく視覚化されているか未検証
4. waterYOffset は 0.02→0.15 に修正済み (Z-fighting対応)

### 修正方針
- Theme_Dark.asset / Theme_Parchment.asset の水色調整
- Preset_Grid.asset / Preset_Mountain.asset で hasRiver=true に変更
  (Mountain は渓流、Grid は運河が自然)
- Water.shader のdepth→色グラデーション検証

### 制約
- テーマ色はコード変更なし (SO のフィールド値調整のみ)
- プリセット変更時は WaterProfile SO との整合を確認
- WaterProfile が null のプリセットは CreateDefaultFallback() 使用
```

---

## Archived Example C: プリセット拡充・数値調整

```
MiniMapGame (Unity 6.3 / URP) のマップ生成プリセットを拡充してください。

### 現状
- 4プリセット: Coastal, Rural, Grid, Mountain
- Assets/Resources/Presets/ に ScriptableObject として配置
- Editor/MapPresetCreator.cs でメニューから新規作成可能
- GeneratorType: Organic, Grid, Mountain, Rural (IMapGenerator実装)

### MapPreset 主要フィールド
- worldWidth/worldHeight: ワールドサイズ
- generatorType: 使用するジェネレータ
- nodeCount, edgeMultiplier: グラフ密度
- buildingDensity: 建物密度 (0-1)
- hasCoast, hasRiver: 水体生成フラグ
- maxElevation: 最大標高
- hillCount, hillRadiusMin/Max: 丘陵パラメータ
- roadProfile: RoadProfile SO (道路の見た目)
- waterProfile: WaterProfile SO (水面の見た目)

### 拡充方向
1. 既存4プリセットの数値バランス調整
   - 各プリセットで F2 俯瞰ビューから見て特徴が明確に出るか
   - 建物密度・道路密度のバランス
2. 新プリセット候補 (新GeneratorTypeは不要、既存typeの組合せで)
   - Island: Organic + hasCoast全方向 + 小さいworldSize
   - Downtown: Grid + 高buildingDensity + 小maxElevation
   - Valley: Mountain + hasRiver + 狭い通路

### 制約
- 新プリセットは Resources/Presets/ に .asset として追加
- 既存コード変更は不要 (MapControlUI にボタン追加する場合は別途)
- MapPresetCreator (Editor) で作成し、Inspector で値を調整
```

---

## 使い方

新しいエージェントセッションへ渡すプロンプトは、このファイルからではなく、
現在の normal resume chain から作り直す。
必ず `docs/runtime-state.md` を含め、active lane / active slice / current
non-goals を反映する。

作業完了後の統合・commit・push は、現在の branch strategy と
`docs/ai/AGENT_RULES.md`、およびユーザーの明示指示に従う。
