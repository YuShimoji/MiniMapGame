using System.Collections.Generic;
using UnityEngine;
using MiniMapGame.GameLoop;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Manages persistent exploration records for all buildings.
    /// Subscribes to MapEventBus for automatic progress updates.
    /// </summary>
    public class ExplorationProgressManager : MonoBehaviour
    {
        [Header("References")]
        public MapEventBus eventBus;

        private readonly Dictionary<string, BuildingExplorationRecord> _records = new();
        private string _activeBuildingId;

        void OnEnable()
        {
            if (eventBus == null) return;
            eventBus.Subscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
            eventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        void OnDisable()
        {
            if (eventBus == null) return;
            eventBus.Unsubscribe<DiscoveryCollectedEvent>(OnDiscoveryCollected);
            eventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        /// <summary>
        /// Called when player enters a building. Creates or updates the record.
        /// </summary>
        public void OnBuildingEntered(string buildingId, InteriorMapData data)
        {
            _activeBuildingId = buildingId;

            if (!_records.TryGetValue(buildingId, out var record))
            {
                record = new BuildingExplorationRecord
                {
                    buildingId = buildingId,
                    hasEntered = true,
                    totalFloors = data.floors.Count,
                    totalDiscoveries = CountTotalDiscoveries(data)
                };

                // Register locked doors for key-door tracking
                for (int fi = 0; fi < data.floors.Count; fi++)
                {
                    var floor = data.floors[fi];
                    for (int di = 0; di < floor.doors.Count; di++)
                    {
                        var door = floor.doors[di];
                        if (door.isLocked)
                        {
                            record.keyDoorStatuses.Add(new KeyDoorStatus
                            {
                                doorIndex = di,
                                keyFound = false,
                                doorOpened = false
                            });
                        }
                    }
                }

                _records[buildingId] = record;
            }
            else
            {
                record.hasEntered = true;
            }

            // Mark ground floor as visited
            record.MarkFloorVisited(0);
        }

        /// <summary>
        /// Called when player exits a building.
        /// </summary>
        public void OnBuildingExited()
        {
            _activeBuildingId = null;
        }

        /// <summary>
        /// Called when player visits a new floor.
        /// </summary>
        public void OnFloorVisited(string buildingId, int floorIndex)
        {
            if (_records.TryGetValue(buildingId, out var record))
                record.MarkFloorVisited(floorIndex);
        }

        public BuildingExplorationRecord GetRecord(string buildingId)
        {
            _records.TryGetValue(buildingId, out var record);
            return record;
        }

        public BuildingExplorationRecord GetActiveRecord()
        {
            if (_activeBuildingId == null) return null;
            return GetRecord(_activeBuildingId);
        }

        public bool HasBeenExplored(string buildingId)
        {
            return _records.TryGetValue(buildingId, out var r) && r.hasEntered;
        }

        public bool IsComplete(string buildingId)
        {
            return _records.TryGetValue(buildingId, out var r) && r.IsComplete;
        }

        /// <summary>
        /// Returns all exploration records. Used for save/load and menu display.
        /// </summary>
        public Dictionary<string, BuildingExplorationRecord> GetAllRecords() => _records;

        /// <summary>
        /// Restores records from save data.
        /// </summary>
        public void RestoreRecords(List<BuildingExplorationRecord> records)
        {
            _records.Clear();
            if (records == null) return;
            foreach (var r in records)
                _records[r.buildingId] = r;
        }

        // ===== Event handlers =====

        private void OnDiscoveryCollected(DiscoveryCollectedEvent evt)
        {
            if (!_records.TryGetValue(evt.buildingId, out var record)) return;

            record.MarkDiscoveryCollected(evt.discoveryId);

            // Check if this discovery is a key for a door
            if (_activeBuildingId == evt.buildingId)
            {
                var interactionMgr = FindAnyObjectByType<InteriorInteractionManager>();
                if (interactionMgr?.SessionState != null)
                {
                    // Check all doors to see if this discovery unlocked any
                    foreach (var kv in interactionMgr.SessionState.doorKeyMap)
                    {
                        if (kv.Value == evt.discoveryId)
                        {
                            record.SetKeyFound(kv.Key);
                            break;
                        }
                    }
                }
            }
        }

        private void OnDoorUnlocked(DoorUnlockedEvent evt)
        {
            if (!_records.TryGetValue(evt.buildingId, out var record)) return;
            record.SetDoorOpened(evt.doorIndex);
        }

        // ===== Helpers =====

        private static int CountTotalDiscoveries(InteriorMapData data)
        {
            int count = 0;
            foreach (var floor in data.floors)
            {
                foreach (var furniture in floor.furniture)
                {
                    if (DiscoveryInteractable.IsDiscoveryType(furniture.type))
                        count++;
                }
            }
            return count;
        }
    }
}
