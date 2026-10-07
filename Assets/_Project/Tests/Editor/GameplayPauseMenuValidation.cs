using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tutorial;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class GameplayPauseMenuValidation
    {
        private static int _checks;
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException("Pause: " + message); _checks++; }
        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        [MenuItem("Tikal/Validation/Validate Gameplay Pause Menu")]
        public static void Run()
        {
            _checks = 0;
            try
            {
                ValidateBindings();
                if (Application.isPlaying) ValidateRuntime();
                else foreach (var path in GameplayPauseMenuAuthoring.Scenes)
                {
                    var scene = EditorSceneManager.OpenPreviewScene(path);
                    try
                    {
                        var pauses = GameplayPauseMenuAuthoring.Components<PauseMenuController>(scene);
                        Check(pauses.Length == 1, path + ": apply Gameplay Pause Menu authoring first");
                        Check(pauses[0].IsConfigured, path + ": all critical pause/menu/settings references");
                        Check(pauses[0].isActiveAndEnabled && pauses[0].HasIndependentOverlay,
                            path + ": active listener outside hidden overlay");
                        Check(((PlayerInputReader)Field(pauses[0], "_input")).isActiveAndEnabled, "active gameplay input reader");
                        Check(pauses[0].GetComponentInParent<Canvas>() != null && pauses[0].GetComponentInParent<Canvas>().isActiveAndEnabled,
                            "active existing gameplay Canvas");
                        Check(GameplayPauseMenuAuthoring.Components<EventSystem>(scene).Length == 1, "one EventSystem");
                        var events = GameplayPauseMenuAuthoring.Components<EventSystem>(scene).Single();
                        Check(events.GetComponent<InputSystemUIInputModule>() != null, "native Input System UI navigation");
                        var module = events.GetComponent<InputSystemUIInputModule>();
                        Check(events.isActiveAndEnabled && module.isActiveAndEnabled && module.move?.action != null &&
                            module.submit?.action != null && module.cancel?.action != null, "active connected UI navigation");
                        Check(GameplayPauseMenuAuthoring.Components<SettingsMenuController>(scene).Length == 1, "one reused settings controller");
                        var settings = GameplayPauseMenuAuthoring.Components<SettingsMenuController>(scene).Single();
                        Check(Field(settings, "_runtimeApplier") == (object)GameplayPauseMenuAuthoring.Components<PersistentSettingsApplier>(scene).Single(), "same gameplay settings applier");
                        foreach (var slider in settings.GetComponentsInChildren<Slider>(true))
                            Check(slider.navigation.mode == Navigation.Mode.Explicit && slider.navigation.selectOnLeft == null && slider.navigation.selectOnRight == null,
                                "slider horizontal navigation adjusts value: " + slider.name);
                        var hasTutorial = GameplayPauseMenuAuthoring.Components<TutorialSequenceController>(scene).Length == 1;
                        Check((bool)Field(pauses[0], "_restartTutorialFromStart") == hasTutorial, "only Tutorial may restart at scene start");
                        Check(!((GameObject)Field(pauses[0], "_overlay")).activeSelf, "Pause authored closed");
                        foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
                            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no Missing Scripts: " + transform.name);
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                Debug.Log("TIKAL_GAMEPLAY_PAUSE_VALID checks=" + _checks + (Application.isPlaying ? " runtime" : " authored references; run again in Play Mode"));
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private static void ValidateBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");
            var pause = asset.FindAction("Global/Pause", true);
            Check(pause.bindings.Any(b => b.effectivePath == "<Keyboard>/escape") && pause.bindings.Any(b => b.effectivePath == "<Gamepad>/start"), "existing Escape/Start Pause bindings");
            foreach (var name in new[] { "Navigate", "Submit", "Cancel", "Point", "Click", "ScrollWheel" })
                Check(GameplayPauseMenuAuthoring.ActionReference(name).action.id == asset.FindAction("UI/" + name, true).id,
                    "authoring resolves UI action including legacy duplicate references: " + name);
            var source = File.ReadAllText("Assets/_Project/Code/UI/PauseMenuController.cs");
            Check(!source.Contains("SecretCommandAction"), "Controls never reveals secret action");
            Check(source.Contains("_input.PausePressed +=") && source.Contains("_player.SetGameplayActive(false)"), "reuse Pause event and gameplay lock authority");
            Check(!source.Contains("PlayerPrefs") && source.Contains("Time.timeScale = 0"), "real pause, no persisted pause state");
        }
        private static void ValidateRuntime()
        {
            var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            Check(pause != null && pause.IsConfigured && !pause.IsOpen && pause.CanOpen, "authored gameplay scene, close menu before validation");
            var player = (PlayerController)Field(pause, "_player");
            var reader = (PlayerInputReader)Field(pause, "_input");
            Check(pause.IsListening && pause.HasIndependentOverlay, "runtime listener subscribed outside hidden overlay");
            Check(reader.isActiveAndEnabled && reader.PauseAction != null && reader.PauseAction.enabled &&
                reader.PauseAction.actionMap.name == "Global" && reader.PauseAction.actionMap.enabled,
                "Global/Pause enabled before opening gameplay menu");
            var overlay = (GameObject)Field(pause, "_overlay");
            Check(!overlay.activeSelf && pause.isActiveAndEnabled, "hidden overlay keeps listener active");
            var journal = (ClueJournalPresenter)Field(pause, "_journal");
            var events = (EventSystem)Field(pause, "_events");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length == 1 &&
                events.isActiveAndEnabled && events.GetComponent<InputSystemUIInputModule>()?.isActiveAndEnabled == true,
                "one active runtime EventSystem with native UI input module");
            var settings = (SettingsMenuController)Field(pause, "_settings");
            var applier = (PersistentSettingsApplier)Field(settings, "_runtimeApplier");
            var original = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(applier.CurrentSettings));
            var originalScale = Time.timeScale; var originalVolume = AudioListener.volume;
            var originalCursor = Cursor.lockState; var originalVisible = Cursor.visible;
            var originalPersistence = Field(settings, "_persistence");
            var actions = (TikalInputActions)Field(reader, "_actions"); var oldDevices = actions.devices;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pads = new Gamepad[] { InputSystem.AddDevice<XInputController>(), InputSystem.AddDevice<DualShockGamepad>(), InputSystem.AddDevice<Gamepad>() };
            var tempRoot = Path.GetFullPath("Temp/PauseValidation-" + Guid.NewGuid().ToString("N"));
            void Late() => Invoke(pause, "LateUpdate");
            void PressKey(Key key) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update(); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); Late(); }
            void Pad(Gamepad pad, GamepadButton button) { InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); InputSystem.Update(); InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update(); Late(); }
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                actions.devices = new InputDevice[] { keyboard, pads[0], pads[1], pads[2] };
                for (var device = -1; device < pads.Length; device++)
                {
                    if (device < 0) PressKey(Key.Escape); else Pad(pads[device], GamepadButton.Start);
                    Check(pause.IsOpen && Time.timeScale == 0 && !player.GameplayActive && !reader.GameplayEnabled, "native Pause opens and locks: " + device);
                    Check(overlay.activeInHierarchy && pause.IsListening, "native Pause makes panel visible and keeps listener subscribed: " + device);
                    var module = events.GetComponent<InputSystemUIInputModule>();
                    Check(module.move?.action?.enabled == true && module.submit?.action?.enabled == true && module.cancel?.action?.enabled == true,
                        "UI navigation/Submit/Cancel enabled while paused: " + device);
                    Check(actions.Global.Pause.enabled && actions.Global.Journal.enabled && !actions.Player.enabled, "Global remains active, gameplay including secret disabled");
                    Check(!((ReticlePresenter)UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>()).IsVisible, "reticle hidden during pause");
                    Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "mouse cursor available");
                    Check(events.currentSelectedGameObject == ((Button)Field(pause, "_continue")).gameObject, "Continue selected");
                    var position = player.transform.position; var rotation = player.GetComponentInChildren<Camera>().transform.rotation;
                    PressKey(Key.W); PressKey(Key.G); PressKey(Key.J); Invoke(player, "Update");
                    Check(player.transform.position == position && player.GetComponentInChildren<Camera>().transform.rotation == rotation && reader.Move == Vector2.zero && reader.Look == Vector2.zero,
                        "movement/look/TPS orbit/secret gated");
                    Check(journal == null || !journal.IsOpen, "Journal cannot overlay Pause");
                    pause.ShowControls();
                    if (device < 0) PressKey(Key.H); else Pad(pads[device], GamepadButton.East);
                    var body = ((TMP_Text)Field(pause, "_controlsText")).text;
                    Check(!body.Contains("SecretCommand") && !body.Contains("Botón norte") && !body.Contains("△"), "no secret spoiler in controls");
                    Check(body.Contains("Moverse") && body.Contains("Pausa") && body.Contains("Diario"), "eight principal controls");
                    Check(body.Contains("Interactuar   " + (device < 0 ? "E" : new[] { "X", "□", "Botón oeste" }[device])), "device-aware controls: " + device);
                    if (device < 0) PressKey(Key.Escape); else Pad(pads[device], GamepadButton.Start);
                    Check(!pause.IsOpen && player.GameplayActive && reader.GameplayEnabled && Time.timeScale == originalScale, "native Pause restores gameplay/time");
                    Check(!overlay.activeSelf && pause.IsListening && reader.PauseAction.enabled, "native Pause closes without disabling listener: " + device);
                }
                PressKey(Key.Escape);
                ((Button)Field(pause, "_continue")).onClick.Invoke();
                Check(!pause.IsOpen && pause.IsListening && reader.GameplayEnabled && Time.timeScale == originalScale,
                    "Continue button restores gameplay/time and preserves listener");
                if (journal != null) { journal.Open(); pause.Open(); Check(pause.IsOpen && !journal.IsOpen && !journal.enabled, "Pause closes/disables open Journal"); pause.Resume(); Check(journal.enabled && reader.GameplayEnabled, "Journal re-enabled after Resume"); }
                pause.Open(); pause.ShowSettings();
                // Exercise the actual existing UI handlers without writing the user's settings file.
                originalPersistence ??= Field(settings, "_persistence");
                typeof(SettingsMenuController).GetField("_persistence", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, new GamePersistenceService(tempRoot));
                ((TMP_Dropdown)Field(settings, "_cameraMode")).value = original.CameraMode == CameraMode.FirstPerson ? 1 : 0;
                Check(applier.CameraMode != original.CameraMode && player.GetComponent<PlayerCameraController>().Mode == applier.CameraMode, "runtime CameraMode applied while paused");
                ((TMP_Dropdown)Field(settings, "_reticleMode")).value = original.ReticleMode == ReticleMode.Cross ? 1 : 2;
                Check(UnityEngine.Object.FindAnyObjectByType<ReticleSettings>().Mode == applier.CurrentSettings.ReticleMode, "runtime reticle preference applied");
                ((Slider)Field(settings, "_mouseSensitivity")).value = .2f;
                ((Slider)Field(settings, "_gamepadSensitivity")).value = 200;
                ((Toggle)Field(settings, "_invertY")).isOn = !original.InvertY;
                var look = player.GetComponent<PlayerLook>();
                Check(Mathf.Approximately(look.MouseSensitivity, .2f) && Mathf.Approximately(look.GamepadSensitivity, 200) && look.InvertY == !original.InvertY, "runtime sensitivities and InvertY");
                ((Slider)Field(settings, "_masterVolume")).value = .5f;
                ((Slider)Field(settings, "_musicVolume")).value = .4f;
                ((Slider)Field(settings, "_sfxVolume")).value = .6f;
                Check(Mathf.Approximately(AudioListener.volume, .5f), "supported runtime master volume");
                var saved = new GamePersistenceService(tempRoot).LoadSettings();
                Check(saved.CameraMode == applier.CameraMode && saved.InvertY == look.InvertY && saved.ReticleMode == applier.CurrentSettings.ReticleMode, "same settings format persists actual edits");
                Check(Mathf.Approximately(saved.MusicVolume, .4f) && Mathf.Approximately(saved.SfxVolume, .6f), "music/SFX preferences persist without a mixer");
                var mouseSlider = (Slider)Field(settings, "_mouseSensitivity");
                var beforeValue = mouseSlider.value;
                ExecuteEvents.Execute(mouseSlider.gameObject, new AxisEventData(events) { moveDir = MoveDirection.Right }, ExecuteEvents.moveHandler);
                Check(mouseSlider.value > beforeValue && Mathf.Approximately(look.MouseSensitivity, mouseSlider.value), "native slider navigation updates runtime preference at timeScale zero");
                pause.Back(); Check(events.currentSelectedGameObject == ((Button)Field(pause, "_configuration")).gameObject, "Back restores Settings selection");
                pause.Resume(); pause.Open(); pause.enabled = false;
                Check(!pause.IsOpen && Time.timeScale == originalScale && reader.GameplayEnabled, "disable restores pause lifecycle"); pause.enabled = true;
                Check(pause.CanRestart == (Field(pause, "_tutorial") != null), "Tutorial restart available; Level01 checkpoint unavailable by approved scope");
            }
            finally
            {
                pause.Resume();
                if (originalPersistence != null) typeof(SettingsMenuController).GetField("_persistence", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, originalPersistence);
                applier.Apply(original); Time.timeScale = originalScale; AudioListener.volume = originalVolume;
                Cursor.lockState = originalCursor; Cursor.visible = originalVisible;
                actions.devices = oldDevices;
                InputSystem.RemoveDevice(keyboard); foreach (var pad in pads) InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }
    }
}
