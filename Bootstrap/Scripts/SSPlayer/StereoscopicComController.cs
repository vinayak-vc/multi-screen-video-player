using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using static Modules.Utility.Utility;


namespace StereoscopicComControl {
    public class StereoscopicComController {
        private NamedPipeServerStream server;
        private StreamReader reader;
        private StreamWriter writer;
        private bool isConnected = false;

        private TaskCompletionSource<bool> _connectedTcs;

        public async Task StartServerAsync() {
            Log("Server starting...");

            _connectedTcs = new TaskCompletionSource<bool>();

            server = new NamedPipeServerStream("CR7", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            _ = Task.Run(async () => {
                try {
                    Log("Waiting for client...");
                    await server.WaitForConnectionAsync();

                    reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
                    writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                    isConnected = true;
                    _connectedTcs.TrySetResult(true); // 🔑 SIGNAL READY

                    Log("Client connected.");
                } catch (Exception ex) {
                    _connectedTcs.TrySetException(ex);
                    Log($"Server error: {ex.Message}");
                }
            });
        }

        public async Task SendMessage(string message) {
            // ✅ Wait until pipe is ACTUALLY ready
            if (_connectedTcs != null)
                await _connectedTcs.Task;

            if (!isConnected || writer == null) {
                LogError("Not connected");
                return;
            }

            try {
                await writer.WriteLineAsync(message);
                Log($"Sent: {message}");
            } catch (Exception ex) {
                LogError($"Send failed: {ex.Message}");
                isConnected = false;
            }
        }

        // private async Task ReadLoopAsync() {
        //     while (isConnected) {
        //         try {
        //             string? message = await reader.ReadLineAsync();
        //             if (message != null) {
        //                 Log($"Received: {message}");
        //                 OnMessageReceived?.Invoke(message); // Unity-safe event
        //             }
        //         } catch (Exception ex) {
        //             Log($"Read error: {ex.Message}");
        //             break;
        //         }
        //     }
        //
        //     isConnected = false;
        //     OnDisconnected?.Invoke();
        // }

        public async Task StopServerAsync() {
            isConnected = false;
            reader?.Dispose();
            writer?.Dispose();
            server?.Dispose();
        }
    }
}
