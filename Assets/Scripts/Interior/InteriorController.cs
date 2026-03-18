using UnityEngine;
using MiniMapGame.Runtime;
using MiniMapGame.Player;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Controls building entry/exit: generates interior, toggles exterior visibility,
    /// teleports player, switches camera mode.
    /// </summary>
    public class InteriorController : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public InteriorRenderer interiorRenderer;
        public CameraController cameraController;
        public Transform playerTransform;

        [Header("Floor Navigation")]
        public FloorNavigator floorNavigator;

        [Header("Visibility")]
        public InteriorVisibilityController visibilityController;

        [Header("Interaction")]
        public InteriorInteractionManager interactionManager;

        [Header("Exploration")]
        public ExplorationProgressManager explorationProgress;

        [Header("Interior Camera")]
        public float interiorCameraHeight = 40f;
        public float interiorOrthoMargin = 5f;

        public bool IsInside { get; private set; }

        private BuildingInteraction _currentBuilding;
        private Vector3 _savedPlayerPosition;
        private PlayerMovement _playerMovement;

        void Start()
        {
            _playerMovement = playerTransform != null ? playerTransform.GetComponent<PlayerMovement>() : null;
        }

        void Update()
        {
            if (IsInside && Input.GetKeyDown(KeyCode.Escape))
                ExitBuilding();
        }

        public void EnterBuilding(BuildingInteraction building)
        {
            if (IsInside || building == null) return;

            _currentBuilding = building;
            _savedPlayerPosition = playerTransform.position;
            IsInside = true;

            int seed = building.buildingId.GetHashCode();
            var buildingPos = building.transform.position;

            InteriorPreset preset = GetInteriorPreset();
            var data = InteriorMapGenerator.Generate(building.context, preset, seed);
            interiorRenderer.Render(data, buildingPos, preset, building.buildingId, seed);

            // Initialize floor navigation
            if (floorNavigator != null && data.floors.Count > 0)
                floorNavigator.Initialize(data, buildingPos);

            // Set visibility controller to force-show interior
            if (visibilityController != null)
                visibilityController.forceFullVisibility = true;

            // Teleport player to entrance room on ground floor
            Vector3 entranceWorld = FindEntrancePosition(data, buildingPos);
            TeleportPlayer(entranceWorld);

            // Initialize interaction system
            if (interactionManager != null)
                interactionManager.Initialize(building.buildingId, building.context.category);

            // Record exploration progress
            if (explorationProgress != null)
                explorationProgress.OnBuildingEntered(building.buildingId, data);

            // Switch camera to building view (perspective, no ortho switch)
            float maxDist = CalculateInteriorExtent(data);
            float viewDist = maxDist + interiorOrthoMargin;
            cameraController.SetBuildingViewMode(buildingPos, viewDist);
        }

        public void ExitBuilding()
        {
            if (!IsInside) return;
            IsInside = false;

            // Deactivate floor navigation
            if (floorNavigator != null)
                floorNavigator.Deactivate();

            // Restore visibility controller
            if (visibilityController != null)
                visibilityController.forceFullVisibility = false;

            // Cleanup interaction system before clearing interior
            if (interactionManager != null)
                interactionManager.Cleanup();

            // Record exploration exit and update map marker
            if (explorationProgress != null)
            {
                string exitBuildingId = _currentBuilding != null ? _currentBuilding.buildingId : null;
                explorationProgress.OnBuildingExited();

                if (exitBuildingId != null && mapManager != null && mapManager.buildingSpawner != null)
                {
                    var record = explorationProgress.GetRecord(exitBuildingId);
                    if (record != null)
                        mapManager.buildingSpawner.SetExplorationMarker(exitBuildingId, record.IsComplete);
                }
            }

            // Clear interior
            interiorRenderer.Clear();

            // Return player to saved position
            TeleportPlayer(_savedPlayerPosition);

            // Restore camera
            cameraController.ResetToFollowMode();

            _currentBuilding = null;
        }

        private InteriorPreset GetInteriorPreset()
        {
            if (mapManager != null && mapManager.activePreset != null)
                return mapManager.activePreset.defaultInteriorPreset;
            return null;
        }

        private Vector3 FindEntrancePosition(InteriorMapData data, Vector3 buildingPos)
        {
            if (data.floors.Count == 0)
                return buildingPos;

            // Find ground floor (floorIndex == 0)
            int groundIdx = 0;
            for (int i = 0; i < data.floors.Count; i++)
            {
                if (data.floors[i].floorIndex == 0)
                {
                    groundIdx = i;
                    break;
                }
            }

            var groundFloor = data.floors[groundIdx];
            foreach (var room in groundFloor.rooms)
            {
                if (room.type == InteriorRoomType.Entrance)
                    return buildingPos + new Vector3(room.position.x, 0f, room.position.y);
            }

            // Fallback: first room on ground floor
            if (groundFloor.rooms.Count > 0)
            {
                var first = groundFloor.rooms[0];
                return buildingPos + new Vector3(first.position.x, 0f, first.position.y);
            }

            return buildingPos;
        }

        private void TeleportPlayer(Vector3 position)
        {
            if (_playerMovement != null)
                _playerMovement.Teleport(position);
            else if (playerTransform != null)
                playerTransform.position = position;
        }

        private float CalculateInteriorExtent(InteriorMapData data)
        {
            float maxDist = 0f;
            foreach (var floor in data.floors)
            {
                foreach (var room in floor.rooms)
                {
                    float d = room.position.magnitude + Mathf.Max(room.size.x, room.size.y);
                    if (d > maxDist) maxDist = d;
                }
            }
            return maxDist;
        }
    }
}
