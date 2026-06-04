using System.Text;
using TMPro;
using UnityEngine;
using MiniMapGame.Data;
using MiniMapGame.Runtime;

namespace MiniMapGame.UI
{
    /// <summary>
    /// Lightweight overlay for Gate-1 manual verification.
    /// Keeps the current preset/theme/seed visible while the user checks render quality.
    /// Deletion condition: SP-032 Slice 5 (4preset x 2theme手動検証) 完了後に削除可。
    /// </summary>
    public class VerificationChecklistUI : MonoBehaviour
    {
        [Header("References")]
        public MapManager mapManager;
        public ThemeManager themeManager;
        public GameObject panelRoot;
        public TextMeshProUGUI summaryText;
        public TextMeshProUGUI checklistText;

        [Header("Toggle")]
        public KeyCode toggleKey = KeyCode.F2;

        private string _lastPresetName;
        private string _lastThemeName;
        private int _lastSeed;

        void OnEnable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated += OnMapGenerated;
            Refresh(force: true);
        }

        void OnDisable()
        {
            if (mapManager != null)
                mapManager.OnMapGenerated -= OnMapGenerated;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey) && panelRoot != null)
                panelRoot.SetActive(!panelRoot.activeSelf);

            Refresh();
        }

        private void OnMapGenerated(MapData mapData)
        {
            Refresh(force: true);
        }

        private void Refresh(bool force = false)
        {
            string presetName = mapManager != null && mapManager.activePreset != null
                ? mapManager.activePreset.displayName
                : "---";
            string themeName = themeManager != null && themeManager.activeTheme != null
                ? themeManager.activeTheme.displayName
                : "---";
            int seed = mapManager != null ? mapManager.seed : 0;

            if (!force && presetName == _lastPresetName && themeName == _lastThemeName && seed == _lastSeed)
                return;

            _lastPresetName = presetName;
            _lastThemeName = themeName;
            _lastSeed = seed;

            if (summaryText != null)
                summaryText.text = BuildSummaryText(presetName, themeName, seed);

            if (checklistText != null)
                checklistText.text = BuildChecklistText(presetName, themeName);
        }

        private static string BuildSummaryText(string presetName, string themeName, int seed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Preset: {presetName}");
            sb.AppendLine($"Theme: {themeName}");
            sb.AppendLine($"Seed: {seed}");
            sb.Append("Record result in docs/verification/road-p4-gate-results.md");
            return sb.ToString();
        }

        private static string BuildChecklistText(string presetName, string themeName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Gate-1 checks");
            sb.AppendLine("1. Render: width tiers read clearly");
            sb.AppendLine("2. Markings: lane lines stay stable");
            sb.AppendLine("3. Intersection: joins do not break");
            sb.AppendLine("4. Setback: roads avoid building overlap");
            sb.AppendLine("5. AutoBind: preset picks correct profile");
            sb.AppendLine("6. Theme Sync: road look follows theme");
            sb.AppendLine();
            sb.AppendLine("Case matrix");
            AppendCase(sb, "C01", "Coastal", "Dark", presetName, themeName);
            AppendCase(sb, "C02", "Coastal", "Parchment", presetName, themeName);
            AppendCase(sb, "C03", "Rural", "Dark", presetName, themeName);
            AppendCase(sb, "C04", "Rural", "Parchment", presetName, themeName);
            AppendCase(sb, "C05", "Grid", "Dark", presetName, themeName);
            AppendCase(sb, "C06", "Grid", "Parchment", presetName, themeName);
            AppendCase(sb, "C07", "Mountain", "Dark", presetName, themeName);
            AppendCase(sb, "C08", "Mountain", "Parchment", presetName, themeName);
            sb.AppendLine();
            sb.Append("F2: toggle checklist");
            return sb.ToString();
        }

        private static void AppendCase(StringBuilder sb, string caseId, string preset, string theme,
            string activePreset, string activeTheme)
        {
            bool isActivePreset = IsNameMatch(activePreset, preset);
            bool isActiveTheme = IsNameMatch(activeTheme, theme);
            string marker = isActivePreset && isActiveTheme ? ">>" : "  ";
            sb.AppendLine($"{marker} {caseId} {preset} / {theme}");
        }

        private static bool IsNameMatch(string currentName, string expectedName)
        {
            return !string.IsNullOrEmpty(currentName) &&
                   currentName.ToLowerInvariant().Contains(expectedName.ToLowerInvariant());
        }
    }
}
