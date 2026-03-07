using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Boss room mini-game: Power meter oscillates left-right, press Space to stop.
    /// 3 rounds with increasing speed. Hit green zone (±15%) to score.
    /// </summary>
    public class TimingCombatGame : IMiniGame
    {
        public MiniGameType Type => MiniGameType.TimingCombat;

        private RectTransform _uiRoot;
        private GameObject _container;

        // UI elements
        private Image _barBackground;
        private Image _indicator;
        private Image _targetZone;
        private TextMeshProUGUI _roundText;
        private TextMeshProUGUI _resultText;

        // Game state
        private int _currentRound;
        private int _totalRounds = 3;
        private int _hits;
        private float _indicatorPos; // 0..1
        private float _speed;
        private bool _movingRight = true;
        private bool _waitingForInput;
        private bool _showingResult;
        private float _resultTimer;
        private bool _finished;
        private float _targetCenter = 0.5f;

        private const float TargetHalfWidth = 0.15f;
        private const float ResultDisplayTime = 0.8f;
        private static readonly float[] RoundSpeeds = { 0.8f, 1.2f, 1.8f };

        public void Begin(MiniGameContext context, RectTransform uiRoot)
        {
            _uiRoot = uiRoot;
            _currentRound = 0;
            _hits = 0;
            _finished = false;

            var rng = new System.Random(context.seed);
            BuildUI(rng);
            StartRound(rng);
        }

        public bool Tick(float deltaTime)
        {
            if (_finished) return false;

            if (_showingResult)
            {
                _resultTimer -= deltaTime;
                if (_resultTimer <= 0f)
                {
                    _showingResult = false;
                    _resultText.gameObject.SetActive(false);
                    _currentRound++;

                    if (_currentRound >= _totalRounds)
                    {
                        _finished = true;
                        return false;
                    }

                    var rng = new System.Random(_currentRound * 7919);
                    StartRound(rng);
                }
                return true;
            }

            if (_waitingForInput)
            {
                // Oscillate indicator
                float delta = _speed * deltaTime;
                if (_movingRight)
                {
                    _indicatorPos += delta;
                    if (_indicatorPos >= 1f) { _indicatorPos = 1f; _movingRight = false; }
                }
                else
                {
                    _indicatorPos -= delta;
                    if (_indicatorPos <= 0f) { _indicatorPos = 0f; _movingRight = true; }
                }

                UpdateIndicatorVisual();

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    EvaluateHit();
                }
            }

            return true;
        }

        public MiniGameResult GetResult()
        {
            return new MiniGameResult
            {
                success = _hits >= 2,
                score = _hits * 20
            };
        }

        public void Cleanup()
        {
            if (_container != null)
                Object.Destroy(_container);
            _container = null;
        }

        private void BuildUI(System.Random rng)
        {
            _container = new GameObject("TimingCombatUI");
            _container.transform.SetParent(_uiRoot, false);
            var containerRect = _container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            // Semi-transparent background overlay
            var overlay = CreateImage(_container.transform, "Overlay",
                new Color(0f, 0f, 0f, 0.6f));
            var overlayRect = overlay.rectTransform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Title
            _roundText = CreateText(_container.transform, "RoundText",
                "", 28, TextAlignmentOptions.Center);
            var rtRect = _roundText.rectTransform;
            rtRect.anchorMin = new Vector2(0.5f, 0.7f);
            rtRect.anchorMax = new Vector2(0.5f, 0.7f);
            rtRect.sizeDelta = new Vector2(400, 50);

            // Bar background
            _barBackground = CreateImage(_container.transform, "BarBG",
                new Color(0.15f, 0.15f, 0.2f));
            var barRect = _barBackground.rectTransform;
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(500, 40);

            // Target zone (green)
            _targetZone = CreateImage(_barBackground.transform, "TargetZone",
                new Color(0.2f, 0.8f, 0.3f, 0.5f));
            var tzRect = _targetZone.rectTransform;
            tzRect.anchorMin = new Vector2(0f, 0f);
            tzRect.anchorMax = new Vector2(1f, 1f);
            tzRect.sizeDelta = Vector2.zero;

            // Indicator (white line)
            _indicator = CreateImage(_barBackground.transform, "Indicator",
                Color.white);
            var indRect = _indicator.rectTransform;
            indRect.anchorMin = new Vector2(0f, 0f);
            indRect.anchorMax = new Vector2(0f, 1f);
            indRect.sizeDelta = new Vector2(4, 0);
            indRect.pivot = new Vector2(0.5f, 0.5f);

            // Result text
            _resultText = CreateText(_container.transform, "ResultText",
                "", 36, TextAlignmentOptions.Center);
            var resRect = _resultText.rectTransform;
            resRect.anchorMin = new Vector2(0.5f, 0.35f);
            resRect.anchorMax = new Vector2(0.5f, 0.35f);
            resRect.sizeDelta = new Vector2(400, 60);
            _resultText.gameObject.SetActive(false);

            // Instruction
            var instrText = CreateText(_container.transform, "InstrText",
                "[Space] \u3067\u30bf\u30a4\u30df\u30f3\u30b0\u3092\u5408\u308f\u305b\u308d\uff01", 20, TextAlignmentOptions.Center);
            var instrRect = instrText.rectTransform;
            instrRect.anchorMin = new Vector2(0.5f, 0.25f);
            instrRect.anchorMax = new Vector2(0.5f, 0.25f);
            instrRect.sizeDelta = new Vector2(400, 40);
            instrText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void StartRound(System.Random rng)
        {
            _indicatorPos = 0f;
            _movingRight = true;
            _waitingForInput = true;
            _showingResult = false;
            _speed = RoundSpeeds[Mathf.Min(_currentRound, RoundSpeeds.Length - 1)];

            // Randomize target center (0.25..0.75)
            _targetCenter = 0.25f + (float)rng.NextDouble() * 0.5f;
            UpdateTargetZoneVisual();

            _roundText.text = $"\u30e9\u30a6\u30f3\u30c9 {_currentRound + 1}/{_totalRounds}  \u30d2\u30c3\u30c8: {_hits}";
            UpdateIndicatorVisual();
        }

        private void EvaluateHit()
        {
            _waitingForInput = false;

            float dist = Mathf.Abs(_indicatorPos - _targetCenter);
            bool hit = dist <= TargetHalfWidth;

            if (hit) _hits++;

            _resultText.gameObject.SetActive(true);
            _resultText.text = hit ? "\u2714 HIT!" : "\u2718 MISS";
            _resultText.color = hit ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);

            _roundText.text = $"\u30e9\u30a6\u30f3\u30c9 {_currentRound + 1}/{_totalRounds}  \u30d2\u30c3\u30c8: {_hits}";

            _showingResult = true;
            _resultTimer = ResultDisplayTime;
        }

        private void UpdateIndicatorVisual()
        {
            if (_indicator == null) return;
            var rect = _indicator.rectTransform;
            rect.anchorMin = new Vector2(_indicatorPos, 0f);
            rect.anchorMax = new Vector2(_indicatorPos, 1f);
        }

        private void UpdateTargetZoneVisual()
        {
            if (_targetZone == null) return;
            var rect = _targetZone.rectTransform;
            float left = Mathf.Clamp01(_targetCenter - TargetHalfWidth);
            float right = Mathf.Clamp01(_targetCenter + TargetHalfWidth);
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }
    }
}
