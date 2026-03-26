using System.Text;
using UnityEngine;
using TMPro;
using MiniMapGame.GameLoop;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Always-visible mini display showing up to 3 active quest objectives.
    /// Auto-refreshes on quest progress and completion events.
    /// </summary>
    public class QuestHUD : MonoBehaviour
    {
        [Header("References")]
        public QuestManager questManager;
        public MapEventBus eventBus;

        [Header("UI")]
        public TextMeshProUGUI hudText;

        [Header("Settings")]
        public int maxDisplayed = 3;

        private readonly StringBuilder _sb = new();
        private bool _dirty = true;

        void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<QuestProgressEvent>(OnProgress);
                eventBus.Subscribe<QuestCompletedEvent>(OnCompleted);
                eventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
            }
        }

        void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<QuestProgressEvent>(OnProgress);
                eventBus.Unsubscribe<QuestCompletedEvent>(OnCompleted);
                eventBus.Unsubscribe<SessionStartedEvent>(OnSessionStarted);
            }
        }

        void LateUpdate()
        {
            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (questManager == null || hudText == null) return;

            _sb.Clear();

            int shown = 0;
            foreach (var kvp in questManager.ActiveQuests)
            {
                if (kvp.Value.status != QuestStatus.Active) continue;
                if (shown >= maxDisplayed) break;

                var def = questManager.GetDefinition(kvp.Key);
                if (def == null) continue;

                _sb.Append($"<b>{def.title}</b>  ");

                for (int i = 0; i < def.objectives.Count; i++)
                {
                    int current = i < kvp.Value.objectiveProgress.Count ? kvp.Value.objectiveProgress[i] : 0;
                    int target = def.objectives[i].count;
                    bool done = current >= target;

                    if (done)
                        _sb.Append("<color=#6B6>OK</color> ");
                    else
                        _sb.Append($"{current}/{target} ");
                }

                _sb.AppendLine();
                shown++;
            }

            if (shown == 0)
                _sb.Append("<color=#666>No active quests</color>");

            hudText.text = _sb.ToString();
        }

        private void OnProgress(QuestProgressEvent evt) => _dirty = true;
        private void OnCompleted(QuestCompletedEvent evt) => _dirty = true;
        private void OnSessionStarted(SessionStartedEvent evt) => _dirty = true;
    }
}
