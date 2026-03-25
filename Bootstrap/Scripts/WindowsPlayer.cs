using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using StereoscopicComControl;

using TMPro;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.Networking;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class WindowsPlayer : MonoBehaviour {
        public static WindowsPlayer Instance {
            get;
            private set;
        }

        public static readonly string[] VideoExtensions = {
            ".mp4"
        };
        public static readonly string[] AudioExtensions = {
            ".wav", ".mp3"
        };

        [SerializeField]
        private WindowsUIController windowsUIController;

        [SerializeField]
        private VideoPlayerController videoContainerPrefab;

        private readonly List<VideoPlayerController> _videoContainerList = new();

        [SerializeField]
        private TextMeshProUGUI ipAddress;

        [SerializeField]
        private Camera cameraPrefab;

        [SerializeField]
        private bool ssPlayer;

        public bool isBothInSameScene;

        public bool videoPlayer360;
        public float rotationSpeed;
        public Transform mainCamera;

        public MeshRenderer videoPlayer360Renderer;

        public static Action VideoLoaded;

        internal bool Loop = false;
        private VideoPlayerController _currentVideoPlayerController;
        private string _videoPathJsonString;
        private int _index;
        private StereoscopicComController _stereoComController = new StereoscopicComController();
        public BasicWebSocketClient basicWebSocketClient;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
            ipAddress.text = IPManager.GetIP(ADDRESSFAM.IPv4);
        }

        private void Start() {
            try {
                if (ssPlayer) {
                    _stereoComController.RunAsync(basicWebSocketClient);
                }
            } catch (Exception e) {
                LogError($"Failed to start StereoscopicComController: {e.Message}");
            }
        }

        private void Update() {
            ipAddress.gameObject.SetActive(Input.GetKey(KeyCode.F5));

            if (Input.GetKeyDown(KeyCode.F4) && !isBothInSameScene) {
                windowsUIController.addNewPanel.SetActive(!windowsUIController.addNewPanel.activeInHierarchy);
                if (_currentVideoPlayerController != null) {
                    if (windowsUIController.addNewPanel.activeInHierarchy) {
                        _currentVideoPlayerController.Pause();
                    } else {
                        _currentVideoPlayerController.Play();
                    }
                }
            }
        }

        private IEnumerator StartContinuouslyUpdateProgress() {
            while (true) {
                if (_currentVideoPlayerController != null) {
                    if (_currentVideoPlayerController.GetIsPrepared()) {
                        if (_currentVideoPlayerController.GetIsPlaying()) {
                            UpdateProgress(_currentVideoPlayerController.GetTime(), _currentVideoPlayerController.GetLength());
                        } else {
                            double time = _currentVideoPlayerController.GetTime();
                            double length = _currentVideoPlayerController.GetLength();
                            if (length > 0 && Math.Abs(time - length) < 0.5) {
                                _currentVideoPlayerController.OnLoopPointReached(null);
                                _currentVideoPlayerController = null;
                            }
                        }
                    }
                }
                yield return new WaitForSecondsRealtime(1);
            }
        }

        private void EnterFullScreenSSplayer() {
            StartCoroutine(enumerator());
            return;

            IEnumerator enumerator() {
                _stereoComController.SendMessage("EnterFullscreen");
                _stereoComController.SendMessage($"SetViewingMethod{Commands.Separator}SoftPageflip");
                _stereoComController.SendMessage($"SetSwapEyes{Commands.Separator}true");
                yield return new WaitForSecondsRealtime(1);
            }
        }

        public bool IsThisSSPlayer() {
            return ssPlayer;
        }

        public bool IsThis360VideoPlayer() {
            return videoPlayer360;
        }

        public void FillVideoContainerList(VideoContainerList containerList) {
            if (containerList == null || containerList.videoContainerList == null) {
                LogError("FillVideoContainerList called with null container list.");
                return;
            }

            if (!IsThisSSPlayer()) {
                for (int i = 1; i < Display.displays.Length; i++) {
                    Display.displays[i].Activate();
                }
            }

            for (int index = 0; index < containerList.videoContainerList.Count; index++) {
                VideoContainer videoContainer = containerList.videoContainerList[index];
                VideoPlayerController videoPlayerController = Instantiate(videoContainerPrefab, transform);
                videoPlayerController.Init(videoContainer);
                _videoContainerList.Add(videoPlayerController);
            }

            if (containerList.videoContainerList.Count > 0 && containerList.videoContainerList[0].videoPath != null) {
                if (!IsThisSSPlayer() && !videoPlayer360) {
                    for (int i = 0; i < containerList.videoContainerList[0].videoPath.Length; i++) {
                        Camera cam = Instantiate(cameraPrefab, transform);
                        cam.targetDisplay = i + 1;
                    }
                } else if (!videoPlayer360) {
                    Camera cam = Instantiate(cameraPrefab, transform);
                    cam.targetDisplay = 0;
                }
            }

            _videoPathJsonString = JsonUtility.ToJson(containerList);
            _index = -1;

            StartCoroutine(StartContinuouslyUpdateProgress());

            VideoLoaded?.Invoke();
        }

        public void ExecuteCommand(string command) {
            Log($"Executing Command : {command}");
            string[] parts = command.Split(Commands.Separator);
            if (parts.Length > 1) {
                ExecuteCommandVideoPlayer(parts[0], parts.Skip(1).ToArray());
            } else {
                ExecuteCommandVideoPlayer(parts[0]);
            }
        }

        public void ExecuteCommandVideoPlayer(string command, params string[] args) {
            // Commands that don't require an active video player
            switch (command) {
                case Commands.NameVideo:
                    NameVideo();
                    return;
                case Commands.PlayThisVideo:
                    if (args.Length > 0) PlayThisVideo(args[0]);
                    return;
                case Commands.Loop:
                    if (args.Length > 0) LoopChange(args[0]);
                    return;
                case Commands.FullScreen:
                    EnterFullScreenSSplayer();
                    return;
                case Commands.GetImages:
                    GetImages();
                    return;
                case Commands.Input:
                    if (args.Length >= 2) TrackpadInputReceived(args[0], args[1]);
                    else LogError($"Input command missing arguments.");
                    return;
            }

            if (_currentVideoPlayerController == null) {
                LogError($"Command '{command}' ignored: no video is currently selected.");
                return;
            }

            switch (command) {
                case Commands.Play:
                    _currentVideoPlayerController.Play();
                    break;
                case Commands.Pause:
                    _currentVideoPlayerController.Pause();
                    break;
                case Commands.Stop:
                    _currentVideoPlayerController.Stop();
                    break;
                case Commands.ToggleMute:
                    _currentVideoPlayerController.ToggleMute();
                    break;
                case Commands.Restart:
                    _currentVideoPlayerController.Restart();
                    break;

                case Commands.SetPosition: {
                    if (args.Length > 0 && double.TryParse(args[0], out double seekTime)) {
                        _currentVideoPlayerController.Seek(seekTime);
                    } else {
                        LogError($"Invalid SetPosition argument: '{(args.Length > 0 ? args[0] : "none")}'");
                    }
                    break;
                }

                case Commands.SetPlaybackSpeed: {
                    if (args.Length > 0 && float.TryParse(args[0], out float speed)) {
                        _currentVideoPlayerController.SetPlaybackSpeed(speed);
                    } else {
                        LogError($"Invalid SetPlaybackSpeed argument: '{(args.Length > 0 ? args[0] : "none")}'");
                    }
                    break;
                }
            }

            if (ssPlayer) {
                if (args.Length > 0) {
                    _stereoComController.SendMessage($"{command}{Commands.Separator}{args[0]}");
                } else {
                    _stereoComController.SendMessage(command);
                }
            }
        }

        private void TrackpadInputReceived(string inputType, string command) {
            switch (inputType) {
                case Commands.InputCommands.Delta:
                    DeltaInputReceived(command);
                    break;
            }
        }

        private void DeltaInputReceived(string command) {
            if (mainCamera == null) {
                LogError("mainCamera is not assigned on WindowsPlayer.");
                return;
            }
            Vector2 delta = FromString(command);
            mainCamera.Rotate(Vector3.up, delta.x * rotationSpeed, Space.World);
            mainCamera.Rotate(Vector3.right, -delta.y * rotationSpeed, Space.World);
        }

        private async void GetImages() {
            try {
                await basicWebSocketClient.Send($"{Commands.GetImages}{Commands.Separator}{windowsUIController.FillTheImages()}");
            } catch (Exception e) {
                LogError($"GetImages failed: {e.Message}");
            }
        }

        private void LoopChange(string loop) {
            if (bool.TryParse(loop, out bool result)) {
                Loop = result;
            } else {
                LogError($"Invalid loop value received: '{loop}'");
            }
        }

        public void PlayThisVideo(string folderName, bool sendDataToAndroid = false) {
            for (int i = 0; i < _videoContainerList.Count; i++) {
                VideoPlayerController videoPlayerController = _videoContainerList[i];
                string name = videoPlayerController.GetFolderName();
                if (name == folderName) {
                    if (ssPlayer) {
                        VideoContainer videoContainer = videoPlayerController.GetContainer();
                        _stereoComController.SendMessage($"{Commands.OpenFile}{Commands.Separator}{videoContainer.videoPath[0]}{Commands.Separator}{videoContainer.videoPath[1]}{Commands.Separator}{videoContainer.audioPath}");
                    }
                    videoPlayerController.Play();
                    _currentVideoPlayerController = videoPlayerController;
                    _index = i;
                    if (windowsUIController.FolderObjectList.TryGetValue(name, out FolderObjects folderObj)) {
                        folderObj.HighLightButton();
                    }
                    if (sendDataToAndroid) {
                        SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
                    }
                } else {
                    videoPlayerController.Stop();
                    if (windowsUIController.FolderObjectList.TryGetValue(name, out FolderObjects folderObj)) {
                        folderObj.DeHighLightButton();
                    }
                }
            }
        }

        public void PlayNextVideo() {
            if (_videoContainerList.Count == 0) {
                LogError("PlayNextVideo called but video list is empty.");
                return;
            }
            _index = (_index + 1) % _videoContainerList.Count;
            PlayThisVideo(_videoContainerList[_index].GetFolderName());
            SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
        }

        private void NameVideo() {
            if (_videoContainerList.Count == 0) return;
            UpdateProgress(_videoContainerList[0].GetTime(), _videoContainerList[0].GetLength());
            SendCommandToClient($"{Commands.NameVideo}{Commands.Separator}{_videoPathJsonString}{Commands.Separator}{_index}");
        }

        private void UpdateProgress(double currentTime, double length) {
            SendCommandToClient($"{Commands.SliderData}{Commands.Separator}{currentTime}{Commands.Separator}{length}");
        }

        internal void NewFolderAdded(VideoContainerList containerList) {
            VideoPlayerController videoPlayerController = Instantiate(videoContainerPrefab, transform);
            videoPlayerController.Init(containerList.videoContainerList[^1]);
            _videoContainerList.Add(videoPlayerController);

            _videoPathJsonString = JsonUtility.ToJson(containerList);
            string newFolderJson = JsonUtility.ToJson(containerList.videoContainerList[^1]);
            SendCommandToClient($"{Commands.NewVideo}{Commands.Separator}{newFolderJson}");
        }

        private void SendCommandToClient(string command) {
            if (!isBothInSameScene) {
                BootstrapManager.Instance.networkObject?.SendCommandToClient(command);
            } else {
                AndroidPlayer.Instance.ExecuteCommand(command);
            }
        }

        public IEnumerator PlayAudioFromFile(string filePath, Action<AudioClip> onAudioClipLoaded) {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".wav") {
                byte[] wavData = System.IO.File.ReadAllBytes(filePath);
                AudioClip audioClip = CreateWavClip(wavData, Path.GetFileNameWithoutExtension(filePath));
                if (audioClip != null) {
                    onAudioClipLoaded.Invoke(audioClip);
                } else {
                    LogError("Error loading WAV file.");
                }
                yield break;
            } else {
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG)) {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success) {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        onAudioClipLoaded.Invoke(audioClip);
                    } else {
                        LogError("Error loading MP3: " + www.error);
                    }
                }
            }
        }

        private static AudioClip CreateWavClip(byte[] wavFile, string clipName = "AudioClip") {
            const int headerSize = 44;
            if (wavFile.Length < headerSize) return null;

            int sampleRate = BitConverter.ToInt32(wavFile, 24);
            int channels = BitConverter.ToInt16(wavFile, 22);
            int sampleCount = (wavFile.Length - headerSize) / 2;

            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++) {
                short value = BitConverter.ToInt16(wavFile, headerSize + i * 2);
                data[i] = value / 32768f;
            }
            AudioClip audioClip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
            audioClip.SetData(data, 0);
            return audioClip;
        }

        public VideoPlayerController GetVideoContainer(string foldername) {
            return _videoContainerList.Find(x => x.GetFolderName() == foldername);
        }

        private void OnDisable() {
            try {
            } finally {
                if (BootstrapManager.Instance) {
                    BootstrapManager.Instance.DisconnectServer();
                }
            }
        }
    }
}
