namespace MiniMapGame.Interior
{
    /// <summary>
    /// Furniture and prop types for interior decoration.
    /// Used by FurnitureSpawner in Phase 2.
    /// </summary>
    public enum FurnitureType
    {
        // Generic
        Table,
        Chair,
        Shelf,
        Cabinet,
        Lamp,

        // Residential
        Bed,
        Sofa,
        Fridge,
        Stove,
        Bathtub,
        Sink,

        // Commercial
        ShopCounter,
        Register,
        DisplayCase,
        Mannequin,

        // Industrial
        Crate,
        Barrel,
        Machine,
        Workbench,
        Pallet,

        // Public / Office
        Desk,
        FileCabinet,
        Bookshelf,
        Computer,

        // Decay / Special
        Rubble,
        Debris,
        Cobweb,
        Vine,

        // Exploration markers
        Document,
        Photo,
        Note,
        Container
    }
}
