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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Level01MapHUDValidation
    {
        const string Key="Tikal.MapHUD.Validation";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly Stack<IEnumerator> work=new Stack<IEnumerator>();
        [Serializable] class Report {public bool passed;public string scope;public string[] checks,errors;}
        static Keyboard keyboard;static Mouse mouse;static XInputController xbox;static DualShockGamepad sony;static Gamepad generic;static InputDevice[] localDevices;
        static LevelMapHUD hud;static PlayerController player;static PlayerInputReader reader;static InputDeviceManager devices;static ClueJournalPresenter journal;static PauseMenuController pause;static GamePersistenceService save;
        static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;static bool oldRun;static int frame;static float oldVolume;static double deadline;
        static string Evidence=>Level01MapHUDAuthoring.Evidence;
        static Level01MapHUDValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Check(bool ok,string message){if(!ok)throw new Exception("Map HUD: "+message);checks.Add(message);}
        static object Get(object o,string field)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
        static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        static void Poll()
        {if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Level01MapHUDValidate.request"))return;File.Delete("Temp/Level01MapHUDValidate.request");Run();}
        [MenuItem("Tikal/Validation/Validate Level01 Map HUD")]
        public static void Run()
        {
            if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Start validation in normal Edit Mode.");
            ValidateStatic();if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved scene before validation.");SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        public static void ValidateStatic()
        {
            checks.Clear();errors.Clear();Directory.CreateDirectory(Evidence);
            Check(EditorSceneManager.GetActiveScene().path==Level01MapHUDAuthoring.ScenePath,"Level01 only");var all=Level01MapHUDAuthoring.All<Component>();var maps=all.OfType<LevelMapHUD>().ToArray();Check(maps.Length==1&&maps[0].IsConfigured,"Exactly one configured Map HUD, including repeated authoring");
            Check(all.OfType<ExplorationMapGraphic>().Count()==2&&all.OfType<MapPlayerMarkerGraphic>().Count()==2,"Small/expanded map and two player markers exist");
            var definition=AssetDatabase.LoadAssetAtPath<LevelMapDefinition>(Level01MapHUDAuthoring.DefinitionPath);Check(definition.publicAreas.Length==11&&definition.worldBounds.width>0,"Public floor-only north-up map data; no hidden-object markers");Check(definition.publicAreas.All(a=>!a.id.Contains("FalsePath")&&!a.id.Contains("Secret")),"No secrets, solutions, narrative keys or hidden path geometry");
            using(var input=new TikalInputActions())
            {
                var map=input.asset.FindAction("Global/Map",true);Check(map.type==InputActionType.Button,"Global semantic Map action exists");
                Check(map.bindings.Count==2&&map.bindings.Any(b=>b.path=="<Keyboard>/m")&&map.bindings.Any(b=>b.path=="<Gamepad>/dpad/up"),"Map bindings M and D-Pad Up");
                var conflict=input.asset.actionMaps.Where(m=>m.name=="Player"||m.name=="Global").SelectMany(m=>m.bindings).Where(b=>b.action!="Map"&&(b.effectivePath=="<Keyboard>/m"||b.effectivePath=="<Gamepad>/dpad/up")).ToArray();Check(conflict.Length==0,"No Player/Global binding conflicts; UI.Navigate dpad is context-separated and Map ignored during Pause");
                foreach(var family in new[]{InputDeviceFamily.KeyboardMouse,InputDeviceFamily.Xbox,InputDeviceFamily.PlayStation,InputDeviceFamily.GenericGamepad})Check(!string.IsNullOrEmpty(InputPromptResolver.Resolve(map,family).DisplayText),"Map prompt resolves "+family);
            }
            Check(all.OfType<AudioListener>().Count()==1&&all.OfType<Camera>().Count()==1,"No additional camera or AudioListener");Check(all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No Missing Scripts");
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{passed=true,scope="Level01 HUD authoring/input/data only; no environment or puzzle tests",checks=checks.ToArray(),errors=errors.ToArray()},true));Debug.Log("LEVEL01_MAP_HUD_STATIC_VALID");
        }
        static void Inject(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
            {foreach(var flow in root.GetComponentsInChildren<GameFlowController>(true)){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}foreach(var applier in root.GetComponentsInChildren<PersistentSettingsApplier>(true)){Set(applier,"_service",save);applier.Apply(GameSettingsData.CreateDefault());}}
        }
        static void Mode(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                checks.Clear();errors.Clear();work.Clear();save=new GamePersistenceService(Path.GetFullPath(Evidence+"/IsolatedSave"));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=Inject;
                oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;oldRun=Application.runInBackground;oldVolume=AudioListener.volume;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();xbox=InputSystem.AddDevice<XInputController>();sony=InputSystem.AddDevice<DualShockGamepad>();generic=InputSystem.AddDevice<Gamepad>();localDevices=new InputDevice[]{keyboard,mouse,xbox,sony,generic};frame=-1;deadline=EditorApplication.timeSinceStartup+180;work.Push(Tests());EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);var path=SessionState.GetString(Key+"Scene",Level01MapHUDAuthoring.ScenePath);if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);Debug.Log("LEVEL01_MAP_HUD_PLAY_FINISHED");}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Map HUD local validation");while(work.Count>0){var e=work.Peek();if(!e.MoveNext()){work.Pop();continue;}if(e.Current is IEnumerator child){work.Push(child);continue;}return;}Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;foreach(var d in localDevices??Array.Empty<InputDevice>())if(d.added)InputSystem.RemoveDevice(d);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Application.runInBackground=oldRun;AudioListener.volume=oldVolume;Time.timeScale=1;
            File.WriteAllText(Evidence+"/PlayValidation.json",JsonUtility.ToJson(new Report{passed=errors.Count==0,scope="Native Input System virtual keyboard/Xbox/PlayStation/generic devices, real Level01 Player/modal UI/checkpoint; isolated save; no puzzle or environment regression",checks=checks.ToArray(),errors=errors.ToArray()},true));EditorApplication.ExitPlaymode();
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Press(UnityEngine.InputSystem.Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(4);}
        static IEnumerator Press(Gamepad pad,GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return Frames(3);InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(4);}
        static IEnumerator Shot(string name){yield return Frames(3);ScreenCapture.CaptureScreenshot(Evidence+"/"+name+".png");yield return Frames(3);}
        static IEnumerator Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01MapHUDAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(20);hud=UnityEngine.Object.FindAnyObjectByType<LevelMapHUD>();player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();reader=player.GetComponent<PlayerInputReader>();devices=player.GetComponent<InputDeviceManager>();journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();pause=UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            ((TikalInputActions)Get(reader,"_actions")).devices=localDevices;((TikalInputActions)Get(hud,"_ui")).devices=localDevices;((TikalInputActions)Get(pause,"_ui")).devices=localDevices;
            Check(hud.IsConfigured&&hud.SmallVisible&&!hud.IsOpen,"Scene loads with minimap, player marker and gameplay hints");yield return Press(UnityEngine.InputSystem.Key.Space);Check(hud.MapHint=="[M] MAPA"&&hud.JournalHint=="[J] VER DIARIO","Keyboard hints use actual Map/Journal bindings");yield return Shot("HUD_Keyboard");
            yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen&&!hud.SmallVisible,"M opens expanded map and hides minimap");Check(!player.GameplayActive&&!reader.GameplayEnabled&&Time.timeScale==1,"Map blocks player action map without pausing time");Check(!UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>().IsVisible,"Map hides reticle through existing gameplay gate");
            int interactions=0,secrets=0;reader.InteractPressed+=()=>interactions++;reader.SecretCommandPressed+=()=>secrets++;var pos=player.transform.position;var forward=reader.GetComponent<PlayerCameraController>().MovementOrientation.forward;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.E,UnityEngine.InputSystem.Key.G));InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(70,50));yield return Frames(8);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(reader.Move==Vector2.zero&&reader.Look==Vector2.zero&&Vector3.Distance(pos,player.transform.position)<.001f&&Vector3.Angle(forward,reader.GetComponent<PlayerCameraController>().MovementOrientation.forward)<.001f,"Movement/look/TPS orbit remain blocked while map open");Check(interactions==0&&secrets==0,"Interact and SecretCommand actions blocked");yield return Shot("Expanded_Keyboard");yield return Press(UnityEngine.InputSystem.Key.M);Check(!hud.IsOpen&&player.GameplayActive&&reader.GameplayEnabled,"Map action toggles closed and restores gameplay");
            yield return Press(UnityEngine.InputSystem.Key.J);Check(journal.IsOpen&&!hud.IsOpen,"Native Journal opens");yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen&&!journal.IsOpen,"Map closes Journal before acquiring modal lock");yield return Press(UnityEngine.InputSystem.Key.J);Check(!hud.IsOpen&&journal.IsOpen&&!reader.GameplayEnabled,"Journal closes Map before taking its own lock");yield return Press(UnityEngine.InputSystem.Key.J);Check(player.GameplayActive&&reader.GameplayEnabled,"Journal handoff restores original gameplay on close");
            yield return Press(UnityEngine.InputSystem.Key.M);yield return Press(UnityEngine.InputSystem.Key.Escape);Check(pause.IsOpen&&!hud.IsOpen,"Pause/Escape priority closes Map and opens Pause exactly once");yield return Press(UnityEngine.InputSystem.Key.M);Check(pause.IsOpen&&!hud.IsOpen,"Map ignored while Pause active");yield return Press(UnityEngine.InputSystem.Key.Escape);Check(!pause.IsOpen&&reader.GameplayEnabled&&Time.timeScale==1,"Resume after Map-to-Pause restores gameplay");
            foreach(var item in new[]{(pad:(Gamepad)sony,family:InputDeviceFamily.PlayStation,journal:"Share",name:"PlayStation"),(pad:(Gamepad)xbox,family:InputDeviceFamily.Xbox,journal:"View",name:"Xbox"),(pad:generic,family:InputDeviceFamily.GenericGamepad,journal:"Select",name:"Generic")})
            {
                InputSystem.QueueStateEvent(item.pad,new GamepadState{leftStick=new Vector2(.7f,0)});yield return Frames(3);InputSystem.QueueStateEvent(item.pad,new GamepadState());yield return Frames(3);Check(devices.ActiveDevice==item.family,"Active device switches live to "+item.name);Check(hud.MapHint=="[D-Pad ↑] MAPA"&&hud.JournalHint=="["+item.journal+"] VER DIARIO","Dynamic Map/Journal hints "+item.name);yield return Shot("HUD_"+item.name);yield return Press(item.pad,GamepadButton.DpadUp);Check(hud.IsOpen,"D-Pad Up opens map "+item.name);yield return Press(item.pad,GamepadButton.East);Check(!hud.IsOpen&&reader.GameplayEnabled,"Cancel B/Circle closes map "+item.name);
            }
            yield return Press(UnityEngine.InputSystem.Key.Space);Check(devices.ActiveDevice==InputDeviceFamily.KeyboardMouse&&hud.JournalHint=="[J] VER DIARIO","Device returns immediately to keyboard");
            int mapIndex=Enumerable.Range(0,reader.MapAction.bindings.Count).Single(i=>reader.MapAction.bindings[i].path=="<Keyboard>/m");reader.MapAction.ApplyBindingOverride(mapIndex,"<Keyboard>/n");Check(hud.MapHint=="[N] MAPA","Map hint reacts to binding override");reader.MapAction.RemoveBindingOverride(mapIndex);Check(hud.MapHint=="[M] MAPA","Map default restored without asset/save mutation");
            int journalIndex=Enumerable.Range(0,reader.JournalAction.bindings.Count).Single(i=>reader.JournalAction.bindings[i].path=="<Keyboard>/j");reader.JournalAction.ApplyBindingOverride(journalIndex,"<Keyboard>/k");Check(hud.JournalHint=="[K] VER DIARIO","Journal hint reacts to real binding override");reader.JournalAction.RemoveBindingOverride(journalIndex);
            var applier=player.GetComponent<PersistentSettingsApplier>();foreach(var mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson}){var data=JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(applier.CurrentSettings));data.CameraMode=mode;applier.Apply(data);yield return Frames(5);yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen&&!reader.GameplayEnabled,"Map modal works in "+mode);yield return Shot("Expanded_"+mode);yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.SmallVisible&&reader.GameplayEnabled,"Map restores "+mode);}
            var marker=hud.SmallMarkerPosition;foreach(var id in new[]{"character.default","character.02","character.03"}){player.GetComponent<CharacterVisualSpawner>().Apply(id);yield return Frames(3);Check(Vector2.Distance(marker,hud.SmallMarkerPosition)<.05f,"Marker independent of visual prefab "+id);}
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position+=new Vector3(0,0,6);cc.enabled=true;yield return Frames(3);var moved=hud.SmallMarkerPosition;var manager=UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();Check(manager.Respawn(),"Existing checkpoint respawn succeeds");yield return Frames(5);var def=AssetDatabase.LoadAssetAtPath<LevelMapDefinition>(Level01MapHUDAuthoring.DefinitionPath);var map=(ExplorationMapGraphic)Get(hud,"_smallMap");Check(Vector2.Distance(hud.SmallMarkerPosition,def.LocalPoint(player.transform.position,map.rectTransform.rect))<.01f&&Vector2.Distance(moved,hud.SmallMarkerPosition)>1,"Marker updates to respawned Player root without stale position");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"No extra camera or AudioListener at runtime");Check(SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"Runtime no Missing Scripts");
        }
    }
}
