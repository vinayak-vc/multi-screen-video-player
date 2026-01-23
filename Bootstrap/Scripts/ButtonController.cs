using System;
using System.IO;

using Coffee.UIEffects;

using DG.Tweening;

using TMPro;

using UnityEngine;
using UnityEngine.UI;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class ButtonController : MonoBehaviour {
        [SerializeField]
        private TextMeshProUGUI buttonText;

        [SerializeField]
        private Button button;

        [SerializeField]
        private Sprite variation1;

        [SerializeField]
        private Sprite variation2;

        [SerializeField]
        private UIEffect highlightObject;

        [SerializeField]
        private RawImage buttonImage;

        private bool _ignoreHighlight;
        private int _myIndex;

        public void Init(string folderName, int index, bool ignoreHighlight = false) {
            buttonText.text = folderName;
            _myIndex = index;
            name = folderName;
            _ignoreHighlight = ignoreHighlight;
            
        }
        

        private void OnEnable() {
            button.onClick.AddListener(OnClick);

            //button.image.sprite = transform.GetSiblingIndex() % 2 == 0 ? variation1 : variation2;
        }

        private void OnDisable() {
            button.onClick.RemoveListener(OnClick);
        }

        public void FillTheThumbnail(string imagePath) {
            if (buttonImage && imagePath != string.Empty) {
                try {
                    buttonImage.texture = Modules.Utility.Utility.LoadTexture(imagePath);
                } catch {
                    // ignored
                }
                buttonImage.enabled = buttonImage.texture;
            }
        }

        public void OnClick() {
            AndroidPlayer.Instance.mobileUIController.PlayThisVideo(buttonText.text, _myIndex);
        }

        public void HighLightButton() {
            if (_ignoreHighlight) return;

            highlightObject.enabled = true;
            transform.SetAsLastSibling();
            button.image.sprite = variation2;
            transform.DOScale(Vector3.one * 1.5f, 0.2f).SetEase(Ease.OutBack);
        }

        public void DeHighLightButton() {
            if (_ignoreHighlight) return;

            highlightObject.enabled = false;
            button.image.sprite = variation1;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        public Button GetButton() {
            return button;
        }
    }
}