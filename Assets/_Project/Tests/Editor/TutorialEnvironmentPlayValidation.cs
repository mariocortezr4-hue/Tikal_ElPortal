using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Player;
using Tikal.Persistence;
using Tikal.Tutorial;
using Tikal.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class TutorialEnvironmentPlayValidation
    {
        const string Key = "Tikal.TutorialArt.Play";
        [Serializable] class Setup { public SceneSetup[] scenes; }
        [Serializable] public class View { public string name; public Vector3 player, camera; public long visibleTriangles; public int drawCalls; public float cameraDistance; }
        [Serializable] public class Report { public bool passed, enteredPlayMode; public string scope; public string[] checks, errors; public View[] views; public double meanFrameMs, p95FrameMs; }
        [Serializable] public class CharacterMeasure { public string id; public float standingHeight, standingCenter, standingTop, crouchHeight, crouchCenter, crouchTop, standingVisualTop, crouchIdleTop, crouchWalkTop, crouchEntryTop; }
        [Serializable] public class MeasurementReport { public bool passed; public string scope; public CharacterMeasure[] characters; }
        static readonly List<CharacterMeasure> measurements = new List<CharacterMeasure>();
        static readonly List<string> checks = new List<string>(), errors = new List<string>();
        static readonly List<View> views = new List<View>(); static readonly List<double> frameTimes = new List<double>();
        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static IEnumerator routine; static GamePersistenceService save; static Gamepad pad; static Keyboard keyboard;
        static int excludeFrames; static bool measuring; static double deadline, lastReal; static int lastFrame, oldCapture; static bool oldBackground; static float oldVolume;
        static PlayerController player; static PlayerMovement movement; static PlayerCameraController camera;
        static TutorialSequenceController sequence; static string character = CharacterIds.Default; static CameraMode mode;
        static TutorialEnvironmentPlayValidation() { EditorApplication.update += Poll; EditorApplication.playModeStateChanged += Mode; EditorApplication.delayCall += Resume; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(File.Exists("Temp/TutorialGuidanceMeasure.request")){File.Delete("Temp/TutorialGuidanceMeasure.request");SessionState.SetBool(Key+"Measure",true);Run();return;}
            if(!File.Exists("Temp/TutorialArtPlay.request"))return;
            SessionState.SetBool(Key+"Measure",false);
            File.Delete("Temp/TutorialArtPlay.request"); Run();
        }
        [MenuItem("Tikal/Validation/Run Tutorial Environment Play Tests")]
        public static void Run()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene before Play validation");
            EditorSceneManager.OpenScene(TutorialEnvironmentArtPass.ScenePath);
            TutorialEnvironmentArtPass.Validate();
            SessionState.SetString(Key + "Setup", JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        static void Resume() { if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying) Start(); }
        static void Mode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) Start();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false); EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<Setup>(SessionState.GetString(Key + "Setup", "")).scenes);
                AssetDatabase.Refresh(); Debug.Log("TUTORIAL_ART_PLAY_FINISHED");
            }
        }
        static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        static void StaticSet(string field, object value) => typeof(GameFlowController).GetField(field, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        static void Inject(Scene scene, LoadSceneMode loadMode)
        {
            foreach (var flow in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameFlowController>(true))) { Set(flow, "_persistence", save); Set(flow, "_progress", save.LoadProgress()); }
            foreach (var spawn in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CharacterVisualSpawner>(true))) spawn.Apply(character);
        }
        static void Start()
        {
            if (routine != null) return; checks.Clear(); errors.Clear(); views.Clear(); frameTimes.Clear();
            oldCapture = Time.captureFramerate; Time.captureFramerate = 30; oldBackground = Application.runInBackground; Application.runInBackground = true; oldVolume = AudioListener.volume;
            save = new GamePersistenceService(Path.Combine(Path.GetTempPath(), "TikalTutorialArt-" + Guid.NewGuid().ToString("N"))); save.SaveProgress(GameProgressData.CreateDefault());
            character = CharacterIds.Default; SceneManager.sceneLoaded += Inject; Application.logMessageReceived += Log; StaticSet("_introReplayToMenu", false); StaticSet("_tutorialReplayToMenu", false);
            pad = InputSystem.AddDevice<Gamepad>(); keyboard = InputSystem.AddDevice<Keyboard>(); lastFrame = -1; lastReal = Time.realtimeSinceStartupAsDouble; deadline = EditorApplication.timeSinceStartup + 900;
            measurements.Clear(); routine = SessionState.GetBool(Key+"Measure",false) ? MeasureCharacters() : Tests(); stack.Clear(); stack.Push(routine); EditorApplication.update += Tick;
        }
        static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack); }
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add(message); Debug.Log("TUTORIAL_ART_PLAY_CHECK " + message); }
        static void Tick()
        {
            if (!EditorApplication.isPlaying || lastFrame == Time.frameCount) return; lastFrame = Time.frameCount;
            double now = Time.realtimeSinceStartupAsDouble; if (measuring && excludeFrames <= 0 && player != null && SceneManager.GetActiveScene().path == TutorialEnvironmentArtPass.ScenePath) frameTimes.Add((now - lastReal) * 1000); if(excludeFrames>0)excludeFrames--; lastReal = now;
            try { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Tutorial Play deadline"); while (stack.Count > 0) { var top = stack.Peek(); if (!top.MoveNext()) { stack.Pop(); continue; } if (top.Current is IEnumerator nested) { stack.Push(nested); continue; } return; } Finish(); }
            catch (Exception e) { errors.Add(e.ToString()); Finish(); }
        }
        static void Finish()
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SceneManager.sceneLoaded -= Inject; routine = null;
            if (pad != null) InputSystem.RemoveDevice(pad); if (keyboard != null) InputSystem.RemoveDevice(keyboard); pad = null; keyboard = null;
            Time.captureFramerate = oldCapture; Application.runInBackground = oldBackground; AudioListener.volume = oldVolume;
            var sorted = frameTimes.OrderBy(t => t).ToArray();
            Directory.CreateDirectory(TutorialEnvironmentArtPass.Evidence);
            File.WriteAllText(TutorialEnvironmentArtPass.Evidence + "/PlayValidation.json", JsonUtility.ToJson(new Report { passed = errors.Count == 0, enteredPlayMode = true, checks = checks.ToArray(), errors = errors.ToArray(), views = views.ToArray(), meanFrameMs = sorted.Length == 0 ? 0 : sorted.Average(), p95FrameMs = sorted.Length == 0 ? 0 : sorted[(int)((sorted.Length - 1) * .95)], scope = "Normal interactive Editor. Virtual gamepad drives unchanged Player/Tutorial code through real physics in Dwight TPS and FPS. Camera is reset to straight ahead after Look. Intro skip uses real UI.Cancel. Saves isolated; frame timings exclude initial warm-up and five frames after captures but retain Editor overhead and are not a standalone GPU benchmark. Visible triangles use frustum and selected LOD, without occlusion or shadow passes." }, true));
            if(SessionState.GetBool(Key+"Measure",false))File.WriteAllText(TutorialEnvironmentArtPass.Evidence+"/CrouchMeasurements.json",JsonUtility.ToJson(new MeasurementReport{passed=errors.Count==0,characters=measurements.ToArray(),scope="Real unchanged production Player/Animator in interactive Play Mode. Tops are world-space maximum CPU-skinned geometry, including head/hair/clothing; capsule tops include actual player floor elevation. Crouch walk sampled for two simulated seconds."},true));
            EditorApplication.ExitPlaymode();
        }
        static void Input(Vector2 move, Vector2 look = default, GamepadButton? button = null)
        {
            var state = new GamepadState { leftStick = move, rightStick = look }; if (button.HasValue) state = state.WithButton(button.Value);
            InputSystem.QueueStateEvent(pad, state);
        }
        static IEnumerator Until(Func<bool> condition, int frames, string name, Vector2 move, GamepadButton? button = null)
        {
            for (int i = 0; i < frames && !condition(); i++) { Input(move, default, button); yield return null; }
            Input(Vector2.zero); Check(condition(), name);
        }
        static bool Active(TutorialPromptAction action) => sequence != null && sequence.CurrentStep != null && sequence.CurrentStep.PromptAction == action && sequence.State == TutorialSequenceState.Running;
        static void Capture(string label)
        {
            var cam = player.GetComponent<Tikal.Interaction.PlayerInteraction>().ViewCamera;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); long triangles = 0;
            foreach (var group in UnityEngine.Object.FindObjectsByType<LODGroup>())
            {
                if (!group.transform.IsChildOf(TutorialEnvironmentArtPass.Find("Environment"))) continue;
                float distance = Vector3.Distance(cam.transform.position, group.transform.TransformPoint(group.localReferencePoint));
                var scale = group.transform.lossyScale; float height = group.size * Mathf.Max(scale.x, scale.y, scale.z) * QualitySettings.lodBias / (2 * Mathf.Max(.01f, distance) * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2));
                var lods = group.GetLODs(); var lod = lods.FirstOrDefault(l => height >= l.screenRelativeTransitionHeight);
                if (lod.renderers != null) foreach (var renderer in lod.renderers) if (renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) triangles += Triangles(renderer.GetComponent<MeshFilter>().sharedMesh);
            }
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                if (!filter.transform.IsChildOf(TutorialEnvironmentArtPass.Find("Environment")) || filter.GetComponentInParent<LODGroup>() != null) continue;
                var renderer = filter.GetComponent<Renderer>(); if (renderer != null && renderer.enabled && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) triangles += Triangles(filter.sharedMesh);
            }
            views.Add(new View { name = label, player = player.transform.position, camera = cam.transform.position, visibleTriangles = triangles, drawCalls = UnityStats.drawCalls, cameraDistance = Vector3.Distance(cam.transform.position, player.transform.position + Vector3.up * 1.4f) });
            excludeFrames = 5; ScreenCapture.CaptureScreenshot(TutorialEnvironmentArtPass.Evidence + "/" + label + ".png");
        }
        static long Triangles(Mesh mesh) { long count = 0; for (int i = 0; i < mesh.subMeshCount; i++) count += (long)mesh.GetIndexCount(i) / 3; return count; }
        static IEnumerator Route(CameraMode cameraMode)
        {
            mode = cameraMode; player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); movement = player.GetComponent<PlayerMovement>(); camera = player.GetComponent<PlayerCameraController>(); sequence = UnityEngine.Object.FindAnyObjectByType<TutorialSequenceController>();
            var settings = UnityEngine.Object.FindAnyObjectByType<PersistentSettingsApplier>(); var chosen = JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(settings.CurrentSettings)); chosen.CameraMode = mode; settings.Apply(chosen);
            camera.ApplyCameraMode(mode); for (int i = 0; i < 8; i++) yield return null;
            for(int warm=0;warm<45;warm++) yield return null; measuring=true;
            string prefix = mode == CameraMode.ThirdPerson ? "TPS" : "FPS"; var spawn = player.GetComponent<CharacterVisualSpawner>();
            Check(spawn.CurrentVisual.name == CharacterIds.Default && spawn.CurrentVisual.activeInHierarchy == (mode == CameraMode.ThirdPerson), prefix + " canonical Dwight visibility"); Capture(prefix + "_01_Start");
            yield return Until(() => !Active(TutorialPromptAction.Move), 100, prefix + " Move completes with real locomotion", Vector2.up);
            yield return Until(() => Active(TutorialPromptAction.Look), 60, prefix + " Look activates after feedback", Vector2.zero);
            for (int i = 0; i < 90 && Active(TutorialPromptAction.Look); i++) { Input(Vector2.zero, new Vector2(.85f, 0)); yield return null; }
            Input(Vector2.zero); Check(!Active(TutorialPromptAction.Look), prefix + " Look completes from real look input");
            player.transform.rotation = Quaternion.identity; player.GetComponent<PlayerLook>().SynchronizePlayerYaw(); camera.SnapAfterTeleport();
            yield return Until(() => Active(TutorialPromptAction.Jump), 60, prefix + " Jump activates", Vector2.zero);
            yield return Until(() => player.transform.position.z >= 9.1f, 150, prefix + " Approach jump without collision drift", Vector2.up);
            Capture(prefix + "_02_Jump");
            Input(Vector2.up, default, GamepadButton.South); yield return null; Input(Vector2.up);
            float peak = player.transform.position.y;
            for (int i = 0; i < 80 && player.transform.position.z < 13.4f; i++) { Input(Vector2.up); peak = Mathf.Max(peak, player.transform.position.y); yield return null; }
            Input(Vector2.zero); Check(peak > .7f && player.transform.position.z > 12, prefix + " Actual jump clears original obstacle collider");
            yield return Until(() => Active(TutorialPromptAction.Crouch), 90, prefix + " Crouch activates", Vector2.zero);
            yield return Until(() => player.transform.position.z >= 21.4f, 200, prefix + " Crouched production capsule crosses locally calibrated lintel", Vector2.up, GamepadButton.East);
            Check(sequence.Steps[3].State == TutorialStepState.Completed, prefix + " Crouch step completes in original trigger"); Capture(prefix + "_03_CrouchExit");
            yield return Until(() => Active(TutorialPromptAction.Sprint), 90, prefix + " Sprint activates", Vector2.zero);
            yield return Until(() => !Active(TutorialPromptAction.Sprint), 140, prefix + " Sprint distance completes", Vector2.up, GamepadButton.LeftStick);
            Capture(prefix + "_04_Sprint");
            yield return Until(() => Active(TutorialPromptAction.Interact), 90, prefix + " Interact activates", Vector2.zero);
            yield return Until(() => player.transform.position.z >= 33.2f, 180, prefix + " Reach interactable along clear path", Vector2.up);
            yield return Until(() => player.GetComponent<PlayerInteraction>().CurrentInteractable != null, 60, prefix + " Real raycast resolves gate and prompt", Vector2.zero);
            Capture(prefix + "_05_Interact"); yield return null; Input(Vector2.zero, default, GamepadButton.West); yield return null; Input(Vector2.zero);
            yield return Until(() => sequence.State == TutorialSequenceState.AwaitingExit, 100, prefix + " Real interaction opens final phase", Vector2.zero);
            for (int i = 0; i < 35; i++) yield return null;
            Check(sequence.AllControlStepsCompleted && sequence.ExitGate.ExitAvailable, prefix + " Every original step completed before Exit"); Capture(prefix + "_06_Exit");
            yield return Until(() => SceneManager.GetActiveScene().path != TutorialEnvironmentArtPass.ScenePath, 180, prefix + " Real capsule crosses Exit and loads destination", Vector2.up);
            for (int i = 0; i < 5; i++) yield return null;
            Check(SceneManager.GetActiveScene().name == "Level01_SecretEntrance" && save.LoadProgress().TutorialCompleted && save.LoadProgress().CurrentLevelId == GameSceneIds.Level01, prefix + " Tutorial completion persists to isolated save and enters Level01");
            measuring=false; player = null;
        }
        static IEnumerator MeasureCharacters()
        {
            EditorSceneManager.LoadSceneInPlayMode(TutorialEnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
            player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();movement=player.GetComponent<PlayerMovement>();camera=player.GetComponent<PlayerCameraController>();sequence=UnityEngine.Object.FindAnyObjectByType<TutorialSequenceController>();sequence.enabled=false;
            camera.ApplyCameraMode(CameraMode.ThirdPerson);
            var spawn=player.GetComponent<CharacterVisualSpawner>();var capsule=player.GetComponent<CharacterController>();
            foreach(var id in new[]{CharacterIds.Default,CharacterIds.Character02,CharacterIds.Character03})
            {
                Input(Vector2.zero);capsule.enabled=false;player.transform.SetPositionAndRotation(new Vector3(0,.1f,0),Quaternion.identity);capsule.enabled=true;movement.ResetForRespawn();camera.SnapAfterTeleport();spawn.Apply(id);
                for(int i=0;i<35;i++)yield return null;
                var measure=new CharacterMeasure{id=id,standingHeight=capsule.height,standingCenter=capsule.center.y,standingTop=capsule.bounds.max.y};
                for(int i=0;i<30;i++){if(i%3==0)measure.standingVisualTop=Mathf.Max(measure.standingVisualTop,AxelGameplayAuthoring.GeometryBounds(spawn.CurrentVisual).max.y);yield return null;}
                for(int i=0;i<35;i++){Input(Vector2.zero,default,GamepadButton.East);yield return null;}
                measure.crouchHeight=capsule.height;measure.crouchCenter=capsule.center.y;measure.crouchTop=capsule.bounds.max.y;
                for(int i=0;i<60;i++){Input(Vector2.zero,default,GamepadButton.East);if(i%3==0)measure.crouchIdleTop=Mathf.Max(measure.crouchIdleTop,AxelGameplayAuthoring.GeometryBounds(spawn.CurrentVisual).max.y);yield return null;}
                for(int i=0;i<60;i++){Input(Vector2.up,default,GamepadButton.East);if(i%3==0)measure.crouchWalkTop=Mathf.Max(measure.crouchWalkTop,AxelGameplayAuthoring.GeometryBounds(spawn.CurrentVisual).max.y);yield return null;}
                measurements.Add(measure);Check(measure.crouchHeight<measure.standingHeight,"Measured real capsule and skinned poses: "+id);Input(Vector2.zero);
            }
            player=null;
        }
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode, "Interactive Editor, no batchmode");
            EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); for (int i = 0; i < 10; i++) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
            for (int i = 0; i < 600 && SceneManager.GetActiveScene().path == AncientMapIntroAuthoring.ScenePath; i++) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); for (int i = 0; i < 8; i++) yield return null;
            Check(SceneManager.GetActiveScene().path == TutorialEnvironmentArtPass.ScenePath && save.LoadProgress().IntroSeen, "Unchanged Intro skip actually enters dressed Tutorial");
            yield return TutorialGuidancePlayTests.Run(pad, save, Check);
            yield return Route(CameraMode.ThirdPerson);
            save.SaveProgress(GameProgressData.CreateDefault()); StaticSet("_tutorialReplayToMenu", false);
            EditorSceneManager.LoadSceneInPlayMode(TutorialEnvironmentArtPass.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); for (int i = 0; i < 8; i++) yield return null;
            yield return Route(CameraMode.FirstPerson);
            EditorSceneManager.LoadSceneInPlayMode(TutorialEnvironmentArtPass.ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); for (int i = 0; i < 8; i++) yield return null;
            var p = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); var spawn = p.GetComponent<CharacterVisualSpawner>(); var cam = p.GetComponent<PlayerCameraController>();
            foreach (var id in new[] { CharacterIds.Character02, CharacterIds.Character03 })
            {
                spawn.Apply(id); yield return null; cam.ApplyCameraMode(CameraMode.ThirdPerson); yield return null;
                var actor = spawn.CurrentVisual.GetComponentInChildren<Animator>(); Check(actor.avatar.isHuman && actor.avatar.isValid && actor.runtimeAnimatorController != null && !actor.applyRootMotion && spawn.CurrentVisual.activeInHierarchy, "Shared selected character TPS works: " + id);
                cam.ApplyCameraMode(CameraMode.FirstPerson); Check(!spawn.CurrentVisual.activeInHierarchy, "Shared selected character FPS hiding works: " + id);
            }
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l => l.isActiveAndEnabled) == 1, "No duplicate runtime AudioListeners");
        }
    }
}
