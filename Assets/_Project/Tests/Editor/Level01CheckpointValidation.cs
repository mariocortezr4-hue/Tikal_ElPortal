using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class Level01CheckpointValidation
    {
        private static int _checks;
        private static bool _running;
        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Check(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("Checkpoints: " + message); _checks++; }
        [MenuItem("Tikal/Validation/Validate Level01 Checkpoints")]
        public static void Run()
        {
            if (_running) return;
            _checks = 0;
            try
            {
                var source = File.ReadAllText("Assets/_Project/Code/Core/CheckpointManager.cs");
                Check(!source.Contains("PlayerPrefs") && !source.Contains("SaveProgress") && !source.Contains("ResetPuzzle") && !source.Contains("ClueTracker"),
                    "session-local authority does not own persistence/puzzle/clue state");
                Check(source.Contains("if (!HasCheckpoint) { SafeFallback();") && source.Contains("_flow.RestartCurrentGameplayScene()"),
                    "missing checkpoint has existing safe scene restart fallback");
                if (Application.isPlaying)
                {
                    var manager = UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();
                    Check(manager != null && manager.gameObject.scene.path == Level01CheckpointAuthoring.ScenePath, "play authored Level01");
                    ValidateScene(manager.gameObject.scene);
                    _running = true; manager.StartCoroutine(GuardRuntime(manager));
                }
                else
                {
                    var scene = EditorSceneManager.OpenPreviewScene(Level01CheckpointAuthoring.ScenePath);
                    try { ValidateScene(scene); }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                    Debug.Log("TIKAL_LEVEL01_CHECKPOINTS_VALID checks=" + _checks + " references; run again in Play Mode.");
                }
            }
            catch (Exception exception) { _running = false; Debug.LogException(exception); }
        }
        internal static void ValidateScene(Scene scene)
        {
            var managers = Level01CheckpointAuthoring.Components<CheckpointManager>(scene);
            Check(managers.Length == 1 && managers[0].IsConfigured && managers[0].isActiveAndEnabled, "one configured active manager");
            var manager = managers[0]; var player = (PlayerController)Field(manager, "_player");
            Check(player.isActiveAndEnabled && manager.PlayerCollider == player.GetComponent<CharacterController>(), "real active Player and capsule references");
            var anchors = Level01CheckpointAuthoring.Components<CheckpointAnchor>(scene);
            Check(anchors.All(a => a.IsValid) && anchors.Select(a => a.CheckpointId).Distinct(StringComparer.Ordinal).Count() == anchors.Length, "valid unique checkpoint IDs");
            var start = anchors.Single(a => a.CheckpointId == "level01.start");
            Check(Field(manager, "_initialCheckpoint") == (object)start, "automatic initial checkpoint reference");
            var registered = (CheckpointAnchor[])Field(manager, "_checkpoints");
            Check(registered.Length == anchors.Length && anchors.All(a => registered.Contains(a)), "all available checkpoints registered");
            var pause = Level01CheckpointAuthoring.Components<PauseMenuController>(scene).Single();
            Check(Field(pause, "_checkpointRestart") == (object)manager && Field(pause, "_restart") != null, "existing Pause restart connected to sole authority");
            var outcome = Level01CheckpointAuthoring.Components<LevelOutcomeController>(scene).Single();
            Check(Field(outcome, "_checkpoints") == (object)manager && Field(manager, "_outcome") == (object)outcome, "defeat/retry share checkpoint authority");
            var resets = (MonoBehaviour[])Field(manager, "_resetTargets");
            Check(resets.All(r => r is PointOfNoReturnTrigger && r is IRespawnResettable) && !resets.Any(r => r is PuzzleBase), "only temporal trap closures reset");
            var traps = Level01CheckpointAuthoring.Components<PointOfNoReturnTrigger>(scene);
            Check(traps.Length > 0 && traps.All(t => resets.Contains(t)), "all no-return traps have reset hook");
            Physics.SyncTransforms();
            var physics = scene.GetPhysicsScene(); var overlaps = new Collider[64];
            foreach (var anchor in anchors)
            {
                foreach (var trap in traps)
                    Check(anchor.Position.z < trap.GetComponent<BoxCollider>().bounds.min.z - 1, "checkpoint before irreversible boundary: " + anchor.CheckpointId);
                Check(physics.Raycast(anchor.Position + Vector3.up * .5f, Vector3.down, out var hit, 2, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > .7f,
                    "safe ground under spawn: " + anchor.CheckpointId);
                var capsule = manager.PlayerCollider; var bottom = anchor.Position + Vector3.up * (capsule.radius + .03f);
                var top = anchor.Position + Vector3.up * ((float)Field(player.GetComponent<PlayerMovement>(), "_standingHeight") - capsule.radius + .03f);
                var count = physics.OverlapCapsule(bottom, top, capsule.radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
                Check(count < overlaps.Length && overlaps.Take(count).All(c => c.transform.IsChildOf(player.transform)),
                    "spawn capsule has unobstructed standing clearance: " + anchor.CheckpointId);
            }
            foreach (var trigger in Level01CheckpointAuthoring.Components<CheckpointTrigger>(scene))
                Check(trigger.GetComponent<BoxCollider>().isTrigger && Field(trigger, "_manager") == (object)manager && anchors.Contains((CheckpointAnchor)Field(trigger, "_checkpoint")),
                    "trigger routes actual Player to registered anchor");
            Check(typeof(PlayerMovement).GetMethod("ResetForRespawn") != null && typeof(PlayerCameraController).GetMethod("SnapAfterTeleport") != null &&
                typeof(PlayerInteraction).GetMethod("ClearFocusForRespawn") != null, "explicit movement/camera/interaction reset hooks");
            Check(Level01CheckpointAuthoring.Components<Canvas>(scene).Length == 1 && Level01CheckpointAuthoring.Components<UnityEngine.EventSystems.EventSystem>(scene).Length == 1,
                "fade reuses existing Canvas/EventSystem");
            foreach (var transform in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no Missing Scripts: " + transform.name);
        }
        private static IEnumerator GuardRuntime(CheckpointManager manager)
        {
            var checks = RuntimeChecks(manager);
            try
            {
                while (true)
                {
                    bool next;
                    try { next = checks.MoveNext(); }
                    catch (Exception exception) { Debug.LogException(exception); yield break; }
                    if (!next) yield break;
                    yield return checks.Current;
                }
            }
            finally { (checks as IDisposable)?.Dispose(); _running = false; }
        }
        private static IEnumerator RuntimeChecks(CheckpointManager manager)
        {
            yield return null;
            var player = (PlayerController)Field(manager, "_player");
            var reader = player.GetComponent<PlayerInputReader>(); var movement = player.GetComponent<PlayerMovement>();
            var camera = player.GetComponent<PlayerCameraController>(); var interaction = player.GetComponent<PlayerInteraction>();
            var pause = (PauseMenuController)Field(manager, "_pause"); var outcome = (LevelOutcomeController)Field(manager, "_outcome");
            var journal = (ClueJournalPresenter)Field(manager, "_journal");
            var applier = (PersistentSettingsApplier)Field(camera, "_settingsApplier");
            var originalSettings = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(applier.CurrentSettings));
            var actions = (TikalInputActions)Field(reader, "_actions"); var oldDevices = actions.devices;
            var initial = manager.CurrentCheckpoint;
            var puzzles = Level01CheckpointAuthoring.Components<PuzzleBase>(manager.gameObject.scene); var states = puzzles.Select(p => p.State).ToArray();
            var tracker = UnityEngine.Object.FindAnyObjectByType<ClueTracker>(); var clues = tracker.DiscoveredClues.ToArray();
            var door = UnityEngine.Object.FindAnyObjectByType<Door>(); var doorState = door.State;
            var sceneHandle = manager.gameObject.scene.handle; GameObject extra = null;
            try
            {
                Check(manager.HasCheckpoint && manager.CanRestartFromCheckpoint && outcome.State == LevelOutcome.Playing && !pause.IsOpen && !journal.IsOpen,
                    "default checkpoint valid at fresh gameplay; close UI before validation");
                actions.devices = Array.Empty<InputDevice>();
                extra = new GameObject("Temporary checkpoint B"); extra.transform.position = initial.Position + Vector3.right * .75f; extra.transform.rotation = Quaternion.Euler(0,90,0);
                var checkpointB = extra.AddComponent<CheckpointAnchor>(); var data = new SerializedObject(checkpointB);
                data.FindProperty("_checkpointId").stringValue = "validation.b"; data.ApplyModifiedPropertiesWithoutUndo();
                Check(manager.Register(checkpointB), "register valid B");
                Check(!manager.Activate(null) && manager.CurrentCheckpoint == initial, "invalid activation does not corrupt checkpoint");
                foreach (var mode in new[] { CameraMode.FirstPerson, CameraMode.ThirdPerson })
                {
                    var settings = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(originalSettings)); settings.CameraMode = mode; applier.Apply(settings);
                    Check(manager.Activate(initial), "activate A");
                    movement.RequestJump(); movement.Tick(Vector2.right, true, true, .05f);
                    Check(manager.Respawn() && Vector3.Distance(player.transform.position, initial.Position) < .001f, mode + " A respawn destination");
                    Check(movement.CurrentSpeed == 0 && movement.VerticalVelocity == 0 && !movement.IsSprinting &&
                        !(bool)Field(movement,"_jumpRequested") && (float)Field(movement,"_coyoteRemaining") == 0 && !reader.GamepadSprintLatched,
                        "movement/jump/coyote/sprint reset");
                    Check(interaction.CurrentInteractable == null && !interaction.HasResolvedAimPoint, "no stale focus/reticle hit");
                    Check(!float.IsNaN((float)Field(camera,"_yawVelocity")) && !float.IsNaN((float)Field(camera,"_pitchVelocity")) &&
                        !float.IsNaN((float)Field(camera,"_distanceVelocity")), "snap leaves finite smoothing state");
                    Check(camera.Mode == mode && Vector3.Distance(interaction.ViewCamera.transform.position, player.transform.position) < 8,
                        "camera preserves mode and snaps near destination");
                    Check(manager.Activate(checkpointB) && manager.Respawn() && Vector3.Distance(player.transform.position, checkpointB.Position) < .001f,
                        mode + " A then B respawns at B");
                    Check(Quaternion.Angle(player.transform.rotation, checkpointB.Rotation) < .01f && camera.Mode == mode, "spawn rotation respected without preference change");
                    Check(manager.Activate(initial), "return to A");
                    pause.Open(); Check(pause.CanRestart && ((Button)Field(pause,"_restart")).interactable, "Pause checkpoint restart enabled");
                    pause.Restart(); Check(!pause.IsOpen && Time.timeScale == 1 && player.GameplayActive && reader.GameplayEnabled &&
                        Vector3.Distance(player.transform.position, initial.Position) < .001f, "Pause closes then respawns without reload");
                    foreach (var trap in Level01CheckpointAuthoring.Components<PointOfNoReturnTrigger>(manager.gameObject.scene))
                    {
                        trap.GetType().GetField("_triggered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(trap, true);
                        ((GameObject)Field(trap,"_closingPassage")).SetActive(true);
                    }
                    outcome.TriggerDefeat(); Check(manager.IsRespawning && !manager.Respawn(), "defeat starts sole flow; parallel respawn rejected");
                    var limit = Time.realtimeSinceStartup + (float)Field(manager,"_defeatMessageSeconds") + 2*(float)Field(manager,"_fadeSeconds") + 5;
                    while (manager.IsRespawning) { Check(Time.realtimeSinceStartup < limit, "defeat flow timeout"); yield return null; }
                    Check(outcome.State == LevelOutcome.Playing && player.GameplayActive && reader.GameplayEnabled && journal.enabled,
                        mode + " defeat returns control/UI through local checkpoint");
                    Check(manager.gameObject.scene.handle == sceneHandle && tracker == UnityEngine.Object.FindAnyObjectByType<ClueTracker>(), "no normal scene reload");
                    Check(puzzles.Select(p=>p.State).SequenceEqual(states) && tracker.DiscoveredClues.SequenceEqual(clues), "puzzles and discovered Journal clues preserved");
                    Check(door.State == doorState, "main Door state preserved");
                    foreach (var trap in Level01CheckpointAuthoring.Components<PointOfNoReturnTrigger>(manager.gameObject.scene))
                        Check(!(bool)Field(trap,"_triggered") && !((GameObject)Field(trap,"_closingPassage")).activeSelf, "temporary trap reset; ready for another attempt");
                }
                initial.enabled = false;
                Check(!manager.HasCheckpoint && !manager.CanRestartFromCheckpoint, "invalid current anchor disables restart; safe fallback contract checked in source");
                initial.enabled = true;
                Debug.Log("TIKAL_LEVEL01_CHECKPOINT_RUNTIME_VALID checks=" + _checks + " FPS/TPS local respawn, Pause, defeat, preserved puzzle/clue state.");
            }
            finally
            {
                if (initial != null) { initial.enabled = true; manager.Activate(initial); }
                if (extra != null) { manager.Unregister(extra.GetComponent<CheckpointAnchor>()); UnityEngine.Object.DestroyImmediate(extra); }
                actions.devices = oldDevices; applier.Apply(originalSettings); _running = false;
            }
        }
    }
}
