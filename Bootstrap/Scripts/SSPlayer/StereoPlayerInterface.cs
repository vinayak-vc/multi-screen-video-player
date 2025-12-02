// Stereoscopic Player COM automation interface
//
// Copyright (c) Peter Wimmer
// All rights reserved.
//
// Interface definition for Stereoscopic Player COM automation. 
// The ProgId of the automation class is "StereoPlayer.Automation".
//
// VBScript examples are available on the 3dtv.at website for download.

using System;   
using System.Runtime.InteropServices;


namespace StereoPlayer {
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("73B28B6E-D306-4589-B032-9ED17AA4D182")]
    [TypeLibType(TypeLibTypeFlags.FDispatchable | TypeLibTypeFlags.FDual | TypeLibTypeFlags.FOleAutomation)]
    public interface IAutomation {
        /// <summary>
        /// Returns the number of items in the playlist.
        /// </summary>
        [DispId(0xc9)]
        void GetPlaylistItemCount(out int count);

        /// <summary>
        /// Returns the index of the currently loaded playlist item.
        /// </summary>
        [DispId(0xca)]
        void GetPlaylistItem(out int index);

        /// <summary>
        /// Loads the specified playlist item.
        /// </summary>
        [DispId(0xcb)]
        void SetPlaylistItem([In] int index);

        /// <summary>
        /// Loads the next playlist item.
        /// </summary>
        [DispId(0xcc)]
        void NextPlaylistItem();

        /// <summary>
        /// Loads the previous playlist item.
        /// </summary>
        [DispId(0xcd)]
        void PreviousPlaylistItem();

        /// <summary>
        /// Opens a stereoscopic video file.
        /// </summary>
        [DispId(0xce)]
        void OpenFile([In, MarshalAs(UnmanagedType.BStr)] string filename);

        /// <summary>
        /// Opens separate left and right video files as well as an optional audio file.
        /// </summary>
        [DispId(0xcf)]
        void OpenLeftRightFiles([In, MarshalAs(UnmanagedType.BStr)] string leftFilename, [In, MarshalAs(UnmanagedType.BStr)] string rightFilename, [In, MarshalAs(UnmanagedType.BStr)] string audioFilename, [In] AudioMode audioMode);

        /// <summary>
        /// Opens a DVD, specified by the path (without VIDEO_TS folder).
        /// </summary>
        [DispId(0xd0)]
        void OpenDVD([In, MarshalAs(UnmanagedType.BStr)] string path);

        /// <summary>
        /// Opens a URL.
        /// </summary>
        [DispId(0xd1)]
        void OpenURL([In, MarshalAs(UnmanagedType.BStr)] string url);

        /// <summary>
        /// Opens a capture device. Either specifiy the friendly name (might not be unique) or the device identified.
        /// </summary>
        [DispId(210)]
        void OpenDevice([In, MarshalAs(UnmanagedType.BStr)] string device);

        /// <summary>
        /// Returns the player's state (playing, stopped, ...).
        /// </summary>
        [DispId(0xd3)]
        void GetPlaybackState(out PlaybackState state);

        /// <summary>
        /// Executes a playback command (play, stop, ...).
        /// </summary>
        [DispId(0xd4)]
        void SetPlaybackState([In] PlaybackState state);

        /// <summary>
        /// Returns TRUE if playback has completed, else FALSE.
        /// </summary>
        [DispId(0xd5)]
        void GetPlaybackCompleted(out bool completed);

        /// <summary>
        /// Returns the duration of the video in seconds.
        /// </summary>
        [DispId(0xd7)]
        void GetDuration(out double duration);

        /// <summary>
        /// Returns the current playback position in seconds.
        /// </summary>
        [DispId(0xd8)]
        void GetPosition(out double position);

        /// <summary>
        /// Returns the playback mode (none, file, DVD, ...).
        /// </summary>
        [DispId(0xd6)]
        void GetPlaybackMode(out PlaybackMode mode);

        /// <summary>
        /// Sets the current playback position in seconds.
        /// </summary>
        [DispId(0xd9)]
        void SetPosition([In] double position);

        /// <summary>
        /// Returns if repeat mode is enabled.
        /// </summary>
        [DispId(0xda)]
        void GetRepeat(out bool repeatMode);

        /// <summary>
        /// Enables or disables repeat mode.
        /// </summary>
        [DispId(0xdb)]
        void SetRepeat([In] bool repeatMode);

        /// <summary>
        /// Closes the video.
        /// </summary>
        [DispId(220)]
        void CloseVideo();

        /// <summary>
        /// Terminates the Stereoscopic Player.
        /// </summary>
        [DispId(0xdd)]
        void ClosePlayer();

        /// <summary>
        /// Enters full screen mode.
        /// </summary>
        [DispId(0xde)]
        void EnterFullscreenMode();

        /// <summary>
        /// Leaves full screen mode.
        /// </summary>
        [DispId(0xdf)]
        void LeaveFullscreenMode();

        /// <summary>
        /// Returns if the player is ready. Many of the other commands fail as long as the player is not ready.
        /// </summary>
        [DispId(0xe0)]
        void GetReady(out bool ready);

        /// <summary>
        /// Returns the current audio volume.
        /// </summary>
        /// <param name="volume"></param>
        [DispId(0xe1)]
        void GetVolume(out double volume);

        /// <summary>
        /// Sets the current audio volume.
        /// </summary>
        [DispId(0xe2)]
        void SetVolume([In] double volume);

        /// <summary>
        /// Sets the horizontal parallax of the current video.
        /// </summary>
        [DispId(0xe3)]
        void SetParallaxHorizontal([In] int parallax);

        /// <summary>
        /// Sets the vertical parallax of the current video.
        /// </summary>
        [DispId(0xe4)]
        void SetParallaxVertical([In] int parallax);

        /// <summary>
        /// Sets horizontal and vertical parallax of the current video.
        /// </summary>
        [DispId(0xe5)]
        void SetParallax([In] int horizontal, [In] int vertical);

        /// <summary>
        /// Returns the current zoom in percent.
        /// </summary>
        [DispId(230)]
        void GetZoom(out double zoom);

        /// <summary>
        /// Sets the current zoom in percent.
        /// </summary>
        [DispId(0xe7)]
        void SetZoom([In] double zoom);

        /// <summary>
        /// Goes to the next DVD chapter.
        /// </summary>
        [DispId(0xe8)]
        void NextChapter();

        /// <summary>
        /// Goes to the previous DVD chapter.
        /// </summary>
        [DispId(0xe9)]
        void PreviousChapter();

        /// <summary>
        /// Loads the next file from the folder of the current file.
        /// </summary>
        [DispId(0xea)]
        void NextFileInFolder();

        /// <summary>
        /// Loads the previous file from the folder of the current file.
        /// </summary>
        [DispId(0xeb)]
        void PreviousFileInFolder();

        /// <summary>
        /// Returns if play folder mode is enabled.
        /// </summary>
        [DispId(0xec)]
        void GetPlayFolder(out bool playFolderMode);

        /// <summary>
        /// Enables or disables play folder mode.
        /// </summary>
        [DispId(0xed)]
        void SetPlayFolder([In] bool playFolderMode);

        /// <summary>
        /// Sets the viewing method.
        /// </summary>
        /// <param name="viewingMethod"></param>
        [DispId(0xee)]
        void GetViewingMethod([Out, MarshalAs(UnmanagedType.BStr)] out string viewingMethod);

        /// <summary>
        /// Returns the current viewing method.
        /// </summary>
        [DispId(0xef)]
        void SetViewingMethod([In, MarshalAs(UnmanagedType.BStr)] string viewingMethod);

        /// <summary>
        /// Jumps back to last start position.
        /// </summary>
        [DispId(0xf0)]
        void Replay();

        /// <summary>
        /// Steps one frame forwards.
        /// </summary>
        [DispId(0xf1)]
        void StepForwards();

        /// <summary>
        /// Steps one frame backwards.
        /// </summary>
        [DispId(0xf2)]
        void StepBackwards();

        /// <summary>
        /// Returns if effect control DMX output is enabled.
        /// </summary>
        [DispId(0xf3)]
        void GetEffectControlDMXOutputEnabled(out bool enabled);

        /// <summary>
        /// Enables or disables effect control DMX output.
        /// </summary>
        [DispId(0xf4)]
        void SetEffectControlDMXOutputEnabled([In] bool enabled);

        /// <summary>
        /// Returns if effect control byte stream output is enabled.
        /// </summary>
        [DispId(0xf5)]
        void GetEffectControlByteStreamOutputEnabled(out bool enabled);

        /// <summary>
        /// Enables or disables effect control byte stream output.
        /// </summary>
        [DispId(0xf6)]
        void SetEffectControlByteStreamOutputEnabled([In] bool enabled);

        /// <summary>
        /// Returns if effect control named devices output is enabled.
        /// </summary>
        [DispId(0xf7)]
        void GetEffectControlNamedDevicesOutputEnabled(out bool enabled);

        /// <summary>
        /// Enables or disables effect control named devices output.
        /// </summary>
        [DispId(0xf8)]
        void SetEffectControlNamedDevicesOutputEnabled([In] bool enabled);

        /// <summary>
        /// Returns if effect control command execution is enabled.
        /// </summary>
        [DispId(0xf9)]
        void GetEffectControlCommandExecutionEnabled(out bool enabled);

        /// <summary>
        /// Enables or disables effect control command execution.
        /// </summary>
        [DispId(0xfa)]
        void SetEffectControlCommandExecutionEnabled([In] bool enabled);

        /// <summary>
        /// Returns if toolbar is visible.
        /// </summary>
        [DispId(0xfb)]
        void GetToolbarVisible(out bool enabled);

        /// <summary>
        /// Shows or hides the toolbar.
        /// </summary>
        [DispId(0xfc)]
        void SetToolbarVisible([In] bool visible);

        /// <summary>
        /// Returns if menu is visible.
        /// </summary>
        [DispId(0xfd)]
        void GetMenuVisible(out bool enabled);

        /// <summary>
        /// Shows or hides the menu.
        /// </summary>
        [DispId(0xfe)]
        void SetMenuVisible([In] bool visible);

        /// <summary>
        /// Returns if window border is visible.
        /// </summary>
        [DispId(0xff)]
        void GetWindowBorderVisible(out bool enabled);

        /// <summary>
        /// Shows or hides the window border.
        /// </summary>
        [DispId(0x100)]
        void SetWindowBorderVisible([In] bool visible);

        /// <summary>
        /// Returns if left/right are swapped.
        /// </summary>
        [DispId(0x101)]
        void GetSwapLeftRight(out bool swapped);

        /// <summary>
        /// Swaps left/right.
        /// </summary>
        [DispId(0x102)]
        void SetSwapLeftRight([In] bool swapped);

        /// <summary>
        /// Returns if playback start after open.
        /// </summary>
        [DispId(0x103)]
        void GetAutoPlay(out bool enabled);

        /// <summary>
        /// Enables or disables playback start after open.
        /// </summary>
        [DispId(0x104)]
        void SetAutoPlay([In] bool enabled);

        /// <summary>
        /// Returns if HID events are handled.
        /// </summary>
        [DispId(0x105)]
        void GetHIDEventsEnabled(out bool enabled);

        /// <summary>
        /// Enables or disables human input device events (mouse and keyboard
        /// events). The menu will remain active, it can be disabled separately.
        /// </summary>
        [DispId(0x106)]
        void SetHIDEventsEnabled([In] bool enabled);

        /// <summary>
        /// Returns the duration of a single frame in seconds (1 / frame rate).
        /// </summary>
        [DispId(0x107)]
        void GetFrameDuration(out double duration);
    }

    [ComVisible(true)]
    [Guid("9D79E9CE-E2EC-423D-846C-17B887E6CD3E")]
    public enum AudioMode {
        NoAudio = 0,
        SeparateFile = 1,
        LeftFile = 2,
        RightFile = 3
    }

    [ComVisible(true)]
    [Guid("36F2BF51-D29E-498C-AFEA-BFCE002AE1AF")]
    public enum PlaybackMode {
        None = 0,
        File = 1,
        LeftRightFiles = 2,
        Dvd = 3,
        Url = 4,
        Device = 5
    }

    [ComVisible(true)]
    [Guid("44C1616D-FBE8-415D-869C-6D113C780C7F")]
    public enum PlaybackState {
        Play = 0,
        Pause = 1,
        Stop = 2,
        FastForward = 3,
        FastBackward = 4
    }
}
