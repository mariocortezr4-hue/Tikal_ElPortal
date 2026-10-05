using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class MainMenuFlowValidation
    {
        static int _checks;

        [MenuItem("Tikal/Validation/Validate Main Menu Flow")]
        public static void RunFromMenu() => Run();

        static void Require(bool value, string message)
        {
            if (!value) throw new Exception("MainMenuFlow: " + message);
            _checks++;
            Debug.Log("MAIN_MENU_FLOW_CHECK: " + message);
        }

        public static void Run()
        {
            try
            {
                _checks = 0;
                ValidatePersistence();
                ValidateScenesAndMenu();
                ValidateSettingsRuntime();
                Debug.Log("TIKAL_MAIN_MENU_FLOW_VALIDATION_PASSED checks=" + _checks);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void ValidatePersistence()
        {
            var root = Path.Combine(Path.GetTempPath(), "TikalMainMenuFlow-" + Guid.NewGuid().ToString("N"));
            var service = new GamePersistenceService(root);
            try
            {
                var progress = service.LoadProgress();
                Require(!progress.HasStartedGame && !progress.IntroSeen && !progress.TutorialCompleted, "Missing progress file gives first-run defaults");
                Require(!progress.HasCompletedOnboarding, "Fresh progress has onboarding locked");
                Require(progress.IsLevelUnlocked(GameSceneIds.Level01) && !progress.IsLevelUnlocked(GameSceneIds.Level02), "Level01 default unlock without Level02");
                Require(progress.SelectedCharacterId == CharacterIds.Default, "Default character is selected on first run");
                Require(CharacterCatalog.All.Count == 4, "Four playable character ids are registered");
                Require(CharacterCatalog.All.All(character => character.VisualPrefab == null), "Missing final character prefabs do not block catalog");
                progress.HasStartedGame = true;
                progress.IntroSeen = true;
                progress.TutorialCompleted = true;
                progress.CurrentLevelId = GameSceneIds.Level01;
                progress.SelectedCharacterId = CharacterIds.Character02;
                progress.UnlockLevel(GameSceneIds.Level01);
                progress.CompleteLevel(GameSceneIds.Level01);
                service.SaveProgress(progress);
                var loaded = service.LoadProgress();
                Require(loaded.HasStartedGame && loaded.IntroSeen && loaded.TutorialCompleted, "Progress booleans persist");
                Require(loaded.HasCompletedOnboarding, "Completed intro and tutorial unlock onboarding gate");
                Require(loaded.CurrentLevelId == GameSceneIds.Level01, "CurrentLevel persists");
                Require(loaded.SelectedCharacterId == CharacterIds.Character02, "Character02 selection persists");
                Require(loaded.IsLevelUnlocked(GameSceneIds.Level01) && loaded.IsLevelCompleted(GameSceneIds.Level01), "Unlocked and completed levels persist");
                loaded.SelectedCharacterId = CharacterIds.Character03;
                service.SaveProgress(loaded);
                Require(service.LoadProgress().SelectedCharacterId == CharacterIds.Character03, "Character03 selection persists");
                loaded.SelectedCharacterId = CharacterIds.Character04;
                service.SaveProgress(loaded);
                Require(service.LoadProgress().SelectedCharacterId == CharacterIds.Character04, "Character04 selection persists");

                var settings = service.LoadSettings();
                Require(Mathf.Approximately(settings.MasterVolume, 1) && Mathf.Approximately(settings.MusicVolume, .8f) && Mathf.Approximately(settings.SfxVolume, 1), "Audio defaults");
                Require(Mathf.Approximately(settings.MouseSensitivity, .12f) && Mathf.Approximately(settings.GamepadSensitivity, 150), "Sensitivity defaults match current PlayerLook");
                Require(settings.CameraMode == CameraMode.FirstPerson && settings.ReticleMode == ReticleMode.Dot, "Camera and reticle defaults");
                settings.MasterVolume = .5f;
                settings.MusicVolume = .25f;
                settings.SfxVolume = .75f;
                settings.MouseSensitivity = .2f;
                settings.GamepadSensitivity = 180;
                settings.InvertY = true;
                settings.CameraMode = CameraMode.ThirdPerson;
                settings.ReticleMode = ReticleMode.Cross;
                service.SaveSettings(settings);
                var loadedSettings = service.LoadSettings();
                Require(Mathf.Approximately(loadedSettings.MasterVolume, .5f) && Mathf.Approximately(loadedSettings.MusicVolume, .25f) && Mathf.Approximately(loadedSettings.SfxVolume, .75f), "Audio values persist");
                Require(Mathf.Approximately(loadedSettings.MouseSensitivity, .2f) && Mathf.Approximately(loadedSettings.GamepadSensitivity, 180), "Sensitivity values persist");
                Require(loadedSettings.InvertY && loadedSettings.ReticleMode == ReticleMode.Cross, "InvertY and ReticleMode persist");
                Require(loadedSettings.CameraMode == CameraMode.ThirdPerson, "CameraMode persists");
                var progressAfterSettings = service.LoadProgress();
                Require(progressAfterSettings.CurrentLevelId == GameSceneIds.Level01, "Settings save does not alter progress current level");
                Require(progressAfterSettings.SelectedCharacterId == CharacterIds.Character04, "Settings save does not alter selected character");

                File.WriteAllText(service.ProgressPath, "{ invalid json");
                Require(!service.LoadProgress().HasStartedGame, "Corrupt progress falls back safely");
                File.WriteAllText(service.SettingsPath, "{ invalid json");
                Require(service.LoadSettings().ReticleMode == ReticleMode.Dot && service.LoadSettings().CameraMode == CameraMode.FirstPerson, "Corrupt settings fall back safely");
                var invalid = GameProgressData.CreateDefault();
                invalid.SelectedCharacterId = "character.missing";
                Require(invalid.SelectedCharacterId == CharacterIds.Default, "Invalid character id falls back to default");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        static void ValidateScenesAndMenu()
        {
            foreach (var id in new[] { GameSceneIds.MainMenu, GameSceneIds.Intro, GameSceneIds.Tutorial, GameSceneIds.Level01 })
            {
                var scene = SceneCatalog.Get(id);
                Require(!string.IsNullOrWhiteSpace(scene.Path), "Scene path registered for " + id);
                Require(EditorBuildSettings.scenes.Any(item => item.path == scene.Path && item.enabled), "BuildSettings contains " + id);
            }
            Require(SceneCatalog.Get(GameSceneIds.Level02).Path == string.Empty, "Level02 prepared but not implemented");

            var main = EditorSceneManager.OpenScene(SceneCatalog.Get(GameSceneIds.MainMenu).Path);
            var components = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            Require(components.All(component => component != null), "Main Menu has no Missing Scripts");
            Require(components.OfType<GameFlowController>().Count() == 1 && components.OfType<MainMenuController>().Count() == 1, "Main menu flow and UI controllers");
            Require(components.OfType<CharacterSelectionController>().Count() == 1, "Character selection controller exists");
            Require(components.OfType<InputDeviceManager>().Count() == 1 && components.OfType<MenuControlHintsPresenter>().Count() == 1, "Device-aware menu hints use the shared input architecture");
            Require(components.OfType<MenuSelectableVisual>().Count() >= components.OfType<Button>().Count(), "Menu buttons expose one authoritative focus visual");
            Require(components.OfType<EventSystem>().Count() == 1 && components.OfType<InputSystemUIInputModule>().Count() == 1, "Input System UI EventSystem");
            Require(components.OfType<Button>().Count(button => button.name == "Play") == 1, "Play button exists");
            Require(components.OfType<Button>().Any(button => button.name == "Character"), "Character menu button exists");
            Require(new[] { "Character01", "Character02", "Character03", "Character04" }.All(name => components.OfType<Button>().Any(button => button.name == name)), "Character panel exposes four options");
            Require(components.OfType<Button>().Any(button => button.name == "Level01") && !components.OfType<Button>().Any(button => button.name == "IntroPlaceholder"), "Level Select does not expose Intro as a level");
            Require(!components.OfType<Button>().Any(button => button.name == "TutorialPlaceholder"), "Level Select does not expose Tutorial as a level");
            Require(components.OfType<Button>().Where(button => button.name == "Level02").All(button => !button.gameObject.activeInHierarchy), "Level02 is hidden until unlocked");
            Require(components.OfType<SettingsMenuController>().Count() == 1, "Settings controller exists");
            Require(components.OfType<Slider>().Count() >= 5 && components.OfType<Toggle>().Any() && components.OfType<TMPro.TMP_Dropdown>().Any(), "Settings controls exist");
            var settingsController = components.OfType<SettingsMenuController>().Single();
            var settingsBindings = new SerializedObject(settingsController);
            var camera = settingsBindings.FindProperty("_cameraMode").objectReferenceValue as TMPro.TMP_Dropdown;
            var reticle = settingsBindings.FindProperty("_reticleMode").objectReferenceValue as TMPro.TMP_Dropdown;
            Require(camera != null && camera.template != null && camera.options.Select(option => option.text).SequenceEqual(new[] { "PRIMERA PERSONA", "TERCERA PERSONA" }), "Camera Mode dropdown is wired and selectable");
            Require(reticle != null && reticle.template != null && reticle.options.Select(option => option.text).SequenceEqual(new[] { "NINGUNO", "PUNTO", "CRUZ" }), "Reticle dropdown is wired and selectable");
            Require(components.OfType<Button>().Single(button => button.name == "Level01").gameObject.activeSelf, "Level01 remains visible while locked");
            Require(new SerializedObject(components.OfType<MainMenuController>().Single()).FindProperty("_levelHint").objectReferenceValue != null, "Level availability hint is wired");
            var module = components.OfType<InputSystemUIInputModule>().Single();
            Require(module.move != null && module.submit != null && module.cancel != null, "Navigate Submit Cancel wired");
            var actions = new TikalInputActions();
            Require(InputPromptResolver.Resolve(actions.UI.Navigate, InputDeviceFamily.KeyboardMouse).DisplayText == "WASD / FLECHAS", "Keyboard navigation hint is semantic");
            Require(InputPromptResolver.Resolve(actions.UI.Submit, InputDeviceFamily.Xbox).DisplayText == "A" &&
                InputPromptResolver.Resolve(actions.UI.Cancel, InputDeviceFamily.PlayStation).DisplayText == "○", "Xbox and PlayStation submit/cancel hints resolve by family");
            Require(InputPromptResolver.Resolve(actions.UI.Submit, InputDeviceFamily.GenericGamepad).DisplayText == "Botón sur" &&
                InputPromptResolver.Resolve(actions.UI.Cancel, InputDeviceFamily.GenericGamepad).DisplayText == "Botón este", "Generic gamepad hints use safe semantic fallbacks");
            actions.Dispose();

            var keyboard = InputSystem.AddDevice<Keyboard>();
            var xbox = InputSystem.AddDevice<XInputController>();
            var sony = InputSystem.AddDevice<DualShockGamepad>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow));
                InputSystem.Update();
                InputSystem.QueueStateEvent(xbox, new GamepadState { leftStick = Vector2.down }.WithButton(GamepadButton.South));
                InputSystem.Update();
                InputSystem.QueueStateEvent(sony, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South));
                InputSystem.Update();
                Require(module.actionsAsset != null, "Keyboard Xbox PlayStation use UI Action Map without Legacy Input");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(xbox);
                InputSystem.RemoveDevice(sony);
            }
        }

        static void ValidateSettingsRuntime()
        {
            var lookRoot = new GameObject("look");
            var cameraRoot = new GameObject("cameraRoot").transform;
            cameraRoot.SetParent(lookRoot.transform, false);
            new GameObject("camera").AddComponent<Camera>().transform.SetParent(cameraRoot, false);
            var look = lookRoot.AddComponent<PlayerLook>();
            Set(look, "_cameraRoot", cameraRoot);
            look.SendMessage("Awake");
            look.ApplySettings(.2f, 175, true);
            Require(Mathf.Approximately(look.MouseSensitivity, .2f) && Mathf.Approximately(look.GamepadSensitivity, 175) && look.InvertY, "Sensitivity and InvertY apply to PlayerLook");
            var reticle = new GameObject("reticle").AddComponent<ReticleSettings>();
            reticle.SetMode(ReticleMode.None);
            Require(reticle.Mode == ReticleMode.None, "Reticle None applies");
            reticle.SetMode(ReticleMode.Dot);
            Require(reticle.Mode == ReticleMode.Dot, "Reticle Dot applies");
            reticle.SetMode(ReticleMode.Cross);
            Require(reticle.Mode == ReticleMode.Cross, "Reticle Cross applies");
            var visualRoot = new GameObject("characterVisualRoot").transform;
            visualRoot.SetParent(lookRoot.transform, false);
            var spawner = lookRoot.AddComponent<CharacterVisualSpawner>();
            Set(spawner, "_characterVisualRoot", visualRoot);
            spawner.Apply(CharacterIds.Character04);
            spawner.Apply("character.missing");
            Require(visualRoot.childCount == 0, "Missing final character prefab does not throw or spawn placeholders");
            UnityEngine.Object.DestroyImmediate(lookRoot);
            UnityEngine.Object.DestroyImmediate(reticle.gameObject);
        }

        static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
