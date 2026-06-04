using System.Collections.Generic;
using MiniMapGame.Interior;

namespace MiniMapGame.GameLoop
{
    [System.Serializable]
    public class SaveData
    {
        public int seed;
        public string presetName;
        public string timestamp;
        public List<BuildingExplorationRecord> explorationRecords;
        public List<QuestSaveEntry> questStates;
    }

    /// <summary>
    /// Serializable quest state for save/load.
    /// </summary>
    [System.Serializable]
    public class QuestSaveEntry
    {
        public string questId;
        public int status;
        public List<int> objectiveProgress = new();
    }
}
