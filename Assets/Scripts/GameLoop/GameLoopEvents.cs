using MiniMapGame.Data;

namespace MiniMapGame.GameLoop
{
    [System.Serializable]
    public struct ValueCollectedEvent
    {
        public string objectId;
        public int value;
        public int totalValue;
    }

    [System.Serializable]
    public struct EncounterTriggeredEvent
    {
        public int edgeIndex;
        public MapEdge chokeEdge;
        public int encounterNumber;
    }

    [System.Serializable]
    public struct ExtractionDecisionEvent
    {
        public int nodeIndex;
        public bool extracted;
        public int finalValue;
    }

    [System.Serializable]
    public struct GameLoopStartedEvent
    {
        public int seed;
        public int deadEndCount;
        public int chokePointCount;
        public int extractionPointCount;
    }

    [System.Serializable]
    public struct GameLoopEndedEvent
    {
        public int finalValue;
        public int encounterCount;
        public int itemsCollected;
        public bool extracted;
    }
}
