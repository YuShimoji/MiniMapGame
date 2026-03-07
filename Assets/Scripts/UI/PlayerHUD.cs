using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniMapGame.Runtime;
using MiniMapGame.GameLoop;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Player HUD showing HP bar, compass, proximity info, and inventory summary.
    /// Subscribes to game events via MapEventBus for real-time updates.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public MapEventBus eventBus;
        public Transform playerTransform;

        [Header("HP Bar")]
        public Slider hpSlider;
        public Image hpFillImage;
        public TextMeshProUGUI hpText;

        [Header("Compass")]
        public TextMeshProUGUI compassText;

        [Header("Proximity Info")]
        public TextMeshProUGUI proximityText;

        [Header("Inventory Summary")]
        public TextMeshProUGUI inventoryText;

        private static readonly string[] Directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private static readonly Color HPHigh = new(0.2f, 0.85f, 0.3f);
        private static readonly Color HPMid = new(0.9f, 0.8f, 0.2f);
        private static readonly Color HPLow = new(0.9f, 0.2f, 0.2f);

        private GameLoopController _gameLoop;
        private ExtractionPoint[] _extractionPoints;
        private int _totalValueObjects;

        void Start()
        {
            _gameLoop = Object.FindAnyObjectByType<GameLoopController>();

            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;

            eventBus?.Subscribe<ValueCollectedEvent>(OnValueCollected);
            eventBus?.Subscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            eventBus?.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            eventBus?.Subscribe<GameLoopStartedEvent>(OnGameStarted);

            UpdateHP(100, 100);
        }

        void OnDestroy()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;

            eventBus?.Unsubscribe<ValueCollectedEvent>(OnValueCollected);
            eventBus?.Unsubscribe<EncounterTriggeredEvent>(OnEncounterTriggered);
            eventBus?.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            eventBus?.Unsubscribe<GameLoopStartedEvent>(OnGameStarted);
        }

        void Update()
        {
            UpdateCompass();
            UpdateProximity();
        }

        private void OnMapGenerated(Data.MapData data)
        {
            _extractionPoints = null; // Will be refreshed lazily
            _totalValueObjects = data.analysis.deadEndIndices.Count;
            UpdateAllUI();
        }

        private void OnGameStarted(GameLoopStartedEvent evt)
        {
            UpdateAllUI();
        }

        private void OnValueCollected(ValueCollectedEvent evt)
        {
            UpdateInventory();
        }

        private void OnEncounterTriggered(EncounterTriggeredEvent evt)
        {
            UpdateInventory();
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            UpdateHP(evt.remainingHP, evt.maxHP);
        }

        private void UpdateAllUI()
        {
            if (_gameLoop != null)
            {
                var stats = _gameLoop.State.stats;
                UpdateHP(stats.currentHP, stats.maxHP);
                UpdateInventory();
            }
        }

        private void UpdateHP(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0f;

            if (hpSlider != null)
                hpSlider.value = ratio;

            if (hpFillImage != null)
            {
                if (ratio > 0.5f)
                    hpFillImage.color = Color.Lerp(HPMid, HPHigh, (ratio - 0.5f) * 2f);
                else
                    hpFillImage.color = Color.Lerp(HPLow, HPMid, ratio * 2f);
            }

            if (hpText != null)
                hpText.text = $"HP: {current}/{max}";
        }

        private void UpdateCompass()
        {
            if (compassText == null || playerTransform == null) return;

            float angle = playerTransform.eulerAngles.y;
            int idx = Mathf.RoundToInt(angle / 45f) % 8;
            if (idx < 0) idx += 8;
            compassText.text = Directions[idx];
        }

        private void UpdateProximity()
        {
            if (proximityText == null || playerTransform == null) return;

            // Lazily find extraction points
            if (_extractionPoints == null)
                _extractionPoints = Object.FindObjectsByType<ExtractionPoint>(FindObjectsSortMode.None);

            if (_extractionPoints.Length == 0)
            {
                proximityText.text = "Exit: ---";
                return;
            }

            float minDist = float.MaxValue;
            foreach (var ep in _extractionPoints)
            {
                if (ep == null) continue;
                float d = Vector3.Distance(playerTransform.position, ep.transform.position);
                if (d < minDist) minDist = d;
            }

            proximityText.text = $"Exit: {Mathf.RoundToInt(minDist)}m";
        }

        private void UpdateInventory()
        {
            if (inventoryText == null || _gameLoop == null) return;

            var s = _gameLoop.State;
            int remaining = _totalValueObjects - s.collectedItemIds.Count;
            inventoryText.text = $"V:{s.collectedValue}  Items:{s.collectedItemIds.Count}  Left:{remaining}";
        }
    }
}
