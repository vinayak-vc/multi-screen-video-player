using System;

using StereoPlayer;


namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private IAutomation _playerCom;

        public bool IsConnected => _playerCom != null;

        public bool Connect() {
            try {
                Guid clsid = new Guid("54150FC5-F6D5-419A-BC0D-E2BE08558934"); // example CLSID, replace with actual if different
                Type comType = Type.GetTypeFromCLSID(clsid);
                _playerCom = (IAutomation)Activator.CreateInstance(comType);
                return true;
            } catch (Exception e) {
                UnityEngine.Debug.LogError("COM Activation failed: " + e.Message);
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
