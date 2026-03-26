namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Fired when a quest objective makes progress.
    /// </summary>
    public struct QuestProgressEvent
    {
        public string questId;
        public int objectiveIndex;
        public int current;
        public int target;
    }

    /// <summary>
    /// Fired when all objectives of a quest are completed.
    /// </summary>
    public struct QuestCompletedEvent
    {
        public string questId;
        public string title;
        public int rewardValue;
    }

    /// <summary>
    /// Fired when a building is entered for the first time in a session.
    /// Published by QuestManager to decouple from interior system.
    /// </summary>
    public struct BuildingEnteredEvent
    {
        public string buildingId;
        public string buildingCategory;
    }
}
