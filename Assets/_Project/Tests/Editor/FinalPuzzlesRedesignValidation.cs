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
    public static class FinalPuzzlesRedesignValidation
    {
        const string Active="Tikal.FinalRedesign.Active",Result="Tikal.FinalRedesign.Result",Count="Tikal.FinalRedesign.Count";
        static int checks;
        static IEnumerator routine;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static Keyboard keyboard;
        static Mouse mouse;
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
        static FinalPuzzlesRedesignValidation()=>EditorApplication.playModeStateChanged+=State;
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
                var secretClue = AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.revelation.asset");
                Require(secretClue.Id == "level01.clue05" && secretClue.SemanticContent == ClueSemanticContent.SecretCommand &&
                    secretClue.Description == "Ya conoces la llave.", "Clue05 stores semantic SecretCommand with no universal G text");
                foreach (var presenter in all.OfType<ClueDiscoveryPresenter>().Cast<UnityEngine.Object>().Concat(all.OfType<ClueJournalPresenter>()))
                {
                    var serialized = new SerializedObject(presenter);
                    Require(serialized.FindProperty("_inputReader").objectReferenceValue == all.OfType<PlayerInputReader>().Single() &&
                        serialized.FindProperty("_devices").objectReferenceValue == all.OfType<InputDeviceManager>().Single(),
                        "Clue UI shares canonical action source and active device");
                }
                Require(all.OfType<LevelOutcomeController>().Count()==1&&all.OfType<SecretEntranceCommandReceiver>().Count()==1,"One outcome and one explicit secret receiver");
                Require(all.OfType<EventSystem>().Count()==1&&all.OfType<Canvas>().Count()==1,"One UI event system and reused Canvas");
                Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==scene.path),"Level01 registered for real runtime retry");
                var receiver=all.OfType<SecretEntranceCommandReceiver>().Single();
                Require(receiver.Range==3&&Mathf.Approximately(receiver.MinimumDot,.7f),"Exact 3m / dot 0.7 configuration");
                foreach(var name in new[]{"_inputReader","_player","_camera","_target","_zone","_outcome"})Require(Ref(receiver,name)!=null,"Explicit secret reference "+name);
                Require(!all.OfType<GlyphStone>().Any()&&!all.OfType<GlyphSequencePuzzle>().Any()&&!all.OfType<RevelationStele>().Any()&&!all.OfType<RevelationPuzzle>().Any(),"No old playable mechanisms left in scene");
                Require(all.OfType<PuzzleBase>().Count()==5,"Exactly five canonical puzzles");
                var observation=all.OfType<ObservationAlignmentPuzzle>().Single();
                Require(observation.MinimumDot==.97f&&observation.HoldSeconds==.35f,"Exact dot .97 and .35 second hold");
                Require(all.OfType<ReadableInscription>().Any(r=>r.Text=="Debes aprender a observar."),"Observation philosophy inscription");
                var anchor=all.OfType<Transform>().Single(r=>r.name=="FinalWarningSteleAnchor");
                Require(anchor.GetComponentsInChildren<ReadableInscription>(true).Length==0&&anchor.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Final warning proxy has no interaction, prompt or clue behavior");
                Require(anchor.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="FinalWarningStele_Proxy"),"Replacement proxy under independent final warning anchor");
                Require(all.OfType<PointOfNoReturnTrigger>().Single().transform.position.z-anchor.position.z==8,"Eight longitudinal metres before trigger centre");
                foreach(var target in new UnityEngine.Object[]{all.OfType<ObservationPoint>().Single(),all.OfType<Zone05RevelationGate>().Single(),all.OfType<PointOfNoReturnTrigger>().Single()})
                {
                    var so=new SerializedObject(target);var iterator=so.GetIterator();
                    while(iterator.NextVisible(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference&&iterator.name!="m_Script")
                        Require(iterator.objectReferenceValue!=null,"Critical assigned reference "+target.GetType().Name+"/"+iterator.name);
                }
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
                keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();pads=new Gamepad[]{InputSystem.AddDevice<XInputController>(),InputSystem.AddDevice<DualShockGamepad>(),InputSystem.AddDevice<Gamepad>()};
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
            actions.devices=new InputDevice[]{keyboard,mouse,pads[0],pads[1],pads[2]};
            tracker=UnityEngine.Object.FindAnyObjectByType<ClueTracker>();door=UnityEngine.Object.FindAnyObjectByType<Door>();
            journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();prompt=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            reticle=UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>();secret=UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
            path=UnityEngine.Object.FindAnyObjectByType<FalsePathReveal>();result=UnityEngine.Object.FindAnyObjectByType<LevelResultPresenter>();
            module=UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();uiAsset=module.actionsAsset;
            if(!uiSaved){oldUI=uiAsset.devices;uiSaved=true;}uiAsset.devices=new InputDevice[]{keyboard,mouse,pads[0],pads[1],pads[2]};
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
        static ObservationAlignmentPuzzle Observation=>UnityEngine.Object.FindAnyObjectByType<ObservationAlignmentPuzzle>();
        static SecretKeyDiscoveryPuzzle Discovery=>UnityEngine.Object.FindObjectsByType<SecretKeyDiscoveryPuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
        static Zone05RevelationGate Gate=>UnityEngine.Object.FindAnyObjectByType<Zone05RevelationGate>();
        static void Observe(float seconds)=>Method(typeof(ObservationAlignmentPuzzle),"Evaluate").Invoke(Observation,new object[]{seconds});
        static Vector3 AltarFeet()=>((ObservationPoint)Ref(Observation,"_point")).transform.position+Vector3.up*.28f;
        static Vector3 AimTarget()=>((Transform)Ref(Observation,"_target")).position;
        static void NativeLook(int device)
        {
            var look=reader.GetComponent<PlayerLook>();var pivot=(Transform)Ref(look,"_cameraRoot");
            camera.transform.localRotation=Quaternion.identity;pivot.localRotation=Quaternion.identity;
            typeof(PlayerLook).GetField("_yaw",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(look,160f);
            typeof(PlayerLook).GetField("_pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(look,0f);
            reader.transform.rotation=Quaternion.Euler(0,160,0);
            var direction=AimTarget()-camera.transform.position;
            float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            for(int step=0;step<100;step++)
            {
                float currentYaw=(float)typeof(PlayerLook).GetField("_yaw",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(look);
                float x=Mathf.DeltaAngle(currentYaw,yaw),y=look.Pitch-pitch;
                if(Mathf.Abs(x)<.01f&&Mathf.Abs(y)<.01f)break;
                if(device<0)InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(x,y)/.12f});
                else InputSystem.QueueStateEvent(pads[device],new GamepadState{rightStick=new Vector2(Mathf.Clamp(x/3,-1,1),Mathf.Clamp(y/3,-1,1))});
                InputSystem.Update();look.Tick(reader.Look,reader.LookMode,.02f);
            }
            InputSystem.QueueStateEvent(mouse,new MouseState());foreach(var pad in pads)InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();
            Require(Vector3.Dot(camera.transform.forward,(AimTarget()-camera.transform.position).normalized)>.999f,"Native mouse/right stick aligns production PlayerLook "+device);
        }
        static IEnumerator Validate()
        {
            yield return null;
            NeutralCheck();
            var gate=Gate;var discovery=Discovery;var content=(GameObject)Ref(gate,"_content");
            Place(new Vector3(0,.6f,68.5f),new Vector3(0,1.5f,71));Refresh();
            Require(!content.activeInHierarchy&&!discovery.CanInteract&&tracker.Count==0,"Early Zone05 visit hides key, discovery and reward");
            Require(interaction.CurrentInteractable==null&&!result.IsVisible,"No Examinar or secret representation during early visit");
            discovery.Interact();Require(!discovery.IsSolved&&!tracker.HasClue("level01.clue05"),"Cannot obtain Clue05 before Puzzle04");
            var trap=UnityEngine.Object.FindAnyObjectByType<PointOfNoReturnTrigger>(FindObjectsInactive.Include);
            Method(typeof(PointOfNoReturnTrigger),"OnTriggerEnter").Invoke(trap,new object[]{reader.GetComponent<CharacterController>()});
            Require(outcome.State==LevelOutcome.Playing&&!path.IsRevealed,"False path cannot defeat before discovery even if trigger is called");
            var ground=AltarFeet()-Vector3.up*.28f;
            Place(ground,AimTarget());Observe(1);
            Require(!Observation.IsSolved,"Correct camera from ordinary ground does not solve");
            Place(AltarFeet()+Vector3.right*2,AimTarget());Observe(1);
            Require(!Observation.IsSolved,"Outside observation zone cannot solve");
            Place(AltarFeet(),AltarFeet()+Vector3.forward*10);Observe(1);
            Require(!Observation.IsSolved,"Wrong orientation cannot solve");
            for(int device=-1;device<3;device++)
            {
                gate=Gate;content=(GameObject)Ref(gate,"_content");discovery=Discovery;
                int reveals=0,rewards=0;gate.Revealed+=()=>reveals++;tracker.ClueDiscovered+=_=>rewards++;
                Place(AltarFeet()+new Vector3(device*.15f,0,.1f),AimTarget());
                NativeLook(device);
                Observe(.2f);Require(!Observation.IsSolved,"Short alignment does not solve");
                camera.transform.rotation*=Quaternion.Euler(0,30,0);Observe(.1f);
                Require(!Observation.IsSolved,"Interrupted alignment resets hold");
                NativeLook(device);Observe(.2f);Require(!Observation.IsSolved,"Interrupted hold cannot accumulate separated intervals");
                Observe(.16f);
                Require(Observation.IsSolved&&gate.IsRevealed&&content.activeInHierarchy&&reveals==1,"Puzzle04 event reveals Zone05 exactly once "+device);
                Require(tracker.Count==1&&tracker.HasClue("level01.clue04")&&tracker.DiscoveredClues[0].Title=="La mirada","Clue04 canonical reward");
                Require(UnityEngine.Object.FindObjectsByType<PuzzleBase>().Where(p=>p is MuralFragmentsPuzzle||p is PathGuardiansPuzzle||p is EchoSequencePuzzle).All(p=>!p.IsSolved),"Puzzle01–03 are not prerequisites");
                Require(content.transform.GetComponentsInChildren<Renderer>().Any(r=>r.transform.IsChildOf(content.transform.Find("KeySymbol_VisualProxy"))),"Key proxy visible after gate");
                gate.enabled=false;gate.enabled=true;Observe(1);
                Require(reveals==1&&tracker.Count==1&&discovery.CanInteract,"Gate re-enable does not duplicate reveal and Examinar enabled");
                Place(new Vector3(0,.6f,68.5f),new Vector3(0,1.5f,71));Focus(discovery,new Vector3(0,1.5f,71));Keys(Key.E);
                Require(discovery.IsSolved&&tracker.Count==2&&tracker.HasClue("level01.clue05")&&path.IsRevealed,"Native Examinar completes Puzzle05 and opens path");
                discovery.Interact();discovery.enabled=false;discovery.enabled=true;
                foreach(var reward in discovery.transform.root.GetComponentsInChildren<PuzzleClueReward>(true))if((PuzzleBase)Ref(reward,"_puzzle")==discovery){reward.enabled=false;reward.enabled=true;}
                Require(tracker.Count==2&&rewards==2&&!discovery.CanInteract,"One Clue05 after repeated interactions and reward re-enable");
                var clue=tracker.DiscoveredClues.Single(c=>c.Id=="level01.clue05");
                Require(clue.SemanticContent==ClueSemanticContent.SecretCommand&&!clue.Description.Contains("G")&&clue.Title=="La llave","Clue05 stores semantic content rather than physical binding");
                var popup=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();var popupText=(TMP_Text)Ref(popup,"_description");
                var popupTitle=(TMP_Text)Ref(popup,"_title");
                Keys(Key.H);Require(popupText.text.Contains("[ G ]")&&popupTitle.text.Contains("DESCUBRIMIENTO")&&popupTitle.text.Contains("LA LLAVE"),"Keyboard discovery presents DESCUBRIMIENTO / LA LLAVE / G");
                Keys(Key.J);Require(journal.IsOpen&&((TMP_Text)Ref(journal,"_content")).text.Contains("LA MIRADA"),"Journal opens with discovered clues");
                string[] expected={"Y","△","Botón norte"};
                for(int pad=0;pad<3;pad++)
                {
                    Pad(pads[pad],GamepadButton.North);
                    Require(popupText.text.Contains("[ "+expected[pad]+" ]")&&((TMP_Text)Ref(journal,"_content")).text.Contains("[ "+expected[pad]+" ]")&&tracker.Count==2,"Device-aware discovery and already open Journal "+expected[pad]);
                }
                Keys(Key.H);Require(((TMP_Text)Ref(journal,"_content")).text.Contains("[ G ]"),"Open Journal switches back to keyboard");
                actions.Player.SecretCommand.ApplyBindingOverride(0,"<Keyboard>/h");
                InputSystem.Update();
                Require(journal.IsOpen && ((TMP_Text)Ref(journal,"_content")).text.Contains("[ H ]") &&
                    popupText.text.Contains("[ H ]"), "Already open Journal and visible discovery resolve live effective binding without device change");
                actions.Player.SecretCommand.RemoveAllBindingOverrides();InputSystem.Update();
                Require(((TMP_Text)Ref(journal,"_content")).text.Contains("[ G ]") && popupText.text.Contains("[ G ]") &&
                    tracker.Count == 2 && tracker.DiscoveredClues.Contains(clue), "Binding/device refresh preserves discovered clue and restores keyboard hint");
                Keys(Key.J);
                Require(!popupText.isTextOverflowing&&!popupTitle.isTextOverflowing,"Discovery labels fit their rects");
                Place(new Vector3(6,.6f,82),new Vector3(6,2.22f,89));Refresh();
                Require(interaction.CurrentInteractable==null&&outcome.State==LevelOutcome.Playing,"Final warning visual needs no interaction and remains safe");
                Place(new Vector3(6,.6f,89.8f),new Vector3(6,2.22f,96));
                var cc=reader.GetComponent<CharacterController>();var simulation=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
                try{for(int step=0;step<12&&outcome.State==LevelOutcome.Playing;step++){cc.Move(Vector3.forward*.2f);Physics.Simulate(.02f);}}
                finally{Physics.simulationMode=simulation;}
                yield return null;
                Require(outcome.State==LevelOutcome.Defeat&&((GameObject)Ref(trap=UnityEngine.Object.FindAnyObjectByType<PointOfNoReturnTrigger>(),"_closingPassage")).activeSelf,"Physical crossing after Puzzle05 closes passage and defeats");
                Require(!outcome.TriggerDefeat()&&!outcome.TriggerVictory(),"Outcome is idempotent");
                OutcomeCheck(LevelOutcome.Defeat);
                var reset=Retry(device);while(reset.MoveNext())yield return reset.Current;
            }
            var journeys=Journeys();while(journeys.MoveNext())yield return journeys.Current;
            Debug.Log("TIKAL_REDESIGN_VALIDATION_PASSED checks="+checks);
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
            Walk(0,0);Walk(0,12);Walk(29,12);Walk(29,39);Walk(0,39);Walk(0,67);Walk(0,68.5f);
            Require(!((GameObject)Ref(Gate,"_content")).activeInHierarchy&&!Discovery.CanInteract,"Physical early Zone05 visit keeps narrative content hidden");
            Walk(0,67);Walk(0,39);Walk(29,39);Walk(29,12);Walk(0,12);Walk(-5.8f,14);
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
            Walk(0,39);Walk(-5,39);
            camera.transform.LookAt(AimTarget());for(int step=0;step<18;step++)Observe(.02f);
            Require(Observation.IsSolved&&tracker.Count==4,"Physical ascent of altar solves observation and grants Clue04");
            Walk(0,39);Walk(0,67);Walk(0,68.5f);
            var discovery=Discovery;Focus(discovery,new Vector3(0,1.5f,71));Keys(Key.E);
            Require(discovery.IsSolved&&tracker.Count==5&&path.IsRevealed&&outcome.State==LevelOutcome.Playing,"Discovery grants fifth clue and leaves decision");
            reader.GetComponent<PlayerController>().enabled=true;Keys(Key.J);
            var body=(TMP_Text)Ref(journal,"_content");
            string[] titles={"EL CAMINO","EL ORIGEN","EL PRIMER CONTACTO","LA MIRADA","LA LLAVE"};
            Require(journal.IsOpen&&titles.All(t=>body.text.Contains(t))&&Enumerable.Range(0,4).All(i=>body.text.IndexOf(titles[i],StringComparison.Ordinal)<body.text.IndexOf(titles[i+1],StringComparison.Ordinal)),"Journey Journal displays five clues in order before decision");
            Keys(Key.J);
            Walk(0,68.5f);Walk(6,73);Walk(6,82);
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==5,"Journey reaches visual warning safely without sixth clue");
        }
        static IEnumerator Journeys()
        {
            var first=SolveJourney();while(first.MoveNext())yield return first.Current;
            Walk(6,74,true);Walk(0,67,true);Walk(0,39,true);Walk(29,39,true);Walk(29,12,true);Walk(0,12,true);Walk(0,2.5f,true);
            Require(outcome.State==LevelOutcome.Playing&&tracker.Count==5,"Canonical return from warning through all five zones preserves decision");
            reader.GetComponent<PlayerController>().enabled=true;camera.transform.LookAt(((Transform)Ref(secret,"_target")).position);Keys(Key.G);
            Require(outcome.State==LevelOutcome.Victory&&tracker.Count==5,"CANONICAL native G at original Entrance wins after all five puzzles");
            OutcomeCheck(LevelOutcome.Victory);
            Debug.Log("TIKAL_REDESIGN_JOURNEY_CANONICAL_PASSED: physical traversal, native E and production rotation ticks, five real puzzle rewards, visual warning backtrack, original Door secret");
            var reset=Retry(-1);while(reset.MoveNext())yield return reset.Current;
            var second=SolveJourney();while(second.MoveNext())yield return second.Current;
            Walk(6,92);
            Require(outcome.State==LevelOutcome.Defeat&&tracker.Count==5,"DEFEAT physical continuation crosses real trigger after all five puzzles");
            OutcomeCheck(LevelOutcome.Defeat);
            Debug.Log("TIKAL_REDESIGN_JOURNEY_DEFEAT_PASSED: all five puzzles, warning, actual no-return crossing, Defeat and Retry");
            reset=Retry(1);while(reset.MoveNext())yield return reset.Current;
            Walk(-6,-10);Walk(-6,-3);Walk(0,-3);Walk(0,-2.6f);
            reader.GetComponent<PlayerController>().enabled=true;camera.transform.LookAt(((Transform)Ref(secret,"_target")).position);Pad(pads[2],GamepadButton.North);
            Require(outcome.State==LevelOutcome.Victory&&tracker.Count==0&&UnityEngine.Object.FindObjectsByType<PuzzleBase>().All(p=>!p.IsSolved),"SPEEDRUN physical spawn to Entrance wins without puzzle or clue");
            OutcomeCheck(LevelOutcome.Victory);
            Debug.Log("TIKAL_REDESIGN_JOURNEY_SPEEDRUN_PASSED: physical approach, Generic buttonNorth, zero clues and unsolved puzzles");
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
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pads!=null)foreach(var pad in pads)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            routine=null;keyboard=null;pads=null;uiSaved=false;
        }
    }
}
