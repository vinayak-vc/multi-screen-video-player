using System;
using System.IO;

using TMPro;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.Video;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class WindowsPlayer : MonoBehaviour {
        public static WindowsPlayer Instance { get; private set; }

        public TextMeshProUGUI ipAddress;
        public VideoPlayer videoPlayer;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }

            ipAddress.text = IPManager.GetIP(ADDRESSFAM.IPv4);
            string videoPath = Path.Combine(Application.streamingAssetsPath, "DefaultVideo.mp4");
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoPath;
            videoPlayer.Prepare();
        }

        private void Start() {
            // Check if multiple displays exist
            for (int i = 1; i < Display.displays.Length; i++)
            {
                // Activate additional displays
                Display.displays[i].Activate();
            }
        }

        private void FixedUpdate() {
            if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.isPlaying) {
                UpdateProgress(videoPlayer.time, videoPlayer.length);
            }
            
            ipAddress.gameObject.SetActive(Input.GetKey(KeyCode.F5));
        }

        public void ExecuteCommand(string command) {
            string[] parts = command.Split(Commands.Separator);
            Log(parts[0]);
            switch (parts[0]) {
                case Commands.Play:
                    Play();
                    break;
                case Commands.Pause:
                    Pause();
                    break;
                case Commands.Stop:
                    Stop();
                    break;
                case Commands.ToggleMute:
                    ToggleMute();
                    break;
                case Commands.Restart:
                    Restart();
                    break;

                case Commands.Seek: {
                    if (parts.Length > 1 && double.TryParse(parts[1], out double seekTime)) {
                        Seek(seekTime);
                    }
                    break;
                }

                case Commands.SetPlaybackSpeed: {
                    if (float.TryParse(parts[1], out float speed)) {
                        SetPlaybackSpeed(speed);
                    }
                    break;
                }

                case Commands.NameVideo:
                    NameVideo();
                    break;
            }
        }

        private void NameVideo() {
            UpdateProgress(videoPlayer.time, videoPlayer.length);
            BootstrapManager.Instance.networkObject.SendCommandToClient($"{Commands.NameVideo}{Commands.Separator}{Path.GetFileNameWithoutExtension(videoPlayer.url)}");
        }

        // Play the video
        private void Play() {
            if (videoPlayer != null && videoPlayer.isPrepared && !videoPlayer.isPlaying) {
                videoPlayer.Play();
            }
        }

        // Pause the video
        private void Pause() {
            if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.isPlaying) {
                videoPlayer.Pause();
            }
        }

        // Stop the video
        private void Stop() {
            if (videoPlayer != null && videoPlayer.isPrepared) {
                videoPlayer.Stop();
            }
        }

        // Seek to a specific time in seconds
        private void Seek(double timeInSeconds) {
            if (videoPlayer != null && videoPlayer.canSetTime) {
                videoPlayer.time = timeInSeconds;
                if (!videoPlayer.isPlaying && videoPlayer.isPrepared) {
                    videoPlayer.Play();
                }
            }
        }

        // Toggle mute state
        private void ToggleMute() {
            if (videoPlayer != null) {
                videoPlayer.SetDirectAudioMute(0, !videoPlayer.GetDirectAudioMute(0));
            }
        }

        // Restart the video from the beginning
        private void Restart() {
            if (videoPlayer != null) {
                videoPlayer.Stop();
                videoPlayer.time = 0;
                videoPlayer.Play();
            }
        }

        // Set playback speed (1.0 is normal speed)
        private void SetPlaybackSpeed(float speed) {
            if (videoPlayer != null && speed > 0) {
                videoPlayer.playbackSpeed = speed;
            }
        }
        private void UpdateProgress(double currentTime, double length) {
            BootstrapManager.Instance.networkObject.SendCommandToClient($"{Commands.SliderData}{Commands.Separator}{currentTime}{Commands.Separator}{length}");
        }
    }
}