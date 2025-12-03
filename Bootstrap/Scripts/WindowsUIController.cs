using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using ViitorCloud.Utility.PopupManager;

using static Modules.Utility.Utility;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class WindowsUIController : MonoBehaviour {
        public GameObject addNewPanel;

        [SerializeField]
        private Button addNewButton;

        [SerializeField]
        private Transform folderParent;

        [SerializeField]
        private FolderObjects folderObjectPrefab;

        public Dictionary<string, FolderObjects> FolderObjectList {
            get;
            private set;
        }

        private VideoContainerList _videoContainerList;
        private string _jsonPath;
        private bool _isFirstTime;

        private void OnEnable() => addNewButton.onClick.AddListener(OnAddNewButtonClickEvent);
        private void OnDisable() => addNewButton.onClick.RemoveListener(OnAddNewButtonClickEvent);

        private void Awake() {
            FolderObjectList = new Dictionary<string, FolderObjects>();
            _jsonPath = Path.Combine(Application.persistentDataPath, "videoContainerList.json");
        }

        private async void Start() {
            await LoadOrInitializeVideoList();

            StreamingAssetScan();

            if (_videoContainerList.videoContainerList.Count > 0) {
                // Restore existing folders
                for (int index = 0; index < _videoContainerList.videoContainerList.Count; index++) {
                    VideoContainer folder = _videoContainerList.videoContainerList[index];
                    await FillVideoContainerList(folder.folderPath, false);
                }
                WindowsPlayer.Instance.FillVideoContainerList(_videoContainerList);
            } else {
                _isFirstTime = true;
                addNewPanel.SetActive(true);
                PopupManager.Instance.ShowToast("No Folders Found, Please Add New Folder by clicking 'Add New Folder'");
            }

            if (WindowsPlayer.Instance.isBothInSameScene) {
                BootstrapManager.OnClientConnected.Invoke(true);
            }
        }
        private async void OnAddNewButtonClickEvent() {
            try {
                string lastDir = PlayerPrefs.GetString("dir", Application.streamingAssetsPath);
                string dir = FileExplorer.OpenFolder(lastDir);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) {
                    PlayerPrefs.SetString("dir", dir);
                    await FillVideoContainerList(dir, true);

                    if (_isFirstTime && FolderObjectList.Count > 0) {
                        WindowsPlayer.Instance.FillVideoContainerList(_videoContainerList);
                        _isFirstTime = false;
                    } else {
                    }
                    WindowsPlayer.Instance.NewFolderAdded(_videoContainerList);
                } else {
                    PopupManager.Instance.ShowToast("Selected folder does not exist or is invalid");
                }
            } catch (Exception ex) {
                LogError($"Error adding new folder: {ex.Message}");
                PopupManager.Instance.ShowToast("Failed to add new folder");
            }
        }

        private void StreamingAssetScan() {
            string path = Application.streamingAssetsPath;
            if (!Directory.Exists(path)) {
                return;
            }

            foreach (string folder in Directory.GetDirectories(path)) {
                _ = FillVideoContainerList(folder, true, true); // Fire and forget
            }
        }

        private async Task FillVideoContainerList(string folderPath, bool addToTheList, bool streamingAsset = false) {
            if (!Directory.Exists(folderPath)) {
                LogError($"Folder does not exist: {folderPath}");
                return;
            }

            string[] files = Directory.GetFiles(folderPath)
                .OrderBy(Path.GetFileName)
                .ToArray();
            string[] videos = files.Where(file => WindowsPlayer.VideoExtensions.Contains(Path.GetExtension(file)
                .ToLower()))
                .ToArray();

            string audio = files.FirstOrDefault(file => WindowsPlayer.AudioExtensions.Contains(Path.GetExtension(file)
                .ToLower()));

            VideoContainer videoContainer = new() { folderPath = folderPath, folderName = Path.GetFileName(folderPath), videoPath = videos, audioPath = audio ?? string.Empty };

            bool alreadyExists = _videoContainerList.videoContainerList.Any(x => x.folderName == videoContainer.folderName) || FolderObjectList.ContainsKey(videoContainer.folderName);

            if (FolderObjectList.ContainsKey(videoContainer.folderName) == false) {
                FolderObjects obj = Instantiate(folderObjectPrefab, folderParent)
                    .Init(videoContainer, this);
                FolderObjectList.Add(videoContainer.folderName, obj);
            }


            if (!alreadyExists) {
                if (addToTheList) {
                    _videoContainerList.videoContainerList.Add(videoContainer);
                    if (!streamingAsset) {
                        await WriteTextToFile(_jsonPath, JsonUtility.ToJson(_videoContainerList));
                    }
                    PopupManager.Instance.ShowToast("Folder Added");
                }
            } else if (!streamingAsset && addToTheList) {
                Log($"Folder Already Added: {videoContainer.folderName}");
                PopupManager.Instance.ShowPopup("Folder Already Added", MessageType.Error, PopupType.NoButton);
            }
        }

        public async void DeleteFolder(VideoContainer folder) {
            _videoContainerList.videoContainerList.Remove(folder);
            FolderObjectList.Remove(folder.folderName);

            await WriteTextToFile(_jsonPath, JsonUtility.ToJson(_videoContainerList));
        }

        private async Task LoadOrInitializeVideoList() {
            if (File.Exists(_jsonPath)) {
                string jsonContent = await File.ReadAllTextAsync(_jsonPath);
                _videoContainerList = JsonUtility.FromJson<VideoContainerList>(jsonContent) ?? new VideoContainerList { videoContainerList = new List<VideoContainer>() };
            } else {
                _videoContainerList = new VideoContainerList { videoContainerList = new List<VideoContainer>() };
                await CreateFile(_jsonPath);
            }
        }
    }
}
