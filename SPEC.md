# MiniMapGame — Technical Specification

> Version: 3.1.0
> Date: 2026-03-13
> Reference: map-generator-v3.jsx (React/Canvas prototype, 793 lines)

---

## 1. ゲーム概要

手続き型マップ探索ゲーム。プロシージャル生成される都市・地形マップ上をプレイヤーが自由に移動し、
建物内部の探索や発見物の収集を行う。

**コア体験**: 未知のマップを歩き回り、地形・建物・隠し要素を発見すること。

**現行コアループ**:
1. マップ生成（シード指定 or ランダム） → 都市構造の自由探索
2. ランドマーク建物に接近 → インテリアマップ自動生成 → 内部探索
3. 発見物の収集（配置ロジックは再設計予定）

> **凍結中の機能** (コード残存・開発対象外):
> - エンカウントシステム（EncounterZone: チョークポイント自動ダメージ）
> - 脱出判断システム（ExtractionPoint）
> - HPシステム（PlayerStats）
> - 将来候補: 追跡者（対処不可能な敵）による探索の緊張感

---

## 2. プリセット定義

ランタイム基準となる 7 プリセットを `Assets/Resources/Presets/` に配置する。
`MapControlUI` / `SceneBootstrapper` の既定ボタンは現状 4 種
(`Coastal` / `Rural` / `Grid` / `Mountain`) のままだが、
追加 3 種 (`Island` / `Downtown` / `Valley`) は asset-ready 状態で、
Inspector 手動割当または将来の UI 拡張で利用する。

| ID | 名前 | Generator | 幹線数 | 環状道路 | 曲率 | 建物密度 | 海岸 | 河川 | 丘陵 | ワールド |
|----|------|-----------|--------|----------|------|----------|------|------|------|----------|
| `coastal` | Coastal | Organic | 7-8 | ✓ | 0.62 | 0.68 | ✓ | ✓ | 0.38 | 860x580 |
| `rural` | Rural | Rural | 4-5 | ✗ | 0.58 | 0.20 | ✗ | ✓ | 0.55 | 860x580 |
| `grid` | Grid | Grid | 5-7* | ✗ | 0.02 | 0.82 | ✗ | ✓ | 0.02 | 860x580 |
| `mountain` | Mountain | Mountain | 2-3* | ✗ | 0.94 | 0.08 | ✗ | ✓ | 1.00 | 860x580 |
| `island` | Island | Organic | 4-6 | ✓ | 0.67 | 0.52 | ✓ | ✗ | 0.22 | 620x620 |
| `downtown` | Downtown | Grid | 6-8* | ✗ | 0.01 | 0.98 | ✗ | ✗ | 0.01 | 720x520 |
| `valley` | Valley | Mountain | 2-3* | ✗ | 0.74 | 0.07 | ✗ | ✓ | 1.00 | 640x720 |

\* `arterialRange` は `GridGenerator` / `MountainGenerator` では参照されず、
設計意図の保持と将来 UI 表示用の値として asset に残している。

### Generator-specific preset notes

- `hasRingRoad` が実際に効くのは `OrganicGenerator` のみ。
- `arterialRange` が実際に効くのは `OrganicGenerator` と `RuralGenerator`。
- `Island` は asset-only の「島風 preset」。現行 `WaterGenerator` は
  1 辺 coastline 前提のため、真の全周 coastline ではなく
  shoreline-dominant compact map として定義する。

### MapPreset (ScriptableObject)

```csharp
namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewMapPreset", menuName = "MiniMapGame/MapPreset")]
    public class MapPreset : ScriptableObject
    {
        public string displayName;
        public GeneratorType generatorType; // enum: Organic, Grid, Mountain, Rural
        public Vector2Int arterialRange;
        public bool hasRingRoad;
        [Range(0f, 1f)] public float curveAmount;
        [Range(0f, 1f)] public float buildingDensity;
        public bool hasCoast;
        public bool hasRiver;
        [Range(0f, 1f)] public float hillDensity;
        [TextArea] public string description;

        public float worldWidth = 860f;
        public float worldHeight = 580f;
        public float borderPadding = 50f;

        [Header("Roads")]
        public RoadProfile roadProfile;

        [Header("Water")]
        public WaterProfile waterProfile;

        [Header("Decoration")]
        [Range(0f, 1f)] public float decorationDensity = 0.5f;

        [Header("Interior")]
        public MiniMapGame.Interior.InteriorPreset defaultInteriorPreset;

        [Header("Elevation")]
        public float maxElevation = 15f;
        public float elevationScale = 1f;
        [Range(0f, 1f)] public float steepnessBias = 0.5f;
        public bool enableBridges = true;
        public bool enableTunnels = false;
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
    public float elevation;    // 高度 (ElevationMap.ApplyToNodesで設定)
}

public enum NodeType { None, Hub, Gate, Shelter, Farm }
```

### 3.2 MapEdge

```csharp
[System.Serializable]
public struct MapEdge
{
    public int nodeA;          // nodes配列のインデックス
    public int nodeB;          // nodes配列のインデックス
    public int tier;           // 0=幹線, 1=街路, 2=路地
    public Vector2 controlPoint; // ベジェ制御点 (quadratic bezier)
    public int layer;          // 0=地上, 1=橋, -1=トンネル (BridgeTunnelDetectorで設定)
}
```

- tier 0: 幹線道路（最も太い、中央破線あり）
- tier 1: 街路（中間幅）
- tier 2: 路地（最も細い）

### 3.3 MapBuilding

```csharp
[System.Serializable]
public struct MapBuilding : ISpatialBounds
{
    public Vector2 position;   // ワールド座標 (X, Z)
    public float width;
    public float height;       // building depth (not Y-axis height)
    public float angle;        // ラジアン。道路方向に沿った回転
    public int tier;           // 配置されたエッジのtier
    public bool isLandmark;    // true=ランドマーク（インテリア生成対象）
    public int floors;         // 階数 (tier別レンジ: tier0=3-8, tier1=2-5, tier2=1-3)
    public int shapeType;      // 0=Box, 1=L-shape, 2=Cylinder, 3=Stepped
    public string id;          // "B0", "B1", ... ユニークID（シーン検索用）
}
```

- ランドマーク: 5%確率 && tier==0 のエッジ沿い
- ランドマークは通常建物の1.9倍サイズ
- 建物Y高さ: floors × floorHeight (1.2f)

### 3.4 MapTerrain

```csharp
[System.Serializable]
public class MapTerrain
{
    public List<WaterBodyData> waterBodies;  // 水体データ（河川・海岸等）
    public List<HillData> hills;             // 丘陵/等高線
    public List<HillCluster> hillClusters;   // 丘陵クラスタ
    public int coastSide = -1;               // 海岸方向: 0=右, 1=下, 2=左, 3=上, -1=なし
}

[System.Serializable]
public struct HillData
{
    public Vector2 position;
    public float radiusX;     // 楕円半径X
    public float radiusY;     // 楕円半径Y
    public float angle;       // 楕円回転
    public int layers;        // 等高線数 (2-4)
    public SlopeProfile profile;  // 断面形状 (Gaussian/Steep/Gentle/Plateau/Mesa)
    public int clusterId;     // 所属クラスタID (-1 = 独立)
}

public enum SlopeProfile
{
    Gaussian,   // 標準ベルカーブ: Exp(-distSq * 1.5)
    Steep,      // 急峻: Exp(-distSq * 3.0)
    Gentle,     // なだらか: Exp(-distSq * 0.7)
    Plateau,    // 平頂+急斜面
    Mesa        // 平頂+垂直壁
}

[System.Serializable]
public struct HillCluster
{
    public int id;
    public ClusterType type;
    public Vector2 center;
    public float orientationAngle;
    public SlopeProfile dominantProfile;
}

public enum ClusterType
{
    Ridge,          // 直線状の尾根 (3-6丘)
    MoundGroup,     // 円形丘群 (3-5丘)
    ValleyFramer,   // 平行2尾根で谷を形成
    Solitary        // 独立した単独丘
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
    public List<MapDecoration> decorations;  // 装飾オブジェクト
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

**道路幅テーブル** (JSX `tierW`, `tierBW` 由来、セットバック距離として使用):

| Tier | 道路半幅 [max,min] | 建物幅 [max,min] |
|------|-------------------|-----------------|
| 0 (幹線) | 12, 8 | 16, 12 |
| 1 (街路) | 8, 5 | 11, 8 |
| 2 (路地) | 5, 3 | 7, 5 |

**RoadProfile連携**: `preset.roadProfile` が存在する場合、`effectiveHw = max(上記半幅, profile.TotalWidth*0.5)` で下限を保証。通常プロファイルではレイアウト不変、極端に広い道路でのみ自動拡大。

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

TerrainGenerator（丘陵）+ WaterGenerator（河川・海岸）で MapTerrain を生成。
WaterTerrainInteraction で水体が地形をカービング。ElevationMap が最終標高を統合管理。

### 9.1 海岸線 (WaterGenerator)
- `hasCoast == true` の場合のみ
- WaterGenerator.DetermineCoastSide で4方向ランダム決定 (rng消費1回)
- WaterProfile.CoastConfig 駆動: inlandReach / coastlineRoughness / depthBase
- 各方向専用の生成メソッド (GenerateCoastRight/Bottom/Left/Top)

### 9.2 河川 (W-1: メタ地理ベース勾配降下)
- `hasRiver == true` の場合のみ
- **源流決定**: ElevationMapを8×8グリッドサンプリング → 上位3高標高候補から1点選択。海岸ポリゴン内除外
- **勾配降下歩行**: 各ステップで中心差分勾配 (delta=10) を計算、降下方向に沿って前進
- **安定化**: momentum保持 / 最大旋回角45° / ループ検出(20u) / 平坦地はmomentum維持
- **終端**: マップ端到達 or 海岸ポリゴン進入 (最低4ステップガード)
- **蛇行**: meanderFrequency駆動のsin波 + ジッター（進行方向の垂直成分として適用）
- **W-5自動調整**: デフォルトWaterProfile時、GeneratorType別にfreq/sway乗算 (Rural 0.6/0.65, Mountain 1.6/0.4, Grid 0.2/0.3)

### 9.3 丘陵 (クラスタベース生成)

`hillDensity` に基づきクラスタを生成し、各クラスタから複数の丘を展開。

**クラスタ生成**: `hillDensity * (3 + rng*4)` → 2-7クラスタ。クラスタ間最低60ユニット間隔。

| ClusterType | 丘数 | 配置パターン |
|-------------|------|------------|
| Ridge | 3-6 | 直線上、中央が最も高い、楕円長軸が尾根方向に整列 |
| MoundGroup | 3-5 | 円形、中央丘が最大（周囲の60-80%半径） |
| ValleyFramer | 4-8 (2列) | 平行尾根、間隔60-100ユニット、Steep寄り |
| Solitary | 1 | 独立配置 |

**SlopeProfile**: 各丘に断面プロファイルを割り当て、ElevationMap.ComputeFalloff で使用:
- Gaussian: 標準 `Exp(-distSq * 1.5)`
- Steep: 急峻 `Exp(-distSq * 3.0)`
- Gentle: なだらか `Exp(-distSq * 0.7)`
- Plateau: `distSq < 0.09` で平頂、以降急降下
- Mesa: `distSq < 0.16` で平頂、以降垂直壁

**配置制約**: 海岸側回避 + 道路ノードから最低30ユニット離す (3回ナッジ)

**MapPreset.steepnessBias** (0-1): Steep/Mesa系プロファイルの出現確率を制御

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

**生成フロー** (18ステップ):

1. `SeededRng rng = new(seed)` → center計算
2. `IMapGenerator.Generate(rng, center, preset)` → nodes, edges
3. `WaterGenerator.DetermineCoastSide` → coastSide
4. `TerrainGenerator.Generate(rng, center, preset, coastSide, nodes)` → terrain (hills only)
5. `ElevationMap` 生成 (from terrain hills)
6. `WaterGenerator.Generate` → terrain.waterBodies (勾配降下川流路 + プリセット別蛇行)
7. `WaterTerrainInteraction.ApplyWaterCarving` → ElevationMapへcarving適用
8. `ElevationMap.ApplyToNodes` → 最終地形形状を反映
9. `BuildingPlacer.Place` → buildings
10. `MapAnalyzer.Analyze` → analysis
11. `BridgeTunnelDetector.Detect` → edge.layer (waterBodies参照)
11b. `GroundSemanticMaskBaker.Bake` → HeightSlopeTex + SemanticTex (SP-032)
12. `MapRenderer.Render` → road meshes (RoadProfile駆動, Road.shader)
13. `BuildingSpawner.Spawn` → building GOs (4 shapes x floor-based height)
14. `WaterRenderer.Render` → water meshes (typed waterBodies, depth UV2, roughness vertex color)
15. `DecorationPlacer.Place` → decorations
16. `DecorationSpawner.Spawn` → decoration GOs + LOD
17. `EnsureGroundPlane` → elevation-following ground mesh (material instance + mask bind + preset defaults)
18. `OnMapGenerated` event → ThemeManager再適用 + PlayerMovement位置リセット

### 10.2 RoadProfile (ScriptableObject)

```csharp
namespace MiniMapGame.Data
{
    [CreateAssetMenu(fileName = "NewRoadProfile", menuName = "MiniMapGame/RoadProfile")]
    public class RoadProfile : ScriptableObject
    {
        [System.Serializable]
        public struct RoadTierConfig
        {
            public string tierName;
            [Range(1, 6)] public int laneCount;
            public float laneWidth, shoulderWidth, curbWidth;
            public bool hasCenterLine, centerLineSolid, hasLaneDividers, hasEdgeLines;
            public float markingWidth, dashLength, dashGap;
            [Range(0f,1f)] public float roughness, wear, crackDensity;
            public float TotalWidth => 2f*(curbWidth+shoulderWidth) + laneCount*laneWidth;
        }
        public RoadTierConfig[] tiers = new RoadTierConfig[3];
        public static RoadProfile CreateDefaultFallback(); // T0=1.4, T1=0.9, T2=0.5
    }
}
```

**デフォルトプロファイル3種** (Editor/RoadProfileCreator で生成):
| プロファイル | T0 | T1 | T2 |
|------------|----|----|-----|
| Modern | Highway 4車線/全標示 | Street 2車線/中央+端線 | Alley 1車線/標示なし |
| Rural | Country Road 2車線/中央線のみ | Farm Track 1車線/標示なし | Path 1車線/荒れ |
| Historic | Boulevard 2車線/標示なし/高粗さ | Cobblestone 1車線/石畳風 | Passage 1車線 |

**後方互換**: `MapPreset.roadProfile == null` → `CreateDefaultFallback()` で旧ハードコード幅と一致する値を生成。

### 10.3 Road.shader (URP HLSL)

```
道路断面 (UV.x マッピング):
0.0          curbR     shoulder          0.5           shoulder     curbR          1.0
|---curb----|--shoulder--|------lanes------|------lanes------|--shoulder--|---curb----|
| _CurbColor| _CasingClr| _BaseColor+mark | _BaseColor+mark | _CasingClr| _CurbColor|
```

- `uMirror = abs(UV.x - 0.5) * 2.0` で左右対称処理
- UV.y = 累積距離 → ダッシュパターン (`_DashLength` / `_DashGap`)
- 路面ノイズ: `hash21`/`noise2D` で磨耗・ひび割れ・粗さ
- Lambert照明 + フォグ + シャドウ (ForwardLit + ShadowCaster)
- Queue = Geometry+1 (地面の上)

**シェーダープロパティ** (17個):
`_BaseColor`, `_CasingColor`, `_MarkingColor`, `_CurbColor`,
`_CurbRatio`, `_ShoulderRatio`, `_LaneCount`, `_MarkingWidthRatio`,
`_HasCenterLine`, `_CenterLineSolid`, `_HasLaneDividers`, `_HasEdgeLines`,
`_DashLength`, `_DashGap`, `_Roughness`, `_Wear`, `_CrackDensity`

### 10.4 MapRenderer (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    /// <summary>
    /// 道路ネットワークをプロシージャルメッシュで描画。
    /// 単一メッシュ per (tier, layer)、UV駆動 Road.shader で車線標示・路面表現。
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Road Materials (per tier, uses Road.shader)")]
        public Material[] roadMaterials = new Material[3];

        [Header("Rendering")]
        public int bezierSegments = 16;
        public float roadYOffset = 0.01f;

        [Header("Intersections")]
        [Range(0f, 2f)] public float intersectionRadiusFactor = 0.7f;
        public int intersectionSegments = 12;

        public ElevationMap ElevMap { get; set; }
        public void Render(MapData data);  // Edges → Intersections → Pillars → Markers
        public void Clear();
    }
}
```

**レンダリングパイプライン**:
1. **RenderEdges**: tier×layer 別にメッシュをバッチ生成。ベジェ曲線を分割し ribbon 形状構築
   - UV.x = [0,1] 道路横断位置、UV.y = 累積ワールド距離
   - `ApplyProfileToMaterial()` で RoadTierConfig → シェーダープロパティ設定
2. **RenderIntersections**: degree≥3 ノードに円盤メッシュ生成（標示なし専用マテリアル）
3. **RenderBridgePillars**: layer=1 エッジの中間点にピラー (Cylinder) 生成
4. **RenderNodeMarkers**: plazaIndices / intersectionIndices にマーカープレハブ配置

**draw call**: 旧 outer+inner 2ストリップ → 統合1ストリップで約半減。

### 10.5 BuildingSpawner (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    public class BuildingSpawner : MonoBehaviour
    {
        public void Spawn(MapData data);
        public void Clear();
    }
}
```

**スポーンロジック**:
- `MapBuilding` ごとにプロシージャルメッシュを生成（プレハブ不使用）
- 4形状: Box / L-shape / Cylinder / Stepped (`shapeType` フィールドで決定)
- Y高さ: `floors × floorHeight (1.2f)`, Y座標は `ElevationMap.Sample` で高度追従
- マテリアルは色別Dictionary キャッシュで再利用
- isLandmark → BuildingInteractionコンポーネント追加

### 10.6 BuildingInteraction (MonoBehaviour)

```csharp
namespace MiniMapGame.Runtime
{
    /// <summary>
    /// ランドマーク建物近接時にインテリア進入を処理。
    /// InteriorControllerと連携。
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        public string buildingId;    // "B42" etc.
        // トリガーベースの近接検出 → InteriorController.EnterBuilding()
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
        /// BSP分割 + ランダム分岐で4-8部屋を生成。
        /// </summary>
        public static InteriorMapData Generate(int seed);
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

> **注意**: 以下のインターフェースおよび実装クラスは完全に実装済みだが、
> ジャンル再定義（探索・発見ゲーム）に伴い**凍結中**。
> 追跡者システム導入時に再設計する可能性がある。

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

---

## 18. 実装済み機能一覧 (2026-03-08 時点)

### Phase A-D: 基盤 (完了)
- データ構造全種、SeededRng、SpatialHash
- 4ジェネレータ (Organic/Grid/Mountain/Rural)
- MapManager統合、MapRenderer、BuildingSpawner
- InteriorController + BSP部屋生成
- GameLoop (遭遇/回収/脱出) + セーブ/ロード
- ミニゲーム3種 (TimingCombat/MemoryMatch/TrapDodge)

### Phase E: ビジュアル (完了)
- ライティング/ポストプロセス/フォグ/パーティクル
- GridGround shader (グリッド線 + 高度色 + 斜面色)
- テーマシステム (Dark/Parchment)

### 高低差システム (完了)
- ElevationMap: HillData から SlopeProfile別 ComputeFalloff でサンプリング
  - 5プロファイル: Gaussian/Steep/Gentle/Plateau/Mesa
  - SampleSlope(): central differences で傾斜勾配を計算
  - CarvingData: 水系による地形削り込み
- 道路: ベジェ制御点を高度追従
- 建物: Y座標を ElevationMap.Sample で設定、高さ ±7.5% ランダム変動、Smoothness 0.3-0.6 バリエーション
- 地面メッシュ: 96×96 グリッド、各頂点が高度追従 (SP-032で40→96に引き上げ)
- GridGround.shader: SP-032で13ステップ合成パイプラインに刷新 (詳細は下記「地表合成パイプライン」セクション参照)
- 橋/トンネル: BridgeTunnelDetector (edge.layer = -1/0/1)

### 水面レンダリング (完了)
- River: ribbonメッシュ (幅が下流に向け1.8倍増大)
- Coast: fan三角形分割ポリゴン
- Water.shader: UVスクロール半透明

### 装飾・LOD (完了)
- DecorationPlacer: 道路沿い配置 + 地形対応配置 (SpatialHash衝突回避)
- 12種:
  - 道路沿い: StreetLight, Tree, Bench, Bollard
  - 地形対応: Rock (高地急斜面), Boulder (急斜面), GrassClump (平坦低地), Wildflower (水辺低地), Shrub (丘の縁), Fence (道路沿いRural/Mountain), Stump (Tree付近Rural), SignPost (交差点Mountain/Rural)
- 地形選択: ElevationMap高度・SampleSlope傾斜・MinDistToWater水距離で自動判定
- LOD 3段階: <12f=全表示, <30f=LOD0+1, ≥30f=LOD0のみ

### P1改善 (完了, commit 6438fc4)
- 建物高さ: floors フィールド (tier別レンジ) × floorHeight 1.2f
- プリセット個別調整: maxElevation/elevationScale/enableBridges/decorationDensity + WaterProfile(SO)
- 河川-道路橋: BridgeTunnelDetector.DetectRiverCrossings
- 海岸方向ランダム: 4方向 (Right/Bottom/Left/Top)

### P2改善 (完了, commit f7d410c)
- 建物形状: 4種 (Box/L-shape/Cylinder/Stepped), shapeType フィールド
- Per-tier道路マテリアル: 3 outer + 3 inner → P4で Road.shader 統合1ストリップに刷新
- 地形テクスチャ: GridGround.shader に elevation/slope ブレンド追加
- 川幅変化: WaterProfile.RiverConfig.widthGrowth (上流→下流)

### P3改善 (完了, commit a25d4ec)
- Rural閉路: 隣接スポーク間クロスリンク (距離<200f, 70%確率)
- Mountain副峰: 1-2本の二次尾根 (spine 40-80%から分岐)
- Grid大通り: 2本の斜め大通り + 中心Hub ノード
- 海岸-建物排除: ray-cast point-in-polygon テスト
- 丘陵-道路協調: 丘陵中心を道路ノードから最低30f離す (3回ナッジ)

### P4改善: 道路レンダリングシステム刷新
- **RoadProfile SO**: 車線数・幅・標示・路面荒れ具合をInspectorで調整可能
- **Road.shader**: URP HLSL、UV駆動の車線標示+路面ノイズ (17プロパティ)
- **1ストリップ統合**: outer+inner 2層 → 単一ストリップ (draw call約半減)
- **交差点自動拡張**: degree≥3 ノードに円盤メッシュ (標示なし専用マテリアル)
- **デフォルトプロファイル3種**: Modern / Rural / Historic (RoadProfileCreator)
- **テーマ連携**: MapTheme に markingColor / curbColor 追加、Road.shader プロパティへ色適用
- **後方互換**: roadProfile=null → fallback生成、旧Material配列は [HideInInspector] で保持
- **プリセット自動バインド**: Bootstrap時 Coastal/Grid→Modern、Rural/Mountain→Rural を自動割当
- **BuildingPlacer連携**: RoadProfile.TotalWidthを建物セットバックの下限として参照

### P5改善: 地形品質向上
- **H1 丘クラスタ**: ランダム散布 → クラスタベース配置 (Ridge/MoundGroup/ValleyFramer/Solitary)
  - 4種のクラスタタイプ、方向・サイズ・間隔の自動制御
  - 新データ: ClusterType enum, HillCluster struct, MapTerrain.hillClusters
- **H2 装飾拡張**: 4種 → 12種、地形対応配置
  - 高度・傾斜・水距離に基づく自動選択
  - DecorationPlacer に ElevationMap/MapTerrain 参照を追加
- **H3 傾斜プロファイル**: 均一 Gaussian → 5種 SlopeProfile
  - ElevationMap.ComputeFalloff が SlopeProfile で分岐
  - MapPreset.steepnessBias でプロファイル確率制御
- **Q1 等高線**: GridGround.shader に topographic contour lines (2ワールドユニット間隔)
- **Q2 地面解像度**: GroundGridRes 20→40→96 (SP-032で更に引き上げ)
- **Q3 二段階グリッド**: 細グリッド + 5倍の粗グリッド
- **Q4 建物高さバリエーション**: ±7.5% ランダム (IDハッシュ基準)
- **Q5 斜面着色強化**: 0.6→0.75
- **Q6 建物マテリアル粗さ**: Smoothness 0.3-0.6 バリエーション (ハッシュbit24-31)
- **デバッグ可視化**: AnalysisVisualizer Terrain モード (Tab 3段階切替)
  - クラスタ中心+方向矢印、丘楕円輪郭、装飾位置ドット
  - MapControlUI にクラスタ・装飾統計情報追加

### SP-032: 地表合成レンダリングパイプライン (Slice 1-4 実装済み)

Ground carrier mesh + CPU semantic masks + compositing shader で地表を航空地図風に表現。
道路・水面・建物の独立メッシュはそのまま維持し、地表側に hillshade/contour/moisture/influence 等の補助表現を合成する。

- **GroundSemanticMaskBaker**: CPU前処理で2枚のRGBA8テクスチャを生成
  - `_GroundHeightSlopeTex`: R=elevation, G=slope, B=curvature, A=contour jitter
  - `_GroundSemanticTex`: R=moisture/shore, G=road influence, B=building influence, A=intersection boost
- **GridGround.shader**: 13ステップ合成パイプライン
  1. Mask sampling (mesh UV)
  2. Elevation gradient (Base→Mid→High 3色ブレンド)
  3. Slope tinting (slopeNorm > 0.3 で混合)
  4. Hillshade (4-tap法線再構成 + 2D光源)
  5. Curvature enhancement (谷暗/尾根明)
  6. Contour lines (interval + jitter)
  7. Moisture tint (水辺湿潤)
  8. Road influence tint (道路沿い)
  9. Building influence tint (建物基部)
  10. Intersection boost (交差点微増光)
  11. Near/far + Grid overlay (dual-scale, distance fade)
  12. Lighting + Fog (Lambert + ambient + URP fog)
- **GroundSurfacePresetDefaults**: GeneratorType別の強度自動選択
  - Mountain: hillshade 0.7, contour 0.35 (地形強調)
  - Grid: hillshade 0.25, road 0.4, building 0.35 (都市強調)
  - Rural: moisture 0.5 (水辺強調)
- **MapTheme連携**: 7色フィールド (groundMidColor/HighColor/SlopeColor/MoistureTint/RoadTint/BuildingTint/ContourColor)
  - Theme切替時はmask再生成なし、色のみ差し替え
- **マテリアルライフサイクル**: template asset + runtime instance分離、Clear()で破棄
- **地面メッシュ解像度**: groundGridResolution=96 (P5の40から引き上げ)
- **残タスク** (Slice 5): 色パレット最終調整、4preset x 2theme手動検証

---

## 19. ジェネレータ詳細仕様 (現行実装)

### 19.1 OrganicGenerator (港湾都市)
- Hub中心 → N本幹線放射 (arterialRange制御)
- 各幹線: 3-5ステップ、角度偏向+近接スナップ
- 環状道路: hasRingRoad=true時、終端atan2ソート接続
- 二次道路: tier 0 midpoint分岐 → tier 1 → tier 2
- ラベル: 中心="市中心", 終端="門N"

### 19.2 GridGenerator (NYCグリッド)
- 等間隔グリッド (spacing=42+rng*10, jitter付き)
- 水平/垂直エッジ: 中央・端=tier 0, 4n間隔=tier 1, 残=tier 2
- 斜め大通り2本: CreateDiagonalAvenue (tier 0, curveAmount=0.12)
- 中心ノード: NodeType.Hub, ラベル="中心街"

### 19.3 MountainGenerator (山道)
- 蛇行spine (上→下, curveAmount制御)
- 高度プロファイル: bell curve (peakPos=0.65付近)
- 副峰: 1-2本 (spine 40-80%分岐, 3-5ステップ, 高度=親の70%-40%)
- Dead-end枝: 40%確率, tier 1-2
- ラベル: "登山口"/"山頂"/"峠"/"避難小屋"/"副峰"

### 19.4 RuralGenerator (田舎町)
- Hub中心="村" → N本放射道路
- 各spoke: 4-8ステップ, 前半tier 0, 後半tier 1
- 横枝: 25%確率 tier 2
- クロスリンク: 隣接spoke間 mid-level接続 (距離<200f, 70%)
- ラベル: 終端="農場"(60%)

---

## 20. MapPreset 全フィールド (現行)

| Preset | Generator | world | arterialRange | ring | curve | bldg | coast | river | hill | maxElev | elevScale | steep | bridges | decor | roadProfile |
|--------|-----------|-------|---------------|------|-------|------|-------|-------|------|---------|-----------|-------|---------|-------|-------------|
| Coastal | Organic | 860x580 | 7,8 | ✓ | 0.62 | 0.68 | ✓ | ✓ | 0.38 | 12.0 | 0.90 | 0.45 | ✓ | 0.62 | Modern |
| Rural | Rural | 860x580 | 4,5 | ✗ | 0.58 | 0.20 | ✗ | ✓ | 0.55 | 10.0 | 0.85 | 0.28 | ✓ | 0.48 | Rural |
| NYC Grid | Grid | 860x580 | 5,7 | ✗ | 0.02 | 0.82 | ✗ | ✓ | 0.02 | 1.5 | 0.10 | 0.15 | ✓ | 0.42 | Modern |
| Mountain | Mountain | 860x580 | 2,3 | ✗ | 0.94 | 0.08 | ✗ | ✓ | 1.00 | 48.0 | 1.80 | 0.88 | ✓ | 0.16 | Rural |
| Island | Organic | 620x620 | 4,6 | ✓ | 0.67 | 0.52 | ✓ | ✗ | 0.22 | 8.0 | 0.70 | 0.35 | ✗ | 0.58 | Historic |
| Downtown | Grid | 720x520 | 6,8 | ✗ | 0.01 | 0.98 | ✗ | ✗ | 0.01 | 1.2 | 0.06 | 0.10 | ✗ | 0.28 | Modern |
| Valley | Mountain | 640x720 | 2,3 | ✗ | 0.74 | 0.07 | ✗ | ✓ | 1.00 | 34.0 | 1.35 | 0.94 | ✓ | 0.22 | Rural |

- waterProfile は現行 7 preset とも `null` (WaterRenderer default / Theme 駆動)。
- borderPadding: Coastal/Rural/Grid/Mountain=50, Island/Downtown=40, Valley=50。

---

## 21. 今後の方針・ユーザー要望

### 開発方針
- **速度重視**: テスト最小限、実装優先
- **自律実行**: 推奨選択肢・改善提案を順次実行。意思決定またはテストが必要になるまで前進
- **日本語**: コミュニケーションは日本語
- **並列処理**: 可能な限りタスクを並列実行

### 未実装・次期候補
- **パフォーマンス最適化**: メッシュ結合、GPU instancing、オブジェクトプール
- **プレイヤー体験**: エンカウント演出、アイテム収集エフェクト、脱出カウントダウン
- **マップ多様性**: 新ジェネレータ追加 (Island, Maze等)
- **建物内装**: インテリア装飾のバリエーション強化
- **オーディオ**: 環境音、BGM、SE
- **モバイル対応**: タッチ操作、UI最適化
