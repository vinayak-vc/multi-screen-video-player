using UnityEngine;
using UnityEngine.EventSystems;
namespace ViitorCloud.MultiScreenVideoPlayer {
    public class SliderPointerEvents : MonoBehaviour , IPointerDownHandler , IPointerUpHandler {

        public void OnPointerDown(PointerEventData eventData) {
            BootstrapManager.Instance.uiController.OnSliderPointerDown();
        }
        public void OnPointerUp(PointerEventData eventData) {
            BootstrapManager.Instance.uiController.OnSliderPointerUp();
        }
    }
}