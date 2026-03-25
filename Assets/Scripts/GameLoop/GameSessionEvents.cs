namespace MiniMapGame.GameLoop
{
    public struct SessionStartedEvent
    {
        public float sessionDuration;
    }

    public struct SessionEndedEvent
    {
        public float elapsedTime;
        public int buildingsEntered;
        public int buildingsCompleted;
        public int totalDiscoveries;
        public bool timedOut;
    }
}
