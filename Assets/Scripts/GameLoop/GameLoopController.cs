using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.GameLoop
{
    public class GameLoopController : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public MapEventBus eventBus;

        [Header("Prefabs")]
        public GameObject valueObjectPrefab;
        public GameObject encounterZonePrefab;
        public GameObject extractionPointPrefab;

        [Header("Value Settings")]
        public int baseValueMin = 10;
        public int baseValueMax = 50;

        [Header("Encounter Settings")]
        public int encounterDamageMin = 10;
        public int encounterDamageMax = 30;

        [Header("UI")]
        public GameLoopUI gameLoopUI;

        public GameState State { get; private set; } = new();

        private readonly List<GameObject> _spawnedEntities = new();
        private MapPreset _currentPreset;

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;
            eventBus?.Subscribe<ValueCollectedEvent>(OnValueCollected);
            eventBus?.Subscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            eventBus?.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
            eventBus?.Unsubscribe<ValueCollectedEvent>(OnValueCollected);
            eventBus?.Unsubscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            eventBus?.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        }

        private void OnMapGenerated(MapData mapData)
        {
            ClearEntities();
            State.Reset();
            _currentPreset = mapManager.activePreset;

            SpawnValueObjects(mapData);
            SpawnEncounterZones(mapData);
            SpawnExtractionPoints(mapData);

            eventBus?.Publish(new GameLoopStartedEvent
            {
                seed = mapData.seed,
                deadEndCount = mapData.analysis.deadEndIndices.Count,
                chokePointCount = mapData.analysis.chokeEdgeIndices.Count,
                extractionPointCount = CountExtractionNodes(mapData)
            });

            gameLoopUI?.ShowHUD(State);
        }

        private void SpawnValueObjects(MapData mapData)
        {
            if (valueObjectPrefab == null) return;
            var rng = new SeededRng(mapData.seed + 1000);

            foreach (int idx in mapData.analysis.deadEndIndices)
            {
                var node = mapData.nodes[idx];
                var worldPos = MapGenUtils.ToWorldPosition(node.position, _currentPreset);
                worldPos.y = 0.5f;

                var go = Instantiate(valueObjectPrefab, worldPos, Quaternion.identity, transform);
                go.name = $"ValueObj_N{idx}";

                var vo = go.GetComponent<ValueObjectBehaviour>();
                if (vo == null) vo = go.AddComponent<ValueObjectBehaviour>();
                vo.Initialize($"VO_{idx}", rng.Range(baseValueMin, baseValueMax + 1), this, eventBus);

                _spawnedEntities.Add(go);
            }
        }

        private void SpawnEncounterZones(MapData mapData)
        {
            if (encounterZonePrefab == null) return;
            var rng = new SeededRng(mapData.seed + 2000);

            foreach (int edgeIdx in mapData.analysis.chokeEdgeIndices)
            {
                var edge = mapData.edges[edgeIdx];
                var nodeA = mapData.nodes[edge.nodeA];
                var nodeB = mapData.nodes[edge.nodeB];

                var mid2D = MapGenUtils.BezierPoint(
                    nodeA.position, edge.controlPoint, nodeB.position, 0.5f);
                var worldPos = MapGenUtils.ToWorldPosition(mid2D, _currentPreset);

                var go = Instantiate(encounterZonePrefab, worldPos, Quaternion.identity, transform);
                go.name = $"Encounter_E{edgeIdx}";

                var ez = go.GetComponent<EncounterZone>();
                if (ez == null) ez = go.AddComponent<EncounterZone>();
                ez.damageAmount = rng.Range(encounterDamageMin, encounterDamageMax + 1);
                ez.Initialize(edgeIdx, edge, mapData, this, eventBus);

                _spawnedEntities.Add(go);
            }
        }

        private void SpawnExtractionPoints(MapData mapData)
        {
            if (extractionPointPrefab == null) return;

            for (int i = 0; i < mapData.nodes.Count; i++)
            {
                var node = mapData.nodes[i];
                if (!IsExtractionNode(node)) continue;

                var worldPos = MapGenUtils.ToWorldPosition(node.position, _currentPreset);

                var go = Instantiate(extractionPointPrefab, worldPos, Quaternion.identity, transform);
                go.name = $"Extract_N{i}";

                var ep = go.GetComponent<ExtractionPoint>();
                if (ep == null) ep = go.AddComponent<ExtractionPoint>();
                ep.Initialize(i, mapData, this, eventBus, gameLoopUI);

                _spawnedEntities.Add(go);
            }
        }

        private static bool IsExtractionNode(MapNode node)
        {
            if (node.type == NodeType.Gate) return true;
            return !string.IsNullOrEmpty(node.label) && node.label.Contains("門");
        }

        private static int CountExtractionNodes(MapData mapData)
        {
            int count = 0;
            foreach (var n in mapData.nodes)
                if (IsExtractionNode(n)) count++;
            return count;
        }

        public void HandleExtraction(bool extracted)
        {
            eventBus?.Publish(new GameLoopEndedEvent
            {
                finalValue = State.collectedValue,
                encounterCount = State.encounterCount,
                itemsCollected = State.collectedItemIds.Count,
                extracted = extracted
            });

            if (extracted)
                gameLoopUI?.ShowExtractionResult(State);
        }

        private void OnValueCollected(ValueCollectedEvent evt)
        {
            gameLoopUI?.UpdateHUD(State);
        }

        private void OnEncounterTriggered(EncounterTriggeredEvent evt)
        {
            gameLoopUI?.UpdateHUD(State);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            if (!State.stats.IsAlive)
                HandleExtraction(false);
        }

        private void ClearEntities()
        {
            foreach (var obj in _spawnedEntities)
                if (obj != null) Destroy(obj);
            _spawnedEntities.Clear();
        }
    }
}
