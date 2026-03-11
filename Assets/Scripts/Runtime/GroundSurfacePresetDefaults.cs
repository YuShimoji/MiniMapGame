using MiniMapGame.Data;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Returns per-GeneratorType default tuning values for ground surface compositing.
    /// These drive shader uniform strengths without needing a new SO asset.
    /// </summary>
    public static class GroundSurfacePresetDefaults
    {
        public struct Defaults
        {
            public float hillshadeStrength;
            public float contourStrength;
            public float moistureStrength;
            public float roadInfluenceStrength;
            public float buildingInfluenceStrength;
            public float nearStart;
            public float nearEnd;
        }

        public static Defaults Get(GeneratorType type)
        {
            return type switch
            {
                GeneratorType.Mountain => new Defaults
                {
                    hillshadeStrength = 0.7f,
                    contourStrength = 0.35f,
                    moistureStrength = 0.4f,
                    roadInfluenceStrength = 0.25f,
                    buildingInfluenceStrength = 0.2f,
                    nearStart = 20f,
                    nearEnd = 80f,
                },
                GeneratorType.Rural => new Defaults
                {
                    hillshadeStrength = 0.5f,
                    contourStrength = 0.2f,
                    moistureStrength = 0.5f,
                    roadInfluenceStrength = 0.2f,
                    buildingInfluenceStrength = 0.15f,
                    nearStart = 25f,
                    nearEnd = 100f,
                },
                GeneratorType.Grid => new Defaults
                {
                    hillshadeStrength = 0.25f,
                    contourStrength = 0.1f,
                    moistureStrength = 0.2f,
                    roadInfluenceStrength = 0.4f,
                    buildingInfluenceStrength = 0.35f,
                    nearStart = 15f,
                    nearEnd = 60f,
                },
                // Organic (default / Coastal)
                _ => new Defaults
                {
                    hillshadeStrength = 0.55f,
                    contourStrength = 0.25f,
                    moistureStrength = 0.45f,
                    roadInfluenceStrength = 0.3f,
                    buildingInfluenceStrength = 0.25f,
                    nearStart = 20f,
                    nearEnd = 80f,
                },
            };
        }
    }
}
