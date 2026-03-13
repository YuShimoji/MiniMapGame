using UnityEngine;
using MiniMapGame.Player;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Controls interior rendering visibility based on camera distance.
    /// Provides LOD-like behavior: full detail at close range, walls-only at mid range,
    /// completely hidden at far range. Always fully visible when player is inside.
    /// </summary>
    public class InteriorVisibilityController : MonoBehaviour
    {
        [Header("References")]
        public CameraController cameraController;

        [Header("Distance Thresholds")]
        [Tooltip("Below this distance: full detail (rooms, walls, doors, furniture)")]
        public float nearThreshold = 15f;
        [Tooltip("Below this distance: walls and doors only. Above: hide everything")]
        public float farThreshold = 30f;

        [Header("Override")]
        [Tooltip("When true, forces full visibility regardless of distance (e.g. player is inside)")]
        public bool forceFullVisibility;

        private InteriorRenderer _renderer;
        private VisibilityLevel _currentLevel = VisibilityLevel.Full;

        private enum VisibilityLevel
        {
            Full,       // All details visible
            Minimal,    // Walls/doors only
            Hidden      // Nothing visible
        }

        void Awake()
        {
            _renderer = GetComponent<InteriorRenderer>();
        }

        void LateUpdate()
        {
            if (_renderer == null || cameraController == null) return;

            VisibilityLevel targetLevel;
            if (forceFullVisibility)
            {
                targetLevel = VisibilityLevel.Full;
            }
            else
            {
                float dist = cameraController.CurrentDistance;
                if (dist < nearThreshold)
                    targetLevel = VisibilityLevel.Full;
                else if (dist < farThreshold)
                    targetLevel = VisibilityLevel.Minimal;
                else
                    targetLevel = VisibilityLevel.Hidden;
            }

            if (targetLevel != _currentLevel)
            {
                _currentLevel = targetLevel;
            }

            ApplyVisibility(targetLevel);
        }

        private void ApplyVisibility(VisibilityLevel level)
        {
            switch (level)
            {
                case VisibilityLevel.Full:
                    SetFullVisibility();
                    break;
                case VisibilityLevel.Minimal:
                    SetVisibilityByNamePattern(true, "Wall", "Door");
                    SetVisibilityByNamePattern(false, "RoomFloor", "Corridor", "Furniture");
                    break;
                case VisibilityLevel.Hidden:
                    SetAllChildrenActive(false);
                    break;
            }
        }

        private void SetFullVisibility()
        {
            int floorRootIndex = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Floor_"))
                {
                    bool isCurrentFloor = floorRootIndex == _renderer.CurrentFloorIndex;
                    child.gameObject.SetActive(isCurrentFloor);
                    if (isCurrentFloor)
                    {
                        SetActiveRecursive(child, true);
                    }
                    floorRootIndex++;
                }
                else
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        private void SetAllChildrenActive(bool active)
        {
            int floorRootIndex = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Floor_"))
                {
                    child.gameObject.SetActive(active && floorRootIndex == _renderer.CurrentFloorIndex);
                    floorRootIndex++;
                }
                else
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private void SetVisibilityByNamePattern(bool active, params string[] patterns)
        {
            // Walk all children recursively
            SetVisibilityRecursive(transform, active, patterns);
        }

        private void SetVisibilityRecursive(Transform parent, bool active, string[] patterns)
        {
            int floorRootIndex = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string name = child.name;

                if (parent == transform && name.StartsWith("Floor_"))
                {
                    bool isCurrentFloor = floorRootIndex == _renderer.CurrentFloorIndex;
                    floorRootIndex++;
                    child.gameObject.SetActive(isCurrentFloor);
                    if (isCurrentFloor)
                    {
                        SetVisibilityRecursive(child, active, patterns);
                    }
                    continue;
                }

                bool matches = false;
                foreach (var pattern in patterns)
                {
                    if (name.Contains(pattern))
                    {
                        matches = true;
                        break;
                    }
                }

                if (matches)
                {
                    child.gameObject.SetActive(active);
                }

                if (child.childCount > 0)
                {
                    SetVisibilityRecursive(child, active, patterns);
                }
            }
        }

        private static void SetActiveRecursive(Transform parent, bool active)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(active);
                if (child.childCount > 0)
                {
                    SetActiveRecursive(child, active);
                }
            }
        }
    }
}
