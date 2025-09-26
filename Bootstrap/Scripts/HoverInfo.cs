using TMPro;

using static Modules.Utility.Utility;

using UnityEngine;
using UnityEngine.EventSystems;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class HoverInfo : MonoBehaviour {
        public Animation hoverPanel; // Reference to the panel GameObject
        public TextMeshProUGUI hoverText; // Reference to the Text component inside the panel
        [SerializeField] private CanvasGroup canvasGroup;

        public static HoverInfo Instance;
        private RectTransform _rectTransform;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Destroy(gameObject);
            }

            _rectTransform = hoverPanel.GetComponent<RectTransform>();
        }

        public void Show(string message) {
            if (hoverPanel != null) {
                if (canvasGroup.alpha < 0.5f) {
                    AnimationReversal(this, hoverPanel, false, 0, 0, false, false, true);
                }
                if (hoverText != null)
                    hoverText.text = message;
            }   
            Vector2 mousePosition = Input.mousePosition;
            mousePosition.x += 10;
            _rectTransform.anchoredPosition = mousePosition;
        }

        public void Hide() {
            if (hoverPanel != null) {
                AnimationReversal(this, hoverPanel, true, 0, 0, false, false);
            }
        }
    }
}