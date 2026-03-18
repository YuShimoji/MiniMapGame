using System;
using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Core;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// A single discovery text entry with bilingual support.
    /// </summary>
    [System.Serializable]
    public class DiscoveryTextEntry
    {
        public string id;
        public FurnitureType furnitureType;
        public BuildingCategory buildingCategory;
        public bool hasCategoryFilter;
        public DiscoveryRarity rarity;
        public string textEN;
        public string textJA;
    }

    /// <summary>
    /// Loads discovery texts from JSON and provides selection logic.
    /// Uses SeededRng for deterministic text selection.
    /// </summary>
    public class DiscoveryTextSystem
    {
        private List<DiscoveryTextEntry> _entries;
        private readonly HashSet<string> _usedInSession = new();
        private string _locale = "en";

        // Rarity weights (out of 100)
        private const int WEIGHT_COMMON = 70;
        private const int WEIGHT_UNCOMMON = 25;
        private const int WEIGHT_RARE = 5;

        /// <summary>
        /// Load texts from the JSON resource file.
        /// Call once at startup or when locale changes.
        /// </summary>
        public void Load(string locale = "en")
        {
            _locale = locale;
            var jsonAsset = Resources.Load<TextAsset>("DiscoveryTexts/discovery-texts");
            if (jsonAsset == null)
            {
                Debug.LogWarning("[DiscoveryTextSystem] discovery-texts.json not found in Resources");
                _entries = new List<DiscoveryTextEntry>();
                return;
            }

            var wrapper = JsonUtility.FromJson<DiscoveryTextPoolJson>(jsonAsset.text);
            _entries = new List<DiscoveryTextEntry>();

            if (wrapper?.entries == null) return;

            foreach (var raw in wrapper.entries)
            {
                var entry = new DiscoveryTextEntry
                {
                    id = raw.id,
                    furnitureType = ParseFurnitureType(raw.furnitureType),
                    rarity = ParseRarity(raw.rarity),
                    textEN = raw.textEN ?? "",
                    textJA = raw.textJA ?? ""
                };

                if (!string.IsNullOrEmpty(raw.buildingCategory))
                {
                    entry.hasCategoryFilter = true;
                    entry.buildingCategory = ParseBuildingCategory(raw.buildingCategory);
                }

                _entries.Add(entry);
            }

            Debug.Log($"[DiscoveryTextSystem] Loaded {_entries.Count} entries");
        }

        /// <summary>
        /// Select a text for the given discovery context.
        /// Returns null if no matching text is found.
        /// </summary>
        public DiscoveryTextResult SelectText(
            FurnitureType furnitureType,
            BuildingCategory category,
            SeededRng rng)
        {
            if (_entries == null || _entries.Count == 0) return null;

            // Step 1: Filter by furnitureType + category (exact match)
            var candidates = FilterEntries(furnitureType, category, exactCategory: true);

            // Step 2: Fallback to furnitureType only
            if (candidates.Count == 0)
                candidates = FilterEntries(furnitureType, category, exactCategory: false);

            // Step 3: Remove already-used texts this session
            candidates.RemoveAll(e => _usedInSession.Contains(e.id));

            // Step 4: If all used, allow repeats
            if (candidates.Count == 0)
                candidates = FilterEntries(furnitureType, category, exactCategory: false);

            if (candidates.Count == 0) return null;

            // Step 5: Weighted random selection by rarity
            var selected = WeightedSelect(candidates, rng);
            _usedInSession.Add(selected.id);

            string text = _locale == "ja" && !string.IsNullOrEmpty(selected.textJA)
                ? selected.textJA
                : !string.IsNullOrEmpty(selected.textEN)
                    ? selected.textEN
                    : "...";

            return new DiscoveryTextResult
            {
                text = text,
                rarity = selected.rarity,
                entryId = selected.id
            };
        }

        /// <summary>
        /// Reset used-text tracking. Call when entering a new building.
        /// </summary>
        public void ResetSession()
        {
            _usedInSession.Clear();
        }

        public void SetLocale(string locale)
        {
            _locale = locale;
        }

        // ===== Private helpers =====

        private List<DiscoveryTextEntry> FilterEntries(
            FurnitureType type, BuildingCategory category, bool exactCategory)
        {
            var result = new List<DiscoveryTextEntry>();
            foreach (var entry in _entries)
            {
                if (entry.furnitureType != type) continue;

                if (exactCategory)
                {
                    if (!entry.hasCategoryFilter || entry.buildingCategory != category)
                        continue;
                }
                else
                {
                    // Accept entries with no category filter, or matching category
                    if (entry.hasCategoryFilter && entry.buildingCategory != category)
                        continue;
                }

                result.Add(entry);
            }
            return result;
        }

        private DiscoveryTextEntry WeightedSelect(List<DiscoveryTextEntry> candidates, SeededRng rng)
        {
            if (candidates.Count == 1) return candidates[0];

            int totalWeight = 0;
            foreach (var c in candidates)
                totalWeight += GetWeight(c.rarity);

            int roll = rng.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var c in candidates)
            {
                cumulative += GetWeight(c.rarity);
                if (roll < cumulative) return c;
            }

            return candidates[candidates.Count - 1];
        }

        private static int GetWeight(DiscoveryRarity rarity)
        {
            return rarity switch
            {
                DiscoveryRarity.Common => WEIGHT_COMMON,
                DiscoveryRarity.Uncommon => WEIGHT_UNCOMMON,
                DiscoveryRarity.Rare => WEIGHT_RARE,
                _ => WEIGHT_COMMON
            };
        }

        private static FurnitureType ParseFurnitureType(string s)
        {
            if (Enum.TryParse<FurnitureType>(s, true, out var result))
                return result;
            return FurnitureType.Table; // fallback
        }

        private static BuildingCategory ParseBuildingCategory(string s)
        {
            if (Enum.TryParse<BuildingCategory>(s, true, out var result))
                return result;
            return BuildingCategory.Residential; // fallback
        }

        private static DiscoveryRarity ParseRarity(string s)
        {
            if (Enum.TryParse<DiscoveryRarity>(s, true, out var result))
                return result;
            return DiscoveryRarity.Common;
        }
    }

    /// <summary>
    /// Result of a text selection.
    /// </summary>
    public class DiscoveryTextResult
    {
        public string text;
        public DiscoveryRarity rarity;
        public string entryId;
    }

    // ===== JSON deserialization types =====

    [System.Serializable]
    internal class DiscoveryTextPoolJson
    {
        public int version;
        public string locale_default;
        public List<DiscoveryTextEntryJson> entries;
    }

    [System.Serializable]
    internal class DiscoveryTextEntryJson
    {
        public string id;
        public string furnitureType;
        public string buildingCategory;
        public string rarity;
        public string textEN;
        public string textJA;
    }
}
