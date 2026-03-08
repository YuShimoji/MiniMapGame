namespace MiniMapGame.Data
{
    /// <summary>
    /// Commercial building subtypes. Grouped by typical road tier placement.
    /// </summary>
    public enum ShopSubtype
    {
        None,

        // Tier 0 (arterial / main road) — large, prominent
        Department,
        Bank,
        Hotel,
        Restaurant,

        // Tier 1 (secondary road) — mid-size, neighborhood
        Grocery,
        Pharmacy,
        Bookstore,
        Cafe,
        Clinic,

        // Tier 2 (back street) — small, specialized
        Pawnshop,
        Bar,
        ArcadeShop,
        Laundry,
        Tattoo,

        // Arcade / covered street
        Stall,
        Vendor
    }
}
