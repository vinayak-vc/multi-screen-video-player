using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

            Log("Waiting for client...");
            await server.WaitForConnectionAsync();
            Log("Client connected.");

            reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
            writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

            isConnected = true;
        }

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
