namespace MiniMapGame.Data
{
    /// <summary>
    /// Defines falloff shape for hill elevation profiles.
    /// Used by ElevationMap.ComputeFalloff to vary terrain character.
    /// </summary>
    public enum SlopeProfile
    {
        Gaussian,   // Standard bell curve: Exp(-distSq * 1.5)
        Steep,      // Sharp cliff edges: Exp(-distSq * 3.0)
        Gentle,     // Rolling hills: Exp(-distSq * 0.7)
        Plateau,    // Flat top with steep sides
        Mesa        // Flat top with near-vertical walls
    }
}
