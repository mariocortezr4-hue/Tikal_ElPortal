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
    public static class Level01MapPresentationValidation
    {
        const string Key="Tikal.MapPresentation.Validation";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly Stack<IEnumerator> work=new Stack<IEnumerator>();
        [Serializable] class Report {public bool passed;public string scope;public string[] checks,errors;}
        static Keyboard keyboard;static Mouse mouse;static XInputController xbox;static DualShockGamepad sony;static Gamepad generic;static InputDevice[] localDevices;
        static LevelMapHUD hud;static PlayerController player;static PlayerInputReader reader;static InputDeviceManager devices;static ClueJournalPresenter journal;static PauseMenuController pause;static GamePersistenceService save;
        static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;static bool oldRun;static int frame;static float oldVolume;static double deadline;
        const string Evidence="../_Level01MapPresentation_20261023";
        static Level01MapPresentationValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Check(bool ok,string message){if(!ok)throw new Exception("Map HUD: "+message);checks.Add(message);}
        static object Get(object o,string field)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
        static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        static void Poll()
        {if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Level01MapPresentationValidate.request"))return;File.Delete("Temp/Level01MapPresentationValidate.request");Run();}
        [MenuItem("Tikal/Validation/Validate Level01 Map Presentation")]
        public static void Run()
        {
            if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Start validation in normal Edit Mode.");
            ValidateStatic();SetCaptureResolution();if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved scene before validation.");SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        public static void ValidateStatic()
        {
            checks.Clear();errors.Clear();Directory.CreateDirectory(Evidence);
            Check(EditorSceneManager.GetActiveScene().path==Level01MapHUDAuthoring.ScenePath,"Level01 visual scope");
            var all=Level01MapHUDAuthoring.All<Component>();var map=all.OfType<LevelMapHUD>().Single();Check(map.IsConfigured,"Configured original HUD");
            Check(all.OfType<ExplorationMapGraphic>().Count()==2&&all.OfType<MapPlayerMarkerGraphic>().Count()==2,"Exactly two maps and markers");
            Check(map.GetComponentsInChildren<ExpeditionMapFrame>(true).Length==6,"Exactly six frame/compass/rule decorations, no duplicate UI");
            Check(map.GetComponentsInChildren<TMPro.TMP_Text>(true).GroupBy(t=>t.transform.parent.name+"/"+t.name).All(g=>g.Count()==1),"No duplicate labels or footer");
            Check(map.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(MaterialValid),"No Missing UI Materials");
            Check(all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No Missing Scripts");
            Check(all.OfType<Camera>().Count()==1&&all.OfType<AudioListener>().Count()==1,"No additional camera or AudioListener");
            Check(map.GetComponentsInChildren<ExplorationMapGraphic>(true).All(g=>g.playerRoot!=null),"Current-area styling references Player root");
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{passed=true,scope="Map visual presentation only",checks=checks.ToArray(),errors=errors.ToArray()},true));
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
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();xbox=InputSystem.AddDevice<XInputController>();sony=InputSystem.AddDevice<DualShockGamepad>();generic=InputSystem.AddDevice<Gamepad>();localDevices=new InputDevice[]{keyboard,mouse,xbox,sony,generic};frame=-1;deadline=EditorApplication.timeSinceStartup+240;work.Push(Tests());EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode){RestoreResolution();SessionState.SetBool(Key,false);var path=SessionState.GetString(Key+"Scene",Level01MapHUDAuthoring.ScenePath);if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);Debug.Log("LEVEL01_MAP_PRESENTATION_FINISHED");}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Map HUD local validation");while(work.Count>0){var e=work.Peek();if(!e.MoveNext()){work.Pop();continue;}if(e.Current is IEnumerator child){work.Push(child);continue;}return;}Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;foreach(var d in localDevices??Array.Empty<InputDevice>())if(d.added)InputSystem.RemoveDevice(d);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Application.runInBackground=oldRun;AudioListener.volume=oldVolume;Time.timeScale=1;
            File.WriteAllText(Evidence+"/PlayValidation.json",JsonUtility.ToJson(new Report{passed=errors.Count==0,scope="Focused map visual presentation, virtual keyboard/Xbox/PlayStation, original modal handoffs, FPS/TPS; 1920x1080; isolated save; no puzzle tests",checks=checks.ToArray(),errors=errors.ToArray()},true));EditorApplication.ExitPlaymode();
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Press(UnityEngine.InputSystem.Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(4);}
        static IEnumerator Press(Gamepad pad,GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return Frames(3);InputSystem.QueueStateEvent(pad,new GamepadState());yield return Frames(4);}
        static IEnumerator Shot(string name){yield return Frames(3);ScreenCapture.CaptureScreenshot(Evidence+"/"+name+".png");yield return Frames(3);}
        static bool MaterialValid(UnityEngine.UI.Graphic g)
        {
            // Never call TMP_SubMeshUI.material: that getter creates a material instance.
            // Empty cached fallback submeshes do not render; examine shared resources instead.
            if(!g.gameObject.activeInHierarchy)return true;
            if(g is TMPro.TMP_SubMeshUI sub)
            {
                int index=(int)Get(sub,"m_materialReferenceIndex");var info=sub.textComponent.textInfo;
                if(index>=info.materialCount||info.meshInfo[index].vertexCount==0)return true;
                return sub.sharedMaterial!=null&&sub.sharedMaterial.shader!=null;
            }
            var material=g is TMPro.TMP_Text text?text.fontSharedMaterial:g.material;
            return material!=null&&material.shader!=null;
        }
        static EditorWindow gameView;static int oldSizeIndex,customSizeIndex=-1;static object sizeGroup;
        static void SetCaptureResolution()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");gameView=EditorWindow.GetWindow(viewType);var prop=viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);oldSizeIndex=(int)prop.GetValue(gameView);
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");sizeGroup=sizesType.GetMethod("GetGroup").Invoke(singleton,new[]{System.Enum.Parse(groupType,"Standalone")});
            int count=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);var sizeType=assembly.GetType("UnityEditor.GameViewSize");var modeType=assembly.GetType("UnityEditor.GameViewSizeType");var size=System.Activator.CreateInstance(sizeType,new[]{System.Enum.Parse(modeType,"FixedResolution"),(object)1920,1080,"Map presentation capture"});sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});customSizeIndex=count;prop.SetValue(gameView,count);gameView.Repaint();
        }
        static void RestoreResolution()
        {
            if(gameView==null||sizeGroup==null)return;
            gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(gameView,oldSizeIndex);
            int builtin=(int)sizeGroup.GetType().GetMethod("GetBuiltinCount").Invoke(sizeGroup,null);sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{customSizeIndex-builtin});gameView.Repaint();
        }
        static IEnumerator Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01MapHUDAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(20);
            hud=UnityEngine.Object.FindAnyObjectByType<LevelMapHUD>();player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();reader=player.GetComponent<PlayerInputReader>();devices=player.GetComponent<InputDeviceManager>();journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();pause=UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            ((TikalInputActions)Get(reader,"_actions")).devices=localDevices;((TikalInputActions)Get(hud,"_ui")).devices=localDevices;((TikalInputActions)Get(pause,"_ui")).devices=localDevices;
            Check(Screen.width==1920&&Screen.height==1080,"Capture layout is 1920x1080 / 16:9");
            yield return Press(UnityEngine.InputSystem.Key.Space);Check(hud.SmallVisible&&!hud.IsOpen,"Minimap visible");Check(hud.MapHint=="[M] MAPA"&&hud.JournalHint=="[J] VER DIARIO","Dynamic keyboard Map and Journal hints unchanged");yield return Shot("MapHUD_Keyboard");
            yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen&&!hud.SmallVisible,"Map opens using approved M action");yield return Frames(3);
            var graphics=hud.GetComponentsInChildren<ExplorationMapGraphic>(true);var definition=graphics[0].definition;
            var small=(ExplorationMapGraphic)Get(hud,"_smallMap");var marker=(RectTransform)Get(hud,"_smallMarker");
            Check(Vector2.Distance(hud.SmallMarkerPosition,definition.LocalPoint(player.transform.position,small.rectTransform.rect))<.01f,"Marker projection unchanged");
            Check(Mathf.Abs(Mathf.DeltaAngle(marker.localEulerAngles.z,-player.transform.eulerAngles.y))<.01f,"Player marker yaw / north-up unchanged");
            Check(graphics.All(g=>g.CurrentAreaIndex>=0),"Current public area highlighted");yield return Shot("ExpandedMap_Keyboard");
            yield return Press(UnityEngine.InputSystem.Key.M);Check(!hud.IsOpen&&reader.GameplayEnabled,"Map closes and restores control");
            yield return Press(UnityEngine.InputSystem.Key.J);yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen&&!journal.IsOpen,"Journal to Map exclusivity preserved");yield return Press(UnityEngine.InputSystem.Key.J);Check(!hud.IsOpen&&journal.IsOpen,"Map to Journal exclusivity preserved");yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Press(UnityEngine.InputSystem.Key.M);yield return Press(UnityEngine.InputSystem.Key.Escape);Check(pause.IsOpen&&!hud.IsOpen,"Pause priority preserved");yield return Press(UnityEngine.InputSystem.Key.Escape);
            foreach(var item in new[]{(pad:(Gamepad)xbox,family:InputDeviceFamily.Xbox,journal:"View",name:"Xbox"),(pad:(Gamepad)sony,family:InputDeviceFamily.PlayStation,journal:"Share",name:"PlayStation")})
            {
                InputSystem.QueueStateEvent(item.pad,new GamepadState{leftStick=new Vector2(.6f,0)});yield return Frames(3);InputSystem.QueueStateEvent(item.pad,new GamepadState());yield return Frames(3);
                Check(devices.ActiveDevice==item.family&&hud.MapHint=="[D-Pad ↑] MAPA"&&hud.JournalHint=="["+item.journal+"] VER DIARIO","Dynamic "+item.name+" hints preserved");
                if(item.name=="Xbox")yield return Shot("MapHUD_Gamepad");yield return Press(item.pad,GamepadButton.DpadUp);Check(hud.IsOpen,"Gamepad map presentation "+item.name);yield return Shot("ExpandedMap_"+item.name);yield return Press(item.pad,GamepadButton.East);Check(!hud.IsOpen,"Cancel closes "+item.name);
            }
            yield return Press(UnityEngine.InputSystem.Key.Space);
            var applier=player.GetComponent<PersistentSettingsApplier>();foreach(var mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson})
            {var data=JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(applier.CurrentSettings));data.CameraMode=mode;applier.Apply(data);yield return Frames(4);Check(hud.SmallVisible,"Minimap correct "+mode);yield return Press(UnityEngine.InputSystem.Key.M);Check(hud.IsOpen,"Expanded HUD correct "+mode);yield return Press(UnityEngine.InputSystem.Key.M);}
            int originalArea=small.CurrentAreaIndex;var cc=player.GetComponent<CharacterController>();cc.enabled=false;var zone=definition.publicAreas[2].worldXZ;player.transform.position=new Vector3(zone.center.x,player.transform.position.y,zone.center.y);cc.enabled=true;yield return Frames(3);
            yield return Press(UnityEngine.InputSystem.Key.M);Check(small.CurrentAreaIndex==2&&small.CurrentAreaIndex!=originalArea,"Current-zone highlight follows Player without changing data");yield return Shot("ExpandedMap_PlayerDifferentZone");
            var invalid=hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Where(g=>!MaterialValid(g)).ToArray();
            Check(invalid.Length==0,"No Missing UI Materials in Play Mode"+(invalid.Length==0?"":": "+string.Join(", ",invalid.Select(g=>g.transform.parent.name+"/"+g.name+" ("+g.GetType().Name+")"))));
            Check(hud.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No Missing Scripts in HUD");
            Check(hud.GetComponentsInChildren<ExpeditionMapFrame>(true).Length==6,"No duplicated presentation UI in Play Mode");yield return Press(UnityEngine.InputSystem.Key.M);
        }
    }
}
