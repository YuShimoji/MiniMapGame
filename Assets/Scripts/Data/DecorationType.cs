namespace MiniMapGame.Data
{
    public enum DecorationType
    {
        // Road-relative (existing)
        StreetLight,    // 0
        Tree,           // 1
        Bench,          // 2
        Bollard,        // 3

        // Terrain-aware (new)
        Rock,           // 4 - high elevation, steep slopes
        Boulder,        // 5 - steep slopes
        GrassClump,     // 6 - flat lowlands
        Wildflower,     // 7 - lowlands near water
        Shrub,          // 8 - hill edges (transition zones)
        Fence,          // 9 - along roads (Rural/Mountain)
        Stump,          // 10 - near trees (Rural)
        SignPost        // 11 - at intersections (Mountain/Rural)
    }
}
