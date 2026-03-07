using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Visualizes MapAnalysis data: dead ends, choke points, intersections, plazas.
    /// Toggle on/off at runtime via isEnabled or Tab key.
    /// Colors match JSX reference: dead=red, choke=orange, intersection=green, plaza=blue.
    /// </summary>
    public class AnalysisVisualizer : MonoBehaviour
    {
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

        private MapData _mapData;
        private MapPreset _preset;
        private readonly List<GameObject> _visualObjects = new();
        private bool _built;
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
                isEnabled = !isEnabled;
                SetVisibility(isEnabled);
            }
        }

        private void OnMapGenerated(MapData mapData)
        {
            ClearVisuals();
            _mapData = mapData;
            _preset = mapManager.activePreset;
            BuildVisuals();
        }

        private void BuildVisuals()
        {
            if (_mapData == null || _preset == null) return;

            var analysis = _mapData.analysis;
            float yLine = 0.15f; // slightly above ground

            // Dead Ends — red circles
            foreach (int idx in analysis.deadEndIndices)
            {
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateCircle(worldPos, deadEndRadius, deadEndColor, deadEndSegments, "DeadEnd");
            }

            // Choke Edges — orange dashed bezier curves
            foreach (int edgeIdx in analysis.chokeEdgeIndices)
            {
                var edge = _mapData.edges[edgeIdx];
                var nodeA = _mapData.nodes[edge.nodeA];
                var nodeB = _mapData.nodes[edge.nodeB];
                CreateChokeLine(nodeA.position, nodeB.position, edge.controlPoint,
                    nodeA.elevation, nodeB.elevation, yLine);

                // Diamond marker at midpoint
                var mid2D = MapGenUtils.BezierPoint(nodeA.position, edge.controlPoint, nodeB.position, 0.5f);
                float midElev = (nodeA.elevation + nodeB.elevation) * 0.5f;
                var midWorld = MapGenUtils.ToWorldPosition(mid2D, midElev, _preset);
                midWorld.y += yLine;
                CreateDiamond(midWorld, 1.5f, chokeColor, "ChokeMid");
            }

            // Intersections — green crosses (skip plazas)
            foreach (int idx in analysis.intersectionIndices)
            {
                if (analysis.plazaIndices.Contains(idx)) continue;
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateCross(worldPos, intersectionSize, intersectionColor, "Intersection");
            }

            // Plazas — blue squares
            foreach (int idx in analysis.plazaIndices)
            {
                var node = _mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, node.elevation, _preset);
                worldPos.y += yLine;
                CreateSquare(worldPos, plazaSize, plazaColor, "Plaza");
            }

            _built = true;
            SetVisibility(isEnabled);
        }

        private void CreateCircle(Vector3 center, float radius, Color color, int segments, string label)
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

            _visualObjects.Add(go);
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

            _visualObjects.Add(go);
        }

        private void CreateDiamond(Vector3 center, float size, Color color, string label)
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

            _visualObjects.Add(go);
        }

        private void CreateCross(Vector3 center, float size, Color color, string label)
        {
            float half = size * 0.5f;

            // Horizontal line
            var goH = new GameObject($"Viz_{label}_H");
            goH.transform.SetParent(transform);
            var lrH = goH.AddComponent<LineRenderer>();
            ConfigureLine(lrH, color, 0.12f);
            lrH.positionCount = 2;
            lrH.SetPosition(0, center + new Vector3(-half, 0, 0));
            lrH.SetPosition(1, center + new Vector3(half, 0, 0));
            _visualObjects.Add(goH);

            // Vertical line
            var goV = new GameObject($"Viz_{label}_V");
            goV.transform.SetParent(transform);
            var lrV = goV.AddComponent<LineRenderer>();
            ConfigureLine(lrV, color, 0.12f);
            lrV.positionCount = 2;
            lrV.SetPosition(0, center + new Vector3(0, 0, -half));
            lrV.SetPosition(1, center + new Vector3(0, 0, half));
            _visualObjects.Add(goV);
        }

        private void CreateSquare(Vector3 center, float size, Color color, string label)
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

            _visualObjects.Add(go);
        }

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

        private void SetVisibility(bool visible)
        {
            foreach (var obj in _visualObjects)
                if (obj != null) obj.SetActive(visible);
        }

        private void ClearVisuals()
        {
            foreach (var obj in _visualObjects)
                if (obj != null) Destroy(obj);
            _visualObjects.Clear();
            foreach (var mat in _materialCache.Values)
                if (mat != null) Destroy(mat);
            _materialCache.Clear();
            _built = false;
        }
    }
}
