using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private CharacterController _controller;
        private Camera _mainCamera;
        private MapManager _mapManager;

        [Header("Movement")]
        public float moveSpeed = 12f;
        public float rotationSmoothTime = 0.08f;
        public float gravity = -20f;

        [Header("Interaction UI")]
        public TextMeshProUGUI interactionMessageText;

        [Header("Interaction")]
        public KeyCode interactKey = KeyCode.E;

        private Collider _currentInteractionCollider;
        private BuildingInteraction _currentBuilding;
        private float _rotationVelocity;
        private float _verticalVelocity;

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

            EnsureController();
        }

        void OnDestroy()
        {
            if (_mapManager != null)
                _mapManager.OnMapGenerated -= HandleMapGenerated;
        }

        void Update()
        {
            if (_mainCamera == null || _controller == null) return;

            // Skip movement input while typing in a UI input field
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null)
                return;

            // WASD / arrow key input
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = Vector3.zero;

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
                move = moveDir * moveSpeed;

                // Smooth rotation toward movement direction
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle,
                    ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }

            // Gravity
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // Small downward force to stay grounded
            else
                _verticalVelocity += gravity * Time.deltaTime;

            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);

            // Building interaction
            if (_currentBuilding != null && Input.GetKeyDown(interactKey))
            {
                _currentBuilding.Interact();
            }
        }

        private void HandleMapGenerated(MapData _)
        {
            // Re-position player to map center after generation
            if (_mapManager != null && _mapManager.CurrentMap != null)
            {
                var center = _mapManager.CurrentMap.center;
                var preset = _mapManager.activePreset;
                float y = _mapManager.CurrentElevationMap != null
                    ? _mapManager.CurrentElevationMap.Sample(center) + 2f
                    : 2f;
                float worldZ = preset != null ? preset.worldHeight - center.y : center.y;

                _controller.enabled = false;
                transform.position = new Vector3(center.x, y, worldZ);
                _controller.enabled = true;
            }
        }

        private void EnsureController()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = 2f;
                _controller.radius = 0.3f;
                _controller.center = new Vector3(0f, 1f, 0f);
                _controller.slopeLimit = 60f;
                _controller.stepOffset = 0.8f;
            }
        }

        /// <summary>
        /// Teleport the player to a specific position (used by InteriorController etc.)
        /// </summary>
        public void Teleport(Vector3 position)
        {
            if (_controller != null)
            {
                _controller.enabled = false;
                transform.position = position;
                _controller.enabled = true;
            }
            else
            {
                transform.position = position;
            }
            _verticalVelocity = 0f;
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
