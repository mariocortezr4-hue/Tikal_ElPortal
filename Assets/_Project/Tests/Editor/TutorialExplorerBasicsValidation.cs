using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class TutorialExplorerBasicsValidation
    {
        const string ScenePath = "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity";

        [MenuItem("Tikal/Validation/Validate Explorer Tutorial")]
        public static void RunFromMenu()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(component => component != null), "Tutorial scene has no Missing Scripts");
                var sequence = components.OfType<TutorialSequenceController>().Single();
                Require(sequence.Steps.Count == 6, "Tutorial contains six steps");
                var expected = new[] { TutorialPromptAction.Move, TutorialPromptAction.Look, TutorialPromptAction.Jump, TutorialPromptAction.Crouch, TutorialPromptAction.Sprint, TutorialPromptAction.Interact };
                Require(sequence.Steps.Select(step => step.PromptAction).SequenceEqual(expected), "Tutorial step order is Move, Look, Jump, Crouch, Sprint, Interact");
                var expectedTypes = new[] { typeof(TutorialMovementStep), typeof(TutorialLookStep), typeof(TutorialJumpStep), typeof(TutorialCrouchPassageStep), typeof(TutorialSprintStep), typeof(TutorialInteractStep) };
                Require(sequence.Steps.Select(step => step.GetType()).SequenceEqual(expectedTypes), "Tutorial uses the expected concrete step type at every index");
                Require(!sequence.Steps.Select(step => step.PromptAction).Except(expected).Any(), "SecretCommand is not a tutorial step");
                Require(sequence.Steps.All(step => step != null && step.HasRequiredReferences), "Every tutorial step has its critical references");
                Require(sequence.Steps.All(step => !string.IsNullOrWhiteSpace(step.Title) && !string.IsNullOrWhiteSpace(step.Instruction)), "Every control step has visible guidance");
                Require(sequence.Steps.All(step => step.gameObject.activeInHierarchy), "Every tutorial step is active in the authored hierarchy");
                for (var index = 0; index < sequence.Steps.Count; index++)
                {
                    var step = sequence.Steps[index];
                    var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(step));
                    Require(Path.GetFileNameWithoutExtension(scriptPath) == step.GetType().Name, step.GetType().Name + " has a stable same-name MonoScript asset");
                    if (index < sequence.Steps.Count - 1)
                        Require(sequence.GetNextStep(step) == sequence.Steps[index + 1], step.PromptAction + " activates " + sequence.Steps[index + 1].PromptAction);
                }
                Require(sequence.GetNextStep(sequence.Steps[5]) == null, "Interact transitions to AwaitingExit instead of another control step");
                Require(sequence.GetNextStep(sequence.Steps[1]) is TutorialJumpStep, "Look explicitly advances to Jump");
                var jumpStep = (TutorialJumpStep)sequence.Steps[2];
                Require(jumpStep.PromptAction == TutorialPromptAction.Jump && jumpStep.Title == "SALTA", "Jump owns the SALTA presentation");
                Require(jumpStep.Instruction == "Supera el obstáculo.", "Jump has the obstacle instruction");
                Require(jumpStep.GetComponent<BoxCollider>() != null && jumpStep.GetComponent<BoxCollider>().isTrigger, "Jump completion uses its obstacle zone");
                var gate = components.OfType<TutorialExitGate>().Single();
                Require(sequence.ExitGate == gate, "Sequence owns the final exit phase");
                Require(gate.HasRequiredReferences, "Exit gate has its player, moving stone and trigger references");
                Require(gate.GetComponent<BoxCollider>().isTrigger, "Exit gate uses an unambiguous trigger");
                Require(!gate.ExitAvailable, "Exit gate starts locked until every control step is complete");
                var jumpObstacle = components.OfType<Transform>().Single(item => item.name == "Jump root");
                var crouchObstacle = components.OfType<Transform>().Single(item => item.name == "Low ruin lintel");
                Require(gate.transform.position.z > jumpObstacle.position.z && gate.transform.position.z > crouchObstacle.position.z, "Exit gate is after the control route");
                Require(components.OfType<Light>().Any(light => light.name == "Warm exit beacon"), "Exit route has a warm environmental beacon");
                Require(components.OfType<PlayerInputReader>().Count() == 1 && components.OfType<InputDeviceManager>().Count() == 1, "Tutorial reuses Explorer input and device detection");
                var presenter = components.OfType<TutorialPromptPresenter>().Single();
                Require(presenter.Sequence == sequence && presenter.HasRequiredReferences, "Prompt presenter is fully connected to the sequence and input services");
                Require(presenter.RemainsActiveDuringFeedback, "Prompt presenter stays subscribed while objective and completion panels switch");
                Require(SceneCatalog.Get(GameSceneIds.Tutorial).Path == ScenePath, "GameFlow tutorial id routes to TUT_ExplorerBasics");
                Require(GameFlowController.GetTutorialCompletionDestination(false) == GameSceneIds.Level01, "First-run tutorial exit routes to Level01");
                Require(GameFlowController.GetTutorialCompletionDestination(true) == GameSceneIds.MainMenu, "Tutorial replay exit routes to MainMenu");
                var actions = new TikalInputActions();
                foreach (var promptAction in expected)
                foreach (InputDeviceFamily family in Enum.GetValues(typeof(InputDeviceFamily)))
                    Require(!string.IsNullOrWhiteSpace(InputPromptResolver.Resolve(ActionFor(actions, promptAction), family).DisplayText), promptAction + " has prompt text for " + family);
                Require(InputPromptResolver.Resolve(actions.Player.Move, InputDeviceFamily.KeyboardMouse).DisplayText == "WASD", "Keyboard movement prompt resolves from Move");
                Require(InputPromptResolver.Resolve(actions.Player.Look, InputDeviceFamily.Xbox).DisplayText == "Right Stick", "Xbox look prompt resolves from Look");
                RequirePrompt(actions.Player.Jump, InputDeviceFamily.KeyboardMouse, "<Keyboard>/space", "Space", "Keyboard jump");
                RequirePrompt(actions.Player.Jump, InputDeviceFamily.Xbox, "<Gamepad>/buttonSouth", "A", "Xbox jump");
                RequirePrompt(actions.Player.Jump, InputDeviceFamily.PlayStation, "<Gamepad>/buttonSouth", "×", "PlayStation jump");
                RequirePrompt(actions.Player.Jump, InputDeviceFamily.GenericGamepad, "<Gamepad>/buttonSouth", "Botón sur", "Generic gamepad jump");
                RequirePrompt(actions.Player.Crouch, InputDeviceFamily.KeyboardMouse, "<Keyboard>/leftCtrl", null, "Keyboard crouch");
                RequirePrompt(actions.Player.Crouch, InputDeviceFamily.Xbox, "<Gamepad>/buttonEast", "B", "Xbox crouch");
                RequirePrompt(actions.Player.Sprint, InputDeviceFamily.KeyboardMouse, "<Keyboard>/leftShift", null, "Keyboard sprint");
                RequirePrompt(actions.Player.Sprint, InputDeviceFamily.Xbox, "<Gamepad>/leftStickPress", "LS", "Xbox sprint");
                RequirePrompt(actions.Player.Sprint, InputDeviceFamily.PlayStation, "<Gamepad>/leftStickPress", "L3", "PlayStation sprint");
                Require(InputPromptResolver.Resolve(actions.Player.Interact, InputDeviceFamily.Xbox).DisplayText == "X", "Xbox interaction prompt resolves from Interact");
                Require(InputPromptResolver.Resolve(actions.Player.Interact, InputDeviceFamily.PlayStation).DisplayText == "□", "PlayStation interaction prompt resolves from Interact");
                Require(InputPromptResolver.Resolve(actions.Player.Interact, InputDeviceFamily.GenericGamepad).DisplayText == "Botón oeste", "Generic interaction prompt resolves from Interact");
                actions.Dispose();
                Debug.Log("TIKAL_EXPLORER_TUTORIAL_VALIDATION_PASSED");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Explorer tutorial: " + message);
        }

        static void RequirePrompt(UnityEngine.InputSystem.InputAction action, InputDeviceFamily family, string path, string display, string label)
        {
            var prompt = InputPromptResolver.Resolve(action, family);
            Require(prompt.ControlPath == path, label + " uses the expected Input System binding");
            Require(!string.IsNullOrWhiteSpace(prompt.DisplayText), label + " has visible prompt text");
            if (display != null) Require(prompt.DisplayText == display, label + " uses the expected device label");
        }

        static UnityEngine.InputSystem.InputAction ActionFor(TikalInputActions actions, TutorialPromptAction prompt)
        {
            switch (prompt)
            {
                case TutorialPromptAction.Move: return actions.Player.Move;
                case TutorialPromptAction.Look: return actions.Player.Look;
                case TutorialPromptAction.Jump: return actions.Player.Jump;
                case TutorialPromptAction.Crouch: return actions.Player.Crouch;
                case TutorialPromptAction.Sprint: return actions.Player.Sprint;
                default: return actions.Player.Interact;
            }
        }
    }
}
