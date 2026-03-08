using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using MiniMapGame.Runtime;
using MiniMapGame.Player;
using MiniMapGame.MiniGame;
using MiniMapGame.Data;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Controls building entry/exit: generates interior, toggles exterior visibility,
    /// teleports player, switches camera mode. Supports both v2 (context-aware) and
    /// legacy (seed-only) generation paths.
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

        [Header("MiniGame")]
        public MiniGameManager miniGameManager;

        [Header("Interior Camera")]
        public float interiorCameraHeight = 40f;
        public float interiorOrthoMargin = 5f;

        public bool IsInside { get; private set; }

        private BuildingInteraction _currentBuilding;
        private Vector3 _savedPlayerPosition;
        private NavMeshAgent _playerAgent;

        void Start()
        {
            _playerAgent = playerTransform != null ? playerTransform.GetComponent<NavMeshAgent>() : null;
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

            // Try v2 generation path (context-aware)
            InteriorMapData data;
            InteriorPreset preset = GetInteriorPreset();

            if (building.context.buildingId != null && preset != null)
            {
                data = InteriorMapGenerator.Generate(building.context, preset, seed);
                SetExteriorVisible(false);
                interiorRenderer.Render(data, buildingPos, preset, building.buildingId, seed);
            }
            else
            {
                // Legacy fallback
#pragma warning disable CS0618
                data = InteriorMapGenerator.Generate(seed);
                SetExteriorVisible(false);
                interiorRenderer.Render(data, buildingPos, building.buildingId, seed);
#pragma warning restore CS0618
            }

            // Initialize floor navigation
            if (floorNavigator != null && data.floors.Count > 0)
                floorNavigator.Initialize(data, buildingPos);

            // Set visibility controller to force-show interior
            if (visibilityController != null)
                visibilityController.forceFullVisibility = true;

            // Teleport player to entrance room on ground floor
            Vector3 entranceWorld = FindEntrancePosition(data, buildingPos);
            TeleportPlayer(entranceWorld);

            // Switch camera to interior top-down mode
            float maxDist = CalculateInteriorExtent(data);
            cameraController.SetInteriorMode(
                buildingPos + new Vector3(0, interiorCameraHeight, 0),
                maxDist + interiorOrthoMargin);

            // Re-bake NavMesh for interior floor
            RebakeNavMesh();
        }

        public void ExitBuilding()
        {
            if (!IsInside) return;
            IsInside = false;

            // Abort any active mini-game
            if (miniGameManager != null)
                miniGameManager.AbortIfActive();

            // Deactivate floor navigation
            if (floorNavigator != null)
                floorNavigator.Deactivate();

            // Restore visibility controller
            if (visibilityController != null)
                visibilityController.forceFullVisibility = false;

            // Clear interior
            interiorRenderer.Clear();

            // Show exterior map
            SetExteriorVisible(true);

            // Return player to saved position
            TeleportPlayer(_savedPlayerPosition);

            // Restore camera
            cameraController.ResetToFollowMode();

            // Re-bake NavMesh for exterior
            RebakeNavMesh();

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
            // New path: find Entrance room on ground floor (floorIndex == 0)
            if (data.floors.Count > 0)
            {
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
            }

            // Legacy fallback
#pragma warning disable CS0618
            if (data.rooms != null && data.rooms.Count > 0)
            {
                var entrance = data.rooms[0];
                return buildingPos + new Vector3(entrance.position.x, 0f, entrance.position.y);
            }
#pragma warning restore CS0618

            return buildingPos;
        }

        private void TeleportPlayer(Vector3 position)
        {
            if (_playerAgent != null)
                _playerAgent.Warp(position);
            else if (playerTransform != null)
                playerTransform.position = position;
        }

        private void SetExteriorVisible(bool visible)
        {
            if (mapManager == null) return;

            if (mapManager.mapRenderer != null)
                mapManager.mapRenderer.gameObject.SetActive(visible);

            if (mapManager.buildingSpawner != null)
                mapManager.buildingSpawner.gameObject.SetActive(visible);

            if (mapManager.groundPlane != null)
                mapManager.groundPlane.SetActive(visible);
        }

        private void RebakeNavMesh()
        {
            var surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();
        }

        private float CalculateInteriorExtent(InteriorMapData data)
        {
            float maxDist = 0f;

            // New path
            if (data.floors.Count > 0)
            {
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

            // Legacy path
#pragma warning disable CS0618
            if (data.rooms != null)
            {
                foreach (var room in data.rooms)
                {
                    float d = room.position.magnitude + Mathf.Max(room.size.x, room.size.y);
                    if (d > maxDist) maxDist = d;
                }
            }
#pragma warning restore CS0618

            return maxDist;
        }
    }
}
