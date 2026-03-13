using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MiniMapGame.GameLoop;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Manages all interior interactions: discovery collection, door unlock/open,
    /// hidden door proximity reveal. Active only while InteriorController.IsInside.
    /// </summary>
    public class InteriorInteractionManager : MonoBehaviour
    {
        [Header("References")]
        public InteriorController interiorController;
        public InteriorRenderer interiorRenderer;
        public Transform playerTransform;
        public MapEventBus eventBus;

        [Header("UI")]
        public GameObject promptPanel;
        public TextMeshProUGUI promptText;

        [Header("Settings")]
        public float hiddenDoorRevealRadius = 1.2f;
        public KeyCode interactKey = KeyCode.E;

        public InteriorSessionState SessionState { get; private set; }

        private string _buildingId;
        private readonly List<IInteriorInteractable> _interactables = new();
        private readonly List<DoorInteractable> _hiddenDoors = new();
        private IInteriorInteractable _currentTarget;

        /// <summary>
        /// Called by InteriorController after interior is rendered.
        /// Collects all interactables and builds key-door mappings.
        /// </summary>
        public void Initialize(string buildingId)
        {
            _buildingId = buildingId;
            SessionState = new InteriorSessionState();
            _interactables.Clear();
            _hiddenDoors.Clear();
            _currentTarget = null;

            // Collect all interactables from rendered interior
            var discoveries = interiorRenderer.GetComponentsInChildren<DiscoveryInteractable>(true);
            var doors = interiorRenderer.GetComponentsInChildren<DoorInteractable>(true);

            _interactables.AddRange(discoveries);
            _interactables.AddRange(doors);

            // Register hidden doors for proximity reveal
            foreach (var door in doors)
            {
                if (door.isHidden)
                    _hiddenDoors.Add(door);
            }

            // Build 1:1 key-door mapping
            BuildKeyDoorMappings(discoveries, doors);
        }

        /// <summary>
        /// Called by InteriorController before interior is cleared.
        /// </summary>
        public void Cleanup()
        {
            SessionState?.Reset();
            SessionState = null;
            _interactables.Clear();
            _hiddenDoors.Clear();
            _currentTarget = null;
            _buildingId = null;

            if (promptPanel != null)
                promptPanel.SetActive(false);
        }

        void Update()
        {
            if (interiorController == null || !interiorController.IsInside) return;
            if (SessionState == null || playerTransform == null) return;

            int activeFloor = interiorRenderer.CurrentFloorIndex;

            // Hidden door proximity reveal
            CheckHiddenDoorReveal(activeFloor);

            // Find nearest available interactable on active floor
            _currentTarget = FindNearestInteractable(activeFloor);

            // Update prompt UI
            UpdatePromptUI();

            // Interact on key press
            if (_currentTarget != null && Input.GetKeyDown(interactKey))
            {
                _currentTarget.Interact(this);
            }
        }

        private void UpdatePromptUI()
        {
            string msg = GetCurrentPrompt();
            if (string.IsNullOrEmpty(msg))
            {
                if (promptPanel != null && promptPanel.activeSelf)
                    promptPanel.SetActive(false);
            }
            else
            {
                if (promptPanel != null && !promptPanel.activeSelf)
                    promptPanel.SetActive(true);
                if (promptText != null)
                    promptText.text = msg;
            }
        }

        private void CheckHiddenDoorReveal(int activeFloor)
        {
            for (int i = _hiddenDoors.Count - 1; i >= 0; i--)
            {
                var door = _hiddenDoors[i];
                if (door == null || door.FloorIndex != activeFloor) continue;

                float dist = Vector3.Distance(playerTransform.position, door.WorldPosition);
                if (dist <= hiddenDoorRevealRadius)
                {
                    door.RevealHiddenDoor(this);
                    _hiddenDoors.RemoveAt(i);
                }
            }
        }

        private IInteriorInteractable FindNearestInteractable(int activeFloor)
        {
            IInteriorInteractable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var target in _interactables)
            {
                // Skip destroyed objects
                if (target is MonoBehaviour mb && mb == null) continue;
                if (!target.IsAvailable) continue;
                if (target.FloorIndex != activeFloor) continue;

                float dist = Vector3.Distance(playerTransform.position, target.WorldPosition);
                if (dist <= target.InteractRadius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = target;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Build 1:1 key-door mapping: each locked door maps to the nearest
        /// Container discovery in an adjacent room.
        /// </summary>
        private void BuildKeyDoorMappings(DiscoveryInteractable[] discoveries, DoorInteractable[] doors)
        {
            var usedDiscoveries = new HashSet<string>();

            foreach (var door in doors)
            {
                if (!door.isLocked) continue;

                // Find the nearest Container-type discovery not yet assigned
                DiscoveryInteractable bestKey = null;
                float bestDist = float.MaxValue;

                foreach (var disc in discoveries)
                {
                    if (disc.furnitureType != FurnitureType.Container) continue;
                    if (usedDiscoveries.Contains(disc.discoveryId)) continue;

                    float dist = Vector3.Distance(door.WorldPosition, disc.WorldPosition);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestKey = disc;
                    }
                }

                if (bestKey != null)
                {
                    SessionState.doorKeyMap[door.doorIndex] = bestKey.discoveryId;
                    bestKey.linkedDoorIndex = door.doorIndex;
                    usedDiscoveries.Add(bestKey.discoveryId);
                }
            }
        }

        // ===== Event publishing =====

        public void PublishDiscoveryCollected(DiscoveryInteractable discovery)
        {
            if (eventBus == null) return;
            eventBus.Publish(new DiscoveryCollectedEvent
            {
                discoveryId = discovery.discoveryId,
                furnitureType = discovery.furnitureType,
                value = discovery.value,
                buildingId = _buildingId ?? ""
            });
        }

        public void PublishDoorUnlocked(DoorInteractable door, DoorUnlockMethod method)
        {
            if (eventBus == null) return;
            eventBus.Publish(new DoorUnlockedEvent
            {
                doorIndex = door.doorIndex,
                roomA = door.roomA,
                roomB = door.roomB,
                unlockMethod = method,
                buildingId = _buildingId ?? ""
            });
        }

        public void PublishHiddenDoorRevealed(DoorInteractable door)
        {
            if (eventBus == null) return;
            eventBus.Publish(new HiddenDoorRevealedEvent
            {
                doorIndex = door.doorIndex,
                buildingId = _buildingId ?? ""
            });
        }

        /// <summary>
        /// Returns the current interaction target's prompt message, or empty string.
        /// Used by UI to display interaction prompt.
        /// </summary>
        public string GetCurrentPrompt()
        {
            if (_currentTarget == null) return "";
            if (_currentTarget is MonoBehaviour mb && mb == null) return "";
            return _currentTarget.PromptMessage;
        }
    }
}
