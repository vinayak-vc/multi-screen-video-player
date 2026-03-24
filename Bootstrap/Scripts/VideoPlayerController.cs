using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class VideoPlayerController : MonoBehaviour {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        [SerializeField]
        private Canvas canvasPrefab;


        private readonly List<Canvas> _myCanvas = new();
        private VideoContainer _container;
        private readonly List<VideoPlayer> _videoPlayerList = new();

        private AudioClip _audioClip;
        private AudioSource _audioSource;

        public Action VideoPlayerOnLoopPointReached;

        public MeshRenderer myVideoPlayer360Renderer;
        public RenderTexture renderTexture360;

        public void Init(VideoContainer container) {
            gameObject.name = container.folderName;
            _container = container;
            for (int i = 0; i < container.videoPath.Length; i++) {
                RenderTexture renderTexture;
                if (!WindowsPlayer.Instance.IsThis360VideoPlayer()) {
                    renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                } else {
                    renderTexture = new RenderTexture(renderTexture360);
                }
                GameObject videoPlayerPrefab = new();
                videoPlayerPrefab.transform.SetParent(transform);
                videoPlayerPrefab.transform.localPosition = Vector3.zero;
                VideoPlayer videoContainer = videoPlayerPrefab.AddComponent<VideoPlayer>();
                videoContainer.renderMode = VideoRenderMode.RenderTexture;
                videoContainer.targetTexture = renderTexture;
                videoContainer.isLooping = false;
                videoContainer.skipOnDrop = true;
                videoContainer.waitForFirstFrame = true;
                videoContainer.playOnAwake = false;
                videoContainer.source = VideoSource.Url;
                videoContainer.url = container.videoPath[i];
                videoContainer.audioOutputMode = VideoAudioOutputMode.None;

                videoContainer.Prepare();

                if (!WindowsPlayer.Instance.IsThis360VideoPlayer()) {
                    Canvas canvas = Instantiate(canvasPrefab, transform);
                    int increment = i + (WindowsPlayer.Instance.IsThisSSPlayer() ? 0 : 1);
                    canvas.targetDisplay = increment;

                    RawImage rawImage = canvas.transform.GetChild(0).GetComponent<RawImage>();
                    rawImage.texture = renderTexture;

                    videoPlayerPrefab.name = Path.GetFileNameWithoutExtension(container.videoPath[i]) + " VideoPlayer";
                    canvas.name = $"Display {increment} Canvas";
                    rawImage.name = $"{increment} RawImage";

                    _myCanvas.Add(canvas);
                    if (WindowsPlayer.Instance.IsThisSSPlayer()) {
                        canvas.enabled = false;
                    }
                } else {
                    myVideoPlayer360Renderer = Instantiate(WindowsPlayer.Instance.videoPlayer360Renderer);
                    myVideoPlayer360Renderer.material = new Material(myVideoPlayer360Renderer.material);
                    myVideoPlayer360Renderer.material.SetTexture(BaseMap, renderTexture);
                    myVideoPlayer360Renderer.gameObject.SetActive(false);
                }

                _videoPlayerList.Add(videoContainer);

            }
            if (_videoPlayerList is not { Count: > 1 }) return;
            _videoPlayerList[0].audioOutputMode = WindowsPlayer.Instance.IsThisSSPlayer() ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
            _videoPlayerList[0].prepareCompleted += OnPrepareCompleted;
        }

        private void OnDisable() {
            if (_videoPlayerList is not { Count: > 1 }) return;
            //_videoPlayerList[0].loopPointReached -= OnLoopPointReached;
            _videoPlayerList[0].prepareCompleted -= OnPrepareCompleted;
        }

        public void OnLoopPointReached(VideoPlayer source) {
            if (WindowsPlayer.Instance.Loop) {
                WindowsPlayer.Instance.PlayNextVideo();
            } else {
                foreach (VideoPlayer videoPlayerList in GetVideoPlayerList()) {
                    videoPlayerList.targetTexture.DiscardContents();
                }
            }
            Log("loop Point reached");
            VideoPlayerOnLoopPointReached?.Invoke();
        }

        private void OnPrepareCompleted(VideoPlayer source) {
            source.frame = 0;
        }

        public void Play() {
            StartCoroutine(PlayEnumerator());
            return;

            IEnumerator PlayEnumerator() {
                if (_videoPlayerList != null) {
                    for (int index = 0; index < _videoPlayerList.Count; index++) {
                        VideoPlayer videoPlayer = _videoPlayerList[index];
                        if (!videoPlayer.isPrepared) {
                            videoPlayer.Prepare();
                        }
                        yield return new WaitUntil(() => videoPlayer.isPrepared);
                        if (!videoPlayer.isPrepared || videoPlayer.isPlaying) continue;
                        videoPlayer.Play();
                        if (_audioClip) {
                            _audioSource.Play();
                        }
                    }
                } else {
                    LogError("VideoPlayerList is null", gameObject);
                }

                if (!WindowsPlayer.Instance.IsThis360VideoPlayer()) {
                    for (int i = 0; i < _myCanvas.Count; i++) {
                        _myCanvas[i].gameObject.SetActive(true);
                    }
                } else {
                    myVideoPlayer360Renderer.gameObject.SetActive(true);
                }
            }
        }

        // Stop the video
        public void Stop() {
            if (_videoPlayerList != null) {
                for (int index = 0; index < _videoPlayerList.Count; index++) {
                    VideoPlayer videoPlayer = _videoPlayerList[index];
                    if (videoPlayer.isPrepared) {
                        videoPlayer.Stop();
                    }
                }
            } else {
                LogError("VideoPlayerList is null", gameObject);
            }
            if (!WindowsPlayer.Instance.IsThis360VideoPlayer()) {
                for (int i = 0; i < _myCanvas.Count; i++) {
                    _myCanvas[i].gameObject.SetActive(false);
                }
            } else {
                myVideoPlayer360Renderer.gameObject.SetActive(false);
            }
        }

        // Pause the video
        public void Pause() {
            if (_videoPlayerList != null) {
                foreach (VideoPlayer videoPlayer in _videoPlayerList) {
                    if (videoPlayer.isPrepared && videoPlayer.isPlaying) {
                        videoPlayer.Pause();
                    }
                }
            } else {
                LogError("VideoPlayerList is null", gameObject);
            }
        }

        // Seek to a specific time in seconds
        public void Seek(double timeInSeconds) {
            if (_videoPlayerList != null) {
                foreach (VideoPlayer videoPlayer in _videoPlayerList) {
                    if (videoPlayer.canSetTime) {
                        videoPlayer.time = timeInSeconds;
                        if (!videoPlayer.isPlaying && videoPlayer.isPrepared) {
                            videoPlayer.Play();
                        }
                    } else {
                        LogError("Cannot seek video player", gameObject);
                    }
                }
            } else {
                LogError("VideoPlayerList is null", gameObject);
            }
        }

        // Toggle mute state
        public void ToggleMute() {
            if (_videoPlayerList != null) {
                foreach (VideoPlayer videoPlayer in _videoPlayerList) {
                    videoPlayer.SetDirectAudioMute(0, !videoPlayer.GetDirectAudioMute(0));
                }
            }
        }

        // Restart the video from the beginning
        public void Restart() {
            if (_videoPlayerList != null) {
                foreach (VideoPlayer videoPlayer in _videoPlayerList) {
                    videoPlayer.Stop();
                    videoPlayer.time = 0;
                    videoPlayer.Play();
                }
            }
        }

        // Set playback speed (1.0 is normal speed)
        public void SetPlaybackSpeed(float speed) {
            if (_videoPlayerList != null && speed > 0) {
                foreach (VideoPlayer videoPlayer in _videoPlayerList) {
                    videoPlayer.playbackSpeed = speed;
                }
            }
        }

        public bool GetIsPrepared() {
            if (_videoPlayerList == null) {
                LogError("VideoPlayerList is null", gameObject);
                return false;
            }

            if (_videoPlayerList.Count == 0) {
                LogError("VideoPlayerList is empty", gameObject);
                return false;
            }

            return _videoPlayerList[0].isPrepared;
        }

        public bool GetIsPlaying() {
            if (_videoPlayerList == null) {
                LogError("VideoPlayerList is null", gameObject);
                return false;
            }

            if (_videoPlayerList.Count == 0) {
                LogError("VideoPlayerList is empty", gameObject);
                return false;
            }

            return _videoPlayerList[0].isPlaying;
        }

        public int GetTime() {
            if (_videoPlayerList == null) {
                LogError("VideoPlayerList is null", gameObject);
                return -1;
            }

            if (_videoPlayerList.Count == 0) {
                LogError("VideoPlayerList is empty", gameObject);
                return -1;
            }

            if (!_videoPlayerList[0].isPrepared) {
                LogError("VideoPlayer is not prepared", gameObject);
                return -1;
            }

            // if (!_videoPlayerList[0].isPlaying) {
            //     LogError("VideoPlayer is not playing", gameObject);
            //     return -1;
            // }

            return (int)_videoPlayerList[0].time;
        }

        public int GetLength() {
            if (_videoPlayerList == null) {
                LogError("VideoPlayerList is null", gameObject);
                return -1;
            }

            if (_videoPlayerList.Count == 0) {
                LogError("VideoPlayerList is empty", gameObject);
                return -1;
            }

            if (!_videoPlayerList[0].isPrepared) {
//                LogError("VideoPlayer is not prepared", gameObject);
                return -1;
            }

            // if (!_videoPlayerList[0].isPlaying) {
            //     LogError("VideoPlayer is not playing", gameObject);
            //     return -1;
            // }

            return (int)_videoPlayerList[0].length;
        }

        public string GetFolderName() {
            return _container.folderName;
        }

        public string GetFolderPath() {
            return _container.folderPath;
        }

        public VideoContainer GetContainer() {
            return _container;
        }

        public List<VideoPlayer> GetVideoPlayerList() {
            return _videoPlayerList;
        }
    }
}