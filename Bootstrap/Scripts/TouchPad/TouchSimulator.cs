using System;

using UnityEngine;

namespace ViitorCloud.MobileTouchPad {
    public class TouchDeltaInput : MonoBehaviour {
        [SerializeField] private RectTransform touchPanel;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Vector2 resolution = new(1920, 1080);

        public static Action OnDown;
        public static Action<Vector2> OnDelta;
        public static Action<Vector2> SendTouchData;
        public static Action OnUp;

        private bool _isTouching;
        private Vector2 _previousLocal;

        private Rect _rect;
        private float _width;
        private float _height;

        private void Awake() {
            _rect = touchPanel.rect;
            _width = _rect.width;
            _height = _rect.height;
        }

        private void Update() {
            if (!TryGetInput(out Vector2 screenPos, out InputState state))
                return;

            switch (state) {
                case InputState.Down:
                    Begin(screenPos);
                    break;

                case InputState.Hold:
                    Move(screenPos);
                    break;

                case InputState.Up:
                    End();
                    break;
            }
        }

        private void Begin(Vector2 screenPos) {
            if (!TryGetLocal(screenPos, out _previousLocal))
                return;

            _isTouching = true;
            OnDown?.Invoke();

            // 🔹 Send initial absolute position
            SendAbsolute(_previousLocal);
        }

        private void Move(Vector2 screenPos) {
            if (!_isTouching)
                return;

            if (!TryGetLocal(screenPos, out Vector2 currentLocal))
                return;

            Vector2 delta = currentLocal - _previousLocal;

            delta.x /= _width;
            delta.y /= _height;

            OnDelta?.Invoke(delta);

            SendAbsolute(currentLocal);

            _previousLocal = currentLocal;
        }

        private void End() {
            if (!_isTouching)
                return;

            _isTouching = false;
            OnUp?.Invoke();
        }

        private void SendAbsolute(Vector2 local) {
            float normalizedX = (local.x + _width * 0.5f) / _width;
            float normalizedY = (local.y + _height * 0.5f) / _height;

            Vector2 mapped;
            mapped.x = normalizedX * resolution.x;
            mapped.y = normalizedY * resolution.y;

            SendTouchData?.Invoke(mapped);
        }

        private bool TryGetLocal(Vector2 screenPos, out Vector2 local) {
            if (!RectTransformUtility.RectangleContainsScreenPoint(touchPanel, screenPos, null)) {
                local = default(Vector2);
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchPanel, screenPos, null, out local);
        }

        private bool TryGetInput(out Vector2 pos, out InputState state) {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) {
                pos = Input.mousePosition;
                state = InputState.Down;
                return true;
            }

            if (Input.GetMouseButton(0)) {
                pos = Input.mousePosition;
                state = InputState.Hold;
                return true;
            }

            if (Input.GetMouseButtonUp(0)) {
                pos = Input.mousePosition;
                state = InputState.Up;
                return true;
            }
#else
            if (Input.touchCount > 0) {
                Touch touch = Input.GetTouch(0);
                pos = touch.position;

                state = touch.phase switch {
                    TouchPhase.Began => InputState.Down,
                    TouchPhase.Moved => InputState.Hold,
                    TouchPhase.Stationary => InputState.Hold,
                    TouchPhase.Ended => InputState.Up,
                    TouchPhase.Canceled => InputState.Up,
                    _ => InputState.None
                };

                return state != InputState.None;
            }
#endif
            pos = default(Vector2);
            state = InputState.None;
            return false;
        }

        private enum InputState {
            None,
            Down,
            Hold,
            Up
        }
    }
}