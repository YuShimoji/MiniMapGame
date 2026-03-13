using UnityEngine;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Interface for all interactable objects inside buildings.
    /// Implemented by DiscoveryInteractable and DoorInteractable.
    /// </summary>
    public interface IInteriorInteractable
    {
        Vector3 WorldPosition { get; }
        float InteractRadius { get; }
        string PromptMessage { get; }
        bool IsAvailable { get; }
        int FloorIndex { get; }
        void Interact(InteriorInteractionManager manager);
    }
}
