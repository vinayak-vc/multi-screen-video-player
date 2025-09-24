using System;
using System.Collections.Generic;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public static class Commands {
        public const char Separator = '~';
        public const string Play = "Play";
        public const string Pause = "Pause";
        public const string Stop = "Stop";
        public const string Restart = "Restart";
        public const string Mute = "Mute";
        public const string Unmute = "Unmute";
        public const string ToggleMute = "ToggleMute";
        public const string Seek = "Seek";
        public const string SetPlaybackSpeed = "SetPlaybackSpeed";
        public const string SliderData = "SliderData";
        public const string NameVideo = "NameVideo";
        public const string PlayThisVideo = "PlayThisVideo";
        public const string Loop = "Loop";
    }

    [Serializable]
    public class VideoContainerList {
        public List<VideoContainer> videoContainerList = new List<VideoContainer>();
    }

    [Serializable]
    public class VideoContainer {
        public string folderName;
        public string[] videoPath;
        public string audioPath;
    }
}