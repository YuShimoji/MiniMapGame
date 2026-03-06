using UnityEngine;

namespace MiniMapGame.UI
{
    public class WorldPositionTrackerUI : MonoBehaviour
    {
        [Header("Tracking Target")]
        public Transform targetTransform;

        [Header("Position Offset")]
        public Vector3 worldOffset = new Vector3(0, 1.5f, 0);

        private RectTransform _rectTransform;
        private Camera _mainCamera;

        void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _mainCamera = Camera.main;

            if (targetTransform == null)
            {
                Debug.LogWarning("WorldPositionTrackerUI: targetTransform not assigned.", gameObject);
                enabled = false;
            }
        }

        void LateUpdate()
        {
            if (targetTransform == null) return;
            Vector3 worldPos = targetTransform.position + worldOffset;
            _rectTransform.position = _mainCamera.WorldToScreenPoint(worldPos);
        }
    }
}
