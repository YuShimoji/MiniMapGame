using UnityEngine;
using MiniMapGame.Data;

namespace MiniMapGame.Core
{
    /// <summary>
    /// Deterministic classification of buildings into categories and shop subtypes.
    /// Uses per-building SeededRng for reproducible results.
    /// </summary>
    public static class BuildingClassifier
    {
        // Tier 0 (arterial): Commercial 40%, Public 25%, Residential 20%, Special 15%
        private static readonly float[] Tier0Weights = { 0.40f, 0.25f, 0.20f, 0.15f };
        private static readonly BuildingCategory[] Tier0Cats =
        {
            BuildingCategory.Commercial, BuildingCategory.Public,
            BuildingCategory.Residential, BuildingCategory.Special
        };

        // Tier 1 (secondary): Commercial 50%, Residential 35%, Public 10%, Industrial 5%
        private static readonly float[] Tier1Weights = { 0.50f, 0.35f, 0.10f, 0.05f };
        private static readonly BuildingCategory[] Tier1Cats =
        {
            BuildingCategory.Commercial, BuildingCategory.Residential,
            BuildingCategory.Public, BuildingCategory.Industrial
        };

        // Tier 2 (back street): Residential 30%, Commercial 30%, Industrial 25%, Special 15%
        private static readonly float[] Tier2Weights = { 0.30f, 0.30f, 0.25f, 0.15f };
        private static readonly BuildingCategory[] Tier2Cats =
        {
            BuildingCategory.Residential, BuildingCategory.Commercial,
            BuildingCategory.Industrial, BuildingCategory.Special
        };

        // Tier 0 shop subtypes
        private static readonly ShopSubtype[] Tier0Shops =
        {
            ShopSubtype.Department, ShopSubtype.Bank,
            ShopSubtype.Hotel, ShopSubtype.Restaurant
        };

        // Tier 1 shop subtypes
        private static readonly ShopSubtype[] Tier1Shops =
        {
            ShopSubtype.Grocery, ShopSubtype.Pharmacy,
            ShopSubtype.Bookstore, ShopSubtype.Cafe, ShopSubtype.Clinic
        };

        // Tier 2 shop subtypes
        private static readonly ShopSubtype[] Tier2Shops =
        {
            ShopSubtype.Pawnshop, ShopSubtype.Bar,
            ShopSubtype.ArcadeShop, ShopSubtype.Laundry, ShopSubtype.Tattoo
        };

        public static InteriorBuildingContext Classify(
            MapBuilding building,
            MapPreset preset,
            MapTerrain terrain,
            ElevationMap elevMap)
        {
            // Per-building deterministic RNG
            var rng = new SeededRng(building.id.GetHashCode());

            var ctx = new InteriorBuildingContext
            {
                buildingId = building.id,
                footprintWidth = building.width,
                footprintHeight = building.height,
                angle = building.angle,
                tier = building.tier,
                isLandmark = building.isLandmark,
                floors = building.floors,
                shapeType = building.shapeType,
                mapType = preset.generatorType
            };

            // Environmental context
            ctx.elevation = elevMap?.Sample(building.position) ?? 0f;
            ctx.nearCoast = terrain?.waterBodies != null &&
                WaterGenerator.MinDistToWater(building.position,
                    terrain.waterBodies.FindAll(w => w.bodyType == Data.WaterBodyType.Coast)) < 30f;
            ctx.nearRiver = terrain?.waterBodies != null &&
                WaterGenerator.MinDistToWater(building.position,
                    terrain.waterBodies.FindAll(w => w.bodyType == Data.WaterBodyType.River)) < 20f;
            ctx.nearHill = ctx.elevation > preset.maxElevation * 0.3f;

            // Classify category
            if (building.isLandmark)
            {
                ctx.category = BuildingCategory.Special;
            }
            else
            {
                ctx.category = ClassifyCategory(rng, building.tier, preset.generatorType);
            }

            // Assign shop subtype for commercial buildings
            if (ctx.category == BuildingCategory.Commercial)
            {
                ctx.shopSubtype = ClassifyShop(rng, building.tier);
            }

            return ctx;
        }

        private static BuildingCategory ClassifyCategory(
            SeededRng rng, int tier, GeneratorType mapType)
        {
            float roll = rng.Next();

            // Apply map type bias
            float residentialBias = 0f;
            float commercialBias = 0f;
            float industrialBias = 0f;

            switch (mapType)
            {
                case GeneratorType.Rural:
                    residentialBias = 0.15f;
                    commercialBias = -0.10f;
                    break;
                case GeneratorType.Mountain:
                    industrialBias = 0.10f;
                    break;
                case GeneratorType.Grid:
                    commercialBias = 0.10f;
                    break;
            }

            float[] weights;
            BuildingCategory[] cats;

            switch (Mathf.Clamp(tier, 0, 2))
            {
                case 0:
                    weights = (float[])Tier0Weights.Clone();
                    cats = Tier0Cats;
                    break;
                case 1:
                    weights = (float[])Tier1Weights.Clone();
                    cats = Tier1Cats;
                    break;
                default:
                    weights = (float[])Tier2Weights.Clone();
                    cats = Tier2Cats;
                    break;
            }

            // Apply biases
            for (int i = 0; i < cats.Length; i++)
            {
                switch (cats[i])
                {
                    case BuildingCategory.Residential:
                        weights[i] = Mathf.Max(0.05f, weights[i] + residentialBias);
                        break;
                    case BuildingCategory.Commercial:
                        weights[i] = Mathf.Max(0.05f, weights[i] + commercialBias);
                        break;
                    case BuildingCategory.Industrial:
                        weights[i] = Mathf.Max(0.05f, weights[i] + industrialBias);
                        break;
                }
            }

            // Normalize and select
            float total = 0f;
            foreach (float w in weights) total += w;

            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i] / total;
                if (roll < cumulative) return cats[i];
            }

            return cats[cats.Length - 1];
        }

        private static ShopSubtype ClassifyShop(SeededRng rng, int tier)
        {
            ShopSubtype[] pool = tier switch
            {
                0 => Tier0Shops,
                1 => Tier1Shops,
                _ => Tier2Shops
            };

            int idx = Mathf.FloorToInt(rng.Next() * pool.Length);
            return pool[Mathf.Min(idx, pool.Length - 1)];
        }

        private static bool IsNearPolyline(Vector2 point, System.Collections.Generic.List<Vector2> polyline, float threshold)
        {
            if (polyline == null || polyline.Count < 2) return false;

            float thresholdSq = threshold * threshold;
            for (int i = 0; i < polyline.Count - 1; i++)
            {
                float distSq = PointToSegmentDistSq(point, polyline[i], polyline[i + 1]);
                if (distSq < thresholdSq) return true;
            }
            return false;
        }

        private static float PointToSegmentDistSq(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.001f) return (p - a).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            var proj = a + ab * t;
            return (p - proj).sqrMagnitude;
        }
    }
}
