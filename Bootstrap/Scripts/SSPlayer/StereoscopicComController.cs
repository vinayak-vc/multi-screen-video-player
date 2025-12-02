using System;

using StereoPlayer;
using System.Runtime.InteropServices;

namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private dynamic _playerCom;

        public bool IsConnected => _playerCom != null;

        public bool Connect() {
            try {
                Guid clsid = new Guid("73B28B6E-D306-4589-B032-9ED17AA4D182"); // example CLSID, replace with actual if different
                Type comType = Type.GetTypeFromCLSID(clsid);
                _playerCom = Activator.CreateInstance(comType);
                return true;
            } catch (Exception e) {
                UnityEngine.Debug.LogError("COM Activation failed: " + e.Message);
                return false;
            }
        }

        public void OpenLeftRightFiles(string leftVideo, string rightVideo, string audioPath) {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");

            // Adjust method name & parameters according to official automation docs
            _playerCom.OpenLeftRightFiles(leftVideo, rightVideo, audioPath, 1);
            Play();
        }

        public void Play() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(0);
        }

        public void Pause() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(1);
        }

        public void Stop() {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _playerCom.SetPlaybackState(2);
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
