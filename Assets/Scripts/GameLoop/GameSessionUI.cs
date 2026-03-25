using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Programmatically built UI for the 5-minute play session.
    /// Four panels: Title, HUD, Pause, Results.
    /// </summary>
    public class GameSessionUI : MonoBehaviour
    {
        // ── Panel roots ──
        private GameObject _titlePanel;
        private GameObject _hudPanel;
        private GameObject _pausePanel;
        private GameObject _resultsPanel;

        // ── HUD elements ──
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _objectiveText;

        // ── Results elements ──
        private TextMeshProUGUI _resultsHeaderText;
        private TextMeshProUGUI _resultsBodyText;

        // ── Callbacks ──
        public event Action OnPlayClicked;
        public event Action OnResumeClicked;
        public event Action OnRestartClicked;
        public event Action OnQuitClicked;

        // ── Colors ──
        private static readonly Color OverlayColor = new(0.02f, 0.03f, 0.06f, 0.88f);
        private static readonly Color AccentColor = new(0.2f, 0.6f, 0.9f, 1f);
        private static readonly Color ButtonTextColor = Color.white;
        private static readonly Color TimerWarningColor = new(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color TimerNormalColor = Color.white;

        private bool _built;

        void Awake()
        {
            if (!_built) BuildUI(transform);
        }

        public void BuildUI(Transform parent)
        {
            if (_built) return;
            _built = true;

            BuildTitlePanel(parent);
            BuildHUDPanel(parent);
            BuildPausePanel(parent);
            BuildResultsPanel(parent);

            HideAll();
        }

        // ════════════════════════════════════════
        //  Panel visibility
        // ════════════════════════════════════════

        public void ShowTitle()
        {
            HideAll();
            _titlePanel.SetActive(true);
        }

        public void ShowHUD()
        {
            HideAll();
            _hudPanel.SetActive(true);
        }

        public void ShowPause()
        {
            // Keep HUD visible behind pause overlay
            _pausePanel.SetActive(true);
        }

        public void HidePause()
        {
            _pausePanel.SetActive(false);
        }

        public void ShowResults(SessionEndedEvent stats)
        {
            HideAll();
            _resultsPanel.SetActive(true);

            _resultsHeaderText.text = stats.timedOut ? "TIME'S UP" : "SESSION COMPLETE";

            int minutes = Mathf.FloorToInt(stats.elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(stats.elapsedTime % 60f);
            _resultsBodyText.text =
                $"Time Played: {minutes}:{seconds:D2}\n\n" +
                $"Buildings Entered: {stats.buildingsEntered}\n" +
                $"Fully Explored: {stats.buildingsCompleted}\n" +
                $"Discoveries: {stats.totalDiscoveries}";
        }

        // ════════════════════════════════════════
        //  HUD updates
        // ════════════════════════════════════════

        public void UpdateTimer(float remainingSeconds)
        {
            if (_timerText == null) return;

            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int secs = Mathf.FloorToInt(remainingSeconds % 60f);
            _timerText.text = $"{minutes}:{secs:D2}";

            if (remainingSeconds < 10f)
            {
                _timerText.color = TimerWarningColor;
                float pulse = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 8f);
                _timerText.transform.localScale = Vector3.one * pulse;
            }
            else if (remainingSeconds < 30f)
            {
                _timerText.color = TimerWarningColor;
                _timerText.transform.localScale = Vector3.one;
            }
            else
            {
                _timerText.color = TimerNormalColor;
                _timerText.transform.localScale = Vector3.one;
            }
        }

        public void UpdateObjective(int buildingsEntered, int discoveries)
        {
            if (_objectiveText != null)
                _objectiveText.text = $"Buildings: {buildingsEntered}  |  Discoveries: {discoveries}";
        }

        // ════════════════════════════════════════
        //  Panel builders
        // ════════════════════════════════════════

        private void BuildTitlePanel(Transform parent)
        {
            _titlePanel = CreateOverlayPanel(parent, "TitlePanel");

            var layout = _titlePanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 30f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Title text
            var titleGo = CreateTextElement(_titlePanel.transform, "TitleText",
                "MINI MAP GAME", 48, TextAlignmentOptions.Center);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(600f, 80f);

            // Subtitle
            var subtitleGo = CreateTextElement(_titlePanel.transform, "SubtitleText",
                "Explore. Discover. 5 Minutes.", 20, TextAlignmentOptions.Center);
            var subRect = subtitleGo.GetComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(400f, 40f);
            subtitleGo.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.7f, 0.8f);

            // Play button
            var playBtn = CreateButton(_titlePanel.transform, "PlayButton", "PLAY", 280f, 60f);
            playBtn.onClick.AddListener(() => OnPlayClicked?.Invoke());
        }

        private void BuildHUDPanel(Transform parent)
        {
            _hudPanel = new GameObject("HUDPanel");
            _hudPanel.transform.SetParent(parent, false);
            var rect = _hudPanel.AddComponent<RectTransform>();
            SetFullStretch(rect);
            // No background image — HUD is transparent overlay

            // Timer (top-center)
            var timerGo = CreateTextElement(_hudPanel.transform, "TimerText",
                "5:00", 36, TextAlignmentOptions.Center);
            var timerRect = timerGo.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0f, -20f);
            timerRect.sizeDelta = new Vector2(200f, 50f);
            _timerText = timerGo.GetComponent<TextMeshProUGUI>();

            // Add subtle shadow behind timer for readability
            var timerShadow = timerGo.AddComponent<Shadow>();
            timerShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            timerShadow.effectDistance = new Vector2(2f, -2f);

            // Objective text (below timer)
            var objGo = CreateTextElement(_hudPanel.transform, "ObjectiveText",
                "Buildings: 0  |  Discoveries: 0", 18, TextAlignmentOptions.Center);
            var objRect = objGo.GetComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 1f);
            objRect.anchorMax = new Vector2(0.5f, 1f);
            objRect.pivot = new Vector2(0.5f, 1f);
            objRect.anchoredPosition = new Vector2(0f, -72f);
            objRect.sizeDelta = new Vector2(400f, 30f);
            _objectiveText = objGo.GetComponent<TextMeshProUGUI>();
            _objectiveText.color = new Color(0.7f, 0.8f, 0.9f);

            var objShadow = objGo.AddComponent<Shadow>();
            objShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            objShadow.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildPausePanel(Transform parent)
        {
            _pausePanel = CreateOverlayPanel(parent, "PausePanel");

            var layout = _pausePanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Header
            var headerGo = CreateTextElement(_pausePanel.transform, "PauseHeader",
                "PAUSED", 42, TextAlignmentOptions.Center);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(300f, 60f);

            // Buttons
            var resumeBtn = CreateButton(_pausePanel.transform, "ResumeButton", "RESUME", 240f, 50f);
            resumeBtn.onClick.AddListener(() => OnResumeClicked?.Invoke());

            var restartBtn = CreateButton(_pausePanel.transform, "RestartButton", "RESTART", 240f, 50f);
            restartBtn.onClick.AddListener(() => OnRestartClicked?.Invoke());

            var quitBtn = CreateButton(_pausePanel.transform, "QuitButton", "QUIT", 240f, 50f);
            quitBtn.onClick.AddListener(() => OnQuitClicked?.Invoke());
        }

        private void BuildResultsPanel(Transform parent)
        {
            _resultsPanel = CreateOverlayPanel(parent, "ResultsPanel");

            var layout = _resultsPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Header
            var headerGo = CreateTextElement(_resultsPanel.transform, "ResultsHeader",
                "TIME'S UP", 42, TextAlignmentOptions.Center);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(400f, 60f);
            _resultsHeaderText = headerGo.GetComponent<TextMeshProUGUI>();

            // Stats body
            var bodyGo = CreateTextElement(_resultsPanel.transform, "ResultsBody",
                "", 22, TextAlignmentOptions.Center);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.sizeDelta = new Vector2(400f, 200f);
            _resultsBodyText = bodyGo.GetComponent<TextMeshProUGUI>();
            _resultsBodyText.color = new Color(0.75f, 0.82f, 0.9f);

            // Buttons
            var playAgainBtn = CreateButton(_resultsPanel.transform, "PlayAgainButton", "PLAY AGAIN", 260f, 50f);
            playAgainBtn.onClick.AddListener(() => OnRestartClicked?.Invoke());

            var quitBtn = CreateButton(_resultsPanel.transform, "QuitButton2", "QUIT", 260f, 50f);
            quitBtn.onClick.AddListener(() => OnQuitClicked?.Invoke());
        }

        // ════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════

        private void HideAll()
        {
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_hudPanel != null) _hudPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            SetFullStretch(rect);

            var img = panel.AddComponent<Image>();
            img.color = OverlayColor;
            img.raycastTarget = true;

            return panel;
        }

        private static GameObject CreateTextElement(Transform parent, string name,
            string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;

            return go;
        }

        private static Button CreateButton(Transform parent, string name,
            string label, float width, float height)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(width, height);

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = AccentColor;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = AccentColor;
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
            colors.pressedColor = new Color(0.15f, 0.45f, 0.7f);
            btn.colors = colors;

            var textGo = CreateTextElement(btnGo.transform, "Label",
                label, 22, TextAlignmentOptions.Center);
            var textRect = textGo.GetComponent<RectTransform>();
            SetFullStretch(textRect);
            textGo.GetComponent<TextMeshProUGUI>().color = ButtonTextColor;

            return btn;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
