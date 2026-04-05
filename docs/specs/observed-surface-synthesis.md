# SP-041: Observed Surface Synthesis

**Status**: partial  
**Category**: system  
**上位参照元**: SP-040 (マップビジュアルディレクション), SP-032 (地表合成レンダリングパイプライン)  
**対象**: browser-preview / Unity runtime 共通の「上空観測密度」表現

---

## 1. Executive Summary

本仕様は、`SP-040` の **hybrid orthophoto relief** をさらに一段進め、
「航空写真の模写」ではなく **上空観測で読み取れる土地の痕跡密度** を再構成するための方針を定義する。

目標は以下である。

- 裏山が「ただの緑の塊」ではなく、**林冠密度・斜面・抜け・尾根筋・谷筋**を持つ塊として読める
- 鉄道・車庫・広い人工地形が、単なる灰色面ではなく、**用途のありそうな人工的広がり**として読める
- 川や川沿いが、単なる青い線ではなく、**河道・土手・湿り・河原・合流形状**を伴って読める
- 住宅地が、単体建物アイコンの集合ではなく、**屋根群・隙間・敷地断片・付属物**の密度として読める

この仕様は、画像生成的なピクセル描写を採らない。  
代わりに **Field / Grammar / Glyph** の 3 層で「観測密度」を作る。

---

## 2. Why Relief Alone Is Not Enough

現状の SP-040 / SP-032 系は、主に以下を得意とする。

- 地勢の把握
- 道路・水面・建物の clean edge
- 地表の連続感

しかしユーザーが求めているのは、そこに加えて以下の情報である。

- 土地利用の違い
- 植生密度の違い
- 人工物の雑然さ / 反復 / 敷地の切れ方
- 上空から見たときの「何かありそう」な気配

つまり不足しているのは「高度」ではなく **痕跡密度** である。

Google マップ風の擬似標高可視化が目標外である理由もここにある。
あの方向は高低差の理解には寄与するが、森林の厚み、鉄道敷や車庫の用途感、住宅密集の異様さといった
**観測された土地の手触り**までは表現しない。

---

## 3. Core Model: Field / Grammar / Glyph

### 3.1 Field

**Field** は連続場である。  
「どこが高いか」だけでなく、「どこが湿っているか」「どこが樹冠で覆われるか」「どこが人工面化しているか」を持つ。

例:

- `ElevationField`
- `SlopeField`
- `MoistureField`
- `CanopyDensityField`
- `ImperviousSurfaceField`
- `ParcelFragmentationField`
- `RoofDensityField`
- `CorridorAnisotropyField`

### 3.2 Grammar

**Grammar** は土地のまとまり方である。  
forest / river / rail yard / detached housing / warehouse block のような
**地表アーキタイプごとの振る舞いルール**を定義する。

Grammar は Field を参照して「この領域はどういう密度で埋まるべきか」を決める。

### 3.3 Glyph

**Glyph** は情報の単位である。  
ここでいう glyph は文字ではなく、上空から観測される小さな痕跡のこと。

例:

- 小さな林冠クラスタ
- 平行な線路群
- 屋根 ridge の短線
- 物置 / carport / service shed の小矩形
- 河岸の砂州帯
- 敷地の余白 / 駐車枠 / 資材置き場

重要なのは、glyph を「一個ずつリアルに描く」ことではなく、
**grammar に従って密度と反復を制御すること**である。

---

## 4. Target Output

最終的な画面は、以下の 3 種が重なったものとして扱う。

1. **Relief layer**  
   地勢・標高・陰影・等高線

2. **Observed surface layer**  
   植生・人工面・敷地断片・河川帯・屋根群などの面としての密度

3. **Hard edge layer**  
   道路 / 水面 / 建物 / 主要構造物の clean edge

SP-032 は主に 1 を担う。  
SP-041 は 2 を定義する。  
既存 geometry / shader 群は 3 を担う。

---

## 5. Surface Archetypes

### 5.1 Backhill Forest

**狙い**: 裏山を「森が生えている丘」ではなく、**林冠の厚みを持つ斜面**として読む

構成:

- canopy mass: 大小の林冠塊
- trunk gap / clearing: 密度の薄い抜け
- ridge light / valley cool: 尾根と谷で色相と密度を変える
- footpath scar: 細い踏み分け / 管理路 / 斜面の切れ
- edge roughness: 林縁の不規則さ

禁止:

- 木アイコンを大量散布して森を表現すること
- 一様な緑ノイズで済ませること

必要な表現:

- 林冠は「粒」ではなく **mass with holes**
- 斜面方向に沿った密度の伸び
- 樹種差を示す色群の揺らぎ

### 5.2 Rail / Depot / Broad Artificial Yard

**狙い**: 鉄道や車庫や資材置き場を、**平行性・反復・広い人工面**として読む

構成:

- ballast ribbons: 線路下の帯
- siding bundles: 複線 / 留置線 / 車庫列
- service pads: コンクリ面 / 砕石面 / 保守区画
- long shed roofs: 細長い屋根反復
- parking / container / stockpile glyphs
- fence / retaining edge / drainage traces

禁止:

- 灰色の大きな矩形だけで済ませること
- 線路一本を引いて終わること

必要な表現:

- **parallel repetition**
- **用途感のある余白**
- **長さ方向に異方性を持つ面**

### 5.3 Riparian Corridor

**狙い**: 川や川沿いを、**線ではなく帯**として読む

構成:

- main water ribbon
- shallow shelf / bank shadow
- wet edge vegetation
- gravel / sand bar
- floodplain tint
- confluence wedge rule

禁止:

- 合流部を丸い節点で処理すること
- 川幅と河岸帯の情報を同一線に圧縮すること

必要な表現:

- river core / bank / outer wet belt の 3 層
- 合流部では丸ではなく **楔形・舌状** に膨らむ
- 川沿いの建物・道路・植生が流れ方向に反応する

### 5.4 Detached Housing Patch

**狙い**: 家屋の形状を、単体記号ではなく **屋根密度と敷地断片の暴力**として読む

構成:

- roof cluster: 主屋 + 増築 + carport + sheds
- parcel gaps: 細い路地 / 隣棟間隔 / 庭の余白
- orientation drift: 屋根方向の微妙なズレ
- service clutter: 小屋、物干し、駐車、細長い舗装帯
- neighborhood rhythm: 同系屋根の並びと破れ

禁止:

- 単純な box footprint を一個ずつ置いて住宅地らしさを出そうとすること
- 住宅地を「均一密度の四角」で塗ること

必要な表現:

- 主屋一棟ごとではなく **roof swarm**
- 住宅地の「密だけど完全整列ではない」感じ
- 付属物込みの敷地感

---

## 6. Rendering Strategy

### 6.1 What Is Procedural, What Is Explicit

| 層 | 手法 | 例 |
|----|------|----|
| soft field | texture / field bake | canopy density, imperviousness, wetness |
| structured patch | polygon / region grammar | rail yard area, residential patch, riparian belt |
| repeated marks | glyph batch / stamp atlas | roof ridges, sidings, crowns, sheds |
| hard edge | explicit geometry | roads, water mesh, building silhouette |

### 6.2 Preferred Synthesis

優先するのは **field-driven stamp synthesis** である。

意味:

- まず field を作る
- 次に grammar が field を読んで patch を決める
- 最後に glyph/stamp が patch 内に配列される

これは image generation ではなく、
**規則で密度を生成する設計**である。

### 6.3 Why Not Pure Texture Noise

ノイズだけでは以下が作れない。

- 住宅地の parcel 感
- 車庫の平行反復
- 河岸の意味のある広がり
- 森林の抜けと塊の対比

したがって、soft texture だけでは不十分であり、
grammar と glyph が必須になる。

---

## 7. Data / Runtime Design

### 7.1 New Conceptual Data

追加候補:

- `ObservedSurfaceArchetype`
- `ObservedSurfacePatch`
- `ObservedSurfacePatchSet`
- `CanopyDensityField`
- `ImperviousSurfaceField`
- `RoofClusterField`
- `LinearArtifactSet`

### 7.2 New Runtime Responsibilities

追加候補:

- `ObservedSurfaceSynthesizer`
  - preset / terrain / roads / buildings / water から patch を決定
- `ObservedSurfaceBaker`
  - field + patch から stamp atlas / detail mask を焼く
- `ObservedSurfaceRenderer`
  - browser-preview では Canvas 2D
  - Unity では material bind + optional decal/glyph batch

### 7.3 Relationship to Existing Systems

- `GroundSemanticMaskBaker`
  - 既存の 2xRGBA8 は relief 用として維持
  - observed surface を無理に同居させない

- `DecorationPlacer`
  - tree / shrub / fence の単体配置は残す
  - ただし「森の見え方」を担当させない

- `BuildingPlacer`
  - footprint は hard edge の正本
  - observed surface 側では roof cluster / parcel clutter の密度源に使う

---

## 8. Browser Preview Strategy

browser-preview では以下を行う。

### 8.1 Phase A — 試行済み・不採用 (browser-preview)

4段階の試行を行ったが、いずれも不採用。

**試行と不採用理由:**

1. **shader直訳** (pixel-by-pixel): sRGB/リニア空間不一致。Canvas 2D に乗算/fog がない
2. **レイヤー分離** (個別α合成): 薄い情報を重ねても密度にならない
3. **glyph散布** (ellipse/rect乱数配置): field/grammarなしの散布 = 地理的意味のないノイズ
4. **surface classification** (enum + density scalar色lerp): 1セル1ラベル1スカラー → 内部構造が消失。256x172セルのImageData操作では多スケール・異方性を原理的に表現不能

**共通の失敗原因:** 全てが「ピクセル単位で色を計算する」ImageData操作に閉じている。SP-041が定義するField/Grammar/Glyphは一度も正しく実装されていない。

**方向転換:** ImageData ピクセル操作 → Canvas 2D パス描画 API (Path2D, clip, createPattern, globalCompositeOperation multiply/overlay) による patch/glyph 構造描画に転換する。

残存ファイル: `browser-preview/observed-surface.js` (参考用に残すが成功物として扱わない)

### 8.2 Phase B

以下を比較可能にする。

- forest density on/off
- roof swarm on/off
- riparian belt on/off
- industrial yard grammar on/off

目的は、ユーザーが「どの層が効いているか」を見分けられるようにすること。

### 8.3 Phase C

良かった構成だけを Unity に持ち帰る。  
browser-preview を肥大化させて最終レンダラ化しない。

---

## 9. Unity Runtime Strategy

Unity 側では次の順で導入する。

1. relief は現行 SP-032 を維持
2. observed surface 用の追加 mask / atlas を導入
3. forest / riparian / yard / roof-clutter の順で追加
4. hard edge geometry は既存 renderer を維持

重要:

- 3D geometry を無秩序に増やして詳細さを作らない
- details はなるべく projected / baked / batched に寄せる
- 本当に立てるべきものだけ explicit mesh にする

---

## 10. Non-Goals

以下は目標外。

- 航空写真の直接模写
- 1 棟 1 棟にフォトリアルな屋根材差分を入れること
- 木 1 本 1 本を視覚主役にすること
- Google Maps 的な高度色分けを主役にすること
- SSAO や高価な screen-space effect で情報量不足を補うこと

---

## 11. Acceptance Criteria

以下を満たしたとき、SP-041 の方向性は成功とみなす。

1. 裏山が「森の塊」として読め、林冠密度の違いが見える
2. 鉄道・車庫・資材置き場が、用途のありそうな人工地形として読める
3. 川が線ではなく帯として読め、合流部が丸い節ではなく地形的につながる
4. 住宅地が box の並びではなく、屋根群と敷地断片の密度として読める
5. relief / observed surface / hard edge の役割分担が崩れない
6. 画像生成に頼らず、seed 決定論的に再現できる

---

## 12. Next Actions

1. browser-preview に `ObservedSurfaceSynthesizer2D` 相当の層を追加する
2. `ObservedSurfaceArchetype` の最小セットを定義する
3. forest / river / housing の 3 archetype から先に検証する
4. rail yard は industrial/public corridor の生成が安定してから追加する

---

## 13. Naming Guidance

会話や実装で使う推奨語:

- **observed surface synthesis**
- **field / grammar / glyph**
- **roof swarm**
- **riparian belt**
- **canopy mass**
- **industrial yard patch**

避けたい語:

- 「もっとリアルにする」
- 「航空写真っぽくする」
- 「テクスチャを足す」

これらは方向を曖昧にし、再び延長線上の改善へ戻る原因になる。
