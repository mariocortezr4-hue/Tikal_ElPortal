using System;
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
                Require(!sequence.Steps.Select(step => step.PromptAction).Except(expected).Any(), "SecretCommand is not a tutorial step");
                Require(components.OfType<TutorialExitGate>().Count() == 1, "Tutorial reuses one existing-style interactable exit gate");
                Require(components.OfType<PlayerInputReader>().Count() == 1 && components.OfType<InputDeviceManager>().Count() == 1, "Tutorial reuses Explorer input and device detection");
                Require(components.OfType<TutorialPromptPresenter>().Count() == 1, "Tutorial owns one device-aware objective presenter");
                Require(SceneCatalog.Get(GameSceneIds.Tutorial).Path == ScenePath, "GameFlow tutorial id routes to TUT_ExplorerBasics");
                var actions = new TikalInputActions();
                Require(InputPromptResolver.Resolve(actions.Player.Move, InputDeviceFamily.KeyboardMouse).DisplayText == "WASD", "Keyboard movement prompt resolves from Move");
                Require(InputPromptResolver.Resolve(actions.Player.Interact, InputDeviceFamily.Xbox).DisplayText == "X", "Xbox interaction prompt resolves from Interact");
                Require(InputPromptResolver.Resolve(actions.Player.Interact, InputDeviceFamily.PlayStation).DisplayText == "□", "PlayStation interaction prompt resolves from Interact");
                actions.Dispose();
                Debug.Log("TIKAL_EXPLORER_TUTORIAL_VALIDATION_PASSED");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Explorer tutorial: " + message);
        }
    }
}
