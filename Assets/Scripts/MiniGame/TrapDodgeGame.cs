using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Alcove room mini-game: QTE-style directional arrow pressing.
    /// 5 rounds with decreasing time window. 4/5 correct = success.
    /// </summary>
    public class TrapDodgeGame : IMiniGame
    {
        public MiniGameType Type => MiniGameType.TrapDodge;

        private RectTransform _uiRoot;
        private GameObject _container;

        // UI
        private TextMeshProUGUI _arrowText;
        private TextMeshProUGUI _countText;
        private TextMeshProUGUI _resultText;
        private Image _timerBar;
        private Image _timerBarBG;

        // State
        private int _currentRound;
        private int _totalRounds = 5;
        private int _successes;
        private float _timeWindow;
        private float _roundTimer;
        private Direction _requiredDir;
        private bool _waitingForInput;
        private bool _showingResult;
        private float _resultTimer;
        private bool _finished;
        private System.Random _rng;

        private const float ResultDisplayTime = 0.6f;
        private static readonly float[] TimeWindows = { 1.5f, 1.3f, 1.1f, 0.9f, 0.8f };
        private static readonly string[] ArrowChars = { "\u2191", "\u2193", "\u2190", "\u2192" };
        private static readonly KeyCode[] ArrowKeys =
            { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

        private enum Direction { Up, Down, Left, Right }

        public void Begin(MiniGameContext context, RectTransform uiRoot)
        {
            _uiRoot = uiRoot;
            _currentRound = 0;
            _successes = 0;
            _finished = false;
            _rng = new System.Random(context.seed);

            BuildUI();
            StartRound();
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
                    StartRound();
                }
                return true;
            }

            if (_waitingForInput)
            {
                _roundTimer -= deltaTime;
                UpdateTimerBar();

                if (_roundTimer <= 0f)
                {
                    ShowRoundResult(false);
                    return true;
                }

                // Check all arrow keys
                for (int i = 0; i < ArrowKeys.Length; i++)
                {
                    if (Input.GetKeyDown(ArrowKeys[i]))
                    {
                        bool correct = (Direction)i == _requiredDir;
                        if (correct) _successes++;
                        ShowRoundResult(correct);
                        return true;
                    }
                }

                // Also check WASD
                if (Input.GetKeyDown(KeyCode.W))
                {
                    bool c = _requiredDir == Direction.Up; if (c) _successes++;
                    ShowRoundResult(c); return true;
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    bool c = _requiredDir == Direction.Down; if (c) _successes++;
                    ShowRoundResult(c); return true;
                }
                if (Input.GetKeyDown(KeyCode.A))
                {
                    bool c = _requiredDir == Direction.Left; if (c) _successes++;
                    ShowRoundResult(c); return true;
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    bool c = _requiredDir == Direction.Right; if (c) _successes++;
                    ShowRoundResult(c); return true;
                }
            }

            return true;
        }

        public MiniGameResult GetResult()
        {
            return new MiniGameResult
            {
                success = _successes >= 4,
                score = _successes * 15
            };
        }

        public void Cleanup()
        {
            if (_container != null)
                Object.Destroy(_container);
            _container = null;
        }

        private void BuildUI()
        {
            _container = new GameObject("TrapDodgeUI");
            _container.transform.SetParent(_uiRoot, false);
            var containerRect = _container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            // Overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(_container.transform, false);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            // Title
            var title = CreateText(_container.transform, "Title",
                "\u77e2\u5370\u306e\u65b9\u5411\u30ad\u30fc\u3067\u56de\u907f\uff01", 22, TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.75f);
            titleRect.anchorMax = new Vector2(0.5f, 0.75f);
            titleRect.sizeDelta = new Vector2(400, 40);
            title.color = new Color(0.7f, 0.7f, 0.7f);

            // Arrow display
            _arrowText = CreateText(_container.transform, "Arrow",
                "", 80, TextAlignmentOptions.Center);
            var arrowRect = _arrowText.rectTransform;
            arrowRect.anchorMin = new Vector2(0.5f, 0.55f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.55f);
            arrowRect.sizeDelta = new Vector2(200, 120);

            // Count text
            _countText = CreateText(_container.transform, "Count",
                "", 24, TextAlignmentOptions.Center);
            var countRect = _countText.rectTransform;
            countRect.anchorMin = new Vector2(0.5f, 0.4f);
            countRect.anchorMax = new Vector2(0.5f, 0.4f);
            countRect.sizeDelta = new Vector2(300, 40);

            // Timer bar BG
            var barBGGO = new GameObject("TimerBarBG");
            barBGGO.transform.SetParent(_container.transform, false);
            _timerBarBG = barBGGO.AddComponent<Image>();
            _timerBarBG.color = new Color(0.15f, 0.15f, 0.2f);
            var barBGRect = _timerBarBG.rectTransform;
            barBGRect.anchorMin = new Vector2(0.5f, 0.32f);
            barBGRect.anchorMax = new Vector2(0.5f, 0.32f);
            barBGRect.sizeDelta = new Vector2(300, 12);

            // Timer bar fill
            var barFillGO = new GameObject("TimerBarFill");
            barFillGO.transform.SetParent(barBGGO.transform, false);
            _timerBar = barFillGO.AddComponent<Image>();
            _timerBar.color = new Color(0.3f, 0.8f, 1f);
            var barFillRect = _timerBar.rectTransform;
            barFillRect.anchorMin = new Vector2(0f, 0f);
            barFillRect.anchorMax = new Vector2(1f, 1f);
            barFillRect.sizeDelta = Vector2.zero;
            barFillRect.pivot = new Vector2(0f, 0.5f);

            // Result text
            _resultText = CreateText(_container.transform, "Result",
                "", 36, TextAlignmentOptions.Center);
            var resRect = _resultText.rectTransform;
            resRect.anchorMin = new Vector2(0.5f, 0.55f);
            resRect.anchorMax = new Vector2(0.5f, 0.55f);
            resRect.sizeDelta = new Vector2(400, 60);
            _resultText.gameObject.SetActive(false);
        }

        private void StartRound()
        {
            _requiredDir = (Direction)_rng.Next(4);
            _timeWindow = TimeWindows[Mathf.Min(_currentRound, TimeWindows.Length - 1)];
            _roundTimer = _timeWindow;
            _waitingForInput = true;
            _showingResult = false;

            _arrowText.text = ArrowChars[(int)_requiredDir];
            _arrowText.color = Color.white;
            _arrowText.gameObject.SetActive(true);
            _countText.text = $"{_currentRound + 1}/{_totalRounds}  \u6210\u529f: {_successes}";
            UpdateTimerBar();
        }

        private void ShowRoundResult(bool correct)
        {
            _waitingForInput = false;
            _arrowText.gameObject.SetActive(false);

            _resultText.gameObject.SetActive(true);
            _resultText.text = correct ? "\u2714 \u56de\u907f!" : "\u2718 \u88ab\u5f3e!";
            _resultText.color = correct
                ? new Color(0.3f, 1f, 0.4f)
                : new Color(1f, 0.3f, 0.3f);

            _countText.text = $"{_currentRound + 1}/{_totalRounds}  \u6210\u529f: {_successes}";

            _showingResult = true;
            _resultTimer = ResultDisplayTime;
        }

        private void UpdateTimerBar()
        {
            if (_timerBar == null) return;
            float ratio = Mathf.Clamp01(_roundTimer / _timeWindow);
            _timerBar.rectTransform.anchorMax = new Vector2(ratio, 1f);

            _timerBar.color = ratio > 0.5f
                ? new Color(0.3f, 0.8f, 1f)
                : ratio > 0.25f
                    ? new Color(1f, 0.8f, 0.2f)
                    : new Color(1f, 0.3f, 0.2f);
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
