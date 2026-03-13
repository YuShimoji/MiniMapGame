using UnityEngine;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Interactable component for stairwell floor navigation.
    /// Attached to child GameObjects within Stairwell rooms.
    /// Triggers floor change when player interacts with E key.
    /// </summary>
    public class StairInteractable : MonoBehaviour, IInteriorInteractable
    {
        public int floorIndex;           // Current floor this stair is on
        public int targetFloorIndex;     // Target floor to move to
        public bool goesUp;              // true = upstairs, false = downstairs

        public Vector3 WorldPosition => transform.position;
        public float InteractRadius => 2.0f;
        public bool IsAvailable => true;
        public int FloorIndex => floorIndex;

        public string PromptMessage => goesUp ? "E: Go Upstairs" : "E: Go Downstairs";

        public void Interact(InteriorInteractionManager manager)
        {
            manager.ChangeFloor(targetFloorIndex);
        }
    }
}
