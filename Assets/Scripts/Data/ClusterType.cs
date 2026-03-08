namespace MiniMapGame.Data
{
    /// <summary>
    /// Defines the shape pattern of a hill cluster.
    /// Used by TerrainGenerator to create coherent terrain features.
    /// </summary>
    public enum ClusterType
    {
        Ridge,          // Linear chain of elongated hills forming a ridge
        MoundGroup,     // Circular cluster of 3-5 hills
        ValleyFramer,   // Two parallel ridges with a gap between
        Solitary        // Single isolated hill
    }
}
