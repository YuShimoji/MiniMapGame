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
        [HideInInspector] public bool isLandmark;

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
