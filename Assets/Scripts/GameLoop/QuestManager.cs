using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Interior;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Manages quest lifecycle: loading, activation, progress tracking, completion.
    /// Subscribes to MapEventBus for automatic objective updates.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Header("References")]
        public MapEventBus eventBus;

        [Header("Quest Data")]
        [Tooltip("JSON TextAsset containing quest definitions")]
        public TextAsset questDataAsset;

        private readonly List<QuestDefinition> _definitions = new();
        private readonly Dictionary<string, QuestState> _activeQuests = new();
        private readonly List<string> _completedQuestIds = new();

        // Cached definition lookup
        private readonly Dictionary<string, QuestDefinition> _defLookup = new();

        public IReadOnlyList<QuestDefinition> Definitions => _definitions;
        public IReadOnlyDictionary<string, QuestState> ActiveQuests => _activeQuests;
        public IReadOnlyList<string> CompletedQuestIds => _completedQuestIds;

        // ════════════════════════════════════════
        //  Lifecycle
        // ════════════════════════════════════════

        void OnEnable()
        {
            if (eventBus == null) return;
            eventBus.Subscribe<BuildingEnteredEvent>(OnBuildingEntered);
            eventBus.Subscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
            eventBus.Subscribe<FloorChangedEvent>(OnFloorChanged);
            eventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
        }

        void OnDisable()
        {
            if (eventBus == null) return;
            eventBus.Unsubscribe<BuildingEnteredEvent>(OnBuildingEntered);
            eventBus.Unsubscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
            eventBus.Unsubscribe<FloorChangedEvent>(OnFloorChanged);
            eventBus.Unsubscribe<SessionStartedEvent>(OnSessionStarted);
        }

        // ════════════════════════════════════════
        //  Quest loading
        // ════════════════════════════════════════

        /// <summary>
        /// Load quest definitions from JSON and activate all eligible quests.
        /// Called on session start.
        /// </summary>
        public void LoadAndActivateQuests()
        {
            _definitions.Clear();
            _defLookup.Clear();
            _activeQuests.Clear();
            _completedQuestIds.Clear();

            if (questDataAsset == null)
            {
                Debug.LogWarning("[QuestManager] No quest data asset assigned.");
                return;
            }

            var collection = JsonUtility.FromJson<QuestCollection>(questDataAsset.text);
            if (collection?.quests == null)
            {
                Debug.LogWarning("[QuestManager] Failed to parse quest data.");
                return;
            }

            foreach (var def in collection.quests)
            {
                _definitions.Add(def);
                _defLookup[def.id] = def;
            }

            // Phase 1: activate all quests (no prerequisite filtering)
            foreach (var def in _definitions)
            {
                var state = new QuestState(def);
                _activeQuests[def.id] = state;
            }

            Debug.Log($"[QuestManager] Loaded {_definitions.Count} quests, {_activeQuests.Count} active.");
        }

        // ════════════════════════════════════════
        //  Event handlers
        // ════════════════════════════════════════

        private void OnSessionStarted(SessionStartedEvent evt)
        {
            LoadAndActivateQuests();
        }

        private void OnBuildingEntered(BuildingEnteredEvent evt)
        {
            foreach (var kvp in _activeQuests)
            {
                if (kvp.Value.status != QuestStatus.Active) continue;
                if (!_defLookup.TryGetValue(kvp.Key, out var def)) continue;

                for (int i = 0; i < def.objectives.Count; i++)
                {
                    var obj = def.objectives[i];
                    if (obj.type != ObjectiveType.EnterBuilding) continue;
                    if (obj.target != "*" && obj.target != evt.buildingCategory) continue;
                    if (obj.IsCompleted) continue;

                    IncrementObjective(kvp.Key, def, kvp.Value, i);
                }
            }
        }

        private void OnDiscoveryCollected(DiscoveryCollectedEvent evt)
        {
            foreach (var kvp in _activeQuests)
            {
                if (kvp.Value.status != QuestStatus.Active) continue;
                if (!_defLookup.TryGetValue(kvp.Key, out var def)) continue;

                for (int i = 0; i < def.objectives.Count; i++)
                {
                    var obj = def.objectives[i];
                    if (obj.IsCompleted) continue;

                    switch (obj.type)
                    {
                        case ObjectiveType.CollectDiscovery:
                            if (obj.target == "*" || obj.target == evt.furnitureType.ToString())
                                IncrementObjective(kvp.Key, def, kvp.Value, i);
                            break;

                        case ObjectiveType.FindRare:
                            if (obj.target == evt.rarity.ToString())
                                IncrementObjective(kvp.Key, def, kvp.Value, i);
                            break;
                    }
                }
            }
        }

        private void OnFloorChanged(FloorChangedEvent evt)
        {
            foreach (var kvp in _activeQuests)
            {
                if (kvp.Value.status != QuestStatus.Active) continue;
                if (!_defLookup.TryGetValue(kvp.Key, out var def)) continue;

                for (int i = 0; i < def.objectives.Count; i++)
                {
                    var obj = def.objectives[i];
                    if (obj.type != ObjectiveType.VisitFloor) continue;
                    if (obj.IsCompleted) continue;

                    IncrementObjective(kvp.Key, def, kvp.Value, i);
                }
            }
        }

        // ════════════════════════════════════════
        //  Progress tracking
        // ════════════════════════════════════════

        private void IncrementObjective(string questId, QuestDefinition def, QuestState state, int objIndex)
        {
            var obj = def.objectives[objIndex];
            obj.current++;
            state.objectiveProgress[objIndex] = obj.current;

            eventBus?.Publish(new QuestProgressEvent
            {
                questId = questId,
                objectiveIndex = objIndex,
                current = obj.current,
                target = obj.count
            });

            // Check if all objectives completed
            CheckCompletion(questId, def, state);
        }

        private void CheckCompletion(string questId, QuestDefinition def, QuestState state)
        {
            foreach (var obj in def.objectives)
            {
                if (!obj.IsCompleted) return;
            }

            state.status = QuestStatus.Completed;
            _completedQuestIds.Add(questId);

            eventBus?.Publish(new QuestCompletedEvent
            {
                questId = questId,
                title = def.title,
                rewardValue = def.reward?.value ?? 0
            });

            Debug.Log($"[QuestManager] Quest completed: {def.title}");
        }

        // ════════════════════════════════════════
        //  Public API
        // ════════════════════════════════════════

        public QuestDefinition GetDefinition(string questId)
        {
            return _defLookup.TryGetValue(questId, out var def) ? def : null;
        }

        public QuestState GetState(string questId)
        {
            return _activeQuests.TryGetValue(questId, out var state) ? state : null;
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var kvp in _activeQuests)
                    if (kvp.Value.status == QuestStatus.Active) count++;
                return count;
            }
        }

        public int CompletedCount => _completedQuestIds.Count;
    }
}
