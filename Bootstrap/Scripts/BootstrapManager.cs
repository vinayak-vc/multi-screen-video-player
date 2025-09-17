using System;

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

        private UnityTransport _transport;
        private NetworkManager _networkManager;
        [HideInInspector] public NetworkMediator networkObject;
        private void Awake() {
            Instance = this;
            _networkManager = NetworkManager.Singleton;
            _transport = GetComponent<UnityTransport>();
        }

        private void OnEnable() {
            _networkManager.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDisable() {
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong obj) {
            if (!isThisWindows) {
                AndroidPlayer.Instance.GetVideoName();
            }
        }

        private void Start() {
            if (isThisWindows) {
                _transport.SetConnectionData(IPManager.GetIP(ADDRESSFAM.IPv4), 7777);
                _networkManager.StartHost();
            } else {
                _networkManager.StartClient();
            }
        }
    }
}