using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles.Level01;
using Tikal.Puzzles.Clues;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.DualShock;
namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class InscriptionValidation
    {
        const string Active="Tikal.Inscription.Active",Result="Tikal.Inscription.Result";
        static double deadline;
        static GameObject panel;
        static int checks;
        static void Require(bool value,string message){if(!value)throw new Exception("Inscription: "+message);checks++;Debug.Log("INSCRIPTION_CHECK: "+message);}
        static InscriptionValidation(){EditorApplication.playModeStateChanged+=State;}
        public static void Run()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c!=null),"No Missing Scripts");DynamicPromptValidation.ValidateScene(components);
                var inscription=components.OfType<ReadableInscription>().Single(i=>i.Text=="Debes ordenar el patrón.");
                Require(inscription.transform.parent.name=="Zone02_PathGuardians","Optional inscription in Zone02");
                var presenter=components.OfType<InscriptionPresenter>().Single();
                var so=new SerializedObject(presenter);Require(so.FindProperty("_duration").floatValue==5,"Five second nonblocking reading");
                Require(components.OfType<Canvas>().Count()==1,"Existing Canvas reused");
                SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try{ValidateInput();deadline=EditorApplication.timeSinceStartup+5.6;EditorApplication.update+=Tick;}
                catch(Exception e){Fail(e);}
            }
            else if(change==PlayModeStateChange.EnteredEditMode)
            {EditorApplication.update-=Tick;SessionState.SetBool(Active,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Result,1));}
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<deadline)return;
            try{Require(!panel.activeSelf,"Reading automatically disappears");Debug.Log("TIKAL_INSCRIPTION_TESTS_PASSED checks="+checks);SessionState.SetInt(Result,0);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
            catch(Exception e){Fail(e);}
        }
        static void Fail(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;SessionState.SetInt(Result,1);EditorApplication.ExitPlaymode();}
        static void Refresh(PlayerInteraction i,InteractionPromptPresenter p)
        {
            Physics.SyncTransforms();typeof(PlayerInteraction).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(i,null);
            typeof(InteractionPromptPresenter).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,null);
        }
        static void ValidateInput()
        {
            var readable=UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(i=>i.Text=="Debes ordenar el patrón.");
            var puzzle=UnityEngine.Object.FindAnyObjectByType<PathGuardiansPuzzle>();var state=puzzle.State;var tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();int clues=tracker.Count;
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();player.enabled=false;
            var reader=player.GetComponent<PlayerInputReader>();reader.EnableGameplay();var interaction=player.GetComponent<PlayerInteraction>();
            var presenter=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();var ps=new SerializedObject(presenter);var label=(TMP_Text)ps.FindProperty("_label").objectReferenceValue;
            var rs=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<InscriptionPresenter>());panel=(GameObject)rs.FindProperty("_panel").objectReferenceValue;
            var body=(TMP_Text)rs.FindProperty("_text").objectReferenceValue;
            var camera=(Camera)new SerializedObject(interaction).FindProperty("_viewCamera").objectReferenceValue;
            var target=readable.GetComponentInChildren<Collider>().bounds.center;camera.transform.position=target+Vector3.left*1.8f;camera.transform.LookAt(target);
            var settings=InputSystem.settings;var bg=settings.backgroundBehavior;var ed=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var k=InputSystem.AddDevice<Keyboard>();var x=InputSystem.AddDevice<XInputController>();var s=InputSystem.AddDevice<DualShockGamepad>();
            var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(reader);var old=actions.devices;actions.devices=new InputDevice[]{k,x,s};int reads=0;readable.ReadRequested+=_=>reads++;
            try
            {
                InputSystem.QueueStateEvent(k,new KeyboardState(Key.H));InputSystem.Update();InputSystem.QueueStateEvent(k,new KeyboardState());InputSystem.Update();Refresh(interaction,presenter);
                Require(ReferenceEquals(interaction.CurrentInteractable,readable)&&label.text=="[ E ]  Leer","Approachable native keyboard Leer prompt");
                InputSystem.QueueStateEvent(k,new KeyboardState(Key.G));InputSystem.Update();InputSystem.QueueStateEvent(k,new KeyboardState());InputSystem.Update();Refresh(interaction,presenter);Require(reads==0,"Secret key does not read or alter puzzle");
                InputSystem.QueueStateEvent(k,new KeyboardState(Key.E));InputSystem.Update();InputSystem.QueueStateEvent(k,new KeyboardState());InputSystem.Update();Refresh(interaction,presenter);Require(reads==1&&panel.activeSelf&&body.text==readable.Text,"Native E presents exact inscription");
                foreach(var pad in new Gamepad[]{x,s})
                {
                    InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));InputSystem.Update();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Refresh(interaction,presenter);
                    Require(label.text==(pad==x?"[ X ]  Leer":"[ □ ]  Leer"),"Native controller Leer prompt "+pad.layout);
                    int before=reads;InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North));InputSystem.Update();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Refresh(interaction,presenter);Require(reads==before,"Secret controller button has no effect");
                    InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));InputSystem.Update();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Refresh(interaction,presenter);Require(reads==before+1&&body.text==readable.Text,"Native west reads "+pad.layout);
                }
                Require(reader.GameplayEnabled,"Reading leaves gameplay enabled");Require(tracker.Count==clues&&puzzle.State==state,"Reading gives no clue and does not change Puzzle02");
                var so=new SerializedObject(puzzle);var pieces=so.FindProperty("_guardians");var answers=so.FindProperty("_solution");int turns=0;
                for(int n=0;n<3;n++){var g=(PathGuardian)pieces.GetArrayElementAtIndex(n).objectReferenceValue;turns+=(answers.GetArrayElementAtIndex(n).intValue-g.CurrentOrientation+4)%4;}
                Require(turns==3&&puzzle.IsGuardianAligned(0),"Initial example A and minimum three turns preserved");
                var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.path_guardians.asset");Require(clue.Id=="level01.clue02"&&clue.Title=="El origen"&&clue.Description=="Todo camino recuerda dónde comenzó.","Clue02 unchanged");
            }
            finally{actions.devices=old;InputSystem.RemoveDevice(k);InputSystem.RemoveDevice(x);InputSystem.RemoveDevice(s);settings.backgroundBehavior=bg;settings.editorInputBehaviorInPlayMode=ed;}
        }
    }
}
