using System;

using Coffee.UIEffects;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class ButtonController : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Button button;

        [SerializeField] private Sprite variation1;
        [SerializeField] private Sprite variation2;

        [SerializeField] private UIEffect highlightObject;

        private int _myIndex;

        public void Init(string folderName, int index) {
            buttonText.text = folderName;
            _myIndex = index;
        }

        private void OnEnable() {
            button.onClick.AddListener(OnClick);
            button.image.sprite = transform.GetSiblingIndex() % 2 == 0 ? variation1 : variation2;
        }

        private void OnDisable() {
            button.onClick.RemoveListener(OnClick);
        }
        public void OnClick() {
            AndroidPlayer.Instance.uiController.PlayThisVideo(buttonText.text, _myIndex);
        }
        public void HighLightButton() {
            highlightObject.enabled = true;
        }
        public void DeHighLightButton() {
            highlightObject.enabled = false;
        }
    }
}