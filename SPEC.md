# MiniMapGame — Technical Specification

> Version: 1.0.0-draft
> Date: 2026-03-07
> Reference: map-generator-v3.jsx (React/Canvas prototype, 793 lines)

---

## 1. ゲーム概要

ミニマリスト抽象タクティカルゲーム。手続き型で生成される都市マップ上をプレイヤーが移動し、
建物に侵入（インテリアマップ生成）、遭遇イベント、価値物回収、脱出判断を行う。

**コアループ**:
1. マップ生成（シード指定） → 都市構造の探索
2. ランドマーク建物に接近 → インテリアマップ自動生成
3. チョークポイントで遭遇イベント発火
4. 行き止まりに価値物配置 → 回収判断
5. 出口ノードで脱出判断

---

## 2. プリセット定義

リファレンス実装から移植する4プリセット:

| ID | 名前 | Generator | 幹線数 | 環状道路 | 曲率 | 建物密度 | 海岸 | 河川 | 丘陵 |
|----|------|-----------|--------|----------|------|----------|------|------|------|
| `coastal` | 港湾都市 | Organic | 6-8 | ✓ | 0.50 | 0.80 | ✓ | ✓ | 0.30 |
| `rural` | 田舎町 | Rural | 3-4 | ✗ | 0.70 | 0.25 | ✗ | ✓ | 0.75 |
| `grid` | NYCグリッド | Grid | 5-7 | ✗ | 0.06 | 0.95 | ✗ | ✗ | 0.00 |
| `mountain` | 山道 | Mountain | 2-3 | ✗ | 0.88 | 0.18 | ✗ | ✗ | 0.95 |

### MapPreset (ScriptableObject)

```csharp
namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewMapPreset", menuName = "MiniMapGame/MapPreset")]
    public class MapPreset : ScriptableObject
    {
        public string displayName;          // "港湾都市"
        public GeneratorType generatorType; // enum: Organic, Grid, Mountain, Rural
        public Vector2Int arterialRange;    // min-max arterial count (e.g. 6,8)
        public bool hasRingRoad;
        [Range(0f, 1f)] public float curveAmount;
        [Range(0f, 1f)] public float buildingDensity;
        public bool hasCoast;
        public bool hasRiver;
        [Range(0f, 1f)] public float hillDensity;
        [TextArea] public string description;

        // World-space scaling (JSX pixel coords → Unity world units)
        public float worldWidth  = 860f;   // CW equivalent
        public float worldHeight = 580f;   // CH equivalent
        public float borderPadding = 50f;  // cpt() margin
    }
}
```

---

## 3. データ構造

全データクラスは `namespace MiniMapGame.Data` に配置。
MonoBehaviour依存なし。`[System.Serializable]`必須。

### 3.1 MapNode

```csharp
[System.Serializable]
public struct MapNode
{
    public Vector2 position;   // ワールド座標 (X, Z in Unity 3D)
    public int degree;         // 接続エッジ数 (0で初期化、エッジ追加時にインクリメント)
    public string label;       // "市中心", "門1", "" etc.
    public NodeType type;      // enum: None, Hub, Gate, Shelter, Farm
}

public enum NodeType { None, Hub, Gate, Shelter, Farm }
```

**JSX対応**: `{...p, degree:0, label, type}` → position は `cpt()` でクランプ済み

### 3.2 MapEdge

```csharp
[System.Serializable]
public struct MapEdge
{
    public int nodeA;          // nodes配列のインデックス
    public int nodeB;          // nodes配列のインデックス
    public int tier;           // 0=幹線, 1=街路, 2=路地
    public Vector2 controlPoint; // ベジェ制御点 (quadratic bezier)
}
```

**JSX対応**: `{a, b, tier, ctrl:{x,y}}`
- tier 0: 幹線道路（最も太い、中央破線あり）
- tier 1: 街路（中間幅）
- tier 2: 路地（最も細い）

### 3.3 MapBuilding

```csharp
[System.Serializable]
public struct MapBuilding
{
    public Vector2 position;   // ワールド座標 (X, Z)
    public float width;
    public float height;       // building depth (not Y-axis height)
    public float angle;        // ラジアン。道路方向に沿った回転
    public int tier;           // 配置されたエッジのtier
    public bool isLandmark;    // true=ランドマーク（インテリア生成対象）
    public string id;          // "B0", "B1", ... ユニークID（シーン検索用）
}
```

**JSX対応**: `{x, y, w, h, angle, tier, landmark, id}`
- ランドマーク: 5%確率 && tier==0 のエッジ沿い
- ランドマークは通常建物の1.9倍サイズ

### 3.4 MapTerrain

```csharp
[System.Serializable]
public class MapTerrain
{
    public List<Vector2> coastPoints;  // 海岸線ポリゴン頂点
    public List<Vector2> riverPoints;  // 河川中心線
    public List<HillData> hills;       // 丘陵/等高線
}

[System.Serializable]
public struct HillData
{
    public Vector2 position;
    public float radiusX;     // 楕円半径X
    public float radiusY;     // 楕円半径Y
    public float angle;       // 楕円回転
    public int layers;        // 等高線数 (2-4)
}
```

### 3.5 MapAnalysis

```csharp
[System.Serializable]
public class MapAnalysis
{
    public List<int> deadEndIndices;       // degree==1 のノードインデックス
    public List<int> intersectionIndices;  // degree>=3
    public List<int> plazaIndices;         // degree>=4
    public List<int> chokeEdgeIndices;     // tier<=1 && 両端degree<=2 && 距離>32
}
```

**JSX対応**: `analyze()` 関数 → `MapAnalyzer.Analyze(nodes, edges)`

### 3.6 MapData (統合コンテナ)

```csharp
[System.Serializable]
public class MapData
{
    public List<MapNode> nodes;
    public List<MapEdge> edges;
    public List<MapBuilding> buildings;
    public MapTerrain terrain;
    public MapAnalysis analysis;
    public Vector2 center;    // マップ中心点 (cx, cy)
    public int seed;          // 生成に使用されたシード
}
```

---

## 4. SeededRng (決定論的乱数)

```csharp
namespace MiniMapGame.Core
{
    /// <summary>
    /// XOR-shift PRNG. JSX mkRng() の完全移植。
    /// 同一seedで同一シーケンスを保証。
    /// </summary>
    public class SeededRng
    {
        private uint state;

        public SeededRng(int seed)
        {
            state = (uint)seed;
            if (state == 0) state = 1; // 0は固定点なので回避
        }

        /// <summary>0.0 ~ 1.0 の浮動小数を返す</summary>
        public float Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state / (float)0x100000000UL;
        }

        /// <summary>min (inclusive) ~ max (exclusive) の整数を返す</summary>
        public int Range(int min, int max)
        {
            return min + (int)(Next() * (max - min));
        }

        /// <summary>min ~ max の浮動小数を返す</summary>
        public float Range(float min, float max)
        {
            return min + Next() * (max - min);
        }
    }
}
```

**JSXとの対応**:
```javascript
// JSX
let s = seed >>> 0;
return () => { s ^= s<<13; s ^= s>>17; s ^= s<<5; return (s>>>0)/0x100000000; };
```

---

## 5. マップジェネレータ

### 5.1 IMapGenerator

```csharp
namespace MiniMapGame.MapGen
{
    public interface IMapGenerator
    {
        /// <summary>
        /// ノードとエッジのネットワークを生成する。
        /// buildingsやterrainは含まない（後段で生成）。
        /// </summary>
        (List<MapNode> nodes, List<MapEdge> edges) Generate(
            SeededRng rng,
            Vector2 center,
            MapPreset preset
        );
    }
}
```

### 5.2 共通ヘルパー (MapGenUtils)

```csharp
public static class MapGenUtils
{
    /// <summary>ノード追加。座標をプリセット境界内にクランプ。</summary>
    public static int AddNode(List<MapNode> nodes, float x, float y,
        string label = "", NodeType type = NodeType.None, MapPreset preset = null);

    /// <summary>エッジ追加。制御点をランダムオフセットで生成。degree更新。</summary>
    public static void AddEdge(List<MapNode> nodes, List<MapEdge> edges,
        int a, int b, int tier, SeededRng rng, float curveAmount = 0.5f);

    /// <summary>2点間距離</summary>
    public static float Distance(Vector2 a, Vector2 b);

    /// <summary>正規化方向ベクトル</summary>
    public static Vector2 Direction(Vector2 from, Vector2 to);

    /// <summary>法線（垂直ベクトル）</summary>
    public static Vector2 Perpendicular(Vector2 dir);

    /// <summary>二次ベジェ曲線上の点</summary>
    public static Vector2 BezierPoint(Vector2 a, Vector2 control, Vector2 b, float t);

    /// <summary>座標をプリセット境界にクランプ (JSX cpt() 相当)</summary>
    public static Vector2 ClampToPreset(Vector2 p, MapPreset preset);
}
```

### 5.3 OrganicGenerator

**JSX対応**: `buildOrganic()`

アルゴリズム:
1. 中心ノード (Hub) を配置
2. N本の幹線道路を放射状に生成
   - 各幹線: 3-5ステップ、角度をランダム偏向しながら延伸
   - 近接ノード(距離<20)へのスナップ機能
   - 終端ノードに「門N」ラベル
3. 環状道路 (ring==true): 幹線終端をatan2ソートし隣接接続
4. 二次道路: tier 0エッジから分岐
   - 分岐点をエッジ中間に配置
   - 2-5ステップの枝道 (tier 1)
   - さらにランダムで三次道路 (tier 2) を分岐

```
中心(Hub)
  ├── 幹線0 ──→ 門1 ──┐
  ├── 幹線1 ──→ 門2 ──┤ (Ring Road)
  ├── ...             │
  │   └── 街路 ── 路地  │
  └── 幹線N ──→ 門N ──┘
```

### 5.4 GridGenerator

**JSX対応**: `buildGrid()`

アルゴリズム:
1. 等間隔グリッド配置 (spacing = 42+rng*10)
2. 水平エッジ: 中央行・端行=tier 0, 4列間隔=tier 1, 残り=tier 2
3. 垂直エッジ: 中央列・端列=tier 0, 4行間隔=tier 1, 残り=tier 2
4. 斜め大通り (Broadway風): 左上→右下方向にtier 0エッジを配置

```
┌──────────────────────┐
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─│  tier 2 横
│ ═══════════════════  │  tier 0 横 (中央行)
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─│
│ ║ │ │ ║ │ │ ║ │ │ ║ │  ║=tier0, │=tier2
│    ╲                 │  斜め大通り
└──────────────────────┘
```

### 5.5 MountainGenerator

**JSX対応**: `buildMountain()`

アルゴリズム:
1. 蛇行する背骨道路を上→下に生成
   - x座標をランダム偏向 (`curveAmount`制御)
   - y座標を38-66単位ずつ増加
   - 先頭=「登山口」、末尾=「山頂」
2. 背骨の各ノードから40%確率で枝分かれ
   - tier 1 の行き止まり枝
   - 60%確率でさらにtier 2枝（「避難小屋」ラベル可能性）

```
  登山口
    │
    ╲
     │──── 枝 (避難小屋)
    ╱
    │
     ╲──── 枝
    │
  山頂
```

### 5.6 RuralGenerator

**JSX対応**: `buildRural()`

アルゴリズム:
1. 中心ノード (Hub=「村」)
2. N本の道路を放射状に延伸
   - 前半ステップ=tier 0、後半=tier 1
   - 終端に「農場」ラベル（60%確率）
3. 各ステップで75%超の確率で横枝 (tier 2)
   - 短い行き止まり

```
        農場
       ╱
  農場── 村 ──農場
       ╲
        農場
```

---

## 6. SpatialHash (衝突検出)

```csharp
namespace MiniMapGame.Core
{
    /// <summary>
    /// 2D空間ハッシュ。建物配置時のOBB重複検出に使用。
    /// JSX SpatialHash クラスの移植。
    /// </summary>
    public class SpatialHash<T> where T : ISpatialBounds
    {
        private readonly float cellSize;
        private readonly Dictionary<long, List<T>> cells;

        public SpatialHash(float cellSize = 40f);

        /// <summary>AABB境界を回転角度を考慮して計算</summary>
        public Rect GetBounds(T item);

        /// <summary>アイテムを登録</summary>
        public void Insert(T item);

        /// <summary>重複チェック (AABB近似)</summary>
        public bool Overlaps(T item);

        /// <summary>全クリア</summary>
        public void Clear();
    }

    public interface ISpatialBounds
    {
        Vector2 Position { get; }
        float Width { get; }
        float Height { get; }
        float Angle { get; }
    }
}
```

**衝突検出アルゴリズム** (JSX `_bounds()` + `overlaps()` 移植):
1. 回転を考慮したAABB拡張: `hw = (w*cos + h*sin)/2 + 3`, `hh = (w*sin + h*cos)/2 + 3`
2. セルキー = `(floor(x/cs), floor(y/cs))` → long化
3. 対象セル範囲を列挙し、全候補とAABB交差テスト

---

## 7. BuildingPlacer

```csharp
namespace MiniMapGame.Core
{
    public static class BuildingPlacer
    {
        /// <summary>
        /// 全エッジに沿って建物を配置。
        /// SpatialHashで衝突を回避しながら挿入。
        /// </summary>
        public static List<MapBuilding> Place(
            List<MapNode> nodes,
            List<MapEdge> edges,
            SeededRng rng,
            MapPreset preset
        );
    }
}
```

**配置アルゴリズム** (JSX `placeBuildings()` 移植):

1. エッジごとに処理:
   - 距離 < 15 のエッジはスキップ
   - 方向ベクトル `dir` と法線 `pd` を計算
2. 両側 (side = -1, +1) に対して:
   - `baseOff = (半幅 + 3) * side` で道路からのオフセット
   - `spacing = 7 + rng * 8` で建物間隔
   - ベジェ曲線上の `t` 位置で配置座標を算出
3. 各候補:
   - `rng > buildingDensity` → スキップ
   - ランドマーク判定: `rng > 0.95 && tier == 0`
   - SpatialHashで重複チェック → 重複なしなら挿入

**道路幅テーブル** (JSX `tierW`, `tierBW`):

| Tier | 道路半幅 [min,max] | 建物幅 [min,max] |
|------|-------------------|-----------------|
| 0 (幹線) | 12, 8 | 16, 12 |
| 1 (街路) | 8, 5 | 11, 8 |
| 2 (路地) | 5, 3 | 7, 5 |

---

## 8. MapAnalyzer

```csharp
namespace MiniMapGame.Core
{
    public static class MapAnalyzer
    {
        /// <summary>
        /// ノード/エッジのグラフ構造を分析。
        /// JSX analyze() 関数の移植。
        /// </summary>
        public static MapAnalysis Analyze(List<MapNode> nodes, List<MapEdge> edges);
    }
}
```

**分析ルール**:
- **Dead End**: `node.degree == 1` → 価値物配置候補 (IValueObject)
- **Intersection**: `node.degree >= 3` → 交差点マーカー
- **Plaza**: `node.degree >= 4` → 広場（IValueObject高価値候補）
- **Choke Edge**: `tier <= 1 && 両端degree <= 2 && distance > 32` → 遭遇イベント候補 (IEncounterTrigger)

---

## 9. 地形生成

MapPresetの設定に基づき、`TerrainGenerator` が `MapTerrain` を生成。
JSX `genTerrain()` の移植。

### 9.1 海岸線
- `hasCoast == true` の場合のみ
- マップ右端に沿って不規則なポリゴンを生成
- 起点: `(worldWidth * (0.62 + rng * 0.1), 0)`
- 右端→下端→上向きにウェーブポイント

### 9.2 河川
- `hasRiver == true` の場合のみ
- マップ上端から下端まで蛇行するスプライン
- x偏向量: rural=35, それ以外=55

### 9.3 丘陵
- `hillDensity` に比例して 0〜16 個の楕円を生成
- 各楕円: position, radiusX(35-115), radiusY(22-72), angle, layers(2-4)

---

## 10. Unity シーンアーキテクチャ

### 10.1 MapManager (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    /// <summary>
    /// マップ生成のオーケストレータ。MapDataを所有しイベントを公開。
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        [Header("Configuration")]
        public MapPreset activePreset;
        public int seed;

        [Header("References")]
        public MapRenderer mapRenderer;
        public BuildingSpawner buildingSpawner;

        // Read-only access
        public MapData CurrentMap { get; private set; }

        // Events
        public event System.Action<MapData> OnMapGenerated;
        public event System.Action OnMapCleared;

        public void Generate();          // activePreset + seedでフル生成
        public void Generate(int seed);  // シード指定
        public void Clear();             // マップクリア
    }
}
```

**生成フロー**:
1. `SeededRng rng = new(seed)`
2. center計算: `(worldWidth * (0.30 + rng * 0.22), worldHeight * (0.32 + rng * 0.30))`
3. `IMapGenerator.Generate(rng, center, preset)` → nodes, edges
4. `BuildingPlacer.Place(nodes, edges, rng, preset)` → buildings
5. `TerrainGenerator.Generate(rng, center, preset)` → terrain
6. `MapAnalyzer.Analyze(nodes, edges)` → analysis
7. `MapData` に集約
8. `OnMapGenerated` イベント発火
9. `MapRenderer.Render(mapData)`, `BuildingSpawner.Spawn(mapData)` 呼び出し

### 10.2 MapRenderer (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    /// <summary>
    /// 道路ネットワークをLineRendererで描画。
    /// 3段階の幅でtierを表現。
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Road Width by Tier")]
        public float[] outerWidths = { 14f, 9f, 5f };  // ケーシング幅
        public float[] innerWidths = { 9f, 5.5f, 2.8f }; // 内部幅

        [Header("Materials")]
        public Material roadOuterMaterial;
        public Material roadInnerMaterial;

        public void Render(MapData data);
        public void Clear();
    }
}
```

**レンダリング手法**:
- エッジごとに2つのLineRenderer (outer casing + inner fill)
- ベジェ曲線を10-20セグメントに分割して頂点列化
- ノード交差点は半径に応じたスプライト表示
- tier 0エッジには中央破線 (dash) 用LineRenderer追加

### 10.3 BuildingSpawner (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    public class BuildingSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject normalBuildingPrefab;
        public GameObject landmarkBuildingPrefab;

        public void Spawn(MapData data);
        public void Clear();
    }
}
```

**スポーンロジック**:
- `MapBuilding` ごとにプレハブをインスタンス化
- position → `new Vector3(position.x, 0, position.y)` (Y=0, XZ平面)
- rotation → `Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0)`
- scale → `new Vector3(width, buildingHeight, height)` (heightは固定 or tier依存)
- isLandmark → landmarkBuildingPrefab使用、BuildingInteractionコンポーネント追加

### 10.4 BuildingInteraction (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    /// <summary>
    /// ランドマーク建物クリック時にインテリアシーンをロード。
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        public string buildingId;    // "B42" etc.
        // BuildingPreset は将来のインテリア種別定義用
        // public BuildingPreset interiorPreset;

        public void OnClick();  // → InteriorMapGenerator.Generate()
    }
}
```

---

## 11. インテリアマップシステム

### InteriorMapGenerator

```csharp
namespace MiniMapGame.Interior
{
    public static class InteriorMapGenerator
    {
        /// <summary>
        /// 建物IDからシードを生成し、部屋・廊下構造を返す。
        /// </summary>
        public static InteriorMapData Generate(int seed, BuildingPreset preset);
    }

    [System.Serializable]
    public class InteriorMapData
    {
        public List<RoomNode> rooms;        // ノード = 部屋
        public List<CorridorEdge> corridors; // エッジ = 廊下
        public List<int> alcoveIndices;      // degree==1 の部屋 = 隠し小部屋
    }

    [System.Serializable]
    public struct RoomNode
    {
        public Vector2 position;
        public Vector2 size;
        public RoomType type;  // enum: Normal, Entrance, Boss, Treasure, Alcove
    }

    [System.Serializable]
    public struct CorridorEdge
    {
        public int roomA;
        public int roomB;
        public float width;
    }

    public enum RoomType { Normal, Entrance, Boss, Treasure, Alcove }
}
```

**生成フロー**:
1. `seed = building.id.GetHashCode()` (e.g. "B42" → int)
2. BSP分割 or ランダムウォークで部屋配置
3. 最小全域木 + α で廊下接続
4. `degree == 1` の部屋 → Alcove(隠し小部屋)
5. Additive SceneとしてUnityにロード

---

## 12. ゲームループインターフェース

全てインターフェーススタブのみ。ロジック実装は後続フェーズ。

```csharp
namespace MiniMapGame.GameLoop
{
    /// <summary>チョークフラグ付きMapEdge進入時に発火</summary>
    public interface IEncounterTrigger
    {
        void OnEncounter(MapEdge chokeEdge, MapData context);
    }

    /// <summary>行き止まりノードに配置される価値物</summary>
    public interface IValueObject
    {
        string ObjectId { get; }
        int Value { get; }
        void OnCollect();
    }

    /// <summary>出口ノード到達時の脱出判断</summary>
    public interface IExtractDecision
    {
        bool ShouldExtract(MapData context, int collectedValue);
        void OnExtract();
        void OnContinue();
    }

    /// <summary>
    /// ScriptableObject チャンネルによるイベントバス。
    /// MonoBehaviour間の疎結合通信。
    /// </summary>
    public interface IMapEventBus
    {
        void Publish<T>(T eventData);
        void Subscribe<T>(System.Action<T> handler);
        void Unsubscribe<T>(System.Action<T> handler);
    }
}
```

---

## 13. 座標系変換

JSXプロトタイプは2D Canvas座標 (0,0=左上、右下方向正)。
Unity 3Dではワールド座標XZ平面を使用。

| | JSX (Canvas) | Unity (World) |
|---|---|---|
| 水平 | x: 0 → CW (860) | x: 0 → worldWidth |
| 垂直 | y: 0 (上) → CH (580) (下) | z: worldHeight (北) → 0 (南) |
| 高さ | なし | y: 固定 (0 = 地面) |

**変換式**:
```csharp
Vector3 ToWorldPosition(Vector2 jsxCoord, MapPreset preset)
{
    return new Vector3(
        jsxCoord.x,                          // X → X
        0f,                                   // 地面高さ
        preset.worldHeight - jsxCoord.y       // Y反転 → Z
    );
}
```

---

## 14. ファイル一覧と実装順序

| # | ファイル | パス | 依存 |
|---|---------|------|------|
| 1 | `NodeType.cs` | Data/ | なし |
| 2 | `GeneratorType.cs` | Data/ | なし |
| 3 | `MapNode.cs` | Data/ | NodeType |
| 4 | `MapEdge.cs` | Data/ | なし |
| 5 | `MapBuilding.cs` | Data/ | ISpatialBounds |
| 6 | `HillData.cs` | Data/ | なし |
| 7 | `MapTerrain.cs` | Data/ | HillData |
| 8 | `MapAnalysis.cs` | Data/ | なし |
| 9 | `MapData.cs` | Data/ | 1-8 |
| 10 | `MapPreset.cs` | Data/ | GeneratorType |
| 11 | `SeededRng.cs` | Core/ | なし |
| 12 | `ISpatialBounds.cs` | Core/ | なし |
| 13 | `SpatialHash.cs` | Core/ | ISpatialBounds |
| 14 | `MapGenUtils.cs` | Core/ | Data types |
| 15 | `IMapGenerator.cs` | MapGen/ | Data types, SeededRng |
| 16 | `OrganicGenerator.cs` | MapGen/ | IMapGenerator, MapGenUtils |
| 17 | `GridGenerator.cs` | MapGen/ | IMapGenerator, MapGenUtils |
| 18 | `MountainGenerator.cs` | MapGen/ | IMapGenerator, MapGenUtils |
| 19 | `RuralGenerator.cs` | MapGen/ | IMapGenerator, MapGenUtils |
| 20 | `TerrainGenerator.cs` | Core/ | Data types, SeededRng |
| 21 | `BuildingPlacer.cs` | Core/ | SpatialHash, MapGenUtils |
| 22 | `MapAnalyzer.cs` | Core/ | Data types |
| 23 | `MapManager.cs` | Runtime/ | 全Core + MapGen |
| 24 | `MapRenderer.cs` | Runtime/ | MapData |
| 25 | `BuildingSpawner.cs` | Runtime/ | MapData |
| 26 | `InteriorMapGenerator.cs` | Interior/ | SeededRng |
| 27 | `BuildingInteraction.cs` | Runtime/ | InteriorMapGenerator |
| 28-31 | Interface stubs | GameLoop/ | Data types |

---

## 15. 既存コードの扱い

| ファイル | 判断 | 理由 |
|----------|------|------|
| PlayerMovement.cs | **リファクタ** | Orthographic依存 → Perspective対応。NavMeshは維持。 |
| CameraController.cs | **維持** | 既にPerspective対応済み。微調整のみ。 |
| InteractionPointController.cs | **吸収→削除** | BuildingInteraction に機能統合。 |
| WorldPositionTrackerUI.cs | **維持** | 汎用UI追従。変更不要。 |
| LabelController.cs | **リファクタ** | orthographicSize → カメラ距離ベースに変更。 |

---

## 16. フェーズ計画

### Phase A: データ基盤 (Day 1-2)
- #1-14: 全データ構造 + SeededRng + SpatialHash + ユーティリティ

### Phase B: ジェネレータ (Day 3-4)
- #15-22: 4種ジェネレータ + 地形 + 建物配置 + 分析

### Phase C: Unity統合 (Day 5-7)
- #23-25: MapManager + Renderer + Spawner
- 既存コードリファクタ (PlayerMovement, LabelController)

### Phase D: インテリア + ゲームループ (Week 2)
- #26-31: InteriorMapGenerator + BuildingInteraction + Interface stubs

### Phase E: ビジュアル + ポリッシュ (Week 3-4)
- テーマシステム移植
- シェーダー/マテリアル調整
- UI統合

---

## 17. 設計決定事項 (2026-03-07 確定)

1. **ナビゲーション方式**: **NavMesh動的ベイク** — マップ生成後にNavMeshSurface.BuildNavMesh()。既存PlayerMovement(NavMeshAgent)をそのまま活用。
2. **カメラ視点**: **自由軌道カメラ維持** — 現CameraController(Perspective orbit/pan/zoom/follow)をそのまま使用。
3. **スケール**: **1:1** — JSX 1px = Unity 1unit。マップは860x580ユニット。
4. **建物の3D表現**: **シンプルボックス(Cube)** → 将来的にモジュラープレハブ化。鳥瞰図なのでプリミティブ組み合わせで十分。
5. **インテリア生成**: **BSP分割** — 現在の実装を維持。部屋=ノード、廊下=エッジ、行き止まり=隠し小部屋。

### 次のマイルストーン: Phase E (Unity統合テスト)
- NavMeshSurface動的ベイク実装
- MapPresetアセット4種作成 (coastal, rural, grid, mountain)
- ビルディングプレハブ (Cube) 作成
- エディタ上で生成→可視化→プレイヤー歩行テスト
