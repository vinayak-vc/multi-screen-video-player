using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using ViitorCloud.Utility.PopupManager;

using static Modules.Utility.Utility;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class WindowsUIController : MonoBehaviour {
        public GameObject addNewPanel;

        public Dictionary<string, FolderObjects> FolderObjectList;

        [SerializeField] private Button addNewButton;
        [SerializeField] private Transform folderParent;
        [SerializeField] private FolderObjects folderObjectPrefab;

        private VideoContainer _videoContainer;
        private VideoContainerList _videoContainerList;
        private string _jsonPath;
        private bool _isFirstTime;

        private async void Start() {
            FolderObjectList = new Dictionary<string, FolderObjects>();
            _videoContainerList = new VideoContainerList {
                videoContainerList = new List<VideoContainer>()
            };
            _jsonPath = Path.Combine(Application.persistentDataPath, "videoContainerList.json");
            if (File.Exists(_jsonPath)) {
                _videoContainerList = JsonUtility.FromJson<VideoContainerList>(await File.ReadAllTextAsync(_jsonPath)) ?? new VideoContainerList {
                    videoContainerList = new List<VideoContainer>()
                };
            } else {
                await CreateFile(_jsonPath);
            }

            StreamingAssetScan();

            if (_videoContainerList != null && _videoContainerList.videoContainerList.Count > 0) {
                for (int index = 0; index < _videoContainerList.videoContainerList.Count; index++) {
                    VideoContainer folders = _videoContainerList.videoContainerList[index];
                    FillVideoContainerList(folders.folderPath, false);
                }
                WindowsPlayer.Instance.FillVideoContainerList(_videoContainerList);
            } else {
                _videoContainerList = new VideoContainerList {
                    videoContainerList = new List<VideoContainer>()
                };
                _isFirstTime = true;
                addNewPanel.SetActive(true);
                PopupManager.Instance.ShowToast("No Folders Found, Please Add New Folder by click on 'Add New Folder' Button");
            }
        }

        private void OnEnable() {
            addNewButton.onClick.AddListener(OnAddNewButtonClickEvent);
        }

        private void OnDisable() {
            addNewButton.onClick.RemoveListener(OnAddNewButtonClickEvent);
        }

        private void OnAddNewButtonClickEvent() {
            string dir = FileExplorer.OpenFolder(PlayerPrefs.GetString(nameof(dir), Application.streamingAssetsPath));
            PlayerPrefs.SetString(nameof(dir), dir);
            FillVideoContainerList(dir, true);
            if (_isFirstTime) {
                _isFirstTime = true;
                if (FolderObjectList.Count > 0) {
                    WindowsPlayer.Instance.FillVideoContainerList(_videoContainerList);
                }
            }
        }

        private void StreamingAssetScan() {
            string path = Application.streamingAssetsPath;
            if (!Directory.Exists(path)) {
                Debug.LogError("StreamingAssets path not found: " + path);
                return;
            }
            string[] subFolders = Directory.GetDirectories(path);
            foreach (string folder in subFolders) {
                FillVideoContainerList(folder, true, true);
            }
        }

        private async void FillVideoContainerList(string folderPath, bool addToTheList, bool streamingAsset = false) {
            List<string> videos = new List<string>();
            string audioPath = null;

            string[] files = Directory.GetFiles(folderPath);
            string[] sortedFiles = files.OrderBy(Path.GetFileName).ToArray();

            foreach (string file in sortedFiles) {
                string ext = Path.GetExtension(file).ToLower();
                if (Array.Exists(WindowsPlayer.VideoExtensions, e => e == ext)) {
                    videos.Add(file);
                }
                if (audioPath == null && Array.Exists(WindowsPlayer.AudioExtensions, e => e == ext) && File.Exists(file)) {
                    audioPath = file;
                } else {
                    audioPath = string.Empty;
                }
            }
            VideoContainer videoContainer = new VideoContainer {
                folderPath = folderPath,
                folderName = Path.GetFileName(folderPath),
                videoPath = videos.ToArray(),
                audioPath = audioPath
            };

            if (_videoContainerList.videoContainerList.Find(x => x.folderName == videoContainer.folderName) == null || (!addToTheList && !FolderObjectList.ContainsKey(videoContainer.folderName))) {
                FolderObjectList.Add(videoContainer.folderName, Instantiate(folderObjectPrefab, folderParent).Init(videoContainer, this));
                if (addToTheList) {
                    _videoContainerList.videoContainerList.Add(videoContainer);
                    await WriteTextToFile(_jsonPath, JsonUtility.ToJson(_videoContainerList));
                    PopupManager.Instance.ShowToast("Folder Added");
                }
            } else {
                if (!streamingAsset) {
                    Log($"Folder Already Added {videoContainer.folderName}");
                    PopupManager.Instance.ShowPopup("Folder Already Added", MessageType.Error, PopupType.NoButton);
                }
            }
        }
        public async void DeleteFolder(VideoContainer videoContainerFolderName) {
            _videoContainerList.videoContainerList.Remove(videoContainerFolderName);
            FolderObjectList.Remove(videoContainerFolderName.folderName);
            await WriteTextToFile(_jsonPath, JsonUtility.ToJson(_videoContainerList));
        }
    }
}