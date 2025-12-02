using System;
using System.Collections;
using System.Collections.Generic;

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
        public static BootstrapManager Instance {
            get;
            private set;
        }

        public int port = 7777;
        public bool isThisWindows;

        public static Action OnClientDisconnected;
        public static Action<bool> OnClientConnected;
        private bool _isConnected;

        private UnityTransport _transport;
        private NetworkManager _networkManager;
        public NetworkMediator networkObject;
        public List<NetworkMediator> networkObjects;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
            _networkManager = NetworkManager.Singleton;
            _transport = GetComponent<UnityTransport>();
        }

        private void OnEnable() {
            if (_networkManager) {
                _networkManager.OnClientConnectedCallback += OnClientConnectedToServer;
                _networkManager.OnClientDisconnectCallback += NetworkManagerOnOnClientDisconnectCallback;
            }
        }

        private void OnDisable() {
            if (_networkManager) {
                _networkManager.OnClientConnectedCallback -= OnClientConnectedToServer;
                _networkManager.OnClientDisconnectCallback -= NetworkManagerOnOnClientDisconnectCallback;
            }
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
                _transport.SetConnectionData(IPManager.GetIP(ADDRESSFAM.IPv4), (ushort)port);
                _ = _networkManager.StartHost();
            }
        }

        public void DisconnectClient() {
            _networkManager.Shutdown();
        }

        public void Connect(string ipTextText) {
            _transport.SetConnectionData(ipTextText, (ushort)port);
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
