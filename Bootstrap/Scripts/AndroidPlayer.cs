using System;

using static Modules.Utility.Utility;

using UnityEngine;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class AndroidPlayer : MonoBehaviour {
        public static AndroidPlayer Instance {
            get;
            private set;
        }

        public MobileUIController mobileUIController;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }
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
            if (string.IsNullOrEmpty(command)) {
                LogError("ExecuteCommand called with null or empty command.");
                return;
            }

            string[] commandData = command.Split(Commands.Separator);
            try {
                switch (commandData[0]) {
                    case Commands.SliderData:
                        if (commandData.Length < 3) {
                            LogError($"SliderData command missing arguments: '{command}'");
                            break;
                        }
                        if (double.TryParse(commandData[1], out double currentTime) &&
                            double.TryParse(commandData[2], out double length)) {
                            mobileUIController.SetProgress(currentTime, length);
                        } else {
                            LogError($"SliderData has invalid numeric arguments: '{command}'");
                        }
                        break;

                    case Commands.NameVideo:
                        Log($"Executing Command : {command}");
                        if (commandData.Length < 3) {
                            LogError($"NameVideo command missing arguments: '{command}'");
                            break;
                        }
                        VideoContainerList videoList = JsonUtility.FromJson<VideoContainerList>(commandData[1]);
                        if (videoList == null) {
                            LogError($"NameVideo failed to parse VideoContainerList.");
                            break;
                        }
                        mobileUIController.SetVideoName(videoList, commandData[2]);
                        break;

                    case Commands.PlayThisVideo:
                        Log($"Executing Command : {command}");
                        if (commandData.Length < 2) {
                            LogError($"PlayThisVideo command missing index argument: '{command}'");
                            break;
                        }
                        mobileUIController.HighLightThisButton(commandData[1]);
                        break;

                    case Commands.NewVideo:
                        Log($"Executing Command : {command}");
                        if (commandData.Length < 2) {
                            LogError($"NewVideo command missing argument: '{command}'");
                            break;
                        }
                        mobileUIController.NewVideoAdded(commandData[1]);
                        break;

                    case Commands.GetImages:
                        Log($"Executing Command : {command}");
                        if (commandData.Length < 2) {
                            LogError($"GetImages command missing argument: '{command}'");
                            break;
                        }
                        mobileUIController.ImagesReceived(commandData[1]);
                        break;
                }
            } catch (Exception e) {
                LogError($"Error executing command '{commandData[0]}': {e.Message}");
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
                if (BootstrapManager.Instance == null) {
                    LogError("GetVideoName: BootstrapManager.Instance is null.");
                    return;
                }
                if (BootstrapManager.Instance.networkObject == null) {
                    LogError("GetVideoName: networkObject is null — client may not be connected yet.");
                    return;
                }
                BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.NameVideo);
            }
        }

        private void SendInputCommandToServer(string inputType, string command) {
            if (BootstrapManager.Instance?.networkObject == null) return;
            BootstrapManager.Instance.networkObject.SendCommandToServer($"{Commands.Input}{Commands.Separator}{inputType}{Commands.Separator}{command}");
        }
    }
}
