using System.Collections.Generic;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Per-visit state for interior exploration. Reset on building exit.
    /// Tracks collected discoveries, unlocked doors, and key-door mappings.
    /// </summary>
    public class InteriorSessionState
    {
        public readonly HashSet<string> collectedDiscoveries = new();
        public readonly HashSet<int> unlockedDoors = new();
        public readonly HashSet<int> revealedHiddenDoors = new();

        /// <summary>
        /// 1:1 mapping from doorIndex to the discoveryId that serves as its key.
        /// Set during InteriorInteractionManager.Initialize().
        /// </summary>
        public readonly Dictionary<int, string> doorKeyMap = new();

        public int discoveryCount;
        public int totalDiscoveryValue;

        public void RecordDiscovery(string id, int value)
        {
            if (collectedDiscoveries.Add(id))
            {
                discoveryCount++;
                totalDiscoveryValue += value;
            }
        }

        public bool HasCollected(string id) => collectedDiscoveries.Contains(id);

        /// <summary>
        /// Check if the key for a specific door has been collected.
        /// </summary>
        public bool HasKeyForDoor(int doorIndex)
        {
            return doorKeyMap.TryGetValue(doorIndex, out var keyId)
                   && collectedDiscoveries.Contains(keyId);
        }

        public void UnlockDoor(int doorIndex) => unlockedDoors.Add(doorIndex);
        public bool IsDoorUnlocked(int doorIndex) => unlockedDoors.Contains(doorIndex);

        public void RevealDoor(int doorIndex) => revealedHiddenDoors.Add(doorIndex);
        public bool IsDoorRevealed(int doorIndex) => revealedHiddenDoors.Contains(doorIndex);

        public void Reset()
        {
            collectedDiscoveries.Clear();
            unlockedDoors.Clear();
            revealedHiddenDoors.Clear();
            doorKeyMap.Clear();
            discoveryCount = 0;
            totalDiscoveryValue = 0;
        }
    }
}
