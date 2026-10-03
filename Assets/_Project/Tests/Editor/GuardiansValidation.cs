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
    public static class GuardiansValidation
    {
        const string Active="Tikal.Guardians.Active",Result="Tikal.Guardians.Result";
        static int checks,stage;
        static double deadline,wallDeadline;
        static PathGuardiansPuzzle puzzle;
        static PathGuardian[] pieces;
        static int[] initial,solution;
        static int solves;
        static ClueTracker tracker;
        static ClueDiscoveryPresenter discovery;
        static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
        static void Require(bool value,string message)
        {if(!value)throw new Exception("Guardians: "+message);checks++;Debug.Log("GUARDIANS_CHECK: "+message);}
        static GuardiansValidation(){EditorApplication.playModeStateChanged+=State;}
        [MenuItem("Tikal/Tests/Path guardians and second clue")]
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c!=null),"No Missing Scripts");DynamicPromptValidation.ValidateScene(components);
                Require(components.OfType<PathGuardiansPuzzle>().Count()==1,"Exactly one guardians puzzle installed");
                Require(components.OfType<PathGuardian>().Count()==3&&components.OfType<DestinationMarker>().Count()==3,"Scene contains exactly three guardians and three destinations");
                puzzle=components.OfType<PathGuardiansPuzzle>().Single();
                var so=new SerializedObject(puzzle);var refs=so.FindProperty("_guardians");var answers=so.FindProperty("_solution");
                Require(refs.arraySize==3&&answers.arraySize==3,"Exactly three authored guardians and solution entries");
                pieces=Enumerable.Range(0,3).Select(i=>(PathGuardian)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                solution=Enumerable.Range(0,3).Select(i=>answers.GetArrayElementAtIndex(i).intValue).ToArray();
                Require(pieces.All(p=>p!=null)&&pieces.Distinct().Count()==3,"Three unique valid fragments");
                initial=pieces.Select(p=>new SerializedObject(p).FindProperty("_initialOrientation").intValue).ToArray();
                var destinations=so.FindProperty("_destinations");Require(destinations.arraySize==3,"Exactly three destinations");
                var markers=Enumerable.Range(0,3).Select(i=>(DestinationMarker)destinations.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                Require(markers.All(m=>m!=null)&&markers.Distinct().Count()==3&&pieces.Select(g=>g.Symbol).Distinct().Count()==3,"Unique destination and guardian symbols");
                Physics.SyncTransforms();
                for(int i=0;i<3;i++)
                {
                    Require(markers[i].Symbol==pieces[i].Symbol,"One-to-one matching abstract symbol "+i);
                    var direction=markers[i].transform.position-pieces[i].transform.position;direction.y=0;
                    Require(Vector3.Angle(direction,Quaternion.Euler(0,solution[i]*90,0)*Vector3.forward)<.01f,"Discrete solution points toward matching destination "+i);
                    Vector3 eye=pieces[i].transform.position+Vector3.up*1.6f;Vector3 target=markers[i].transform.position+Vector3.up*1.8f;
                    var ray=target-eye;var hits=Physics.RaycastAll(eye+ray.normalized*.7f,ray.normalized,ray.magnitude-.7f);
                    Require(!hits.Any(h=>h.collider.GetComponentInParent<DestinationMarker>()!=markers[i]&&h.collider.GetComponentInParent<PathGuardian>()!=pieces[i]),"Matching destination visible without solid occlusion "+i);
                }
                                Vector3 entry=new Vector3(29,2.82f,12);
                foreach(var target in new[]{pieces[0].transform.position+Vector3.up*1.63f,markers[0].transform.position+Vector3.up*1.8f})
                {
                    Vector3 ray=target-entry;
                    var hits=Physics.RaycastAll(entry,ray.normalized,ray.magnitude-.6f);
                    Require(hits.Length==0,"Example A and matching destination have clear entry sightlines");
                }
                Require(markers.All(m=>m.transform.Find("Matching abstract symbol").localScale.x>=1.25f&&m.transform.Find("Rear matching symbol")!=null),"Enlarged two-sided destination symbols");
                Require(solution.Distinct().Count()==3&&initial.Zip(solution,(a,b)=>(b-a+4)%4).Sum()==3,"Distinct target directions and minimum three turns");
                for(int i=0;i<3;i++)
                {
                    var fs=new SerializedObject(pieces[i]);Require(fs.FindProperty("_puzzle").objectReferenceValue==puzzle&&fs.FindProperty("_visual").objectReferenceValue!=null,"Fragment references valid "+i);
                    Require((solution[i]-initial[i]+4)%4==i,"Initial turns A=0 B=1 C=2: "+i);
                    Require(Mathf.Approximately(fs.FindProperty("_rotationDuration").floatValue,.35f),"Mechanical 0.35 second turn "+i);
                }
                Require(puzzle.transform.parent.name=="PuzzleAnchor"&&puzzle.transform.parent.parent.name=="Zone02_PathGuardians","Installed at existing Zone02 anchor");
                tracker=components.OfType<ClueTracker>().Single();discovery=components.OfType<ClueDiscoveryPresenter>().Single();
                var reward=new SerializedObject(puzzle.GetComponent<PuzzleClueReward>());
                Require(reward.FindProperty("_puzzle").objectReferenceValue==puzzle&&reward.FindProperty("_tracker").objectReferenceValue==tracker,"Reward composed outside PuzzleBase");
                var clue=(ClueDefinition)reward.FindProperty("_clue").objectReferenceValue;
                Require(clue.Id=="level01.clue02"&&clue.Title=="El origen"&&clue.Description=="Todo camino recuerda dónde comenzó.","Clue02 narrative contract");
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
                    puzzle=UnityEngine.Object.FindAnyObjectByType<PathGuardiansPuzzle>();
                    pieces=puzzle.GetComponentsInChildren<PathGuardian>().OrderBy(f=>f.name).ToArray();
                    tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();discovery=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();
                    UnityEngine.Object.FindAnyObjectByType<PlayerController>().enabled=false;
                    UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>().EnableGameplay();
                    solves=0;puzzle.PuzzleSolved+=_=>solves++;
                    Require(puzzle.State==PuzzleState.Available&&pieces.All(f=>f.CanInteract),"Available and all three interactable at entry");
                    stage=0;deadline=Time.timeAsDouble+.1;wallDeadline=EditorApplication.timeSinceStartup+20;EditorApplication.update+=Tick;
                }
                catch(Exception e){Fail(e);}
            }
            else if(change==PlayModeStateChange.EnteredEditMode)
            {EditorApplication.update-=Tick;int result=SessionState.GetInt(Result,1);SessionState.SetBool(Active,false);if(Application.isBatchMode)EditorApplication.Exit(result);}
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(stage<=1&&Time.timeAsDouble<deadline&&EditorApplication.timeSinceStartup<wallDeadline)return;
            if(stage==2&&EditorApplication.timeSinceStartup<deadline)return;
            try
            {
                if(stage==0)
                {
                    Require(puzzle.State==PuzzleState.Available&&!puzzle.IsSolved&&puzzle.AlignedCount==1&&puzzle.IsGuardianAligned(0),"Guardian A teaches the rule without auto solve");
                    ValidateFeedback(1);
                    pieces[0].Interact();Require(pieces[0].IsRotating&&!pieces[0].CanInteract,"Real coroutine suppresses repeated input");
                    Require(pieces[0].CurrentOrientation==initial[0],"Index commits only at completion");ValidateFeedback(0);
                    stage=1;deadline=Time.timeAsDouble+.6;wallDeadline=EditorApplication.timeSinceStartup+20;
                }
                else if(stage==1)
                {
                    Require(!pieces[0].IsRotating&&pieces[0].CurrentOrientation==(initial[0]+1)%4,"Actual Unity frame coroutine completes smoothly (gameTime="+Time.timeAsDouble+", rotating="+pieces[0].IsRotating+")");
                    Require(puzzle.ResetPuzzle(),"Active reset permitted");ValidateFeedback(1);
                    ValidateRotations();ValidatePrompts();Solve();
                    stage=2;deadline=EditorApplication.timeSinceStartup+6.5;
                }
                else
                {
                    var panel=(GameObject)new SerializedObject(discovery).FindProperty("_panel").objectReferenceValue;
                    Require(!panel.activeSelf,"Discovery auto dismisses using realtime after six seconds");
                    SessionState.SetInt(Result,0);Debug.Log("TIKAL_GUARDIANS_VALIDATION_PASSED checks="+checks);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e){Fail(e);}
        }
        static void Fail(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
        static void Finish(PathGuardian f)
        {for(int n=0;n<20;n++)Method(typeof(PathGuardian),"AdvanceRotation").Invoke(f,new object[]{.02f});}
        static Transform Visual(PathGuardian f)=>(Transform)new SerializedObject(f).FindProperty("_visual").objectReferenceValue;
        static void ValidateFeedback(int count)
        {
            var feedback=puzzle.GetComponent<GuardiansAlignmentFeedback>();Require(feedback!=null,"Environmental alignment feedback present");
            var so=new SerializedObject(feedback);int individual=0,central=0;
            foreach(string field in new[]{"_individualMarks","_centralMarks"})
            {
                var marks=so.FindProperty(field);Require(marks.arraySize==3,"Three geometric marks: "+field);
                for(int i=0;i<3;i++)
                {
                    var renderer=(Renderer)marks.GetArrayElementAtIndex(i).objectReferenceValue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                    bool lit=block.GetColor("_EmissionColor").maxColorComponent>0;
                    Require(lit==puzzle.IsGuardianAligned(i),"Emission reflects discrete alignment "+field+"/"+i);
                    if(lit){if(field=="_individualMarks")individual++;else central++;}
                }
            }
            Require(individual==count&&central==count,"Individual and central progress = "+count+"/3");
        }
        static void ValidateRotations()
        {
            for(int i=0;i<3;i++)
            {
                int changes=0,finished=0;Action<PathGuardian> change=_=>changes++,end=_=>finished++;
                pieces[i].OrientationChanged+=change;pieces[i].RotationFinished+=end;
                for(int turn=0;turn<16;turn++)
                {
                    int before=pieces[i].CurrentOrientation;pieces[i].Interact();for(int n=0;n<10;n++)pieces[i].Interact();
                    Require(pieces[i].IsRotating&&pieces[i].CurrentOrientation==before,"Repeated requests ignored during turn "+i+"/"+turn);
                    Finish(pieces[i]);Require(pieces[i].CurrentOrientation==(before+1)%4,"Discrete cyclic orientation "+i+"/"+turn);
                    Require(Quaternion.Angle(Visual(pieces[i]).localRotation,Quaternion.AngleAxis(pieces[i].CurrentOrientation*90,Vector3.up))<.001f,"Exact snapped rotation without accumulated drift "+i+"/"+turn);
                }
                Require(changes==16&&finished==16,"One changed and finished event per turn "+i);
                pieces[i].OrientationChanged-=change;pieces[i].RotationFinished-=end;
            }
            ValidateFeedback(1);
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
                    camera.transform.position=fragment.transform.position+Vector3.up*1.1f+Vector3.back*1.8f;camera.transform.rotation=Quaternion.identity;Refresh(interaction,presenter);
                    Require(ReferenceEquals(interaction.CurrentInteractable,fragment),"All three fragment colliders reachable from front: "+fragment.name);
                }
                camera.transform.position=pieces[0].transform.position+Vector3.up*1.1f+Vector3.back*1.8f;camera.transform.rotation=Quaternion.identity;Refresh(interaction,presenter);
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
                        var mural=UnityEngine.Object.FindAnyObjectByType<MuralFragmentsPuzzle>();
            var ms=new SerializedObject(mural);var mf=ms.FindProperty("_fragments");var ma=ms.FindProperty("_solution");
            for(int i=0;i<4;i++)
            {
                var piece=(MuralFragment)mf.GetArrayElementAtIndex(i).objectReferenceValue;
                while(piece.CurrentOrientation!=ma.GetArrayElementAtIndex(i).intValue)
                {piece.Interact();for(int n=0;n<20;n++)Method(typeof(MuralFragment),"AdvanceRotation").Invoke(piece,new object[]{.02f});}
            }
            Require(mural.IsSolved&&tracker.HasClue("level01.clue01")&&tracker.Count==1,"Puzzle01 still solves and awards Clue01 before Puzzle02");
            for(int i=0;i<3;i++)
            {
                int count=(solution[i]-initial[i]+4)%4;
                for(int turn=0;turn<count;turn++)
                {
                    pieces[i].Interact();Require(!puzzle.IsSolved,"No completion while final piece is still moving");Finish(pieces[i]);ValidateFeedback(puzzle.AlignedCount);
                    if(turn==count-1&&i<2)Require(!puzzle.IsSolved&&puzzle.AlignedCount==i+1,"One or two aligned guardians cannot solve: "+(i+1));
                }
            }
            Require(puzzle.State==PuzzleState.Solved&&solves==1,"Complete configuration solves exactly once");
            Require(puzzle.RotationCount==3&&puzzle.AlignedCount==3,"Optimal deterministic solve accepts three rotations and all three aligned");
            Require(tracker.Count==2&&tracker.HasClue("level01.clue02")&&tracker.HasClue("level01.clue01"),"Clue02 awarded once while Clue01 is preserved");
            foreach(var f in pieces){int n=f.CurrentOrientation;for(int j=0;j<10;j++)f.Interact();Require(!f.CanInteract&&!f.IsRotating&&f.CurrentOrientation==n,"Solved fragment locked "+f.name);}
            Require(!puzzle.ResetPuzzle()&&!puzzle.BeginPuzzle()&&solves==1,"Terminal solution cannot reset or begin twice");
            var reward=puzzle.GetComponent<PuzzleClueReward>();reward.enabled=false;reward.enabled=true;
            var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.path_guardians.asset");
            Require(!tracker.Discover(clue)&&tracker.Count==2,"Reward catch-up and repeated discovery do not duplicate");
            var ds=new SerializedObject(discovery);var panel=(GameObject)ds.FindProperty("_panel").objectReferenceValue;
            Canvas.ForceUpdateCanvases();
            Require(discovery.GetComponent<RectTransform>()!=null,"Discovery has responsive Canvas-aligned layout root");
            var title=(TMP_Text)ds.FindProperty("_title").objectReferenceValue;
            Require(title.font.HasCharacter('á')&&title.font.HasCharacter('í'),"Existing font supports Spanish clue characters");
            Require(panel.activeSelf&&((TMP_Text)ds.FindProperty("_title").objectReferenceValue).text=="El origen"&&((TMP_Text)ds.FindProperty("_description").objectReferenceValue).text==clue.Description,"Discovery displays correct small provisional narrative");
            Require(puzzle.GetComponentInChildren<Light>(true).enabled,"Warm solved light is enabled");
            Debug.Log("GUARDIANS_AUTOMATED_METRICS optimumRotations=3 animationBudgetSeconds=1.05 humanDifficulty=not_measured");
        }
    }
}
