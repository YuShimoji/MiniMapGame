using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Treasure room mini-game: 3x4 card matching with 6 symbol pairs.
    /// Find all pairs within 30 seconds to win.
    /// </summary>
    public class MemoryMatchGame : IMiniGame
    {
        public MiniGameType Type => MiniGameType.MemoryMatch;

        private RectTransform _uiRoot;
        private GameObject _container;

        // UI
        private TextMeshProUGUI _timerText;
        private readonly List<Button> _cardButtons = new();
        private readonly List<Image> _cardImages = new();
        private readonly List<TextMeshProUGUI> _cardTexts = new();

        // State
        private int[] _cardSymbols; // symbol index per card
        private bool[] _matched;
        private int _firstPick = -1;
        private int _secondPick = -1;
        private float _flipBackTimer;
        private float _timeRemaining;
        private int _matchedPairs;
        private bool _finished;
        private bool _success;

        private const float TimeLimit = 30f;
        private const float FlipBackDelay = 1.0f;
        private const int Rows = 3;
        private const int Cols = 4;
        private const int TotalCards = Rows * Cols;
        private const int PairCount = TotalCards / 2;

        private static readonly string[] Symbols =
            { "\u2660", "\u2665", "\u2666", "\u2663", "\u2605", "\u25cf" };
        private static readonly Color[] SymbolColors =
        {
            new(0.3f, 0.5f, 1f), new(1f, 0.3f, 0.3f), new(1f, 0.8f, 0.2f),
            new(0.3f, 0.9f, 0.4f), new(0.9f, 0.5f, 1f), new(1f, 0.6f, 0.3f)
        };

        public void Begin(MiniGameContext context, RectTransform uiRoot)
        {
            _uiRoot = uiRoot;
            _finished = false;
            _success = false;
            _matchedPairs = 0;
            _firstPick = -1;
            _secondPick = -1;
            _flipBackTimer = 0f;
            _timeRemaining = TimeLimit;

            ShuffleCards(context.seed);
            _matched = new bool[TotalCards];
            BuildUI();
        }

        public bool Tick(float deltaTime)
        {
            if (_finished) return false;

            _timeRemaining -= deltaTime;
            if (_timerText != null)
                _timerText.text = $"\u6b8b\u308a {_timeRemaining:F1}\u79d2";

            if (_timeRemaining <= 0f)
            {
                _finished = true;
                _success = false;
                return false;
            }

            // Handle flip-back delay
            if (_secondPick >= 0)
            {
                _flipBackTimer -= deltaTime;
                if (_flipBackTimer <= 0f)
                {
                    // Check match
                    if (_cardSymbols[_firstPick] == _cardSymbols[_secondPick])
                    {
                        _matched[_firstPick] = true;
                        _matched[_secondPick] = true;
                        SetCardState(_firstPick, CardState.Matched);
                        SetCardState(_secondPick, CardState.Matched);
                        _matchedPairs++;

                        if (_matchedPairs >= PairCount)
                        {
                            _finished = true;
                            _success = true;
                            return false;
                        }
                    }
                    else
                    {
                        SetCardState(_firstPick, CardState.FaceDown);
                        SetCardState(_secondPick, CardState.FaceDown);
                    }

                    _firstPick = -1;
                    _secondPick = -1;
                }
            }

            return true;
        }

        public MiniGameResult GetResult()
        {
            return new MiniGameResult
            {
                success = _success,
                score = _success ? Mathf.CeilToInt(_timeRemaining * 5f) : 0
            };
        }

        public void Cleanup()
        {
            _cardButtons.Clear();
            _cardImages.Clear();
            _cardTexts.Clear();
            if (_container != null)
                Object.Destroy(_container);
            _container = null;
        }

        private void ShuffleCards(int seed)
        {
            _cardSymbols = new int[TotalCards];
            for (int i = 0; i < PairCount; i++)
            {
                _cardSymbols[i * 2] = i;
                _cardSymbols[i * 2 + 1] = i;
            }

            var rng = new System.Random(seed);
            for (int i = TotalCards - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (_cardSymbols[i], _cardSymbols[j]) = (_cardSymbols[j], _cardSymbols[i]);
            }
        }

        private void BuildUI()
        {
            _container = new GameObject("MemoryMatchUI");
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

            // Timer
            _timerText = CreateText(_container.transform, "Timer", "", 24,
                TextAlignmentOptions.Center);
            var timerRect = _timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.5f, 0.85f);
            timerRect.anchorMax = new Vector2(0.5f, 0.85f);
            timerRect.sizeDelta = new Vector2(300, 40);

            // Title
            var title = CreateText(_container.transform, "Title",
                "\u30ab\u30fc\u30c9\u3092\u3081\u304f\u3063\u3066\u30da\u30a2\u3092\u63a2\u305b\uff01", 22, TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.78f);
            titleRect.anchorMax = new Vector2(0.5f, 0.78f);
            titleRect.sizeDelta = new Vector2(400, 40);
            title.color = new Color(0.7f, 0.7f, 0.7f);

            // Grid container
            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(_container.transform, false);
            var gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(Cols * 90, Rows * 100);

            var grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80, 90);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Cols;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Cards
            for (int i = 0; i < TotalCards; i++)
            {
                var cardGO = new GameObject($"Card_{i}");
                cardGO.transform.SetParent(gridGO.transform, false);

                var cardImg = cardGO.AddComponent<Image>();
                cardImg.color = new Color(0.2f, 0.25f, 0.35f);
                _cardImages.Add(cardImg);

                var btn = cardGO.AddComponent<Button>();
                int cardIdx = i;
                btn.onClick.AddListener(() => OnCardClicked(cardIdx));
                _cardButtons.Add(btn);

                var symbolText = CreateText(cardGO.transform, "Symbol", "?", 32,
                    TextAlignmentOptions.Center);
                symbolText.rectTransform.anchorMin = Vector2.zero;
                symbolText.rectTransform.anchorMax = Vector2.one;
                symbolText.rectTransform.sizeDelta = Vector2.zero;
                symbolText.color = new Color(0.5f, 0.5f, 0.5f);
                _cardTexts.Add(symbolText);
            }
        }

        private void OnCardClicked(int index)
        {
            if (_finished || _matched[index]) return;
            if (_secondPick >= 0) return; // waiting for flip-back
            if (index == _firstPick) return;

            SetCardState(index, CardState.FaceUp);

            if (_firstPick < 0)
            {
                _firstPick = index;
            }
            else
            {
                _secondPick = index;
                _flipBackTimer = FlipBackDelay;
            }
        }

        private enum CardState { FaceDown, FaceUp, Matched }

        private void SetCardState(int index, CardState state)
        {
            var img = _cardImages[index];
            var txt = _cardTexts[index];
            int sym = _cardSymbols[index];

            switch (state)
            {
                case CardState.FaceDown:
                    img.color = new Color(0.2f, 0.25f, 0.35f);
                    txt.text = "?";
                    txt.color = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case CardState.FaceUp:
                    img.color = new Color(0.3f, 0.35f, 0.45f);
                    txt.text = Symbols[sym];
                    txt.color = SymbolColors[sym];
                    break;
                case CardState.Matched:
                    img.color = new Color(0.15f, 0.3f, 0.15f, 0.5f);
                    txt.text = Symbols[sym];
                    txt.color = SymbolColors[sym] * 0.6f;
                    if (_cardButtons[index] != null)
                        _cardButtons[index].interactable = false;
                    break;
            }
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
