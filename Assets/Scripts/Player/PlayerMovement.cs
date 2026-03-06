using UnityEngine;
using UnityEngine.AI;
using TMPro;
using MiniMapGame.Runtime;

namespace MiniMapGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private Camera _mainCamera;

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
            _agent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;

            if (_agent == null)
            {
                Debug.LogError("PlayerMovement: NavMeshAgent not found.");
                enabled = false;
                return;
            }
            if (_mainCamera == null)
            {
                Debug.LogError("PlayerMovement: Main Camera not found.");
                enabled = false;
                return;
            }

            if (interactionMessageText != null)
                interactionMessageText.gameObject.SetActive(false);

            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        void Update()
        {
            if (_agent == null || _mainCamera == null) return;

            // Speed based on camera distance (Perspective-compatible)
            float camDist = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float zoomRatio = Mathf.InverseLerp(minCameraDistance, maxCameraDistance, camDist);
            _agent.speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(zoomRatio));

            // Click-to-move
            if (Input.GetMouseButtonDown(0))
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
