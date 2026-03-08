using MiniMapGame.Core;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Strategy interface for floor plan generation.
    /// Each implementation handles a specific building category layout.
    /// </summary>
    public interface IFloorPlanGenerator
    {
        InteriorFloorData Generate(
            SeededRng rng,
            InteriorBuildingContext context,
            InteriorPreset preset,
            int floorIndex
        );
    }
}
