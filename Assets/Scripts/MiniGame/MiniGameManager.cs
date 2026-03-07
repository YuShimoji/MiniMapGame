using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiniMapGame.GameLoop;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Manages mini-game lifecycle: start, tick, end, reward/damage via MapEventBus.
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        public Canvas miniGameCanvas;
        public RectTransform uiRoot;
        public MapEventBus eventBus;

        private readonly Dictionary<MiniGameType, IMiniGame> _games = new();
        private readonly HashSet<string> _completedRooms = new();
        private IMiniGame _activeGame;
        private MiniGameContext _activeContext;
        private float _elapsed;

        public bool IsPlaying => _activeGame != null;

        public void RegisterGame(IMiniGame game)
        {
            _games[game.Type] = game;
        }

        public void StartMiniGame(MiniGameContext context)
        {
            if (_activeGame != null) return;

            string roomKey = $"{context.buildingId}_{context.roomIndex}";
            if (_completedRooms.Contains(roomKey)) return;

            if (!_games.TryGetValue(context.type, out var game)) return;

            _activeGame = game;
            _activeContext = context;
            _elapsed = 0f;

            if (miniGameCanvas != null)
                miniGameCanvas.gameObject.SetActive(true);

            _activeGame.Begin(context, uiRoot);
        }

        private void Update()
        {
            if (_activeGame == null) return;

            _elapsed += Time.deltaTime;

            if (!_activeGame.Tick(Time.deltaTime))
            {
                EndCurrentGame();
            }
        }

        private void EndCurrentGame()
        {
            var result = _activeGame.GetResult();
            result.timeSpent = _elapsed;
            _activeGame.Cleanup();

            string roomKey = $"{_activeContext.buildingId}_{_activeContext.roomIndex}";
            _completedRooms.Add(roomKey);

            if (miniGameCanvas != null)
                miniGameCanvas.gameObject.SetActive(false);

            eventBus?.Publish(new MiniGameCompletedEvent
            {
                type = _activeContext.type,
                success = result.success,
                score = result.score,
                roomIndex = _activeContext.roomIndex,
                buildingId = _activeContext.buildingId
            });

            _activeGame = null;
        }

        public void AbortIfActive()
        {
            if (_activeGame == null) return;

            _activeGame.Cleanup();

            if (miniGameCanvas != null)
                miniGameCanvas.gameObject.SetActive(false);

            _activeGame = null;
        }

        public void ClearCompletedRooms()
        {
            _completedRooms.Clear();
        }
    }
}
