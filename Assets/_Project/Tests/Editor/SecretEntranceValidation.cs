using System;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Puzzles.Clues;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class SecretEntranceValidation
    {
        private static int _checks;
        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException("Secret Entrance: " + message);
            _checks++;
        }
        private static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Tick(Door door, float seconds) => typeof(Door).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(door, new object[] { seconds });

        [MenuItem("Tikal/Validation/Validate Level01 Secret Entrance")]
        public static void Run()
        {
            _checks = 0;
            try
            {
                if (Application.isPlaying) ValidateRuntime();
                else
                {
                    var scene = EditorSceneManager.OpenPreviewScene(SecretEntranceAuthoring.ScenePath);
                    try { ValidateScene(scene); }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                Debug.Log("TIKAL_SECRET_ENTRANCE_VALID checks=" + _checks +
                    (Application.isPlaying ? " runtime matrix + native speedrun victory" : " scene references; run again in Play Mode at fresh Level01"));
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        internal static void ValidateScene(Scene scene)
        {
            var receivers = SecretEntranceAuthoring.Components<SecretEntranceCommandReceiver>(scene);
            Check(receivers.Length == 1, "one secret receiver");
            var receiver = receivers[0];
            var door = SecretEntranceAuthoring.Components<Door>(scene).Single();
            var reader = SecretEntranceAuthoring.Components<PlayerInputReader>(scene).Single();
            Check(receiver.IsConfigured && receiver.isActiveAndEnabled && receiver.MainDoor == door, "configured active receiver and logical Main Door");
            Check(Field(receiver, "_inputReader") == (object)reader && Field(receiver, "_player") == (object)reader.transform,
                "same canonical semantic input and player");
            Check(Field(receiver, "_outcome") == (object)SecretEntranceAuthoring.Components<LevelOutcomeController>(scene).Single(), "canonical outcome reference");
            var zone = (BoxCollider)Field(receiver, "_zone");
            var target = (Transform)Field(receiver, "_target");
            Check(zone.isTrigger && zone.enabled && zone.gameObject.activeInHierarchy && !zone.transform.IsChildOf(door.transform) &&
                !target.IsChildOf(door.transform), "stable active trigger and target outside moving Door");
            var worldSize = Vector3.Scale(zone.size, zone.transform.lossyScale);
            Check(receiver.Range == 3 && worldSize.x > 0 && worldSize.x <= 3.4f + .01f && worldSize.y > 0 && worldSize.y <= 3.01f &&
                worldSize.z > 0 && worldSize.z <= 5.81f, "bounded entrance zone with 3m player reach");
            Check(Vector3.Distance(target.position, door.transform.position + Vector3.up * 1.6f) < 2.1f,
                "secret target near Main Door, not elsewhere in level");
            Check(typeof(SecretEntranceCommandReceiver).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .All(f => f.FieldType != typeof(Camera) && f.FieldType != typeof(PlayerInteraction)), "no camera or CurrentInteractable reference");
            var action = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions").FindAction("Player/SecretCommand", true);
            Check(action.bindings.Any(b => b.effectivePath == "<Keyboard>/g") && action.bindings.Any(b => b.effectivePath == "<Gamepad>/buttonNorth"),
                "unchanged G / Y / triangle / generic north bindings");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no Missing Scripts: " + transform.name);
        }

        private static void ValidateRuntime()
        {
            var receiver = UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
            Check(receiver != null && receiver.gameObject.scene.path == SecretEntranceAuthoring.ScenePath, "play fresh Level01 for the victory test");
            ValidateScene(receiver.gameObject.scene);
            var reader = (PlayerInputReader)Field(receiver, "_inputReader");
            var player = reader.GetComponent<PlayerController>();
            var outcome = (LevelOutcomeController)Field(receiver, "_outcome");
            var tracker = UnityEngine.Object.FindAnyObjectByType<ClueTracker>();
            Check(outcome.State == LevelOutcome.Playing && player.GameplayActive && reader.GameplayEnabled && tracker.Count == 0,
                "fresh active gameplay with zero clues (validation ends in victory)");
            var door = receiver.MainDoor;
            Check(door.State == Door.DoorState.Closed, "start with Door closed");
            var cameraController = reader.GetComponent<PlayerCameraController>();
            var applier = (PersistentSettingsApplier)Field(cameraController, "_settingsApplier");
            var originalSettings = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(applier.CurrentSettings));
            var originalPosition = reader.transform.position;
            var capsule = reader.GetComponent<CharacterController>();
            var wasCapsuleEnabled = capsule.enabled;
            var zone = (BoxCollider)Field(receiver, "_zone");
            var inside = zone.transform.TransformPoint(zone.center) - Vector3.up;
            var outside = inside + zone.transform.right * (zone.size.x * Mathf.Abs(zone.transform.lossyScale.x) * .5f + .2f);
            var camera = reader.GetComponentInChildren<Camera>();
            var cameraPosition = camera.transform.position; var cameraRotation = camera.transform.rotation;
            var actions = (TikalInputActions)Field(reader, "_actions"); var oldDevices = actions.devices;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pads = new Gamepad[] { InputSystem.AddDevice<XInputController>(), InputSystem.AddDevice<DualShockGamepad>(), InputSystem.AddDevice<Gamepad>() };
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            int received = 0;
            void Count() => received++;
            void Send(int device)
            {
                if (device < 0) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G)); InputSystem.Update(); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); }
                else { InputSystem.QueueStateEvent(pads[device], new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update(); InputSystem.QueueStateEvent(pads[device], new GamepadState()); }
                InputSystem.Update();
            }
            try
            {
                capsule.enabled = false;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                actions.devices = new InputDevice[] { keyboard, pads[0], pads[1], pads[2] };
                // Isolate semantic routing checks from the terminal victory side effect.
                receiver.enabled = false; reader.SecretCommandPressed += Count;
                for (int device = -1; device < pads.Length; device++)
                {
                    var before = received; Send(device);
                    Check(received == before + 1, "native semantic SecretCommand routed once: " + device);
                }
                reader.SecretCommandPressed -= Count; receiver.enabled = true;
                foreach (var mode in new[] { CameraMode.FirstPerson, CameraMode.ThirdPerson })
                {
                    var settings = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(originalSettings)); settings.CameraMode = mode;
                    applier.Apply(settings);
                    Check(cameraController.Mode == mode, "real camera mode applied for matrix: " + mode);
                    reader.transform.position = inside;
                    camera.transform.position = inside + new Vector3(40, 10, -40); camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                    Check(receiver.CanExecuteCommand, mode + " CLOSED + inside succeeds despite distant camera facing away");
                    reader.transform.position = outside;
                    for (int device = -1; device < pads.Length; device++) { Send(device); Check(outcome.State == LevelOutcome.Playing, mode + " outside rejects: " + device); }
                    reader.transform.position = inside;
                    door.Interact(); Check(door.State == Door.DoorState.Opening && !receiver.CanExecuteCommand, "Interact opens; opening rejects secret");
                    Tick(door, 10);
                    Check(door.State == Door.DoorState.Open && !receiver.CanExecuteCommand, mode + " OPEN rejects");
                    for (int device = -1; device < pads.Length; device++) { Send(device); Check(outcome.State == LevelOutcome.Playing && door.State == Door.DoorState.Open, mode + " open neither wins nor auto-closes: " + device); }
                    door.Interact(); Tick(door, .02f);
                    Check(door.State == Door.DoorState.Closing && !receiver.CanExecuteCommand, "Interact closes; closing rejects secret");
                    Tick(door, 10); Check(receiver.CanExecuteCommand, "Interact returns Door to closed and secret is permitted");
                    reader.DisableGameplay(); Check(!receiver.CanExecuteCommand, "gameplay lock blocks secret"); reader.EnableGameplay();
                }
                Check(tracker.Count == 0, "camera/device matrix does not discover Clue05 or require clues");
                Send(2);
                Check(outcome.State == LevelOutcome.Victory && tracker.Count == 0 && door.State == Door.DoorState.Closed,
                    "TPS native generic north executes production victory with Door closed, camera away and zero clues");
            }
            finally
            {
                reader.SecretCommandPressed -= Count; receiver.enabled = true;
                if (door.State == Door.DoorState.Open || door.State == Door.DoorState.Opening) door.Interact();
                Tick(door, 10);
                if (outcome.State == LevelOutcome.Playing && player.GameplayActive) reader.EnableGameplay();
                reader.transform.position = originalPosition; capsule.enabled = wasCapsuleEnabled;
                applier.Apply(originalSettings); camera.transform.position = cameraPosition; camera.transform.rotation = cameraRotation;
                actions.devices = oldDevices;
                InputSystem.RemoveDevice(keyboard); foreach (var pad in pads) InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            }
        }
    }
}
