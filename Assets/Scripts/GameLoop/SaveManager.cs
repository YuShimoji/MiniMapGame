using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Interior;
using MiniMapGame.Runtime;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Handles JSON save/load of map seed, preset, and exploration state.
    /// Save file: Application.persistentDataPath/save.json
    /// Manages JSON save/load for map seed, preset, and exploration progress.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public ExplorationProgressManager explorationProgress;

        private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        private SaveData _pendingLoad;

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGeneratedAfterLoad;
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGeneratedAfterLoad;
        }

        public void Save()
        {
            if (mapManager == null) return;

            var data = new SaveData
            {
                seed = mapManager.seed,
                presetName = mapManager.activePreset != null ? mapManager.activePreset.displayName : "",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                explorationRecords = explorationProgress != null
                    ? explorationProgress.GetAllRecords().Values.ToList()
                    : null
            };

            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveManager] Saved to {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}\n{e.StackTrace}");
            }
        }

        public void Load()
        {
            if (!HasSave())
            {
                Debug.LogWarning("[SaveManager] No save file found.");
                return;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("[SaveManager] Failed to parse save file.");
                return;
            }

            // Find matching preset by displayName
            var presets = Resources.LoadAll<MapPreset>("Presets");
            MapPreset targetPreset = null;
            foreach (var p in presets)
            {
                if (p.displayName == data.presetName)
                {
                    targetPreset = p;
                    break;
                }
            }

            if (targetPreset != null)
                mapManager.activePreset = targetPreset;

            mapManager.seed = data.seed;
            _pendingLoad = data;

            // Generate map — state will be restored in OnMapGeneratedAfterLoad
            mapManager.Generate();
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void DeleteSave()
        {
            if (HasSave())
            {
                File.Delete(SavePath);
                Debug.Log("[SaveManager] Save file deleted.");
            }
        }

        private void OnMapGeneratedAfterLoad(MapData mapData)
        {
            if (_pendingLoad == null) return;

            var pendingData = _pendingLoad;
            _pendingLoad = null;

            if (explorationProgress != null && pendingData.explorationRecords != null)
            {
                explorationProgress.RestoreRecords(pendingData.explorationRecords);

                // Refresh map markers from restored exploration data
                if (mapManager != null && mapManager.buildingSpawner != null)
                    mapManager.buildingSpawner.RefreshAllExplorationMarkers(explorationProgress);
            }

            Debug.Log($"[SaveManager] Loaded save from {SavePath}");
        }
    }
}
