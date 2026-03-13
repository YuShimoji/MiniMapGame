using UnityEngine;

namespace MiniMapGame.Player
{
    public class CameraController : MonoBehaviour
    {
        private enum CameraState { Following, ManualOrbit, ManualPan, Interior }

        [Header("Focus Settings")]
        public Transform playerTarget;
        public Vector3 focusOffset = new Vector3(0, 1.0f, 0);

        [Header("Camera Control")]
        public float panReleaseReturnTime = 1.0f;

        [Header("Following Behavior")]
        public Vector2 idealScreenPosition = new Vector2(0.5f, 0.4f);
        public Vector2 screenMargin = new Vector2(0.1f, 0.1f);
        public float followSmoothTime = 0.5f;

        [Header("Orbit (Rotation & Pitch)")]
        public Vector2 pitchMinMax = new Vector2(10, 85);
        public float rotationSpeed = 3f;
        public float followRotationSmoothTime = 0.3f;

        [Header("Zoom (Distance)")]
        public float initialDistance = 15f;
        [Tooltip("Logarithmic zoom sensitivity (proportional to current distance)")]
        public float zoomSpeed = 10f;
        public Vector2 distanceMinMax = new Vector2(3, 300);
        public float zoomSmoothTime = 0.2f;

        [Header("Pan")]
        public float panSpeed = 1f;

        [Header("Camera Presets")]
        [Tooltip("F2: Bird's-eye top-down view for map overview")]
        public KeyCode topDownKey = KeyCode.F2;
        [Tooltip("F3: Reset to default follow view")]
        public KeyCode resetViewKey = KeyCode.F3;
        public float topDownDistance = 200f;
        public float topDownPitch = 85f;

        private Camera _camera;
        private CameraState _state;
        private float _lastPanTime;
        private Vector3 _focusPoint;
        private Vector3 _focusPointVelocity;
        private float _targetDistance;
        private float _currentDistance;
        private float _distanceVelocity;
        private Vector2 _targetOrbitAngles;
        private Vector2 _currentOrbitAngles;
        private Vector2 _orbitVelocity;

        public float CurrentDistance => _currentDistance;

        // Saved state for restoring after interior mode
        private Vector2 _savedOrbitAngles;
        private float _savedDistance;

        void Start()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null || _camera.orthographic)
            {
                Debug.LogError("CameraController requires a Perspective Camera.");
                enabled = false;
                return;
            }

            _state = CameraState.Following;
            _lastPanTime = -panReleaseReturnTime;
            _targetDistance = _currentDistance = initialDistance;

            if (playerTarget != null)
            {
                _focusPoint = playerTarget.position + focusOffset;
            }
            else
            {
                _focusPoint = Vector3.zero;
            }
            _targetOrbitAngles = new Vector2(50f, 0f);
            _currentOrbitAngles = _targetOrbitAngles;
            ApplyCameraTransform();
        }

        void LateUpdate()
        {
            if (_state == CameraState.Interior) return;

            HandleCameraPresets();
            UpdateState();
            UpdateFocusPoint();
            UpdateCameraOrbitAndDistance();
            ApplyCameraTransform();
        }

        private void UpdateState()
        {
            bool orbitInput = Input.GetMouseButton(1);
            bool panInput = Input.GetMouseButton(2);

            if (panInput)
            {
                _state = CameraState.ManualPan;
                _lastPanTime = Time.time;
            }
            else if (orbitInput)
            {
                _state = CameraState.ManualOrbit;
            }
            else if (_state == CameraState.ManualPan)
            {
                if (Time.time - _lastPanTime > panReleaseReturnTime)
                    _state = CameraState.Following;
            }
            else if (_state == CameraState.ManualOrbit)
            {
                // Return to Following when right-click is released
                _state = CameraState.Following;
            }
        }

        private void UpdateFocusPoint()
        {
            if (_state == CameraState.ManualPan)
            {
                var mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                Quaternion yawRotation = Quaternion.Euler(0, _currentOrbitAngles.y, 0);
                Vector3 panMovement = yawRotation * new Vector3(-mouseDelta.x, 0, -mouseDelta.y);
                _focusPoint += panMovement * panSpeed * _currentDistance * 0.1f;
            }
            else if (playerTarget != null)
            {
                Vector3 targetFocus = playerTarget.position + focusOffset;
                if (_state == CameraState.Following)
                {
                    float dist = Vector3.Distance(_focusPoint, targetFocus);
                    float smooth = dist > 3f ? followSmoothTime / 4f : followSmoothTime;
                    _focusPoint = Vector3.SmoothDamp(_focusPoint, targetFocus, ref _focusPointVelocity, smooth);
                }
                else
                {
                    _focusPoint = targetFocus;
                }
            }
        }

        private void UpdateCameraOrbitAndDistance()
        {
            // Logarithmic zoom: speed proportional to current distance
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                float zoomFactor = 1f - scroll * zoomSpeed * 0.15f;
                _targetDistance *= zoomFactor;
                _targetDistance = Mathf.Clamp(_targetDistance, distanceMinMax.x, distanceMinMax.y);
            }
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, zoomSmoothTime);

            if (_state == CameraState.ManualOrbit)
            {
                _currentOrbitAngles.y += Input.GetAxis("Mouse X") * rotationSpeed;
                _currentOrbitAngles.x -= Input.GetAxis("Mouse Y") * rotationSpeed;
                _currentOrbitAngles.x = Mathf.Clamp(_currentOrbitAngles.x, pitchMinMax.x, pitchMinMax.y);
                _targetOrbitAngles = _currentOrbitAngles;
            }
            else
            {
                // Smooth orbit interpolation for preset transitions
                _currentOrbitAngles.x = Mathf.SmoothDamp(
                    _currentOrbitAngles.x, _targetOrbitAngles.x,
                    ref _orbitVelocity.x, followRotationSmoothTime);
                _currentOrbitAngles.y = Mathf.SmoothDamp(
                    _currentOrbitAngles.y, _targetOrbitAngles.y,
                    ref _orbitVelocity.y, followRotationSmoothTime);
            }
        }

        void ApplyCameraTransform()
        {
            if (_state == CameraState.Interior) return;

            Quaternion rotation = Quaternion.Euler(_currentOrbitAngles.x, _currentOrbitAngles.y, 0);
            transform.position = _focusPoint - (rotation * Vector3.forward * _currentDistance);
            transform.rotation = rotation;
        }

        private void HandleCameraPresets()
        {
            if (Input.GetKeyDown(topDownKey))
            {
                _targetDistance = topDownDistance;
                _targetOrbitAngles.x = topDownPitch;
                _state = CameraState.Following;
            }
            else if (Input.GetKeyDown(resetViewKey))
            {
                _targetDistance = initialDistance;
                _targetOrbitAngles.x = 50f;
                _state = CameraState.Following;
            }
        }

        public void SetInteriorMode(Vector3 position, float orthoSize)
        {
            if (_camera == null) return;

            _savedOrbitAngles = _currentOrbitAngles;
            _savedDistance = _currentDistance;
            _state = CameraState.Interior;

            _camera.orthographic = true;
            _camera.orthographicSize = orthoSize;
            transform.position = position;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        public void ResetToFollowMode()
        {
            if (_camera == null) return;

            _camera.orthographic = false;
            _currentOrbitAngles = _savedOrbitAngles;
            _targetOrbitAngles = _savedOrbitAngles;
            _currentDistance = _savedDistance;
            _targetDistance = _savedDistance;
            _state = CameraState.Following;
        }
    }
}
