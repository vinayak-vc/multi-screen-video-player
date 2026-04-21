using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViitorCloud.MultiScreenVideoPlayer
{
    /// <summary>
    /// Manages the three UI states for the QuickPlay scene:
    ///   Idle   — shows hint text and available video count
    ///   Picker — scrollable list of video files, click to play
    ///   Playing — shows now-playing label
    ///
    /// Also handles transient Loading and Error overlays.
    /// </summary>
    public class QuickPlayUIController : MonoBehaviour
    {
        // ── Panels ────────────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject idlePanel;
        [SerializeField] private GameObject pickerPanel;
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;

        // ── Idle panel ────────────────────────────────────────────────────────

        [Header("Idle Panel")]
        [SerializeField] private TMP_Text idleHintText;
        [SerializeField] private TMP_Text videoCountText;

        // ── Picker panel ──────────────────────────────────────────────────────

        [Header("Picker Panel")]
        [SerializeField] private TMP_Text pickerHintText;
        // Folder rows are managed by QuickPlayController (FolderObjects prefab).
        // pickerContentParent is on the controller; no button template needed here.

        // ── Playing panel ─────────────────────────────────────────────────────

        [Header("Playing Panel")]
        [SerializeField] private TMP_Text nowPlayingText;
        [SerializeField] private TMP_Text playingHintText;

        // ── Loading / Error overlays ──────────────────────────────────────────

        [Header("Overlays")]
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private TMP_Text errorText;

        // ── Public API ────────────────────────────────────────────────────────

        public void ShowIdle(int videoCount)
        {
            SetAllPanels(false);
            idlePanel.SetActive(true);

            if (idleHintText != null)
                idleHintText.text = "Press SPACE to play\nPress ESC to browse videos";

            if (videoCountText != null)
                videoCountText.text = videoCount > 0
                    ? $"{videoCount} video{(videoCount == 1 ? "" : "s")} available"
                    : "No videos found — set Video Folder Path in Inspector";
        }

        /// <summary>
        /// Show the picker panel. The folder rows (FolderObjects) are populated
        /// by QuickPlayController — this method just switches panels and updates
        /// the hint text.
        /// </summary>
        public void ShowPicker(int folderCount)
        {
            SetAllPanels(false);
            pickerPanel.SetActive(true);

            if (pickerHintText != null)
                pickerHintText.text = folderCount > 0
                    ? $"{folderCount} folder{(folderCount == 1 ? "" : "s")} — click Play  |  ESC to cancel"
                    : "No folders yet — use Add Folder button";
        }

        public void ShowPlaying(string videoName)
        {
            SetAllPanels(false);
            playingPanel.SetActive(true);

            if (nowPlayingText != null)
                nowPlayingText.text = videoName;

            if (playingHintText != null)
                playingHintText.text = "SPACE — pause/resume  |  ESC — browse videos";
        }

        public void ShowLoading(string videoName)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null)
                    loadingText.text = $"Loading: {videoName}...";
            }
        }

        public void ShowError(string message)
        {
            SetAllPanels(false);
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
                if (errorText != null)
                    errorText.text = message;
            }
            else
            {
                // Fallback to idle if no error panel wired up
                idlePanel.SetActive(true);
                if (idleHintText != null)
                    idleHintText.text = "Error: " + message + "\n\nPress ESC to browse";
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void SetAllPanels(bool active)
        {
            if (idlePanel != null)    idlePanel.SetActive(active);
            if (pickerPanel != null)  pickerPanel.SetActive(active);
            if (playingPanel != null) playingPanel.SetActive(active);
            if (loadingPanel != null) loadingPanel.SetActive(active);
            if (errorPanel != null)   errorPanel.SetActive(active);
        }

        private void Start()
        {
            // Safe init — hide all until controller calls ShowIdle
            SetAllPanels(false);
        }
    }
}
