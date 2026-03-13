using System.Collections.Generic;
using MiniMapGame.Interior;

namespace MiniMapGame.GameLoop
{
    [System.Serializable]
    public class SaveData
    {
        public int seed;
        public string presetName;
        public GameState gameState;
        public string timestamp;
        public List<BuildingExplorationRecord> explorationRecords;
    }
}
