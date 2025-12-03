using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

using static Modules.Utility.Utility;


namespace StereoscopicComControl {
    public class StereoscopicComController {
        private NamedPipeServerStream server;
        private bool running = true;
        private BinaryWriter bw;
        private ConcurrentQueue<string> outgoing = new();

        public void Run() {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            server = new NamedPipeServerStream("CR7", PipeDirection.InOut, 1, PipeTransmissionMode.Message);

            Log("Waiting for connection...");
            server.WaitForConnection();
            Log("Client connected");

            BinaryReader br = new(server, Encoding.UTF8);
            bw = new BinaryWriter(server, Encoding.UTF8);

            try {
                while (running) {
                    // ---- READ ----
                    if (server.IsConnected && server.CanRead && server.InBufferSize > 0) {
                        uint len = br.ReadUInt32();
                        if (len is 0 or > 1024 * 1024)
                            break;

                        byte[] data = br.ReadBytes((int)len);
                        string msg = Encoding.UTF8.GetString(data);

                        Log("Client →\n" + msg);
                    }

                    // ---- WRITE ----
                    while (outgoing.TryDequeue(out var msg)) {
                        byte[] data = Encoding.UTF8.GetBytes(msg);
                        bw.Write((uint)data.Length);
                        bw.Write(data);
                        bw.Flush();
                    }

                    Thread.Sleep(1); // prevent CPU spin
                }
            } catch (EndOfStreamException) {
                Log("Client disconnected");
            } finally {
                server.Dispose();
            }
        }

        public void SendMessage(string msg) {
            outgoing.Enqueue(msg);
        }

        public void Dispose() {
            server.Dispose();
        }
    }
}
