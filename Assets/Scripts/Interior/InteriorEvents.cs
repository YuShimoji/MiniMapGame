namespace MiniMapGame.Interior
{
    /// <summary>
    /// Method used to unlock a door. Extensible for future mechanics.
    /// </summary>
    public enum DoorUnlockMethod
    {
        Key,        // Corresponding key item collected
        Force,      // Door broken (future)
        MiniGame,   // Mini-game cleared (future)
        InsideOpen, // Opened from the other side (future)
        Bypass      // Alternate route / vent (future)
    }

    [System.Serializable]
    public struct DiscoveryCollectedEvent
    {
        public string discoveryId;
        public FurnitureType furnitureType;
        public int value;
        public string buildingId;
    }

    [System.Serializable]
    public struct DoorUnlockedEvent
    {
        public int doorIndex;
        public int roomA;
        public int roomB;
        public DoorUnlockMethod unlockMethod;
        public string buildingId;
    }

    [System.Serializable]
    public struct HiddenDoorRevealedEvent
    {
        public int doorIndex;
        public string buildingId;
    }

    [System.Serializable]
    public struct FloorChangedEvent
    {
        public int floorIndex;
        public string floorLabel;
        public string buildingId;
    }
}
