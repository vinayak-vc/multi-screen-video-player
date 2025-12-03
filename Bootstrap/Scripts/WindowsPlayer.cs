using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;

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

        public static readonly string[] VideoExtensions = { ".mp4" };
        public static readonly string[] AudioExtensions = { ".wav", ".mp3" };

        [SerializeField]
        private WindowsUIController windowsUIController;

        [SerializeField]
        private VideoPlayerController videoContainerPrefab;

        private readonly List<VideoPlayerController> _videoContainerList = new List<VideoPlayerController>();

        [SerializeField]
        private TextMeshProUGUI ipAddress;

        [SerializeField]
        private Camera cameraPrefab;

        [SerializeField]
        private bool ssPlayer;

        private bool _loop = true;
        private VideoPlayerController _currentVideoPlayerController;
        private string _videoPathJsonString;
        private int _index;
        private StereoscopicComController stereoComController;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
            ipAddress.text = IPManager.GetIP(ADDRESSFAM.IPv4);
        }

        private void Start() {
            if (ssPlayer) {
                stereoComController = new StereoscopicComController();
                stereoComController.Connect();
            }
        }

        private void Update() {
            ipAddress.gameObject.SetActive(Input.GetKey(KeyCode.F5));

            if (Input.GetKeyDown(KeyCode.F4)) {
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
                if (_currentVideoPlayerController != null && _currentVideoPlayerController.GetIsPrepared() && _currentVideoPlayerController.GetIsPlaying()) {
                    UpdateProgress(_currentVideoPlayerController.GetTime(), _currentVideoPlayerController.GetLength());
                }
                yield return new WaitForSecondsRealtime(1);
            }
        }

        public void FillVideoContainerList(VideoContainerList containerList) {
            // Check if multiple displays exist
            for (int i = 1; i < Display.displays.Length; i++) {
                Display.displays[i].Activate();
            }

            for (int index = 0; index < containerList.videoContainerList.Count; index++) {
                VideoContainer videoContainer = containerList.videoContainerList[index];
                VideoPlayerController videoPlayerController = Instantiate(videoContainerPrefab, transform);
                videoPlayerController.Init(videoContainer);
                _videoContainerList.Add(videoPlayerController);
            }
            if (containerList.videoContainerList != null) {
                for (int i = 0; i < containerList.videoContainerList[0].videoPath.Length; i++) {
                    Camera cam = Instantiate(cameraPrefab, transform);
                    cam.targetDisplay = i;
                }
            }

            _videoPathJsonString = JsonUtility.ToJson(containerList);
            Log("Filled VideoContainerList with " + containerList.videoContainerList.Count + " valid folders.");

            _index = -1;
            PlayNextVideo();
            StartCoroutine(StartContinuouslyUpdateProgress());
        }

        public void ExecuteCommand(string command) {
            Log($"Executing Command : {command}");
            string[] parts = command.Split(Commands.Separator);
            if (ssPlayer) {
                if (parts.Length > 1) {
                    ExecuteCommandSSVideoPlayer(parts[0], parts.Skip(1)
                        .ToArray());
                } else {
                    ExecuteCommandSSVideoPlayer(parts[0]);
                }
            } else {
                if (parts.Length > 1) {
                    ExecuteCommandVideoPlayer(parts[0], parts.Skip(1)
                        .ToArray());
                } else {
                    ExecuteCommandVideoPlayer(parts[0]);
                }
            }
        }

        public void ExecuteCommandVideoPlayer(string command, params string[] args) {
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

                case Commands.Seek: {
                    if (args.Length > 1 && double.TryParse(args[0], out double seekTime)) {
                        _currentVideoPlayerController.Seek(seekTime);
                    }
                    break;
                }

                case Commands.SetPlaybackSpeed: {
                    if (float.TryParse(args[0], out float speed)) {
                        _currentVideoPlayerController.SetPlaybackSpeed(speed);
                    }
                    break;
                }

                case Commands.NameVideo:
                    NameVideo();
                    return;

                case Commands.PlayThisVideo:
                    PlayThisVideo(args[0]);
                    return;
                case Commands.Loop:
                    LoopChange(args[0]);
                    return;
            }
        }

        public void ExecuteCommandSSVideoPlayer(string command, params string[] args) {
            switch (command) {
                case Commands.Play:
                    stereoComController.Play();
                    break;
                case Commands.Pause:
                    stereoComController.Pause();
                    break;
                case Commands.Stop:
                    stereoComController.Stop();
                    break;
                case Commands.ToggleMute:
                    stereoComController.ToggleMute();
                    break;
                case Commands.Restart:
                    stereoComController.Restart();
                    break;

                case Commands.Seek: {
                    if (args.Length > 1 && double.TryParse(args[0], out double seekTime)) {
                        stereoComController.Seek(seekTime);
                    }
                    break;
                }

                case Commands.SetPlaybackSpeed: {
                    if (float.TryParse(args[0], out float speed)) {
                        stereoComController.SetPlaybackSpeed(speed);
                    }
                    break;
                }

                case Commands.NameVideo:
                    NameVideoSS();
                    return;

                case Commands.PlayThisVideo:
                    PlayThisVideoSSPlayer(args[0]);
                    return;
                case Commands.Loop:
                    LoopChange(args[0]);
                    return;
            }
        }

        private void LoopChange(string loop) {
            this._loop = bool.Parse(loop);
        }

        public void PlayThisVideoSSPlayer(string folderName, bool sendDataToAndroid = false) {
            for (int i = 0; i < _videoContainerList.Count; i++) {
                VideoPlayerController videoPlayerController = _videoContainerList[i];
                if (videoPlayerController.GetFolderName() == folderName) {
                    VideoContainer videoContainer = videoPlayerController.GetContainer();
                    Log("Video 1 : " + videoContainer.videoPath[0]);
                    Log("Video 2 : " + videoContainer.videoPath[1]);
                    stereoComController.OpenLeftRightFiles(videoContainer.videoPath[0], videoContainer.videoPath[1], videoContainer.audioPath);
                    _currentVideoPlayerController = videoPlayerController;
                    _index = i;
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].HighLightButton();
                    Log("Playing video: " + folderName, videoPlayerController);
                    if (sendDataToAndroid) {
                        BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
                    }
                } else {
                    videoPlayerController.Stop();
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].DeHighLightButton();
                }
            }
        }

        public void PlayThisVideo(string folderName, bool sendDataToAndroid = false) {
            for (int i = 0; i < _videoContainerList.Count; i++) {
                VideoPlayerController videoPlayerController = _videoContainerList[i];
                if (videoPlayerController.GetFolderName() == folderName) {
                    videoPlayerController.Play();
                    _currentVideoPlayerController = videoPlayerController;
                    _index = i;
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].HighLightButton();
                    Log("Playing video: " + folderName, videoPlayerController);
                    if (sendDataToAndroid) {
                        if (false) {
                        }
                        BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
                    }
                } else {
                    videoPlayerController.Stop();
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].DeHighLightButton();
                }
            }
        }

        public void PlayNextVideo() {
            _index = (_index + 1) % _videoContainerList.Count;
            Log(_index + "");
            PlayThisVideo(_videoContainerList[_index].GetFolderName());
            BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
        }

        private void NameVideo() {
            UpdateProgress(_videoContainerList[0].GetTime(), _videoContainerList[0].GetLength());
            BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.NameVideo}{Commands.Separator}{_videoPathJsonString}{Commands.Separator}{_index}");
        }

        private void NameVideoSS() {
            UpdateProgress(_videoContainerList[0].GetTime(), _videoContainerList[0].GetLength());
            BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.NameVideo}{Commands.Separator}{_videoPathJsonString}{Commands.Separator}{_index}");
        }

        private void UpdateProgress(double currentTime, double length) {
            BootstrapManager.Instance.networkObject?.SendCommandToClient($"{Commands.SliderData}{Commands.Separator}{currentTime}{Commands.Separator}{length}");
        }

        public IEnumerator PlayAudioFromFile(string filePath, Action<AudioClip> onAudioClipLoaded) {
            string ext = Path.GetExtension(filePath)
                .ToLower();
            if (ext == ".wav") {
                byte[] wavData = System.IO.File.ReadAllBytes(filePath);
                AudioClip audioClip = CreateWavClip(wavData, Path.GetFileNameWithoutExtension(filePath));
                if (audioClip != null) {
                    Log("Playing WAV audio...");
                    onAudioClipLoaded.Invoke(audioClip);
                } else {
                    LogError("Error loading WAV file.");
                }
                yield break; // No need to yield for wav
            } else {
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG)) {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success) {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        Log("Playing MP3 audio...");
                        onAudioClipLoaded.Invoke(audioClip);
                    } else {
                        LogError("Error loading MP3: " + www.error);
                    }
                }
            }
        }

        private AudioClip CreateWavClip(byte[] wavFile, string clipName = "AudioClip") {
            int headerSize = 44;
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
        
        void OnDestroy() {
            stereoComController.Dispose();
        }
    }
}
