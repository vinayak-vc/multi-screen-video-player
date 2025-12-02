using System;

using StereoPlayer;


namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private IAutomation _playerCom;

        public bool IsConnected => _playerCom != null;

        public bool Connect() {
            if (_playerCom != null)
                return true;

            // 1. Try to attach to a running instance
            Type playerType = Type.GetTypeFromProgID("StereoPlayer.Automation");
            IAutomation player = (IAutomation)Activator.CreateInstance(playerType);
            if (player == null)
                return false; // ProgID not registered

            try {
                // Creates or gets a running COM instance, depending on implementation
                _playerCom = player;
                return true;
            } catch (Exception) {
                _playerCom = null;
                return false;
            }
        }

        public void OpenLeftRightFiles(string leftVideo, string rightVideo, string audioPath) {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");

            // Adjust method name & parameters according to official automation docs
            _playerCom.OpenLeftRightFiles(leftVideo, rightVideo, audioPath, AudioMode.SeparateFile);
            Play();
        }

        public void Play() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(PlaybackState.Play);
        }

        public void Pause() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(PlaybackState.Pause);
        }

        public void Stop() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(PlaybackState.Stop);
        }

        bool isMuted = false;
        private float volume = 1;

        public void ToggleMute() {
            if (isMuted) {
                _playerCom.SetVolume(1);
            } else {
                _playerCom.SetVolume(0);
            }
            isMuted = !isMuted;
        }

        public void Restart() {
            _playerCom.Replay();
        }

        public void Seek(double seekTime) {
            throw new NotImplementedException();
        }

        public void SetPlaybackSpeed(float result) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            if (_playerCom != null) {
                try {
                    // Optional: tell player to quit if automation supports it
                    // _playerCom.Quit();
                } catch {
                }

                _playerCom = null;
            }
        }
    }
}
