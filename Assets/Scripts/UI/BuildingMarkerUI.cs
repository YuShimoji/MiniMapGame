using UnityEngine;
using TMPro;
using MiniMapGame.Data;

namespace MiniMapGame.UI
{
    /// <summary>
    /// World-space UI marker above a building showing exploration state.
    /// Billboard-faces camera and LODs by distance.
    /// SP-020 Layer 2.
    /// </summary>
    public class BuildingMarkerUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Main icon text (TMP). Shows category symbol or '?'.")]
        public TextMeshPro iconText;
        [Tooltip("Progress text below icon. Shows 'n/n Floors, n/n Items'.")]
        public TextMeshPro progressText;

        [Header("Distance LOD")]
        public float nearDistance = 30f;
        public float farDistance = 80f;

        [Header("Scale")]
        public float baseScale = 0.4f;
        public float minScale = 0.15f;

        private Camera _mainCamera;
        private BuildingMarkerState _state = BuildingMarkerState.Unknown;
        private string _buildingId;
        private BuildingCategory _category;
        private int _totalFloors;

        // Category icon characters (simple ASCII for TMP without custom font atlas)
        private static readonly string[] CategoryIcons = { "H", "S", "F", "P", "*" };
        // Residential=H(ouse), Commercial=S(hop), Industrial=F(actory), Public=P, Special=*

        // State colors
        private static readonly Color ColorUnknown = new(0.9f, 0.9f, 0.9f, 0.8f);
        private static readonly Color ColorDiscovered = new(0.95f, 0.95f, 0.95f, 0.9f);
        private static readonly Color ColorEntered = new(0.5f, 0.5f, 0.55f, 0.85f);
        private static readonly Color ColorInProgress = new(0.95f, 0.65f, 0.15f, 0.95f);
        private static readonly Color ColorComplete = new(0.2f, 0.85f, 0.35f, 0.95f);

        public void Initialize(string buildingId, BuildingCategory category, int totalFloors)
        {
            _buildingId = buildingId;
            _category = category;
            _totalFloors = totalFloors;
            _mainCamera = Camera.main;

            SetState(BuildingMarkerState.Unknown);
        }

        public void SetState(BuildingMarkerState state)
        {
            _state = state;
            UpdateVisuals();
        }

        public void SetState(BuildingMarkerState state, int visitedFloors, int totalFloors,
            int collectedItems, int totalItems)
        {
            _state = state;
            _totalFloors = totalFloors;
            UpdateVisuals();

            if (progressText != null)
            {
                if (state == BuildingMarkerState.InProgress)
                    progressText.text = $"{visitedFloors}/{totalFloors}F {collectedItems}/{totalItems}";
                else if (state == BuildingMarkerState.Complete)
                    progressText.text = "\u2713"; // checkmark
                else
                    progressText.text = "";
            }
        }

        private void UpdateVisuals()
        {
            if (iconText == null) return;

            switch (_state)
            {
                case BuildingMarkerState.Unknown:
                    iconText.text = "";
                    if (progressText != null) progressText.text = "";
                    gameObject.SetActive(false);
                    return;

                case BuildingMarkerState.Discovered:
                    iconText.text = "?";
                    iconText.color = ColorDiscovered;
                    if (progressText != null) progressText.text = "";
                    break;

                case BuildingMarkerState.Entered:
                    iconText.text = GetCategoryIcon();
                    iconText.color = ColorEntered;
                    if (progressText != null) progressText.text = "";
                    break;

                case BuildingMarkerState.InProgress:
                    iconText.text = GetCategoryIcon();
                    iconText.color = ColorInProgress;
                    break;

                case BuildingMarkerState.Complete:
                    iconText.text = GetCategoryIcon();
                    iconText.color = ColorComplete;
                    if (progressText != null)
                    {
                        progressText.text = "\u2713";
                        progressText.color = ColorComplete;
                    }
                    break;
            }

            gameObject.SetActive(true);
        }

        private string GetCategoryIcon()
        {
            int idx = (int)_category;
            return idx >= 0 && idx < CategoryIcons.Length ? CategoryIcons[idx] : "?";
        }

        void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // Billboard: face camera
            transform.rotation = _mainCamera.transform.rotation;

            // Distance-based LOD
            float dist = Vector3.Distance(_mainCamera.transform.position, transform.position);

            if (dist > farDistance)
            {
                // Too far: hide everything
                if (iconText != null) iconText.enabled = false;
                if (progressText != null) progressText.enabled = false;
                return;
            }

            if (iconText != null) iconText.enabled = true;

            if (dist < nearDistance)
            {
                // Near: show full (icon + progress)
                if (progressText != null) progressText.enabled = true;
            }
            else
            {
                // Mid: icon only
                if (progressText != null) progressText.enabled = false;
            }

            // Scale by distance for readability
            float t = Mathf.InverseLerp(nearDistance * 0.5f, farDistance, dist);
            float scale = Mathf.Lerp(baseScale, minScale, t);
            transform.localScale = Vector3.one * scale;
        }
    }
}
