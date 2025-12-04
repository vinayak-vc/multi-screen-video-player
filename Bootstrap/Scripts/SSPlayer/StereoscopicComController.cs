using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

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
            string clientAppPath = Path.Combine(Application.streamingAssetsPath, "COMBridgeAppV1.exe");

            if (!IsProcessRunning("COMBridgeAppV1")) {
                if (File.Exists(clientAppPath)) {
                    clientProcessId = CrossPlatformProcessLauncher.Start(clientAppPath, Application.streamingAssetsPath, "", true);
                    clientProcess = Process.GetProcessById(clientProcessId);
                }
            }
        }

        public void Run() {
            LaunchComClient();
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            while (running) {
                NamedPipeServerStream server = new("CR7", PipeDirection.InOut, 1, PipeTransmissionMode.Message);

                Log("Waiting for client...");
                server.WaitForConnection();
                Log("Client connected");

                using BinaryReader br = new(server, Encoding.UTF8);
                using BinaryWriter bw = new(server, Encoding.UTF8);

                try {
                    while (running && server.IsConnected) {
                        // ✅ blocking read – exits on disconnect
                        uint len = br.ReadUInt32();
                        if (len is 0 or > 1024 * 1024) {
                            break;
                        }

                        byte[] data = br.ReadBytes((int)len);
                        string msg = Encoding.UTF8.GetString(data);

                        Log("Client → " + msg);

                        // write outgoing
                        while (outgoing.TryDequeue(out string outMsg)) {
                            byte[] outData = Encoding.UTF8.GetBytes(outMsg);
                            bw.Write((uint)outData.Length);
                            bw.Write(outData);
                            bw.Flush();
                        }
                    }
                } catch (EndOfStreamException) {
                    Log("Client stream ended");
                } catch (IOException) {
                    Log("Client disconnected");
                }

                // ✅ loop repeats → pipe recreated → waits again
                Log("Resetting pipe...");
                LaunchComClient();
            }
        }

        public void SendMessage(string msg) {
            outgoing.Enqueue(msg);
        }

        public void Dispose() {
        }
    }
}
