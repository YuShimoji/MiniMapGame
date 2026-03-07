using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniMapGame.Data;
using MiniMapGame.Runtime;
using MiniMapGame.GameLoop;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Runtime UI panel for regenerating maps with different seeds and presets.
    /// Similar to JSX reference sidebar controls.
    /// </summary>
    public class MapControlUI : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public AnalysisVisualizer analysisVisualizer;
        public ThemeManager themeManager;

        [Header("Theme Buttons")]
        public Button darkThemeButton;
        public Button parchmentThemeButton;

        [Header("Theme Assets (assign from Resources/Themes/)")]
        public MapTheme darkTheme;
        public MapTheme parchmentTheme;

        [Header("Preset Buttons")]
        public Button coastalButton;
        public Button ruralButton;
        public Button gridButton;
        public Button mountainButton;

        [Header("Controls")]
        public TMP_InputField seedInput;
        public Slider buildingDensitySlider;
        public TextMeshProUGUI densityLabel;
        public Button regenerateButton;
        public Button randomButton;

        [Header("Info Display")]
        public TextMeshProUGUI presetNameText;
        public TextMeshProUGUI statsText;

        [Header("Presets (assign from Resources/Presets/)")]
        public MapPreset coastalPreset;
        public MapPreset ruralPreset;
        public MapPreset gridPreset;
        public MapPreset mountainPreset;

        [Header("Save/Load")]
        public SaveManager saveManager;
        public Button saveButton;
        public Button loadButton;

        [Header("Panel Toggle")]
        public KeyCode toggleKey = KeyCode.F1;
        public GameObject controlPanel;

        private float? _customDensity;

        void Start()
        {
            // Wire button callbacks
            coastalButton?.onClick.AddListener(() => SelectPreset(coastalPreset));
            ruralButton?.onClick.AddListener(() => SelectPreset(ruralPreset));
            gridButton?.onClick.AddListener(() => SelectPreset(gridPreset));
            mountainButton?.onClick.AddListener(() => SelectPreset(mountainPreset));

            regenerateButton?.onClick.AddListener(Regenerate);
            randomButton?.onClick.AddListener(RandomGenerate);

            darkThemeButton?.onClick.AddListener(() => SelectTheme(darkTheme));
            parchmentThemeButton?.onClick.AddListener(() => SelectTheme(parchmentTheme));

            saveButton?.onClick.AddListener(() => saveManager?.Save());
            loadButton?.onClick.AddListener(() => saveManager?.Load());

            if (buildingDensitySlider != null)
            {
                buildingDensitySlider.onValueChanged.AddListener(OnDensityChanged);
                if (mapManager != null && mapManager.activePreset != null)
                {
                    buildingDensitySlider.value = mapManager.activePreset.buildingDensity;
                    UpdateDensityLabel(mapManager.activePreset.buildingDensity);
                }
            }

            if (seedInput != null && mapManager != null)
                seedInput.text = mapManager.seed.ToString();

            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;

            UpdatePresetName();
        }

        void OnDestroy()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey) && controlPanel != null)
                controlPanel.SetActive(!controlPanel.activeSelf);
        }

        private void SelectPreset(MapPreset preset)
        {
            if (mapManager == null || preset == null) return;
            mapManager.activePreset = preset;
            _customDensity = null;

            if (buildingDensitySlider != null)
            {
                buildingDensitySlider.value = preset.buildingDensity;
                UpdateDensityLabel(preset.buildingDensity);
            }

            UpdatePresetName();
            Regenerate();
        }

        private void Regenerate()
        {
            if (mapManager == null) return;

            if (seedInput != null && int.TryParse(seedInput.text, out int seed))
                mapManager.seed = seed;

            // Apply custom density if set
            if (_customDensity.HasValue && mapManager.activePreset != null)
                mapManager.activePreset.buildingDensity = _customDensity.Value;

            mapManager.Generate();
        }

        private void RandomGenerate()
        {
            if (mapManager == null) return;
            int newSeed = Random.Range(0, 99999);
            mapManager.seed = newSeed;

            if (seedInput != null)
                seedInput.text = newSeed.ToString();

            if (_customDensity.HasValue && mapManager.activePreset != null)
                mapManager.activePreset.buildingDensity = _customDensity.Value;

            mapManager.Generate();
        }

        private void SelectTheme(MapTheme theme)
        {
            if (themeManager == null || theme == null) return;
            themeManager.ApplyTheme(theme);
        }

        private void OnDensityChanged(float value)
        {
            _customDensity = value;
            UpdateDensityLabel(value);
        }

        private void UpdateDensityLabel(float value)
        {
            if (densityLabel != null)
                densityLabel.text = $"Building Density: {Mathf.RoundToInt(value * 100)}%";
        }

        private void UpdatePresetName()
        {
            if (presetNameText != null && mapManager != null && mapManager.activePreset != null)
                presetNameText.text = mapManager.activePreset.displayName;
        }

        private void OnMapGenerated(MapData data)
        {
            UpdatePresetName();

            if (seedInput != null)
                seedInput.text = mapManager.seed.ToString();

            if (statsText != null)
            {
                var a = data.analysis;
                statsText.text =
                    $"Nodes: {data.nodes.Count}  Edges: {data.edges.Count}\n" +
                    $"Buildings: {data.buildings.Count}\n" +
                    $"Dead Ends: {a.deadEndIndices.Count}  Chokes: {a.chokeEdgeIndices.Count}\n" +
                    $"Intersections: {a.intersectionIndices.Count}  Plazas: {a.plazaIndices.Count}";
            }
        }
    }
}
