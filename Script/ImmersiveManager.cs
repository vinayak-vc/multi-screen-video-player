using System;

using static Modules.Utility.Utility;

using Modules.Utility;

using UnityEngine;

using ViitorCloud.MultiScreenVideoPlayer;


public class ImmersiveManager : MonoBehaviour {

    public ButtonController[] buttonControllers;
    public Sprite[] buttonImages;

    public FadeAnimationCanvasGroup playFadeAnimationCanvasGroup;
    public FadeAnimationCanvasGroup controlsFadeAnimationCanvasGroup;

    public MobileUIController mobileUIController;

    public LightScrollSnap.ScrollSnap scrollSnap;

    private void Awake() {
        MobileUIController.ConnectionSuccesfull += ConnectionSuccesfullCallback;
        MobileUIController.HighLightButtonEvent += HighLightButtonEvent;
    }

    private void Start() {
        buttonControllers[0].GetButton().onClick.AddListener(PlayButtonClickEvent);
        buttonControllers[0].GetButton().onClick.AddListener(mobileUIController.SetFullScreenSSPlayer);
        buttonControllers[0].Init("DefaultVideo", 0, true);

        //buttonControllers[1].Init("Smart technology automation", 1, true);
        //buttonControllers[2].Init("Advance safety and worker welfare", 2, true);
        //buttonControllers[3].Init("Renewable energy integration", 3, true);
        //buttonControllers[4].Init("Water management and conservation", 4, true);
        //buttonControllers[5].Init("Environment restoration and conservation", 5, true);
        //buttonControllers[6].Init("Community healthcare excellence", 6, true);
        //buttonControllers[7].Init("Education and skill development", 7, true);
        //buttonControllers[8].Init("Inclusive and sustainable growth", 8, true);
    }

    private void ConnectionSuccesfullCallback() {
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();
        mobileUIController.OnLoopToggleValueChanged(true);
    }

    private void HighLightButtonEvent(int index) {
        ItemSelected(index, null);
    }

    public void ItemSelected(int index, RectTransform rectTransform) {
        // rectTransform.GetComponent<ButtonController>().OnClick();
        scrollSnap.SmoothScrollToItem(index);
    }

    private void PlayButtonClickEvent() {
        controlsFadeAnimationCanvasGroup.FadeIn();
        playFadeAnimationCanvasGroup.FadeOut();
    }

    public void BackButtonClickEvent() {
        controlsFadeAnimationCanvasGroup.FadeOut();
        playFadeAnimationCanvasGroup.FadeIn();
        mobileUIController.OnStopButtonClicked();
    }

    private void OnDisable() {
        MobileUIController.ConnectionSuccesfull -= ConnectionSuccesfullCallback;
        MobileUIController.HighLightButtonEvent -= HighLightButtonEvent;

    }
}