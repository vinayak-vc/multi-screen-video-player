using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using Modules.Utility;

using static Modules.Utility.Utility;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class BasicWebSocketClient : MonoBehaviour {
        [SerializeField] private bool isThisWindows;
        public static Action<string> MessageReceived;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;

        private async void Start() {
            try {
                _cts = new CancellationTokenSource();
                _socket = new ClientWebSocket();

                if (isThisWindows) {
                    CrossPlatformProcessLauncher.Start(
                        Path.Combine(Application.dataPath, "unity-websocket-server-win.exe"),
                        Application.dataPath,
                        "",
                        true
                    );

                    // ✅ Non-blocking 1 second delay
                    await Task.Delay(1000);

                    await Connect(IPManager.GetIP(ADDRESSFAM.IPv4));
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }


        public async Task Connect(string ip) {
            Uri serverUri = new Uri($"ws://{ip}:8484"); // change to your server
            await Connect(serverUri);
        }

        private async Task Connect(Uri uri) {
            try {
                await _socket.ConnectAsync(uri, _cts.Token);
                Log("WebSocket connected");

                _ = ReceiveLoop(); // fire and forget receive loop
            } catch (Exception e) {
                LogError($"WebSocket connect error: {e.Message}");
            }
        }

        public async Task Send(string message) {
            if (_socket.State != WebSocketState.Open)
                return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            ArraySegment<byte> buffer = new ArraySegment<byte>(data);

            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop() {
            byte[] buffer = new byte[4096];

            while (_socket.State == WebSocketState.Open) {
                List<byte> messageBytes = new List<byte>();

                WebSocketReceiveResult result;

                do {
                    result = await _socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cts.Token
                    );

                    messageBytes.AddRange(buffer.AsSpan(0, result.Count).ToArray());

                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close) {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed",
                        CancellationToken.None
                    );
                    break;
                }

                string fullMessage = Encoding.UTF8.GetString(messageBytes.ToArray());
                MessageReceived?.Invoke(fullMessage);
            }
        }


        private async void OnDestroy() {
            try {
                _cts.Cancel();

                if (_socket != null && _socket.State == WebSocketState.Open) {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application quit", CancellationToken.None);
                }

                _socket?.Dispose();
            } catch {
                // ignored
            }
        }
    }
}