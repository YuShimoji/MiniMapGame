using UnityEngine;

namespace MiniMapGame.MiniGame
{
    /// <summary>
    /// Interface for all mini-games. Lifecycle: Begin → Tick(loop) → GetResult → Cleanup.
    /// </summary>
    public interface IMiniGame
    {
        MiniGameType Type { get; }
        void Begin(MiniGameContext context, RectTransform uiRoot);
        bool Tick(float deltaTime); // true = continue, false = finished
        MiniGameResult GetResult();
        void Cleanup();
    }
}
