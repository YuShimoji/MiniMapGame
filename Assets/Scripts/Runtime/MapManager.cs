using System;
using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Core;
using MiniMapGame.Data;
using MiniMapGame.MapGen;

namespace MiniMapGame.Runtime
{
    /// <summary>
    /// Orchestrates map generation. Owns MapData and exposes events.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        [Header("Configuration")]
        public MapPreset activePreset;
        public int seed;

        [Header("References")]
        public MapRenderer mapRenderer;
        public BuildingSpawner buildingSpawner;

        public MapData CurrentMap { get; private set; }

        public event Action<MapData> OnMapGenerated;
        public event Action OnMapCleared;

        private void Start()
        {
            if (activePreset != null)
                Generate();
        }

        public void Generate()
        {
            Generate(seed);
        }

        public void Generate(int newSeed)
        {
            seed = newSeed;
            Clear();

            var rng = new SeededRng(seed);
            float cx = activePreset.worldWidth * (0.30f + rng.Next() * 0.22f);
            float cy = activePreset.worldHeight * (0.32f + rng.Next() * 0.30f);
            var center = new Vector2(cx, cy);

            IMapGenerator generator = CreateGenerator(activePreset.generatorType);
            var (nodes, edges) = generator.Generate(rng, center, activePreset);
            var buildings = BuildingPlacer.Place(nodes, edges, rng, activePreset);
            var terrain = TerrainGenerator.Generate(rng, center, activePreset);
            var analysis = MapAnalyzer.Analyze(nodes, edges);

            CurrentMap = new MapData
            {
                nodes = nodes,
                edges = edges,
                buildings = buildings,
                terrain = terrain,
                analysis = analysis,
                center = center,
                seed = seed
            };

            if (mapRenderer != null) mapRenderer.Render(CurrentMap);
            if (buildingSpawner != null) buildingSpawner.Spawn(CurrentMap);

            OnMapGenerated?.Invoke(CurrentMap);
        }

        public void Clear()
        {
            if (mapRenderer != null) mapRenderer.Clear();
            if (buildingSpawner != null) buildingSpawner.Clear();
            CurrentMap = null;
            OnMapCleared?.Invoke();
        }

        private static IMapGenerator CreateGenerator(GeneratorType type)
        {
            return type switch
            {
                GeneratorType.Organic => new OrganicGenerator(),
                GeneratorType.Grid => new GridGenerator(),
                GeneratorType.Mountain => new MountainGenerator(),
                GeneratorType.Rural => new RuralGenerator(),
                _ => new OrganicGenerator()
            };
        }
    }
}
