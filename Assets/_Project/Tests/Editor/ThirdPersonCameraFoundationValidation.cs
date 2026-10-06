using System;
using System.IO;
using System.Linq;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tutorial;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class ThirdPersonCameraFoundationValidation
    {
        [MenuItem("Tikal/Validation/Validate Third Person Camera Foundation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                try { ValidateScene(SceneManager.GetActiveScene()); Debug.Log("TIKAL_THIRD_PERSON_CAMERA_RUNTIME_REFERENCES_VALID"); }
                catch (Exception exception) { Debug.LogException(exception); }
                return;
            }
            try
            {
                var prefab = PrefabUtility.LoadPrefabContents(ThirdPersonCameraFoundationAuthoring.ExplorerPath);
                try
                {
                    var player = prefab.GetComponent<PlayerController>();
                    ValidatePlayer(player);
                    ThirdPersonCameraFoundationAuthoring.ConfigurePlayer(player, player.GetComponent<PlayerCameraController>().Profile);
                    var first = Fingerprint(prefab);
                    for (var pass = 0; pass < 5; pass++)
                        ThirdPersonCameraFoundationAuthoring.ConfigurePlayer(player, player.GetComponent<PlayerCameraController>().Profile);
                    Require(first == Fingerprint(prefab), "authoring is structurally idempotent after one/five repeats");
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                foreach (var path in ThirdPersonCameraFoundationAuthoring.GameplayScenes)
                {
                    var scene = EditorSceneManager.OpenPreviewScene(path);
                    try { ValidateScene(scene); }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                ValidateSourceContracts();
                Debug.Log("TIKAL_THIRD_PERSON_CAMERA_VALID: prefab + gameplay scene references + source contracts. Run Play Mode behavior checks separately.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static string Fingerprint(GameObject root) => string.Join("\n",
            root.GetComponentsInChildren<Transform>(true).SelectMany(t =>
                t.GetComponents<Component>().Select(c => c.GetType().FullName + EditorJsonUtility.ToJson(c))));

        internal static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("TPS validation: " + message);
        }

        internal static void ValidateScene(Scene scene)
        {
            var players = ThirdPersonCameraFoundationAuthoring.Components<PlayerController>(scene);
            Require(players.Length == 1, scene.path + ": one Explorer");
            ValidatePlayer(players[0]);
            var cameras = ThirdPersonCameraFoundationAuthoring.Components<Camera>(scene).Where(c => c.enabled && c.gameObject.activeInHierarchy).ToArray();
            Require(cameras.Length == 1 && cameras[0].CompareTag("MainCamera"), scene.path + ": exactly one active MainCamera");
            Require(ThirdPersonCameraFoundationAuthoring.Components<AudioListener>(scene).Count(l => l.enabled && l.gameObject.activeInHierarchy) == 1,
                scene.path + ": exactly one active AudioListener");
            foreach (var root in scene.GetRootGameObjects()) ValidateMissingScripts(root);
            foreach (var lookStep in ThirdPersonCameraFoundationAuthoring.Components<TutorialLookStep>(scene))
                Require(lookStep.HasRequiredReferences, "Tutorial Look references preserved");
            var appliers = ThirdPersonCameraFoundationAuthoring.Components<PersistentSettingsApplier>(scene);
            Require(appliers.Count(a => a.enabled && a.gameObject.activeInHierarchy) == 1, "one active existing settings flow");
            foreach (var reticle in ThirdPersonCameraFoundationAuthoring.Components<ReticlePresenter>(scene))
            {
                var serialized = new SerializedObject(reticle);
                foreach (var field in new[] { "_settings", "_inputReader", "_dot", "_cross" })
                    Require(serialized.FindProperty(field).objectReferenceValue != null, "existing reticle reference " + field);
            }
        }

        internal static void ValidatePlayer(PlayerController player)
        {
            Require(player != null, "PlayerController exists");
            var controller = player.GetComponent<PlayerCameraController>();
            Require(controller != null && controller.enabled && controller.IsConfigured, "configured PlayerCameraController exists");
            Require(player.GetComponent<PlayerInputReader>() != null && player.GetComponent<PlayerLook>().IsConfigured &&
                player.GetComponent<PlayerMovement>() != null && player.GetComponent<CharacterController>() != null, "existing player components preserved");
            var serialized = new SerializedObject(controller);
            var camera = (Camera)serialized.FindProperty("_mainCamera").objectReferenceValue;
            var fps = (Transform)serialized.FindProperty("_firstPersonTarget").objectReferenceValue;
            var pivot = (Transform)serialized.FindProperty("_thirdPersonPivot").objectReferenceValue;
            var lookTarget = (Transform)serialized.FindProperty("_lookTarget").objectReferenceValue;
            var visual = (GameObject)serialized.FindProperty("_characterVisualRoot").objectReferenceValue;
            var proxy = (GameObject)serialized.FindProperty("_thirdPersonHumanProxy").objectReferenceValue;
            var applier = (PersistentSettingsApplier)serialized.FindProperty("_settingsApplier").objectReferenceValue;
            Require(applier.enabled && new SerializedObject(applier).FindProperty("_playerLook").objectReferenceValue == player.GetComponent<PlayerLook>(),
                "single settings source targets the existing PlayerLook");
            Require(player.GetComponentsInChildren<Camera>(true).Length == 1 && camera.CompareTag("MainCamera"), "unique MainCamera");
            Require(player.GetComponentsInChildren<AudioListener>(true).Length == 1 && camera.GetComponent<AudioListener>() != null &&
                camera.GetComponent<AudioListener>().enabled, "unique AudioListener on camera");
            Require(fps.name == "FirstPersonTarget" && pivot.name == "ThirdPersonPivot" && lookTarget.name == "LookTarget", "three logical camera targets");
            Require(fps.IsChildOf(player.GetComponent<PlayerLook>().CameraRoot) && pivot.IsChildOf(player.transform) && lookTarget.IsChildOf(player.transform), "stable controller-owned targets");
            Require(visual.name == "CharacterVisualRoot" && proxy.name == "ThirdPersonHumanProxy" && proxy.transform.IsChildOf(visual.transform), "replaceable visual hierarchy");
            Require(proxy.GetComponentsInChildren<Renderer>(true).Length >= 2 && proxy.transform.Find("Head") != null && proxy.transform.Find("Body") != null, "humanoid proxy");
            Require(proxy.GetComponentsInChildren<Collider>(true).Length == 0, "proxy cannot occlude camera or interaction");
            Require(controller.Profile.IsValid, "distance/minimum, collision radius/mask, clamp and smoothing profile valid");
            Require(Enum.IsDefined(typeof(CameraMode), CameraMode.FirstPerson) && Enum.IsDefined(typeof(CameraMode), CameraMode.ThirdPerson), "existing persisted camera modes");
            ValidateMissingScripts(player.gameObject);
            if (!Application.isPlaying)
            {
                var oldMode = controller.Mode;
                var visualActive = visual.activeSelf;
                var proxyActive = proxy.activeSelf;
                try
                {
                    controller.ApplyCameraMode(CameraMode.ThirdPerson);
                    Require(visual.activeSelf && proxy.activeSelf, "proxy visible in TPS");
                    controller.ApplyCameraMode(CameraMode.FirstPerson);
                    Require(!visual.activeSelf && !proxy.activeSelf, "proxy hidden in FPS");
                }
                finally
                {
                    controller.ApplyCameraMode(oldMode);
                    visual.SetActive(visualActive);
                    proxy.SetActive(proxyActive);
                }
            }
            else Require(visual.activeSelf == controller.OwnsLook && proxy.activeSelf == controller.OwnsLook, "live mode visibility");
        }

        private static void ValidateMissingScripts(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no Missing Scripts: " + transform.name);
        }

        private static void ValidateSourceContracts()
        {
            var camera = File.ReadAllText("Assets/_Project/Code/Player/PlayerCameraController.cs");
            Require(camera.Contains("QueryTriggerInteraction.Ignore") && camera.Contains("IsChildOf(transform)"), "ignore triggers and own hierarchy");
            Require(camera.Contains("_look.MouseSensitivity") && camera.Contains("_look.GamepadSensitivity") && camera.Contains("_look.InvertY"), "existing sensitivity/InvertY reused");
            Require(camera.Contains("_player.GameplayActive") && camera.Contains("_input.GameplayEnabled"), "shared Pause/Journal/modal gate");
            Require(camera.Contains("SettingsApplied +=") && camera.Contains("SettingsApplied -=") && !camera.Contains("PlayerPrefs") && !camera.Contains("ReticleMode"), "event settings; no extra preference/reticle system");
            Require(!camera.Contains("Keyboard.current") && !camera.Contains("Mouse.current") && !camera.Contains("Gamepad.current") && !camera.Contains("Animator"), "semantic input; no Animator dependency");
            Require(!File.ReadAllText("Assets/_Project/Code/Tutorial/TutorialLookStep.cs").Contains("SecretCommand"), "Tutorial Look adds no SecretCommand");
            Require(!File.ReadAllText("Assets/_Project/Tests/Editor/ThirdPersonCameraFoundationAuthoring.cs").Contains("Intro_AncientMap"), "cinematics excluded from authoring");
        }
    }
}
