using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ViitorCloud.Utility.PopupManager;

using static Modules.Utility.Utility;

namespace ViitorCloud.MultiScreenVideoPlayer {
    public class UIController : MonoBehaviour {
        private bool _isPlaying;
        [SerializeField] private Slider progressSlider;

        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Button unmuteButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button setPlaybackSpeedButton;
        [SerializeField] private Button secondsPrev;
        [SerializeField] private Button secondsNext;
        [SerializeField] private Button ipButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Toggle loopToggle;

        [SerializeField] private TextMeshProUGUI sliderText;
        [SerializeField] private TextMeshProUGUI setPlaybackSpeedButtonText;

        [SerializeField] private GameObject ipPanel;
        [SerializeField] private GameObject autoConnectPanel;
        [SerializeField] private TMP_InputField ipText;
        [SerializeField] private TextMeshProUGUI ipErrorText;
        [SerializeField] private TextMeshProUGUI autoConnectText;

        [SerializeField] private ButtonController buttonControllerPrefab;
        [SerializeField] private Transform buttonControllerTransform;
        private List<ButtonController> _buttonControllerList = new List<ButtonController>();

        private readonly float[] _playbackSpeeds = {
            1f, 1.5f, 2f, 2.5f, 3f
        };

        private int _playbackSpeedIndex;
        private bool _isDraggingSlider = false;

        private void OnEnable() {
            loopToggle.onValueChanged.AddListener(OnLoopToggleValueChanged);
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
            refreshButton.onClick.AddListener(RefreshButtonClickEvent);
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
            refreshButton.onClick.RemoveListener(RefreshButtonClickEvent);
            BootstrapManager.OnClientDisconnected -= OnClientDisconnected;
            BootstrapManager.OnClientConnected -= SuccessCallBack;
            loopToggle.onValueChanged.RemoveListener(OnLoopToggleValueChanged);
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
#if UNITY_EDITOR || UNITY_STANDALONE
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
#endif
        private void OnPlayButtonClicked() {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            BootstrapManager.Instance.networkObject?.SendCommandToServer(Commands.Play);
        }

        private void OnPauseButtonClicked() {
            pauseButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
            BootstrapManager.Instance.networkObject?.SendCommandToServer(Commands.Pause);
        }

        private void OnStopButtonClicked() {
            BootstrapManager.Instance.networkObject?.SendCommandToServer(Commands.Stop);
        }

        private void OnToggleMuteClicked() {
            muteButton.gameObject.SetActive(!muteButton.gameObject.activeSelf);
            unmuteButton.gameObject.SetActive(!unmuteButton.gameObject.activeSelf);
            BootstrapManager.Instance.networkObject?.SendCommandToServer(Commands.ToggleMute);
        }

        private void OnRestartButtonClicked() {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            BootstrapManager.Instance.networkObject?.SendCommandToServer(Commands.Restart);
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

            if (_playbackSpeedIndex == _playbackSpeeds.Length) {
                _playbackSpeedIndex = 0;
            } else {
                _playbackSpeedIndex += 1;
            }
            setPlaybackSpeedButtonText.text = $"x{_playbackSpeeds[_playbackSpeedIndex]}";

            BootstrapManager.Instance.networkObject?.SendCommandToServer($"{Commands.SetPlaybackSpeed}{Commands.Separator}{_playbackSpeeds[_playbackSpeedIndex]}");
        }

        // Called from WindowsPlayer when UpdateProgressClientRpc runs on this client
        public void SetProgress(double currentTime, double length) {
            if (!_isDraggingSlider) {
                progressSlider.maxValue = (float)length;
                progressSlider.value = (float)currentTime;
                sliderText.text = $"{ConvertSecondsToMinutes((int)progressSlider.value)} / {ConvertSecondsToMinutes((int)progressSlider.maxValue)}";
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
            BootstrapManager.Instance.networkObject?.SendCommandToServer($"{Commands.Seek}{Commands.Separator}{time}");
        }

        public void SetVideoName(VideoContainerList vName, string ind) {

            if (vName?.videoContainerList == null || vName.videoContainerList.Count == 0) {
                PopupManager.Instance.ShowToast("No Folders Found, Please Add New Folder from the Computer and Restart Application.");
                return;
            }
            
            for (int i = 0; i < vName.videoContainerList.Count; i++) {
                VideoContainer videoPlayerController = vName.videoContainerList[i];
                ButtonController buttonController = Instantiate(buttonControllerPrefab, buttonControllerTransform);
                buttonController.Init(videoPlayerController.folderName, i);
                _buttonControllerList.Add(buttonController);
            }
            try {
                int index = int.Parse(ind);
                if (index >= 0 && index < vName.videoContainerList.Count) {
                    _buttonControllerList[index].HighLightButton();
                }
            } catch (Exception e) {
                LogError(e);
            }
        }

        public void PlayThisVideo(string folderName, int index) {
            BootstrapManager.Instance.networkObject?.SendCommandToServer($"{Commands.PlayThisVideo}{Commands.Separator}{folderName}");
            OnRestartButtonClicked();
            HighLightThisButton(index.ToString());
        }

        private void RefreshButtonClickEvent() {
            BootstrapManager.Instance.DisconnectClient();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnLoopToggleValueChanged(bool arg0) {
            BootstrapManager.Instance.networkObject?.SendCommandToServer($"{Commands.Loop}{Commands.Separator}{arg0}");
        }
        public void HighLightThisButton(string s) {
            int index = int.Parse(s);
            for (int i = 0; i < _buttonControllerList.Count; i++) {
                if (index == i) {
                    _buttonControllerList[i].HighLightButton();
                } else {
                    _buttonControllerList[i].DeHighLightButton();
                }
            }
        }
    }
}