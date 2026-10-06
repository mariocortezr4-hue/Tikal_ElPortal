using System;
using System.Reflection;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using UnityEditor;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    /// <summary>Isolated, temporary physics fixtures in Play Mode; never writes persisted settings.</summary>
    public static class ThirdPersonCameraFoundationBehaviorChecks
    {
        [MenuItem("Tikal/Validation/Run Third Person Camera Behavior Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Enter Play Mode, then run third person behavior checks.");
                return;
            }
            var cursorLock = Cursor.lockState;
            var cursorVisible = Cursor.visible;
            var volume = AudioListener.volume;
            GameObject fixture = null;
            try
            {
                fixture = new GameObject("TPS temporary behavior checks");
                fixture.transform.position = new Vector3(50000, 50000, 50000);
                fixture.SetActive(false);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath);
                Check(prefab != null, "authored Explorer asset");
                var instance = UnityEngine.Object.Instantiate(prefab, fixture.transform);
                instance.transform.localPosition = Vector3.zero;
                var controller = instance.GetComponent<PlayerCameraController>();
                Check(controller != null && controller.IsConfigured, "authoring completed before behavior checks");
                var camera = instance.GetComponentInChildren<Camera>(true);
                camera.enabled = false;
                camera.GetComponent<AudioListener>().enabled = false;
                fixture.SetActive(true); // Awake now runs with both rendering and listening already disabled.
                var look = instance.GetComponent<PlayerLook>();
                var player = instance.GetComponent<PlayerController>();
                var reader = instance.GetComponent<PlayerInputReader>();
                var applier = instance.GetComponent<PersistentSettingsApplier>();
                var settings = GameSettingsData.CreateDefault();
                settings.CameraMode = CameraMode.FirstPerson;
                applier.Apply(settings);
                Check(!controller.OwnsLook, "FPS input remains owned by PlayerLook");
                look.Tick(new Vector2(10, 10), LookInputMode.Delta, .02f);
                Check(Mathf.Abs(Mathf.DeltaAngle(0, instance.transform.eulerAngles.y) - 1.2f) < .01f && Mathf.Abs(look.Pitch + 1.2f) < .01f,
                    "legacy delta sensitivity and FPS yaw/pitch");
                for (var i = 0; i < 60; i++) look.TickSprintFeedback(true, .02f);
                Check(Mathf.Abs(camera.fieldOfView - 78) < .01f, "legacy sprint FOV 78");
                for (var i = 0; i < 60; i++) look.TickSprintFeedback(false, .02f);
                Check(Mathf.Abs(camera.fieldOfView - 75) < .01f, "legacy normal FOV 75");
                var fpsLocalPosition = camera.transform.localPosition;
                var fpsLocalRotation = camera.transform.localRotation;
                settings.CameraMode = CameraMode.ThirdPerson;
                applier.Apply(settings);
                var yawBefore = instance.transform.rotation;
                var orbitBefore = controller.Yaw;
                controller.TickLook(new Vector2(100, 0), LookInputMode.Delta, .02f);
                Check(Mathf.Abs(Mathf.DeltaAngle(orbitBefore, controller.Yaw) - 12) < .01f && instance.transform.rotation == yawBefore, "free orbit; player stays still");
                orbitBefore = controller.Yaw;
                controller.TickLook(Vector2.right, LookInputMode.Rate, .02f);
                Check(Mathf.Abs(Mathf.DeltaAngle(orbitBefore, controller.Yaw) - 3) < .01f, "stick rate sensitivity uses deltaTime");
                settings.MouseSensitivity = .24f;
                settings.GamepadSensitivity = 100f;
                settings.InvertY = true;
                applier.Apply(settings);
                var pitch = controller.Pitch;
                controller.TickLook(new Vector2(0, 10), LookInputMode.Delta, .02f);
                Check(Mathf.Abs(controller.Pitch - pitch - 2.4f) < .01f, "settings update sensitivity + InvertY live");
                controller.TickLook(new Vector2(0, 10000), LookInputMode.Delta, .02f);
                Check(controller.Pitch == controller.Profile.PitchMax, "maximum pitch clamp");
                controller.TickLook(new Vector2(0, -10000), LookInputMode.Delta, .02f);
                Check(controller.Pitch == controller.Profile.PitchMin, "minimum pitch clamp");
                player.SetGameplayActive(false);
                orbitBefore = controller.Yaw;
                controller.TickLook(Vector2.one * 100, LookInputMode.Delta, .02f);
                Check(controller.Yaw == orbitBefore, "shared Journal/Pause gameplay lock");
                player.SetGameplayActive(true);
                reader.DisableGameplay();
                controller.TickLook(Vector2.one * 100, LookInputMode.Delta, .02f);
                Check(controller.Yaw == orbitBefore, "modal input map lock");
                reader.EnableGameplay();
                // Exercise actual physics casts in the fixture's remote location.
                var origin = instance.transform.position + Vector3.up * controller.Profile.PivotHeight;
                var wall = new GameObject("wall", typeof(BoxCollider));
                wall.transform.SetParent(fixture.transform);
                wall.transform.position = origin + Vector3.back * 1.5f;
                wall.GetComponent<BoxCollider>().size = new Vector3(4, 4, .2f);
                Physics.SyncTransforms();
                var safe = Cast(controller, origin, Vector3.back, 3.4f);
                Check(safe > 1 && safe < 1.3f, "sphere collision retracts before wall with padding");
                wall.GetComponent<BoxCollider>().isTrigger = true;
                Physics.SyncTransforms();
                Check(Mathf.Approximately(Cast(controller, origin, Vector3.back, 3.4f), 3.4f), "triggers ignored");
                var self = instance.AddComponent<BoxCollider>();
                self.center = new Vector3(0, controller.Profile.PivotHeight, -1);
                self.size = Vector3.one * .2f;
                Physics.SyncTransforms();
                Check(Mathf.Approximately(Cast(controller, origin, Vector3.back, 3.4f), 3.4f), "all own colliders filtered without ignoring world layers");
                self.enabled = false;
                wall.GetComponent<BoxCollider>().isTrigger = false;
                wall.transform.position = origin + Vector3.back * .45f;
                Physics.SyncTransforms();
                Check(Cast(controller, origin, Vector3.back, 3.4f) < controller.Profile.MinimumDistance, "collision overrides minimum framing distance");
                wall.transform.position = origin;
                Physics.SyncTransforms();
                var resolved = (Vector3)Invoke(controller, "ResolveOverlaps", origin);
                Check(Vector3.Distance(resolved, wall.GetComponent<BoxCollider>().ClosestPoint(resolved)) >= controller.Profile.CollisionRadius - .001f,
                    "initial sphere overlap is depenetrated");
                wall.transform.position = origin + Vector3.back * 1.5f;
                instance.transform.rotation = Quaternion.identity;
                look.Tick(Vector2.zero, LookInputMode.Delta, .02f);
                controller.ApplyCameraMode(CameraMode.FirstPerson);
                controller.ApplyCameraMode(CameraMode.ThirdPerson);
                // Seed a level orbit for deterministic behind-player framing.
                Field(controller, "_yaw", 0f); Field(controller, "_pitch", 0f);
                Field(controller, "_smoothYaw", 0f); Field(controller, "_smoothPitch", 0f);
                Physics.SyncTransforms();
                Invoke(controller, "UpdateThirdPersonPose", .02f);
                var compressed = Vector3.Distance(origin, camera.transform.position);
                Check(compressed < 1.4f, "actual camera pose stays in front of wall");
                wall.GetComponent<BoxCollider>().enabled = false;
                Physics.SyncTransforms();
                Invoke(controller, "UpdateThirdPersonPose", .02f);
                var returning = Vector3.Distance(origin, camera.transform.position);
                Check(returning > compressed && returning < 3.3f, "wall removal returns gradually");
                for (var i = 0; i < 100; i++) Invoke(controller, "UpdateThirdPersonPose", .02f);
                Check(Mathf.Abs(Vector3.Distance(origin, camera.transform.position) - Mathf.Sqrt(3.4f * 3.4f + .35f * .35f)) < .03f, "unobstructed full distance restored");
                controller.ApplyCameraMode(CameraMode.FirstPerson);
                Check(camera.transform.localPosition == fpsLocalPosition && camera.transform.localRotation == fpsLocalRotation, "runtime switch restores exact FPS local pose");
                Debug.Log("TIKAL_THIRD_PERSON_CAMERA_BEHAVIOR_VALID: FPS, FOV, mode event, orbit, sensitivities, inversion, clamps, locks, collision, triggers, self, overlap and return smoothing.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (fixture != null)
                {
                    fixture.SetActive(false);
                    UnityEngine.Object.Destroy(fixture);
                }
                AudioListener.volume = volume;
                Cursor.lockState = cursorLock;
                Cursor.visible = cursorVisible;
            }
        }

        private static void Check(bool condition, string message) => ThirdPersonCameraFoundationValidation.Require(condition, message);
        private static object Invoke(PlayerCameraController target, string name, params object[] args) =>
            typeof(PlayerCameraController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Field(PlayerCameraController target, string name, float value) =>
            typeof(PlayerCameraController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static float Cast(PlayerCameraController target, Vector3 origin, Vector3 direction, float distance) =>
            (float)Invoke(target, "SafeDistance", origin, direction, distance);
    }
}
