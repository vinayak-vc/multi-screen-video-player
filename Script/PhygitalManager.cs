using System.Collections;

using Modules.Utility;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using ViitorCloud.MultiScreenVideoPlayer;


public class PhygitalManager : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ButtonController adaniNaturalResourcesButtonController;
    public ButtonController chhatishgadhButtonController;

    public FadeAnimationCanvasGroup playFadeAnimationCanvasGroup;
    public FadeAnimationCanvasGroup controlsFadeAnimationCanvasGroup;

    public VideoPlayerController adaniNaturalResourcesVideoContainer;
    public VideoPlayerController chhatishgadhVideoContainer;

    public Button backButton;

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
    }

    private void BackButtonPressed() {
        adaniNaturalResourcesVideoContainer.gameObject.SetActive(false);
        chhatishgadhVideoContainer.gameObject.SetActive(false);
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();
    }

    private void ChhatishgadhVideoLoopReached() {
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();
        chhatishgadhVideoContainer.gameObject.SetActive(false);
    }

    private void AdaniNaturalResourcesVideoLoopReached() {
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();

        adaniNaturalResourcesVideoContainer.gameObject.SetActive(false);
        // foreach (VideoPlayer videoPlayerList in adaniNaturalResourcesVideoContainer.GetVideoPlayerList()) {
        //     videoPlayerList.Stop();
        //     videoPlayerList.targetTexture.DiscardContents();
        // }
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
