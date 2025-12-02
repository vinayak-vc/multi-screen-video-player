using Unity.Netcode;

using UnityEngine;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class NetworkMediator : NetworkBehaviour {
        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();

            // Initialization logic if needed
            BootstrapManager.Instance.networkObject = this;
            BootstrapManager.Instance.networkObjects.Add(this);
            name = IsLocalPlayer ? "LocalPlayer" : "RemotePlayer";
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
            if (BootstrapManager.Instance.networkObjects.Count >= 2) {
                SubmitCommandClientRpc(command);
            }
        }

        // RPC to send the command to the server (Windows app)
        [ClientRpc]
        private void SubmitCommandClientRpc(string command, ClientRpcParams rpcParams = default) {
            try {
                if (AndroidPlayer.Instance) {
                    AndroidPlayer.Instance.ExecuteCommand(command);
                }
            } catch {
                // ignored
            }
        }
    }
}
