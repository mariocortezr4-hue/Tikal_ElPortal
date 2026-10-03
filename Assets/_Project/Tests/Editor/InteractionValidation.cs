using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Tests.Support;
using Tikal.Interaction;
using Tikal.Interaction.Debugging;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class InteractionValidation
    {
        private const string ScenePath = "Assets/_Project/Scenes/Testing/InteractionTest.unity";
        private const string ActiveKey = "Tikal.Interaction.Validation.Active";
        private const string ResultKey = "Tikal.Interaction.Validation.Result";
        private static double _regressionDeadline;
        private static readonly MethodInfo FocusTick = typeof(PlayerInteraction).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo DoorTick = typeof(Door).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);

        static InteractionValidation() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem("Tikal/Tests/Universal interaction and player regressions")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Exit Play Mode before running interaction validation."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var components = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(component => component != null), "No Missing Scripts in InteractionTest.");
                var player = components.OfType<PlayerInteraction>().Single();
                var playerSettings = new SerializedObject(player);
                Require(playerSettings.FindProperty("_inputSource").objectReferenceValue is IPlayerInputReader, "Semantic input source assigned.");
                Require(playerSettings.FindProperty("_viewCamera").objectReferenceValue != null, "Explorer camera assigned.");
                Require(PrefabUtility.GetCorrespondingObjectFromSource(player.gameObject) != null, "Explorer is a prefab instance.");
                var door = components.OfType<Door>().Single();
                Require(PrefabUtility.GetCorrespondingObjectFromSource(door.gameObject) != null, "Door is a prefab instance.");
                Require(new SerializedObject(door).FindProperty("_pivot").objectReferenceValue != null, "Door hinge assigned.");
                Require(components.OfType<CharacterController>().Count() == 1 && components.OfType<Camera>().Count() == 1, "Valid Explorer and one camera.");
                foreach (var component in components.OfType<PlayerController>())
                    foreach (var field in new[] { "_inputReader", "_movement", "_look" })
                        Require(new SerializedObject(component).FindProperty(field).objectReferenceValue != null, "Explorer reference: " + field);
                var prompt = components.OfType<InteractionDebugPrompt>().Single();
                Require(new SerializedObject(prompt).FindProperty("_interaction").objectReferenceValue == player, "Development prompt observes interaction authority.");
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(ResultKey, 1);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    ValidateInteraction();
                    EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Testing/PlayerMovementTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    _regressionDeadline = EditorApplication.timeSinceStartup + 30;
                    EditorApplication.update += ValidateRegressionsWhenLoaded;
                    return;
                }
                catch (Exception exception) { Debug.LogException(exception); }
                EditorApplication.ExitPlaymode();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                var result = SessionState.GetInt(ResultKey, 1);
                SessionState.SetBool(ActiveKey, false);
                if (Application.isBatchMode) EditorApplication.Exit(result);
                else Debug.Log("Interaction validation result: " + result);
            }
        }

        private static void ValidateRegressionsWhenLoaded()
        {
            var loaded = SceneManager.GetActiveScene().path == "Assets/_Project/Scenes/Testing/PlayerMovementTest.unity";
            if (!loaded && EditorApplication.timeSinceStartup < _regressionDeadline) return;
            EditorApplication.update -= ValidateRegressionsWhenLoaded;
            try
            {
                Require(loaded, "Player regression scene loads within timeout.");
                ExplorerFeelValidation.ValidateCurrentPlayer();
                SessionState.SetInt(ResultKey, 0);
                Debug.Log("TIKAL_INTERACTION_VALIDATION_PASSED: 13 required interaction cases, occlusion, layers, gameplay gate, destroyed targets, E/G and gamepad separation; full Input/Player regression suite.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            EditorApplication.ExitPlaymode();
        }
        private static void ValidateInteraction()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            var reader = player.GetComponent<PlayerInputReader>();
            var camera = player.GetComponentInChildren<Camera>();
            var door = UnityEngine.Object.FindAnyObjectByType<Door>();
            var pivot = (Transform)new SerializedObject(door).FindProperty("_pivot").objectReferenceValue;
            var coordinator = player.GetComponent<PlayerController>();
            coordinator.enabled = false;
            reader.EnableGameplay();
            var actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
            var settings = InputSystem.settings;
            var oldBackground = settings.backgroundBehavior;
            var oldEditor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var oldDevices = actions.devices;
            actions.devices = new InputDevice[] { keyboard, gamepad };
            var focused = new List<IInteractable>();
            Action<IInteractable> observer = target => focused.Add(target);
            player.FocusChanged += observer;
            var created = new List<GameObject>();
            try
            {
                View(camera, new Vector3(0,1.3f,0.4f), Vector3.back); Refresh(player);
                View(camera, new Vector3(0,1.3f,0.4f), Vector3.forward); Refresh(player);
                Require(ReferenceEquals(player.CurrentInteractable, door) && ReferenceEquals(focused.Last(), door), "Raycast acquires parent Door from child collider.");
                var eventCount = focused.Count; Refresh(player);
                Require(focused.Count == eventCount, "Unchanged focus does not emit duplicate events.");
                View(camera, new Vector3(0,1.3f,-3), Vector3.forward); Refresh(player);
                Require(player.CurrentInteractable == null && focused.Last() == null, "Leaving range clears focus.");
                View(camera, new Vector3(-3,1.3f,0), Vector3.forward); Refresh(player);
                Require(player.CurrentInteractable == null, "Non-interactable collider has no focus.");
                View(camera, new Vector3(0,1.3f,0.4f), Vector3.back); Refresh(player);
                Require(player.CurrentInteractable == null, "Looking away clears focus.");

                var probe = Probe(new Vector3(4,0,2.4f)); created.Add(probe.gameObject);
                var other = Probe(new Vector3(6,0,2.4f)); created.Add(other.gameObject);
                View(camera, new Vector3(4,1.3f,0.4f), Vector3.forward); Refresh(player);
                Require(ReferenceEquals(player.CurrentInteractable, probe), "Arbitrary IInteractable acquired.");
                View(camera, new Vector3(6,1.3f,0.4f), Vector3.forward); Refresh(player);
                Require(ReferenceEquals(player.CurrentInteractable, other) && ReferenceEquals(focused.Last(), other), "A to B changes focus.");
                View(camera, new Vector3(4,1.3f,0.4f), Vector3.forward); Refresh(player);
                probe.SetInteractionAvailable(false);
                Press(keyboard, Key.E, player);
                Require(probe.InteractionCount == 0 && player.CurrentInteractable == null, "Unavailable object blocks interaction and focus.");
                probe.SetInteractionAvailable(true); Refresh(player);
                Press(keyboard, Key.E, player);
                Require(probe.InteractionCount == 1, "One InteractPressed executes exactly once.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update(); Refresh(player);
                Require(probe.InteractionCount == 2, "Gamepad Interact uses same contract.");
                Press(keyboard, Key.G, player);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update();
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update(); Refresh(player);
                Require(probe.InteractionCount == 2, "SecretCommand has no interaction consumer.");

                player.enabled = false;
                Require(player.CurrentInteractable == null, "Disable clears focus.");
                Press(keyboard, Key.E, null);
                Require(probe.InteractionCount == 2, "Disabled interaction unsubscribes input.");
                for (var index = 0; index < 5; index++)
                {
                    player.enabled = true; Refresh(player); Press(keyboard, Key.E, player);
                    Require(probe.InteractionCount == index + 3, "Enable/Disable does not duplicate subscriptions.");
                    player.enabled = false;
                }
                player.enabled = true; Refresh(player);
                reader.DisableGameplay(); Refresh(player); Press(keyboard, Key.E, player);
                Require(player.CurrentInteractable == null && probe.InteractionCount == 7, "Gameplay gate clears focus and blocks input.");
                reader.EnableGameplay(); Refresh(player);
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); created.Add(wall);
                wall.transform.position = new Vector3(4,1.3f,1.3f); wall.transform.localScale = new Vector3(1,2,0.2f); Refresh(player);
                Require(player.CurrentInteractable == null, "Solid non-interactable occludes target.");
                UnityEngine.Object.DestroyImmediate(wall); Refresh(player);
                Require(ReferenceEquals(player.CurrentInteractable, probe), "Focus returns when obstruction disappears.");
                var layers = typeof(PlayerInteraction).GetField("_interactionLayers", BindingFlags.Instance | BindingFlags.NonPublic);
                var oldLayers = (LayerMask)layers.GetValue(player); layers.SetValue(player, (LayerMask)0); Refresh(player);
                Require(player.CurrentInteractable == null, "Interaction layer mask respected.");
                layers.SetValue(player, oldLayers); Refresh(player);
                // A press queued before the player looks away must not act on last frame's target.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                View(camera, new Vector3(4,1.3f,0.4f), Vector3.back); Refresh(player);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Require(probe.InteractionCount == 7, "Queued input does not execute on stale focus.");
                View(camera, new Vector3(4,1.3f,0.4f), Vector3.forward); Refresh(player);
                UnityEngine.Object.DestroyImmediate(probe); Refresh(player);
                Require(player.CurrentInteractable == null, "Destroyed target safely clears focus.");

                View(camera, new Vector3(0,1.3f,0.4f), Vector3.forward); Refresh(player);
                Require(door.State == Door.DoorState.Closed && door.InteractionText == "Abrir", "Initial door state and semantic prompt.");
                var closed = pivot.localRotation;
                Press(keyboard, Key.G, player);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update();
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update(); Refresh(player);
                Require(door.State == Door.DoorState.Closed && Quaternion.Angle(closed,pivot.localRotation) < 0.01f, "G and North leave Door unchanged.");
                Press(keyboard, Key.E, player);
                Require(door.State == Door.DoorState.Opening && door.InteractionText == "Cerrar" && Quaternion.Angle(closed,pivot.localRotation) < 0.01f, "Door begins opening without teleporting.");
                Animate(door, 0.15f);
                Require(Quaternion.Angle(closed,pivot.localRotation) > 0 && Quaternion.Angle(closed,pivot.localRotation) < 89, "Door has intermediate rotation.");
                Press(keyboard, Key.E, player);
                Require(door.State == Door.DoorState.Closing, "Repeated press reverses opening.");
                Animate(door, 2f);
                Require(door.State == Door.DoorState.Closed && Quaternion.Angle(closed,pivot.localRotation) < 0.01f, "Door closes to original pose.");
                Press(keyboard, Key.E, player); Animate(door, 2f);
                Require(door.State == Door.DoorState.Open && door.InteractionText == "Cerrar", "Door fully opens.");
                // Door moves out of the original ray: aim at its collider's current world center.
                var leaf = door.transform.Find("Pivot/DoorMesh");
                View(camera, leaf.position - Vector3.forward * 2f, Vector3.forward); Refresh(player);
                Press(keyboard, Key.E, player); Animate(door, 2f);
                Require(door.State == Door.DoorState.Closed && door.InteractionText == "Abrir", "Door fully closes.");
                View(camera, new Vector3(0,1.3f,0.4f), Vector3.forward); Refresh(player);
                for (var index = 0; index < 8; index++)
                {
                    Press(keyboard, Key.E, player); Animate(door,0.02f);
                }
                Animate(door,2f);
                Require(door.State == Door.DoorState.Closed && Quaternion.Angle(closed,pivot.localRotation) < 0.01f, "Rapid repeated presses preserve consistent state.");
            }
            finally
            {
                player.FocusChanged -= observer;
                actions.devices = oldDevices;
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
                settings.backgroundBehavior = oldBackground;
                settings.editorInputBehaviorInPlayMode = oldEditor;
                foreach (var go in created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static InteractionTestProbe Probe(Vector3 position)
        {
            var root = new GameObject("InteractionProbe"); root.transform.position = position;
            var child = new GameObject("ColliderChild"); child.transform.SetParent(root.transform,false); child.transform.localPosition = Vector3.up * 1.3f;
            child.AddComponent<BoxCollider>();
            return root.AddComponent<InteractionTestProbe>();
        }
        private static void View(Camera camera, Vector3 position, Vector3 direction)
        {
            camera.transform.position = position; camera.transform.rotation = Quaternion.LookRotation(direction);
        }
        private static void Refresh(PlayerInteraction interaction)
        {
            Physics.SyncTransforms(); FocusTick.Invoke(interaction, null);
        }
        private static void Press(Keyboard keyboard, Key key, PlayerInteraction interaction)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            if (interaction != null) Refresh(interaction);
        }
        private static void Animate(Door door, float duration)
        {
            const float step = 0.02f;
            for (var remaining = duration; remaining > 0; remaining -= step) DoorTick.Invoke(door, new object[] { Mathf.Min(step, remaining) });
            Physics.SyncTransforms();
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
