using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Interior;
using MiniMapGame.Player;
using MiniMapGame.Runtime;
using MiniMapGame.UI;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Orchestrates the 5-minute play session lifecycle.
    /// Title → Playing → Paused → Results → Restart loop.
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        public enum SessionPhase { Title, Playing, Paused, Results }

        [Header("References")]
        public MapManager mapManager;
        public MapEventBus eventBus;
        public ExplorationProgressManager explorationProgress;
        public GameSessionUI sessionUI;

        [Header("Optional")]
        [Tooltip("Hide during gameplay")]
        public MapControlUI mapControlUI;
        public QuestManager questManager;

        [Header("Session Settings")]
        public float sessionDuration = 300f;

        public SessionPhase Phase => _phase;

        private SessionPhase _phase = SessionPhase.Title;
        private float _remainingTime;
        private int _buildingsEntered;
        private int _totalDiscoveries;
        private PlayerMovement _playerMovement;
        private bool _mapReady;
        private bool _waitingForMap;

        // ════════════════════════════════════════
        //  Lifecycle
        // ════════════════════════════════════════

        void Start()
        {
            _playerMovement = FindAnyObjectByType<PlayerMovement>();

            if (sessionUI != null)
            {
                sessionUI.OnPlayClicked += StartSession;
                sessionUI.OnResumeClicked += ResumeSession;
                sessionUI.OnRestartClicked += RestartSession;
                sessionUI.OnQuitClicked += QuitGame;
            }

            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;

            if (eventBus != null)
                eventBus.Subscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);

            // Show title — map may already be generating from MapManager.Start()
            SetPhase(SessionPhase.Title);
        }

        void OnDestroy()
        {
            if (sessionUI != null)
            {
                sessionUI.OnPlayClicked -= StartSession;
                sessionUI.OnResumeClicked -= ResumeSession;
                sessionUI.OnRestartClicked -= RestartSession;
                sessionUI.OnQuitClicked -= QuitGame;
            }

            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;

            if (eventBus != null)
                eventBus.Unsubscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
        }

        void Update()
        {
            if (_phase == SessionPhase.Playing)
            {
                _remainingTime -= Time.deltaTime;
                sessionUI?.UpdateTimer(_remainingTime);

                if (_remainingTime <= 0f)
                {
                    _remainingTime = 0f;
                    EndSession(timedOut: true);
                    return;
                }
            }

            // ESC key: pause/resume toggle (only during Playing/Paused)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_phase == SessionPhase.Playing)
                    PauseSession();
                else if (_phase == SessionPhase.Paused)
                    ResumeSession();
            }
        }

        // ════════════════════════════════════════
        //  Session control
        // ════════════════════════════════════════

        private void StartSession()
        {
            // Reset stats
            _buildingsEntered = 0;
            _totalDiscoveries = 0;

            // Clear previous exploration progress
            if (explorationProgress != null)
                explorationProgress.RestoreRecords(null);

            // Generate a new map with random seed
            if (mapManager != null)
            {
                int newSeed = Random.Range(0, 100000);
                _mapReady = false;
                _waitingForMap = true;
                mapManager.Generate(newSeed);
                // OnMapGenerated callback will complete the transition
            }

            // Generate() may have completed synchronously
            if (_mapReady)
                TransitionToPlaying();
        }

        private void TransitionToPlaying()
        {
            _waitingForMap = false;
            _remainingTime = sessionDuration;

            // Enable player
            if (_playerMovement != null)
                _playerMovement.enabled = true;

            // Hide debug UI
            if (mapControlUI != null)
                mapControlUI.gameObject.SetActive(false);

            // Ensure time flows
            Time.timeScale = 1f;

            SetPhase(SessionPhase.Playing);

            // Publish event
            eventBus?.Publish(new SessionStartedEvent { sessionDuration = sessionDuration });
        }

        private void PauseSession()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPhase(SessionPhase.Paused);
        }

        private void ResumeSession()
        {
            Time.timeScale = 1f;
            SetPhase(SessionPhase.Playing);
        }

        private void EndSession(bool timedOut)
        {
            Time.timeScale = 1f;

            // Disable player
            if (_playerMovement != null)
                _playerMovement.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Gather final stats
            GatherStats();

            var stats = new SessionEndedEvent
            {
                elapsedTime = sessionDuration - _remainingTime,
                buildingsEntered = _buildingsEntered,
                buildingsCompleted = CountCompletedBuildings(),
                totalDiscoveries = _totalDiscoveries,
                questsCompleted = questManager != null ? questManager.CompletedCount : 0,
                questsTotal = questManager != null ? questManager.Definitions.Count : 0,
                timedOut = timedOut
            };

            eventBus?.Publish(stats);
            SetPhase(SessionPhase.Results, stats);
        }

        private void RestartSession()
        {
            Time.timeScale = 1f;
            SetPhase(SessionPhase.Title);
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ════════════════════════════════════════
        //  Phase transitions
        // ════════════════════════════════════════

        private void SetPhase(SessionPhase newPhase, SessionEndedEvent? stats = null)
        {
            _phase = newPhase;

            switch (newPhase)
            {
                case SessionPhase.Title:
                    if (_playerMovement != null)
                        _playerMovement.enabled = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    sessionUI?.ShowTitle();
                    break;

                case SessionPhase.Playing:
                    sessionUI?.ShowHUD();
                    sessionUI?.UpdateTimer(_remainingTime);
                    sessionUI?.UpdateObjective(_buildingsEntered, _totalDiscoveries);
                    break;

                case SessionPhase.Paused:
                    sessionUI?.ShowPause();
                    break;

                case SessionPhase.Results:
                    if (stats.HasValue)
                        sessionUI?.ShowResults(stats.Value);
                    break;
            }
        }

        // ════════════════════════════════════════
        //  Event handlers
        // ════════════════════════════════════════

        private void OnMapGenerated(MapData mapData)
        {
            _mapReady = true;

            // If Play was clicked and we were waiting for map generation
            if (_waitingForMap)
            {
                _waitingForMap = false;
                TransitionToPlaying();
            }
        }

        private void OnDiscoveryCollected(DiscoveryCollectedEvent evt)
        {
            if (_phase != SessionPhase.Playing) return;

            _totalDiscoveries++;
            GatherBuildingCount();
            sessionUI?.UpdateObjective(_buildingsEntered, _totalDiscoveries);
        }

        // ════════════════════════════════════════
        //  Stats
        // ════════════════════════════════════════

        private void GatherStats()
        {
            GatherBuildingCount();
        }

        private void GatherBuildingCount()
        {
            if (explorationProgress == null) return;

            int entered = 0;
            foreach (var kvp in explorationProgress.GetAllRecords())
            {
                if (kvp.Value.hasEntered)
                    entered++;
            }
            _buildingsEntered = entered;
        }

        private int CountCompletedBuildings()
        {
            if (explorationProgress == null) return 0;

            int completed = 0;
            foreach (var kvp in explorationProgress.GetAllRecords())
            {
                if (kvp.Value.IsComplete)
                    completed++;
            }
            return completed;
        }
    }
}
