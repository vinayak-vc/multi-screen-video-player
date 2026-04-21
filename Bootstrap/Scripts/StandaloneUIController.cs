using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViitorCloud.MultiScreenVideoPlayer
{
    public class StandaloneUIController : MonoBehaviour
    {
        [SerializeField] private GameObject idlePanel;
        [SerializeField] private GameObject pickerPanel;
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private TMP_Text playingLabel;
        [SerializeField] private Transform pickerContent;
        [SerializeField] private Button videoButtonPrefab;

        public void ShowIdle()
        {
            SetActive(idlePanel, true);
            SetActive(pickerPanel, false);
            SetActive(playingPanel, false);
        }

        public void ShowPicker(List<VideoContainer> videos, Action<string> onSelected)
        {
            SetActive(idlePanel, false);
            SetActive(pickerPanel, true);
            SetActive(playingPanel, false);

            if (pickerContent == null || videoButtonPrefab == null) return;

            foreach (Transform child in pickerContent)
                Destroy(child.gameObject);

            foreach (VideoContainer vc in videos)
            {
                Button btn = Instantiate(videoButtonPrefab, pickerContent);
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = vc.folderName;
                string folder = vc.folderName;
                btn.onClick.AddListener(() => onSelected?.Invoke(folder));
            }
        }

        public void ShowPlaying(string folderName)
        {
            SetActive(idlePanel, false);
            SetActive(pickerPanel, false);
            SetActive(playingPanel, true);
            if (playingLabel != null) playingLabel.text = folderName;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
