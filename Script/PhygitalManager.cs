using System.Collections;

using Modules.Utility;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using ViitorCloud.MultiScreenVideoPlayer;
using System.IO;


public class PhygitalManager : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ButtonController adaniNaturalResourcesButtonController;
    public ButtonController chhatishgadhButtonController;

    public FadeAnimationCanvasGroup playFadeAnimationCanvasGroup;
    public FadeAnimationCanvasGroup controlsFadeAnimationCanvasGroup;

    public VideoPlayerController adaniNaturalResourcesVideoContainer;
    public VideoPlayerController chhatishgadhVideoContainer;

    public Button backButton;

    public float maxTimeBetweenPresses = 0.3f; // seconds

    public VideoPlayer videoPlayer1;
    public VideoPlayer videoPlayer2;
    private string bgVideoPath1;
    private string bgVideoPath2;

    private float lastPressTime;
    private int pressCount;

    private IEnumerator Start() {
        adaniNaturalResourcesButtonController.Init("Adani", 0, true);
        chhatishgadhButtonController.Init("Chhattisgarh", 1, true);

        adaniNaturalResourcesButtonController.GetComponent<Button>()
            .onClick.AddListener(AdaniNaturalResourcesButtonClickEvent);
        chhatishgadhButtonController.GetComponent<Button>()
            .onClick.AddListener(ChhatishgadhButtonClickEvent);

        yield return new WaitForSecondsRealtime(2);

        adaniNaturalResourcesVideoContainer = WindowsPlayer.Instance.GetVideoContainer("Adani");
        chhatishgadhVideoContainer = WindowsPlayer.Instance.GetVideoContainer("Chhattisgarh");

        adaniNaturalResourcesVideoContainer.VideoPlayerOnLoopPointReached += BackButtonPressed;
        chhatishgadhVideoContainer.VideoPlayerOnLoopPointReached += BackButtonPressed;

        backButton.onClick.AddListener(BackButtonPressed);

        bgVideoPath1 = Path.Combine(Application.streamingAssetsPath, "bgVideo1.mp4");
        bgVideoPath2 = Path.Combine(Application.streamingAssetsPath, "bgVideo2.mp4");
        VideoPlayerAssignemnt(videoPlayer1 , bgVideoPath1);
        VideoPlayerAssignemnt(videoPlayer2, bgVideoPath2);
    }

    private void VideoPlayerAssignemnt(VideoPlayer videoPlayer , string bgVideoPath) {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.url = bgVideoPath;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
    }

    private void VideoPlayer_prepareCompleted(VideoPlayer source) {
        source.prepareCompleted -= VideoPlayer_prepareCompleted;
        source.Play();
    }

    private void BackButtonPressed() {
        adaniNaturalResourcesVideoContainer.gameObject.SetActive(false);
        chhatishgadhVideoContainer.gameObject.SetActive(false);
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();
    }

    public void OnButtonPressed() {
        float time = Time.time;

        if (time - lastPressTime <= maxTimeBetweenPresses) {
            pressCount++;
        } else {
            pressCount = 1;
        }

        lastPressTime = time;

        if (pressCount == 2) {
            OnDoublePressed();
            pressCount = 0;
        }
    }

    private void OnDoublePressed() {
        Log("Bye");
        Application.Quit(0);
    }


    private void ChhatishgadhButtonClickEvent() {
        chhatishgadhVideoContainer.gameObject.SetActive(true);
        controlsFadeAnimationCanvasGroup.FadeIn();
        playFadeAnimationCanvasGroup.FadeOut();
    }

    private void AdaniNaturalResourcesButtonClickEvent() {
        adaniNaturalResourcesVideoContainer.gameObject.SetActive(true);
        controlsFadeAnimationCanvasGroup.FadeIn();
        playFadeAnimationCanvasGroup.FadeOut();
    }
}
