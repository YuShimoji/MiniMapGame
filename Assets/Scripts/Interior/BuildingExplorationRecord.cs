using System.Collections.Generic;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Persistent exploration record for a single building.
    /// Survives building exit and is saved/loaded with game state.
    /// </summary>
    [System.Serializable]
    public class BuildingExplorationRecord
    {
        public string buildingId;
        public bool hasEntered;
        public int totalFloors;
        public int totalDiscoveries;
        public List<int> visitedFloors = new();
        public List<string> collectedDiscoveries = new();
        public List<KeyDoorStatus> keyDoorStatuses = new();

        public int VisitedFloorCount => visitedFloors.Count;
        public int CollectedCount => collectedDiscoveries.Count;

        public bool IsComplete =>
            visitedFloors.Count >= totalFloors
            && collectedDiscoveries.Count >= totalDiscoveries;

        public void MarkFloorVisited(int floorIndex)
        {
            if (!visitedFloors.Contains(floorIndex))
                visitedFloors.Add(floorIndex);
        }

        public void MarkDiscoveryCollected(string discoveryId)
        {
            if (!collectedDiscoveries.Contains(discoveryId))
                collectedDiscoveries.Add(discoveryId);
        }

        public void SetKeyFound(int doorIndex)
        {
            for (int i = 0; i < keyDoorStatuses.Count; i++)
            {
                if (keyDoorStatuses[i].doorIndex == doorIndex)
                {
                    var s = keyDoorStatuses[i];
                    s.keyFound = true;
                    keyDoorStatuses[i] = s;
                    return;
                }
            }
        }

        public void SetDoorOpened(int doorIndex)
        {
            for (int i = 0; i < keyDoorStatuses.Count; i++)
            {
                if (keyDoorStatuses[i].doorIndex == doorIndex)
                {
                    var s = keyDoorStatuses[i];
                    s.doorOpened = true;
                    keyDoorStatuses[i] = s;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Status of a key-door pair for exploration tracking.
    /// </summary>
    [System.Serializable]
    public struct KeyDoorStatus
    {
        public int doorIndex;
        public bool keyFound;
        public bool doorOpened;
    }
}
