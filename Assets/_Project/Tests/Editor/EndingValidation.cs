using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Tikal.Core;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class EndingValidation
    {
        const string Active="Tikal.Ending.Active",Result="Tikal.Ending.Result",Count="Tikal.Ending.Count";
        static int checks;
        static IEnumerator routine;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static Keyboard keyboard;
        static Gamepad[] pads;
        static LevelOutcomeController outcome;
        static PlayerInputReader reader;
        static PlayerInteraction interaction;
        static Camera camera;
        static TikalInputActions actions;
        static ClueTracker tracker;
        static Door door;
        static ClueJournalPresenter journal;
        static InteractionPromptPresenter prompt;
        static ReticlePresenter reticle;
        static SecretEntranceCommandReceiver secret;
        static FalsePathReveal path;
        static LevelResultPresenter result;
        static InputSystemUIInputModule module;
        static InputActionAsset uiAsset;
        static UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? oldUI;
        static bool uiSaved;
        static EndingValidation()=>EditorApplication.playModeStateChanged+=State;
        static UnityEngine.Object Ref(UnityEngine.Object target,string field)=>new SerializedObject(target).FindProperty(field).objectReferenceValue;
        static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
        static void Require(bool value,string message){if(!value)throw new Exception("Ending: "+message);checks++;Debug.Log("ENDING_CHECK: "+message);}
        public static void Run()
        {
            try
            {
                checks=0;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var all=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Component>(true)).ToArray();
                Require(all.All(c=>c!=null),"No Missing Scripts");
                Require(all.OfType<LevelOutcomeController>().Count()==1&&all.OfType<SecretEntranceCommandReceiver>().Count()==1,"One outcome and one explicit secret receiver");
                Require(all.OfType<EventSystem>().Count()==1&&all.OfType<Canvas>().Count()==1,"One UI event system and reused Canvas");
                Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==scene.path),"Level01 registered for real runtime retry");
                var receiver=all.OfType<SecretEntranceCommandReceiver>().Single();
                Require(receiver.Range==3&&Mathf.Approximately(receiver.MinimumDot,.7f),"Exact 3m / dot 0.7 configuration");
                foreach(var name in new[]{"_inputReader","_player","_camera","_target","_zone","_outcome"})Require(Ref(receiver,name)!=null,"Explicit secret reference "+name);
                var warning=all.OfType<ReadableInscription>().Single(r=>r.Text=="Si continúas, termina tu recorrido.");
                Require(warning.InteractionText=="Leer"&&warning.transform.Find("VisualProxy")!=null&&warning.transform.Find("Collider").GetComponent<BoxCollider>()!=null,"Replaceable warning model with independent collider/readable logic");
                Require(all.OfType<PointOfNoReturnTrigger>().Single().transform.position.z-warning.transform.position.z==8.5f,"Warning precedes no-return centre by 8.5m");
                DynamicPromptValidation.ValidateScene(all);
                SessionState.SetInt(Count,checks);SessionState.SetBool(Active,true);SessionState.SetInt(Result,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void State(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                checks=SessionState.GetInt(Count,0);
                oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();pads=new Gamepad[]{InputSystem.AddDevice<XInputController>(),InputSystem.AddDevice<DualShockGamepad>(),InputSystem.AddDevice<Gamepad>()};
                Bind();routine=Validate();EditorApplication.update+=Tick;
            }
            else if(state==PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update-=Tick;SessionState.SetBool(Active,false);
                if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt(Result,1));
            }
        }
        static void Bind()
        {
            outcome=UnityEngine.Object.FindAnyObjectByType<LevelOutcomeController>();reader=UnityEngine.Object.FindAnyObjectByType<PlayerInputReader>();
            interaction=reader.GetComponent<PlayerInteraction>();camera=reader.GetComponentInChildren<Camera>();
            actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);
            actions.devices=new InputDevice[]{keyboard,pads[0],pads[1],pads[2]};
            tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();door=UnityEngine.Object.FindAnyObjectByType<Door>();
            journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();prompt=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            reticle=UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>();secret=UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
            path=UnityEngine.Object.FindAnyObjectByType<FalsePathReveal>();result=UnityEngine.Object.FindAnyObjectByType<LevelResultPresenter>();
            module=UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();uiAsset=module.actionsAsset;
            if(!uiSaved){oldUI=uiAsset.devices;uiSaved=true;}uiAsset.devices=new InputDevice[]{keyboard,pads[0],pads[1],pads[2]};
        }
        static void Refresh(){Physics.SyncTransforms();Method(typeof(PlayerInteraction),"LateUpdate").Invoke(interaction,null);Method(typeof(InteractionPromptPresenter),"LateUpdate").Invoke(prompt,null);}
        static void Keys(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();Refresh();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Refresh();}
        static void Pad(Gamepad pad,GamepadButton button){InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));InputSystem.Update();Refresh();InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();Refresh();}
        static void Place(Vector3 point,Vector3 look)
        {
            var controller=reader.GetComponent<CharacterController>();controller.enabled=false;reader.transform.position=point;reader.transform.rotation=Quaternion.identity;controller.enabled=true;
            camera.transform.localPosition=Vector3.zero;camera.transform.LookAt(look);Refresh();
        }
        static void NeutralCheck()
        {
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==0&&UnityEngine.Object.FindObjectsByType<PuzzleBase>().All(p=>!p.IsSolved),"Fresh Playing state, no clues or solved puzzles");
            Require(!path.IsRevealed&&!((GameObject)Ref(path,"_path")).activeSelf&&((GameObject)Ref(path,"_gate")).activeSelf,"False path initially inaccessible and hidden");
            Require(!result.IsVisible&&reticle.IsVisible&&reader.GameplayEnabled&&journal.enabled&&door.State==Door.DoorState.Closed,"Fresh gameplay, Door, reticle and UI");
        }
        static void OutcomeCheck(LevelOutcome expected)
        {
            Require(outcome.State==expected&&result.IsVisible,"Correct terminal outcome and result UI");
            var title=(TMP_Text)Ref(result,"_title");var subtitle=(TMP_Text)Ref(result,"_subtitle");
            Require(title.text==(expected==LevelOutcome.Victory?"NIVEL COMPLETADO":"HAS QUEDADO ATRAPADO")&&subtitle.text==(expected==LevelOutcome.Victory?"La Entrada Secreta":""),"Exact sober result text");
            Require(!reader.GameplayEnabled&&!reader.GetComponent<PlayerController>().GameplayActive&&!reticle.IsVisible&&!prompt.enabled,"Gameplay, reticle and world prompt gated");
            Keys(Key.J);Require(!journal.enabled&&!journal.IsOpen&&Cursor.lockState==CursorLockMode.None&&Cursor.visible,"Journal cannot cover result and cursor released");
            Require(EventSystem.current.currentSelectedGameObject==((Button)Ref(result,"_retry")).gameObject&&module.submit.action.enabled,"Retry selected and native UI Submit enabled");
            Canvas.ForceUpdateCanvases();title.ForceMeshUpdate();Require(!title.isTextOverflowing,"Result title fits");
        }
        static IEnumerator Retry(int device)
        {
            var oldOutcome=outcome;
            if(device<0)InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
            else InputSystem.QueueStateEvent(pads[device],new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();module.Process();
            if(device<0)InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            else InputSystem.QueueStateEvent(pads[device],new GamepadState());
            InputSystem.Update();
            double limit=EditorApplication.timeSinceStartup+10;
            while(oldOutcome!=null){RequireTime(EditorApplication.timeSinceStartup<limit,"Native UI Submit did not reload scene");yield return null;}
            Bind();NeutralCheck();Require(reader.transform.position.z<-8,"Retry restores original spawn");
        }
        static void RequireTime(bool value,string message){if(!value)throw new Exception(message);}
        static IEnumerator Validate()
        {
            yield return null;
            NeutralCheck();
            Keys(Key.G);Require(outcome.State==LevelOutcome.Playing,"Secret at spawn does not win");
            var target=((Transform)Ref(secret,"_target")).position;
            Place(new Vector3(0,0,-4),target);Keys(Key.G);Require(outcome.State==LevelOutcome.Playing,"Too far does not win");
            Place(new Vector3(0,0,-2.5f),new Vector3(0,1.62f,-10));Keys(Key.G);Require(outcome.State==LevelOutcome.Playing,"Facing backwards does not win");
            Place(new Vector3(0,0,-2.5f),target);camera.transform.rotation=Quaternion.Euler(0,60,0);Keys(Key.G);Require(outcome.State==LevelOutcome.Playing,"Dot below 0.7 does not win");
            Place(new Vector3(0,0,-2.5f),target);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary line of sight test";wall.transform.position=new Vector3(0,1.6f,-1.2f);wall.transform.localScale=new Vector3(2,2,.2f);Physics.SyncTransforms();
            Keys(Key.G);Require(outcome.State==LevelOutcome.Playing,"Wall between camera and target blocks secret");
            UnityEngine.Object.DestroyImmediate(wall);Refresh();
            Keys(Key.E);Require(outcome.State==LevelOutcome.Playing&&door.State==Door.DoorState.Opening,"Normal E opens Door without secret result");
            Keys(Key.E);Require(door.State==Door.DoorState.Closed&&outcome.State==LevelOutcome.Playing,"Normal E closes/reverses independently");
            for(int device=-1;device<3;device++)
            {
                if(device!=-1)NeutralCheck();
                Place(new Vector3(0,0,-2.5f),target);
                if(device==0){door.Interact();Require(door.State==Door.DoorState.Opening,"Opening state tested");}
                if(device==1){door.Interact();Method(typeof(Door),"Tick").Invoke(door,new object[]{2f});Require(door.State==Door.DoorState.Open,"Open state tested");}
                if(device==2){door.Interact();Method(typeof(Door),"Tick").Invoke(door,new object[]{2f});door.Interact();Require(door.State==Door.DoorState.Closing,"Closing state tested");camera.transform.rotation=Quaternion.Euler(0,45,0);}
                int notifications=0;outcome.OutcomeChanged+=_=>notifications++;
                for(int clue=1;clue<=5;clue++)Require(!tracker.HasClue("level01.clue0"+clue),"Speedrun does not require Clue0"+clue);
                var before=door.State;
                Debug.Log("ENDING_SECRET_POSE player="+reader.transform.position+" camera="+camera.transform.position+" target="+target+" zone="+((BoxCollider)Ref(secret,"_zone")).bounds);
                if(device<0)Keys(Key.G);else Pad(pads[device],GamepadButton.North);
                Require(outcome.State==LevelOutcome.Victory&&tracker.Count==0&&door.State==before,"Fresh native secret wins independently of all puzzles/device "+device);
                Require(!outcome.TriggerVictory()&&!outcome.TriggerDefeat()&&notifications==1,"Victory idempotent and rejects Defeat");
                OutcomeCheck(LevelOutcome.Victory);
                var retry=Retry(device);while(retry.MoveNext())yield return retry.Current;
                target=((Transform)Ref(secret,"_target")).position;
            }
            // A solved fifth puzzle reveals the path, without requiring earlier clues or causing an outcome.
            var revelation=UnityEngine.Object.FindAnyObjectByType<RevelationPuzzle>();
            var steles=UnityEngine.Object.FindObjectsByType<RevelationStele>().OrderBy(s=>s.transform.position.x).ToArray();
            Place(new Vector3(steles[0].transform.position.x,.6f,68.5f),steles[0].GetComponentsInChildren<Renderer>().Single(r=>r.name=="Upright stele").bounds.center);Keys(Key.E);
            Place(new Vector3(steles[2].transform.position.x,.6f,68.5f),steles[2].GetComponentsInChildren<Renderer>().Single(r=>r.name=="Upright stele").bounds.center);Keys(Key.E);
            Require(revelation.IsSolved&&path.IsRevealed&&((GameObject)Ref(path,"_path")).activeSelf&&!((GameObject)Ref(path,"_gate")).activeSelf,"Puzzle05 event reveals physical false path");
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==1,"Revelation alone never wins or defeats");
            var warning=UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(r=>r.Text=="Si continúas, termina tu recorrido.");
            int readings=0;warning.ReadRequested+=_=>readings++;
            Place(new Vector3(6,.6f,82),warning.GetComponentInChildren<Collider>().bounds.center);Keys(Key.E);
            Require(readings==1&&tracker.Count==1&&outcome.State==LevelOutcome.Playing,"Warning is readable without recording Clue06 or causing defeat");
            Keys(Key.J);Require(journal.IsOpen&&!((TMP_Text)Ref(journal,"_content")).text.Contains(warning.Text),"Journal excludes warning");Keys(Key.J);
            Place(new Vector3(6,.6f,90.3f),new Vector3(6,2.22f,96));Require(outcome.State==LevelOutcome.Playing,"Safe separation before no return");
            var cc=reader.GetComponent<CharacterController>();var simulation=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            try{for(int step=0;step<10&&outcome.State==LevelOutcome.Playing;step++){cc.Move(Vector3.forward*.2f);Physics.Simulate(.02f);}}
            finally{Physics.simulationMode=simulation;}
            yield return null;
            Require(outcome.State==LevelOutcome.Defeat,"Actual CharacterController crosses trigger and defeats");
            var trap=UnityEngine.Object.FindAnyObjectByType<PointOfNoReturnTrigger>();
            Require(((GameObject)Ref(trap,"_closingPassage")).activeSelf,"Passage closes behind player");
            int defeats=0;outcome.OutcomeChanged+=_=>defeats++;
            Method(typeof(PointOfNoReturnTrigger),"OnTriggerEnter").Invoke(trap,new object[]{cc});
            Require(!outcome.TriggerDefeat()&&!outcome.TriggerVictory()&&defeats==0,"Defeat idempotent and rejects Victory");
            OutcomeCheck(LevelOutcome.Defeat);
            var again=Retry(0);while(again.MoveNext())yield return again.Current;
            Require(UnityEngine.Object.FindObjectsByType<GlyphStone>().All(s=>!s.IsActivated)&&UnityEngine.Object.FindObjectsByType<RevelationStele>().All(s=>!s.IsSelected),"Retry resets puzzle visuals");
            var journeys=Journeys();while(journeys.MoveNext())yield return journeys.Current;
            Debug.Log("TIKAL_ENDING_VALIDATION_PASSED checks="+checks);
        }
        static void DriveStep(Vector2 direction,bool sprint=false)
        {
            reader.transform.rotation=Quaternion.identity;
            InputSystem.QueueStateEvent(pads[0],new GamepadState{leftStick=direction}.WithButton(GamepadButton.LeftStick,sprint));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            reader.GetComponent<PlayerMovement>().Tick(reader.Move,reader.SprintRequested,false,.02f);
            Method(typeof(Door),"Tick").Invoke(door,new object[]{.02f});
            Physics.Simulate(.02f);Refresh();
            RequireTime(reader.transform.position.y>-.6f&&reader.transform.position.y<3,"Journey left walkable elevation");
        }
        static void Walk(float x,float z,bool sprint=false)
        {
            var player=reader.GetComponent<PlayerController>();player.enabled=false;reader.EnableGameplay();
            var original=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            try
            {
                for(int step=0;step<2200;step++)
                {
                    if(outcome.State!=LevelOutcome.Playing)return;
                    Vector3 delta=new Vector3(x,0,z)-reader.transform.position;delta.y=0;
                    if(delta.magnitude<.18f){for(int settle=0;settle<12;settle++)DriveStep(Vector2.zero);Require(reader.GetComponent<PlayerMovement>().IsGrounded,"Journey grounded at "+x+"/"+z);return;}
                    DriveStep(new Vector2(delta.x,delta.z).normalized*Mathf.Clamp01(delta.magnitude),sprint&&delta.magnitude>1.2f);
                }
                throw new Exception("Journey blocked at "+reader.transform.position+" toward "+x+"/"+z);
            }
            finally{Physics.simulationMode=original;}
        }
        static void Focus(Component target,Vector3 point)
        {
            camera.transform.LookAt(point);Refresh();
            Require(ReferenceEquals(interaction.CurrentInteractable,target),"Journey ray focuses "+target.name);
        }
        static IEnumerator SolveJourney()
        {
            NeutralCheck();
            Walk(-6,-10);Walk(-6,-3);Walk(0,-3);Walk(0,-2.6f);
            Focus(door,new Vector3(0,1.62f,0));Keys(Key.E);Require(door.State==Door.DoorState.Opening,"Journey opens original Door normally");
            for(int i=0;i<45;i++)
            {
                var original=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
                try{DriveStep(Vector2.zero);}finally{Physics.simulationMode=original;}
            }
            Walk(0,0);Walk(0,12);Walk(-5.8f,14);
            var mural=UnityEngine.Object.FindAnyObjectByType<MuralFragmentsPuzzle>();var ms=new SerializedObject(mural);var fragments=ms.FindProperty("_fragments");var solution=ms.FindProperty("_solution");
            for(int i=0;i<4;i++)
            {
                var fragment=(MuralFragment)fragments.GetArrayElementAtIndex(i).objectReferenceValue;
                while(fragment.CurrentOrientation!=solution.GetArrayElementAtIndex(i).intValue)
                {
                    Focus(fragment,fragment.GetComponentInChildren<Collider>().bounds.center);Keys(Key.E);
                    for(int frame=0;frame<20;frame++)Method(typeof(MuralFragment),"AdvanceRotation").Invoke(fragment,new object[]{.02f});
                }
            }
            Require(mural.IsSolved&&tracker.Count==1,"Journey Puzzle01 solves and grants Clue01");
            Walk(0,12);Walk(29,12);
            var guardians=UnityEngine.Object.FindAnyObjectByType<PathGuardiansPuzzle>();var gs=new SerializedObject(guardians);var items=gs.FindProperty("_guardians");var targets=gs.FindProperty("_solution");
            for(int i=0;i<3;i++)
            {
                var guardian=(PathGuardian)items.GetArrayElementAtIndex(i).objectReferenceValue;
                Walk(guardian.transform.position.x-1.3f,guardian.transform.position.z);
                while(guardian.CurrentOrientation!=targets.GetArrayElementAtIndex(i).intValue)
                {
                    Focus(guardian,guardian.transform.position+Vector3.up*.9f);Keys(Key.E);
                    for(int frame=0;frame<20;frame++)Method(typeof(PathGuardian),"AdvanceRotation").Invoke(guardian,new object[]{.02f});
                }
            }
            Require(guardians.IsSolved&&tracker.Count==2,"Journey Puzzle02 solves and grants Clue02");
            Walk(29,12);Walk(29,39);Walk(30,39);Walk(30,41.6f);Walk(33,41.6f);
            var echo=UnityEngine.Object.FindAnyObjectByType<EchoSequencePuzzle>();var source=UnityEngine.Object.FindAnyObjectByType<EchoSource>();
            Focus(source,source.transform.position+Vector3.up*1.2f);Keys(Key.E);
            double end=EditorApplication.timeSinceStartup+15;
            while(source.IsPlaying){RequireTime(EditorApplication.timeSinceStartup<end,"Journey echo reference timeout");yield return null;}
            var sounds=UnityEngine.Object.FindObjectsByType<EchoStone>().OrderBy(s=>s.Note).ToArray();
            Walk(34.1f,41.2f);Walk(34.1f,43.4f);Walk(34.7f,43.4f);Focus(sounds[1],sounds[1].transform.position+Vector3.up*.8f);Keys(Key.E);
            Walk(34.1f,43.4f);Walk(34.1f,41.2f);Walk(33,41.6f);Walk(31.5f,41.6f);Walk(31.5f,43.2f);Focus(sounds[0],sounds[0].transform.position+Vector3.up*.8f);Keys(Key.E);
            Walk(31.5f,41.6f);Walk(33,41.6f);Walk(35,41.2f);Focus(sounds[2],sounds[2].transform.position+Vector3.up*1.2f);Keys(Key.E);
            Require(echo.IsSolved&&tracker.Count==3,"Journey Puzzle03 solves and grants Clue03");
            Walk(33,41.6f);Walk(30,41.6f);Walk(29,39);Walk(0,39);Walk(0,37.2f);
            foreach(var stone in UnityEngine.Object.FindObjectsByType<GlyphStone>().OrderBy(s=>s.SequenceValue))
            {
                Walk(stone.transform.position.x,37.2f);Focus(stone,((Renderer)Ref(stone,"_visual")).bounds.center);Keys(Key.E);
            }
            Require(UnityEngine.Object.FindAnyObjectByType<GlyphSequencePuzzle>().IsSolved&&tracker.Count==4,"Journey Puzzle04 solves and grants Clue04");
            Walk(0,37.2f);Walk(0,39);Walk(0,67);Walk(0,68.5f);
            var pair=UnityEngine.Object.FindObjectsByType<RevelationStele>().Where(s=>s.PatternId=="Cycle").OrderBy(s=>s.transform.position.x).ToArray();
            foreach(var stele in pair){Walk(stele.transform.position.x,68.5f);Focus(stele,stele.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Upright stele").bounds.center);Keys(Key.E);}
            Require(UnityEngine.Object.FindAnyObjectByType<RevelationPuzzle>().IsSolved&&tracker.Count==5&&path.IsRevealed&&outcome.State==LevelOutcome.Playing,"Journey Puzzle05 completes five clues and leaves human decision");
            reader.GetComponent<PlayerController>().enabled=true;Keys(Key.J);
            var body=(TMP_Text)Ref(journal,"_content");
            string[] titles={"EL CAMINO","EL ORIGEN","EL PRIMER CONTACTO","EL SÉPTIMO LUGAR","EL CICLO"};
            Require(journal.IsOpen&&titles.All(t=>body.text.Contains(t))&&Enumerable.Range(0,4).All(i=>body.text.IndexOf(titles[i],StringComparison.Ordinal)<body.text.IndexOf(titles[i+1],StringComparison.Ordinal)),"Journey Journal displays five clues in order before decision");
            Keys(Key.J);
            Walk(0,68.5f);Walk(6,73);Walk(6,82);
            var warning=UnityEngine.Object.FindObjectsByType<ReadableInscription>().Single(r=>r.Text=="Si continúas, termina tu recorrido.");
            Focus(warning,warning.GetComponentInChildren<Collider>().bounds.center);Keys(Key.E);
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==5,"Journey reads warning safely with no sixth clue");
        }
        static IEnumerator Journeys()
        {
            var first=SolveJourney();while(first.MoveNext())yield return first.Current;
            Walk(6,74,true);Walk(0,67,true);Walk(0,39,true);Walk(29,39,true);Walk(29,12,true);Walk(0,12,true);Walk(0,2.5f,true);
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==5,"Canonical return from warning through all five zones preserves decision");
            reader.GetComponent<PlayerController>().enabled=true;camera.transform.LookAt(((Transform)Ref(secret,"_target")).position);Keys(Key.G);
            Require(outcome.State==LevelOutcome.Victory&&tracker.Count==5,"CANONICAL native G at original Entrance wins after all five puzzles");
            OutcomeCheck(LevelOutcome.Victory);
            Debug.Log("TIKAL_ENDING_JOURNEY_CANONICAL_PASSED: physical traversal, native E and production rotation ticks, five real puzzle rewards, warning backtrack, original Door secret");
            var reset=Retry(-1);while(reset.MoveNext())yield return reset.Current;
            var second=SolveJourney();while(second.MoveNext())yield return second.Current;
            Walk(6,92);
            Require(outcome.State==LevelOutcome.Defeat&&tracker.Count==5,"DEFEAT physical continuation crosses real trigger after all five puzzles");
            OutcomeCheck(LevelOutcome.Defeat);
            Debug.Log("TIKAL_ENDING_JOURNEY_DEFEAT_PASSED: all five puzzles, warning, actual no-return crossing, Defeat and Retry");
            reset=Retry(1);while(reset.MoveNext())yield return reset.Current;
            Walk(-6,-10);Walk(-6,-3);Walk(0,-3);Walk(0,-2.6f);
            reader.GetComponent<PlayerController>().enabled=true;camera.transform.LookAt(((Transform)Ref(secret,"_target")).position);Pad(pads[2],GamepadButton.North);
            Require(outcome.State==LevelOutcome.Victory&&tracker.Count==0&&UnityEngine.Object.FindObjectsByType<PuzzleBase>().All(p=>!p.IsSolved),"SPEEDRUN physical spawn to Entrance wins without puzzle or clue");
            OutcomeCheck(LevelOutcome.Victory);
            Debug.Log("TIKAL_ENDING_JOURNEY_SPEEDRUN_PASSED: physical approach, Generic buttonNorth, zero clues and unsolved puzzles");
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if(routine.MoveNext())return;
                SessionState.SetInt(Result,0);Dispose();EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetInt(Result,1);Dispose();EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
        }
        static void Dispose()
        {
            if(uiSaved&&uiAsset!=null)uiAsset.devices=oldUI;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pads!=null)foreach(var pad in pads)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            routine=null;keyboard=null;pads=null;uiSaved=false;
        }
    }
}