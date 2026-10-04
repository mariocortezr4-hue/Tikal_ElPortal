using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Player;
using Tikal.UI;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class GameplayComfortValidation
    {
        const string Active = "Tikal.Comfort.Active", Result = "Tikal.Comfort.Result", Count = "Tikal.Comfort.Count";
        static int checks;
        static PlayerInputReader reader;
        static PlayerController player;
        static InputDeviceManager manager;
        static ReticlePresenter reticle;
        static ReticleSettings settings;
        static ClueJournalPresenter journal;
        static GameObject dot, cross;
        static Keyboard keyboard;
        static Mouse mouse;
        static Gamepad[] pads;
        static TikalInputActions actions;
        static UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? oldDevices;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;

        static GameplayComfortValidation() => EditorApplication.playModeStateChanged += State;
        static void Require(bool value, string message)
        {
            if (!value) throw new Exception("Gameplay comfort: " + message);
            checks++;
            Debug.Log("COMFORT_CHECK: " + message);
        }
        static UnityEngine.Object Reference(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;
        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        [MenuItem("Tikal/Tests/Gameplay comfort")]
        public static void Run()
        {
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                checks = 0;
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var all = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Require(all.All(component => component != null), "No Missing Scripts");
                Require(all.OfType<Canvas>().Count() == 1, "Reticle shares the existing Canvas");
                var view = all.OfType<ReticlePresenter>().Single();
                foreach (string field in new[] { "_settings", "_inputReader", "_dot", "_cross" })
                    Require(Reference(view, field) != null, "Reticle reference " + field);
                Require(PrefabUtility.GetCorrespondingObjectFromSource(view.gameObject) != null, "Independent Reticle prefab instance");
                var state = all.OfType<ReticleSettings>().Single();
                Require(state.Mode == ReticleMode.Dot, "Default = Dot");
                Require(Enum.GetNames(typeof(ReticleMode)).SequenceEqual(new[] { "None", "Dot", "Cross" }), "Exactly three stable ReticleMode values");
                Require(view.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget && graphic.material.shader != null), "Passive UI with valid shaders");
                var dotRoot = (GameObject)Reference(view, "_dot");
                var point = dotRoot.transform.Find("Point").GetComponent<Image>();
                var outline = dotRoot.transform.Find("Outline").GetComponent<Image>();
                Require(point.sprite != null && outline.sprite == point.sprite && point.color.r > .9f && outline.color.r < .1f &&
                    ((RectTransform)point.transform).rect.width < ((RectTransform)outline.transform).rect.width, "Small light point has a dark outline for light and dark backgrounds");
                var crossRoot = (GameObject)Reference(view, "_cross");
                float smallestScale = Mathf.Pow(1280f / 1920f, 1 - all.OfType<CanvasScaler>().Single().matchWidthOrHeight) *
                    Mathf.Pow(720f / 1080f, all.OfType<CanvasScaler>().Single().matchWidthOrHeight);
                Require(((RectTransform)crossRoot.transform.Find("Horizontal")).rect.height * smallestScale >= 1f &&
                    ((RectTransform)crossRoot.transform.Find("Vertical")).rect.width * smallestScale >= 1f &&
                    crossRoot.transform.Find("Horizontal").GetComponent<Image>().color.r > .9f &&
                    crossRoot.transform.Find("Horizontal outline").GetComponent<Image>().color.r < .1f,
                    "Small Cross has light bars wide enough to survive Canvas scaling over dark outlines");
                var prompt = (GameObject)Reference(all.OfType<InteractionPromptPresenter>().Single(), "_promptRoot");
                var discovery = (GameObject)Reference(all.OfType<ClueDiscoveryPresenter>().Single(), "_panel");
                ValidateResponsive((RectTransform)view.transform, (RectTransform)prompt.transform,
                    (RectTransform)discovery.transform, all.OfType<CanvasScaler>().Single());
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");
                var sprint = asset.FindAction("Player/Sprint", true);
                Require(sprint.bindings.Any(binding => binding.path == "<Keyboard>/leftShift") &&
                    sprint.bindings.Any(binding => binding.path == "<Gamepad>/rightStickPress"), "Existing semantic Sprint bindings preserved (Shift / R3)");
                Require(asset.FindAction("Player/Interact").bindings.Any(binding => binding.path == "<Keyboard>/e") &&
                    asset.FindAction("Player/SecretCommand").bindings.Any(binding => binding.path == "<Keyboard>/g"), "E/G unchanged");
                SessionState.SetInt(Count, checks);
                SessionState.SetInt(Result, 1);
                SessionState.SetBool(Active, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ValidateResponsive(RectTransform centre, RectTransform prompt, RectTransform discovery, CanvasScaler scaler)
        {
            Require(centre.anchorMin == new Vector2(.5f, .5f) && centre.anchorMax == centre.anchorMin &&
                centre.pivot == centre.anchorMin && centre.anchoredPosition == Vector2.zero, "Exact centre anchors and pivot");
            Require(prompt.anchorMin == Vector2.zero && prompt.anchorMax == Vector2.zero &&
                prompt.anchoredPosition == new Vector2(50, 50), "Interaction prompt stays bottom left");
            Require(discovery.anchorMin == new Vector2(.5f, 1) && discovery.anchorMax == discovery.anchorMin &&
                discovery.anchoredPosition == new Vector2(0, -50), "Discovery stays top centre");
            foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1280, 720), new Vector2(1920, 1200), new Vector2(1280, 800), new Vector2(1280, 960) })
            {
                var go = new GameObject("Temporary comfort layout", typeof(RectTransform), typeof(Canvas));
                go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var parent = (RectTransform)go.transform;
                float scale = Mathf.Pow(size.x / scaler.referenceResolution.x, 1 - scaler.matchWidthOrHeight) *
                    Mathf.Pow(size.y / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
                parent.sizeDelta = size / scale;
                var a = UnityEngine.Object.Instantiate(centre.gameObject, parent, false);
                var b = UnityEngine.Object.Instantiate(prompt.gameObject, parent, false);
                var c = UnityEngine.Object.Instantiate(discovery.gameObject, parent, false);
                try
                {
                    Canvas.ForceUpdateCanvases();
                    var reticleBounds = Bounds((RectTransform)a.transform);
                    var promptBounds = Bounds((RectTransform)b.transform);
                    var discoveryBounds = Bounds((RectTransform)c.transform);
                    Require(Vector2.Distance(reticleBounds.center, Bounds(parent).center) < .001f, "Reticle exactly centred at " + size);
                    Require(!reticleBounds.Overlaps(promptBounds) && !reticleBounds.Overlaps(discoveryBounds) &&
                        !promptBounds.Overlaps(discoveryBounds), "HUD regions do not overlap at " + size);
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
        }

        static void State(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Active, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    checks = SessionState.GetInt(Count, 0);
                    Setup();
                    ValidateSprint();
                    ValidateReticleAndJournal();
                    ValidateMovement();
                    SessionState.SetInt(Result, 0);
                    Debug.Log("TIKAL_GAMEPLAY_COMFORT_PASSED checks=" + checks);
                }
                catch (Exception exception) { Debug.LogException(exception); SessionState.SetInt(Result, 1); }
                finally { DisposeInput(); EditorApplication.ExitPlaymode(); }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Active, false);
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Result, 1));
            }
        }

        static void Setup()
        {
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            reader = player.GetComponent<PlayerInputReader>();
            manager = player.GetComponent<InputDeviceManager>();
            reticle = UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>();
            settings = UnityEngine.Object.FindAnyObjectByType<ReticleSettings>();
            journal = UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();
            dot = (GameObject)Reference(reticle, "_dot");
            cross = (GameObject)Reference(reticle, "_cross");
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            pads = new Gamepad[] { InputSystem.AddDevice<XInputController>(), InputSystem.AddDevice<DualShockGamepad>(), InputSystem.AddDevice<Gamepad>() };
            actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            oldDevices = actions.devices;
            actions.devices = new InputDevice[] { keyboard, mouse, pads[0], pads[1], pads[2] };
            player.SetGameplayActive(true);
            Require(Mathf.Approximately(reader.GamepadSprintNeutralThreshold, .15f), "Configurable neutral threshold defaults to 0.15 on processed Move");
        }

        static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        static void Pad(Gamepad pad, float move, bool sprint = false, GamepadButton? extra = null)
        {
            var state = new GamepadState { leftStick = new Vector2(0, move) }.WithButton(GamepadButton.RightStick, sprint);
            if (extra.HasValue) state = state.WithButton(extra.Value);
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
        }
        static void Neutral()
        {
            Keys();
            foreach (var pad in pads) Pad(pad, 0);
        }

        static void ValidateSprint()
        {
            Keys(Key.W, Key.LeftShift);
            Require(reader.SprintHeld && reader.SprintRequested && !reader.GamepadSprintLatched, "Keyboard hold activates sprint without a latch");
            Keys(Key.W);
            Require(!reader.SprintHeld && !reader.SprintRequested, "Shift release ends keyboard sprint");
            Keys(Key.W, Key.LeftShift); Keys(Key.W); Keys(Key.W);
            Require(!reader.SprintRequested && !reader.GamepadSprintLatched, "Keyboard never toggles sprint");
            var families = new[] { InputDeviceFamily.Xbox, InputDeviceFamily.PlayStation, InputDeviceFamily.GenericGamepad };
            float drift = Mathf.Lerp(InputSystem.settings.defaultDeadzoneMin, InputSystem.settings.defaultDeadzoneMax, .1f);
            for (int index = 0; index < pads.Length; index++)
            {
                Neutral();
                var pad = pads[index];
                string label = families[index].ToString();
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0, .8f) }.WithButton(GamepadButton.LeftStick));
                InputSystem.Update();
                Require(!reader.SprintRequested && !reader.GamepadSprintLatched, label + " leftStickPress does not activate sprint");
                Pad(pad, .8f);
                Pad(pad, .8f, true);
                Require(manager.ActiveDevice == families[index] && reader.GamepadSprintLatched && reader.SprintRequested, label + " moving + SprintPressed activates latch");
                Pad(pad, .8f);
                Require(!reader.SprintHeld && reader.SprintRequested, label + " button release keeps sprint");
                Pad(pad, .65f);
                Require(reader.SprintRequested, label + " continuous movement keeps sprint");
                Pad(pad, drift);
                Require(reader.Move.magnitude > 0 && reader.Move.magnitude <= .15f &&
                    !reader.GamepadSprintLatched && !reader.SprintRequested, label + " nonzero processed drift clears latch below threshold");
                Pad(pad, drift);
                Require(!reader.GamepadSprintLatched, label + " neutral reset stays stable");
                Pad(pad, .8f);
                Require(!reader.SprintRequested, label + " moving again walks");
                Pad(pad, .8f, true);
                Require(reader.SprintRequested, label + " new press reactivates");
                Pad(pad, 0);
                Pad(pad, 0, true);
                Require(!reader.GamepadSprintLatched, label + " press at rest does not arm sprint");
                Pad(pad, .8f, true);
                Require(!reader.SprintRequested, label + " holding a press from rest cannot arm later");
                Pad(pad, .8f); Pad(pad, .8f, true);
                reader.DisableGameplay();
                Require(!reader.SprintRequested && !reader.GamepadSprintLatched && actions.Global.enabled, label + " DisableGameplay clears latch and preserves Global");
                reader.EnableGameplay(); InputSystem.Update();
                Require(!reader.SprintRequested, label + " re-enable never resumes a held gamepad sprint");
                Pad(pad, .8f); Pad(pad, .8f, true);
                Keys(Key.W);
                Require(manager.ActiveDevice == InputDeviceFamily.KeyboardMouse && !reader.GamepadSprintLatched &&
                    !reader.SprintRequested, label + " keyboard switch clears latch even while gamepad sprint button remains held");
                Keys(Key.W, Key.LeftShift);
                Require(reader.SprintRequested, label + " keyboard Shift hold works immediately");
                Pad(pad, .7f, true);
                Require(manager.ActiveDevice == families[index] && !reader.GamepadSprintLatched &&
                    !reader.SprintRequested, label + " keyboard-to-gamepad starts unlatched");
            }
            Neutral();
            Pad(pads[0], .8f, true);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(4, 0) }); InputSystem.Update();
            Require(manager.ActiveDevice == InputDeviceFamily.KeyboardMouse && !reader.SprintRequested &&
                !reader.GamepadSprintLatched, "Mouse switch also clears gamepad sprint");
            Neutral();
            // Changing the inspector threshold changes the policy on the processed action, not its bindings.
            var field = typeof(PlayerInputReader).GetField("_gamepadSprintNeutralThreshold", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(reader, .3f);
            Pad(pads[0], .8f, true);
            Pad(pads[0], Mathf.Lerp(InputSystem.settings.defaultDeadzoneMin, InputSystem.settings.defaultDeadzoneMax, .2f));
            Require(reader.Move.magnitude > .15f && !reader.GamepadSprintLatched, "Neutral threshold is configurable");
            field.SetValue(reader, .15f);
            Neutral();
        }

        static void ValidateReticleAndJournal()
        {
            Require(settings.Mode == ReticleMode.Dot && dot.activeSelf && !cross.activeSelf && reticle.IsVisible, "Default Dot visible during gameplay");
            settings.SetMode(ReticleMode.Cross);
            Require(!dot.activeSelf && cross.activeSelf && reticle.IsVisible, "Dot to Cross");
            settings.SetMode(ReticleMode.None);
            Require(!dot.activeSelf && !cross.activeSelf && !reticle.IsVisible, "Cross to None hides reticle");
            settings.SetMode(ReticleMode.Dot);
            Require(dot.activeSelf && !cross.activeSelf, "None to Dot");
            settings.SetMode(ReticleMode.Cross);
            var cursor = Cursor.lockState;
            bool cursorVisible = Cursor.visible;
            Pad(pads[0], .8f, true);
            Pad(pads[0], .8f, true, GamepadButton.Select);
            Require(journal.IsOpen && !reticle.IsVisible && !reader.GameplayEnabled &&
                !reader.GamepadSprintLatched, "Native gamepad Journal hides reticle and clears active sprint");
            Require(Cursor.lockState == CursorLockMode.None && Cursor.visible && actions.Global.enabled, "Journal releases cursor and retains Global");
            Pad(pads[0], .8f, true);
            Pad(pads[0], .8f, true, GamepadButton.Select);
            InputSystem.Update();
            Require(!journal.IsOpen && cross.activeSelf && !dot.activeSelf && settings.Mode == ReticleMode.Cross, "Journal restores selected Cross");
            Require(reader.GameplayEnabled && !reader.SprintRequested &&
                Cursor.lockState == cursor && Cursor.visible == cursorVisible, "Close restores cursor and gameplay without resuming held sprint");
            Neutral();
            reticle.SetModalVisible(true);
            Require(!reticle.IsVisible && settings.Mode == ReticleMode.Cross, "External modal hides without changing mode");
            settings.SetMode(ReticleMode.Dot);
            Require(!reticle.IsVisible, "Mode change cannot show reticle through a modal");
            reticle.SetModalVisible(false);
            Require(dot.activeSelf, "Closing external modal restores latest mode");
            reader.DisableGameplay();
            Require(!reticle.IsVisible, "Independently disabled gameplay hides reticle");
            reader.EnableGameplay();
            Require(dot.activeSelf, "Gameplay restore shows selected mode");
            settings.SetMode(ReticleMode.None);
            Keys(Key.J); Keys(); Keys(Key.J); Keys();
            Require(!journal.IsOpen && !reticle.IsVisible && settings.Mode == ReticleMode.None, "Journal close respects None");
            settings.SetMode(ReticleMode.Dot);
            for (int index = 0; index < 3; index++)
            {
                reticle.enabled = false;
                Require(!reticle.IsVisible && !dot.activeSelf && !cross.activeSelf, "Disabled presenter hides visuals");
                reticle.enabled = true;
                Require(dot.activeSelf, "Re-enabled presenter restores Dot");
                reader.enabled = false;
                Require(!reticle.IsVisible && !reader.GamepadSprintLatched, "Disabled input reader hides reticle and resets sprint");
                reader.enabled = true;
                Require(dot.activeSelf, "Re-enabled input reader restores reticle once");
            }
            var modeListeners = (Delegate)typeof(ReticleSettings).GetField("ModeChanged", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(settings);
            var inputListeners = (Delegate)typeof(PlayerInputReader).GetField("GameplayEnabledChanged", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reader);
            Require(modeListeners.GetInvocationList().Count(listener => ReferenceEquals(listener.Target, reticle)) == 1 &&
                inputListeners.GetInvocationList().Count(listener => ReferenceEquals(listener.Target, reticle)) == 1, "Lifecycle cycles do not duplicate UI listeners");
            bool rejected = false;
            try { settings.SetMode((ReticleMode)99); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected && settings.Mode == ReticleMode.Dot, "Invalid future settings value leaves state intact");
        }

        static void Step(PlayerMovement movement, int frames)
        {
            for (int frame = 0; frame < frames; frame++)
                movement.Tick(reader.Move, reader.SprintRequested, reader.CrouchHeld, .02f);
        }
        static void ValidateMovement()
        {
            Neutral();
            var movement = player.GetComponent<PlayerMovement>();
            Step(movement, 30);
            Require(movement.IsGrounded, "Real CharacterController grounded before speed checks");
            Keys(Key.W); Step(movement, 30);
            Require(movement.CurrentSpeed > 3.9f && movement.CurrentSpeed <= 4.001f && !movement.IsSprinting, "Walking remains 4 m/s");
            Keys(); Pad(pads[0], .8f, true); Pad(pads[0], .8f); Step(movement, 30);
            float expected = reader.Move.magnitude * 6.5f;
            Require(movement.IsSprinting && Mathf.Abs(movement.CurrentSpeed - expected) < .02f, "Latched intent reaches existing 6.5 m/s scaled by analog Move");
            Pad(pads[0], .8f, false, GamepadButton.East); Step(movement, 30);
            Require(movement.IsCrouching && !movement.IsSprinting && movement.CurrentSpeed <= 2.001f, "Crouch still takes priority and keeps 2 m/s limit");
            Neutral(); Step(movement, 30);
            Require(!movement.IsCrouching && movement.IsGrounded && !reader.SprintRequested, "Standing neutral clears sprint");
            int jumps = 0;
            Action observer = () => jumps++;
            reader.JumpPressed += observer;
            try
            {
                Pad(pads[0], 0, false, GamepadButton.South);
                Step(movement, 1);
                Require(jumps == 1 && movement.VerticalVelocity > 0 && !movement.IsGrounded, "Native semantic jump still works");
            }
            finally { reader.JumpPressed -= observer; }
            Neutral();
        }

        static void DisposeInput()
        {
            if (actions != null) actions.devices = oldDevices;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (pads != null) foreach (var pad in pads) if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            keyboard = null; mouse = null; pads = null; actions = null;
        }
    }
}
