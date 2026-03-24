using System;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.Serialization;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class AndroidPlayer : MonoBehaviour {
        public static AndroidPlayer Instance;
        public MobileUIController mobileUIController;

        private void Awake() {
            Instance = this;
        }

        private void OnEnable() {
            BasicWebSocketClient.MessageReceived += ExecuteCommand;
            MobileTouchPad.TouchDeltaInput.OnDelta += OnDelta;
            MobileTouchPad.TouchDeltaInput.OnDown += OnDown;
            MobileTouchPad.TouchDeltaInput.SendTouchData += SendTouchData;
            MobileTouchPad.TouchDeltaInput.OnUp += OnUp;
        }

        private void OnDisable() {
            BasicWebSocketClient.MessageReceived -= ExecuteCommand;
            MobileTouchPad.TouchDeltaInput.OnDelta -= OnDelta;
            MobileTouchPad.TouchDeltaInput.OnDown -= OnDown;
            MobileTouchPad.TouchDeltaInput.SendTouchData -= SendTouchData;
            MobileTouchPad.TouchDeltaInput.OnUp -= OnUp;
        }


        public void ExecuteCommand(string command) {
            string[] commandData = command.Split(Commands.Separator);
            switch (commandData[0]) {
                case Commands.SliderData:
                    mobileUIController.SetProgress(double.Parse(commandData[1]), double.Parse(commandData[2]));
                    break;
                case Commands.NameVideo:
                    Log($"Executing Command : {command}");
                    mobileUIController.SetVideoName(JsonUtility.FromJson<VideoContainerList>(commandData[1]), commandData[2]);
                    break;
                case Commands.PlayThisVideo:
                    Log($"Executing Command : {command}");
                    mobileUIController.HighLightThisButton(commandData[1]);
                    break;
                case Commands.NewVideo:
                    Log($"Executing Command : {command}");
                    mobileUIController.NewVideoAdded(commandData[1]);
                    break;
                case Commands.GetImages:
                    Log($"Executing Command : {command}");
                    mobileUIController.ImagesReceived(commandData[1]);
                    break;
            }
        }

        private void OnUp() {
            //SendInputCommandToServer(Commands.InputCommands.OnUp, Modules.Utility.Utility.ToString(obj));
        }
        private void SendTouchData(Vector2 obj) {
            //SendInputCommandToServer(Commands.InputCommands.Delta, Modules.Utility.Utility.ToString(obj));
        }
        private void OnDown() {
            //SendInputCommandToServer(Commands.InputCommands.Delta, Modules.Utility.Utility.ToString(obj));
        }
        private void OnDelta(Vector2 obj) {
            SendInputCommandToServer(Commands.InputCommands.Delta, Modules.Utility.Utility.ToString(obj));
        }

        public void GetVideoName() {
            if (mobileUIController.isTheSameScene) {
                mobileUIController.SendCommandToServer(Commands.NameVideo);
            } else {
                BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.NameVideo);
            }
        }

        private void SendInputCommandToServer(string inputType, string command) {
            BootstrapManager.Instance.networkObject.SendCommandToServer($"{Commands.Input}{Commands.Separator}{inputType}{Commands.Separator}{command}");
        }
    }
}