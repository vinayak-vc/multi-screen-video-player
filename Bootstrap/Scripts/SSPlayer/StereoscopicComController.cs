using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;

namespace StereoscopicComControl {

    public class StereoscopicComController : IDisposable {

        private dynamic _playerCom;
        private Thread _staThread;
        private BlockingCollection<Action> _queue = new();
        private AutoResetEvent _ready = new(false);
        private bool _running = true;

        public void Connect() {
            _staThread = new Thread(StaLoop);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
            _ready.WaitOne();
        }

        private void StaLoop() {
            try {
                var clsid = new Guid("73B28B6E-D306-4589-B032-9ED17AA4D182");
                var type = Type.GetTypeFromCLSID(clsid);
                _playerCom = Activator.CreateInstance(type);

                _ready.Set();

                while (_running) {
                    var action = _queue.Take();
                    action();
                }
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError("STA COM thread crashed: " + e);
            }
        }

        private void Enqueue(Action action) {
            if (_playerCom == null)
                throw new InvalidOperationException("Not connected to Stereoscopic Player COM.");
            _queue.Add(action);
        }

        // ✅ Correct COM calls
        public void OpenLeftRightFiles(string left, string right, string audio = "") {
            Enqueue(() => {
                _playerCom.OpenLeftRightFiles(left, right, audio, 1);
                _playerCom.Play();
            });
        }

        public void Play() {
            Enqueue(() => _playerCom.Play());
        }

        public void Pause() {
            Enqueue(() => _playerCom.Pause());
        }

        public void Stop() {
            Enqueue(() => _playerCom.Stop());
        }

        public void Restart() {
            Enqueue(() => _playerCom.Replay());
        }

        public void SetVolume(float volume01) {
            Enqueue(() => {
                int v = (int)(Math.Clamp(volume01, 0f, 1f) * 100);
                _playerCom.SetVolume(v);
            });
        }

        public void Dispose() {
            _running = false;
            _queue.Add(() => { });

            if (_playerCom != null) {
                try {
                    Marshal.ReleaseComObject(_playerCom);
                } catch { }
                _playerCom = null;
            }
        }
    }
}
