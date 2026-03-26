using System.Text;
using UnityEngine;
using TMPro;
using MiniMapGame.GameLoop;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Q-key toggle overlay showing active and completed quests.
    /// Displays quest title, description, objective progress, and reward.
    /// </summary>
    public class QuestLogUI : MonoBehaviour
    {
        [Header("References")]
        public QuestManager questManager;

        [Header("UI")]
        public GameObject logPanel;
        public TextMeshProUGUI logText;

        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.Q;

        [Header("Events")]
        public MapEventBus eventBus;

        private bool _visible;

        void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
                eventBus.Subscribe<QuestProgressEvent>(OnQuestProgress);
            }
        }

        void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
                eventBus.Unsubscribe<QuestProgressEvent>(OnQuestProgress);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        private void Toggle()
        {
            if (logPanel == null) return;
            _visible = !_visible;
            logPanel.SetActive(_visible);
            if (_visible) Refresh();
        }

        private void Refresh()
        {
            if (questManager == null || logText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("<size=22><b>Quest Log</b></size>");
            sb.AppendLine($"<size=14><color=#888>Active: {questManager.ActiveCount}  " +
                          $"Completed: {questManager.CompletedCount}</color></size>\n");

            // Active quests
            bool hasActive = false;
            foreach (var kvp in questManager.ActiveQuests)
            {
                if (kvp.Value.status != QuestStatus.Active) continue;
                var def = questManager.GetDefinition(kvp.Key);
                if (def == null) continue;

                hasActive = true;
                AppendQuest(sb, def, kvp.Value, false);
            }

            if (!hasActive)
                sb.AppendLine("<color=#666>No active quests.</color>\n");

            // Completed quests
            if (questManager.CompletedCount > 0)
            {
                sb.AppendLine("<color=#888>--- Completed ---</color>\n");
                foreach (var kvp in questManager.ActiveQuests)
                {
                    if (kvp.Value.status != QuestStatus.Completed) continue;
                    var def = questManager.GetDefinition(kvp.Key);
                    if (def == null) continue;

                    AppendQuest(sb, def, kvp.Value, true);
                }
            }

            logText.text = sb.ToString();
        }

        private void AppendQuest(StringBuilder sb, QuestDefinition def, QuestState state, bool completed)
        {
            string statusColor = completed ? "#E0C030" : "#FFF";
            string check = completed ? "<color=#6B6>[DONE]</color> " : "";
            sb.AppendLine($"{check}<color={statusColor}><b>{def.title}</b></color>");
            sb.AppendLine($"  <size=12><color=#AAA>{def.description}</color></size>");

            for (int i = 0; i < def.objectives.Count; i++)
            {
                var obj = def.objectives[i];
                int current = i < state.objectiveProgress.Count ? state.objectiveProgress[i] : 0;
                string bar = obj.IsCompleted ? "<color=#6B6>DONE</color>" : $"{current}/{obj.count}";
                string label = GetObjectiveLabel(obj);
                sb.AppendLine($"    {label}: {bar}");
            }

            if (def.reward != null && def.reward.value > 0)
                sb.AppendLine($"  <size=12><color=#E0C030>Reward: +{def.reward.value}</color></size>");

            sb.AppendLine();
        }

        private string GetObjectiveLabel(QuestObjective obj)
        {
            return obj.type switch
            {
                ObjectiveType.EnterBuilding => obj.target == "*" ? "Enter buildings" : $"Enter {obj.target}",
                ObjectiveType.CollectDiscovery => obj.target == "*" ? "Collect items" : $"Find {obj.target}",
                ObjectiveType.VisitFloor => "Visit floors",
                ObjectiveType.CompleteBuilding => obj.target == "*" ? "Complete buildings" : $"Complete {obj.target}",
                ObjectiveType.FindRare => $"Find {obj.target} item",
                ObjectiveType.VisitNode => "Visit locations",
                _ => obj.type.ToString()
            };
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (_visible) Refresh();
        }

        private void OnQuestProgress(QuestProgressEvent evt)
        {
            if (_visible) Refresh();
        }
    }
}
