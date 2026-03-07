using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Controls ambient dust particle system. Theme-aware color, map-size-aware shape.
    /// </summary>
    public class AmbientParticleController : MonoBehaviour
    {
        public ParticleSystem dustSystem;

        public void ApplyTheme(MapTheme theme)
        {
            if (dustSystem == null || theme == null) return;
            var main = dustSystem.main;
            main.startColor = theme.ambientParticleColor;
        }

        public void ConfigureForMap(MapPreset preset)
        {
            if (dustSystem == null || preset == null) return;
            var shape = dustSystem.shape;
            shape.scale = new Vector3(preset.worldWidth, 20f, preset.worldHeight);
            shape.position = new Vector3(preset.worldWidth * 0.5f, 10f, preset.worldHeight * 0.5f);
        }
    }
}
