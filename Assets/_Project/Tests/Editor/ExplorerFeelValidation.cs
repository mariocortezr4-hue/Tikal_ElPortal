using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class ExplorerFeelValidation
    {
        private const string ScenePath = "Assets/_Project/Scenes/Testing/PlayerMovementTest.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Explorer.prefab";
        private const string ActiveKey = "Tikal.Player.Validation.Active";
        private const string ResultKey = "Tikal.Player.Validation.Result";
        private const float StepTime = 0.02f;

        static ExplorerFeelValidation() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem("Tikal/Tests/Explorer game feel")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEngine.Debug.LogError("Exit Play Mode before running Explorer validation.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Require(System.IO.File.Exists(ScenePath), "PlayerMovementTest must exist.");
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var components = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(component => component != null), "Scene has no Missing Scripts.");
                var explorer = components.OfType<PlayerController>().Single();
                Require(PrefabUtility.GetCorrespondingObjectFromSource(explorer.gameObject) != null, "Explorer is a prefab instance.");
                Require(components.OfType<CharacterController>().Count() == 1, "One CharacterController.");
                Require(!components.OfType<Rigidbody>().Any(), "No player Rigidbody.");
                var camera = components.OfType<Camera>().Single();
                Require(camera.CompareTag("MainCamera") && camera.transform.parent.name == "CameraRoot", "MainCamera configured.");
                foreach (var field in new[] { "_inputReader", "_movement", "_look" })
                    Require(new SerializedObject(explorer).FindProperty(field).objectReferenceValue != null, "Controller reference: " + field);
                var look = components.OfType<PlayerLook>().Single();
                Require(new SerializedObject(look).FindProperty("_cameraRoot").objectReferenceValue != null, "CameraRoot reference saved.");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(prefab.GetComponentsInChildren<Component>(true).All(component => component != null), "Prefab has no Missing Scripts.");
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(ResultKey, 1);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    ValidateRuntime();
                    ValidateFeel();
                    SessionState.SetInt(ResultKey, 0);
                    UnityEngine.Debug.Log("TIKAL_EXPLORER_FEEL_PASSED: regression suite, coyote windows, buffer windows, no double jump, safe camera smoothing, invert Y, FOV and bounded sprint transitions.");
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                    SessionState.SetInt(ResultKey, 1);
                }
                EditorApplication.ExitPlaymode();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                var result = SessionState.GetInt(ResultKey, 1);
                SessionState.SetBool(ActiveKey, false);
                if (Application.isBatchMode) EditorApplication.Exit(result);
                else UnityEngine.Debug.Log("Explorer validation result: " + result);
            }
        }

        internal static void ValidateCurrentPlayer()
        {
            ValidateRuntime();
            ValidateFeel();
        }

        private static void ValidateRuntime()
        {
            var coordinator = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Require(coordinator != null && coordinator.enabled, "Explorer loads with enabled coordinator.");
            var reader = coordinator.GetComponent<PlayerInputReader>();
            var movement = coordinator.GetComponent<PlayerMovement>();
            var look = coordinator.GetComponent<PlayerLook>();
            var capsule = coordinator.GetComponent<CharacterController>();
            var cameraRoot = coordinator.transform.Find("CameraRoot");
            var actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            Require(actions != null && actions.Player.enabled && actions.Global.enabled, "Input wrapper available.");
            var settings = InputSystem.settings;
            var originalBackground = settings.backgroundBehavior;
            var originalEditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var oldDevices = actions.devices;
            actions.devices = new InputDevice[] { keyboard, mouse, gamepad };
            var jumpCount = 0; var interactCount = 0; var secretCount = 0; var pauseCount = 0;
            Action jump = () => jumpCount++;
            Action interact = () => interactCount++;
            Action secret = () => secretCount++;
            Action pause = () => pauseCount++;
            reader.JumpPressed += jump; reader.InteractPressed += interact;
            reader.SecretCommandPressed += secret; reader.PausePressed += pause;
            try
            {
                var clearPosition = new Vector3(-10, 0.08f, -8);
                foreach (var key in new[] { Key.W, Key.S, Key.A, Key.D })
                {
                    Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                    var start = coordinator.transform.position;
                    SetKeyboard(keyboard, key); Step(movement, look, reader, 30);
                    var displacement = coordinator.transform.position - start;
                    Require(key == Key.W ? displacement.z > 1 : key == Key.S ? displacement.z < -1 : key == Key.A ? displacement.x < -1 : displacement.x > 1, "WASD direction: " + key);
                }
                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                SetKeyboard(keyboard, Key.W, Key.D); Step(movement, look, reader, 30);
                Require(movement.CurrentSpeed <= 4.001f && movement.CurrentSpeed > 3.9f, "Diagonal speed is clamped to walk speed.");
                SetKeyboard(keyboard, Key.W, Key.LeftShift); Step(movement, look, reader, 30);
                Require(movement.IsSprinting && movement.CurrentSpeed > 6.4f, "Ground sprint reaches configured speed.");
                SetKeyboard(keyboard, Key.W); Step(movement, look, reader, 20);
                Require(!movement.IsSprinting && movement.CurrentSpeed <= 4.001f, "Release sprint returns to walk.");
                SetKeyboard(keyboard); Step(movement, look, reader, 20);
                Require(movement.CurrentSpeed < 0.001f, "Release movement decelerates to rest.");

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(10, 4) }); InputSystem.Update();
                Require(reader.LookMode == LookInputMode.Delta, "Pointer delta is semantic displacement.");
                look.Tick(reader.Look, reader.LookMode, StepTime);
                Require(Mathf.Abs(Mathf.DeltaAngle(0, coordinator.transform.eulerAngles.y) - 1.2f) < 0.01f && look.Pitch < 0, "Mouse delta sensitivity is independent of frame duration.");
                var beforeDelta = coordinator.transform.eulerAngles.y;
                look.Tick(new Vector2(10, 0), LookInputMode.Delta, 0.1f);
                Require(Mathf.Abs(Mathf.DeltaAngle(beforeDelta, coordinator.transform.eulerAngles.y) - 1.2f) < 0.01f, "Mouse does not multiply by deltaTime.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(1, 0) }); InputSystem.Update();
                Require(reader.LookMode == LookInputMode.Rate, "Stick is semantic angular rate.");
                var beforeRate = coordinator.transform.eulerAngles.y;
                look.Tick(reader.Look, reader.LookMode, StepTime);
                Require(Mathf.Abs(Mathf.DeltaAngle(beforeRate, coordinator.transform.eulerAngles.y) - 3f) < 0.01f, "Gamepad look uses degrees per second.");
                look.Tick(new Vector2(0, 100000), LookInputMode.Delta, StepTime);
                Require(Mathf.Abs(look.Pitch + 89) < 0.01f, "Minimum pitch clamp.");
                look.Tick(new Vector2(0, -100000), LookInputMode.Delta, StepTime);
                Require(Mathf.Abs(look.Pitch - 89) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(0, cameraRoot.localEulerAngles.z)) < 0.01f, "Maximum pitch clamp and no roll.");

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0, 1) }); InputSystem.Update();
                var gamepadStart = coordinator.transform.position;
                Step(movement, look, reader, 30);
                Require(coordinator.transform.position.z - gamepadStart.z > 1, "Gamepad moves through same abstraction.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update();
                Step(movement, look, reader, 20);
                var groundY = coordinator.transform.position.y;
                Step(movement, look, reader, 200);
                Require(movement.IsGrounded && Mathf.Abs(coordinator.transform.position.y - groundY) < 0.02f, "Stable ground contact over 200 steps.");
                PressKeyboard(keyboard, Key.Space);
                Require(jumpCount == 1, "One JumpPressed per button press.");
                Step(movement, look, reader, 1);
                Require(!movement.IsGrounded && movement.VerticalVelocity > 0, "Jump starts only from ground.");
                var maximumY = coordinator.transform.position.y;
                for (var index = 0; index < 20; index++)
                {
                    PressKeyboard(keyboard, Key.Space);
                    Step(movement, look, reader, 1);
                    maximumY = Mathf.Max(maximumY, coordinator.transform.position.y);
                }
                Require(maximumY - groundY > 0.9f && maximumY - groundY < 1.3f && movement.VerticalVelocity < 0, "Desired jump height, gravity and no infinite air jumps.");
                Step(movement, look, reader, 80);
                Require(movement.IsGrounded && Mathf.Abs(coordinator.transform.position.y - groundY) < 0.05f, "Jump lands stably.");
                Teleport(coordinator, movement, capsule, new Vector3(-10, 3, -8));
                Step(movement, look, reader, 10);
                Require(movement.VerticalVelocity < -4 && coordinator.transform.position.y < 3, "Gravity accelerates falling.");
                Step(movement, look, reader, 100); Require(movement.IsGrounded, "Fall lands.");

                SetKeyboard(keyboard, Key.LeftCtrl); Step(movement, look, reader, 30);
                Require(movement.IsCrouching && Mathf.Abs(capsule.height - 1f) < 0.001f && Mathf.Abs(capsule.center.y - 0.5f) < 0.001f && Mathf.Abs(cameraRoot.localPosition.y - 0.85f) < 0.01f, "Crouch changes capsule and camera coherently.");
                Teleport(coordinator, movement, capsule, new Vector3(5, 0.08f, 3));
                Step(movement, look, reader, 10);
                SetKeyboard(keyboard); Step(movement, look, reader, 30);
                Require(movement.IsCrouching && Mathf.Abs(capsule.height - 1f) < 0.001f, "Cannot stand below low ceiling.");
                Teleport(coordinator, movement, capsule, clearPosition);
                Step(movement, look, reader, 30);
                Require(!movement.IsCrouching && Mathf.Abs(capsule.height - 1.8f) < 0.001f, "Can stand after leaving ceiling.");

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, new Vector3(-5, 0.08f, -3));
                SetKeyboard(keyboard, Key.W);
                var stepMaximumY = coordinator.transform.position.y;
                for (var index = 0; index < 35; index++) { Step(movement, look, reader, 1); stepMaximumY = Mathf.Max(stepMaximumY, coordinator.transform.position.y); }
                Require(stepMaximumY > 0.18f, "CharacterController traverses 0.25m step.");
                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, new Vector3(-5, 0.08f, 0.5f));
                SetKeyboard(keyboard, Key.W); Step(movement, look, reader, 65);
                Require(coordinator.transform.position.y > 0.5f && coordinator.transform.position.z > 2, "Controller ascends ramp within slope limit.");

                SetKeyboard(keyboard); coordinator.SetGameplayActive(false);
                Require(!actions.Player.enabled && actions.Global.enabled, "Gameplay gate preserves Global.");
                PressKeyboard(keyboard, Key.Escape); Require(pauseCount == 1, "Pause remains available.");
                coordinator.SetGameplayActive(true);
                PressKeyboard(keyboard, Key.E); Require(interactCount == 1 && secretCount == 0, "Interact remains independent.");
                PressKeyboard(keyboard, Key.G); Require(secretCount == 1 && interactCount == 1, "SecretCommand remains independent.");
                coordinator.SetGameplayActive(false); coordinator.SetGameplayActive(true);
                Require(actions.Player.enabled && actions.Global.enabled, "Gameplay gate is reusable.");
            }
            finally
            {
                reader.JumpPressed -= jump; reader.InteractPressed -= interact;
                reader.SecretCommandPressed -= secret; reader.PausePressed -= pause;
                actions.devices = oldDevices;
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(gamepad);
                settings.backgroundBehavior = originalBackground;
                settings.editorInputBehaviorInPlayMode = originalEditorBehavior;
            }
        }

        private static void ValidateFeel()
        {
            var coordinator = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var reader = coordinator.GetComponent<PlayerInputReader>();
            var movement = coordinator.GetComponent<PlayerMovement>();
            var look = coordinator.GetComponent<PlayerLook>();
            var capsule = coordinator.GetComponent<CharacterController>();
            var cameraRoot = coordinator.transform.Find("CameraRoot");
            var camera = cameraRoot.GetComponentInChildren<Camera>();
            var actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            var settings = InputSystem.settings;
            var originalBackground = settings.backgroundBehavior;
            var originalEditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var oldDevices = actions.devices;
            actions.devices = new InputDevice[] { keyboard, gamepad };
            try
            {
                var coyote = GetFloat(movement, "_coyoteTime");
                var clearPosition = new Vector3(-10, 0.08f, -8);
                Require(coyote > 0 && GetFloat(movement, "_jumpBuffer") > 0, "Default forgiveness windows are configured.");
                LeavePlatform(coordinator, movement, look, capsule, cameraRoot, reader, keyboard, gamepad);
                Step(movement, look, reader, Mathf.Max(1, Mathf.FloorToInt(coyote * 0.4f / StepTime)));
                PressKeyboard(keyboard, Key.Space); Step(movement, look, reader, 1);
                Require(movement.VerticalVelocity > 0, "Coyote jump succeeds inside window.");
                var firstVelocity = movement.VerticalVelocity;
                PressKeyboard(keyboard, Key.Space); Step(movement, look, reader, 1);
                Require(movement.VerticalVelocity < firstVelocity, "Consumed coyote cannot jump twice.");
                LeavePlatform(coordinator, movement, look, capsule, cameraRoot, reader, keyboard, gamepad);
                Step(movement, look, reader, Mathf.CeilToInt(coyote * 1.6f / StepTime));
                Require(!movement.IsGrounded, "Outside-window test remains airborne.");
                PressKeyboard(keyboard, Key.Space); Step(movement, look, reader, 1);
                Require(movement.VerticalVelocity <= 0, "Coyote rejected outside window.");

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                Teleport(coordinator, movement, capsule, new Vector3(-10, 0.5f, -8));
                for (var index = 0; index < 30 && coordinator.transform.position.y > 0.2f; index++) Step(movement, look, reader, 1);
                Require(!movement.IsGrounded && movement.VerticalVelocity < 0, "Buffer press happens during a real fall before landing.");
                PressKeyboard(keyboard, Key.Space);
                var starts = 0;
                var previousVelocity = movement.VerticalVelocity;
                for (var index = 0; index < 100; index++)
                {
                    Step(movement, look, reader, 1);
                    if (previousVelocity <= 0 && movement.VerticalVelocity > 0) starts++;
                    previousVelocity = movement.VerticalVelocity;
                }
                Require(starts == 1 && movement.IsGrounded, "Buffer fires exactly one jump at landing.");
                Teleport(coordinator, movement, capsule, new Vector3(-10, 3, -8));
                Step(movement, look, reader, Mathf.CeilToInt(coyote / StepTime) + 2);
                Require(!movement.IsGrounded && GetFloat(movement, "_coyoteRemaining") == 0, "Expiration starts airborne without coyote eligibility.");
                PressKeyboard(keyboard, Key.Space);
                var everAscending = false;
                for (var index = 0; index < 100; index++)
                {
                    Step(movement, look, reader, 1);
                    everAscending |= movement.VerticalVelocity > 0;
                }
                Require(!everAscending && movement.IsGrounded, "Buffer expires before later landing.");

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                look.Tick(new Vector2(12, 8), LookInputMode.Delta, StepTime);
                var rotation = cameraRoot.localRotation;
                var yaw = coordinator.transform.rotation;
                SetKeyboard(keyboard, Key.LeftCtrl);
                var previousHeight = cameraRoot.localPosition.y;
                for (var index = 0; index < 35; index++)
                {
                    Step(movement, look, reader, 1);
                    Require(cameraRoot.localPosition.y <= previousHeight + 0.002f, "Camera crouches monotonically.");
                    Require(cameraRoot.localPosition.y <= movement.MaximumEyeHeight + 0.002f, "Camera remains safely inside capsule.");
                    if (index == 0) Require(cameraRoot.localPosition.y > 0.9f, "Camera does not snap to crouch target.");
                    previousHeight = cameraRoot.localPosition.y;
                }
                Require(Mathf.Abs(cameraRoot.localPosition.y - 0.85f) < 0.01f, "Camera settles crouched.");
                Require(Quaternion.Angle(rotation, cameraRoot.localRotation) < 0.01f && Quaternion.Angle(yaw, coordinator.transform.rotation) < 0.01f, "Crouch preserves yaw/pitch/roll.");
                SetKeyboard(keyboard); Step(movement, look, reader, 40);
                Require(Mathf.Abs(cameraRoot.localPosition.y - 1.62f) < 0.01f && !movement.IsCrouching, "Camera settles standing.");
                SetBool(look, "_invertY", true);
                var beforeInvert = look.Pitch;
                look.Tick(new Vector2(0, 1), LookInputMode.Delta, StepTime);
                Require(look.Pitch > beforeInvert, "Invert Y reverses vertical look.");
                SetBool(look, "_invertY", false);

                Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, clearPosition);
                SetKeyboard(keyboard, Key.W); Step(movement, look, reader, 30);
                var walkSpeed = movement.CurrentSpeed;
                SetKeyboard(keyboard, Key.W, Key.LeftShift); Step(movement, look, reader, 1);
                Require(movement.CurrentSpeed > walkSpeed && movement.CurrentSpeed < 6.5f, "Sprint accelerates without instant jump.");
                Step(movement, look, reader, 30);
                Require(movement.CurrentSpeed <= 6.501f && movement.CurrentSpeed > 6.4f, "Sprint speed stays bounded.");
                SetKeyboard(keyboard, Key.W); Step(movement, look, reader, 1);
                Require(movement.CurrentSpeed > 4 && movement.CurrentSpeed < 6.5f, "Sprint release is progressive.");
                Step(movement, look, reader, 20);
                Require(movement.CurrentSpeed <= 4.001f, "Speed returns to walk.");
                look.TickSprintFeedback(false, 1f);
                look.TickSprintFeedback(true, StepTime);
                Require(camera.fieldOfView > 75 && camera.fieldOfView < 78, "FOV transition is not instant.");
                for (var index = 0; index < 40; index++) look.TickSprintFeedback(true, StepTime);
                Require(Mathf.Abs(camera.fieldOfView - 78) < 0.01f, "Sprint FOV settles.");
                SetBool(look, "_enableSprintFov", false);
                for (var index = 0; index < 40; index++) look.TickSprintFeedback(true, StepTime);
                Require(Mathf.Abs(camera.fieldOfView - 75) < 0.01f, "FOV feedback can be disabled.");
                SetBool(look, "_enableSprintFov", true);
            }
            finally
            {
                actions.devices = oldDevices;
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
                settings.backgroundBehavior = originalBackground;
                settings.editorInputBehaviorInPlayMode = originalEditorBehavior;
            }
        }

        private static void LeavePlatform(PlayerController coordinator, PlayerMovement movement, PlayerLook look,
            CharacterController capsule, Transform cameraRoot, IPlayerInputReader reader, Keyboard keyboard, Gamepad gamepad)
        {
            Reset(coordinator, movement, look, capsule, cameraRoot, keyboard, gamepad, new Vector3(0, 1.45f, 6));
            Require(movement.IsGrounded, "Coyote starts on raised platform.");
            SetKeyboard(keyboard, Key.D);
            for (var index = 0; index < 100 && movement.IsGrounded; index++) Step(movement, look, reader, 1);
            Require(!movement.IsGrounded, "Player walks off a real edge.");
            SetKeyboard(keyboard);
        }

        private static float GetFloat(object target, string field) =>
            (float)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void SetBool(object target, string field, bool value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Reset(PlayerController coordinator, PlayerMovement movement, PlayerLook look, CharacterController capsule, Transform cameraRoot, Keyboard keyboard, Gamepad gamepad, Vector3 position)
        {
            SetKeyboard(keyboard);
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update();
            typeof(PlayerLook).GetField("_yaw", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(look, 0f);
            typeof(PlayerLook).GetField("_pitch", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(look, 0f);
            coordinator.transform.rotation = Quaternion.identity;
            cameraRoot.localRotation = Quaternion.identity;
            Teleport(coordinator, movement, capsule, position);
            Step(movement, look, coordinator.GetComponent<PlayerInputReader>(), 30);
        }

        private static void Teleport(PlayerController coordinator, PlayerMovement movement, CharacterController capsule, Vector3 position)
        {
            capsule.enabled = false;
            coordinator.transform.position = position;
            capsule.enabled = true;
            movement.StopMotion();
            Physics.SyncTransforms();
        }

        private static void Step(PlayerMovement movement, PlayerLook look, IPlayerInputReader input, int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                look.Tick(input.Look, input.LookMode, StepTime);
                movement.Tick(input.Move, input.SprintRequested, input.CrouchHeld, StepTime);
                look.SetEyeHeight(movement.EyeHeight, movement.MaximumEyeHeight, StepTime);
                look.TickSprintFeedback(movement.IsSprinting, StepTime);
            }
        }

        private static void SetKeyboard(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
        }

        private static void PressKeyboard(Keyboard keyboard, Key key)
        {
            SetKeyboard(keyboard, key); SetKeyboard(keyboard);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }

}
