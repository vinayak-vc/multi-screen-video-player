using UnityEngine;
using System.Collections.Generic;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class StandalonePlayerController : MonoBehaviour {
        [SerializeField]
        private WindowsPlayer windowsPlayer;

        [SerializeField]
        private WindowsUIController windowsUIController;

        [SerializeField]
        private StandaloneUIController standaloneUI;

        private enum State { Idle, Playing, Picking }
        private State _state = State.Idle;
        private string _currentVideoFolder;
        private List<VideoContainer> _availableVideos;

        private void Start() {
            standaloneUI.ShowIdle();
            _state = State.Idle;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                if (_state == State.Idle || _state == State.Picking) {
                    PlayDefaultVideo();
                } else if (_state == State.Playing) {
                    windowsPlayer.ExecuteCommand(Commands.Pause);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (_state == State.Playing || _state == State.Idle) {
                    windowsPlayer.ExecuteCommand(Commands.Stop);
                    _availableVideos = windowsUIController.GetVideoContainerList();
                    standaloneUI.ShowPicker(_availableVideos, OnVideoSelected);
                    _state = State.Picking;
                } else if (_state == State.Picking) {
                    standaloneUI.ShowIdle();
                    _state = State.Idle;
                }
            }
        }

        private void PlayDefaultVideo() {
            _availableVideos = windowsUIController.GetVideoContainerList();
            if (_availableVideos == null || _availableVideos.Count == 0) {
                Debug.LogWarning("No videos available");
                return;
            }

            _currentVideoFolder = _availableVideos[0].folderName;
            windowsPlayer.PlayThisVideo(_currentVideoFolder, false);
            standaloneUI.ShowPlaying(_currentVideoFolder);
            _state = State.Playing;
        }

        private void OnVideoSelected(string folderName) {
            _currentVideoFolder = folderName;
            windowsPlayer.PlayThisVideo(folderName, false);
            standaloneUI.ShowPlaying(folderName);
            _state = State.Playing;
        }
    }
}
