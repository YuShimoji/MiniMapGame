namespace MiniMapGame.Interior
{
    /// <summary>
    /// Comprehensive room types for the interior generation system.
    /// Organized by functional category.
    /// </summary>
    public enum InteriorRoomType
    {
        // === Structural ===
        Entrance,           // Building entry, always present on ground floor
        Hallway,            // Narrow connecting passage
        Stairwell,          // Floor transition point
        Corridor,           // Wide connecting passage

        // === Residential ===
        LivingRoom,
        Bedroom,
        Kitchen,
        Bathroom,
        DiningRoom,
        Storage,            // Closets, pantry, small storage

        // === Commercial ===
        Shopfront,          // Customer-facing retail area
        Backroom,           // Storage/office behind shop
        Counter,            // Service counter / register area
        DisplayArea,        // Showcases, shelves, exhibits
        SeatingArea,        // Restaurant/cafe seating
        Bar,                // Bar/counter-dominant drinking area

        // === Industrial ===
        Workshop,
        LoadingDock,
        MachineryRoom,

        // === Public / Office ===
        Lobby,
        Office,
        MeetingRoom,
        Archive,            // Records, library stacks

        // === Special / Exploration ===
        Laboratory,
        ServerRoom,
        SecretRoom,         // Hidden, reward for thorough exploration
        Vault,              // High-value discovery location
        Ruin,               // Damaged/collapsed room
        Rooftop,            // Accessible roof area
        Basement,           // Below ground level

        // === Dead Space ===
        WallVoid,           // Inaccessible gap between rooms
        Shaft,              // Elevator/utility shaft (visual only)

        // === Utility ===
        Restroom,
        Utility             // HVAC, electrical, mechanical
    }
}
