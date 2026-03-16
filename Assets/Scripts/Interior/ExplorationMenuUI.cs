using System.Text;
using UnityEngine;
using TMPro;

namespace MiniMapGame.Interior
{
    /// <summary>
    /// Tab-key toggle menu showing exploration progress.
    /// When inside a building: shows active building detail + all records.
    /// When outside: shows all explored buildings summary.
    /// </summary>
    public class ExplorationMenuUI : MonoBehaviour
    {
        [Header("References")]
        public ExplorationProgressManager explorationProgress;

        [Header("UI")]
        public GameObject menuPanel;
        public TextMeshProUGUI menuText;

        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.I;

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleMenu();
        }

        private void ToggleMenu()
        {
            if (menuPanel == null) return;
            bool show = !menuPanel.activeSelf;
            menuPanel.SetActive(show);
            if (show) RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (explorationProgress == null || menuText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("<size=22><b>Exploration Records</b></size>\n");

            var activeRecord = explorationProgress.GetActiveRecord();
            if (activeRecord != null)
            {
                AppendBuildingDetail(sb, activeRecord);
                sb.AppendLine("\n<color=#888>--- Other Buildings ---</color>\n");
            }

            int count = 0;
            foreach (var kv in explorationProgress.GetAllRecords())
            {
                if (kv.Value == activeRecord) continue;
                AppendBuildingSummary(sb, kv.Value);
                count++;
            }

            if (count == 0 && activeRecord == null)
                sb.AppendLine("<color=#666>No buildings explored yet.</color>");

            menuText.text = sb.ToString();
        }

        private void AppendBuildingSummary(StringBuilder sb, BuildingExplorationRecord r)
        {
            string status = r.IsComplete ? " <color=#E0C030>[COMPLETE]</color>" : "";
            string shortId = ShortenId(r.buildingId);
            sb.AppendLine($"<b>{shortId}</b>{status}");
            sb.AppendLine($"  Floor: {r.VisitedFloorCount}/{r.totalFloors}  " +
                          $"Items: {r.CollectedCount}/{r.totalDiscoveries}");
        }

        private void AppendBuildingDetail(StringBuilder sb, BuildingExplorationRecord r)
        {
            string status = r.IsComplete ? " <color=#E0C030>[COMPLETE]</color>" : "";
            string shortId = ShortenId(r.buildingId);
            sb.AppendLine($"<size=18><b>Current: {shortId}</b>{status}</size>");
            sb.AppendLine($"  Floor: {r.VisitedFloorCount}/{r.totalFloors}  " +
                          $"Items: {r.CollectedCount}/{r.totalDiscoveries}");

            if (r.keyDoorStatuses.Count > 0)
            {
                sb.AppendLine("\n  <b>Key-Door Status:</b>");
                foreach (var kd in r.keyDoorStatuses)
                {
                    string keyLabel = kd.keyFound ? "<color=#6B6>Found</color>" : "---";
                    string doorLabel = kd.doorOpened
                        ? "<s>Locked</s> <color=#6B6>Opened</color>"
                        : "<color=#C44>Locked</color>";
                    sb.AppendLine($"    Door {kd.doorIndex}: Key:{keyLabel} | {doorLabel}");
                }
            }
        }

        /// <summary>
        /// Shorten building IDs like "bld_x123_y456" for display.
        /// </summary>
        private static string ShortenId(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return "???";
            if (buildingId.Length <= 16) return buildingId;
            return buildingId[..16] + "...";
        }
    }
}
