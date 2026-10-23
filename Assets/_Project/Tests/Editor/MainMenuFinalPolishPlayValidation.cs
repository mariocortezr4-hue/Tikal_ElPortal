using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class MainMenuFinalPolishPlayValidation
    {
        const string Key="Tikal.MainMenuFinal.Play";
        static string Folder=>MainMenuFinalPolishAuthoring.Evidence+"/Play";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static Keyboard keyboard;static Mouse mouse;static Gamepad xbox,sony,active;static GamePersistenceService service;static MainMenuController menu;static EventSystem events;static Transform bg;static int lastFrame,oldCapture;static float oldVolume;static double deadline;static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        [Serializable]class Report{public bool passed;public string scope;public string[] checks,errors;}
        static MainMenuFinalPolishPlayValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/MainMenuFinalPlay.request"))return;
            File.Delete("Temp/MainMenuFinalPlay.request");var personal=new GamePersistenceService();SessionState.SetString(Key+"ProgressBefore",File.Exists(personal.ProgressPath)?File.ReadAllText(personal.ProgressPath):"<absent>");SessionState.SetString(Key+"SettingsBefore",File.Exists(personal.SettingsPath)?File.ReadAllText(personal.SettingsPath):"<absent>");MainMenuFinalPolishAuthoring.Check(!EditorSceneManager.GetActiveScene().isDirty,"Save scene first");var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));SessionState.SetBool(Key+"Max",game.maximized);game.maximized=true;Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Mode(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {checks.Clear();errors.Clear();stack.Clear();service=new GamePersistenceService(Path.Combine(Path.GetTempPath(),"TikalMenuFinal-"+Guid.NewGuid().ToString("N")));var progress=GameProgressData.CreateDefault();progress.HasStartedGame=true;progress.IntroSeen=true;progress.TutorialCompleted=true;service.SaveProgress(progress);oldCapture=Time.captureFramerate;Time.captureFramerate=30;oldVolume=AudioListener.volume;oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();xbox=InputSystem.AddDevice<XInputController>();sony=InputSystem.AddDevice<DualShockGamepad>();Application.logMessageReceived+=Log;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+420;stack.Push(Tests());EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).maximized=SessionState.GetBool(Key+"Max",false);SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(MainMenuFinalPolishAuthoring.ScenePath);Debug.Log("TIKAL_MAIN_MENU_FINAL_PLAY_FINISHED");}
        }
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add(message);Debug.Log("MENU_FINAL_CHECK "+message);}
        static void Tick(){if(!EditorApplication.isPlaying||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Local menu test deadline");while(stack.Count>0){var e=stack.Peek();if(!e.MoveNext()){stack.Pop();continue;}if(e.Current is IEnumerator child){stack.Push(child);continue;}return;}Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}}
        static void Finish(){var personal=new GamePersistenceService();if((File.Exists(personal.ProgressPath)?File.ReadAllText(personal.ProgressPath):"<absent>")!=SessionState.GetString(Key+"ProgressBefore","")||(File.Exists(personal.SettingsPath)?File.ReadAllText(personal.SettingsPath):"<absent>")!=SessionState.GetString(Key+"SettingsBefore",""))errors.Add("Personal save data changed during isolated test");else checks.Add("Personal progress/settings files unchanged byte-for-byte");EditorApplication.update-=Tick;Application.logMessageReceived-=Log;foreach(var d in new InputDevice[]{keyboard,mouse,xbox,sony})if(d!=null&&d.added)InputSystem.RemoveDevice(d);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Time.captureFramerate=oldCapture;AudioListener.volume=oldVolume;File.WriteAllText(Folder+"/Validation.json",JsonUtility.ToJson(new Report{passed=errors.Count==0,checks=checks.ToArray(),errors=errors.ToArray(),scope="MainMenu only; native InputSystem keyboard/mouse and virtual Xbox/PlayStation stick+D-pad through real EventSystem. Existing persistence controllers injected with isolated temp service before any interaction. No gameplay scenes or user saves altered; physical controller human playtest remains useful."},true));EditorApplication.ExitPlaymode();}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator KeyPress(UnityEngine.InputSystem.Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(4);}
        static IEnumerator PadPress(GamepadButton button){InputSystem.QueueStateEvent(active,new GamepadState().WithButton(button));yield return Frames(3);InputSystem.QueueStateEvent(active,new GamepadState());yield return Frames(4);}
        static IEnumerator Move(Vector2 direction,bool stick=false)
        {if(active==null){yield return KeyPress(direction.y<0?UnityEngine.InputSystem.Key.DownArrow:direction.y>0?UnityEngine.InputSystem.Key.UpArrow:direction.x<0?UnityEngine.InputSystem.Key.LeftArrow:UnityEngine.InputSystem.Key.RightArrow);}else{var state=new GamepadState();if(stick)state.leftStick=direction;else state=state.WithButton(direction.y<0?GamepadButton.DpadDown:direction.y>0?GamepadButton.DpadUp:direction.x<0?GamepadButton.DpadLeft:GamepadButton.DpadRight);InputSystem.QueueStateEvent(active,state);yield return Frames(3);InputSystem.QueueStateEvent(active,new GamepadState());yield return Frames(4);}}
        static IEnumerator Submit(){if(active==null)yield return KeyPress(UnityEngine.InputSystem.Key.Enter);else yield return PadPress(GamepadButton.South);}
        static IEnumerator Cancel(){if(active==null)yield return KeyPress(UnityEngine.InputSystem.Key.Escape);else yield return PadPress(GamepadButton.East);}
        static void Focus(Transform t)=>events.SetSelectedGameObject(t.gameObject);
        static MenuSelectableVisual Visual(Transform t)=>t.GetComponent<MenuSelectableVisual>();
        static IEnumerator Open(string name,MenuPanelContext panel){Focus(bg.Find("Main/"+name));yield return Submit();Check(menu.CurrentPanel==panel,"Native open "+name);Check(events.currentSelectedGameObject!=null&&events.currentSelectedGameObject.GetComponent<MenuSelectableVisual>().HasFocus,"Clear initial focus "+name);}
        static IEnumerator Capture(string name){yield return Frames(18);ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");yield return Frames(4);}
        static IEnumerator Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(MainMenuFinalPolishAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);menu=UnityEngine.Object.FindAnyObjectByType<MainMenuController>();events=UnityEngine.Object.FindAnyObjectByType<EventSystem>();bg=menu.transform.Find("Canvas/Background");
            // Inactive panels run Awake on first activation. Warm them before replacing their services.
            foreach(var name in new[]{"CharacterSelection","Settings"}){bg.Find(name).gameObject.SetActive(true);bg.Find(name).gameObject.SetActive(false);}
            var flow=menu.GetComponent<GameFlowController>();Set(flow,"_persistence",service);Set(flow,"_progress",service.LoadProgress());var chars=bg.Find("CharacterSelection").GetComponent<CharacterSelectionController>();Set(chars,"_persistence",service);Set(chars,"_progress",service.LoadProgress());Set(chars,"_pendingCharacterId",CharacterIds.Default);Set(bg.Find("Settings").GetComponent<SettingsMenuController>(),"_persistence",service);
            MainMenuFinalPolishValidation.ValidateScene(menu.gameObject.scene);Check(events.currentSelectedGameObject==bg.Find("Main/Play").gameObject,"Initial home focus Play");yield return Capture("MainMenu_Home");
            foreach(var device in new Gamepad[]{null,xbox,sony})
            {
                active=device;string family=device==null?"Keyboard":device==xbox?"Xbox":"PlayStation";Focus(bg.Find("Main/Play"));
                foreach(bool stick in device==null?new[]{false}:new[]{false,true})
                {Focus(bg.Find("Main/Play"));foreach(var name in new[]{"Levels","Character","Cinematics","Tutorial","Settings","Credits","Quit"}){yield return Move(Vector2.down,stick);Check(events.currentSelectedGameObject==bg.Find("Main/"+name).gameObject&&Visual(bg.Find("Main/"+name)).HasFocus,family+" "+(stick?"stick":"Dpad/arrows")+" focus "+name);}}
                Check(menu.GetComponent<InputDeviceManager>().ActiveDevice==(device==null?InputDeviceFamily.KeyboardMouse:device==xbox?InputDeviceFamily.Xbox:InputDeviceFamily.PlayStation),"Native device detection "+family);
                if(device==sony){Focus(bg.Find("Main/Settings"));yield return Capture("MainMenu_Focused");}
                foreach(var entry in new[]{("Levels",MenuPanelContext.Levels),("Cinematics",MenuPanelContext.Cinematics),("Tutorial",MenuPanelContext.Tutorial),("Credits",MenuPanelContext.Credits)}){yield return Open(entry.Item1,entry.Item2);yield return Cancel();Check(menu.CurrentPanel==MenuPanelContext.Main,"Native cancel "+entry.Item1+" "+family);}
                yield return Open("Character",MenuPanelContext.Character);var cards=Enumerable.Range(1,4).Select(i=>bg.Find("CharacterSelection/Character0"+i)).ToArray();
                yield return Move(Vector2.right);Check(events.currentSelectedGameObject==cards[1].gameObject,"Character right to Axel "+family);yield return Move(Vector2.right,true);Check(events.currentSelectedGameObject==cards[2].gameObject,"Character right to Mario "+family);yield return Move(Vector2.right);Check(events.currentSelectedGameObject==cards[2].gameObject&&!cards[3].GetComponent<Button>().interactable,"Unknown cannot receive navigation/selection "+family);
                yield return Submit();yield return Move(Vector2.down);Check(events.currentSelectedGameObject.name=="ConfirmCharacter","Existing confirm action "+family);yield return Submit();Check(service.LoadProgress().SelectedCharacterId==CharacterIds.Character03,"Mario saved through original Confirm "+family);yield return Move(Vector2.up);yield return Move(Vector2.left);yield return Move(Vector2.left);Check(Visual(cards[0]).HasFocus&&Visual(cards[2]).IsPersistent&&!Visual(cards[0]).IsPersistent,"Dwight FOCUSED while Mario SELECTED "+family);
                if(device==sony)
                {
                    for(int i=0;i<3;i++){Focus(cards[i]);yield return Submit();yield return Move(Vector2.down);yield return Submit();Focus(cards[i==2?0:i]);yield return Capture("CharacterSelection_"+CharacterCatalog.All[i].DisplayName);}
                    yield return Capture("CharacterSelection_UnknownLocked");
                }
                yield return Cancel();yield return Open("Settings",MenuPanelContext.Settings);var camera=bg.Find("Settings/CameraMode").GetComponent<MenuSegmentedChoice>();var reticle=bg.Find("Settings/Reticle").GetComponent<MenuSegmentedChoice>();
                yield return Move(Vector2.down);yield return Move(Vector2.down);yield return Move(Vector2.down);Check(events.currentSelectedGameObject==reticle.Segments[0].gameObject,"Native settings navigation reaches reticle from first control "+family);yield return Move(Vector2.right);yield return Move(Vector2.right);yield return Move(Vector2.right);Check(events.currentSelectedGameObject==camera.Segments[0].gameObject,"Native settings navigation crosses reticle to camera "+family);
                Focus(camera.Segments[1].transform);yield return Submit();yield return Move(Vector2.left);Check(camera.Source.value==1&&Visual(camera.Segments[0].transform).HasFocus&&Visual(camera.Segments[1].transform).IsPersistent,"First Person FOCUSED while Third Person SELECTED "+family);if(device==sony)yield return Capture("Settings_CameraMode");yield return Submit();Check(service.LoadSettings().CameraMode==Tikal.Persistence.CameraMode.FirstPerson,"Native camera choice saves First Person "+family);yield return Move(Vector2.right,true);yield return Submit();Check(service.LoadSettings().CameraMode==Tikal.Persistence.CameraMode.ThirdPerson,"Native camera choice saves Third Person "+family);
                for(int i=0;i<3;i++){Focus(reticle.Segments[i].transform);yield return Submit();Check(reticle.Source.value==i&&service.LoadSettings().ReticleMode==(i==0?Tikal.UI.ReticleMode.None:i==1?Tikal.UI.ReticleMode.Dot:Tikal.UI.ReticleMode.Cross),"Native reticle choice and persistence "+i+" "+family);}
                yield return Move(Vector2.left);yield return Move(Vector2.left,true);Check(Visual(reticle.Segments[0].transform).HasFocus&&Visual(reticle.Segments[2].transform).IsPersistent,"None FOCUSED while Cross SELECTED "+family);if(device==sony)yield return Capture("Settings_Reticle");yield return Cancel();yield return Open("Settings",MenuPanelContext.Settings);yield return Frames(4);Check(camera.Source.value==1&&reticle.Source.value==2&&Visual(camera.Segments[1].transform).IsPersistent&&Visual(reticle.Segments[2].transform).IsPersistent,"Panel reopen preserves camera/reticle "+family);yield return Cancel();yield return Open("Character",MenuPanelContext.Character);Check(Visual(cards[2]).IsPersistent&&service.LoadProgress().SelectedCharacterId==CharacterIds.Character03,"Panel reopen preserves Mario "+family);yield return Cancel();
            }
            active=null;yield return Open("Character",MenuPanelContext.Character);var dwight=bg.Find("CharacterSelection/Character01");var mario=bg.Find("CharacterSelection/Character03");Focus(mario);var point=RectTransformUtility.WorldToScreenPoint(null,dwight.TransformPoint(((RectTransform)dwight).rect.center));InputSystem.QueueStateEvent(mouse,new MouseState{position=point,delta=new Vector2(10,0)});yield return Frames(6);Check(Visual(mario).IsPersistent&&Visual(mario).HasFocus&&dwight.Find("HoverOutline").gameObject.activeSelf,"Native mouse hover distinct from focus; selected persists");InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return Frames(3);InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return Frames(4);Check(events.currentSelectedGameObject==dwight.gameObject,"Native mouse click focuses Dwight");Focus(bg.Find("CharacterSelection/ConfirmCharacter"));yield return Submit();Check(service.LoadProgress().SelectedCharacterId==CharacterIds.Default,"Mouse card click proposes Dwight; existing Confirm persists");chars.Select(3);chars.Confirm();Check(service.LoadProgress().SelectedCharacterId==CharacterIds.Default,"Unknown direct select refused by original controller");Check(service.LoadProgress().HasCompletedOnboarding,"Onboarding state preserved");yield return Cancel();
        }
    }
}
