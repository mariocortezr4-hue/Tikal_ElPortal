using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Cinematics;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class PlazaTransitionValidation
    {
        const string Key = "Tikal.PlazaTransition.Validation";
        static readonly List<string> checks = new List<string>();
        static readonly List<string> errors = new List<string>();
        static readonly List<string> warnings = new List<string>();
        static readonly List<string> captures = new List<string>();
        static readonly List<string> clips = new List<string>();
        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static int lastFrame;
        static double deadline;
        static Keyboard keyboard;
        static bool completed;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        static string E => PlazaTransitionAuthoring.Evidence;

        [Serializable] public class Result
        {
            public bool passed, transitionPassed, realPlayMode, allThreeCharactersCompleted;
            public string[] checks, errors, warnings, captures, walkClips;
            public string scope;
        }

        static PlazaTransitionValidation()
        {
            EditorApplication.playModeStateChanged += State;
            EditorApplication.update += Poll;
        }

        public static void Begin()
        {
            if (!Application.dataPath.Replace('\\','/').Contains("/_PlazaKeynes_20261024/")) throw new Exception("Validation must use isolated copy.");
            PlayerSettings.productName = "Tikal_Transition_Validation_20261024";
            EditorSceneManager.OpenScene(PlazaTransitionAuthoring.Level01);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                checks.Clear(); errors.Clear(); warnings.Clear(); captures.Clear(); clips.Clear(); completed = false;
                Application.logMessageReceived += Log;
                Time.captureFramerate = 30;
                Application.runInBackground = true;
                oldBackground = InputSystem.settings.backgroundBehavior;
                oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                deadline = EditorApplication.timeSinceStartup + 1200;
                lastFrame = -1;
                stack.Clear(); stack.Push(Tests());
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                var result = new Result
                {
                    passed = completed && errors.Count == 0,
                    transitionPassed = completed && errors.All(e => e.Contains("UnityEditor.Search.SearchDatabase")),
                    realPlayMode = true,
                    allThreeCharactersCompleted = completed,
                    checks = checks.ToArray(), errors = errors.ToArray(), warnings = warnings.Distinct().ToArray(), captures = captures.ToArray(), walkClips = clips.Distinct().ToArray(),
                    scope = "Actual SecretCommand action via queued InputSystem G on real Level01, single scene loads, Timeline and arrival. Three selections. Negative checks outside zone and ordinary Door open. No global puzzle regression or build performance certification."
                };
                File.WriteAllText(E + "/Play_Validation.json", JsonUtility.ToJson(result, true));
                EditorApplication.Exit(result.passed ? 0 : 6);
            }
        }

        static void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
            else if (type == LogType.Warning) warnings.Add(message);
        }

        static void Check(bool condition, string text)
        {
            if (!condition) throw new Exception(text);
            checks.Add(text);
        }

        static void Poll()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || stack.Count == 0 || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Targeted transition validation timed out.");
                while (stack.Count > 0)
                {
                    var routine = stack.Peek();
                    if (!routine.MoveNext()) { stack.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    return;
                }
                Finish();
            }
            catch (Exception e)
            {
                errors.Add(e.ToString());
                stack.Clear(); Finish();
            }
        }

        static void Finish()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            Time.captureFramerate = 0;
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            Application.logMessageReceived -= Log;
            EditorApplication.ExitPlaymode();
        }

        static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static void Send(bool command) => InputSystem.QueueStateEvent(keyboard, command ? new KeyboardState(UnityEngine.InputSystem.Key.G) : new KeyboardState());
        static void Teleport(PlayerController player, Vector3 point)
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.position = point; cc.enabled = true;
            player.GetComponent<PlayerMovement>().StopMotion(); Physics.SyncTransforms();
        }

        static IEnumerator Tests()
        {
            yield return Frames(35);
            Check(SceneManager.GetActiveScene().name == Level01PlazaTransition.SourceScene, "Real Level01 source opened");
            foreach (var id in new[] { CharacterIds.Default, CharacterIds.Character02, CharacterIds.Character03 })
            {
                var persistence = new GamePersistenceService();
                var progress = persistence.LoadProgress(); progress.SelectedCharacterId = id; persistence.SaveProgress(progress);
                // Reload AFTER saving selection, just like the real menu-to-level route.
                // GameFlow caches progress in Awake; changing only the file in a live scene is not a valid selection test.
                SceneManager.LoadScene(Level01PlazaTransition.SourceScene);
                yield return Frames(35);
                Check(UnityEngine.Object.FindAnyObjectByType<GameFlowController>().Progress.SelectedCharacterId == id,id + ": GameFlow and persisted selection agree before command");
                var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                var reader = player.GetComponent<PlayerInputReader>();
                player.GetComponent<CharacterVisualSpawner>().Apply(id);
                var outcome = UnityEngine.Object.FindAnyObjectByType<LevelOutcomeController>();
                var receiver = UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
                Check(receiver.IsConfigured && outcome.State == LevelOutcome.Playing, id + ": real receiver configured, no transition on load");
                keyboard = InputSystem.AddDevice<Keyboard>();
                reader.InteractAction.actionMap.asset.devices = new InputDevice[] { keyboard };
                if (id == CharacterIds.Default)
                {
                    Send(true); yield return Frames(3); Send(false); yield return Frames(3);
                    Check(outcome.State == LevelOutcome.Playing && Level01PlazaTransition.Active == null, "Out-of-zone SecretCommand does not start transition");
                    receiver.MainDoor.Interact(); yield return Frames(50);
                    Send(true); yield return Frames(3); Send(false); yield return Frames(3);
                    Check(outcome.State == LevelOutcome.Playing && Level01PlazaTransition.Active == null, "Ordinary Door opening does not start transition");
                    receiver.MainDoor.Interact(); yield return Frames(50);
                }
                var target = Field<Transform>(receiver, "_target");
                var zone = Field<BoxCollider>(receiver, "_zone");
                var position = zone.transform.TransformPoint(zone.center) - Vector3.up;
                Teleport(player, position); yield return Frames(2);
                Check(receiver.IsPlayerInZone && receiver.CanExecuteCommand, id + ": current closed-Door/zone/range/input rules permit command");
                Send(true); yield return Frames(3); Send(false);
                Check(outcome.State == LevelOutcome.Victory && Level01PlazaTransition.Active != null, id + ": actual G action triggers real victory and fade");
                Check(Level01PlazaTransition.Active.Fade.alpha > 0 && Level01PlazaTransition.Active.Fade.alpha < 1,id + ": initial fade has intermediate opacity");
                Check(!player.GameplayActive && !reader.enabled, id + ": movement, look and Global/Player actions disabled during departure");
                while (SceneManager.GetActiveScene().name == Level01PlazaTransition.SourceScene) yield return null;
                Check(SceneManager.GetActiveScene().name == Level01PlazaTransition.CinematicScene, id + ": cinematic loaded after initial fade");
                yield return Frames(3);
                var sequence = UnityEngine.Object.FindAnyObjectByType<PlazaPassageSequence>();
                Check(sequence != null && sequence.SelectedCharacterId == id, id + ": cinematic uses selected character");
                Check(UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length == 0, id + ": visual proxy has no gameplay PlayerController");
                Check(UnityEngine.Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None).Length == 0, id + ": no gameplay or journal/map/pause input in cinematic");
                Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).All(s => !s.isPlaying), id + ": no residual VO/audio");
                UniqueCamera(id);
                var cinematicComponents = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                Check(cinematicComponents.All(c => c != null), id + ": cinematic has no missing scripts");
                Check(cinematicComponents.OfType<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader != null && m.shader.name != "Hidden/InternalErrorShader")), id + ": cinematic has valid referenced materials");
                var gate = GameObject.Find("GateLift_AnimationRoot").transform;
                var actor = GameObject.Find("SelectedCharacter_CinematicProxy").transform;
                var beginningY = gate.position.y;
                var beginningZ = actor.position.z;
                Check(Mathf.Abs(beginningZ + 3.2f) < .1f, id + ": cinematic starts outside the door at the authored position");
                Check(Vector3.Distance(Camera.main.transform.position,new Vector3(-3.4f,2.4f,-7)) < .1f, id + ": establishing camera retains authored position");
                var marks = new[] { .75, 1.8, 3.6, 6.4, 9.4, 10.35 };
                var names = new[] { "Transition_DoorClosed", "Transition_DoorOpening", "Transition_WalkIntoEntrance", "Transition_Tunnel", "Transition_DarkTunnel", "Transition_FinalFade" };
                for (int i = 0; i < marks.Length; i++)
                {
                    while (sequence != null && sequence.Director.time < marks[i]) yield return null;
                    Check(sequence != null, id + ": cinematic reached shot " + names[i]);
                    File.AppendAllText(E+"/Runtime_Samples.txt",id+" t="+sequence.Director.time.ToString("F3")+" gateY="+gate.position.y+" startY="+beginningY+" actor="+actor.position+" localFade="+GameObject.Find("LocalFade").GetComponent<CanvasGroup>().alpha+" persistentFade="+Level01PlazaTransition.Active.Fade.alpha+"\n");
                    if (id == CharacterIds.Default) Capture(names[i]);
                    else if (i == 3) Capture("Transition_Tunnel_"+id.Replace('.','_'));
                    if (i == 1) Check(gate.position.y > beginningY + .5f && gate.position.y < 3.65f, id + ": Door lifts vertically during Timeline (y="+gate.position.y+", initial="+beginningY+")");
                    if (i == 2)
                    {
                        Check(Mathf.Abs(gate.position.y - 3.65f) < .02f && Mathf.Abs(gate.position.x) < .01f && Mathf.Abs(gate.position.z) < .01f, id + ": Door reaches upper opening without lateral displacement");
                        Check(!sequence.VisualAnimator.applyRootMotion, id + ": existing character locomotion uses no root motion");
                        var current = sequence.VisualAnimator.GetCurrentAnimatorClipInfo(0).Where(c => c.weight > .1f).Select(c => c.clip.name).ToArray();
                        clips.Add(id + ": " + string.Join(",", current));
                        Check(current.Any(n => n.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0), id + ": real Walk clip is playing");
                    }
                    if (i == 4) Check(actor.position.z > beginningZ + 10, id + ": selected character traversed entrance into dark tunnel");
                }
                while (SceneManager.GetActiveScene().name == Level01PlazaTransition.CinematicScene) yield return null;
                Check(SceneManager.GetActiveScene().name == Level01PlazaTransition.DestinationScene, id + ": real Plaza Keynes loaded");
                Check(!UnityEngine.Object.FindAnyObjectByType<PlayerController>().GameplayActive,id + ": arrival remains locked under black fade");
                while (Level01PlazaTransition.Active != null) yield return null;
                yield return Frames(30);
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                reader = player.GetComponent<PlayerInputReader>();
                Check(player.GameplayActive && reader.enabled && reader.GameplayEnabled, id + ": control restored after arrival fade");
                Check(UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length == 1, id + ": one Player after arrival");
                Check(player.GetComponent<CharacterVisualSpawner>().CurrentVisual.name == id, id + ": selected character preserved in Plaza Keynes");
                Check(persistence.LoadProgress().SelectedCharacterId == id, id + ": persistent selection unchanged");
                Check(Vector3.Distance(player.transform.position, new Vector3(0, 0, -72)) < .6f && player.transform.forward.z > .99f, id + ": validated safe arrival faces the plaza");
                Check(Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, 2, ~0, QueryTriggerInteraction.Ignore), id + ": ground collider supports arrival");
                UniqueCamera(id);
                var all = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                Check(all.All(c => c != null), id + ": no missing scripts in destination");
                Check(all.OfType<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader != null && m.shader.name != "Hidden/InternalErrorShader")), id + ": no missing/error materials in destination");
                Capture(id == CharacterIds.Default ? "Transition_PlazaKeynesArrival" : "Transition_PlazaKeynesArrival_"+id.Replace('.','_'));
                // Actual movement after arrival, using the existing input path.
                var start = player.transform.position;
                reader.InteractAction.actionMap.asset.devices = new InputDevice[] { keyboard };
                for (int i = 0; i < 20; i++) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.W)); yield return null; }
                Send(false); yield return Frames(2);
                Check(player.transform.position.z > start.z + 1, id + ": existing Player moves safely after transition");
                InputSystem.RemoveDevice(keyboard); keyboard = null;
            }
            completed = true;
        }

        static void UniqueCamera(string id)
        {
            Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.CompareTag("MainCamera") && c.enabled) == 1, id + ": one active MainCamera");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.enabled) == 1, id + ": one active AudioListener");
        }

        static void Capture(string name)
        {
            var cam = Camera.main;
            var overlays = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var orders = overlays.Select(c => c.worldCamera).ToArray();
            var distances = overlays.Select(c => c.planeDistance).ToArray();
            var rt = new RenderTexture(1280, 720, 24); rt.Create();
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; float aspect = cam.aspect;
            try
            {
                foreach (var canvas in overlays) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .1f; }
                Canvas.ForceUpdateCanvases(); cam.aspect = 16f / 9;
                RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                var path = E + "/Captures/" + name + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); captures.Add(path);
            }
            finally
            {
                for (int i = 0; i < overlays.Length; i++) { overlays[i].renderMode = RenderMode.ScreenSpaceOverlay; overlays[i].worldCamera = orders[i]; overlays[i].planeDistance = distances[i]; }
                cam.aspect = aspect; RenderTexture.active = previous;
                rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
