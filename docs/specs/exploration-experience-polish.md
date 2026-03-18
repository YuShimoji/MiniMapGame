# SP-020: 探索体験ポリッシュ

status: partial
pct: 30
cat: ui

## 概要

探索ゲームとしての「楽しさ」を構成する2層の体験改善を定義する。

- **Layer 1**: 発見物テキスト断片 — 収集の「手応え」を作る
- **Layer 2**: マップ上探索可視化 — 「次どこ行こう」の判断材料を作る

### 前提

- SP-060/061/062 の Interior Interaction System が完成済み
- Discovery 4種 (Document/Note/Photo/Container) + 鍵-ドア1:1紐づけ
- BuildingExplorationRecord による永続探索記録
- InteriorFeedbackUI (Toast通知 + フロアインジケーター)
- 7プリセット, 5建物カテゴリ, 17商業細分類, 34部屋タイプ

---

## Layer 1: 発見物テキスト断片システム

### 1.1 目的

Discovery収集時に短いテキストを表示し、収集行為に「意味」を与える。
現状の「Collected: Document」だけでは手応えがない。

### 1.2 データ構造

```
DiscoveryTextPool (ScriptableObject)
├── entries: List<DiscoveryTextEntry>
│   ├── furnitureType: FurnitureType
│   ├── buildingCategory: BuildingCategory (optional filter)
│   ├── shopSubtype: ShopSubtype (optional filter)
│   ├── textEN: string (英語, 1-2行, 最大80文字)
│   ├── textJA: string (日本語, 1-2行, 最大40文字)
│   ├── rarity: DiscoveryRarity (Common / Uncommon / Rare)
│   └── tag: string (optional, 将来のコレクション分類用)

DiscoveryTextSettings (ScriptableObject, singleton)
├── locale: string ("en" / "ja")  ※ゲーム設定で切替
└── pool: DiscoveryTextPool
```

### 1.2.1 テキストデータ管理

- テキストデータは JSON ファイル (`Resources/DiscoveryTexts/discovery-texts.json`) で管理
- Editor スクリプトで JSON → SO への変換をサポート
- LLM一括生成 → JSON → レビュー → SO のパイプライン
- 両言語フィールドを持つが、MVP では片方が空でも動作する (フォールバック: EN → JA → "...")

### 1.3 DiscoveryRarity

| レアリティ | 出現率 | 視覚的差異 |
|-----------|--------|-----------|
| Common | 70% | 通常Toast |
| Uncommon | 25% | Toast枠色変化 (青) |
| Rare | 5% | Toast枠色変化 (金) + 表示時間延長 (4秒) |

### 1.4 テキスト選択ロジック

```
SelectDiscoveryText(furnitureType, buildingCategory, shopSubtype, rng):
  1. furnitureType + buildingCategory + shopSubtype で完全一致するエントリを抽出
  2. 該当なければ furnitureType + buildingCategory で抽出
  3. 該当なければ furnitureType のみで抽出
  4. rarity の重み付きランダムで1件選択
  5. 選択されたテキストを返す
```

- SeededRng を使い、同一seedで同一結果を再現可能にする
- 同一建物内で重複テキストを避ける（セッション中の使用済みリストで管理）

### 1.5 テキスト表示UI

- Toast通知を拡張: 「Collected: Document」→「古い帳簿。この街の商人組合の記録らしい」
- Rare発見時は専用の表示演出（色変化 + 表示延長）
- Tabメニュー(ExplorationMenuUI)に収集済みテキスト一覧タブを追加

### 1.6 テキスト設計方針

テキスト内容の設計方針は **[Discovery Text 設計方針](discovery-text-policy.md)** に分離。

要点:

- 空間・環境の客観描写のみ。人間要素・固有名詞・時間軸・主観知覚を排除
- カテゴリ別に空間の物理的質が変わる (Residential=狭い/柔らかい, Industrial=広大/金属的 等)
- レアリティ = 描写の解像度 (Common=1文, Rare=2-3文)
- furnitureType はコード上の分類のみ。テキスト内容には反映しない
- 文体: グラック/マルケス的な精密で淡々とした散文

### 1.7 初期テキスト数

MVP: カテゴリ別 × FurnitureType別 × レアリティ別 = 5 × 4 × 3 = 60エントリ
- Common: 各カテゴリ × 各Type に 2-3件 → 約40件
- Uncommon: 各カテゴリに 2-3件 → 約15件
- Rare: 全体で 5件

### 1.8 テキスト生成パイプライン

1. カテゴリ別 × レアリティ別に執筆 (日本語先行)
2. 英語対訳を作成
3. JSON形式で出力 (`discovery-texts.json`)
4. **[Discovery Text 設計方針](discovery-text-policy.md)** の禁止パターン一覧でレビュー
5. Editor スクリプトで JSON → DiscoveryTextPool SO に変換
6. 必要に応じて追加・差し替え

### 1.9 実装スコープ

- DiscoveryTextPool SO + DiscoveryTextEntry データクラス
- DiscoveryTextSelector (選択ロジック)
- InteriorFeedbackUI のToast拡張 (レアリティ色分け + テキスト表示)
- ExplorationMenuUI に収集テキスト一覧タブ
- テキストデータ: JSON or SO で管理

---

## Layer 2: マップ上探索可視化

### 2.1 目的

外部マップを歩いている時に、探索の進捗が視覚的にわかるようにする。
「次にどの建物に行くべきか」の判断材料を提供する。

### 2.2 建物マーカーシステム

#### 2.2.1 マーカー状態

| 状態 | 条件 | 表示 |
|------|------|------|
| Unknown | 未訪問 | マーカーなし (建物メッシュのみ) |
| Discovered | 近接ハイライト発動済み | 小さい「?」マーカー (白) |
| Entered | hasEntered == true | カテゴリアイコン (灰) |
| InProgress | hasEntered && !IsComplete | カテゴリアイコン (橙) + 進捗バー |
| Complete | IsComplete == true | カテゴリアイコン (緑) + チェック |

#### 2.2.2 カテゴリアイコン

| カテゴリ | アイコン記号 | 色調 |
|---------|------------|------|
| Residential | 家マーク | 暖色 |
| Commercial | 袋/コインマーク | 青系 |
| Industrial | 歯車マーク | 灰系 |
| Public | 柱マーク | 白系 |
| Special | 星マーク | 金系 |

### 2.3 マーカー表示の実装

- 建物の上部にワールドスペースUIとして配置
- カメラ距離に応じた表示制御:
  - 近距離 (< 30m): フルアイコン + 進捗バー
  - 中距離 (30-80m): アイコンのみ
  - 遠距離 (> 80m): 非表示
- LabelController の既存インフラを活用

### 2.4 ミニマップ連携

- MiniMapController にマーカー情報を反映
- 訪問済み建物: ドット色変化
- 完全探索: ドット色を緑に
- InProgress: ドットを橙に

### 2.5 探索ヒントシステム (Optional)

- 近接ハイライト時に、建物のカテゴリとフロア数を表示
  - 例: 「[E] Commercial - 3 Floors」
- 訪問済みなら進捗も表示
  - 例: 「[E] Commercial - 2/3 Floors, 4/7 Items」

### 2.6 実装スコープ

- BuildingMarkerUI コンポーネント (建物上部のワールドスペースUI)
- BuildingMarkerManager (マーカー状態管理 + ExplorationProgressManager連携)
- MiniMapController マーカー色変化
- BuildingInteraction のハイライトテキスト拡張 (カテゴリ + フロア数)

---

## Layer 3: 探索報酬システム (将来)

本仕様では設計方針のみ記載。実装はLayer 1+2の検証後。

### 3.1 方向性候補

- **累積value → アンロック**: 一定ptでプリセット/テーマ/特殊建物を解放
- **Rareコレクション**: Rare発見物をコレクションとして表示
- **初回ボーナス**: 建物初入場時に追加value
- **探索ランク**: 総探索率に応じたランク表示

### 3.2 決定条件

Layer 1+2の手動検証で以下を観察してから決定する:
- テキスト断片が「もっと探したい」動機を生むか
- マーカーが「未踏建物へ行く」動機を生むか
- 現在の value 5-20pt の粒度が適切か

---

## 受け入れ条件

### Layer 1

1. Discovery収集時にテキスト断片が表示される
2. テキストは建物カテゴリ + FurnitureType に応じて変化する
3. Rare発見時に視覚的に区別できる
4. 同一建物内で同じテキストが重複しない
5. Tabメニューで収集済みテキスト一覧が確認できる

### Layer 2

1. 訪問済み建物にカテゴリアイコンが表示される
2. 探索中 (InProgress) と完全探索 (Complete) が色で区別できる
3. ミニマップにマーカー色が反映される
4. 近接ハイライト時にカテゴリとフロア数が表示される
5. カメラ距離に応じてマーカー表示が制御される

---

## やらないこと (Out of Scope)

- Layer 3 (報酬システム) の実装
- テキストの多言語対応
- テキストのストーリー連鎖（テキスト間の因果関係）
- サウンド演出 (SP-021の領域)
- エフェクト演出（パーティクル等）
- 追跡者/危険要素 (保留中の別仕様)
