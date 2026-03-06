using UnityEngine;
using MiniMapGame.Interior;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Attached to landmark buildings. Triggers interior map generation on interaction.
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        [HideInInspector] public string buildingId;

        public void OnClick()
        {
            int interiorSeed = buildingId.GetHashCode();
            var interiorData = InteriorMapGenerator.Generate(interiorSeed);
            Debug.Log($"[BuildingInteraction] Generated interior for {buildingId}: " +
                      $"{interiorData.rooms.Count} rooms, {interiorData.corridors.Count} corridors, " +
                      $"{interiorData.alcoveIndices.Count} alcoves");
            // TODO: Load interior as additive Unity scene
        }
    }
}
