using System;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ViitorCloud.Utility.PopupManager;

using static Modules.Utility.Utility;


namespace ViitorCloud.MultiScreenVideoPlayer {
    public class MobileUIController : MonoBehaviour {
        private bool _isPlaying;

        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button pauseButton;

        [SerializeField]
        private Button stopButton;

        [SerializeField]
        private Button muteButton;

        [SerializeField]
        private Button unmuteButton;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button setPlaybackSpeedButton;

        [SerializeField]
        private Button secondsPrev;

        [SerializeField]
        private Button secondsNext;

        [SerializeField]
        private Button ipButton;

        [SerializeField]
        private Button refreshButton;

        [SerializeField]
        private Toggle loopToggle;

        [SerializeField]
        private TextMeshProUGUI sliderText;

        [SerializeField]
        private TextMeshProUGUI setPlaybackSpeedButtonText;

        [SerializeField]
        private GameObject ipPanel;

        [SerializeField]
        private GameObject autoConnectPanel;

        [SerializeField]
        private TMP_InputField ipText;

        [SerializeField]
        private TextMeshProUGUI ipErrorText;

        [SerializeField]
        private TextMeshProUGUI autoConnectText;

        [SerializeField]
        private ButtonController buttonControllerPrefab;

        [SerializeField]
        private Transform buttonControllerTransform;

        [SerializeField]
        private GridLayoutGroup gridLayoutGroup;

        [SerializeField]
        private TextMeshProUGUI currentPlayingVideoNameText;

        [SerializeField]
        private bool ignoreHighlightButtons;

        public static Action ConnectionSuccesfull;

        private readonly List<ButtonController> _buttonControllerList = new();

        private readonly float[] _playbackSpeeds = { 1f, 1.5f, 2f, 2.5f, 3f };

        internal bool isTheSameScene;

        private int _playbackSpeedIndex;
        private bool _isDraggingSlider;

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
            if (WindowsPlayer.Instance) {
                isTheSameScene = WindowsPlayer.Instance.isBothInSameScene;
            }

            if (!isTheSameScene) {
                if (PlayerPrefs.HasKey("ip")) {
                    ipText.text = PlayerPrefs.GetString("ip", "");
                    IpButtonClickEvent();
                } else {
                    ipPanel.SetActive(true);
                }
            }
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

        private void IpButtonClickEvent() {
            if (!string.IsNullOrEmpty(ipText.text)) {
                autoConnectText.text = "Connecting...";
                autoConnectPanel.SetActive(true);
                if (BootstrapManager.Instance) {
                    BootstrapManager.Instance.Connect(ipText.text);
                }
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
            AndroidPlayer.Instance.GetVideoName();
            ConnectionSuccesfull?.Invoke();
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

        private void OnPlayButtonClicked() {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            SendCommandToServer(Commands.Play);
        }

        private void OnPauseButtonClicked() {
            pauseButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
            SendCommandToServer(Commands.Pause);
        }

        private void OnStopButtonClicked() {
            SendCommandToServer(Commands.Stop);
        }

        private void OnToggleMuteClicked() {
            muteButton.gameObject.SetActive(!muteButton.gameObject.activeSelf);
            unmuteButton.gameObject.SetActive(!unmuteButton.gameObject.activeSelf);
            SendCommandToServer(Commands.ToggleMute);
        }

        private void OnRestartButtonClicked() {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
            SendCommandToServer(Commands.Restart);
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
            if (_playbackSpeedIndex == _playbackSpeeds.Length - 1) {
                _playbackSpeedIndex = 0;
            } else {
                _playbackSpeedIndex += 1;
            }
            setPlaybackSpeedButtonText.text = $"x{_playbackSpeeds[_playbackSpeedIndex % _playbackSpeeds.Length]}";

            SendCommandToServer($"{Commands.SetPlaybackSpeed}{Commands.Separator}{_playbackSpeeds[_playbackSpeedIndex]}");
        }

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
            SendCommandToServer($"{Commands.Seek}{Commands.Separator}{time}");
        }

        public void SetVideoName(VideoContainerList vName, string ind) {
            StartCoroutine(Enumerator());
            return;

            IEnumerator Enumerator() {
                if (vName?.videoContainerList == null || vName.videoContainerList.Count == 0) {
                    PopupManager.Instance.ShowToast("No Folders Found, Please Add New Folder from the Computer and Restart Application.");
                    yield break;
                }
                gridLayoutGroup.enabled = true;
                for (int i = 0; i < vName.videoContainerList.Count; i++) {
                    VideoContainer videoPlayerController = vName.videoContainerList[i];
                    ButtonController buttonController = Instantiate(buttonControllerPrefab, buttonControllerTransform);
                    buttonController.Init(videoPlayerController.folderName, i, ignoreHighlightButtons);
                    _buttonControllerList.Add(buttonController);
                }
                yield return new WaitForEndOfFrame();
                gridLayoutGroup.enabled = false;
                try {
                    // if (ind != null && int.TryParse(ind, out int indParsed) && indParsed < _buttonControllerList.Count) {
                    //     _buttonControllerList[indParsed].HighLightButton();
                    // }
                    HighLightThisButton(ind);
                } catch (Exception e) {
                    LogError(e);
                }
            }
        }

        public void PlayThisVideo(string folderName, int index) {
            SendCommandToServer($"{Commands.PlayThisVideo}{Commands.Separator}{folderName}");
            OnRestartButtonClicked();
            HighLightThisButton(index.ToString());
        }

        private void RefreshButtonClickEvent() {
            if (!isTheSameScene) {
                BootstrapManager.Instance.DisconnectClient();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene()
                .name);
        }

        public void OnLoopToggleValueChanged(bool arg0) {
            SendCommandToServer($"{Commands.Loop}{Commands.Separator}{arg0}");
        }

        internal void SendCommandToServer(string command) {
            if (!isTheSameScene) {
                BootstrapManager.Instance.networkObject?.SendCommandToServer(command);
            } else {
                WindowsPlayer.Instance.ExecuteCommand(command);
            }
        }

        public void HighLightThisButton(string s) {
            int index = int.Parse(s);
            for (int i = 0; i < _buttonControllerList.Count; i++) {
                if (index == i) {
                    _buttonControllerList[i].HighLightButton();
                    currentPlayingVideoNameText.text = _buttonControllerList[i].name;
                    currentPlayingVideoNameText.transform.DOPunchScale(Vector3.one * 1.2f, 0.5f, 10, 0.2f);
                } else {
                    _buttonControllerList[i].DeHighLightButton();
                }
            }
        }

        public void NewVideoAdded(string s) {
            StartCoroutine(Enumerator());
            return;

            IEnumerator Enumerator() {
                gridLayoutGroup.enabled = true;
                yield return new WaitForEndOfFrame();
                VideoContainer videoPlayerController = JsonUtility.FromJson<VideoContainer>(s);
                ;
                ButtonController buttonController = Instantiate(buttonControllerPrefab, buttonControllerTransform);
                buttonController.Init(videoPlayerController.folderName, _buttonControllerList.Count, ignoreHighlightButtons);
                _buttonControllerList.Add(buttonController);
                yield return new WaitForEndOfFrame();
                gridLayoutGroup.enabled = false;
            }
        }
    }
}
