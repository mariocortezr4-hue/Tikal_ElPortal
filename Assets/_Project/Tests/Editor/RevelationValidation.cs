using System;
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
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class RevelationValidation
    {
        const string Active = "Tikal.Revelation.Active", Result = "Tikal.Revelation.Result", Count = "Tikal.Revelation.Count";
        static int checks, solves, discoveries, phase;
        static double deadline, retryTimeout;
        static RevelationPuzzle puzzle;
        static RevelationStele[] stones;
        static RevelationMonument feedback;
        static ClueTracker tracker;
        static PlayerInputReader reader;
        static PlayerInteraction interaction;
        static InteractionPromptPresenter prompt;
        static ClueJournalPresenter journal;
        static ReticlePresenter reticle;
        static Camera camera;
        static TMP_Text promptText;
        static Keyboard keyboard;
        static XInputController xbox;
        static DualShockGamepad sony;
        static TikalInputActions actions;
        static UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? oldDevices;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static RevelationValidation() => EditorApplication.playModeStateChanged += State;
        static UnityEngine.Object Reference(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;
        static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        static void Require(bool value, string message)
        {
            if (!value) throw new Exception("Revelation: " + message);
            checks++;
            Debug.Log("REVELATION_CHECK: " + message);
        }
        static ClueDefinition[] Clues() => new[] { "mural_fragments", "path_guardians", "echo_chamber", "glyph_gallery", "revelation" }
            .Select(name => AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01." + name + ".asset")).ToArray();

        [MenuItem("Tikal/Tests/Revelation chamber")]
        public static void Run()
        {
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                checks = 0;
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var all = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Require(all.All(component => component != null), "No Missing Scripts");
                DynamicPromptValidation.ValidateScene(all);
                var p = all.OfType<RevelationPuzzle>().Single();
                var plates = all.OfType<RevelationStele>().OrderBy(stone => stone.transform.position.x).ToArray();
                Require(plates.Length == 4 && plates.Select(s => s.PatternId).SequenceEqual(new[]{"Cycle","Path","Cycle","Star"}), "Four steles with exactly one stable logical pair");
                Require(p.transform.parent.name == "PuzzleAnchor" && p.transform.parent.parent.name == "Zone05_Revelation", "Existing Zone05 anchor reused");
                foreach(var plate in plates)
                {
                    Require(plate.Puzzle==p && plate.HasVisual && plate.GetComponentInChildren<Collider>()!=null && plate.InteractionText=="Activar","Valid stele references and interaction "+plate.name);
                    Require(plate.transform.Find("Pattern elements").GetComponentsInChildren<Collider>().Length==0,"Neutral pattern does not block raycast");
                }
                Require(plates[0].transform.Find("Pattern elements").childCount==11 && plates[2].transform.Find("Pattern elements").childCount==11 &&
                    plates[0].transform.Find("Pattern elements").localScale!=plates[2].transform.Find("Pattern elements").localScale,"Matching cycles have recognizable equal geometry with subtle scale difference");
                var f=all.OfType<RevelationMonument>().Single();
                Require(Reference(f,"_puzzle")==p && Reference(f,"_panel")!=null && Reference(f,"_interior")!=null &&
                    f.transform.parent.name=="RevelationAnchor","Central monument has preserved anchor and valid references");
                var reward=all.OfType<PuzzleClueReward>().Single(r=>Reference(r,"_puzzle")==p);
                var clue=(ClueDefinition)Reference(reward,"_clue");
                Require(clue.Id=="level01.clue05" && clue.Title=="El ciclo" && clue.Description=="El inicio es el final." && clue.Order==5,"Exact Clue05 contract");
                Require(Reference(reward,"_tracker")!=null,"Existing tracker/reward reused");
                var inscription=all.OfType<ReadableInscription>().Single(r=>r.Text=="Lo que aparece al principio vuelve al final.");
                var list=new SerializedObject(all.OfType<InscriptionPresenter>().Single()).FindProperty("_inscriptions");
                Require(Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==inscription),"Help inscription registered without clue");
                Require(all.OfType<Canvas>().Count()==1 && all.OfType<ReticlePresenter>().Count()==1 && all.OfType<ClueDiscoveryPresenter>().Count()==1,"Existing HUD retained");
                Require(all.OfType<Tikal.Core.LevelOutcomeController>().All(c=>c.State==Tikal.Core.LevelOutcome.Playing),"Revelation does not automatically finish the level");
                SessionState.SetInt(Count, checks);
                SessionState.SetInt(Result, 1);
                SessionState.SetBool(Active, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void State(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Active, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    checks = SessionState.GetInt(Count, 0);
                    Setup();
                    ValidateRuntime();
                    deadline = EditorApplication.timeSinceStartup + .4;
                    retryTimeout = EditorApplication.timeSinceStartup + 10;
                    EditorApplication.update += Tick;
                }
                catch (Exception exception) { Fail(exception); }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= Tick;
                SessionState.SetBool(Active, false);
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Result, 1));
            }
        }
        static void Setup()
        {
            puzzle = UnityEngine.Object.FindAnyObjectByType<RevelationPuzzle>();
            stones = UnityEngine.Object.FindObjectsByType<RevelationStele>().OrderBy(stone => stone.transform.position.x).ToArray();
            feedback = UnityEngine.Object.FindAnyObjectByType<RevelationMonument>();
            tracker = UnityEngine.Object.FindAnyObjectByType<ClueTracker>();
            reader = UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();
            interaction = reader.GetComponent<PlayerInteraction>();
            prompt = UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            promptText = (TMP_Text)Reference(prompt, "_label");
            journal = UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();
            reticle = UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>();
            camera = reader.GetComponentInChildren<Camera>();
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            xbox = InputSystem.AddDevice<XInputController>();
            sony = InputSystem.AddDevice<DualShockGamepad>();
            actions = (TikalInputActions)typeof(PlayerInputReader).GetField("_actions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reader);
            oldDevices = actions.devices;
            actions.devices = new InputDevice[] { keyboard, xbox, sony };
            solves = discoveries = phase = 0;
            puzzle.PuzzleSolved += _ => solves++;

            tracker.ClueDiscovered += clue => { if (clue.Id == "level01.clue05") discoveries++; };
        }
        static void Refresh()
        {
            Physics.SyncTransforms();
            Method(typeof(PlayerInteraction), "LateUpdate").Invoke(interaction, null);
            Method(typeof(InteractionPromptPresenter), "LateUpdate").Invoke(prompt, null);
        }
        static void Keys(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update(); Refresh();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); Refresh();
        }
        static void Pad(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); InputSystem.Update(); Refresh();
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update(); Refresh();
        }
        static void Aim(Component target)
        {
            Vector3 centre = target is RevelationStele stone ? stone.GetComponentsInChildren<Renderer>().Single(r => r.name == "Upright stele").bounds.center : target.GetComponentInChildren<Collider>().bounds.center;
            camera.transform.position = centre + Vector3.back * 2.2f;
            camera.transform.LookAt(centre);
            Refresh();
        }
        static void Native(RevelationStele stone, int device)
        {
            Aim(stone);
            if (device == 0) Keys(Key.H); else Pad(device == 1 ? (Gamepad)xbox : sony, GamepadButton.RightShoulder);
            Require(ReferenceEquals(interaction.CurrentInteractable, stone), "Real ray focuses stone " + stone.name);
            string expected = device == 0 ? "[ E ]  Activar" : device == 1 ? "[ X ]  Activar" : "[ □ ]  Activar";
            Require(promptText.text == expected, "Dynamic prompt " + expected);
            Require(reticle.IsVisible, "Reticle stays stable at focused stone " + stone.name);
            if (device == 0) Keys(Key.E); else Pad(device == 1 ? (Gamepad)xbox : sony, GamepadButton.West);
        }
        static void CheckSelection(params int[] selected)
        {
            for(int i=0;i<4;i++)
            {
                bool active=selected.Contains(i);
                Require(stones[i].IsSelected==active,"Selection state "+i);
                var array=new SerializedObject(stones[i]).FindProperty("_symbols");
                for(int j=0;j<array.arraySize;j++)
                {
                    var block=new MaterialPropertyBlock();
                    ((Renderer)array.GetArrayElementAtIndex(j).objectReferenceValue).GetPropertyBlock(block);
                    Require(active?block.GetColor("_EmissionColor").r>0:block.GetColor("_EmissionColor").r<.001f,"Integrated symbol feedback "+i+"/"+j);
                }
            }
        }
        static Door door;
        static Door.DoorState doorState;
        static Vector3 panelClosed,playerBefore;
        static void ValidateRuntime()
        {
            Require(puzzle.PuzzleId=="level01.revelation" && puzzle.State==PuzzleState.Available && !puzzle.IsEvaluating,"Initial available state");
            CheckSelection();Require(!feedback.IsRevealed,"Monument initially closed");
            panelClosed=((Transform)Reference(feedback,"_panel")).localPosition;
            var readable=UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(r=>r.Text=="Lo que aparece al principio vuelve al final.");
            int readings=0;readable.ReadRequested+=_=>readings++;
            Aim(readable);Keys(Key.E);
            Require(readings==1 && tracker.Count==0,"Leer help works without clue");
            var clues=Clues();foreach(var clue in new[]{clues[3],clues[1],clues[0],clues[2]})tracker.Discover(clue);
            Require(tracker.Count==4 && !tracker.HasClue("level01.clue05"),"Prior four clues retained");
            door=UnityEngine.Object.FindAnyObjectByType<Door>();doorState=door.State;
            Aim(stones[0]);Keys(Key.G);Pad(xbox,GamepadButton.North);Pad(sony,GamepadButton.North);
            Require(!puzzle.IsSolved && puzzle.SelectionCount==0 && door.State==doorState,"Secret input inert before solve");
            Native(stones[0],0);CheckSelection(0);
            Require(!puzzle.IsSolved && puzzle.SelectionCount==1,"First selection does not solve");
            stones[0].Interact();Require(puzzle.SelectionCount==1,"Same stele cannot be selected twice");
            Native(stones[1],1);CheckSelection(0,1);
            Require(!puzzle.IsSolved && puzzle.IsEvaluating && puzzle.IncorrectAttempts==1 && tracker.Count==4,"Wrong pair produces guarded brief feedback");
            stones[2].Interact();Require(puzzle.SelectionCount==2 && !stones[2].IsSelected,"Additional selection rejected during evaluation");
        }
        static void ValidateRetry()
        {
            Require(!puzzle.IsEvaluating && stones.All(s=>s.CanInteract),"Wrong pair clears and retry is available");
            CheckSelection();
            Native(stones[1],2);Require(puzzle.ResetPuzzle(),"Framework reset clears single selection");CheckSelection();
            var settings=UnityEngine.Object.FindAnyObjectByType<ReticleSettings>();settings.SetMode(ReticleMode.Cross);
            Native(stones[2],2);CheckSelection(2);
            Keys(Key.J);Require(journal.IsOpen && !reader.GameplayEnabled && !reticle.IsVisible,"Journal gates player and reticle");
            Aim(stones[0]);Keys(Key.E);Require(!puzzle.IsSolved && puzzle.SelectionCount==4,"Journal cannot activate second stele");
            Keys(Key.J);Require(reader.GameplayEnabled && reticle.IsVisible && settings.Mode==ReticleMode.Cross,"Journal restores Cross/gameplay");
            playerBefore=reader.transform.position;
            Native(stones[0],0);
            Require(puzzle.IsSolved && solves==1 && !puzzle.IsEvaluating,"Matching C+A solves exactly once");CheckSelection(0,2);
            Require(feedback.IsRevealed && ((Transform)Reference(feedback,"_panel")).localPosition==panelClosed+Vector3.up*1.15f,"Central panel raises once");
            var block=new MaterialPropertyBlock();((Renderer)Reference(feedback,"_interior")).GetPropertyBlock(block);
            Require(block.GetColor("_EmissionColor").r>.5f,"Interior becomes warmly illuminated");
            Require(reader.transform.position==playerBefore && reader.GameplayEnabled && door.State==doorState,"Solve preserves freedom, location and entrance Door");
            var clues=Clues();
            Require(tracker.Count==5 && discoveries==1 && clues.All(c=>tracker.HasClue(c.Id)),"Fifth clue awarded once, previous four preserved");
            var discovery=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();
            Require(((GameObject)Reference(discovery,"_panel")).activeSelf && ((TMP_Text)Reference(discovery,"_title")).text=="El ciclo" &&
                ((TMP_Text)Reference(discovery,"_description")).text=="El inicio es el final.","Discovery exact final clue");
            Keys(Key.J);var body=(TMP_Text)Reference(journal,"_content");
            Require(clues.All(c=>body.text.Contains(c.Title.ToUpperInvariant()) && body.text.Contains(c.Description)),"Journal contains all five exact clues");
            Require(Enumerable.Range(0,4).All(i=>body.text.IndexOf(clues[i].Title.ToUpperInvariant(),StringComparison.Ordinal)<body.text.IndexOf(clues[i+1].Title.ToUpperInvariant(),StringComparison.Ordinal)),"Journal order 01 to 05");
            Require(!System.Text.RegularExpressions.Regex.IsMatch(body.text,@"\bG\b|SecretCommand|Door|regresa|presiona",System.Text.RegularExpressions.RegexOptions.IgnoreCase),"Journal adds no solution, key, return or Door hint");
            Canvas.ForceUpdateCanvases();body.ForceMeshUpdate();Require(!body.isTextOverflowing,"Five clues fit journal");
            Keys(Key.J);
            foreach(var stele in stones){stele.Interact();Require(!stele.CanInteract,"Solved stele locked "+stele.name);}
            Require(!puzzle.ResetPuzzle() && !puzzle.BeginPuzzle() && solves==1,"Solved terminal state");
            var reward=UnityEngine.Object.FindObjectsByType<PuzzleClueReward>().Single(r=>Reference(r,"_puzzle")==puzzle);
            reward.enabled=false;reward.enabled=true;
            feedback.enabled=false;feedback.enabled=true;
            Require(tracker.Count==5 && discoveries==1 && ((Transform)Reference(feedback,"_panel")).localPosition==panelClosed+Vector3.up*1.15f,"Reward/monument re-enable cannot duplicate effects");
            Keys(Key.G);Pad(xbox,GamepadButton.North);Pad(sony,GamepadButton.North);
            Require(solves==1 && discoveries==1 && door.State==doorState && reader.GameplayEnabled,"G/Y/triangle inert after fifth clue");
            Require(puzzle.SelectionCount==5 && puzzle.IncorrectAttempts==1 && puzzle.SecondsToSolve>=0,"Development metrics record selections, wrong attempts and elapsed solve time");
            settings.SetMode(ReticleMode.None);Require(!reticle.IsVisible,"None mode preserved");
            settings.SetMode(ReticleMode.Dot);Require(reticle.IsVisible,"Dot mode preserved");
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup<deadline)return;
            try
            {
                if(phase==0)
                {
                    if(puzzle.IsEvaluating && EditorApplication.timeSinceStartup < retryTimeout) return;
                    ValidateRetry();phase=1;deadline=EditorApplication.timeSinceStartup+6.5;return;
                }
                Require(!((GameObject)Reference(UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>(),"_panel")).activeSelf,"Discovery expires normally");
                CheckSelection(0,2);
                Keys(Key.J);Require(journal.IsOpen && Clues().All(c=>((TMP_Text)Reference(journal,"_content")).text.Contains(c.Description)),"Five clues remain accessible");
                Keys(Key.J);Require(reader.GameplayEnabled && reticle.IsVisible && solves==1 && discoveries==1,"Stable completion across frames");
                Debug.Log("TIKAL_REVELATION_VALIDATION_PASSED checks="+checks);
                SessionState.SetInt(Result,0);DisposeInput();EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            }
            catch(Exception e){Fail(e);}
        }
        static void DisposeInput()
        {
            if (actions != null) actions.devices = oldDevices;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (xbox != null) InputSystem.RemoveDevice(xbox);
            if (sony != null) InputSystem.RemoveDevice(sony);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            keyboard = null; xbox = null; sony = null; actions = null;
        }
        static void Fail(Exception exception)
        {
            Debug.LogException(exception); DisposeInput(); EditorApplication.update -= Tick;
            SessionState.SetInt(Result, 1); EditorApplication.ExitPlaymode();
        }
    }
}
