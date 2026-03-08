using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Core;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Runtime debug component for previewing interior generation without player interaction.
    /// Attach to a GameObject, configure parameters in Inspector, and press Play.
    /// Generates and renders an interior at the GameObject's position.
    /// Use with a free-flying camera to inspect results.
    /// </summary>
    public class InteriorDebugSpawner : MonoBehaviour
    {
        [Header("Generation Parameters")]
        public int seed = 42;
        public InteriorPreset preset;

        [Header("Building Context")]
        public BuildingCategory category = BuildingCategory.Residential;
        public ShopSubtype shopSubtype = ShopSubtype.None;
        [Range(0, 2)] public int tier = 1;
        [Range(1, 10)] public int floors = 3;
        [Range(5f, 30f)] public float footprintWidth = 15f;
        [Range(5f, 30f)] public float footprintHeight = 12f;
        [Range(0, 3)] public int shapeType;
        public bool isLandmark;
        public GeneratorType mapType = GeneratorType.Organic;

        [Header("Runtime")]
        public InteriorRenderer interiorRenderer;
        public FloorNavigator floorNavigator;

        [Header("Auto-generate on Start")]
        public bool generateOnStart = true;

        [Header("Debug Info (read-only)")]
        [SerializeField] private int _totalRooms;
        [SerializeField] private int _totalDiscoveries;
        [SerializeField] private string _currentFloorLabel;

        private InteriorMapData _lastData;

        void Start()
        {
            if (generateOnStart)
                Generate();
        }

        void Update()
        {
            if (interiorRenderer != null)
                _currentFloorLabel = interiorRenderer.GetCurrentFloorLabel();

            // Quick floor switch keys for debugging
            if (Input.GetKeyDown(KeyCode.PageUp) && interiorRenderer != null)
                interiorRenderer.GoUpFloor();
            if (Input.GetKeyDown(KeyCode.PageDown) && interiorRenderer != null)
                interiorRenderer.GoDownFloor();

            // Regenerate with new random seed
            if (Input.GetKeyDown(KeyCode.F5))
            {
                seed = UnityEngine.Random.Range(0, int.MaxValue);
                Generate();
            }
        }

        [ContextMenu("Generate Interior")]
        public void Generate()
        {
            if (interiorRenderer == null)
            {
                Debug.LogError("[InteriorDebugSpawner] InteriorRenderer reference is required.");
                return;
            }

            var context = new InteriorBuildingContext
            {
                buildingId = $"debug_{seed}",
                footprintWidth = footprintWidth,
                footprintHeight = footprintHeight,
                angle = 0f,
                tier = tier,
                floors = floors,
                shapeType = shapeType,
                isLandmark = isLandmark,
                category = category,
                shopSubtype = shopSubtype,
                elevation = 0f,
                nearCoast = false,
                nearRiver = false,
                nearHill = false,
                mapType = mapType
            };

            var data = InteriorMapGenerator.Generate(context, preset, seed);
            _lastData = data;

            _totalRooms = data.totalRoomCount;
            _totalDiscoveries = data.totalDiscoveryCount;

            interiorRenderer.Render(data, transform.position, preset, context.buildingId, seed);

            if (floorNavigator != null)
                floorNavigator.Initialize(data, transform.position);

            Debug.Log($"[InteriorDebugSpawner] Generated: {data.floors.Count} floors, " +
                      $"{_totalRooms} rooms, {_totalDiscoveries} discoveries (seed={seed})");
        }
    }
}
