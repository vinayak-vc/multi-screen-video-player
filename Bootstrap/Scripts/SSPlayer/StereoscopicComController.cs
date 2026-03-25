using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using ViitorCloud.MultiScreenVideoPlayer;

using static Modules.Utility.Utility;

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

        public void RunAsync(BasicWebSocketClient basicWebSocketClient) {
            _basicWebSocketClient = basicWebSocketClient;
            LaunchExternalExe(Path.Combine(Application.streamingAssetsPath, "COMBridgeAppV1.exe"), out clientProcessId, out clientProcess);
        }

        public void SendMessage(string msg) {
            if (_basicWebSocketClient == null) {
                LogError("StereoscopicComController.SendMessage: basicWebSocketClient is null.");
                return;
            }
            _ = _basicWebSocketClient.Send("C" + msg);
        }
    }
}
