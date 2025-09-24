using static Modules.Utility.Utility;

using UnityEngine;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class AndroidPlayer : MonoBehaviour {
        public static AndroidPlayer Instance;
        public UIController uiController;
        private void Awake() {
            Instance = this;
        }

        public void ExecuteCommand(string command) {
            string[] commandData = command.Split(Commands.Separator);
            switch (commandData[0]) {
                case Commands.SliderData:
                    uiController.SetProgress(double.Parse(commandData[1]), double.Parse(commandData[2]));
                    break;
                case Commands.NameVideo:
                    Log($"Executing Command : {command}");
                    uiController.SetVideoName(JsonUtility.FromJson<VideoContainerList>(commandData[1]), commandData[2]);
                    break;
                case Commands.PlayThisVideo:
                    Log($"Executing Command : {command}");
                    uiController.HighLightThisButton(commandData[1]);
                    break;
            }
        }
        public void GetVideoName() {
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.NameVideo);
        }
    }
}