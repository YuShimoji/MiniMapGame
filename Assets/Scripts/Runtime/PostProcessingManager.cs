using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Controls URP Volume post-processing parameters per theme.
    /// Manages Bloom, Vignette, ColorAdjustments, and Tonemapping.
    /// </summary>
    public class PostProcessingManager : MonoBehaviour
    {
        public Volume volume;

        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private Tonemapping _tonemapping;

        void Awake()
        {
            CacheOverrides();
        }

        private void CacheOverrides()
        {
            if (volume == null || volume.profile == null) return;
            volume.profile.TryGet(out _bloom);
            volume.profile.TryGet(out _vignette);
            volume.profile.TryGet(out _colorAdjustments);
            volume.profile.TryGet(out _tonemapping);
        }

        public void ApplyTheme(MapTheme theme)
        {
            if (theme == null) return;
            if (_bloom == null) CacheOverrides();

            if (_bloom != null)
            {
                _bloom.intensity.Override(theme.bloomIntensity);
                _bloom.threshold.Override(theme.bloomThreshold);
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(theme.vignetteIntensity);
                _vignette.color.Override(theme.vignetteColor);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.contrast.Override(theme.contrast);
                _colorAdjustments.saturation.Override(theme.saturation);
            }

            if (_tonemapping != null)
            {
                _tonemapping.mode.Override(TonemappingMode.ACES);
            }
        }
    }
}
