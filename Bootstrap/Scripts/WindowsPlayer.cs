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

        public static Action VideoLoaded;

        internal bool Loop = false;
        private VideoPlayerController _currentVideoPlayerController;
        private string _videoPathJsonString;
        private int _index;
        private StereoscopicComController _stereoComController;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
            ipAddress.text = IPManager.GetIP(ADDRESSFAM.IPv4);
        }

        private void OnEnable() {
            //StereoscopicComController.ClientConnected += EnterFullScreenSSplayer;
        }


        private void Start() {
            try {
                if (ssPlayer) {
                    _stereoComController = new StereoscopicComController();
                    new Thread(_stereoComController.RunAsync) {
                        IsBackground = true
                    }.Start();
                }
            } catch (Exception e) {
                throw; // TODO handle exception
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
                            if (_currentVideoPlayerController.GetTime() - _currentVideoPlayerController.GetLength() == 0) {
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

        public void FillVideoContainerList(VideoContainerList containerList) {
            // Check if multiple displays exist

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
            if (containerList.videoContainerList != null) {
                if (!IsThisSSPlayer()) {
                    for (int i = 0; i < containerList.videoContainerList[0].videoPath.Length; i++) {
                        Camera cam = Instantiate(cameraPrefab, transform);
                        cam.targetDisplay = i + 1;
                    }
                } else {
                    Camera cam = Instantiate(cameraPrefab, transform);
                    cam.targetDisplay = 0;
                }
            }

            _videoPathJsonString = JsonUtility.ToJson(containerList);
            _index = -1;

            // PlayNextVideo();
            StartCoroutine(StartContinuouslyUpdateProgress());

            VideoLoaded?.Invoke();
        }

        public void ExecuteCommand(string command) {
            Log($"Executing Command : {command}");
            string[] parts = command.Split(Commands.Separator);
            if (parts.Length > 1) {
                ExecuteCommandVideoPlayer(parts[0], parts.Skip(1)
                    .ToArray());
            } else {
                ExecuteCommandVideoPlayer(parts[0]);
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

                case Commands.SetPosition: {
                    if (args.Length > 0 && double.TryParse(args[0], out double seekTime)) {
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
                case Commands.FullScreen:
                    EnterFullScreenSSplayer();
                    break;
            }
            if (ssPlayer) {
                if (args.Length > 0) {
                    _stereoComController.SendMessage($"{command}{Commands.Separator}{args[0]}");
                } else {
                    _stereoComController.SendMessage(command);
                }
            }
        }

        private void LoopChange(string loop) {
            this.Loop = bool.Parse(loop);
        }

        public void PlayThisVideo(string folderName, bool sendDataToAndroid = false) {
            for (int i = 0; i < _videoContainerList.Count; i++) {
                VideoPlayerController videoPlayerController = _videoContainerList[i];
                if (videoPlayerController.GetFolderName() == folderName) {
                    if (ssPlayer) {
                        VideoContainer videoContainer = videoPlayerController.GetContainer();
                        _stereoComController.SendMessage($"{Commands.OpenFile}{Commands.Separator}{videoContainer.videoPath[0]}{Commands.Separator}{videoContainer.videoPath[1]}{Commands.Separator}{videoContainer.audioPath}");
                    }
                    videoPlayerController.Play();
                    _currentVideoPlayerController = videoPlayerController;
                    _index = i;
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].HighLightButton();
                    if (sendDataToAndroid) {
                        SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
                    }
                } else {
                    videoPlayerController.Stop();
                    windowsUIController.FolderObjectList[videoPlayerController.GetFolderName()].DeHighLightButton();
                }
            }
        }

        public void PlayNextVideo() {
            _index = (_index + 1) % _videoContainerList.Count;
            PlayThisVideo(_videoContainerList[_index].GetFolderName());
            SendCommandToClient($"{Commands.PlayThisVideo}{Commands.Separator}{_index}");
        }

        private void NameVideo() {
            if (_videoContainerList.Count > 0) {
                UpdateProgress(_videoContainerList[0].GetTime(), _videoContainerList[0].GetLength());
                SendCommandToClient($"{Commands.NameVideo}{Commands.Separator}{_videoPathJsonString}{Commands.Separator}{_index}");
            }
        }

        private void UpdateProgress(double currentTime, double length) {
            SendCommandToClient($"{Commands.SliderData}{Commands.Separator}{currentTime}{Commands.Separator}{length}");
        }

        internal void NewFolderAdded(VideoContainerList containerList) {
            VideoPlayerController videoPlayerController = Instantiate(videoContainerPrefab, transform);
            videoPlayerController.Init(containerList.videoContainerList[^1]);
            _videoContainerList.Add(videoPlayerController);

            _videoPathJsonString = JsonUtility.ToJson(containerList);
            string newFodlerjson = JsonUtility.ToJson(containerList.videoContainerList[^1]);
            SendCommandToClient($"{Commands.NewVideo}{Commands.Separator}{newFodlerjson}");
        }

        private void SendCommandToClient(string command) {
            if (!isBothInSameScene) {
                BootstrapManager.Instance.networkObject?.SendCommandToClient(command);
            } else {
                AndroidPlayer.Instance.ExecuteCommand(command);
            }
        }

        public IEnumerator PlayAudioFromFile(string filePath, Action<AudioClip> onAudioClipLoaded) {
            string ext = Path.GetExtension(filePath)
                .ToLower();
            if (ext == ".wav") {
                byte[] wavData = System.IO.File.ReadAllBytes(filePath);
                AudioClip audioClip = CreateWavClip(wavData, Path.GetFileNameWithoutExtension(filePath));
                if (audioClip != null) {
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
                        onAudioClipLoaded.Invoke(audioClip);
                    } else {
                        LogError("Error loading MP3: " + www.error);
                    }
                }
            }
        }

        private static AudioClip CreateWavClip(byte[] wavFile, string clipName = "AudioClip") {
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

        public VideoPlayerController GetVideoContainer(string foldername) {
            return _videoContainerList.Find(x => x.GetFolderName() == foldername);
        }

        private void OnDisable() {
            try {
                _stereoComController?.Dispose();
            } finally {
                if (BootstrapManager.Instance) {
                    BootstrapManager.Instance.DisconnectServer();
                }
                //StereoscopicComController.ClientConnected -= EnterFullScreenSSplayer;
            }
        }
    }
}