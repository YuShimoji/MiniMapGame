using System.Collections.Generic;

namespace MiniMapGame.GameLoop
{
    [System.Serializable]
    public class GameState
    {
        public int collectedValue;
        public int encounterCount;
        public List<string> collectedItemIds = new();
        public PlayerStats stats = new();

        public void Reset()
        {
            collectedValue = 0;
            encounterCount = 0;
            collectedItemIds.Clear();
            stats.Reset();
        }

        public bool HasCollected(string objectId)
        {
            return collectedItemIds.Contains(objectId);
        }

        public void RecordCollection(string objectId, int value)
        {
            if (collectedItemIds.Contains(objectId)) return;
            collectedItemIds.Add(objectId);
            collectedValue += value;
        }

        public void RecordEncounter()
        {
            encounterCount++;
        }
    }
}
