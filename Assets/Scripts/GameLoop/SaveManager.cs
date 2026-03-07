using System.IO;
using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.GameLoop
{
    /// <summary>
    /// Handles JSON save/load of map seed, preset, and game state.
    /// Save file: Application.persistentDataPath/save.json
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public GameLoopController gameLoopController;

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
            if (mapManager == null || gameLoopController == null) return;

            var data = new SaveData
            {
                seed = mapManager.seed,
                presetName = mapManager.activePreset != null ? mapManager.activePreset.displayName : "",
                gameState = gameLoopController.State,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Saved to {SavePath}");
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

            var savedState = _pendingLoad.gameState;
            _pendingLoad = null;

            if (savedState == null || gameLoopController == null) return;

            gameLoopController.RestoreState(savedState);
            Debug.Log($"[SaveManager] Loaded save from {SavePath}");
        }
    }
}
