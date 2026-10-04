using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Level01;
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
    public static class EchoValidation
    {
        const string Active="Tikal.Echo.Active",Result="Tikal.Echo.Result";
        static int stage,checks,solves,discoveries,errors,finished,stoneEvents;
        static double deadline;
        static EchoSequencePuzzle puzzle;
        static EchoSource source;
        static EchoStone[] stones;
        static ClueTracker tracker;
        static PlayerInputReader reader;
        static PlayerInteraction interaction;
        static InteractionPromptPresenter prompt;
        static TMP_Text promptText;
        static Camera view;
        static Keyboard keyboard;
        static XInputController xbox;
        static DualShockGamepad sony;
        static TikalInputActions actions;
        static UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? oldDevices;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static List<EchoNote> heard;
        static GameObject discoveryPanel;
        static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance);
        static void Require(bool value,string message){if(!value)throw new Exception("Echo: "+message);checks++;Debug.Log("ECHO_CHECK: "+message);}
        static EchoValidation(){EditorApplication.playModeStateChanged+=State;}
        [MenuItem("Tikal/Tests/Echo chamber")]
        public static void Run()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(all.All(c=>c!=null),"No Missing Scripts");DynamicPromptValidation.ValidateScene(all);
                puzzle=all.OfType<EchoSequencePuzzle>().Single();source=all.OfType<EchoSource>().Single();stones=all.OfType<EchoStone>().OrderBy(s=>s.Note).ToArray();
                Require(stones.Length==3&&stones.Select(s=>s.Note).Distinct().Count()==3,"Exactly three stones with unique identities A/B/C");
                Require(puzzle.SequenceLength==3&&Enumerable.Range(0,3).Select(puzzle.TargetNoteAt).SequenceEqual(new[]{EchoNote.B,EchoNote.A,EchoNote.C}),"Authored deterministic three-note B A C sequence");
                Require(puzzle.transform.parent.name=="PuzzleAnchor"&&source.transform.parent.name=="AudioAnchor","Existing Zone03 puzzle and audio anchors reused");
                Require(source.transform.parent.parent.name=="Zone03_EchoChamber","Located in echo chamber");
                var ss=new SerializedObject(source);var clips=ss.FindProperty("_tones");Require(clips.arraySize==3,"Exactly three replaceable clips");
                for(int i=0;i<3;i++)
                {
                    var clip=(AudioClip)clips.GetArrayElementAtIndex(i).objectReferenceValue;Require(clip!=null&&Mathf.Abs(clip.length-.45f)<.002f&&clip.channels==1,"Own 0.45 second mono tone "+i);
                    float[] samples=new float[clip.samples];Require(clip.GetData(samples,0),"PCM sample data accessible "+i);int rising=0;for(int n=1;n<samples.Length;n++)if(samples[n-1]<=0&&samples[n]>0)rising++;
                    Require(Math.Abs(rising/.45-(i+1)*220)<5,"Distinct tone frequency "+i);
                    var stone=stones[i];var ts=new SerializedObject(stone);Require(ts.FindProperty("_tone").objectReferenceValue==clip&&stone.Puzzle==puzzle,"Stone references intended puzzle and tone "+i);
                    var audio=(AudioSource)ts.FindProperty("_audio").objectReferenceValue;Require(!audio.playOnAwake&&!audio.loop&&audio.spatialBlend==.35f&&audio.dopplerLevel==0,"Light 3D audio without automatic playback "+i);
                    Require(stone.GetComponentInChildren<Collider>()!=null,"Stone interaction collider "+i);
                }
                Require(stones[0].transform.Find("Low square slab")!=null&&stones[1].transform.Find("Round resonator")!=null&&stones[2].transform.Find("Fork prong 0")!=null,"Shapes distinguish stones without colour");
                Require(all.OfType<Canvas>().Count()==1&&all.OfType<ClueDiscoveryPresenter>().Count()==1,"One shared discovery presenter and Canvas");
                var reward=all.OfType<PuzzleClueReward>().Single(r=>new SerializedObject(r).FindProperty("_puzzle").objectReferenceValue==puzzle);
                Require(reward.transform.parent.name=="ClueAnchor","Clue anchor reused");var clue=(ClueDefinition)new SerializedObject(reward).FindProperty("_clue").objectReferenceValue;
                Require(clue.Id=="level01.clue03"&&clue.Title=="El primer contacto"&&clue.Description=="Lo primero que tocaste aún guarda algo.","Clue03 exact narrative contract");
                Require(all.OfType<ReadableInscription>().Count()>=2&&all.OfType<ReadableInscription>().Any(r=>r.Text=="Escucha. Después, devuelve el eco."),"Both optional inscriptions preserved");
                SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try{Setup();stage=0;BeginReplay(0);Wait(2.2);EditorApplication.update+=Tick;}
                catch(Exception e){Fail(e);}
            }
            else if(change==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool(Active,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Result,1));}
        }
        static void Wait(double seconds){deadline=EditorApplication.timeSinceStartup+seconds;}
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<deadline)return;
            // Cold Editor startup can delay coroutine frames; wait for the real playback completion with a bounded timeout.
            if(stage<=2&&source.IsPlaying&&EditorApplication.timeSinceStartup<deadline+15)return;
            try
            {
                if(stage<=2)
                {
                    Require(!source.IsPlaying&&source.CanInteract&&finished==stage+1,"Replay completes and can be repeated "+stage);
                    Require(heard.SequenceEqual(new[]{EchoNote.B,EchoNote.A,EchoNote.C}),"Exactly three native coroutine notes in deterministic order "+stage);
                    CheckMarks(0);
                    if(stage<2){stage++;BeginReplay(stage);Wait(2.2);}
                    else{stage=3;Native(stones[1],stones[1],0);Require(puzzle.Progress==1&&!puzzle.IsSolved&&stoneEvents==1,"First correct tone advances but cannot solve");stones[1].Interact();Require(stoneEvents==1&&puzzle.Progress==1,"Repeated stone input rejected during its tone");var block=new MaterialPropertyBlock();((Renderer)new SerializedObject(stones[1]).FindProperty("_visual").objectReferenceValue).GetPropertyBlock(block);Require(block.GetColor("_EmissionColor").r>.5f,"Accepted stone visibly pulses");CheckMarks(1);Wait(.6);}
                }
                else if(stage==3)
                {
                    Native(stones[2],stones[2],1);Require(puzzle.Progress==0&&!puzzle.IsSolved&&errors==1&&tracker.Count==2,"Wrong note clears progress, preserves clues and allows retry");stage=4;Wait(.6);
                }
                else if(stage==4)
                {
                    CheckMarks(0);Require(stones.All(s=>s.CanInteract)&&source.CanInteract,"Error leaves all input available after short tone");
                    Native(stones[1],stones[1],0);CheckMarks(1);Native(stones[0],stones[0],1);Require(puzzle.Progress==2&&!puzzle.IsSolved,"Two correct notes are insufficient");CheckMarks(2);Native(stones[2],stones[2],2);
                    Require(puzzle.IsSolved&&puzzle.Progress==3&&solves==1,"Three correct notes solve exactly once");CheckMarks(3);
                    Require(tracker.Count==3&&discoveries==1&&tracker.HasClue("level01.clue01")&&tracker.HasClue("level01.clue02")&&tracker.HasClue("level01.clue03"),"Clue03 granted once with first two clues intact");
                    var discovery=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();var ds=new SerializedObject(discovery);discoveryPanel=(GameObject)ds.FindProperty("_panel").objectReferenceValue;
                    Require(discoveryPanel.activeSelf&&((TMP_Text)ds.FindProperty("_title").objectReferenceValue).text=="El primer contacto"&&((TMP_Text)ds.FindProperty("_description").objectReferenceValue).text=="Lo primero que tocaste aún guarda algo.","Existing discovery UI presents Clue03");
                    Require(ds.FindProperty("_visibleSeconds").floatValue==6,"Discovery lasts six seconds");
                    foreach(var stone in stones){stone.Interact();Require(!stone.CanInteract,"Solved stone unavailable");}source.Interact();Require(!source.CanInteract&&!source.IsPlaying,"Solved source unavailable");
                    Require(!puzzle.ResetPuzzle()&&!puzzle.BeginPuzzle()&&solves==1,"Terminal state cannot reset or solve again");
                    var reward=UnityEngine.Object.FindObjectsByType<PuzzleClueReward>().Single(r=>new SerializedObject(r).FindProperty("_puzzle").objectReferenceValue==puzzle);reward.enabled=false;reward.enabled=true;
                    Require(tracker.Count==3&&discoveries==1,"Reward catch-up does not duplicate Clue03");
                    stage=5;Wait(6.4);
                }
                else
                {
                    Require(!discoveryPanel.activeSelf,"Discovery automatically disappears");CheckMarks(3);Require(solves==1&&discoveries==1,"Completion remains stable across real frames");
                    Debug.Log("TIKAL_ECHO_VALIDATION_PASSED checks="+checks);SessionState.SetInt(Result,0);DisposeInput();EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e){Fail(e);}
        }
        static void BeginReplay(int device)
        {
            heard.Clear();Native(source,source,device);Require(source.IsPlaying&&!source.CanInteract&&stones.All(s=>!s.CanInteract),"Replay excludes overlapping source and stone sounds");
            source.Interact();foreach(var stone in stones)stone.Interact();Require(heard.Count==1&&puzzle.Progress==0&&tracker.Count==2,"Repeated interaction cannot overlap or award clue");CheckMarks(0);
        }
        static void Setup()
        {
            puzzle=UnityEngine.Object.FindAnyObjectByType<EchoSequencePuzzle>();source=UnityEngine.Object.FindAnyObjectByType<EchoSource>();stones=UnityEngine.Object.FindObjectsByType<EchoStone>().OrderBy(s=>s.Note).ToArray();tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();player.enabled=false;reader=player.GetComponent<PlayerInputReader>();reader.EnableGameplay();interaction=player.GetComponent<PlayerInteraction>();view=player.GetComponentInChildren<Camera>();prompt=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();promptText=(TMP_Text)new SerializedObject(prompt).FindProperty("_label").objectReferenceValue;
            oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();xbox=InputSystem.AddDevice<XInputController>();sony=InputSystem.AddDevice<DualShockGamepad>();actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(reader);oldDevices=actions.devices;actions.devices=new InputDevice[]{keyboard,xbox,sony};
            Require(puzzle.State==PuzzleState.Available&&source.CanInteract&&stones.All(s=>s.CanInteract),"Available puzzle with no hidden listening prerequisite");
            source.Interact();Require(source.IsPlaying&&puzzle.State==PuzzleState.Active,"Reference starts an active attempt");Require(puzzle.ResetPuzzle()&&!source.IsPlaying&&puzzle.State==PuzzleState.Available&&puzzle.Progress==0&&tracker.Count==0,"Reset cancels reference audio and clears only the attempt");
            var readable=UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(r=>r.Text=="Escucha. Después, devuelve el eco.");var rs=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<InscriptionPresenter>());
            for(int d=0;d<3;d++){Native(readable,readable,d);Require(((GameObject)rs.FindProperty("_panel").objectReferenceValue).activeSelf&&((TMP_Text)rs.FindProperty("_text").objectReferenceValue).text==readable.Text,"Echo inscription presents via native control "+d);}
            Require(tracker.Count==0&&puzzle.State==PuzzleState.Available&&reader.GameplayEnabled,"Reading has no clue, puzzle or movement gate effect");
            SolveEarlierPuzzles();Require(tracker.Count==2&&tracker.HasClue("level01.clue01")&&tracker.HasClue("level01.clue02"),"Puzzle01 and Puzzle02 still solve in the same scene");
            heard=new List<EchoNote>();source.ReferenceNotePlayed+=n=>heard.Add(n);finished=0;source.PlaybackFinished+=()=>finished++;solves=discoveries=errors=stoneEvents=0;puzzle.PuzzleSolved+=_=>solves++;puzzle.WrongNote+=_=>errors++;tracker.ClueDiscovered+=_=>discoveries++;foreach(var stone in stones)stone.NotePlayed+=_=>stoneEvents++;
        }
        static void SolveEarlierPuzzles()
        {
            var mural=UnityEngine.Object.FindAnyObjectByType<MuralFragmentsPuzzle>();var ms=new SerializedObject(mural);var mf=ms.FindProperty("_fragments");var ma=ms.FindProperty("_solution");
            for(int i=0;i<4;i++){var f=(MuralFragment)mf.GetArrayElementAtIndex(i).objectReferenceValue;while(f.CurrentOrientation!=ma.GetArrayElementAtIndex(i).intValue){f.Interact();for(int n=0;n<20;n++)Method(typeof(MuralFragment),"AdvanceRotation").Invoke(f,new object[]{.02f});}}
            var guardians=UnityEngine.Object.FindAnyObjectByType<PathGuardiansPuzzle>();var gs=new SerializedObject(guardians);var gf=gs.FindProperty("_guardians");var ga=gs.FindProperty("_solution");
            for(int i=0;i<3;i++){var f=(PathGuardian)gf.GetArrayElementAtIndex(i).objectReferenceValue;while(f.CurrentOrientation!=ga.GetArrayElementAtIndex(i).intValue){f.Interact();for(int n=0;n<20;n++)Method(typeof(PathGuardian),"AdvanceRotation").Invoke(f,new object[]{.02f});}}
            Require(mural.IsSolved&&guardians.IsSolved,"Earlier concrete puzzle conditions remain functional");
        }
        static void Refresh(){Physics.SyncTransforms();Method(typeof(PlayerInteraction),"LateUpdate").Invoke(interaction,null);Method(typeof(InteractionPromptPresenter),"LateUpdate").Invoke(prompt,null);}
        static void PressKey(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}
        static void Pad(Gamepad pad,GamepadButton b){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(b));InputSystem.Update();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();}
        static void Native(IInteractable target,Component component,int device)
        {
            var body=component.GetComponentsInChildren<Collider>().OrderByDescending(c=>c.bounds.center.y).First();var center=body.bounds.center;view.transform.position=center+(component is ReadableInscription?Vector3.left:Vector3.back)*(component is ReadableInscription?1.8f:1.4f);view.transform.LookAt(center);
            if(device==0)PressKey(Key.H);else Pad(device==1?xbox:sony,GamepadButton.East);Refresh();
            Require(ReferenceEquals(interaction.CurrentInteractable,target)&&promptText.text==(device==0?"[ E ]  ":device==1?"[ X ]  ":"[ □ ]  ")+target.InteractionText,"Native prompt "+target.InteractionText+" device="+device+" focus="+(interaction.CurrentInteractable as Component)?.name+" available="+target.CanInteract+" label="+promptText.text+" hits="+string.Join(",",Physics.RaycastAll(view.transform.position,view.transform.forward,3).Select(h=>h.collider.name)));
            int progress=puzzle.Progress,clues=tracker.Count;bool playing=source.IsPlaying;var state=puzzle.State;
            if(device==0)PressKey(Key.G);else Pad(device==1?xbox:sony,GamepadButton.North);Refresh();
            Require(puzzle.Progress==progress&&tracker.Count==clues&&puzzle.State==state&&source.IsPlaying==playing,"Secret command has no effect device="+device);
            if(device==0)PressKey(Key.E);else Pad(device==1?xbox:sony,GamepadButton.West);Refresh();
        }
        static void CheckMarks(int count)
        {
            var f=UnityEngine.Object.FindAnyObjectByType<EchoProgressFeedback>();var marks=new SerializedObject(f).FindProperty("_marks");var block=new MaterialPropertyBlock();
            for(int i=0;i<3;i++){((Renderer)marks.GetArrayElementAtIndex(i).objectReferenceValue).GetPropertyBlock(block);var c=block.GetColor("_EmissionColor");Require(i<count?c.r>.5f:c.maxColorComponent<.001f,"Environmental progress without replay visual solution "+i+" count="+count);}
        }
        static void DisposeInput(){if(actions!=null)actions.devices=oldDevices;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(xbox!=null)InputSystem.RemoveDevice(xbox);if(sony!=null)InputSystem.RemoveDevice(sony);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;keyboard=null;xbox=null;sony=null;actions=null;}
        static void Fail(Exception e){Debug.LogException(e);DisposeInput();EditorApplication.update-=Tick;SessionState.SetInt(Result,1);EditorApplication.ExitPlaymode();}
    }
}
