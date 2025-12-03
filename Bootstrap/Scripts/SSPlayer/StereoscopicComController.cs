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
        private volatile bool _running;
        private volatile bool _isReady;

        public bool IsReady => _isReady;

        // ---------------- START ----------------

        public void Connect() {
            if (_running)
                return;

            _running = true;

            _staThread = new Thread(StaThread);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
        }

        private void StaThread() {
            try {
                Type comType = Type.GetTypeFromProgID("StereoPlayer.Automation");
                _player = (IAutomation)Activator.CreateInstance(comType);

                _isReady = true;

                while (_running) {
                    if (!_queue.TryTake(out var action, 50))
                        continue;

                    action?.Invoke();
                }
            } catch (Exception e) {
                Debug.LogError("STA COM thread crashed: " + e);
            }
        }

        private bool TryEnqueue(Action action) {
            if (!_isReady || _player == null)
                return false;

            _queue.Add(action);
            return true;
        }

        // ---------------- PUBLIC API ----------------

        public void OpenLeftRightFiles(string left, string right, string audio = "") {
            TryEnqueue(() => {
                _player.OpenLeftRightFiles(left, right, audio, AudioMode.SeparateFile);
                _player.SetPlaybackState(PlaybackState.Play);
            });
        }

        public void Play() {
            TryEnqueue(() => _player.SetPlaybackState(PlaybackState.Play));
        }

        public void Pause() {
            TryEnqueue(() => _player.SetPlaybackState(PlaybackState.Pause));
        }

        public void Stop() {
            TryEnqueue(() => _player.SetPlaybackState(PlaybackState.Stop));
        }

        public void Seek(double seconds) {
            TryEnqueue(() => _player.SetPosition(seconds));
        }

        public void SetVolume(float volume01) {
            TryEnqueue(() => _player.SetVolume(Math.Clamp(volume01, 0f, 1f) * 100.0));
        }

        // ---------------- CLEANUP ----------------

        public void Dispose() {
            _running = false;

            if (_player != null) {
                try {
                    _player.ClosePlayer();
                    Marshal.ReleaseComObject(_player);
                } catch {
                }

                _player = null;
            }
        }

        public void ToggleMute() {
            throw new NotImplementedException();
        }

        public void Restart() {
            throw new NotImplementedException();
        }

        public void SetPlaybackSpeed(float result) {
            throw new NotImplementedException();
        }
    }
}
