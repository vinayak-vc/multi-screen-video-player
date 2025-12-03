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

        public async Task StartServerAsync() {
            Log("Server starting...");
            server = new NamedPipeServerStream("CR7", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            // Run connection + reading in background task (NON-BLOCKING)
            _ = Task.Run(async () => {
                try {
                    Log("Waiting for client...");
                    await server.WaitForConnectionAsync();
                    Log("Client connected.");

                    reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
                    writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                    isConnected = true;
                    //OnConnected?.Invoke();

                    // Start message loop
//                    await ReadLoopAsync();
                } catch (Exception ex) {
                    Log($"Server error: {ex.Message}");
                }
            });
        }
        
        // private async Task ReadLoopAsync()
        // {
        //     while (isConnected)
        //     {
        //         try
        //         {
        //             string? message = await reader.ReadLineAsync();
        //             if (message != null)
        //             {
        //                 Log($"Received: {message}");
        //                 OnMessageReceived?.Invoke(message);  // Unity-safe event
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             Log($"Read error: {ex.Message}");
        //             break;
        //         }
        //     }
        //
        //     isConnected = false;
        //     OnDisconnected?.Invoke();
        // }


        public async Task SendMessage(string message) // Task, not void
        {
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

        public async Task StopServerAsync() {
            isConnected = false;
            reader?.Dispose();
            writer?.Dispose();
            server?.Dispose();
        }
    }
}
