namespace MiniMapGame.Interior
{
    /// <summary>
    /// Rarity tier for discovery text entries.
    /// Affects visual feedback (toast color) and selection weight.
    /// </summary>
    public enum DiscoveryRarity
    {
        Common,     // 70% weight — normal toast
        Uncommon,   // 25% weight — blue-tinted toast
        Rare        //  5% weight — gold-tinted toast + extended duration
    }
}
