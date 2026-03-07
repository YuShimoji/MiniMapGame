namespace MiniMapGame.MiniGame
{
    public enum MiniGameType
    {
        TimingCombat,
        MemoryMatch,
        TrapDodge
    }

    [System.Serializable]
    public struct MiniGameResult
    {
        public bool success;
        public int score;
        public float timeSpent;
    }

    [System.Serializable]
    public struct MiniGameContext
    {
        public MiniGameType type;
        public int roomIndex;
        public int seed;
        public string buildingId;
    }
}
