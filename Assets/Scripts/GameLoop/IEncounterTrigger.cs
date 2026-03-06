using MiniMapGame.Data;

namespace MiniMapGame.GameLoop
{
    /// <summary>Fires when player enters a choke-flagged MapEdge.</summary>
    public interface IEncounterTrigger
    {
        void OnEncounter(MapEdge chokeEdge, MapData context);
    }
}
