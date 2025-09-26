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
        [SerializeField] private Canvas canvasPrefab;

        private readonly List<Canvas> _myCanvas = new List<Canvas>();
        private VideoContainer _container;
        private readonly List<VideoPlayer> _videoPlayerList = new List<VideoPlayer>();

        private AudioClip _audioClip;
        private AudioSource _audioSource;

        public void Init(VideoContainer container) {
            gameObject.name = container.folderName;
            _container = container;
            for (int i = 0; i < container.videoPath.Length; i++) {
                RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                GameObject videoPlayerPrefab = new GameObject();
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

                Canvas canvas = Instantiate(canvasPrefab, transform);
                canvas.targetDisplay = i;

                RawImage rawImage = canvas.transform.GetChild(0).GetComponent<RawImage>();
                rawImage.texture = renderTexture;

                videoPlayerPrefab.name = Path.GetFileNameWithoutExtension(container.videoPath[i]) + " VideoPlayer";
                canvas.name = $"Display {i} Canvas";
                rawImage.name = $"{i} RawImage";

                _myCanvas.Add(canvas);
                _videoPlayerList.Add(videoContainer);
            }

            if (container.audioPath != string.Empty) {
                StartCoroutine(WindowsPlayer.Instance.PlayAudioFromFile(container.audioPath, (audioClip) => {
                    _audioClip = audioClip;
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.clip = _audioClip;
                    _audioSource.loop = true;
                }));
            } else {
                if (_videoPlayerList is { Count: > 1 }) {
                    _videoPlayerList[0].audioOutputMode = VideoAudioOutputMode.Direct;
                    _videoPlayerList[0].loopPointReached += OnLoopPointReached;
                    _videoPlayerList[0].prepareCompleted += OnPrepareCompleted;
                }
            }
        }


        private void OnDisable() {
            if (_videoPlayerList is { Count: > 1 }) {
                _videoPlayerList[0].loopPointReached -= OnLoopPointReached;
                _videoPlayerList[0].prepareCompleted -= OnPrepareCompleted;
            }
        }
        private void OnLoopPointReached(VideoPlayer source) {
            WindowsPlayer.Instance.PlayNextVideo();
        }

        private void OnPrepareCompleted(VideoPlayer source) {
            Log("VideoPlayer Prepare Completed : " + source.name, source);
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
                        if (videoPlayer.isPrepared && !videoPlayer.isPlaying) {
                            videoPlayer.Play();
                            if (_audioClip) {
                                _audioSource.Play();
                            }
                        }
                    }
                } else {
                    LogError("VideoPlayerList is null", gameObject);
                }

                for (int i = 0; i < _myCanvas.Count; i++) {
                    _myCanvas[i].gameObject.SetActive(true);
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
            for (int i = 0; i < _myCanvas.Count; i++) {
                _myCanvas[i].gameObject.SetActive(false);
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

            if (!_videoPlayerList[0].isPlaying) {
                LogError("VideoPlayer is not playing", gameObject);
                return -1;
            }

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
                LogError("VideoPlayer is not prepared", gameObject);
                return -1;
            }

            if (!_videoPlayerList[0].isPlaying) {
                LogError("VideoPlayer is not playing", gameObject);
                return -1;
            }

            return (int)_videoPlayerList[0].length;
        }

        public string GetFolderName() {
            return _container.folderName;
        }
        
        public string GetFolderPath() {
            return _container.folderPath;
        }
    }
}