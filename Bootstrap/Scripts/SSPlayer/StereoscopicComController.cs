using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using UnityEngine;


namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private object _playerCom;
        private Thread _staThread;
        private readonly BlockingCollection<Action> _queue = new();
        private readonly AutoResetEvent _ready = new(false);
        private volatile bool _running = true;

        private const string CLSID_STR = "73B28B6E-D306-4589-B032-9ED17AA4D182";

        // ---------------- CONNECT ----------------

        public void Connect() {
            _staThread = new Thread(StaLoop);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
            _ready.WaitOne();
        }

        private void StaLoop() {
            try {
                var clsid = new Guid(CLSID_STR);
                var type = Type.GetTypeFromCLSID(clsid);
                _playerCom = Activator.CreateInstance(type);

                _ready.Set();

                while (_running) {
                    var action = _queue.Take();
                    action?.Invoke();
                }
            } catch (Exception e) {
                Debug.LogError("STA COM thread crashed: " + e);
            }
        }

        // ---------------- QUEUE ----------------

        private void Enqueue(Action action) {
            if (_playerCom == null)
                throw new InvalidOperationException("Stereoscopic Player COM not connected.");
            _queue.Add(action);
        }

        // ---------------- CORE COM INVOKER ----------------

        public static void InvokeCom(object com, string method, params object[] args) {
            com.GetType()
                .InvokeMember(method, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, com, args);
        }

        // ---------------- PUBLIC API ----------------

        public void OpenLeftRightFiles(string left, string right, string audio = "") {
            Enqueue(() => {
                // audioMode = 1 or 2 depending on your testing
                InvokeCom(_playerCom, "OpenLeftRightFiles", left, right, audio, 0);
                InvokeCom(_playerCom, "Play");
            });
        }

        public void Play() {
            Enqueue(() => InvokeCom(_playerCom, "Play"));
        }

        public void Pause() {
            Enqueue(() => InvokeCom(_playerCom, "Pause"));
        }

        public void Stop() {
            Enqueue(() => InvokeCom(_playerCom, "Stop"));
        }

        public void Restart() {
            Enqueue(() => InvokeCom(_playerCom, "Replay"));
        }

        public void SetVolume(float volume01) {
            Enqueue(() => {
                int v = (int)(Math.Clamp(volume01, 0f, 1f) * 100);
                InvokeCom(_playerCom, "SetVolume", v);
            });
        }

        // ---------------- CLEANUP ----------------

        public void Dispose() {
            _running = false;
            _queue.Add(() => {
            });

            if (_playerCom != null) {
                try {
                    Marshal.ReleaseComObject(_playerCom);
                } catch {
                }
                _playerCom = null;
            }
        }

        // ---------------- OPTIONAL / NOT IMPLEMENTED ----------------

        public void ToggleMute() {
            throw new NotImplementedException();
        }

        public void Seek(double seekTime) {
            throw new NotImplementedException();
        }

        public void SetPlaybackSpeed(float result) {
            throw new NotImplementedException();
        }
    }
}
