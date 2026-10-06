using System;
using System.IO;
using System.Reflection;
using Tikal.Input;
using Tikal.Player;
using Tikal.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class ThirdPersonMovementValidation
    {
        [MenuItem("Tikal/Validation/Validate Third Person Movement")]
        public static void Run()
        {
            try
            {
                var count = ThirdPersonMovementMathChecks.Run();
                if (Application.isPlaying)
                {
                    ValidateScene(SceneManager.GetActiveScene());
                    ValidatePhysics();
                    Debug.Log("TIKAL_THIRD_PERSON_MOVEMENT_VALID: " + count + " math checks + live references + isolated physics/FPS checks.");
                    return;
                }
                var prefab = PrefabUtility.LoadPrefabContents(ThirdPersonCameraFoundationAuthoring.ExplorerPath);
                try { ValidatePlayer(prefab.GetComponent<PlayerController>()); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                foreach (var path in ThirdPersonCameraFoundationAuthoring.GameplayScenes)
                {
                    var scene = EditorSceneManager.OpenPreviewScene(path);
                    try { ValidateScene(scene); }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
                var controller = File.ReadAllText("Assets/_Project/Code/Player/PlayerController.cs");
                Require(controller.Contains("_movement.TickThirdPerson") && controller.Contains("_movement.Tick(_input.Move") &&
                    controller.Contains("_playerCamera.MovementOrientation") && controller.Contains("!_input.GameplayEnabled"),
                    "TPS/FPS routing and shared input lock");
                Debug.Log("TIKAL_THIRD_PERSON_MOVEMENT_STATIC_VALID: " + count + " math checks + Explorer/Tutorial/Level01/testing references. Run again in Play Mode for physics checks.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static void ValidateScene(Scene scene)
        {
            ThirdPersonCameraFoundationValidation.ValidateScene(scene);
            foreach (var player in ThirdPersonCameraFoundationAuthoring.Components<PlayerController>(scene)) ValidatePlayer(player);
            foreach (var step in ThirdPersonCameraFoundationAuthoring.Components<TutorialStep>(scene))
                Require(step.HasRequiredReferences, "existing Tutorial step references: " + step.name);
        }

        private static void ValidatePlayer(PlayerController player)
        {
            ThirdPersonCameraFoundationValidation.ValidatePlayer(player);
            Require(player.GetComponents<CharacterController>().Length == 1 && player.GetComponents<PlayerMovement>().Length == 1,
                "one existing capsule and movement authority");
            Require(player.GetComponents<PlayerInputReader>().Length == 1 && player.GetComponents<PlayerCameraController>().Length == 1,
                "one input reader and camera controller");
            Require(player.GetComponent<PlayerCameraController>().MovementOrientation != null &&
                player.GetComponent<PlayerMovement>().TurnSmoothTime > 0, "camera movement basis and positive turn smoothing");
        }

        private static void ValidatePhysics()
        {
            var lockState = Cursor.lockState;
            var visible = Cursor.visible;
            var volume = AudioListener.volume;
            GameObject fixture = null;
            try
            {
                fixture = new GameObject("TPS movement temporary fixtures");
                fixture.transform.position = new Vector3(40000, 40000, 40000);
                fixture.SetActive(false);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath);
                var instance = UnityEngine.Object.Instantiate(prefab, fixture.transform);
                instance.transform.localPosition = Vector3.up * .05f;
                var camera = instance.GetComponentInChildren<Camera>(true);
                camera.enabled = false;
                camera.GetComponent<AudioListener>().enabled = false;
                var floor = new GameObject("floor", typeof(BoxCollider));
                floor.transform.SetParent(fixture.transform, false);
                floor.transform.localPosition = Vector3.down * .25f;
                floor.GetComponent<BoxCollider>().size = new Vector3(100, .5f, 100);
                var orientation = new GameObject("independent camera orientation").transform;
                orientation.SetParent(fixture.transform, false);
                orientation.rotation = Quaternion.Euler(0, 90, 0);
                fixture.SetActive(true);
                var movement = instance.GetComponent<PlayerMovement>();
                var look = instance.GetComponent<PlayerLook>();
                var rig = instance.GetComponent<PlayerCameraController>();
                rig.ApplyCameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
                var cameraYaw = rig.Yaw;
                Physics.SyncTransforms();
                Step(movement, Vector2.zero, orientation, 40);
                Require(movement.IsGrounded, "existing ground contact");
                var initial = instance.transform.position;
                movement.TickThirdPerson(Vector2.up, false, false, .02f, orientation);
                var yaw = instance.transform.eulerAngles.y;
                Require(yaw > 0 && yaw < 90, "smooth root turn from north toward east");
                Step(movement, Vector2.up, orientation, 60);
                var delta = instance.transform.position - initial;
                Require(delta.x > 3 && Mathf.Abs(delta.z) < .02f && Mathf.Abs(movement.CurrentSpeed - 4f) < .01f,
                    "east camera + W moves east at existing walk speed");
                Require(Mathf.Abs(Mathf.DeltaAngle(instance.transform.eulerAngles.y, 90)) < 1, "collider/visual/interaction root follows velocity");
                typeof(PlayerCameraController).GetMethod("UpdateThirdPersonPose", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rig, new object[] { .02f });
                Require(Mathf.Abs(Mathf.DeltaAngle(cameraYaw, rig.Yaw)) < .001f &&
                    Mathf.Abs(Mathf.DeltaAngle(cameraYaw, camera.transform.eulerAngles.y)) < .01f,
                    "root turn does not recenter the orbit camera");
                var facing = instance.transform.rotation;
                orientation.rotation = Quaternion.Euler(0, 270, 0);
                Step(movement, Vector2.zero, orientation, 30);
                Require(Quaternion.Angle(facing, instance.transform.rotation) < .001f, "rest does not follow orbit or finish residual turn");
                Step(movement, Vector2.one, orientation, 50);
                Require(movement.CurrentSpeed <= 4.001f, "diagonal cannot exceed walk speed");
                Step(movement, Vector2.up, orientation, 40, true);
                Require(movement.IsSprinting && Mathf.Abs(movement.CurrentSpeed - 6.5f) < .01f, "existing sprint reused");
                movement.RequestJump();
                movement.TickThirdPerson(Vector2.up, true, false, .02f, orientation);
                Require(movement.VerticalVelocity > 0 && !movement.IsGrounded, "existing jump reused");
                Step(movement, Vector2.up, orientation, 80);
                Step(movement, Vector2.up, orientation, 40, true, true);
                Require(movement.IsCrouching && !movement.IsSprinting && movement.CurrentSpeed <= 2.001f, "existing crouch priority/speed");
                Step(movement, Vector2.zero, orientation, 40);
                movement.StopMotion();
                rig.ApplyCameraMode(Tikal.Persistence.CameraMode.FirstPerson);
                instance.transform.rotation = Quaternion.Euler(0, 90, 0);
                initial = instance.transform.position;
                for (var frame = 0; frame < 60; frame++) movement.Tick(Vector2.up, false, false, .02f);
                delta = instance.transform.position - initial;
                Require(delta.x > 3 && Mathf.Abs(delta.z) < .02f && Mathf.Abs(movement.CurrentSpeed - 4f) < .01f,
                    "FPS remains root-relative, independent of camera basis");
                look.SynchronizePlayerYaw();
                look.Tick(new Vector2(10, 0), LookInputMode.Delta, .02f);
                Require(Mathf.Abs(Mathf.DeltaAngle(90, instance.transform.eulerAngles.y) - look.MouseSensitivity * 10) < .01f,
                    "FPS Look still rotates root yaw; resumed yaw does not snap");
                // Exercise the controller's modal-map guard with residual motion and a pending turn.
                rig.ApplyCameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
                instance.GetComponent<PlayerInputReader>().DisableGameplay();
                initial = instance.transform.position;
                facing = instance.transform.rotation;
                typeof(PlayerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance.GetComponent<PlayerController>(), null);
                Require(instance.transform.position == initial && instance.transform.rotation == facing, "disabled input map freezes movement/turn");
            }
            finally
            {
                if (fixture != null) { fixture.SetActive(false); UnityEngine.Object.Destroy(fixture); }
                Cursor.lockState = lockState;
                Cursor.visible = visible;
                AudioListener.volume = volume;
            }
        }

        private static void Step(PlayerMovement movement, Vector2 input, Transform orientation, int frames, bool sprint = false, bool crouch = false)
        {
            for (var frame = 0; frame < frames; frame++) movement.TickThirdPerson(input, sprint, crouch, .02f, orientation);
        }

        private static void Require(bool valid, string message) => ThirdPersonCameraFoundationValidation.Require(valid, message);
    }
}
