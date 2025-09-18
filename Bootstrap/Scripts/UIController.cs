using System;
using System.Collections;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class UIController : MonoBehaviour {
        private bool _isPlaying;
        public Slider progressSlider;

        private bool _isDraggingSlider = false;

        public Button playButton;
        public Button pauseButton;
        public Button stopButton;
        public Button muteButton;
        public Button unmuteButton;
        public Button restartButton;
        public Button setPlaybackSpeedButton;
        public Button secondsPrev;
        public Button secondsNext;
        public Button ipButton;

        public TextMeshProUGUI sliderText;
        public TextMeshProUGUI setPlaybackSpeedButtonText;
        public TextMeshProUGUI videoName;

        public GameObject ipPanel;
        public GameObject autoConnectPanel;
        public TMP_InputField ipText;
        public TextMeshProUGUI ipErrorText;
        public TextMeshProUGUI autoConnectText;

        private readonly float[] _playbackspeeds = {
            1f, 1.5f, 2f, 2.5f, 3f
        };

        private int _playbackSpeedIndex;

        private void OnEnable() {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            //stopButton.onClick.AddListener(OnStopButtonClicked);
            muteButton.onClick.AddListener(OnToggleMuteClicked);
            unmuteButton.onClick.AddListener(OnToggleMuteClicked);
            restartButton.onClick.AddListener(OnRestartButtonClicked);
            secondsPrev.onClick.AddListener(Seek15SecPrev);
            secondsNext.onClick.AddListener(Seek15SecNext);
            setPlaybackSpeedButton.onClick.AddListener(OnSetPlaybackSpeedClicked);
            ipButton.onClick.AddListener(IpButtonClickEvent);
            BootstrapManager.OnClientConnected += SuccessCallBack;
            BootstrapManager.OnClientDisconnected += OnClientDisconnected;
        }
        

        private void OnDisable() {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
            pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            //stopButton.onClick.RemoveListener(OnStopButtonClicked);
            muteButton.onClick.RemoveListener(OnToggleMuteClicked);
            unmuteButton.onClick.RemoveListener(OnToggleMuteClicked);
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            secondsPrev.onClick.RemoveListener(Seek15SecPrev);
            secondsNext.onClick.RemoveListener(Seek15SecNext);
            setPlaybackSpeedButton.onClick.RemoveListener(OnSetPlaybackSpeedClicked);
            ipButton.onClick.RemoveListener(IpButtonClickEvent);
            BootstrapManager.OnClientDisconnected -= OnClientDisconnected;
            BootstrapManager.OnClientConnected -= SuccessCallBack;
        }

        private void Start() {
            if (PlayerPrefs.HasKey("ip")) {
                ipText.text = PlayerPrefs.GetString("ip", "");
                IpButtonClickEvent();
            } else {
                ipPanel.SetActive(true);
            }
        }

        private void IpButtonClickEvent() {
            if (!string.IsNullOrEmpty(ipText.text)) {
                autoConnectText.text = "Connecting...";
                autoConnectPanel.SetActive(true);
                BootstrapManager.Instance.Connect(ipText.text);
            } else {
                ipErrorText.text = "IP text can not be empty";
                autoConnectPanel.SetActive(false);
            }
        }

        private void SuccessCallBack(bool connected) {
            if (!connected) {
                ipErrorText.text = "Cannot connect to the server. Enter correct IP address.";
                autoConnectPanel.SetActive(false);
            } else {
                PlayerPrefs.SetString("ip", ipText.text);
                StartCoroutine(ConnectionSuccessful());
            }
        }
        
        private IEnumerator ConnectionSuccessful() {
            yield return new WaitForSeconds(2f);
            autoConnectText.text = "Connected";
            yield return new WaitForSeconds(1f);
            ipPanel.SetActive(false);
            autoConnectPanel.SetActive(false);
        }
        
        private void OnClientDisconnected() {
            ipPanel.SetActive(true);
            autoConnectPanel.SetActive(false);
            ipErrorText.text = "Disconnected";
        }


        public void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                _isPlaying = !_isPlaying;
                if (_isPlaying) {
                    OnPauseButtonClicked();
                } else {
                    OnPlayButtonClicked();
                }
            }

            if (Input.GetKeyDown(KeyCode.A)) {
                OnStopButtonClicked();
            }

            // Example keys for new commands
            if (Input.GetKeyDown(KeyCode.M)) {
                OnToggleMuteClicked();
            }

            if (Input.GetKeyDown(KeyCode.R)) {
                OnRestartButtonClicked();
            }

            if (Input.GetKeyDown(KeyCode.Plus)) {
                // Example: Set playback speed to 1.5x
                OnSetPlaybackSpeedClicked();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                Seek15SecPrev();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                Seek15SecNext();
            }

        }

        private void OnPlayButtonClicked() {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.Play);
        }

        private void OnPauseButtonClicked() {
            pauseButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.Pause);
        }

        private void OnStopButtonClicked() {
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.Stop);
        }

        public void OnSeekButtonClicked(float time) {
            BootstrapManager.Instance.networkObject.SendCommandToServer($"{Commands.Seek}{Commands.Separator}{time}");
        }

        private void OnToggleMuteClicked() {
            muteButton.gameObject.SetActive(!muteButton.gameObject.activeSelf);
            unmuteButton.gameObject.SetActive(!unmuteButton.gameObject.activeSelf);
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.ToggleMute);
        }

        private void OnRestartButtonClicked() {
            BootstrapManager.Instance.networkObject.SendCommandToServer(Commands.Restart);
        }

        private void Seek15SecNext() {
            StartCoroutine(Enumrator());
            return;

            IEnumerator Enumrator() {
                if (progressSlider.value + 15 < progressSlider.maxValue) {
                    OnSliderPointerDown();
                    yield return new WaitForEndOfFrame();
                    progressSlider.value += 15;
                    yield return new WaitForEndOfFrame();
                    OnSliderPointerUp();
                }
            }
        }

        private void Seek15SecPrev() {
            StartCoroutine(Enumrator());
            return;

            IEnumerator Enumrator() {
                OnSliderPointerDown();
                yield return new WaitForEndOfFrame();
                if (progressSlider.value - 15 > 0) {
                    progressSlider.value -= 15;
                } else {
                    progressSlider.value = 0;
                }
                yield return new WaitForEndOfFrame();
                OnSliderPointerUp(); // User released slider
            }
        }


        private void OnSetPlaybackSpeedClicked() {

            if (_playbackSpeedIndex == _playbackspeeds.Length) {
                _playbackSpeedIndex = 0;
            } else {
                _playbackSpeedIndex += 1;
            }
            setPlaybackSpeedButtonText.text = $"x{_playbackspeeds[_playbackSpeedIndex]}";

            BootstrapManager.Instance.networkObject.SendCommandToServer($"{Commands.SetPlaybackSpeed}{Commands.Separator}{_playbackspeeds[_playbackSpeedIndex]}");
        }

        // Called from WindowsPlayer when UpdateProgressClientRpc runs on this client
        public void SetProgress(double currentTime, double length) {
            if (!_isDraggingSlider) {
                progressSlider.maxValue = (float)length;
                progressSlider.value = (float)currentTime;
                sliderText.text = $"{(int)progressSlider.value} / {(int)progressSlider.maxValue}";
            }
        }

        public void OnSliderPointerDown() {
            _isDraggingSlider = true; // User started dragging slider
        }

        public void OnSliderPointerUp() {
            SendSeekCommand(progressSlider.value);
            _isDraggingSlider = false; // User released slider
        }

        private void SendSeekCommand(float time) {
            BootstrapManager.Instance.networkObject.SendCommandToServer($"{Commands.Seek}{Commands.Separator}{time}");
        }

        public void SetVideoName(string vName) {
            videoName.text = vName;
        }
    }
}