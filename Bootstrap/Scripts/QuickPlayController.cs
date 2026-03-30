using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ViitorCloud.MultiScreenVideoPlayer
{
    /// <summary>
    /// Self-contained controller for the QuickPlay scene.
    /// Mirrors WindowsUIController folder-based logic:
    ///   - Scans StreamingAssets sub-folders on first run
    ///   - Persists folder list to JSON in persistentDataPath
    ///   - Instantiates FolderObjects prefab rows (Play + Delete buttons)
    ///   - Add Folder button opens a folder-picker dialog
    ///
    /// No dependency on WindowsPlayer or Android networking.
    ///
    /// Keyboard shortcuts:
    ///   SPACE  — play first folder's first video when Idle
    ///   ESC    — stop video and show the picker list; ESC again → Idle
    /// </summary>
    public class QuickPlayController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private QuickPlayUIController ui;

        [Header("Folder List")]
        [SerializeField] private FolderObjects folderObjectPrefab;
        [SerializeField] private Transform folderListParent;
        [SerializeField] private Button addFolderButton;

        // ── State ─────────────────────────────────────────────────────────────

        private enum State { Idle, Playing, Picking }
        private State _state = State.Idle;
        private string _currentVideoPath;

        // ── Data ──────────────────────────────────────────────────────────────

        private VideoContainerList _videoContainerList;
        private readonly Dictionary<string, FolderObjects> _folderObjectMap = new();
        private string _jsonPath;

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
            if (Input.GetKeyDown(KeyCode.Space))
                HandleSpace();
            else if (Input.GetKeyDown(KeyCode.Escape))
                HandleEscape();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached -= OnLoopPointReached;

            if (addFolderButton != null)
                addFolderButton.onClick.RemoveListener(OnAddFolderClicked);
        }

        // ── Keyboard ──────────────────────────────────────────────────────────

        private void HandleSpace()
        {
            if (_state == State.Idle || _state == State.Picking)
                PlayFirstAvailable();
        }

        private void HandleEscape()
        {
            switch (_state)
            {
                case State.Playing:
                case State.Idle:
                    StopVideo();
                    ui.ShowPicker(_videoContainerList.videoContainerList.Count);
                    _state = State.Picking;
                    break;
                case State.Picking:
                    ui.ShowIdle(_videoContainerList.videoContainerList.Count);
                    _state = State.Idle;
                    break;
            }
        }

        // ── Folder list — FolderObjects callbacks ─────────────────────────────

        private void OnFolderPlay(VideoContainer vc)
        {
            if (vc.videoPath == null || vc.videoPath.Length == 0)
            {
                Debug.LogWarning("[QuickPlayController] Folder has no videos: " + vc.folderName);
                ui.ShowError("No videos in folder: " + vc.folderName);
                return;
            }
            PlayVideo(vc.videoPath[0]);
        }

        private void OnFolderDelete(VideoContainer vc)
        {
            _videoContainerList.videoContainerList.Remove(vc);
            if (_folderObjectMap.TryGetValue(vc.folderName, out FolderObjects go))
            {
                Destroy(go.gameObject);
                _folderObjectMap.Remove(vc.folderName);
            }
            _ = WriteJsonAsync();
            ui.ShowIdle(_videoContainerList.videoContainerList.Count);
            if (_state == State.Picking)
                ui.ShowPicker(_videoContainerList.videoContainerList.Count);
        }

        // ── Add folder button ─────────────────────────────────────────────────

        private async void OnAddFolderClicked()
        {
            string lastDir = PlayerPrefs.GetString("quickplay_dir", Application.streamingAssetsPath);
            string dir = FileExplorer.OpenFolder(lastDir);

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            PlayerPrefs.SetString("quickplay_dir", dir);

            string folderName = Path.GetFileName(dir);
            if (_videoContainerList.videoContainerList.Any(x => x.folderName == folderName))
            {
                Debug.LogWarning("[QuickPlayController] Folder already added: " + folderName);
                return;
            }

            string[] files = Directory.GetFiles(dir).OrderBy(Path.GetFileName).ToArray();
            string[] videos = files
                .Where(f => WindowsPlayer.VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            if (videos.Length == 0)
            {
                Debug.LogWarning("[QuickPlayController] No videos found in: " + dir);
                return;
            }

            string audio = files.FirstOrDefault(f =>
                WindowsPlayer.AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            VideoContainer vc = new VideoContainer
            {
                folderPath = dir,
                folderName = folderName,
                videoPath = videos,
                audioPath = audio ?? string.Empty,
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
                ui.ShowError("No folders added yet. Use Add Folder or press ESC to browse.");
                return;
            }
            VideoContainer first = _videoContainerList.videoContainerList[0];
            if (first.videoPath == null || first.videoPath.Length == 0)
            {
                ui.ShowError("First folder has no videos.");
                return;
            }
            PlayVideo(first.videoPath[0]);
        }

        private void PlayVideo(string path)
        {
            if (!File.Exists(path))
            {
                ui.ShowError("File not found: " + Path.GetFileName(path));
                return;
            }
            _currentVideoPath = path;
            videoPlayer.url = "file://" + path;
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

        private void OnLoopPointReached(VideoPlayer vp)
        {
            ui.ShowIdle(_videoContainerList.videoContainerList.Count);
            _state = State.Idle;
        }

        // ── Folder scan ───────────────────────────────────────────────────────

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

                string[] files = Directory.GetFiles(folder).OrderBy(Path.GetFileName).ToArray();
                string[] videos = files
                    .Where(f => WindowsPlayer.VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToArray();

                if (videos.Length == 0) continue;

                string audio = files.FirstOrDefault(f =>
                    WindowsPlayer.AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

                _videoContainerList.videoContainerList.Add(new VideoContainer
                {
                    folderPath = folder,
                    folderName = folderName,
                    videoPath = videos,
                    audioPath = audio ?? string.Empty,
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
