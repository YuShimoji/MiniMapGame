using System.Collections;
using UnityEngine;
using TMPro;
using MiniMapGame.GameLoop;
using MiniMapGame.Interior;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Subscribes to MapEventBus interior events and displays toast notifications.
    /// Also shows a persistent floor indicator when inside a building.
    /// </summary>
    public class InteriorFeedbackUI : MonoBehaviour
    {
        [Header("References")]
        public MapEventBus eventBus;
        public InteriorController interiorController;

        [Header("Toast")]
        public TextMeshProUGUI toastText;
        public CanvasGroup toastCanvasGroup;
        public float toastDuration = 2.0f;
        public float toastDurationRare = 4.0f;
        public float fadeDuration = 0.5f;

        [Header("Rarity Colors")]
        public Color commonColor = Color.white;
        public Color uncommonColor = new Color(0.4f, 0.7f, 1.0f); // blue tint
        public Color rareColor = new Color(1.0f, 0.85f, 0.3f);    // gold tint

        [Header("Floor Indicator")]
        public TextMeshProUGUI floorIndicatorText;
        public GameObject floorIndicatorRoot;

        private Coroutine _toastCoroutine;
        private int _lastFloorIndex = -1;

        void OnEnable()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
                eventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
                eventBus.Subscribe<HiddenDoorRevealedEvent>(OnHiddenDoorRevealed);
                eventBus.Subscribe<FloorChangedEvent>(OnFloorChanged);
                eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            }
        }

        void OnDisable()
        {
            if (eventBus != null)
            {
                eventBus.Unsubscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
                eventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
                eventBus.Unsubscribe<HiddenDoorRevealedEvent>(OnHiddenDoorRevealed);
                eventBus.Unsubscribe<FloorChangedEvent>(OnFloorChanged);
                eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            }
        }

        void Update()
        {
            UpdateFloorIndicator();
        }

        // ===== Toast notifications =====

        private void ShowToast(string message, DiscoveryRarity rarity = DiscoveryRarity.Common)
        {
            if (toastText == null || toastCanvasGroup == null) return;

            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);

            toastText.text = message;
            toastText.color = rarity switch
            {
                DiscoveryRarity.Uncommon => uncommonColor,
                DiscoveryRarity.Rare => rareColor,
                _ => commonColor
            };
            _currentToastDuration = rarity == DiscoveryRarity.Rare ? toastDurationRare : toastDuration;
            _toastCoroutine = StartCoroutine(ToastRoutine());
        }

        private float _currentToastDuration;

        private IEnumerator ToastRoutine()
        {
            toastCanvasGroup.alpha = 1f;
            toastCanvasGroup.gameObject.SetActive(true);

            yield return new WaitForSeconds(_currentToastDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                toastCanvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.gameObject.SetActive(false);
            _toastCoroutine = null;
        }

        // ===== Floor indicator =====

        private void UpdateFloorIndicator()
        {
            if (floorIndicatorRoot == null) return;

            bool inside = interiorController != null && interiorController.IsInside;
            floorIndicatorRoot.SetActive(inside);

            if (!inside)
            {
                _lastFloorIndex = -1;
                return;
            }

            var renderer = interiorController.GetComponentInChildren<InteriorRenderer>();
            if (renderer == null) return;

            int currentFloor = renderer.CurrentFloorIndex;
            if (currentFloor != _lastFloorIndex)
            {
                _lastFloorIndex = currentFloor;
                string label = renderer.GetCurrentFloorLabel();
                if (floorIndicatorText != null)
                    floorIndicatorText.text = label;
            }
        }

        // ===== Event handlers =====

        private void OnDiscoveryCollected(DiscoveryCollectedEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.discoveryText))
            {
                ShowToast(evt.discoveryText, evt.rarity);
            }
            else
            {
                string typeName = evt.furnitureType.ToString();
                ShowToast($"Collected: {typeName}");
            }
        }

        private void OnDoorUnlocked(DoorUnlockedEvent evt)
        {
            ShowToast("Door Unlocked");
        }

        private void OnHiddenDoorRevealed(HiddenDoorRevealedEvent evt)
        {
            ShowToast("Hidden passage revealed!");
        }

        private void OnFloorChanged(FloorChangedEvent evt)
        {
            ShowToast($"Moved to {evt.floorLabel}");
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            string reward = evt.rewardValue > 0 ? $" (+{evt.rewardValue})" : "";
            ShowToast($"Quest Complete: {evt.title}{reward}", DiscoveryRarity.Rare);
        }
    }
}
