using UnityEngine;
using MiniMapGame.Interior;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Attached to buildings. Triggers interior map generation on interaction.
    /// Holds InteriorBuildingContext for v2 context-aware generation.
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        [HideInInspector] public string buildingId;
        [HideInInspector] public bool isLandmark;
        [HideInInspector] public InteriorBuildingContext context;

        public string GetInteractionMessage()
        {
            return isLandmark ? $"Enter {buildingId}" : buildingId;
        }

        public void Interact()
        {
            if (!isLandmark) return;

            var controller = Object.FindAnyObjectByType<InteriorController>();
            if (controller != null)
            {
                controller.EnterBuilding(this);
            }
            else
            {
                Debug.LogWarning("[BuildingInteraction] InteriorController not found in scene.");
            }
        }
    }
}
