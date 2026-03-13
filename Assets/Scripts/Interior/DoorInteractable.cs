using UnityEngine;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Attached to door indicator GameObjects. Handles door unlock/open interaction.
    /// Designed to be extensible for future unlock methods (force, mini-game, inside-open).
    /// </summary>
    public class DoorInteractable : MonoBehaviour, IInteriorInteractable
    {
        [HideInInspector] public int doorIndex;
        [HideInInspector] public int roomA;
        [HideInInspector] public int roomB;
        [HideInInspector] public bool isLocked;
        [HideInInspector] public bool isHidden;
        [HideInInspector] public int floorIndex;

        private DoorState _state = DoorState.Locked;
        private bool _isRevealed;

        /// <summary>
        /// Non-trigger collider that blocks player movement through locked doors.
        /// Created and managed externally by InteriorRenderer; destroyed on unlock.
        /// </summary>
        [HideInInspector] public BoxCollider blockingCollider;

        private enum DoorState { Locked, Unlocked, Open }

        // IInteriorInteractable
        public Vector3 WorldPosition => transform.position;
        public float InteractRadius => 1.8f;
        public int FloorIndex => floorIndex;

        public bool IsAvailable =>
            _state != DoorState.Open && (!isHidden || _isRevealed);

        public string PromptMessage
        {
            get
            {
                if (isHidden && !_isRevealed) return "";
                return _state switch
                {
                    DoorState.Locked => "Locked",
                    DoorState.Unlocked => "E: Open Door",
                    _ => ""
                };
            }
        }

        void Awake()
        {
            _state = isLocked ? DoorState.Locked : DoorState.Unlocked;
            _isRevealed = !isHidden;
        }

        public void Interact(InteriorInteractionManager manager)
        {
            switch (_state)
            {
                case DoorState.Locked:
                    if (manager.SessionState.HasKeyForDoor(doorIndex))
                    {
                        Unlock(manager, DoorUnlockMethod.Key);
                    }
                    // else: "Locked" prompt already shown, no action
                    break;

                case DoorState.Unlocked:
                    Open();
                    break;
            }
        }

        /// <summary>
        /// Unlock this door. Called by Interact or future unlock mechanics.
        /// </summary>
        public void Unlock(InteriorInteractionManager manager, DoorUnlockMethod method)
        {
            if (_state != DoorState.Locked) return;

            _state = DoorState.Unlocked;
            manager.SessionState.UnlockDoor(doorIndex);
            manager.PublishDoorUnlocked(this, method);

            // Visual: change color to green
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.material.color = new Color(0.3f, 0.7f, 0.4f, 0.8f);

            // Remove movement blocker
            if (blockingCollider != null)
            {
                Destroy(blockingCollider);
                blockingCollider = null;
            }
        }

        private void Open()
        {
            _state = DoorState.Open;

            // Visual: shrink door indicator
            transform.localScale *= 0.3f;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                r.material.color = new Color(0.3f, 0.7f, 0.4f, 0.3f);
        }

        /// <summary>
        /// Reveal a hidden door (called by InteriorInteractionManager on proximity).
        /// </summary>
        public void RevealHiddenDoor(InteriorInteractionManager manager)
        {
            if (_isRevealed) return;
            _isRevealed = true;

            gameObject.SetActive(true);
            manager.SessionState.RevealDoor(doorIndex);
            manager.PublishHiddenDoorRevealed(this);
        }
    }
}
