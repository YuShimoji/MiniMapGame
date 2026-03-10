using System.Linq;
using System.Text;
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

        [Header("Responsive Layout")]
        public float referenceScreenWidth = 1920f;
        public float referenceScreenHeight = 1080f;
        [Range(0.5f, 1f)] public float minPanelScale = 0.65f;
        [Range(1f, 1.5f)] public float maxPanelScale = 1f;

        private float? _customDensity;
        private RectTransform _controlPanelRect;
        private Vector3 _panelBaseScale = Vector3.one;
        private Vector2 _panelBaseAnchoredPosition;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        void Awake()
        {
            ApplyUiLabels();
            UpdatePresetName();
        }

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

            CacheResponsiveLayoutState();
            ApplyResponsiveLayout(force: true);
            ApplyUiLabels();
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

            ApplyResponsiveLayout();
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
                presetNameText.text = GetPresetDisplayLabel(mapManager.activePreset);
        }

        private void ApplyUiLabels()
        {
            SetButtonLabel(coastalButton, GetPresetDisplayLabel(coastalPreset));
            SetButtonLabel(ruralButton, GetPresetDisplayLabel(ruralPreset));
            SetButtonLabel(gridButton, GetPresetDisplayLabel(gridPreset));
            SetButtonLabel(mountainButton, GetPresetDisplayLabel(mountainPreset));
            SetButtonLabel(darkThemeButton, "Dark");
            SetButtonLabel(parchmentThemeButton, "Parchment");
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = label;
        }

        private static string GetPresetDisplayLabel(MapPreset preset)
        {
            if (preset == null) return "---";

            return preset.generatorType switch
            {
                GeneratorType.Grid => "Grid",
                GeneratorType.Mountain => "Mountain",
                GeneratorType.Rural => "Rural",
                _ when preset.hasCoast => "Coastal",
                _ => string.IsNullOrWhiteSpace(preset.displayName) ? "Preset" : preset.displayName
            };
        }

        private void OnMapGenerated(MapData data)
        {
            UpdatePresetName();

            if (seedInput != null)
                seedInput.text = mapManager.seed.ToString();

            if (statsText != null)
            {
                var sb = new StringBuilder();
                var a = data.analysis;

                // Graph stats
                sb.AppendLine($"Nodes: {data.nodes.Count}  Edges: {data.edges.Count}");
                sb.AppendLine($"Buildings: {data.buildings.Count}");
                sb.AppendLine($"Dead Ends: {a.deadEndIndices.Count}  Chokes: {a.chokeEdgeIndices.Count}");
                sb.AppendLine($"Intersections: {a.intersectionIndices.Count}  Plazas: {a.plazaIndices.Count}");

                // Terrain stats
                var terrain = data.terrain;
                if (terrain != null && terrain.hills != null && terrain.hills.Count > 0)
                {
                    int clusterCount = terrain.hillClusters?.Count ?? 0;
                    sb.AppendLine($"--- Terrain ---");
                    sb.AppendLine($"Hills: {terrain.hills.Count}  Clusters: {clusterCount}");

                    if (terrain.hillClusters != null && clusterCount > 0)
                    {
                        var byType = terrain.hillClusters.GroupBy(c => c.type)
                            .OrderBy(g => (int)g.Key);
                        sb.AppendLine(string.Join("  ",
                            byType.Select(g => $"{g.Key}: {g.Count()}")));
                    }

                    var byProfile = terrain.hills.GroupBy(h => h.profile)
                        .OrderBy(g => (int)g.Key);
                    sb.AppendLine(string.Join("  ",
                        byProfile.Select(g => $"{g.Key}: {g.Count()}")));
                }

                // Decoration stats
                if (data.decorations != null && data.decorations.Count > 0)
                {
                    sb.AppendLine($"--- Decorations: {data.decorations.Count} ---");
                    var byType = data.decorations.GroupBy(d => d.type)
                        .OrderBy(g => (int)g.Key);
                    sb.AppendLine(string.Join("  ",
                        byType.Select(g => $"{g.Key}: {g.Count()}")));
                }

                statsText.text = sb.ToString().TrimEnd();
            }
        }

        private void CacheResponsiveLayoutState()
        {
            if (controlPanel == null) return;

            _controlPanelRect = controlPanel.GetComponent<RectTransform>();
            if (_controlPanelRect == null) return;

            _panelBaseScale = _controlPanelRect.localScale;
            _panelBaseAnchoredPosition = _controlPanelRect.anchoredPosition;
        }

        private void ApplyResponsiveLayout(bool force = false)
        {
            if (_controlPanelRect == null)
                CacheResponsiveLayoutState();

            if (_controlPanelRect == null) return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (!force && screenWidth == _lastScreenWidth && screenHeight == _lastScreenHeight)
                return;

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;

            float widthScale = screenWidth / Mathf.Max(referenceScreenWidth, 1f);
            float heightScale = screenHeight / Mathf.Max(referenceScreenHeight, 1f);
            float panelScale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), minPanelScale, maxPanelScale);

            _controlPanelRect.localScale = _panelBaseScale * panelScale;
            _controlPanelRect.anchoredPosition = _panelBaseAnchoredPosition * panelScale;
        }
    }
}
