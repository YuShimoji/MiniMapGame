using System.Collections.Generic;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Quest type — determines what kind of objective the quest focuses on.
    /// </summary>
    public enum QuestType
    {
        Exploration,  // Enter/explore buildings of a category
        Collection,   // Collect discoveries of a type
        Survey,       // Fully explore buildings in a category
        Discovery,    // Find items of a specific rarity
        Traversal     // Visit specific map nodes
    }

    /// <summary>
    /// Objective type — the specific metric being tracked.
    /// </summary>
    public enum ObjectiveType
    {
        EnterBuilding,      // target = BuildingCategory name
        CollectDiscovery,   // target = FurnitureType name
        VisitFloor,         // target = minimum floor count (e.g. "3")
        CompleteBuilding,   // target = BuildingCategory name or "*"
        FindRare,           // target = DiscoveryRarity name
        VisitNode           // target = node condition (e.g. "deadEnd")
    }

    /// <summary>
    /// A single measurable objective within a quest.
    /// </summary>
    [System.Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;
        public string target;
        public int count;
        public int current;

        public bool IsCompleted => current >= count;
    }

    /// <summary>
    /// Reward granted on quest completion.
    /// </summary>
    [System.Serializable]
    public class QuestReward
    {
        public int value;
        public string unlockId;
    }

    /// <summary>
    /// Complete quest definition. Loaded from JSON at runtime.
    /// </summary>
    [System.Serializable]
    public class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public QuestType questType;
        public List<QuestObjective> objectives = new();
        public QuestReward reward = new();
        public List<string> prerequisites = new();
        public bool isRepeatable;
    }

    /// <summary>
    /// Runtime state of a quest instance during a session.
    /// </summary>
    public enum QuestStatus
    {
        Available,   // Can be started (prerequisites met)
        Active,      // In progress
        Completed,   // All objectives met
        Failed       // Not used in Phase 1 (no fail conditions)
    }

    /// <summary>
    /// Runtime quest instance — pairs a definition with live progress.
    /// </summary>
    [System.Serializable]
    public class QuestState
    {
        public string questId;
        public QuestStatus status;
        public List<int> objectiveProgress = new();

        public QuestState(QuestDefinition def)
        {
            questId = def.id;
            status = QuestStatus.Active;
            objectiveProgress = new List<int>(def.objectives.Count);
            for (int i = 0; i < def.objectives.Count; i++)
                objectiveProgress.Add(0);
        }
    }

    /// <summary>
    /// Wrapper for JSON deserialization of quest list.
    /// </summary>
    [System.Serializable]
    public class QuestCollection
    {
        public List<QuestDefinition> quests = new();
    }
}
