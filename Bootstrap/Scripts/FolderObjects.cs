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
        [SerializeField] private List<PathInfoObject> pathInfos = new();
        [SerializeField] private PathInfoObject pathInfoPrefab;
        [SerializeField] private GameObject showPlayButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private UIEffect highlightObject;
        [SerializeField] private Image myImage;
        [SerializeField] private Sprite variation1;
        [SerializeField] private Sprite variation2;
        

        private WindowsUIController _windowsUIController;
        private Action<VideoContainer> _onPlayCallback;
        private Action<VideoContainer> _onDeleteCallback;
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
            if (_onDeleteCallback != null)
                _onDeleteCallback(_videoContainer);
            else if (_windowsUIController != null)
                _windowsUIController.DeleteFolder(_videoContainer);
            Destroy(gameObject);
        }

        private void PlayVideoFromFolder() {
            if (_onPlayCallback != null)
                _onPlayCallback(_videoContainer);
            else
                WindowsPlayer.Instance.PlayThisVideo(_videoContainer.folderName, true);
        }

        /// <summary>Original Init — used by WindowsUIController (networked Windows scene).</summary>
        public FolderObjects Init(VideoContainer videoContainer, WindowsUIController windowsUIController) {
            _windowsUIController = windowsUIController;
            _onPlayCallback = null;
            _onDeleteCallback = null;
            return InitShared(videoContainer);
        }

        /// <summary>Callback Init — used by standalone/QuickPlay scenes that don't need WindowsUIController.</summary>
        public FolderObjects Init(VideoContainer videoContainer, Action<VideoContainer> onPlay, Action<VideoContainer> onDelete) {
            _windowsUIController = null;
            _onPlayCallback = onPlay;
            _onDeleteCallback = onDelete;
            return InitShared(videoContainer);
        }

        private FolderObjects InitShared(VideoContainer videoContainer) {
            _videoContainer = videoContainer;
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

        public void HighLightButton() {
            //highlightObject.enabled = true;
            myImage.sprite = variation2;
            showPlayButton.gameObject.SetActive(false);
        }
        public void DeHighLightButton() {
            //highlightObject.enabled = false;
            myImage.sprite = variation1;
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