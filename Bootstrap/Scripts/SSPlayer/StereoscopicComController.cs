using System;

using StereoPlayer;

using System.Runtime.InteropServices;
using System.Threading;


namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private dynamic _playerCom;
        private Thread _staThread;
        private AutoResetEvent _ready = new(false);
        private AutoResetEvent _signal = new(false);
        private Action _action;

        public void Connect() {
            _staThread = new Thread(StaLoop);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
            _ready.WaitOne();
        }

        private void StaLoop() {
            var clsid = new Guid("73B28B6E-D306-4589-B032-9ED17AA4D182");
            var type = Type.GetTypeFromCLSID(clsid);
            _playerCom = Activator.CreateInstance(type);

            _ready.Set();

            while (true) {
                _signal.WaitOne();
                _action?.Invoke();
            }
        }

        private void Invoke(Action action) {
            _action = action;
            _signal.Set();
        }

        public void OpenLeftRightFiles(string leftVideo, string rightVideo, string audioPath) {
            if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            Invoke(() => {
                // Adjust method name & parameters according to official automation docs
                _playerCom.OpenLeftRightFiles(leftVideo, rightVideo, audioPath, 1);
                Play();
            });
        }

        public void Play() {
            Invoke(() => {
                if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
                _playerCom.SetPlaybackState(0);
            });
        }

        public void Pause() {
            Invoke(() => {
                if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
                _playerCom.SetPlaybackState(1);
            });
        }

        public void Stop() {
            Invoke(() => {
                if (_playerCom == null) throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
                _playerCom.SetPlaybackState(2);
            });
        }

        bool isMuted = false;
        private float volume = 1;

        public void ToggleMute() {
            Invoke(() => {
                if (isMuted) {
                    _playerCom.SetVolume(1);
                } else {
                    _playerCom.SetVolume(0);
                }
                isMuted = !isMuted;
            });
        }

        public void Restart() {
            Invoke(() => {
                _playerCom.Replay();
            });
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
