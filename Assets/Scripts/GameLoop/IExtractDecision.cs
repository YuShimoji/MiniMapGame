using MiniMapGame.Data;

namespace MiniMapGame.GameLoop
{
    /// <summary>Called when player reaches exit node.</summary>
    public interface IExtractDecision
    {
        bool ShouldExtract(MapData context, int collectedValue);
        void OnExtract();
        void OnContinue();
    }
}
