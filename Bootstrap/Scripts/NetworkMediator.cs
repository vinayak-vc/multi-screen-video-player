using System;

using Unity.Netcode;

using static Modules.Utility.Utility;

using UnityEngine;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class NetworkMediator : NetworkBehaviour {

        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            // Initialization logic if needed
            BootstrapManager.Instance.networkObject = this;
        }

        // This method is called by UIController to send commands to the Windows player
        public void SendCommandToServer(string command) {
            SubmitCommandServerRpc(command);
        }

        // RPC to send the command to the server (Windows app)
        [ServerRpc]
        private void SubmitCommandServerRpc(string command, ServerRpcParams rpcParams = default) {
            
            if (WindowsPlayer.Instance) {
                WindowsPlayer.Instance.ExecuteCommand(command);
            }
        }

        public void SendCommandToClient(string command) {
            SubmitCommandClientRpc(command);
        }

        // RPC to send the command to the server (Windows app)
        [ClientRpc]
        private void SubmitCommandClientRpc(string command, ClientRpcParams rpcParams = default) {
            if (AndroidPlayer.Instance) {
                AndroidPlayer.Instance.ExecuteCommand(command);
            }
        }
    }
}