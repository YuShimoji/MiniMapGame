using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Factory for selecting the appropriate floor plan generator by building category.
    /// </summary>
    public static class FloorPlanFactory
    {
        public static IFloorPlanGenerator Create(BuildingCategory category)
        {
            return category switch
            {
                BuildingCategory.Residential => new ResidentialFloorPlan(),
                BuildingCategory.Commercial => new CommercialFloorPlan(),
                BuildingCategory.Industrial => new IndustrialFloorPlan(),
                BuildingCategory.Public => new CommercialFloorPlan(),
                BuildingCategory.Special => new SpecialFloorPlan(),
                _ => new ResidentialFloorPlan()
            };
        }
    }
}
