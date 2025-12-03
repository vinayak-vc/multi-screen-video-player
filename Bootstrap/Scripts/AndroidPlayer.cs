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
            }
        }

        public void GetVideoName() {
            if (mobileUIController.isTheSameScene) {
                mobileUIController.SendCommandToServer(Commands.NameVideo);
            } else {
                BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.NameVideo);
            }
        }
    }
}
