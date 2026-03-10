using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
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

        [Header("Movement Speed Settings")]
        public float minSpeed = 2f;
        public float maxSpeed = 8f;

        [Header("Camera Distance → Speed Mapping")]
        public float minCameraDistance = 5f;
        public float maxCameraDistance = 50f;

        [Header("Interaction UI")]
        public TextMeshProUGUI interactionMessageText;

        [Header("Interaction")]
        public KeyCode interactKey = KeyCode.E;

        private Collider _currentInteractionCollider;
        private BuildingInteraction _currentBuilding;

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
            if (_agent == null || _mainCamera == null) return;

            // Speed based on camera distance (Perspective-compatible)
            float camDist = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float zoomRatio = Mathf.InverseLerp(minCameraDistance, maxCameraDistance, camDist);
            _agent.speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(zoomRatio));

            // Click-to-move (skip if clicking on UI)
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                {
                    if (_agent.isOnNavMesh)
                        _agent.SetDestination(hit.point);
                }
            }

            // Interact with building
            if (_currentBuilding != null && Input.GetKeyDown(interactKey))
            {
                _currentBuilding.Interact();
            }

            // Face movement direction
            if (_agent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 dir = _agent.velocity.normalized;
                float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
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

            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
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
