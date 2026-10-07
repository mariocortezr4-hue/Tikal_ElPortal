using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tests.Support;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class ThirdPersonReticleAlignmentValidation
    {
        private static int _checks;
        private static void Check(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("TPS reticle: " + message); _checks++; }
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [MenuItem("Tikal/Validation/Validate Third Person Reticle Alignment")]
        public static void Run()
        {
            _checks = 0;
            try
            {
                var source = File.ReadAllText("Assets/_Project/Code/UI/ReticlePresenter.cs");
                Check(!source.Contains("Physics.") && !source.Contains("Raycast"), "no second physics/raycast authority");
                foreach (var path in ThirdPersonCameraFoundationAuthoring.GameplayScenes.Take(2))
                {
                    var scene = EditorSceneManager.OpenPreviewScene(path);
                    try
                    {
                        var roots = scene.GetRootGameObjects();
                        var reticles = roots.SelectMany(g => g.GetComponentsInChildren<ReticlePresenter>(true)).ToArray();
                        Check(reticles.Length == 1, path + ": one reticle");
                        var reticle = reticles[0];
                        Check(reticle.AimSource != null && reticle.ViewCamera == reticle.AimSource.ViewCamera, "same resolved aim source and MainCamera");
                        Check(reticle.ViewCamera.CompareTag("MainCamera") && reticle.AimSource.HasThirdPersonConfiguration,
                            "authored MainCamera and stable InteractionOrigin");
                        Check(typeof(PlayerInteraction).GetProperty("ResolvedAimPoint").GetSetMethod() == null &&
                            typeof(PlayerInteraction).GetProperty("HasResolvedAimPoint").GetSetMethod() == null, "resolved data has no public setter");
                        Check(Mathf.Approximately(reticle.AimSource.InteractionDistance, 3), "unchanged 3m interaction reach");
                        var rect = (RectTransform)reticle.transform;
                        Check(rect.anchorMin == Vector2.one * .5f && rect.anchorMax == rect.anchorMin && rect.anchoredPosition == Vector2.zero,
                            "authored center anchors; runtime offset only");
                        var settings = new SerializedObject(reticle);
                        foreach (var field in new[] { "_settings", "_inputReader", "_dot", "_cross" })
                            Check(settings.FindProperty(field).objectReferenceValue != null, "critical reference " + field);
                        foreach (var transform in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
                            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no Missing Scripts: " + transform.name);
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                ProjectionChecks();
                if (Application.isPlaying) BehaviorChecks();
                Debug.Log("TIKAL_TPS_RETICLE_VALID checks=" + _checks + (Application.isPlaying ? " projection + physics" : " projection; run again in Play Mode for physics"));
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static void ProjectionChecks()
        {
            var cameraRoot = new GameObject("Temporary reticle projection camera", typeof(Camera));
            var canvasRoot = new GameObject("Temporary reticle projection Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var camera = cameraRoot.GetComponent<Camera>(); camera.enabled = false; camera.fieldOfView = 60;
                var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                var parent = (RectTransform)canvas.transform; parent.position = Vector3.forward * 10;
                var reticle = new GameObject("Reticle point", typeof(RectTransform)).GetComponent<RectTransform>();
                reticle.SetParent(parent, false); reticle.anchorMin = reticle.anchorMax = reticle.pivot = Vector2.one * .5f;
                foreach (var size in new[] { new Vector2(1920,1080), new Vector2(1280,720), new Vector2(1920,1200), new Vector2(1280,800) })
                {
                    camera.pixelRect = new Rect(Vector2.zero, size); camera.aspect = size.x / size.y;
                    // CanvasScaler MatchWidthOrHeight=.5 is the geometric mean of width/height scales.
                    var scaleFactor = Mathf.Sqrt(size.x / 1920 * size.y / 1080);
                    parent.sizeDelta = size / scaleFactor;
                    var worldPerCanvasUnit = 20 * Mathf.Tan(30 * Mathf.Deg2Rad) / parent.rect.height;
                    parent.localScale = Vector3.one * worldPerCanvasUnit;
                    var point = new Vector3(1.2f, .7f, 5);
                    Check(ReticlePresenter.TryGetScreenOffset(camera, reticle, point, out var offset), "project valid point at " + size);
                    var expected = new Vector2(point.x, point.y) * (10 / point.z / worldPerCanvasUnit);
                    Check(Vector2.Distance(offset, expected) < .02f, "independent perspective/CanvasScaler math at " + size);
                    reticle.anchoredPosition = offset;
                    Check(Vector2.Distance(camera.WorldToScreenPoint(reticle.position), camera.WorldToScreenPoint(point)) < .02f,
                        "reticle-to-world screen round trip at " + size);
                    Check(!ReticlePresenter.TryGetScreenOffset(camera, reticle, Vector3.back, out offset) && offset == Vector2.zero, "behind camera returns safe center");
                    Check(!ReticlePresenter.TryGetScreenOffset(camera, reticle, new Vector3(float.NaN,0,1), out offset) && offset == Vector2.zero,
                        "NaN rejected without invalid UI position");
                    Check(!ReticlePresenter.TryGetScreenOffset(camera, reticle, new Vector3(0,float.PositiveInfinity,1), out offset) && offset == Vector2.zero,
                        "infinity rejected");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(canvasRoot); UnityEngine.Object.DestroyImmediate(cameraRoot); }
        }

        private static void BehaviorChecks()
        {
            var cursorLock = Cursor.lockState; var cursorVisible = Cursor.visible; var volume = AudioListener.volume;
            var fixture = new GameObject("Temporary reticle physics checks"); fixture.transform.position = Vector3.one * 10000; fixture.SetActive(false);
            var ui = new GameObject("Temporary reticle UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); ui.SetActive(false);
            try
            {
                var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath), fixture.transform);
                player.transform.localPosition = Vector3.zero;
                var camera = player.GetComponentInChildren<Camera>(true); camera.enabled = false; camera.GetComponent<AudioListener>().enabled = false;
                fixture.SetActive(true);
                var reader = player.GetComponent<PlayerInputReader>();
                var actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
                actions.devices = Array.Empty<InputDevice>();
                var interaction = player.GetComponent<PlayerInteraction>(); var controller = player.GetComponent<PlayerCameraController>();
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Reticle.prefab"), ui.transform);
                var reticle = root.GetComponent<ReticlePresenter>(); var settings = root.GetComponent<ReticleSettings>();
                Set(reticle, "_inputReader", reader); Set(reticle, "_interaction", interaction); Set(reticle, "_viewCamera", camera);
                var reticleData = new SerializedObject(reticle);
                var dot = (GameObject)reticleData.FindProperty("_dot").objectReferenceValue;
                var cross = (GameObject)reticleData.FindProperty("_cross").objectReferenceValue;
                ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                ui.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                ui.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920,1080);
                ui.SetActive(true); Canvas.ForceUpdateCanvases();
                void Refresh() { Physics.SyncTransforms(); Invoke(interaction, "LateUpdate"); Invoke(reticle, "LateUpdate"); }
                void Aim(Vector3 position, Vector3 point) => camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(point-position));
                controller.ApplyCameraMode(CameraMode.FirstPerson); Refresh();
                Check(reticle.ScreenOffset == Vector2.zero, "FPS exact center");
                controller.ApplyCameraMode(CameraMode.ThirdPerson);
                var origin = interaction.InteractionOrigin.position;
                Aim(origin + new Vector3(.35f,.2f,-3.4f), origin + Vector3.forward * 2); Refresh();
                Check(!interaction.HasResolvedAimPoint && reticle.ScreenOffset == Vector2.zero && reticle.IsVisible, "TPS no hit falls back to visible center");
                var target = new GameObject("Temporary aim target", typeof(BoxCollider), typeof(InteractionTestProbe));
                target.transform.SetParent(fixture.transform); target.transform.position = origin + Vector3.forward * 2;
                target.GetComponent<BoxCollider>().size = Vector3.one * .4f;
                Aim(origin + new Vector3(2,.2f,-3.4f), target.transform.position); Refresh();
                Check(interaction.HasResolvedAimPoint && interaction.CurrentInteractable != null, "production two-stage hit is exposed read-only");
                void CheckProjection(string message)
                {
                    Check(ReticlePresenter.TryGetScreenOffset(camera, (RectTransform)reticle.transform, interaction.ResolvedAimPoint, out var expected) &&
                        Vector2.Distance(reticle.ScreenOffset, expected) < .02f, message);
                }
                settings.SetMode(ReticleMode.Dot); Refresh(); Check(dot.activeSelf && !cross.activeSelf && reticle.IsVisible, "Dot visible"); CheckProjection("Dot resolved projection");
                settings.SetMode(ReticleMode.Cross); Refresh(); Check(cross.activeSelf && !dot.activeSelf && reticle.IsVisible, "Cross visible"); CheckProjection("Cross same resolved projection");
                settings.SetMode(ReticleMode.None); Refresh(); Check(!dot.activeSelf && !cross.activeSelf && !reticle.IsVisible, "None stays invisible"); settings.SetMode(ReticleMode.Dot);
                var wall = new GameObject("Temporary physical occluder", typeof(BoxCollider)); wall.transform.SetParent(fixture.transform);
                wall.transform.position = origin + Vector3.forward; wall.GetComponent<BoxCollider>().size = new Vector3(.15f,1,.15f);
                Refresh();
                Check(interaction.CurrentInteractable == null && interaction.HasResolvedAimPoint &&
                    wall.GetComponent<BoxCollider>().bounds.Contains(interaction.ResolvedAimPoint), "physical wall is resolved; target is not interactable");
                CheckProjection("occluder projection"); Check(reticle.ScreenOffset.magnitude > .1f, "shoulder parallax yields nonzero screen offset");
                wall.SetActive(false);
                target.transform.position = origin + Vector3.forward * 3.5f; Aim(origin + Vector3.right * 3.4f, target.transform.position); Refresh();
                Check(interaction.HasResolvedAimPoint && interaction.CurrentInteractable == null && interaction.InteractionDistance == 3, "out-of-reach aim without valid interaction");
                target.transform.position = origin + Vector3.forward * 2;
                foreach (var cameraDistance in new[] { .45f, 1f, 3.4f })
                { Aim(origin + new Vector3(.35f,.2f,-cameraDistance), target.transform.position); Refresh(); CheckProjection("collision-shortened pose " + cameraDistance); }
                controller.ApplyCameraMode(CameraMode.FirstPerson); Invoke(reticle, "LateUpdate");
                Check(reticle.ScreenOffset == Vector2.zero, "TPS to FPS resets old offset before rendering");
                reader.DisableGameplay(); Refresh(); Check(!reticle.IsVisible && !interaction.HasResolvedAimPoint, "UI lock hides reticle and clears stale hit");
                reader.EnableGameplay(); Refresh();
                Check(interaction.InteractionDistance == 3, "all checks preserve reach");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui); UnityEngine.Object.DestroyImmediate(fixture);
                Cursor.lockState = cursorLock; Cursor.visible = cursorVisible; AudioListener.volume = volume;
            }
        }
    }
}
