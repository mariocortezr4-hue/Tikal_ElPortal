using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tutorial;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class TutorialGuidancePlayTests
    {
        [Serializable] public class Pose { public string id; public float roofY, maximumInsideRoofY, maxWalkingVisualY, standingHeight, standingCenter, crouchHeight, crouchCenter, blockedZ, finalZ; }
        [Serializable] public class Report { public bool passed; public Pose[] poses; public float boundaryJumpPeak, minimumCameraBoom, maximumCameraStep; public int recoveryCount, cameraContactAdjustments; public float parallelMinimumBoom, parallelMaximumStep; public string[] checks; }
        static void Input(Gamepad pad, Vector2 move, GamepadButton? button = null)
        {
            var state = new GamepadState { leftStick = move }; if (button.HasValue) state = state.WithButton(button.Value); InputSystem.QueueStateEvent(pad, state);
        }
        static void Teleport(PlayerController player, Vector3 position, float yaw = 0)
        {
            var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false; player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); capsule.enabled = true;
            player.GetComponent<PlayerMovement>().ResetForRespawn(); player.GetComponent<PlayerLook>().ResetAfterRespawn(1.62f, 1.7f); player.GetComponent<PlayerLook>().SynchronizePlayerYaw(); Physics.SyncTransforms(); player.GetComponent<PlayerCameraController>().SnapAfterTeleport();
        }
        static float InsideRoofTop(GameObject visual, Bounds roof)
        {
            float top = float.NegativeInfinity;
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights; var poses = mesh.bindposes;
                var matrices = skin.bones.Select((bone, i) => bone.localToWorldMatrix * poses[i]).ToArray();
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i]; var v = vertices[i]; var p = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                    if (p.x >= roof.min.x && p.x <= roof.max.x && p.z >= roof.min.z && p.z <= roof.max.z) top = Mathf.Max(top, p.y);
                }
            }
            return top;
        }
        public static IEnumerator Run(Gamepad pad, GamePersistenceService save, Action<bool, string> outerCheck)
        {
            var checks = new List<string>(); Action<bool, string> check = (v, m) => { outerCheck(v, m); checks.Add(m); };
            var poses = new List<Pose>(); var report = new Report();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); var movement = player.GetComponent<PlayerMovement>(); var camera = player.GetComponent<PlayerCameraController>(); var capsule = player.GetComponent<CharacterController>();
            var sequence = UnityEngine.Object.FindAnyObjectByType<TutorialSequenceController>(); var safety = UnityEngine.Object.FindAnyObjectByType<TutorialOutOfBoundsVolume>();
            camera.ApplyCameraMode(CameraMode.ThirdPerson);
            for (int i = 0; i < 10; i++) { Input(pad, Vector2.up); yield return null; }
            for (int i = 0; i < 15; i++) { Input(pad, Vector2.zero); yield return null; }
            var step = sequence.CurrentStep; var state = sequence.State; var saved = JsonUtility.ToJson(save.LoadProgress());
            player.SetGameplayActive(false); var safe = safety.SafePosition; check(safe.z > .4f, "Last safe point follows real grounded movement away from spawn"); int recovered = safety.RecoveryCount;
            Teleport(player, new Vector3(18, -7, 23));
            for (int i = 0; i < 3; i++) yield return null;
            check(safety.RecoveryCount == recovered + 1 && Vector3.Distance(player.transform.position, safe) < .15f, "OOB fall/outside returns to last grounded safe point");
            check(sequence.CurrentStep == step && sequence.State == state && step.State == TutorialStepState.Active && JsonUtility.ToJson(save.LoadProgress()) == saved, "Safety preserves exact current Tutorial step/state and save; no completion");
            check(Mathf.Abs(movement.VerticalVelocity) < .01f && camera.Mode == CameraMode.ThirdPerson, "Safety resets temporary motion and restores TPS mode");
            report.recoveryCount = safety.RecoveryCount; player.SetGameplayActive(true); sequence.enabled = false;
            foreach (float z in new[] { 4f, 15f, 28f, 44f })
            foreach (int side in new[] { -1, 1 })
            {
                camera.ApplyCameraMode(CameraMode.FirstPerson); Teleport(player, new Vector3(0, .1f, z), side * 90);
                for (int i = 0; i < 60; i++) { Input(pad, Vector2.up, i == 25 ? GamepadButton.South : GamepadButton.LeftStick); report.boundaryJumpPeak = Mathf.Max(report.boundaryJumpPeak, player.transform.position.y); yield return null; }
                Input(pad, Vector2.zero);
                check(Mathf.Abs(player.transform.position.x) < 6.6f && player.transform.position.y > -.1f, "Walk/sprint jump contained at " + (side < 0 ? "left" : "right") + " edge z=" + z);
            }
            check(report.boundaryJumpPeak > .8f && safety.RecoveryCount == report.recoveryCount, "Boundary exploit tested with real jump; primary walls, not recovery, contain player");
            camera.ApplyCameraMode(CameraMode.ThirdPerson); Teleport(player, new Vector3(5.9f, .1f, 27));
            for (int i = 0; i < 20; i++) { Input(pad, Vector2.zero); yield return null; }
            report.parallelMinimumBoom = float.PositiveInfinity; Vector3 previous = Vector3.zero;
            for (int i = 0; i < 30; i++) { Input(pad, Vector2.up); yield return null; var relative = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera.transform.position - (player.transform.position + Vector3.up * 1.4f); report.parallelMinimumBoom = Mathf.Min(report.parallelMinimumBoom, relative.magnitude); if (i > 0) report.parallelMaximumStep = Mathf.Max(report.parallelMaximumStep, Vector3.Distance(relative, previous)); previous = relative; }
            report.minimumCameraBoom = float.PositiveInfinity; previous = Vector3.zero;
            for (int i = 0; i < 35; i++)
            {
                Input(pad, new Vector2(.15f, 1)); yield return null;
                var relative = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera.transform.position - (player.transform.position + Vector3.up * 1.4f);
                report.minimumCameraBoom = Mathf.Min(report.minimumCameraBoom, relative.magnitude); if (i > 0) { float delta = Vector3.Distance(relative, previous); report.maximumCameraStep = Mathf.Max(report.maximumCameraStep, delta); if (delta > .5f) report.cameraContactAdjustments++; } previous = relative;
            }
            report.poses = poses.ToArray(); report.checks = checks.ToArray(); File.WriteAllText(TutorialEnvironmentArtPass.Evidence + "/GuidancePlayValidation.json", JsonUtility.ToJson(report, true));
            check(report.parallelMinimumBoom > 1 && report.parallelMaximumStep < .5f, "TPS parallel to boundary retains stable usable boom");
            check(report.minimumCameraBoom > capsule.radius * 2 + .2f && report.cameraContactAdjustments == 0, "TPS wall pressure keeps camera beyond capsule diameter plus probe radius, without abrupt contact jumps");
            var spawn = player.GetComponent<CharacterVisualSpawner>(); var roof = TutorialEnvironmentArtPass.BoundsOf(TutorialEnvironmentArtPass.Find("Low ruin lintel").Find("Art_ADP_Tutorial_LowLintel_R2").gameObject);
            foreach (string id in new[] { CharacterIds.Default, CharacterIds.Character02, CharacterIds.Character03 })
            {
                Input(pad, Vector2.zero); Teleport(player, new Vector3(0, .1f, 16.5f)); spawn.Apply(id); camera.ApplyCameraMode(CameraMode.ThirdPerson);
                for (int i = 0; i < 30; i++) yield return null;
                var pose = new Pose { id = id, roofY = roof.min.y, standingHeight = capsule.height, standingCenter = capsule.center.y, maximumInsideRoofY = -100 };
                for (int i = 0; i < 55; i++) { Input(pad, Vector2.up); yield return null; }
                pose.blockedZ = player.transform.position.z;
                check(pose.blockedZ < roof.min.z && pose.standingHeight > 1.79f, "Standing actual capsule rejected by raised lintel: " + id);
                bool captured = false;
                // Do not pre-settle crouch: observe the real transition while pushing against the beam.
                for (int i = 0; i < 100 && player.transform.position.z < 21.5f; i++)
                {
                    Input(pad, Vector2.up, GamepadButton.East); yield return null;
                    if (!captured && player.transform.position.z > 18.9f) { ScreenCapture.CaptureScreenshot(TutorialEnvironmentArtPass.Evidence + "/Guidance_Crouch_" + id + ".png"); captured = true; }
                    pose.maximumInsideRoofY = Mathf.Max(pose.maximumInsideRoofY, InsideRoofTop(spawn.CurrentVisual, roof));
                    pose.maxWalkingVisualY = Mathf.Max(pose.maxWalkingVisualY, AxelGameplayAuthoring.GeometryBounds(spawn.CurrentVisual).max.y);
                }
                pose.crouchHeight = capsule.height; pose.crouchCenter = capsule.center.y; pose.finalZ = player.transform.position.z; Input(pad, Vector2.zero);
                poses.Add(pose); report.poses = poses.ToArray(); report.checks = checks.ToArray(); File.WriteAllText(TutorialEnvironmentArtPass.Evidence + "/GuidancePlayValidation.json", JsonUtility.ToJson(report, true));
                check(pose.finalZ > 20.5f && pose.crouchHeight < 1.01f, "Crouched actual capsule crosses opening: " + id);
                // Collect all three transition peaks before reporting any failed clearance.
                yield return null;
            }
            foreach (var pose in poses) check(pose.maximumInsideRoofY < roof.min.y + .02f, "CPU-skinned vertices including transition do not significantly penetrate roof: " + pose.id);
            report.passed = true; report.poses = poses.ToArray(); report.checks = checks.ToArray(); File.WriteAllText(TutorialEnvironmentArtPass.Evidence + "/GuidancePlayValidation.json", JsonUtility.ToJson(report, true));
            EditorSceneManager.LoadSceneInPlayMode(TutorialEnvironmentArtPass.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); for (int i = 0; i < 8; i++) yield return null;
        }
    }
}
