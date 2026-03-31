#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

namespace ViitorCloud.MultiScreenVideoPlayer
{
    /// <summary>
    /// Editor utility — creates the QuickPlay scene hierarchy programmatically.
    /// Menu: Tools > QuickPlay > Build Scene
    /// </summary>
    public static class QuickPlaySceneBuilder
    {
        [MenuItem("Tools/QuickPlay/Build Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QuickPlay";

            // ── Root ──────────────────────────────────────────────────────────
            var root = new GameObject("QuickPlay");

            // ── Camera ────────────────────────────────────────────────────────
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            // ── 360 Sphere ────────────────────────────────────────────────────
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "VideoSphere360";
            sphere.transform.SetParent(root.transform);
            sphere.transform.localScale = new Vector3(-100f, 100f, 100f); // invert normals
            Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sphereRenderer.sharedMaterial = mat;

            // ── VideoPlayer ───────────────────────────────────────────────────
            var vpGo = new GameObject("VideoPlayer");
            vpGo.transform.SetParent(root.transform);
            var vp = vpGo.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.renderMode = VideoRenderMode.RenderTexture;

            // ── Canvas / UI ───────────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(root.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panels
            CreatePanel(canvasGo.transform, "IdlePanel");
            CreatePanel(canvasGo.transform, "PickerPanel");
            CreatePanel(canvasGo.transform, "PlayingPanel");
            CreatePanel(canvasGo.transform, "LoadingPanel");
            CreatePanel(canvasGo.transform, "ErrorPanel");

            // ── Controller ────────────────────────────────────────────────────
            var ctrlGo = new GameObject("QuickPlayController");
            ctrlGo.transform.SetParent(root.transform);

            EditorSceneManager.SaveScene(scene, "Assets/Games/multi-screen-video-player/Bootstrap/Scenes/QuickPlay.unity");
            Debug.Log("[QuickPlaySceneBuilder] QuickPlay scene built and saved.");
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.SetActive(false);
            return go;
        }
    }
}
#endif
