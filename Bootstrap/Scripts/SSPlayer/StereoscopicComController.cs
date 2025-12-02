using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

using UnityEngine;

using StereoPlayer;


namespace StereoscopicComControl {
    public class StereoscopicComController : IDisposable {
        private IAutomation _player;
        private Thread _staThread;
        private readonly BlockingCollection<Action> _queue = new();
        private readonly AutoResetEvent _ready = new(false);
        private volatile bool _running = true;

        // ---------------- CONNECT ----------------

        public void Connect() {
            _staThread = new Thread(StaThread);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
            _ready.WaitOne();
        }

        private void StaThread() {
            try {
                Type comType = Type.GetTypeFromProgID("StereoPlayer.Automation");
                _player = (IAutomation)Activator.CreateInstance(comType);

                _ready.Set();

                while (_running) {
                    var action = _queue.Take();
                    action?.Invoke();
                }
            } catch (Exception e) {
                Debug.LogError("Stereoscopic STA thread crashed: " + e);
            }
        }

        private void Enqueue(Action action) {
            if (_player == null)
                throw new InvalidOperationException("Stereoscopic Player not connected.");
            _queue.Add(action);
        }

        // ---------------- PUBLIC API ----------------

        public void OpenLeftRightFiles(string left, string right, string audio = "") {
            Enqueue(() => {
                _player.OpenLeftRightFiles(left, right, audio, AudioMode.SeparateFile);
                _player.SetPlaybackState(PlaybackState.Play);
            });
        }

        public void Play() {
            Enqueue(() => _player.SetPlaybackState(PlaybackState.Play));
        }

        public void Pause() {
            Enqueue(() => _player.SetPlaybackState(PlaybackState.Pause));
        }

        public void Stop() {
            Enqueue(() => _player.SetPlaybackState(PlaybackState.Stop));
        }

        public void Restart() {
            Enqueue(_player.Replay);
        }

        public void SetVolume(float volume01) {
            Enqueue(() => {
                double v = Math.Clamp(volume01, 0f, 1f) * 100.0;
                _player.SetVolume(v);
            });
        }

        public void Seek(double seconds) {
            Enqueue(() => _player.SetPosition(seconds));
        }

        // ---------------- CLEANUP ----------------

        public void Dispose() {
            _running = false;
            _queue.Add(() => {
            });

            if (_player != null) {
                try {
                    _player.ClosePlayer();
                    Marshal.ReleaseComObject(_player);
                } catch {
                }
                _player = null;
            }
        }
    }
}
