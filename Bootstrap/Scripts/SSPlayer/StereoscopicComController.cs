using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Modules.Utility;

using UnityEngine;

using ViitorCloud.MultiScreenVideoPlayer;


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
        private BasicWebSocketClient _basicWebSocketClient;

        private void LaunchComClient() {
            //return;
            string clientAppPath = Path.Combine(Application.streamingAssetsPath, "COMBridgeAppV1.exe");
#if !UNITY_EDITOR
            if (!Utility.IsProcessRunning("COMBridgeAppV1", out clientProcess)) {
                if (File.Exists(clientAppPath)) {
                    clientProcessId = CrossPlatformProcessLauncher.Start(clientAppPath, Application.streamingAssetsPath, "", true);
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

        public void RunAsync(BasicWebSocketClient basicWebSocketClient) {
            _basicWebSocketClient = basicWebSocketClient;
            LaunchComClient();
        }

        public void SendMessage(string msg) {
            _ = _basicWebSocketClient.Send("C"+msg);
        }
    }
}