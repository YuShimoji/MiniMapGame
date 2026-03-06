using UnityEngine;
using TMPro;

namespace MiniMapGame.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(WorldPositionTrackerUI))]
    public class LabelController : MonoBehaviour
    {
        [Header("Display Range (Camera Distance)")]
        [Tooltip("Camera distance below which label is hidden (too close)")]
        public float displayRangeMin = 5f;
        [Tooltip("Camera distance above which label is hidden (too far)")]
        public float displayRangeMax = 50f;

        [Header("Full Clarity Range")]
        [Tooltip("Camera distance above which label starts fading out")]
        public float fullClarityRangeMax = 40f;
        [Tooltip("Camera distance below which label starts fading in")]
        public float fullClarityRangeMin = 10f;

        [Header("Font Size Range")]
        public float minFontSize = 12f;
        public float maxFontSize = 24f;

        private TextMeshProUGUI _text;
        private Camera _mainCamera;
        private Transform _target;

        void Start()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _mainCamera = Camera.main;

            var tracker = GetComponent<WorldPositionTrackerUI>();
            if (tracker != null)
                _target = tracker.targetTransform;

            if (displayRangeMin >= displayRangeMax || fullClarityRangeMin >= fullClarityRangeMax)
            {
                Debug.LogWarning("LabelController: Range settings invalid.", gameObject);
                enabled = false;
            }
        }

        void Update()
        {
            if (_mainCamera == null) return;

            // Use camera distance to tracked target (or camera-to-self)
            Vector3 refPoint = _target != null ? _target.position : transform.position;
            float camDist = Vector3.Distance(_mainCamera.transform.position, refPoint);

            if (camDist < displayRangeMin || camDist > displayRangeMax)
            {
                _text.enabled = false;
                return;
            }
            _text.enabled = true;

            // Alpha
            float alpha = 1f;
            if (camDist > fullClarityRangeMax)
                alpha = Mathf.InverseLerp(displayRangeMax, fullClarityRangeMax, camDist);
            else if (camDist < fullClarityRangeMin)
                alpha = Mathf.InverseLerp(displayRangeMin, fullClarityRangeMin, camDist);
            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, Mathf.Clamp01(alpha));

            // Font size
            float sizeRatio = Mathf.InverseLerp(displayRangeMin, displayRangeMax, camDist);
            _text.fontSize = Mathf.Lerp(minFontSize, maxFontSize, sizeRatio);
        }
    }
}
