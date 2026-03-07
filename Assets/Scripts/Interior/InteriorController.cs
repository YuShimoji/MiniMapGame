using UnityEngine;
using UnityEngine.AI;
using MiniMapGame.Runtime;
using MiniMapGame.Player;

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

            // Generate interior data
            int seed = building.buildingId.GetHashCode();
            var data = InteriorMapGenerator.Generate(seed);

            // Hide exterior map
            SetExteriorVisible(false);

            // Render interior at building's world position
            var buildingPos = building.transform.position;
            interiorRenderer.Render(data, buildingPos);

            // Teleport player to entrance room
            var entrance = data.rooms[0];
            var entranceWorld = buildingPos + new Vector3(entrance.position.x, 0f, entrance.position.y);

            if (_playerAgent != null)
            {
                _playerAgent.Warp(entranceWorld);
            }
            else
            {
                playerTransform.position = entranceWorld;
            }

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

            // Clear interior
            interiorRenderer.Clear();

            // Show exterior map
            SetExteriorVisible(true);

            // Return player to saved position
            if (_playerAgent != null)
            {
                _playerAgent.Warp(_savedPlayerPosition);
            }
            else
            {
                playerTransform.position = _savedPlayerPosition;
            }

            // Restore camera
            cameraController.ResetToFollowMode();

            // Re-bake NavMesh for exterior
            RebakeNavMesh();

            _currentBuilding = null;
        }

        private void SetExteriorVisible(bool visible)
        {
            if (mapManager == null) return;

            // Toggle MapRenderer children (roads, nodes)
            if (mapManager.mapRenderer != null)
                mapManager.mapRenderer.gameObject.SetActive(visible);

            // Toggle BuildingSpawner children (buildings)
            if (mapManager.buildingSpawner != null)
                mapManager.buildingSpawner.gameObject.SetActive(visible);

            // Toggle ground plane
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
            foreach (var room in data.rooms)
            {
                float d = room.position.magnitude + Mathf.Max(room.size.x, room.size.y);
                if (d > maxDist) maxDist = d;
            }
            return maxDist;
        }
    }
}
