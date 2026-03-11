using UnityEngine;
using UnityEngine.AI;
using TMPro;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private Camera _mainCamera;
        private MapManager _mapManager;

        [Header("Movement")]
        public float moveSpeed = 12f;
        public float rotationSmoothTime = 0.08f;

        [Header("Interaction UI")]
        public TextMeshProUGUI interactionMessageText;

        [Header("Interaction")]
        public KeyCode interactKey = KeyCode.E;

        private Collider _currentInteractionCollider;
        private BuildingInteraction _currentBuilding;
        private float _rotationVelocity;

        void Start()
        {
            _mainCamera = Camera.main;
            _mapManager = FindAnyObjectByType<MapManager>();

            if (_mainCamera == null)
            {
                Debug.LogError("PlayerMovement: Main Camera not found.");
                enabled = false;
                return;
            }

            if (interactionMessageText != null)
                interactionMessageText.gameObject.SetActive(false);

            if (_mapManager != null)
                _mapManager.OnMapGenerated += HandleMapGenerated;

            TryEnsureAgent();
        }

        void OnDestroy()
        {
            if (_mapManager != null)
                _mapManager.OnMapGenerated -= HandleMapGenerated;
        }

        void Update()
        {
            if (_mainCamera == null) return;

            // Retry agent creation until NavMesh is available
            if (_agent == null || !_agent.isOnNavMesh)
            {
                TryEnsureAgent();
                if (_agent == null || !_agent.isOnNavMesh) return;
            }

            // WASD / arrow key input
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h * h + v * v > 0.01f)
            {
                // Camera-relative direction on XZ plane
                Vector3 camFwd = _mainCamera.transform.forward;
                camFwd.y = 0f;
                camFwd.Normalize();
                Vector3 camRight = _mainCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 moveDir = (camFwd * v + camRight * h).normalized;

                _agent.Move(moveDir * moveSpeed * Time.deltaTime);

                // Smooth rotation toward movement direction
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle,
                    ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }

            // Building interaction
            if (_currentBuilding != null && Input.GetKeyDown(interactKey))
            {
                _currentBuilding.Interact();
            }
        }

        private void HandleMapGenerated(MapData _)
        {
            TryEnsureAgent();
        }

        private void TryEnsureAgent()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_agent == null)
            {
                if (!NavMesh.SamplePosition(transform.position, out var hit, 20f, NavMesh.AllAreas))
                    return;

                _agent = gameObject.AddComponent<NavMeshAgent>();
                _agent.Warp(hit.position);
            }
            else if (!_agent.isOnNavMesh)
            {
                // Agent exists but off-mesh — re-warp to nearest valid position
                if (NavMesh.SamplePosition(transform.position, out var hit, 50f, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
                else
                    return;
            }

            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            _agent.ResetPath();
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_currentInteractionCollider == other) return;

            var building = other.GetComponent<BuildingInteraction>();
            if (building != null && interactionMessageText != null)
            {
                interactionMessageText.text = building.GetInteractionMessage();
                interactionMessageText.gameObject.SetActive(true);
                _currentInteractionCollider = other;
                _currentBuilding = building;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other == _currentInteractionCollider)
            {
                if (interactionMessageText != null)
                    interactionMessageText.gameObject.SetActive(false);
                _currentInteractionCollider = null;
                _currentBuilding = null;
            }
        }
    }
}
