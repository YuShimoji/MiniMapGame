using UnityEngine;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Attached to discovery furniture (Document, Note, Photo, Container).
    /// Handles collection interaction for discovery items.
    /// </summary>
    public class DiscoveryInteractable : MonoBehaviour, IInteriorInteractable
    {
        [HideInInspector] public string discoveryId;
        [HideInInspector] public FurnitureType furnitureType;
        [HideInInspector] public int value;
        [HideInInspector] public int floorIndex;

        /// <summary>
        /// If >= 0, collecting this discovery unlocks the corresponding door (1:1 key).
        /// Set by InteriorInteractionManager during Initialize().
        /// </summary>
        [HideInInspector] public int linkedDoorIndex = -1;

        private bool _collected;

        // IInteriorInteractable
        public Vector3 WorldPosition => transform.position;
        public float InteractRadius => 1.5f;
        public bool IsAvailable => !_collected;
        public int FloorIndex => floorIndex;

        public string PromptMessage
        {
            get
            {
                if (_collected) return "";
                if (linkedDoorIndex >= 0)
                    return furnitureType == FurnitureType.Container
                        ? "E: Open Container (Key)"
                        : "E: Collect (Key)";
                return furnitureType switch
                {
                    FurnitureType.Document => "E: Read Document",
                    FurnitureType.Note => "E: Pick Up Note",
                    FurnitureType.Photo => "E: Examine Photo",
                    FurnitureType.Container => "E: Search Container",
                    _ => "E: Examine"
                };
            }
        }

        public void Interact(InteriorInteractionManager manager)
        {
            if (_collected) return;
            _collected = true;

            // Record in session state
            manager.SessionState.RecordDiscovery(discoveryId, value);

            // Publish event
            manager.PublishDiscoveryCollected(this);

            // Visual feedback: shrink + color change
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
            transform.localScale *= 0.5f;

            // Destroy after brief delay
            Destroy(gameObject, 0.3f);
        }

        public static bool IsDiscoveryType(FurnitureType type)
            => type is FurnitureType.Document or FurnitureType.Note
                   or FurnitureType.Photo or FurnitureType.Container;

        public static int GetDefaultValue(FurnitureType type)
            => type switch
            {
                FurnitureType.Container => 20,
                FurnitureType.Document => 15,
                FurnitureType.Photo => 10,
                FurnitureType.Note => 5,
                _ => 5
            };
    }
}
