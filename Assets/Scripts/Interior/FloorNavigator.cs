using System.Collections.Generic;
using UnityEngine;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Handles floor-to-floor navigation within a building interior.
    /// Detects when player enters a Stairwell room and provides up/down floor movement.
    /// Manages floor visibility switching via InteriorRenderer.
    /// </summary>
    public class FloorNavigator : MonoBehaviour
    {
        [Header("References")]
        public InteriorRenderer interiorRenderer;
        public Transform playerTransform;

        [Header("Settings")]
        [Tooltip("Distance from stairwell center to trigger floor change prompt")]
        public float stairwellTriggerRadius = 2f;
        [Tooltip("Keyboard key to go up one floor")]
        public KeyCode goUpKey = KeyCode.PageUp;
        [Tooltip("Keyboard key to go down one floor")]
        public KeyCode goDownKey = KeyCode.PageDown;

        // Tracks stairwell positions per floor index (list index = renderer floor index)
        private List<List<StairwellInfo>> _stairwellsByFloor = new();
        private InteriorMapData _currentData;
        private Vector3 _worldOrigin;
        private bool _isActive;

        // Public state for UI
        public bool IsNearStairwell { get; private set; }
        public bool CanGoUp { get; private set; }
        public bool CanGoDown { get; private set; }

        /// <summary>
        /// Initialize with generated interior data. Call after InteriorRenderer.Render().
        /// </summary>
        public void Initialize(InteriorMapData data, Vector3 worldOrigin)
        {
            _currentData = data;
            _worldOrigin = worldOrigin;
            _isActive = true;
            _stairwellsByFloor.Clear();

            // Build stairwell lookup per floor
            for (int fi = 0; fi < data.floors.Count; fi++)
            {
                var floor = data.floors[fi];
                var stairwells = new List<StairwellInfo>();
                foreach (var room in floor.rooms)
                {
                    if (room.type == InteriorRoomType.Stairwell)
                    {
                        stairwells.Add(new StairwellInfo
                        {
                            localPosition = room.position,
                            worldPosition = new Vector3(
                                worldOrigin.x + room.position.x,
                                0f,
                                worldOrigin.z + room.position.y),
                            roomId = room.id
                        });
                    }
                }
                _stairwellsByFloor.Add(stairwells);
            }
        }

        /// <summary>
        /// Deactivate floor navigation (call on building exit).
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            _currentData = null;
            _stairwellsByFloor.Clear();
            IsNearStairwell = false;
            CanGoUp = false;
            CanGoDown = false;
        }

        void Update()
        {
            if (!_isActive || interiorRenderer == null || playerTransform == null) return;

            int currentFloor = interiorRenderer.CurrentFloorIndex;
            CheckStairwellProximity(currentFloor);

            if (IsNearStairwell)
            {
                if (CanGoUp && Input.GetKeyDown(goUpKey))
                    MoveToFloor(currentFloor + 1);
                else if (CanGoDown && Input.GetKeyDown(goDownKey))
                    MoveToFloor(currentFloor - 1);
            }
        }

        private void CheckStairwellProximity(int floorIndex)
        {
            IsNearStairwell = false;
            CanGoUp = false;
            CanGoDown = false;

            if (floorIndex < 0 || floorIndex >= _stairwellsByFloor.Count) return;

            var stairwells = _stairwellsByFloor[floorIndex];
            Vector3 playerPos = playerTransform.position;

            foreach (var sw in stairwells)
            {
                float dist = Vector2.Distance(
                    new Vector2(playerPos.x, playerPos.z),
                    new Vector2(sw.worldPosition.x, sw.worldPosition.z));

                if (dist <= stairwellTriggerRadius)
                {
                    IsNearStairwell = true;
                    CanGoUp = floorIndex < interiorRenderer.FloorCount - 1;
                    CanGoDown = floorIndex > 0;
                    return;
                }
            }
        }

        /// <summary>
        /// Public API to change floor. Wraps SetActiveFloor and teleports player to stairwell.
        /// Called by InteriorInteractionManager.ChangeFloor() for stair interactions.
        /// </summary>
        public void ChangeFloor(int targetFloorIndex)
        {
            MoveToFloor(targetFloorIndex);
        }

        private void MoveToFloor(int targetFloorIndex)
        {
            if (targetFloorIndex < 0 || targetFloorIndex >= _stairwellsByFloor.Count) return;

            // Find the nearest stairwell on the target floor to teleport to
            var targetStairwells = _stairwellsByFloor[targetFloorIndex];
            if (targetStairwells.Count == 0)
            {
                // No stairwell on target floor — just switch view
                if (targetFloorIndex > interiorRenderer.CurrentFloorIndex)
                    interiorRenderer.GoUpFloor();
                else
                    interiorRenderer.GoDownFloor();
                return;
            }

            // Find closest stairwell on target floor to player's current XZ position
            Vector3 playerPos = playerTransform.position;
            float bestDist = float.MaxValue;
            Vector3 bestPos = targetStairwells[0].worldPosition;

            foreach (var sw in targetStairwells)
            {
                float dist = Vector2.Distance(
                    new Vector2(playerPos.x, playerPos.z),
                    new Vector2(sw.worldPosition.x, sw.worldPosition.z));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = sw.worldPosition;
                }
            }

            // Switch floor rendering
            interiorRenderer.SetActiveFloor(targetFloorIndex);

            // Teleport player to target stairwell
            Vector3 targetPos = new Vector3(bestPos.x, playerPos.y, bestPos.z);
            var pm = playerTransform != null ? playerTransform.GetComponent<MiniMapGame.Player.PlayerMovement>() : null;
            if (pm != null)
                pm.Teleport(targetPos);
            else if (playerTransform != null)
                playerTransform.position = targetPos;
        }

        private struct StairwellInfo
        {
            public Vector2 localPosition;
            public Vector3 worldPosition;
            public int roomId;
        }
    }
}
