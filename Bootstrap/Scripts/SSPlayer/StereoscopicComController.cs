using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using static Modules.Utility.Utility;

using Modules.Utility;


namespace StereoscopicComControl {
    public class StereoscopicComController {
        private bool running = true;
        private ConcurrentQueue<string> outgoing = new();
        private int clientProcessId = -1;
        private Process clientProcess;

        private void LaunchComClient() {
            return;
            string clientAppPath = Path.Combine(Application.streamingAssetsPath, "COMBridgeAppV1.exe");

            if (!IsProcessRunning("COMBridgeAppV1", out clientProcess)) {
                if (File.Exists(clientAppPath)) {
                    clientProcessId = CrossPlatformProcessLauncher.Start(clientAppPath, Application.streamingAssetsPath, "", true);
                    clientProcess = Process.GetProcessById(clientProcessId);
                }
            }
        }

        public void Run() {
            LaunchComClient();
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            while (running) {
                using NamedPipeServerStream server = new("CR7", PipeDirection.InOut, 1, PipeTransmissionMode.Message);

                Log("Waiting for client...");
                server.WaitForConnection();
                Log("Client connected");

                using BinaryReader br = new(server, Encoding.UTF8);
                using BinaryWriter bw = new(server, Encoding.UTF8);

                using CancellationTokenSource cts = new CancellationTokenSource();

                Task readTask = Task.Run(() => ReadLoop(br, cts), cts.Token);
                Task writeTask = Task.Run(() => WriteLoop(bw, cts), cts.Token);

                try {
                    Task.WaitAny(readTask, writeTask);
                } finally {
                    cts.Cancel();
                    Log("Client disconnected");
                    LaunchComClient();
                }
            }
        }

        private void ReadLoop(BinaryReader br, CancellationTokenSource cts) {
            try {
                while (!cts.IsCancellationRequested) {
                    uint len = br.ReadUInt32(); // blocks
                    if (len == 0 || len > 1024 * 1024)
                        throw new IOException("Invalid message length");

                    byte[] data = br.ReadBytes((int)len);
                    string msg = Encoding.UTF8.GetString(data);

                    Log("Client → " + msg);
                }
            } catch (EndOfStreamException) {
            } catch (IOException) {
            } finally {
                cts.Cancel(); // kill writer
            }
        }

        private void WriteLoop(BinaryWriter bw, CancellationTokenSource cts) {
            try {
                while (!cts.IsCancellationRequested) {
                    if (outgoing.TryDequeue(out var msg)) {
                        var data = Encoding.UTF8.GetBytes(msg);
                        bw.Write((uint)data.Length);
                        bw.Write(data);
                        bw.Flush();
                    } else {
                        Thread.Sleep(2); // gentle backoff
                    }
                }
            } catch (IOException) {
            } finally {
                cts.Cancel(); // kill reader
            }
        }

        public void SendMessage(string msg) {
            outgoing.Enqueue(msg);
        }

        public void Dispose() {
            running = false;
            if (clientProcess is {HasExited: false }) {
                clientProcess.Kill();
            }
        }
    }
}
