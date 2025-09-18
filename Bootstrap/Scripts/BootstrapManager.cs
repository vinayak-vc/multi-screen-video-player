using System;
using System.Collections;

using JetBrains.Annotations;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.Serialization;

namespace ViitorCloud.MultiScreenVideoPlayer {
    /// <summary>
    /// Class to display helper buttons and status labels on the GUI, as well as buttons to start host/client/server.
    /// Once a connection has been established to the server, the local player can be teleported to random positions via a GUI button.
    /// </summary>
    public class BootstrapManager : MonoBehaviour {

        public static BootstrapManager Instance { get; private set; }

        public bool isThisWindows;
        public UIController uiController;
        public static Action OnClientDisconnected;
        public static Action<bool> OnClientConnected;
        private bool _isConnected;

        private UnityTransport _transport;
        private NetworkManager _networkManager;
        [HideInInspector] public NetworkMediator networkObject;
        private void Awake() {
            Instance = this;
            _networkManager = NetworkManager.Singleton;
            _transport = GetComponent<UnityTransport>();
        }

        private void OnEnable() {
            _networkManager.OnClientConnectedCallback += OnClientConnectedToServer;
            _networkManager.OnClientDisconnectCallback += NetworkManagerOnOnClientDisconnectCallback;
        }

        private void OnDisable() {
            _networkManager.OnClientConnectedCallback -= OnClientConnectedToServer;
            _networkManager.OnClientDisconnectCallback -= NetworkManagerOnOnClientDisconnectCallback;
        }
        private void NetworkManagerOnOnClientDisconnectCallback(ulong obj) {
            if (_isConnected) {
                _isConnected = false;
                OnClientDisconnected?.Invoke();
            }
        }

        private void OnClientConnectedToServer(ulong obj) {
            if (!isThisWindows) {
                _isConnected = true;
                StopCoroutine(CheckForConnection());
                OnClientConnected?.Invoke(true);
                AndroidPlayer.Instance.GetVideoName();
            }
        }

        private void Start() {
            if (isThisWindows) {
                _transport.SetConnectionData(IPManager.GetIP(ADDRESSFAM.IPv4), 7777);
                _ = _networkManager.StartHost();
            }
        }
        public void Connect(string ipTextText) {
            _transport.SetConnectionData(ipTextText, 7777);
            _networkManager.StartClient();
            StartCoroutine(CheckForConnection());
        }
        private IEnumerator CheckForConnection() {
            yield return new WaitForSecondsRealtime(5);
            if (!_isConnected) {
                OnClientConnected?.Invoke(false);
            }
        }
    }
}