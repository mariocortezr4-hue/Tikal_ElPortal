using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Debugging;
using Tikal.Tests.Support;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class PuzzleFrameworkValidation
    {
        const string Active="Tikal.PuzzleValidation.Active",Result="Tikal.PuzzleValidation.Result";
        static int checks;
        static PuzzleFrameworkValidation() => EditorApplication.playModeStateChanged += State;
        static void Require(bool condition,string message)
        {
            if(!condition) throw new Exception("Puzzle framework: "+message);
            checks++; Debug.Log("PUZZLE_CHECK: "+message);
        }
        static void Set(UnityEngine.Object target,string name,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(name);
            if(value is string s)p.stringValue=s;
            else if(value is bool b)p.boolValue=b;
            else if(value is int n)p.intValue=n;
            else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Tikal/Tests/Reusable puzzle framework")]
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Testing/PuzzleFrameworkTest.unity");
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c!=null),"Test scene has no Missing Scripts");
                Require(components.OfType<TestPuzzle>().Count()==1&&components.OfType<ClueTracker>().Count()==1,"Fixture and tracker present");
                Require(components.OfType<PlayerController>().Count()==1&&components.OfType<PuzzleInteractable>().Count()==2,"Explorer and two composed elements");
                foreach(var adapter in components.OfType<PuzzleInteractable>())Require(new SerializedObject(adapter).FindProperty("_puzzle").objectReferenceValue!=null,"Adapter target assigned");
                foreach(var field in new[]{"_puzzle","_tracker","_clue"})Require(new SerializedObject(components.OfType<PuzzleClueReward>().Single()).FindProperty(field).objectReferenceValue!=null,"Reward reference "+field);
                foreach(var field in new[]{"_inputReader","_movement","_look"})Require(new SerializedObject(components.OfType<PlayerController>().Single()).FindProperty(field).objectReferenceValue!=null,"Explorer reference "+field);
                foreach(var field in new[]{"_puzzle","_tracker","_player","_inputReader"})Require(new SerializedObject(components.OfType<PuzzleFrameworkDebugControls>().Single()).FindProperty(field).objectReferenceValue!=null,"Development controls reference "+field);
                DynamicPromptValidation.ValidateScene(components);
                string[] ids={"level01.mural_fragments","level01.path_guardians","level01.echo_chamber","level01.glyph_gallery","level01.revelation"};
                foreach(string id in ids)
                {
                    Require(AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/"+id+".asset").Id==id,"Stable Level01 definition "+id);
                    var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/"+id+".asset");
                    Require(id=="level01.mural_fragments" ? clue.Id=="level01.clue01"&&clue.Title=="El camino"&&clue.Description=="El camino verdadero no siempre está delante de ti." : id=="level01.path_guardians" ? clue.Id=="level01.clue02"&&clue.Title=="El origen"&&clue.Description=="Todo camino recuerda dónde comenzó." : id=="level01.echo_chamber" ? clue.Id=="level01.clue03"&&clue.Title=="El primer contacto"&&clue.Description=="Lo primero que tocaste aún guarda algo." : id=="level01.glyph_gallery" ? clue.Id=="level01.clue04"&&clue.Title=="El séptimo lugar"&&clue.Description=="La respuesta ocupa el séptimo lugar entre las letras." : clue.Id==id+".clue"&&clue.Title.Contains("PLACEHOLDER")&&clue.Description.Contains("PLACEHOLDER"),"Provisional clue contract "+id);
                }
                Require(typeof(PuzzleBase).IsAbstract&&typeof(PuzzleBase).BaseType==typeof(MonoBehaviour),"Abstract MonoBehaviour independent of interaction inheritance");
                Require(typeof(PuzzleBase).GetProperty("State").SetMethod==null&&typeof(PuzzleBase).GetMethod("TrySolve",BindingFlags.Instance|BindingFlags.NonPublic).IsFamily,"State read-only and solve protected");
                foreach(var file in Directory.GetFiles("Assets/_Project/Code/Puzzles","*.cs",SearchOption.AllDirectories))
                    Require(!Regex.IsMatch(File.ReadAllText(file),@"UnityEngine\.InputSystem|Input\.Get|KeyCode|SceneManager|SecretCommand|GameManager|SaveManager|\bDoor\b|void Update\("),"No physical bindings, global systems or polling: "+Path.GetFileName(file));
                SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try{Validate();SessionState.SetInt(Result,0);Debug.Log("TIKAL_PUZZLE_FRAMEWORK_PASSED checks="+checks);}
                catch(Exception e){Debug.LogException(e);}
                EditorApplication.ExitPlaymode();
            }
            else if(change==PlayModeStateChange.EnteredEditMode)
            {
                int result=SessionState.GetInt(Result,1);SessionState.SetBool(Active,false);
                if(Application.isBatchMode)EditorApplication.Exit(result);
            }
        }
        static void Validate()
        {
            var fixture=UnityEngine.Object.FindAnyObjectByType<TestPuzzle>();
            var tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();
            var adapters=fixture.GetComponentsInChildren<PuzzleInteractable>().OrderBy(a=>a.name).ToArray();
            int started=0,solved=0,reset=0,discovered=0;
            fixture.PuzzleStarted+=p=>{started++;Require(p.State==PuzzleState.Active&&!p.ResetPuzzle(),"Start event observes Active; nested reset blocked");};
            fixture.PuzzleSolved+=p=>{solved++;Require(p.IsSolved,"Solve event observes final state");};
            fixture.PuzzleReset+=p=>reset++;
            tracker.ClueDiscovered+=c=>discovered++;
            string id=fixture.PuzzleId;
            Require(fixture.State==PuzzleState.Inactive&&!fixture.IsSolved,"Initial Inactive state");
            Require(!fixture.BeginPuzzle()&&!fixture.CompleteForTesting()&&!fixture.ResetPuzzle()&&!adapters[0].CanInteract,"Inactive rejects begin, solve, reset and interaction");
            Require(fixture.MakeAvailable()&&!fixture.MakeAvailable(),"Availability transition idempotent");
            var reader=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();var interaction=reader.GetComponent<PlayerInteraction>();reader.GetComponent<PlayerController>().enabled=false;reader.EnableGameplay();
            var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);
            var oldDevices=actions.devices;var settings=InputSystem.settings;var oldBackground=settings.backgroundBehavior;var oldEditor=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();actions.devices=new InputDevice[]{keyboard};
            try
            {
                var tick=typeof(PlayerInteraction).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
                Physics.SyncTransforms();tick.Invoke(interaction,null);Require(ReferenceEquals(interaction.CurrentInteractable,adapters[0]),"Existing raycast resolves puzzle adapter");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));InputSystem.Update();tick.Invoke(interaction,null);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();tick.Invoke(interaction,null);
                Require(fixture.State==PuzzleState.Active&&started==1&&!fixture.BeginPuzzle(),"Semantic Interact begins once");
                adapters[0].Interact();Require(fixture.InteractionCount==1&&fixture.State==PuzzleState.Active,"First element progresses without immediate resolution");
                adapters[1].Interact();Require(fixture.IsSolved&&solved==1&&fixture.LastElementId=="test.element_2","Second element solves through polymorphic hook");
            }
            finally{actions.devices=oldDevices;InputSystem.RemoveDevice(keyboard);settings.backgroundBehavior=oldBackground;settings.editorInputBehaviorInPlayMode=oldEditor;}
            Require(!fixture.CompleteForTesting()&&!fixture.BeginPuzzle()&&!fixture.ResetPuzzle()&&!fixture.TryInteract("ignored")&&solved==1&&reset==0,"Solved terminal, duplicate solve suppressed");
            Require(fixture.PuzzleId==id,"Identifier stable across all transitions");
            var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.mural_fragments.asset");
            var second=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.path_guardians.asset");
            Require(tracker.Count==1&&discovered==1&&tracker.HasClue(clue.Id),"Reward grants first clue once");
            Require(!tracker.Discover(clue)&&discovered==1,"Duplicate definition emits no discovery");
            var duplicate=ScriptableObject.CreateInstance<ClueDefinition>();var definition=ScriptableObject.CreateInstance<PuzzleDefinition>();
            var host=new GameObject("Temporary lifecycle probe");host.SetActive(false);var probe=host.AddComponent<PuzzleLifecycleProbe>();
            var otherTracker=host.AddComponent<ClueTracker>();var reward=host.AddComponent<PuzzleClueReward>();reward.enabled=false;
            try
            {
                Set(duplicate,"_id",clue.Id);Require(!tracker.Discover(duplicate),"Different asset with same ID deduplicated");
                Require(!tracker.Discover(null)&&!tracker.HasClue(null)&&!tracker.HasClue("unknown"),"Invalid discovery and lookup safe");
                Require(tracker.Discover(second)&&tracker.Count==2&&discovered==2&&tracker.HasClue(second.Id),"Two distinct clues tracked");
                Set(definition,"_id","testing.independent");Set(probe,"_definition",definition);Set(probe,"_initialState",1);
                Set(reward,"_puzzle",probe);Set(reward,"_tracker",otherTracker);Set(reward,"_clue",second);
                int starts=0,resets=0,completions=0,rewards=0;
                probe.PuzzleStarted+=p=>starts++;probe.PuzzleReset+=p=>resets++;probe.PuzzleSolved+=p=>completions++;otherTracker.ClueDiscovered+=c=>rewards++;
                host.SetActive(true);
                Require(probe.State==PuzzleState.Available&&otherTracker.Count==0,"Authored Available initial state; isolated session tracker");
                Require(probe.BeginPuzzle()&&probe.BeginHooks==1&&starts==1,"Second derived type begins through base API");
                Set(probe,"_allowReset",false);Require(!probe.ResetPuzzle()&&!probe.CanReset&&probe.State==PuzzleState.Active,"Reset disabled policy enforced");
                Set(probe,"_allowReset",true);Require(probe.ResetPuzzle()&&probe.State==PuzzleState.Available&&probe.ResetHooks==1&&resets==1,"Allowed reset returns Available and calls hook once");
                Require(!probe.ResetPuzzle()&&resets==1,"Redundant reset emits no event");
                Require(probe.BeginPuzzle()&&starts==2&&probe.TryInteract("probe")&&probe.IsSolved&&probe.SolveHooks==1&&completions==1,"Polymorphic completion and exact per-transition events");
                Require(fixture.IsSolved&&fixture.PuzzleId==id&&probe.PuzzleId=="testing.independent","Instances independent and stable IDs distinct");
                Set(definition,"_id","testing.changed_in_editor");Require(probe.PuzzleId=="testing.independent","Runtime identity frozen even if authoring definition changes");
                Require(otherTracker.Count==0,"Disabled reward does not observe completion");
                reward.enabled=true;Require(otherTracker.Count==1&&rewards==1,"Reward catches up on re-enable");
                reward.enabled=false;reward.enabled=true;Require(otherTracker.Count==1&&rewards==1&&!probe.Complete(),"Repeated lifecycle or completion cannot duplicate reward");
            }
            finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(duplicate);UnityEngine.Object.DestroyImmediate(definition);}
        }
    }
}
