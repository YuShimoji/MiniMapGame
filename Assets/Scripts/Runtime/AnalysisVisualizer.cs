using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Visualizes MapAnalysis and terrain debug data.
    /// Tab cycles modes: Off → Analysis → Terrain → Off.
    /// Analysis: dead ends (red), chokes (orange), intersections (green), plazas (blue).
    /// Terrain: hill clusters, hill outlines, decoration positions.
    /// </summary>
    public class AnalysisVisualizer : MonoBehaviour
    {
        private enum DebugMode { Off, Analysis, Terrain }

        [Header("References")]
        public MapManager mapManager;

        [Header("Toggle")]
        public KeyCode toggleKey = KeyCode.Tab;
        public bool isEnabled = true;

        [Header("Dead End (Red)")]
        public Color deadEndColor = new Color(0.86f, 0.20f, 0.20f, 0.85f);
        public float deadEndRadius = 1.5f;
        public int deadEndSegments = 16;

        [Header("Choke Edge (Orange)")]
        public Color chokeColor = new Color(0.90f, 0.55f, 0.08f, 0.75f);
        public float chokeWidth = 0.6f;
        public int chokeBezierSegments = 16;

        [Header("Intersection (Green)")]
        public Color intersectionColor = new Color(0.16f, 0.78f, 0.39f, 0.6f);
        public float intersectionSize = 2f;

        [Header("Plaza (Blue)")]
        public Color plazaColor = new Color(0.12f, 0.39f, 0.71f, 0.7f);
        public float plazaSize = 3f;

        [Header("Terrain: Cluster Types")]
        public Color ridgeColor = new Color(0.80f, 0.30f, 0.30f, 0.9f);
        public Color moundGroupColor = new Color(0.30f, 0.70f, 0.30f, 0.9f);
        public Color valleyFramerColor = new Color(0.30f, 0.30f, 0.80f, 0.9f);
        public Color solitaryColor = new Color(0.70f, 0.70f, 0.30f, 0.9f);
        public float clusterCenterRadius = 2.0f;
        public float clusterArrowLength = 8.0f;

        [Header("Terrain: Slope Profiles")]
        public Color gaussianColor = new Color(0.50f, 0.50f, 0.50f, 0.7f);
        public Color steepColor = new Color(0.85f, 0.25f, 0.25f, 0.7f);
        public Color gentleColor = new Color(0.25f, 0.75f, 0.35f, 0.7f);
        public Color plateauColor = new Color(0.65f, 0.45f, 0.15f, 0.7f);
        public Color mesaColor = new Color(0.55f, 0.20f, 0.60f, 0.7f);
        public int hillEllipseSegments = 24;

        [Header("Terrain: Decorations")]
        public float decorationDotRadius = 0.4f;
        public int decorationDotSegments = 6;

        private DebugMode _debugMode = DebugMode.Off;
        private MapData _mapData;
        private MapPreset _preset;
        private readonly List<GameObject> _analysisObjects = new();
        private readonly List<GameObject> _terrainObjects = new();
        private readonly Dictionary<Color, Material> _materialCache = new();

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _debugMode = _debugMode switch
                {
                    DebugMode.Off => DebugMode.Analysis,
                    DebugMode.Analysis => DebugMode.Terrain,
                    DebugMode.Terrain => DebugMode.Off,
                    _ => DebugMode.Off
                };
                isEnabled = _debugMode != DebugMode.Off;
                ApplyDebugMode();
            }
        }

        private void ApplyDebugMode()
        {
            SetVisibility(_analysisObjects, _debugMode == DebugMode.Analysis);
            SetVisibility(_terrainObjects, _debugMode == DebugMode.Terrain);
        }

        private void OnMapGenerated(MapData mapData)
        {
            ClearVisuals();
            _mapData = mapData;
            _preset = mapManager.activePreset;
            BuildAnalysisVisuals();
            BuildTerrainVisuals();
            ApplyDebugMode();
        }

        // ──────────────────────────────
        //  Analysis visuals (existing)
        // ──────────────────────────────

        private void BuildAnalysisVisuals()
        {
            if (_mapData == null || _preset == null) return;

            var analysis = _mapData.analysis;
            const float yLine = 0.15f;

            // Dead Ends — red circles
            foreach (int idx in analysis.deadEndIndices)
            {
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateCircle(worldPos, deadEndRadius, deadEndColor, deadEndSegments,
                    "DeadEnd", _analysisObjects);
            }

            // Choke Edges — orange bezier curves
            foreach (int edgeIdx in analysis.chokeEdgeIndices)
            {
                var edge = _mapData.edges[edgeIdx];
                var nodeA = _mapData.nodes[edge.nodeA];
                var nodeB = _mapData.nodes[edge.nodeB];
                CreateChokeLine(nodeA.position, nodeB.position, edge.controlPoint,
                    nodeA.elevation, nodeB.elevation, yLine);

                var mid2D = MapGenUtils.BezierPoint(nodeA.position, edge.controlPoint, nodeB.position, 0.5f);
                float midElev = (nodeA.elevation + nodeB.elevation) * 0.5f;
                var midWorld = MapGenUtils.ToWorldPosition(mid2D, midElev, _preset);
                midWorld.y += yLine;
                CreateDiamond(midWorld, 1.5f, chokeColor, "ChokeMid", _analysisObjects);
            }

            // Intersections — green crosses (skip plazas)
            foreach (int idx in analysis.intersectionIndices)
            {
                if (analysis.plazaIndices.Contains(idx)) continue;
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateCross(worldPos, intersectionSize, intersectionColor,
                    "Intersection", _analysisObjects);
            }

            // Plazas — blue squares
            foreach (int idx in analysis.plazaIndices)
            {
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateSquare(worldPos, plazaSize, plazaColor, "Plaza", _analysisObjects);
            }
        }

        // ──────────────────────────────
        //  Terrain visuals (new)
        // ──────────────────────────────

        private void BuildTerrainVisuals()
        {
            if (_mapData == null || _preset == null) return;

            var terrain = _mapData.terrain;
            var elevMap = mapManager.CurrentElevationMap;
            const float yLine = 0.20f; // slightly higher than analysis layer

            // 1. Hill clusters — center circle + orientation arrow
            if (terrain.hillClusters != null)
            {
                foreach (var cluster in terrain.hillClusters)
                {
                    float elev = elevMap != null ? elevMap.Sample(cluster.center) : 0f;
                    var worldPos = MapGenUtils.ToWorldPosition(cluster.center, elev, _preset);
                    worldPos.y += yLine;
                    var clrColor = GetClusterColor(cluster.type);

                    CreateCircle(worldPos, clusterCenterRadius, clrColor, 12,
                        $"Cluster_{cluster.type}", _terrainObjects);
                    CreateArrow(worldPos, cluster.orientationAngle, clusterArrowLength,
                        clrColor, _terrainObjects);
                }
            }

            // 2. Hills — rotated ellipse outlines colored by SlopeProfile
            if (terrain.hills != null)
            {
                foreach (var hill in terrain.hills)
                {
                    float elev = elevMap != null ? elevMap.Sample(hill.position) : 0f;
                    var worldPos = MapGenUtils.ToWorldPosition(hill.position, elev, _preset);
                    worldPos.y += yLine;
                    var profColor = GetSlopeProfileColor(hill.profile);

                    CreateEllipse(worldPos, hill.radiusX, hill.radiusY, hill.angle,
                        profColor, hillEllipseSegments, $"Hill_{hill.profile}",
                        _terrainObjects);
                }
            }

            // 3. Decorations — small colored dots by type
            if (_mapData.decorations != null)
            {
                foreach (var deco in _mapData.decorations)
                {
                    float elev = elevMap != null ? elevMap.Sample(deco.position) : 0f;
                    var worldPos = MapGenUtils.ToWorldPosition(deco.position, elev, _preset);
                    worldPos.y += yLine;
                    var decoColor = GetDecorationColor(deco.type);

                    CreateCircle(worldPos, decorationDotRadius, decoColor,
                        decorationDotSegments, $"Deco_{deco.type}", _terrainObjects);
                }
            }
        }

        // ──────────────────────────────
        //  Color lookups
        // ──────────────────────────────

        private Color GetClusterColor(ClusterType type) => type switch
        {
            ClusterType.Ridge => ridgeColor,
            ClusterType.MoundGroup => moundGroupColor,
            ClusterType.ValleyFramer => valleyFramerColor,
            ClusterType.Solitary => solitaryColor,
            _ => Color.white
        };

        private Color GetSlopeProfileColor(SlopeProfile profile) => profile switch
        {
            SlopeProfile.Gaussian => gaussianColor,
            SlopeProfile.Steep => steepColor,
            SlopeProfile.Gentle => gentleColor,
            SlopeProfile.Plateau => plateauColor,
            SlopeProfile.Mesa => mesaColor,
            _ => Color.gray
        };

        private static Color GetDecorationColor(DecorationType type) => type switch
        {
            DecorationType.StreetLight => new Color(1.0f, 0.95f, 0.6f, 0.8f),
            DecorationType.Tree => new Color(0.2f, 0.65f, 0.2f, 0.8f),
            DecorationType.Bench => new Color(0.55f, 0.35f, 0.15f, 0.8f),
            DecorationType.Bollard => new Color(0.6f, 0.6f, 0.6f, 0.8f),
            DecorationType.Rock => new Color(0.5f, 0.5f, 0.45f, 0.8f),
            DecorationType.Boulder => new Color(0.4f, 0.38f, 0.35f, 0.8f),
            DecorationType.GrassClump => new Color(0.4f, 0.75f, 0.3f, 0.8f),
            DecorationType.Wildflower => new Color(0.85f, 0.4f, 0.65f, 0.8f),
            DecorationType.Shrub => new Color(0.3f, 0.55f, 0.25f, 0.8f),
            DecorationType.Fence => new Color(0.7f, 0.55f, 0.3f, 0.8f),
            DecorationType.Stump => new Color(0.45f, 0.3f, 0.15f, 0.8f),
            DecorationType.SignPost => new Color(0.8f, 0.8f, 0.2f, 0.8f),
            _ => Color.magenta
        };

        // ──────────────────────────────
        //  Shape primitives
        // ──────────────────────────────

        private void CreateCircle(Vector3 center, float radius, Color color,
            int segments, string label, List<GameObject> targetList)
        {
            var go = new GameObject($"Viz_{label}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLine(lr, color, 0.15f);
            lr.loop = true;
            lr.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }

            targetList.Add(go);
        }

        private void CreateEllipse(Vector3 center, float radiusX, float radiusY,
            float rotAngle, Color color, int segments, string label,
            List<GameObject> targetList)
        {
            var go = new GameObject($"Viz_{label}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLine(lr, color, 0.12f);
            lr.loop = true;
            lr.positionCount = segments;

            float cosR = Mathf.Cos(-rotAngle);
            float sinR = Mathf.Sin(-rotAngle);

            for (int i = 0; i < segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                float lx = Mathf.Cos(t) * radiusX;
                float lz = Mathf.Sin(t) * radiusY;
                // Rotate then place — Z axis inverted for Unity world coords
                float wx = lx * cosR - lz * sinR;
                float wz = lx * sinR + lz * cosR;
                lr.SetPosition(i, center + new Vector3(wx, 0, -wz));
            }

            targetList.Add(go);
        }

        private void CreateArrow(Vector3 origin, float angle, float length,
            Color color, List<GameObject> targetList)
        {
            // Shaft
            float dx = Mathf.Cos(angle) * length;
            float dz = -Mathf.Sin(angle) * length; // Y-inversion
            var tip = origin + new Vector3(dx, 0, dz);

            var goShaft = new GameObject("Viz_ClusterArrow");
            goShaft.transform.SetParent(transform);
            var lrShaft = goShaft.AddComponent<LineRenderer>();
            ConfigureLine(lrShaft, color, 0.25f);
            lrShaft.positionCount = 2;
            lrShaft.SetPosition(0, origin);
            lrShaft.SetPosition(1, tip);
            targetList.Add(goShaft);

            // Arrowhead wings
            float headLen = length * 0.25f;
            float wingAngle = 0.45f; // ~25 degrees
            for (int side = -1; side <= 1; side += 2)
            {
                float wa = angle + Mathf.PI + side * wingAngle;
                var wingEnd = tip + new Vector3(
                    Mathf.Cos(wa) * headLen, 0, -Mathf.Sin(wa) * headLen);
                var goWing = new GameObject("Viz_ArrowWing");
                goWing.transform.SetParent(transform);
                var lrWing = goWing.AddComponent<LineRenderer>();
                ConfigureLine(lrWing, color, 0.2f);
                lrWing.positionCount = 2;
                lrWing.SetPosition(0, tip);
                lrWing.SetPosition(1, wingEnd);
                targetList.Add(goWing);
            }
        }

        private void CreateChokeLine(Vector2 posA, Vector2 posB, Vector2 ctrl,
            float elevA, float elevB, float yOffset)
        {
            var go = new GameObject("Viz_ChokeEdge");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLine(lr, chokeColor, chokeWidth);
            lr.positionCount = chokeBezierSegments + 1;

            for (int i = 0; i <= chokeBezierSegments; i++)
            {
                float t = i / (float)chokeBezierSegments;
                var p2d = MapGenUtils.BezierPoint(posA, ctrl, posB, t);
                float elev = Mathf.Lerp(elevA, elevB, t);
                var wp = MapGenUtils.ToWorldPosition(p2d, elev, _preset);
                wp.y += yOffset;
                lr.SetPosition(i, wp);
            }

            _analysisObjects.Add(go);
        }

        private void CreateDiamond(Vector3 center, float size, Color color,
            string label, List<GameObject> targetList)
        {
            var go = new GameObject($"Viz_{label}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLine(lr, color, 0.2f);
            lr.loop = true;
            lr.positionCount = 4;
            lr.SetPosition(0, center + new Vector3(0, 0, size));
            lr.SetPosition(1, center + new Vector3(size, 0, 0));
            lr.SetPosition(2, center + new Vector3(0, 0, -size));
            lr.SetPosition(3, center + new Vector3(-size, 0, 0));

            targetList.Add(go);
        }

        private void CreateCross(Vector3 center, float size, Color color,
            string label, List<GameObject> targetList)
        {
            float half = size * 0.5f;

            var goH = new GameObject($"Viz_{label}_H");
            goH.transform.SetParent(transform);
            var lrH = goH.AddComponent<LineRenderer>();
            ConfigureLine(lrH, color, 0.12f);
            lrH.positionCount = 2;
            lrH.SetPosition(0, center + new Vector3(-half, 0, 0));
            lrH.SetPosition(1, center + new Vector3(half, 0, 0));
            targetList.Add(goH);

            var goV = new GameObject($"Viz_{label}_V");
            goV.transform.SetParent(transform);
            var lrV = goV.AddComponent<LineRenderer>();
            ConfigureLine(lrV, color, 0.12f);
            lrV.positionCount = 2;
            lrV.SetPosition(0, center + new Vector3(0, 0, -half));
            lrV.SetPosition(1, center + new Vector3(0, 0, half));
            targetList.Add(goV);
        }

        private void CreateSquare(Vector3 center, float size, Color color,
            string label, List<GameObject> targetList)
        {
            float half = size * 0.5f;
            var go = new GameObject($"Viz_{label}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLine(lr, color, 0.18f);
            lr.loop = true;
            lr.positionCount = 4;
            lr.SetPosition(0, center + new Vector3(-half, 0, -half));
            lr.SetPosition(1, center + new Vector3(half, 0, -half));
            lr.SetPosition(2, center + new Vector3(half, 0, half));
            lr.SetPosition(3, center + new Vector3(-half, 0, half));

            targetList.Add(go);
        }

        // ──────────────────────────────
        //  Utilities
        // ──────────────────────────────

        private void ConfigureLine(LineRenderer lr, Color color, float width)
        {
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = color;
            if (!_materialCache.TryGetValue(color, out var mat))
            {
                mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = color;
                _materialCache[color] = mat;
            }
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
        }

        private static void SetVisibility(List<GameObject> objects, bool visible)
        {
            foreach (var obj in objects)
                if (obj != null) obj.SetActive(visible);
        }

        private void ClearVisuals()
        {
            foreach (var obj in _analysisObjects)
                if (obj != null) Destroy(obj);
            _analysisObjects.Clear();

            foreach (var obj in _terrainObjects)
                if (obj != null) Destroy(obj);
            _terrainObjects.Clear();

            foreach (var mat in _materialCache.Values)
                if (mat != null) Destroy(mat);
            _materialCache.Clear();
        }
    }
}
