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
        private readonly string pipeName = "CR7";
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<string> _outgoing = new();
        private readonly SemaphoreSlim _sendSignal = new(0);
        private Task? _serverTask;
        private Process? clientProcess;
        private int clientProcessId;

        public static Action ClientConnected;

        private void LaunchComClient() {
            return;
            string clientAppPath = Path.Combine(Application.streamingAssetsPath, "COMBridgeAppV1.exe");
#if !UNITY_EDITOR
            if (!IsProcessRunning("COMBridgeAppV1", out clientProcess)) {
                if (File.Exists(clientAppPath)) {
                    clientProcessId = CrossPlatformProcessLauncher.Start(clientAppPath, Application.streamingAssetsPath, "", false);
                    clientProcess = Process.GetProcessById(clientProcessId);
                }
            }
#else
            Process process = new Process();
            ProcessStartInfo startInfo = process.StartInfo;
            startInfo.FileName = clientAppPath;
            process.Start();
#endif
        }

        public async void RunAsync() {
            try {
                CancellationToken token = _cts.Token;
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                while (!token.IsCancellationRequested) {
                    LaunchComClient();

                    await using NamedPipeServerStream server = new(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                    Log("Waiting for client...");
                    try {
                        await server.WaitForConnectionAsync(token); // non‑blocking
                    } catch (OperationCanceledException) {
                        break;
                    }

                    Log("Client connected");
                    ClientConnected?.Invoke();
                    using BinaryReader br = new(server, Encoding.UTF8, leaveOpen: true);
                    using BinaryWriter bw = new(server, Encoding.UTF8, leaveOpen: true);

                    CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    Task readTask = ReadLoopAsync(br, linkedCts.Token);
                    Task writeTask = WriteLoopAsync(bw, linkedCts.Token);

                    Task completed = await Task.WhenAny(readTask, writeTask);
                    linkedCts.Cancel(); // stop the other side

                    try {
                        await Task.WhenAll(readTask, writeTask);
                    } catch { /* ignore read/write errors here */
                    }

                    Log("Client disconnected");
                }
            } catch {
                // ignored
            }
        }

        private async Task ReadLoopAsync(BinaryReader br, CancellationToken token) {
            try {
                Stream baseStream = br.BaseStream;
                byte[] lenBuffer = new byte[4];

                while (!token.IsCancellationRequested) {
                    // Read length prefix
                    int read = await baseStream.ReadAsync(lenBuffer.AsMemory(0, 4), token);
                    if (read == 0)
                        break; // client closed

                    if (read < 4)
                        throw new IOException("Incomplete length prefix");

                    uint len = BitConverter.ToUInt32(lenBuffer, 0);
                    if (len == 0 || len > 1024 * 1024)
                        throw new IOException("Invalid message length");

                    var data = new byte[len];
                    int offset = 0;
                    while (offset < len) {
                        int n = await baseStream.ReadAsync(data.AsMemory(offset, (int)len - offset), token);
                        if (n == 0)
                            throw new EndOfStreamException();
                        offset += n;
                    }

                    string msg = Encoding.UTF8.GetString(data);
                    Log("Client → " + msg);

                    // Optionally enqueue to main thread via a thread‑safe queue
                }
            } catch (OperationCanceledException ex) {
                Log("Pipe write failed: " + ex.Message);
            } catch (EndOfStreamException ex) {
                Log("Pipe write failed: " + ex.Message);
            } catch (IOException ex) {
                Log("Pipe write failed: " + ex.Message);
            }
        }

        private async Task WriteLoopAsync(BinaryWriter bw, CancellationToken token) {
            var baseStream = bw.BaseStream;
            try {
                while (!token.IsCancellationRequested) {
                    // Wait until there is something to send
                    await _sendSignal.WaitAsync(token);

                    while (_outgoing.TryDequeue(out var msg)) {
                        if (!baseStream.CanWrite)
                            return;

                        byte[] data = Encoding.UTF8.GetBytes(msg);
                        byte[] lenBytes = BitConverter.GetBytes((uint)data.Length);

                        await baseStream.WriteAsync(lenBytes.AsMemory(0, 4), token);
                        await baseStream.WriteAsync(data.AsMemory(0, data.Length), token);
                        await baseStream.FlushAsync(token);
                    }
                }
            } catch (OperationCanceledException ex) {
                Log("Pipe write failed: " + ex.Message);
            } catch (IOException ex) {
                Log("Pipe write failed: " + ex.Message);
            }
        }

        public void SendMessage(string msg) {
            _outgoing.Enqueue(msg);
            _sendSignal.Release();
        }

        public void Dispose() {
            _cts.Cancel();
            try {
                _serverTask?.Wait(1000);
            } catch {
            }
            if (clientProcess is { HasExited: false }) {
                clientProcess.Kill();
            }
            _cts.Dispose();
            _sendSignal.Dispose();
        }
    }
}
