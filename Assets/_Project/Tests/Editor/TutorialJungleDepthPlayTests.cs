using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Player;
using Tikal.Persistence;
using Tikal.Tutorial;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class TutorialJungleDepthPlayTests
    {
        [Serializable] public class View { public string label; public Vector3 player, camera; public float yaw, boom; }
        [Serializable] public class Report { public bool passed; public string scope; public float accumulatedOrbit; public View[] views; }
        [Serializable] public class ApprovedVisual { public string name, guid; public Vector3 scale; public bool restoreLods; }
        [Serializable] public class ApprovedVisuals { public ApprovedVisual[] entries; }
        // Before/after comparison changes objects only in transient Play Mode. No project asset is restored or saved.
        public static void ApplyApprovedVisualBaseline(Scene scene)
        {
            if (scene.path != TutorialEnvironmentArtPass.ScenePath) return;
            var art = TutorialEnvironmentArtPass.Find(TutorialEnvironmentArtPass.RootName);
            art.Find("BackgroundForest").gameObject.SetActive(false); art.Find("FinalEdgeClusters").gameObject.SetActive(false);
            var entries = JsonUtility.FromJson<ApprovedVisuals>(File.ReadAllText(TutorialEnvironmentArtPass.Docs + "TutorialJungleDepthApprovedVisuals.json")).entries;
            foreach (var entry in entries)
            {
                var instance = art.GetComponentsInChildren<Transform>(true).Single(t => t.name == entry.name); instance.localScale = entry.scale;
                if (!entry.restoreLods) continue;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(entry.guid)); var sourceGroup = source.GetComponentInChildren<LODGroup>(); var target = instance.GetComponentInChildren<LODGroup>();
                var lods = sourceGroup.GetLODs().Select(l => new LOD(l.screenRelativeTransitionHeight, l.renderers.Select(renderer => instance.Find(AnimationUtility.CalculateTransformPath(renderer.transform, source.transform)).GetComponent<Renderer>()).ToArray()) { fadeTransitionWidth = l.fadeTransitionWidth }).ToArray();
                target.SetLODs(lods); target.size = sourceGroup.size; target.localReferencePoint = sourceGroup.localReferencePoint; target.fadeMode = sourceGroup.fadeMode;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            }
        }
        static void Input(Gamepad pad, Vector2 move, Vector2 look = default) => InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = move, rightStick = look });
        static void Capture(PlayerController player, List<View> views, string label)
        {
            var camera = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera;
            views.Add(new View { label = label, player = player.transform.position, camera = camera.transform.position, yaw = camera.transform.eulerAngles.y, boom = Vector3.Distance(camera.transform.position, player.transform.position + Vector3.up * 1.4f) });
            ScreenCapture.CaptureScreenshot(TutorialEnvironmentArtPass.Evidence + "/" + label + ".png");
        }
        public static IEnumerator Run(Gamepad pad, Action<bool, string> check)
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); var camera = player.GetComponent<PlayerCameraController>(); var capsule = player.GetComponent<CharacterController>();
            var sequence = UnityEngine.Object.FindAnyObjectByType<TutorialSequenceController>(); var step = sequence.CurrentStep; var state = sequence.State;
            sequence.enabled = false; camera.ApplyCameraMode(CameraMode.ThirdPerson);
            for (int i = 0; i < 45; i++) { Input(pad, Vector2.zero); yield return null; }
            var views = new List<View>(); var report = new Report { scope = "Interactive normal Editor, real TPS look input through production camera. Nine orbit and six boundary captures; no scene edits, isolated test save. Visual horizon/readability require image inspection, not just structural assertions." };
            float previous = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera.transform.eulerAngles.y;
            for (int sample = 0; sample < 9; sample++)
            {
                if (sample > 0)
                {
                    float segment = 0;
                    for (int frame = 0; frame < 120 && segment < 45; frame++)
                    {
                        Input(pad, Vector2.zero, new Vector2(.7f, 0)); yield return null;
                        float yaw = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera.transform.eulerAngles.y; float delta = Mathf.DeltaAngle(previous, yaw); segment += delta; report.accumulatedOrbit += delta; previous = yaw;
                    }
                    check(segment >= 45, "Real TPS orbit segment " + sample);
                    for (int i = 0; i < 8; i++) { Input(pad, Vector2.zero); yield return null; }
                    previous = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera.transform.eulerAngles.y;
                }
                Capture(player, views, "Depth_Orbit_" + sample.ToString("D2")); yield return null;
            }
            check(report.accumulatedOrbit >= 359 && views.TrueForAll(v => v.boom > 2.8f), "Full 360 TPS orbit retains normal framing at spawn");
            foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { 4f, 26, 43 })
            {
                Input(pad, Vector2.zero); capsule.enabled = false; player.transform.SetPositionAndRotation(new Vector3(side * 5.4f, .1f, z), Quaternion.identity); capsule.enabled = true;
                player.GetComponent<PlayerMovement>().ResetForRespawn(); player.GetComponent<PlayerLook>().ResetAfterRespawn(1.62f, 1.7f); player.GetComponent<PlayerLook>().SynchronizePlayerYaw(); Physics.SyncTransforms(); camera.SnapAfterTeleport();
                for (int i = 0; i < 8; i++) { Input(pad, Vector2.up); yield return null; }
                Input(pad, Vector2.zero); Capture(player, views, "Depth_Boundary_" + (side < 0 ? "Left_" : "Right_") + z.ToString("F0")); yield return null;
                check(views[views.Count - 1].boom > 1.2f, "TPS camera near visual edge " + side + "/" + z);
            }
            check(sequence.CurrentStep == step && sequence.State == state, "Visual survey does not advance the Tutorial");
            report.passed = true; report.views = views.ToArray(); File.WriteAllText(TutorialEnvironmentArtPass.Evidence + "/JungleDepthPlayValidation.json", JsonUtility.ToJson(report, true));
            EditorSceneManager.LoadSceneInPlayMode(TutorialEnvironmentArtPass.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); for (int i = 0; i < 8; i++) yield return null;
        }
    }
}
