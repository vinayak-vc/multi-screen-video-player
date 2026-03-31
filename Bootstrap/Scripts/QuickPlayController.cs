using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ViitorCloud.MultiScreenVideoPlayer
{
    /// <summary>
    /// Self-contained controller for the QuickPlay scene.
    ///
    /// Keyboard / Gamepad controls:
    ///   SPACE / A (South)  — play first video (Idle), pause/resume (Playing),
    ///                        confirm selected folder (Picking)
    ///   ESC   / B (East)   — open picker (Idle/Playing), close picker (Picking)
    ///   Left-Stick / D-Pad — navigate folder rows while picker is open
    ///
    /// Folder data mirrors WindowsUIController:
    ///   - StreamingAssets sub-folders scanned on first run
    ///   - Persisted to quickplay_videoContainerList.json in persistentDataPath
    ///   - FolderObjects prefab rows with Play + Delete buttons
    ///   - Add Folder button opens FileExplorer dialog
    /// </summary>
    public class QuickPlayController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private UnityEngine.MeshRenderer videoPlayer360Renderer;
        [SerializeField] private QuickPlayUIController ui;

        [Header("Folder List")]
        [SerializeField] private FolderObjects folderObjectPrefab;
        [SerializeField] private Transform folderListParent;
        [SerializeField] private Button addFolderButton;

        [Header("Gamepad Navigation")]
        [Tooltip("Seconds between repeated navigation steps when joystick is held.")]
        [SerializeField] private float navigateRepeatDelay = 0.12f;

        // ── State ─────────────────────────────────────────────────────────────

        private enum State { Idle, Playing, Picking }
        private State _state = State.Idle;
        private string _currentVideoPath;

        // ── Folder data ───────────────────────────────────────────────────────

        private VideoContainerList _videoContainerList;
        private readonly Dictionary<string, FolderObjects> _folderObjectMap = new();
        private readonly List<FolderObjects> _folderObjectList = new();   // ordered for D-pad nav
        private string _jsonPath;

        // ── Gamepad navigation state ──────────────────────────────────────────

        private int _selectedIndex = -1;          // -1 = nothing selected
        private float _navCooldown;               // time until next repeated nav step

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();

            if (videoPlayer == null)
            {
                Debug.LogError("[QuickPlayController] No VideoPlayer assigned.");
                enabled = false;
                return;
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            // 360-degree video setup — RenderTexture → sphere MeshRenderer
            UnityEngine.RenderTexture rt360 = new UnityEngine.RenderTexture(2048, 1024, 24);
            rt360.Create();
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = rt360;
            if (videoPlayer360Renderer != null)
                videoPlayer360Renderer.material.SetTexture("_BaseMap", rt360);
            videoPlayer.loopPointReached += OnLoopPointReached;
        }

        private async void Start()
        {
            _jsonPath = Path.Combine(Application.persistentDataPath, "quickplay_videoContainerList.json");
            await LoadOrInitVideoList();
            ScanStreamingAssets();

            foreach (VideoContainer vc in _videoContainerList.videoContainerList)
                SpawnFolderObject(vc);

            ui.ShowIdle(_videoContainerList.videoContainerList.Count);
            _state = State.Idle;

            if (addFolderButton != null)
                addFolderButton.onClick.AddListener(OnAddFolderClicked);
        }

        private void Update()
        {
            HandleKeyboard();
            HandleGamepad();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached -= OnLoopPointReached;
            if (addFolderButton != null)
                addFolderButton.onClick.RemoveListener(OnAddFolderClicked);
        }

        // ── Keyboard input ────────────────────────────────────────────────────

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                HandlePrimary();
            else if (Input.GetKeyDown(KeyCode.Escape))
                HandleSecondary();
            else if (_state == State.Picking)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow)) NavigateFolders(1);
                else if (Input.GetKeyDown(KeyCode.UpArrow)) NavigateFolders(-1);
            }
        }

        // ── Gamepad input ─────────────────────────────────────────────────────

        private void HandleGamepad()
        {
            Gamepad gp = Gamepad.current;
            if (gp == null) return;

            // A (South) — primary action
            if (gp.buttonSouth.wasPressedThisFrame)
                HandlePrimary();

            // B (East) — secondary / menu
            if (gp.buttonEast.wasPressedThisFrame)
                HandleSecondary();

            // Y (West) — open Add Folder dialog while in picker
            if (gp.buttonWest.wasPressedThisFrame && _state == State.Picking)
                OnAddFolderClicked();

            // X (North) — delete selected folder with confirmation while in picker
            if (gp.buttonNorth.wasPressedThisFrame && _state == State.Picking && _selectedIndex >= 0)
                ConfirmDeleteFolder();

            // D-Pad / Left-Stick navigation while picker is open
            if (_state == State.Picking)
            {
                _navCooldown -= Time.deltaTime;

                bool downPressed  = gp.dpad.down.wasPressedThisFrame  || gp.leftStick.down.wasPressedThisFrame;
                bool upPressed    = gp.dpad.up.wasPressedThisFrame    || gp.leftStick.up.wasPressedThisFrame;

                float stickY = gp.leftStick.ReadValue().y;
                bool stickHeld = Mathf.Abs(stickY) > 0.5f;

                if (downPressed || upPressed)
                {
                    int dir = downPressed ? 1 : -1;
                    NavigateFolders(dir);
                    _navCooldown = navigateRepeatDelay * 3f; // longer first delay
                }
                else if (stickHeld && _navCooldown <= 0f)
                {
                    NavigateFolders(stickY < 0 ? 1 : -1);
                    _navCooldown = navigateRepeatDelay;
                }
                else if (!stickHeld)
                {
                    _navCooldown = 0f;
                }
            }
        }

        // ── Primary / Secondary actions ───────────────────────────────────────

        /// <summary>A button / Space — context-sensitive primary action.</summary>
        private void HandlePrimary()
        {
            switch (_state)
            {
                case State.Idle:
                    PlayFirstAvailable();
                    break;
                case State.Playing:
                    TogglePauseResume();
                    break;
                case State.Picking:
                    ConfirmSelectedFolder();
                    break;
            }
        }

        /// <summary>B button / Escape — toggle picker menu.</summary>
        private void HandleSecondary()
        {
            switch (_state)
            {
                case State.Playing:
                case State.Idle:
                    StopVideo();
                    OpenPicker();
                    break;
                case State.Picking:
                    ClosePicker();
                    break;
            }
        }

        private void OpenPicker()
        {
            ResetFolderSelection();
            ui.ShowPicker(_videoContainerList.videoContainerList.Count);
            _state = State.Picking;

            // Auto-select first item for gamepad users
            if (_folderObjectList.Count > 0)
                NavigateFolders(0);
        }

        private void ClosePicker()
        {
            ResetFolderSelection();
            ui.ShowIdle(_videoContainerList.videoContainerList.Count);
            _state = State.Idle;
        }

        // ── Gamepad folder navigation ─────────────────────────────────────────

        /// <summary>Move selection by <paramref name="delta"/> rows (0 = re-select current).</summary>
        private void NavigateFolders(int delta)
        {
            if (_folderObjectList.Count == 0) return;

            // Deselect old
            if (_selectedIndex >= 0 && _selectedIndex < _folderObjectList.Count)
                _folderObjectList[_selectedIndex].DeHighLightButton();

            _selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, _folderObjectList.Count - 1);

            // Highlight new
            _folderObjectList[_selectedIndex].HighLightButton();
        }

        private void ConfirmSelectedFolder()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _folderObjectList.Count) return;
            VideoContainer vc = _folderObjectList[_selectedIndex]._videoContainer;
            ResetFolderSelection();
            OnFolderPlay(vc);
        }

        private void ResetFolderSelection()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _folderObjectList.Count)
                _folderObjectList[_selectedIndex].DeHighLightButton();
            _selectedIndex = -1;
        }

        // ── FolderObjects callbacks ───────────────────────────────────────────

        private void OnFolderPlay(VideoContainer vc)
        {
            if (vc.videoPath == null || vc.videoPath.Length == 0)
            {
                ui.ShowError("No videos in folder: " + vc.folderName);
                return;
            }
            PlayVideo(vc.videoPath[0]);
        }

        private void OnFolderDelete(VideoContainer vc)
        {
            int idx = _folderObjectList.FindIndex(f => f._videoContainer.folderName == vc.folderName);
            if (idx >= 0) _folderObjectList.RemoveAt(idx);

            if (_folderObjectMap.TryGetValue(vc.folderName, out FolderObjects go))
            {
                Destroy(go.gameObject);
                _folderObjectMap.Remove(vc.folderName);
            }

            _videoContainerList.videoContainerList.Remove(vc);
            _ = WriteJsonAsync();

            // Fix selection index after removal
            if (_selectedIndex >= _folderObjectList.Count)
                _selectedIndex = _folderObjectList.Count - 1;

            if (_state == State.Picking)
                ui.ShowPicker(_videoContainerList.videoContainerList.Count);
            else
                ui.ShowIdle(_videoContainerList.videoContainerList.Count);
        }

        // ── Delete Folder (gamepad X) ─────────────────────────────────────────

        private void ConfirmDeleteFolder()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _folderObjectList.Count) return;
            VideoContainer vc = _folderObjectList[_selectedIndex]._videoContainer;
            var yesProps = new ViitorCloud.Utility.PopupManager.ButtonProperties
            {
                ButtonName = "Yes",
                ButtonAction = () => OnFolderDelete(vc)
            };
            var noProps = new ViitorCloud.Utility.PopupManager.ButtonProperties
            {
                ButtonName = "No",
                ButtonAction = null
            };
            ViitorCloud.Utility.PopupManager.PopupManager.Instance.ShowPopup(
                "Delete folder '" + vc.folderName + "' from the list?",
                ViitorCloud.Utility.PopupManager.MessageType.Warning,
                ViitorCloud.Utility.PopupManager.PopupType.TwoButton,
                yesProps,
                noProps
            );
        }

        // ── Add Folder ────────────────────────────────────────────────────────

        private async void OnAddFolderClicked()
        {
            string lastDir = PlayerPrefs.GetString("quickplay_dir", Application.streamingAssetsPath);
            string dir = FileExplorer.OpenFolder(lastDir);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            PlayerPrefs.SetString("quickplay_dir", dir);

            string folderName = Path.GetFileName(dir);
            if (_videoContainerList.videoContainerList.Any(x => x.folderName == folderName))
            {
                Debug.LogWarning("[QuickPlayController] Folder already added: " + folderName);
                return;
            }

            string[] files  = Directory.GetFiles(dir).OrderBy(Path.GetFileName).ToArray();
            string[] videos = files.Where(f => WindowsPlayer.VideoExtensions
                .Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();

            if (videos.Length == 0)
            {
                Debug.LogWarning("[QuickPlayController] No videos found in: " + dir);
                return;
            }

            string audio = files.FirstOrDefault(f => WindowsPlayer.AudioExtensions
                .Contains(Path.GetExtension(f).ToLowerInvariant()));

            VideoContainer vc = new VideoContainer
            {
                folderPath = dir,
                folderName = folderName,
                videoPath  = videos,
                audioPath  = audio ?? string.Empty,
            };

            _videoContainerList.videoContainerList.Add(vc);
            SpawnFolderObject(vc);
            await WriteJsonAsync();

            if (_state == State.Picking)
                ui.ShowPicker(_videoContainerList.videoContainerList.Count);
        }

        // ── Video playback ────────────────────────────────────────────────────

        private void PlayFirstAvailable()
        {
            if (_videoContainerList.videoContainerList.Count == 0)
            {
                ui.ShowError("No folders added yet. Press B / ESC to browse.");
                return;
            }
            OnFolderPlay(_videoContainerList.videoContainerList[0]);
        }

        private void PlayVideo(string path)
        {
            if (!File.Exists(path))
            {
                ui.ShowError("File not found: " + Path.GetFileName(path));
                return;
            }
            _currentVideoPath = path;
            videoPlayer.url   = "file://" + path;
            videoPlayer.Prepare();
            StartCoroutine(WaitAndPlay());
        }

        private IEnumerator WaitAndPlay()
        {
            ui.ShowLoading(Path.GetFileNameWithoutExtension(_currentVideoPath));
            float elapsed = 0f;
            const float timeout = 15f;

            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!videoPlayer.isPrepared)
            {
                ui.ShowError("Failed to load: " + Path.GetFileName(_currentVideoPath));
                _state = State.Idle;
                yield break;
            }

            videoPlayer.Play();
            ui.ShowPlaying(Path.GetFileNameWithoutExtension(_currentVideoPath));
            _state = State.Playing;
        }

        private void StopVideo()
        {
            StopAllCoroutines();
            if (videoPlayer != null && (videoPlayer.isPlaying || videoPlayer.isPrepared))
                videoPlayer.Stop();
        }

        private void TogglePauseResume()
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
            else
                videoPlayer.Play();
        }

        private void OnLoopPointReached(VideoPlayer vp)
        {
            ui.ShowIdle(_videoContainerList.videoContainerList.Count);
            _state = State.Idle;
        }

        // ── StreamingAssets scan ──────────────────────────────────────────────

        private void ScanStreamingAssets()
        {
            string root = Application.streamingAssetsPath;
            if (!Directory.Exists(root)) return;

            bool changed = false;
            foreach (string folder in Directory.GetDirectories(root))
            {
                string folderName = Path.GetFileName(folder);
                if (_videoContainerList.videoContainerList.Any(x => x.folderName == folderName))
                    continue;

                string[] files  = Directory.GetFiles(folder).OrderBy(Path.GetFileName).ToArray();
                string[] videos = files.Where(f => WindowsPlayer.VideoExtensions
                    .Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();

                if (videos.Length == 0) continue;

                string audio = files.FirstOrDefault(f => WindowsPlayer.AudioExtensions
                    .Contains(Path.GetExtension(f).ToLowerInvariant()));

                _videoContainerList.videoContainerList.Add(new VideoContainer
                {
                    folderPath = folder,
                    folderName = folderName,
                    videoPath  = videos,
                    audioPath  = audio ?? string.Empty,
                });
                changed = true;
            }

            if (changed) _ = WriteJsonAsync();
        }

        private void SpawnFolderObject(VideoContainer vc)
        {
            if (folderObjectPrefab == null || folderListParent == null) return;
            FolderObjects obj = Instantiate(folderObjectPrefab, folderListParent)
                .Init(vc, OnFolderPlay, OnFolderDelete);
            _folderObjectMap[vc.folderName] = obj;
            _folderObjectList.Add(obj);
        }

        // ── JSON persistence ──────────────────────────────────────────────────

        private async Task LoadOrInitVideoList()
        {
            if (File.Exists(_jsonPath))
            {
                string json = await File.ReadAllTextAsync(_jsonPath);
                _videoContainerList = JsonUtility.FromJson<VideoContainerList>(json)
                    ?? new VideoContainerList { videoContainerList = new List<VideoContainer>() };
            }
            else
            {
                _videoContainerList = new VideoContainerList { videoContainerList = new List<VideoContainer>() };
                await File.WriteAllTextAsync(_jsonPath, JsonUtility.ToJson(_videoContainerList));
            }
        }

        private async Task WriteJsonAsync()
        {
            await File.WriteAllTextAsync(_jsonPath, JsonUtility.ToJson(_videoContainerList));
        }
    }
}
