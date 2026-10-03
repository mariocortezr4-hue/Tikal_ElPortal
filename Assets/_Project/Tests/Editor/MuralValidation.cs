using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles;
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
    public static class MuralValidation
    {
        const string Active="Tikal.Mural.Active",Result="Tikal.Mural.Result";
        static int checks,stage;
        static double deadline,wallDeadline;
        static MuralFragmentsPuzzle puzzle;
        static MuralFragment[] pieces;
        static int[] initial,solution;
        static int solves;
        static ClueTracker tracker;
        static ClueDiscoveryPresenter discovery;
        static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
        static void Require(bool value,string message)
        {if(!value)throw new Exception("Mural: "+message);checks++;Debug.Log("MURAL_CHECK: "+message);}
        static MuralValidation(){EditorApplication.playModeStateChanged+=State;}
        [MenuItem("Tikal/Tests/First mural and clue")]
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c!=null),"No Missing Scripts");DynamicPromptValidation.ValidateScene(components);
                Require(components.OfType<MuralFragmentsPuzzle>().Count()==1,"Exactly one mural installed");
                puzzle=components.OfType<MuralFragmentsPuzzle>().Single();
                var so=new SerializedObject(puzzle);var refs=so.FindProperty("_fragments");var answers=so.FindProperty("_solution");
                Require(refs.arraySize==4&&answers.arraySize==4,"Exactly four authored fragments and solution entries");
                pieces=Enumerable.Range(0,4).Select(i=>(MuralFragment)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                solution=Enumerable.Range(0,4).Select(i=>answers.GetArrayElementAtIndex(i).intValue).ToArray();
                Require(pieces.All(p=>p!=null)&&pieces.Distinct().Count()==4,"Four unique valid fragments");
                initial=pieces.Select(p=>new SerializedObject(p).FindProperty("_initialOrientation").intValue).ToArray();
                for(int i=0;i<4;i++)
                {
                    var fs=new SerializedObject(pieces[i]);Require(fs.FindProperty("_puzzle").objectReferenceValue==puzzle&&fs.FindProperty("_visual").objectReferenceValue!=null,"Fragment references valid "+i);
                    Require(initial[i]!=solution[i]&&solution[i]>=0&&solution[i]<4,"Deterministic incorrect initial index "+i);
                    Require(Mathf.Approximately(fs.FindProperty("_rotationDuration").floatValue,.3f),"Mechanical 0.3 second turn "+i);
                }
                Require(puzzle.transform.parent.name=="PuzzleAnchor"&&puzzle.transform.parent.parent.name=="Zone01_MuralFragments","Installed at existing Zone01 anchor");
                tracker=components.OfType<ClueTracker>().Single();discovery=components.OfType<ClueDiscoveryPresenter>().Single();
                var reward=new SerializedObject(puzzle.GetComponent<PuzzleClueReward>());
                Require(reward.FindProperty("_puzzle").objectReferenceValue==puzzle&&reward.FindProperty("_tracker").objectReferenceValue==tracker,"Reward composed outside PuzzleBase");
                var clue=(ClueDefinition)reward.FindProperty("_clue").objectReferenceValue;
                Require(clue.Id=="level01.clue01"&&clue.Title=="El camino"&&clue.Description=="El camino verdadero no siempre está delante de ti.","Clue01 narrative contract");
                SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    puzzle=UnityEngine.Object.FindAnyObjectByType<MuralFragmentsPuzzle>();
                    pieces=puzzle.GetComponentsInChildren<MuralFragment>().OrderBy(f=>f.name).ToArray();
                    tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();discovery=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();
                    UnityEngine.Object.FindAnyObjectByType<PlayerController>().enabled=false;
                    UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>().EnableGameplay();
                    solves=0;puzzle.PuzzleSolved+=_=>solves++;
                    Require(puzzle.State==PuzzleState.Available&&pieces.All(f=>f.CanInteract),"Available and all four interactable at entry");
                    pieces[0].Interact();Require(pieces[0].IsRotating&&!pieces[0].CanInteract,"Real coroutine starts and suppresses repeated input");
                    Require(pieces[0].CurrentOrientation==initial[0],"Index is committed only after visual turn finishes");
                    stage=1;deadline=Time.timeAsDouble+.6;wallDeadline=EditorApplication.timeSinceStartup+20;EditorApplication.update+=Tick;
                }
                catch(Exception e){Fail(e);}
            }
            else if(change==PlayModeStateChange.EnteredEditMode)
            {EditorApplication.update-=Tick;int result=SessionState.GetInt(Result,1);SessionState.SetBool(Active,false);if(Application.isBatchMode)EditorApplication.Exit(result);}
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(stage==1&&Time.timeAsDouble<deadline&&EditorApplication.timeSinceStartup<wallDeadline)return;
            if(stage==2&&EditorApplication.timeSinceStartup<deadline)return;
            try
            {
                if(stage==1)
                {
                    Require(!pieces[0].IsRotating&&pieces[0].CurrentOrientation==(initial[0]+1)%4,"Actual Unity frame coroutine completes smoothly (gameTime="+Time.timeAsDouble+", rotating="+pieces[0].IsRotating+")");
                    Require(puzzle.ResetPuzzle(),"Active reset permitted");
                    ValidateRotations();ValidatePrompts();Solve();
                    stage=2;deadline=EditorApplication.timeSinceStartup+6.5;
                }
                else
                {
                    var panel=(GameObject)new SerializedObject(discovery).FindProperty("_panel").objectReferenceValue;
                    Require(!panel.activeSelf,"Discovery auto dismisses using realtime after six seconds");
                    SessionState.SetInt(Result,0);Debug.Log("TIKAL_MURAL_VALIDATION_PASSED checks="+checks);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e){Fail(e);}
        }
        static void Fail(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
        static void Finish(MuralFragment f)
        {for(int n=0;n<20;n++)Method(typeof(MuralFragment),"AdvanceRotation").Invoke(f,new object[]{.02f});}
        static Transform Visual(MuralFragment f)=>(Transform)new SerializedObject(f).FindProperty("_visual").objectReferenceValue;
        static void ValidateRotations()
        {
            for(int i=0;i<4;i++)
            {
                int changes=0,finished=0;Action<MuralFragment> change=_=>changes++,end=_=>finished++;
                pieces[i].OrientationChanged+=change;pieces[i].RotationFinished+=end;
                for(int turn=0;turn<16;turn++)
                {
                    int before=pieces[i].CurrentOrientation;pieces[i].Interact();for(int n=0;n<10;n++)pieces[i].Interact();
                    Require(pieces[i].IsRotating&&pieces[i].CurrentOrientation==before,"Repeated requests ignored during turn "+i+"/"+turn);
                    Finish(pieces[i]);Require(pieces[i].CurrentOrientation==(before+1)%4,"Discrete cyclic orientation "+i+"/"+turn);
                    Require(Quaternion.Angle(Visual(pieces[i]).localRotation,Quaternion.AngleAxis(pieces[i].CurrentOrientation*90,Vector3.forward))<.001f,"Exact snapped rotation without accumulated drift "+i+"/"+turn);
                }
                Require(changes==16&&finished==16,"One changed and finished event per turn "+i);
                pieces[i].OrientationChanged-=change;pieces[i].RotationFinished-=end;
            }
            Require(solves==0&&puzzle.State==PuzzleState.Active&&tracker.Count==0,"Incorrect configuration never solves or rewards");
            pieces[1].Interact();Require(pieces[1].IsRotating,"Turn in flight before reset");
            Require(puzzle.ResetPuzzle()&&pieces.Select(p=>p.CurrentOrientation).SequenceEqual(initial)&&pieces.All(p=>!p.IsRotating),"Reset cancels active coroutine and restores all original indices");
            pieces[1].Interact();pieces[1].enabled=false;Require(!pieces[1].IsRotating&&pieces[1].CurrentOrientation==initial[1],"Disable cancels without committing partial orientation");pieces[1].enabled=true;
            Require(puzzle.ResetPuzzle(),"Reset after disable remains safe");
        }
        static void ValidatePrompts()
        {
            var reader=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
            var camera=interaction.GetComponentInChildren<Camera>();var presenter=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            var label=(TMP_Text)new SerializedObject(presenter).FindProperty("_label").objectReferenceValue;
            var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);
            var settings=InputSystem.settings;var oldBackground=settings.backgroundBehavior;var oldEditor=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();var xbox=InputSystem.AddDevice<XInputController>();var sony=InputSystem.AddDevice<DualShockGamepad>();var oldDevices=actions.devices;actions.devices=new InputDevice[]{keyboard,xbox,sony};
            try
            {
                foreach(var fragment in pieces)
                {
                    camera.transform.position=fragment.transform.position+Vector3.back*1.8f;camera.transform.rotation=Quaternion.identity;Refresh(interaction,presenter);
                    Require(ReferenceEquals(interaction.CurrentInteractable,fragment),"All four fragment colliders reachable from front: "+fragment.name);
                }
                camera.transform.position=pieces[0].transform.position+Vector3.back*1.8f;camera.transform.rotation=Quaternion.identity;Refresh(interaction,presenter);
                Require(ReferenceEquals(interaction.CurrentInteractable,pieces[0]),"Real raycast focuses actual fragment collider");
                Key(keyboard,UnityEngine.InputSystem.Key.H);Refresh(interaction,presenter);Require(label.text=="[ E ]  Girar","Keyboard dynamic Girar prompt");
                int before=pieces[0].CurrentOrientation;
                Key(keyboard,UnityEngine.InputSystem.Key.G);Refresh(interaction,presenter);Require(pieces[0].CurrentOrientation==before&&!pieces[0].IsRotating,"Keyboard secret command has no consumer");
                Key(keyboard,UnityEngine.InputSystem.Key.E);Refresh(interaction,presenter);Require(pieces[0].IsRotating,"Native E rotates through PlayerInteraction");Finish(pieces[0]);puzzle.ResetPuzzle();
                foreach(var pad in new Gamepad[]{xbox,sony})
                {
                    Pad(pad,GamepadButton.East);Refresh(interaction,presenter);Require(label.text==(pad==xbox?"[ X ]  Girar":"[ □ ]  Girar"),"Native controller dynamic Girar prompt "+pad.layout);
                    Pad(pad,GamepadButton.North);Refresh(interaction,presenter);Require(pieces[0].CurrentOrientation==initial[0]&&!pieces[0].IsRotating,"Gamepad secret command has no effect "+pad.layout);
                    Pad(pad,GamepadButton.West);Refresh(interaction,presenter);Require(pieces[0].IsRotating,"Native west button rotates "+pad.layout);Finish(pieces[0]);puzzle.ResetPuzzle();
                }
                reader.DisableGameplay();Refresh(interaction,presenter);pieces[0].Interact(); // Direct semantic calls aren't physical input; reset below.
                Require(interaction.CurrentInteractable==null,"Gameplay input gate hides focus");puzzle.ResetPuzzle();reader.EnableGameplay();
            }
            finally
            {
                actions.devices=oldDevices;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(xbox);InputSystem.RemoveDevice(sony);
                settings.backgroundBehavior=oldBackground;settings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
        static void Key(Keyboard k,UnityEngine.InputSystem.Key key)
        {InputSystem.QueueStateEvent(k,new KeyboardState(key));InputSystem.Update();InputSystem.QueueStateEvent(k,new KeyboardState());InputSystem.Update();}
        static void Pad(Gamepad p,GamepadButton button)
        {InputSystem.QueueStateEvent(p,new GamepadState().WithButton(button));InputSystem.Update();InputSystem.QueueStateEvent(p,new GamepadState());InputSystem.Update();}
        static void Refresh(PlayerInteraction i,InteractionPromptPresenter p)
        {Physics.SyncTransforms();Method(typeof(PlayerInteraction),"LateUpdate").Invoke(i,null);Method(typeof(InteractionPromptPresenter),"LateUpdate").Invoke(p,null);}
        static void Solve()
        {
            Require(puzzle.State==PuzzleState.Available&&tracker.Count==0,"Clean reset before solved configuration");
            for(int i=0;i<4;i++)
            {
                int count=(solution[i]-initial[i]+4)%4;
                for(int turn=0;turn<count;turn++)
                {
                    pieces[i].Interact();Require(!puzzle.IsSolved,"No completion while final piece is still moving");Finish(pieces[i]);
                }
            }
            Require(puzzle.State==PuzzleState.Solved&&solves==1,"Complete configuration solves exactly once");
            Require(puzzle.RotationCount==12,"Optimal deterministic solve accepts twelve rotations");
            Require(tracker.Count==1&&tracker.HasClue("level01.clue01"),"Clue01 awarded once");
            foreach(var f in pieces){int n=f.CurrentOrientation;for(int j=0;j<10;j++)f.Interact();Require(!f.CanInteract&&!f.IsRotating&&f.CurrentOrientation==n,"Solved fragment locked "+f.name);}
            Require(!puzzle.ResetPuzzle()&&!puzzle.BeginPuzzle()&&solves==1,"Terminal solution cannot reset or begin twice");
            var reward=puzzle.GetComponent<PuzzleClueReward>();reward.enabled=false;reward.enabled=true;
            var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.mural_fragments.asset");
            Require(!tracker.Discover(clue)&&tracker.Count==1,"Reward catch-up and repeated discovery do not duplicate");
            var ds=new SerializedObject(discovery);var panel=(GameObject)ds.FindProperty("_panel").objectReferenceValue;
            Canvas.ForceUpdateCanvases();
            Require(discovery.GetComponent<RectTransform>()!=null,"Discovery has responsive Canvas-aligned layout root");
            var title=(TMP_Text)ds.FindProperty("_title").objectReferenceValue;
            Require(title.font.HasCharacter('á')&&title.font.HasCharacter('í'),"Existing font supports Spanish clue characters");
            Require(panel.activeSelf&&((TMP_Text)ds.FindProperty("_title").objectReferenceValue).text=="El camino"&&((TMP_Text)ds.FindProperty("_description").objectReferenceValue).text==clue.Description,"Discovery displays correct small provisional narrative");
            Require(puzzle.GetComponentInChildren<Light>(true).enabled,"Warm solved light is enabled");
            Debug.Log("MURAL_AUTOMATED_METRICS optimumRotations=12 animationBudgetSeconds=3.6 humanDifficulty=not_measured");
        }
    }
}
