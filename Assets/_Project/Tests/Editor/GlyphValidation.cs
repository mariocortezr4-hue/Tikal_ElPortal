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
    public static class GlyphValidation
    {
        const string Active = "Tikal.Glyph.Active", Result = "Tikal.Glyph.Result", Count = "Tikal.Glyph.Count";
        static int checks, solves, discoveries, wrong;
        static double deadline;
        static GlyphSequencePuzzle puzzle;
        static GlyphStone[] stones;
        static GlyphProgressFeedback feedback;
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
        static GlyphValidation() => EditorApplication.playModeStateChanged += State;
        static UnityEngine.Object Reference(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;
        static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        static void Require(bool value, string message)
        {
            if (!value) throw new Exception("Glyph: " + message);
            checks++;
            Debug.Log("GLYPH_CHECK: " + message);
        }
        static ClueDefinition[] Clues() => new[] { "mural_fragments", "path_guardians", "echo_chamber", "glyph_gallery" }
            .Select(name => AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01." + name + ".asset")).ToArray();

        [MenuItem("Tikal/Tests/Glyph gallery")]
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
                var p = all.OfType<GlyphSequencePuzzle>().Single();
                var plates = all.OfType<GlyphStone>().OrderBy(stone => stone.transform.position.x).ToArray();
                Require(plates.Length == 7 && plates.Select(stone => stone.SequenceValue).OrderBy(value => value).SequenceEqual(Enumerable.Range(1, 7)), "Exactly seven unique values 1-7 with no gaps or duplicates");
                Require(plates.Select(stone => stone.SequenceValue).SequenceEqual(new[] { 3, 1, 5, 2, 7, 4, 6 }), "Deterministic mixed spatial order");
                Require(p.transform.parent.name == "PuzzleAnchor" && p.transform.parent.parent.name == "Zone04_GlyphGallery", "Existing Zone04 PuzzleAnchor reused");
                foreach (var plate in plates)
                {
                    Require(plate.Puzzle == p && plate.HasVisual && plate.GetComponentInChildren<Collider>() != null, "Valid stone/puzzle/renderer/collider " + plate.SequenceValue);
                    var count = plate.transform.Find("Count marks");
                    Require(count.childCount == plate.SequenceValue && count.GetComponentsInChildren<Collider>().Length == 0, "Count is represented by visible neutral geometry " + plate.SequenceValue);
                    Require(plate.InteractionText == "Activar", "Semantic interaction text " + plate.SequenceValue);
                }
                var f = all.OfType<GlyphProgressFeedback>().Single();
                var marks = new SerializedObject(f).FindProperty("_marks");
                Require(marks.arraySize == 7 && Enumerable.Range(0, 7).All(index => marks.GetArrayElementAtIndex(index).objectReferenceValue != null), "Seven valid environmental progress marks");
                Require(Reference(f, "_frame") != null && ((Renderer)marks.GetArrayElementAtIndex(0).objectReferenceValue).transform.parent.parent.name == "GlyphWallAnchor", "Frieze uses preserved GlyphWallAnchor");
                var reward = all.OfType<PuzzleClueReward>().Single(component => Reference(component, "_puzzle") == p);
                var clue = (ClueDefinition)Reference(reward, "_clue");
                Require(clue.Id == "level01.clue04" && clue.Title == "El séptimo lugar" && clue.Description == "La respuesta ocupa el séptimo lugar entre las letras." && clue.Order == 4, "Exact Clue04 narrative contract and order");
                Require(!clue.Description.Contains("SecretCommand") && !System.Text.RegularExpressions.Regex.IsMatch(clue.Description, @"\bG\b|presiona", System.Text.RegularExpressions.RegexOptions.IgnoreCase), "Clue introduces seventh letter without revealing a key");
                Require(reward.transform.parent.name == "ClueAnchor" && Reference(reward, "_tracker") != null, "Existing reward/tracker architecture");
                var inscription = all.OfType<ReadableInscription>().Single(readable => readable.Text == "Cada marca sigue a la anterior.");
                var list = new SerializedObject(all.OfType<InscriptionPresenter>().Single()).FindProperty("_inscriptions");
                Require(Enumerable.Range(0, list.arraySize).Any(index => list.GetArrayElementAtIndex(index).objectReferenceValue == inscription), "Environmental help wired to existing inscription presentation");
                Require(all.OfType<Canvas>().Count() == 1 && all.OfType<ReticlePresenter>().Count() == 1 && all.OfType<ClueDiscoveryPresenter>().Count() == 1, "No duplicate HUD, reticle or discovery system");
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
                    deadline = EditorApplication.timeSinceStartup + 6.5;
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
            puzzle = UnityEngine.Object.FindAnyObjectByType<GlyphSequencePuzzle>();
            stones = UnityEngine.Object.FindObjectsByType<GlyphStone>().OrderBy(stone => stone.SequenceValue).ToArray();
            feedback = UnityEngine.Object.FindAnyObjectByType<GlyphProgressFeedback>();
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
            solves = discoveries = wrong = 0;
            puzzle.PuzzleSolved += _ => solves++;
            puzzle.WrongSelection += _ => wrong++;
            tracker.ClueDiscovered += clue => { if (clue.Id == "level01.clue04") discoveries++; };
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
            Vector3 centre = target is GlyphStone stone ? ((Renderer)Reference(stone, "_visual")).bounds.center : target.GetComponentInChildren<Collider>().bounds.center;
            camera.transform.position = centre + Vector3.forward * 2.2f;
            camera.transform.LookAt(centre);
            Refresh();
        }
        static void Native(GlyphStone stone, int device)
        {
            Aim(stone);
            if (device == 0) Keys(Key.H); else Pad(device == 1 ? (Gamepad)xbox : sony, GamepadButton.RightShoulder);
            Require(ReferenceEquals(interaction.CurrentInteractable, stone), "Real ray focuses stone " + stone.SequenceValue);
            string expected = device == 0 ? "[ E ]  Activar" : device == 1 ? "[ X ]  Activar" : "[ □ ]  Activar";
            Require(promptText.text == expected, "Dynamic prompt " + expected);
            Require(reticle.IsVisible, "Reticle stays stable at focused stone " + stone.SequenceValue);
            if (device == 0) Keys(Key.E); else Pad(device == 1 ? (Gamepad)xbox : sony, GamepadButton.West);
        }
        static void CheckMarks(int progress)
        {
            var array = new SerializedObject(feedback).FindProperty("_marks");
            for (int index = 0; index < 7; index++)
            {
                var renderer = (Renderer)array.GetArrayElementAtIndex(index).objectReferenceValue;
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                var emission = block.GetColor("_EmissionColor");
                Require(index < progress ? emission.r > .5f : emission.r < .001f, "Progress mark " + index + " matches " + progress);
            }
            foreach (var stone in stones)
                Require(stone.IsActivated == (stone.SequenceValue <= progress), "Stone feedback reflects progress " + stone.SequenceValue);
        }

        static void ValidateRuntime()
        {
            Require(puzzle.PuzzleId == "level01.glyph_gallery" && puzzle.State == PuzzleState.Available &&
                puzzle.ExpectedNextValue == 1 && puzzle.Progress == 0, "Initial available state expects 1");
            CheckMarks(0);
            var readable = UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(value => value.Text == "Cada marca sigue a la anterior.");
            int readings = 0; readable.ReadRequested += _ => readings++;
            Aim(readable); Keys(Key.E);
            Require(readings == 1 && tracker.Count == 0 && puzzle.Progress == 0, "Native Leer provides mechanical help without awarding a clue");
            var clues = Clues();
            foreach (var clue in new[] { clues[2], clues[0], clues[1] }) tracker.Discover(clue);
            Require(tracker.Count == 3 && !tracker.HasClue("level01.clue04"), "Previous three clues exist; Clue04 stays undiscovered");
            var door = UnityEngine.Object.FindAnyObjectByType<Door>(); var doorState = door.State;
            Aim(stones[0]);
            Keys(Key.G); Pad(xbox, GamepadButton.North); Pad(sony, GamepadButton.North);
            Require(puzzle.Progress == 0 && !puzzle.IsSolved && tracker.Count == 3 && door.State == doorState, "G/Y/triangle cannot activate glyphs, solve or change Door");
            Native(stones[0], 0);
            Require(puzzle.ExpectedNextValue == 2 && !puzzle.IsSolved, "1 progresses"); CheckMarks(1);
            Native(stones[1], 1);
            Require(puzzle.ExpectedNextValue == 3, "1 then 2 progresses"); CheckMarks(2);
            Native(stones[4], 2);
            Require(puzzle.ExpectedNextValue == 1 && puzzle.Progress == 0 && !puzzle.IsSolved && wrong == 1 &&
                stones.All(stone => stone.CanInteract), "Wrong value clears progress and permits immediate retry"); CheckMarks(0);
            Native(stones[0], 2);
            Require(puzzle.ExpectedNextValue == 2, "Immediate retry succeeds during soft error feedback");
            Require(puzzle.ResetPuzzle() && puzzle.State == PuzzleState.Available && puzzle.ExpectedNextValue == 1, "Explicit framework reset remains valid"); CheckMarks(0);
            var reticleSettings = UnityEngine.Object.FindAnyObjectByType<ReticleSettings>();
            reticleSettings.SetMode(ReticleMode.Cross);
            for (int index = 0; index < 6; index++)
            {
                Native(stones[index], index % 3);
                Require(!puzzle.IsSolved && puzzle.Progress == index + 1 && tracker.Count == 3, "Sequence before seven cannot solve " + (index + 1));
                CheckMarks(index + 1);
            }
            var cursor = Cursor.lockState; bool visible = Cursor.visible;
            Keys(Key.J);
            Require(journal.IsOpen && !reader.GameplayEnabled && !reticle.IsVisible, "Journal gates glyph interaction and reticle");
            stones[6].enabled = false;
            Aim(stones[6]); Keys(Key.E);
            Require(puzzle.Progress == 6, "Disabled stone and Journal input cannot advance");
            stones[6].enabled = true; Keys(Key.J);
            Require(!journal.IsOpen && reader.GameplayEnabled && reticle.IsVisible && reticleSettings.Mode == ReticleMode.Cross &&
                Cursor.lockState == cursor && Cursor.visible == visible, "Journal restores cursor, gameplay and Cross");
            Native(stones[6], 2);
            Require(puzzle.IsSolved && puzzle.State == PuzzleState.Solved && puzzle.Progress == 7 &&
                puzzle.ExpectedNextValue == 8 && solves == 1, "Ordered 1-7 solves exactly once"); CheckMarks(7);
            Require(tracker.Count == 4 && discoveries == 1 && clues.All(clue => tracker.HasClue(clue.Id)), "Reward adds Clue04 once while retaining Clue01/02/03");
            var discovery = UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();
            Require(((GameObject)Reference(discovery, "_panel")).activeSelf &&
                ((TMP_Text)Reference(discovery, "_title")).text == "El séptimo lugar" &&
                ((TMP_Text)Reference(discovery, "_description")).text == clues[3].Description, "Existing Discovery UI presents exact Clue04");
            Keys(Key.J);
            var body = (TMP_Text)Reference(journal, "_content");
            Require(journal.IsOpen && clues.All(clue => body.text.Contains(clue.Title.ToUpperInvariant()) && body.text.Contains(clue.Description)), "Journal contains all four clues");
            Require(Enumerable.Range(0, 3).All(index => body.text.IndexOf(clues[index].Title.ToUpperInvariant(), StringComparison.Ordinal) <
                body.text.IndexOf(clues[index + 1].Title.ToUpperInvariant(), StringComparison.Ordinal)), "Journal entries are in authored order");
            Require(!body.text.Contains("PLACEHOLDER") && !body.text.Contains("Clue05") && !body.text.Contains("SecretCommand"), "Journal reveals no future clue or command");
            Canvas.ForceUpdateCanvases(); body.ForceMeshUpdate();
            Require(!body.isTextOverflowing, "Four clues fit the existing journal");
            Keys(Key.J);
            foreach (var stone in stones) { stone.Interact(); Require(!stone.CanInteract, "Solved plate remains unavailable " + stone.SequenceValue); }
            Require(!puzzle.ResetPuzzle() && !puzzle.BeginPuzzle() && solves == 1, "Solved terminal state cannot reset or solve again");
            var reward = UnityEngine.Object.FindObjectsByType<PuzzleClueReward>().Single(value => Reference(value, "_puzzle") == puzzle);
            reward.enabled = false; reward.enabled = true;
            Require(discoveries == 1 && tracker.Count == 4, "Reward catch-up cannot duplicate Clue04");
            Keys(Key.G); Pad(xbox, GamepadButton.North); Pad(sony, GamepadButton.North);
            Require(solves == 1 && discoveries == 1 && door.State == doorState, "Secret input remains inert after fourth clue");
            reticleSettings.SetMode(ReticleMode.None);
            Require(!reticle.IsVisible, "None remains available without puzzle-dependent reticle state");
            reticleSettings.SetMode(ReticleMode.Dot);
            Require(reticle.IsVisible, "Dot restored after solve");
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup < deadline) return;
            try
            {
                Require(!((GameObject)Reference(UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>(), "_panel")).activeSelf, "Discovery expires normally");
                CheckMarks(7);
                Keys(Key.J);
                Require(journal.IsOpen && Clues().All(clue => ((TMP_Text)Reference(journal, "_content")).text.Contains(clue.Description)), "All four clues remain consultable after notification expiry");
                Keys(Key.J);
                Require(reader.GameplayEnabled && reticle.IsVisible && solves == 1 && discoveries == 1, "Completion stable across real frames");
                Debug.Log("TIKAL_GLYPH_VALIDATION_PASSED checks=" + checks);
                SessionState.SetInt(Result, 0);
                DisposeInput(); EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
            }
            catch (Exception exception) { Fail(exception); }
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
