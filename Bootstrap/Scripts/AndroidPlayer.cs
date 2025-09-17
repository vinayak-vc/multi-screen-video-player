using System;

using UnityEngine;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class AndroidPlayer : MonoBehaviour {
        public static AndroidPlayer Instance;

        private void Awake() {
            Instance = this;
        }

        public void ExecuteCommand(string command) {
            string[] commandData = command.Split(Commands.Separator);
            switch (commandData[0]) {
                case Commands.SliderData:
                    BootstrapManager.Instance.uiController.SetProgress(double.Parse(commandData[1]), double.Parse(commandData[2]));
                    break;
                case Commands.NameVideo:
                    BootstrapManager.Instance.uiController.SetVideoName(commandData[1]);
                    break;
            }
        }
        public void GetVideoName() {
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.NameVideo);
        }
    }
}