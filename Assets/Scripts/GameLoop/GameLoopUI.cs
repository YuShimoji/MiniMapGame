using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MiniMapGame.GameLoop
{
    public class GameLoopUI : MonoBehaviour
    {
        [Header("HUD")]
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI encounterText;
        public TextMeshProUGUI itemCountText;

        [Header("Message Overlay")]
        public TextMeshProUGUI messageText;
        public float messageDuration = 2f;

        [Header("Extraction Decision")]
        public GameObject extractionPanel;
        public TextMeshProUGUI extractionInfoText;
        public Button extractButton;
        public Button continueButton;

        [Header("Result Screen")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;

        private float _messageTimer;

        void Start()
        {
            if (messageText != null) messageText.gameObject.SetActive(false);
            if (extractionPanel != null) extractionPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        void Update()
        {
            if (_messageTimer <= 0) return;
            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0 && messageText != null)
                messageText.gameObject.SetActive(false);
        }

        public void ShowHUD(GameState state)
        {
            UpdateHUD(state);
        }

        public void UpdateHUD(GameState state)
        {
            if (valueText != null) valueText.text = $"Value: {state.collectedValue}";
            if (encounterText != null) encounterText.text = $"Encounters: {state.encounterCount}";
            if (itemCountText != null) itemCountText.text = $"Items: {state.collectedItemIds.Count}";
        }

        public void ShowEncounterMessage(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            _messageTimer = messageDuration;
        }

        public void ShowExtractionDecision(GameState state, Action onExtract, Action onContinue)
        {
            if (extractionPanel == null) return;
            extractionPanel.SetActive(true);

            if (extractionInfoText != null)
                extractionInfoText.text =
                    $"Collected: {state.collectedValue}\nItems: {state.collectedItemIds.Count}\n\nExtract?";

            if (extractButton != null)
            {
                extractButton.onClick.RemoveAllListeners();
                extractButton.onClick.AddListener(() => onExtract?.Invoke());
            }
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => onContinue?.Invoke());
            }
        }

        public void HideExtractionDecision()
        {
            if (extractionPanel != null) extractionPanel.SetActive(false);
        }

        public void ShowExtractionResult(GameState state)
        {
            HideExtractionDecision();
            if (resultPanel == null) return;
            resultPanel.SetActive(true);
            if (resultText != null)
                resultText.text =
                    $"Extraction Successful!\n\n" +
                    $"Final Value: {state.collectedValue}\n" +
                    $"Encounters: {state.encounterCount}\n" +
                    $"Items: {state.collectedItemIds.Count}";
        }
    }
}
