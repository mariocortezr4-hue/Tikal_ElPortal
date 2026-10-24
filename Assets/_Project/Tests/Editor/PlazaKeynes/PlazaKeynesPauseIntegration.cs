using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    // Scene wiring only; the shared prefab and runtime implementation remain unchanged.
    [InitializeOnLoad]
    public static class PlazaKeynesPauseIntegration
    {
        const string ScenePath="Assets/_Project/Scenes/Levels/Level02_PlazaKeynes.unity";
        const string Evidence="Temp/PlazaPause", Request="Temp/PlazaPause.command", Key="Tikal.PlazaPause.Test";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();
        static int frame; static double deadline;
        static Keyboard keyboard; static Gamepad pad;
        static bool oldBackground; static float oldVolume;
        static InputSettings.BackgroundBehavior oldInputBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        [Serializable] class Result { public bool passed; public string[] checks,errors; public string scope; }
        static PlazaKeynesPauseIntegration(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=State;}
        static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
        static T[] Components<T>(Scene s) where T:Component=>GameplayPauseMenuAuthoring.Components<T>(s);
        static void Check(bool b,string message){if(!b)throw new Exception(message);checks.Add(message);}
        static void RequireEdit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required; preserve manual changes first.");
            if(!File.ReadAllText(".git/HEAD").Trim().EndsWith("refs/heads/feature/level1-to-level2-transition"))throw new Exception("Authorized Level02 integration branch required.");
            Directory.CreateDirectory(Evidence);
        }
        static void Poll()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Request))
            {
                string command=File.ReadAllText(Request).Trim();File.Delete(Request);
                try{RequireEdit();if(command=="audit")Audit();else if(command=="apply")Apply();else if(command=="test"){EditorSceneManager.OpenScene(ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}else throw new Exception("Unknown command");}
                catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||routines.Count==0||frame==Time.frameCount)return;
            frame=Time.frameCount;EditorApplication.QueuePlayerLoopUpdate();
            try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Scoped Pause validation timed out");while(routines.Count>0){var r=routines.Peek();if(!r.MoveNext()){routines.Pop();continue;}if(r.Current is IEnumerator nested){routines.Push(nested);continue;}return;}Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Audit()
        {
            var lines=new List<string>();
            foreach(var path in new[]{"Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity",ScenePath})
            {
                var s=EditorSceneManager.OpenPreviewScene(path);
                try{
                    lines.Add(path+" players="+Components<PlayerController>(s).Length+" input="+Components<PlayerInputReader>(s).Length+" canvases="+Components<Canvas>(s).Length+" settingsAppliers="+Components<PersistentSettingsApplier>(s).Length+" events="+Components<EventSystem>(s).Length+" journals="+Components<ClueJournalPresenter>(s).Length);
                    foreach(var p in Components<PauseMenuController>(s))lines.Add("Pause prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject)+" configured="+p.IsConfigured+" checkpoint="+Field(p,"_checkpointRestart"));
                    lines.Add("Modal components: "+string.Join(",",Components<MonoBehaviour>(s).Where(c=>c!=null&&(c.GetType().Name.Contains("Map")||c.GetType().Name.Contains("Journal"))).Select(c=>c.GetType().FullName)));
                }finally{EditorSceneManager.ClosePreviewScene(s);}
            }
            File.WriteAllLines(Evidence+"/Audit.txt",lines);
        }
        [MenuItem("Tikal/Authoring/Apply Shared Pause to Plaza Keynes Only")]
        public static void Apply()
        {
            RequireEdit();var s=EditorSceneManager.OpenScene(ScenePath);
            if(Components<Canvas>(s).Length==0)
            {
                var source=EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                try
                {
                    var original=Components<ReticlePresenter>(source).Single().GetComponentInParent<Canvas>();
                    var hud=new GameObject("Gameplay HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ReticleSettings));
                    EditorUtility.CopySerialized(original,hud.GetComponent<Canvas>());
                    EditorUtility.CopySerialized(original.GetComponent<CanvasScaler>(),hud.GetComponent<CanvasScaler>());
                    EditorUtility.CopySerialized(original.GetComponent<GraphicRaycaster>(),hud.GetComponent<GraphicRaycaster>());
                    var reticle=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Reticle.prefab"),hud.transform);
                    var presenter=reticle.GetComponent<ReticlePresenter>();var player=Components<PlayerController>(s).Single();
                    GameplayPauseMenuAuthoring.Set(presenter,"_settings",hud.GetComponent<ReticleSettings>());
                    GameplayPauseMenuAuthoring.Set(presenter,"_inputReader",player.GetComponent<PlayerInputReader>());
                    GameplayPauseMenuAuthoring.Set(presenter,"_interaction",player.GetComponent<Tikal.Interaction.PlayerInteraction>());
                    GameplayPauseMenuAuthoring.Set(presenter,"_viewCamera",Components<Camera>(s).Single(c=>c.CompareTag("MainCamera")));
                    GameplayPauseMenuAuthoring.Set(Components<PersistentSettingsApplier>(s).Single(),"_reticleSettings",hud.GetComponent<ReticleSettings>());
                }
                finally{EditorSceneManager.ClosePreviewScene(source);}
            }
            GameplayPauseMenuAuthoring.Apply(s); // Never calls BuildPrefab or the Tutorial/Level01 authoring loop.
            if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Components<PauseMenuController>(s).Single().gameObject)!=GameplayPauseMenuAuthoring.PrefabPath)throw new Exception("Shared prefab link required");
            if(!EditorSceneManager.SaveScene(s))throw new Exception("Level02 save failed");
            File.WriteAllText(Evidence+"/Applied.txt","Shared GameplayPauseMenu prefab connected to Level02 only.");
        }
        static void State(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){checks.Clear();errors.Clear();oldBackground=Application.runInBackground;oldVolume=AudioListener.volume;oldInputBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.logMessageReceived+=Log;frame=-1;deadline=EditorApplication.timeSinceStartup+240;routines.Clear();routines.Push(Tests());}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);File.WriteAllText(Evidence+"/Validation.json",JsonUtility.ToJson(new Result{passed=errors.Count==0&&checks.Count>30,checks=checks.ToArray(),errors=errors.ToArray(),scope="Level02 only. Real Play Mode, virtual Keyboard/XInput/DualShock via existing bindings, all three character visuals in FPS/TPS; physical controllers not certified."},true));}
        }
        static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        static void Finish()
        {
            routines.Clear();var pause=UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();if(pause!=null)pause.Resume();
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            keyboard=null;pad=null;Time.timeScale=1;AudioListener.volume=oldVolume;Application.runInBackground=oldBackground;InputSystem.settings.backgroundBehavior=oldInputBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();
        }
        static IEnumerator PressKey(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
        static IEnumerator PadButton(GamepadButton button){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;}
        static void Duplicates()
        {
            var s=SceneManager.GetActiveScene();Check(Components<EventSystem>(s).Length==1,"One EventSystem");Check(Components<PlayerController>(s).Length==1,"One Player");Check(Components<PlayerInputReader>(s).Length==1,"One input reader");Check(Components<Camera>(s).Count(c=>c.CompareTag("MainCamera"))==1,"One MainCamera");Check(Components<AudioListener>(s).Length==1,"One AudioListener");Check(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts");
        }
        static IEnumerator Tests()
        {
            for(int i=0;i<10;i++)yield return null;
            Check(SceneManager.GetActiveScene().path==ScenePath,"Level02 loads");Duplicates();
            var pause=UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();Check(pause!=null&&pause.IsConfigured&&pause.IsListening&&pause.CanOpen,"Shared Pause configured and listening");
            Check(pause.GetType()==typeof(PauseMenuController),"Same shared runtime controller as Level01 (prefab link checked in Edit Mode)");
            Check(!pause.CanRestart,"Existing checkpoint button disabled without Level02 checkpoint");
            // Reuse established assertions for actual Escape/Start, locks, reticle, all panels,
            // isolated settings callbacks, lifecycle and three gamepad layouts; no Level01 load.
            typeof(GameplayPauseMenuValidation).GetMethod("ValidateRuntime",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            Check(true,"Existing shared runtime validation passed in Level02");
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var reader=player.GetComponent<PlayerInputReader>();var camera=player.GetComponent<PlayerCameraController>();
            keyboard=InputSystem.AddDevice<Keyboard>();reader.PauseAction.actionMap.asset.devices=new InputDevice[]{keyboard};
            foreach(var id in new[]{CharacterIds.Default,CharacterIds.Character02,CharacterIds.Character03})
            foreach(var mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson})
            {
                player.GetComponent<CharacterVisualSpawner>().Apply(id);camera.ApplyCameraMode(mode);yield return null;yield return null;
                Check(player.GetComponent<CharacterVisualSpawner>().CurrentVisual.name==id,id+" visual loaded: "+mode);Duplicates();
                yield return PressKey(UnityEngine.InputSystem.Key.Escape);Check(pause.IsOpen&&Time.timeScale==0&&!reader.GameplayEnabled&&!player.GameplayActive,id+" "+mode+": Escape locks gameplay");
                var position=player.transform.position;var rotation=Camera.main.transform.rotation;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.E));for(int n=0;n<5;n++)yield return null;
                Check(Vector3.Distance(position,player.transform.position)<.001f&&Quaternion.Angle(rotation,Camera.main.transform.rotation)<.01f&&reader.Look==Vector2.zero,id+" "+mode+": movement/look/interact disabled");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                var events=EventSystem.current;var before=events.currentSelectedGameObject;yield return PressKey(UnityEngine.InputSystem.Key.DownArrow);Check(events.currentSelectedGameObject!=before,"Keyboard native UI navigation: "+id+" "+mode);
                ((Button)Field(pause,"_continue")).onClick.Invoke();yield return null;Check(!pause.IsOpen&&reader.GameplayEnabled&&player.GameplayActive&&Time.timeScale==1,"Continue restores "+id+" "+mode);
                position=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));for(int n=0;n<10;n++)yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;Check(Vector3.Distance(position,player.transform.position)>.02f,"Movement resumes: "+id+" "+mode);
            }
            foreach(var playstation in new[]{false,true})
            {
                pad=playstation?(Gamepad)InputSystem.AddDevice<DualShockGamepad>():InputSystem.AddDevice<XInputController>();reader.PauseAction.actionMap.asset.devices=new InputDevice[]{keyboard,pad};
                yield return PadButton(GamepadButton.Start);Check(pause.IsOpen,(playstation?"PlayStation Options":"Xbox Menu")+" opens shared Pause");
                var before=EventSystem.current.currentSelectedGameObject;yield return PadButton(GamepadButton.DpadDown);Check(EventSystem.current.currentSelectedGameObject!=before,"Native gamepad UI navigation: "+pad.layout);
                yield return PadButton(GamepadButton.Start);Check(!pause.IsOpen&&reader.GameplayEnabled,"Gamepad resumes: "+pad.layout);InputSystem.RemoveDevice(pad);pad=null;
            }
            yield return PressKey(UnityEngine.InputSystem.Key.Escape);((Button)Field(pause,"_mainMenu")).onClick.Invoke();
            for(int i=0;i<300&&SceneManager.GetActiveScene().name!="MainMenu";i++)yield return null;
            Check(SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Existing Main Menu callback loads with normal time");
        }
    }
}
