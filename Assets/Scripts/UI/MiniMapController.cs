using UnityEngine;
using UnityEngine.UI;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.UI
{
    /// <summary>
    /// RenderTexture-based minimap. Orthographic top-down camera renders full map,
    /// displayed as RawImage in Canvas. Player indicator tracks position and rotation.
    /// </summary>
    public class MiniMapController : MonoBehaviour
    {
        [Header("References")]
        public Camera miniMapCamera;
        public RawImage miniMapImage;
        public RectTransform playerIndicator;
        public Transform playerTransform;
        public MapManager mapManager;

        [Header("Camera Settings")]
        public float cameraHeight = 500f;

        private RectTransform _mapImageRect;

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
        }

        void Start()
        {
            if (miniMapImage != null)
                _mapImageRect = miniMapImage.GetComponent<RectTransform>();

            if (miniMapCamera != null)
            {
                miniMapCamera.orthographic = true;
                miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        void LateUpdate()
        {
            if (playerTransform == null || miniMapCamera == null || _mapImageRect == null) return;

            // Convert player world position to viewport coordinates on minimap camera
            Vector3 vp = miniMapCamera.WorldToViewportPoint(playerTransform.position);

            if (playerIndicator != null)
            {
                // Position indicator within RawImage bounds
                // Pivot of indicator is at center (0.5, 0.5)
                // anchoredPosition relative to RawImage's pivot
                float w = _mapImageRect.rect.width;
                float h = _mapImageRect.rect.height;
                playerIndicator.anchoredPosition = new Vector2(
                    (vp.x - 0.5f) * w,
                    (vp.y - 0.5f) * h
                );

                // Rotate indicator to match player facing direction
                float playerY = playerTransform.eulerAngles.y;
                playerIndicator.localRotation = Quaternion.Euler(0, 0, -playerY);
            }
        }

        private void OnMapGenerated(MapData mapData)
        {
            if (miniMapCamera == null || mapManager == null) return;

            var preset = mapManager.activePreset;
            if (preset == null) return;

            // Center camera over map
            float centerX = preset.worldWidth * 0.5f;
            float centerZ = preset.worldHeight * 0.5f;
            miniMapCamera.transform.position = new Vector3(centerX, cameraHeight, centerZ);

            // Fit orthographic size to show full map height
            // Width is handled by aspect ratio of the RenderTexture
            miniMapCamera.orthographicSize = preset.worldHeight * 0.5f;
        }
    }
}
