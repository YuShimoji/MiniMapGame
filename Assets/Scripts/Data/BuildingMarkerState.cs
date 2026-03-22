namespace MiniMapGame.Data
{
    /// <summary>
    /// Exploration marker state for a building on the overworld map.
    /// Drives visual representation (marker icon, color, minimap dot).
    /// See SP-020 Layer 2 spec for state transition rules.
    /// </summary>
    public enum BuildingMarkerState
    {
        /// <summary>Never visited or approached. No marker shown.</summary>
        Unknown,

        /// <summary>Player approached (proximity highlight triggered). Shows "?" marker.</summary>
        Discovered,

        /// <summary>Player has entered at least once. Shows category icon (gray).</summary>
        Entered,

        /// <summary>Entered but not fully explored. Shows category icon (orange) + progress bar.</summary>
        InProgress,

        /// <summary>All floors visited and all discoveries collected. Shows category icon (green) + check.</summary>
        Complete
    }
}
