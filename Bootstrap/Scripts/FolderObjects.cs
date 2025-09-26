using System;
using System.Collections.Generic;
using System.IO;

using Coffee.UIEffects;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class FolderObjects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler {
        public VideoContainer _videoContainer;
        [SerializeField] private TextMeshProUGUI folderNameText;
        [SerializeField] private List<PathInfoObject> pathInfos = new List<PathInfoObject>();
        [SerializeField] private PathInfoObject pathInfoPrefab;
        [SerializeField] private GameObject showPlayButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private UIEffect highlightObject;

        private WindowsUIController _windowsUIController;
        private string _hoverText = "";
        private void OnEnable() {
            playButton.onClick.AddListener(PlayVideoFromFolder);
            deleteButton.onClick.AddListener(DeleteVideoFromFolder);
        }

        private void OnDisable() {
            playButton.onClick.RemoveListener(PlayVideoFromFolder);
            deleteButton.onClick.RemoveListener(DeleteVideoFromFolder);
        }
        private void DeleteVideoFromFolder() {
            _windowsUIController.DeleteFolder(_videoContainer);
            Destroy(gameObject);
        }

        public FolderObjects Init(VideoContainer videoContainer, WindowsUIController windowsUIController) {
            _videoContainer = videoContainer;
            _windowsUIController = windowsUIController;
            folderNameText.text = "Folder Name: " + videoContainer.folderName;

            for (int i = 0; i < videoContainer.videoPath.Length; i++) {
                pathInfos.Add(Instantiate(pathInfoPrefab, transform).Init($"Video {i + 1}: " + Path.GetFileNameWithoutExtension(videoContainer.videoPath[i]), $"Video {i + 1}: " + videoContainer.videoPath[i]));
            }
            if (File.Exists(videoContainer.audioPath)) {
                _ = Instantiate(pathInfoPrefab, transform).Init("Audio Name: " + Path.GetFileNameWithoutExtension(videoContainer.audioPath), "Audio Path: " + videoContainer.audioPath);
            }
            
            for (int i = 0; i < _videoContainer.videoPath.Length; i++) {
                _hoverText += $"Video {i + 1}: {_videoContainer.videoPath[i]}\n\n";
            }
            return this;
        }

        private void PlayVideoFromFolder() {
            WindowsPlayer.Instance.PlayThisVideo(_videoContainer.folderName);
        }

        public void HighLightButton() {
            highlightObject.enabled = true;
            showPlayButton.gameObject.SetActive(false);
        }
        public void DeHighLightButton() {
            highlightObject.enabled = false;
            showPlayButton.gameObject.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            HoverInfo.Instance.Show(_hoverText);
        }
        public void OnPointerExit(PointerEventData eventData) {
            HoverInfo.Instance.Hide();
        }
        public void OnPointerMove(PointerEventData eventData) {
            HoverInfo.Instance.Show(_hoverText);
        }
    }
}