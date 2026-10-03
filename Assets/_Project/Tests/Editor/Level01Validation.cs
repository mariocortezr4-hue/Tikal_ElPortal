using System;
using System.Linq;
using System.Reflection;
using Tikal.Input;
using Tikal.Player;
using Tikal.Interaction;
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
    [InitializeOnLoad] public static class Level01Validation
    {
        const string Path="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity",Active="Tikal.Level01.Active",Result="Tikal.Level01.Result";
        static PlayerMovement movement;
        static PlayerInputReader reader;
        static PlayerInteraction interaction;
        static Door door;
        static Camera camera;
        static InteractionPromptPresenter presenter;
        static Keyboard keyboard;
        static Gamepad pad;
        static float elapsed,distance;
        static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
        static Level01Validation()=>EditorApplication.playModeStateChanged+=State;
        [MenuItem("Tikal/Tests/Level 01 physical round trip")] public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene(Path);
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c!=null),"No Missing Scripts");
                Require(components.OfType<PlayerController>().Count()==1&&components.OfType<Door>().Count()==1,"One Explorer and conventional Door");
                DynamicPromptValidation.ValidateScene(components);
                var root=scene.GetRootGameObjects().Single().transform;
                foreach(string group in new[]
                {
                    "Systems","Player","Environment","Lighting","BlockoutMarkers"
                }
                )Require(root.Find(group)!=null,"Hierarchy "+group);
                var env=root.Find("Environment");
                for(int i=1;i<=5;i++)
                {
                    var zone=env.Cast<Transform>().Single(t=>t.name.StartsWith("Zone0"+i));
                    foreach(string name in new[]
                    {
                        "PuzzleAnchor","ClueAnchor"
                    }
                    )Require(zone.Find(name).GetComponents<Component>().Length==1,"Transform-only "+zone.name+"/"+name);
                }
                foreach(string name in new[]
                {
                    "Zone03_EchoChamber/AudioAnchor","Zone04_GlyphGallery/GlyphWallAnchor","Zone05_Revelation/RevelationAnchor"
                }
                )Require(env.Find(name).GetComponents<Component>().Length==1,"Special anchor "+name);
                var actor=components.OfType<CharacterController>().Single();
                Require(Vector3.Distance(actor.transform.position,new Vector3(0,.08f,-10))<.01f,"Spawn 10m before entrance");
                Require(actor.transform.forward==Vector3.forward,"Spawn faces entrance");
                foreach(var ctrl in components.OfType<PlayerController>())foreach(string field in new[]
                {
                    "_inputReader","_movement","_look"
                }
                )Require(new SerializedObject(ctrl).FindProperty(field).objectReferenceValue!=null,"Controller reference "+field);
                var gate=components.OfType<Door>().Single();
                Require(new SerializedObject(gate).FindProperty("_pivot").objectReferenceValue!=null,"Door pivot assigned");
                Require(PrefabUtility.GetCorrespondingObjectFromSource(gate.gameObject)!=null,"Door variant instance");
                SessionState.SetBool(Active,true);
                SessionState.SetInt(Result,1);
                EditorApplication.EnterPlaymode();
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                if(Application.isBatchMode)EditorApplication.Exit(1);
            }
        }
        static void State(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Active,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    Validate();
                    SessionState.SetInt(Result,0);
                    Debug.Log("TIKAL_LEVEL01_VALIDATION_PASSED: real collider traversal, five zones, no jumps or teleportation, return, conventional Door, dynamic prompts, SecretCommand no effect.");
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
                EditorApplication.ExitPlaymode();
            }
            else if(state==PlayModeStateChange.EnteredEditMode)
            {
                int result=SessionState.GetInt(Result,1);
                SessionState.SetBool(Active,false);
                if(Application.isBatchMode)EditorApplication.Exit(result);
            }
        }
        static void Require(bool valid,string message)
        {
            if(!valid)throw new Exception("Level01: "+message);
            Debug.Log("LEVEL01_CHECK: "+message);
        }
        static void Refresh()
        {
            Physics.SyncTransforms();
            Method(typeof(PlayerInteraction),"LateUpdate").Invoke(interaction,null);
            Method(typeof(InteractionPromptPresenter),"LateUpdate").Invoke(presenter,null);
        }
        static void Step(Vector2 move,bool sprint)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState
            {
                leftStick=move
            }
            );
            InputSystem.QueueStateEvent(keyboard,sprint?new KeyboardState(Key.LeftShift):new KeyboardState());
            InputSystem.Update();
            var before=movement.transform.position;
            movement.Tick(reader.Move,reader.SprintHeld,false,.02f);
            Method(typeof(Door),"Tick").Invoke(door,new object[]
            {
                .02f
            }
            );
            elapsed+=.02f;
            var delta=movement.transform.position-before;
            delta.y=0;
            distance+=delta.magnitude;
            RequireSilent(movement.transform.position.y>-.1f&&movement.transform.position.y<.5f,"Player stayed on walkable floor");
            Refresh();
        }
        static void RequireSilent(bool valid,string message)
        {
            if(!valid)throw new Exception(message);
        }
        static void Follow(float x,float z,bool sprint=false)
        {
            var target=new Vector3(x,0,z);
            float start=elapsed;
            while(true)
            {
                var delta=target-movement.transform.position;
                delta.y=0;
                if(delta.magnitude<.18f)break;
                RequireSilent(elapsed-start<40,"Traversal blocked near "+movement.transform.position+" toward "+target);
                Step(new Vector2(delta.x,delta.z).normalized,sprint && delta.magnitude>1.2f);
            }
            for(int i=0;i<12;i++)Step(Vector2.zero,false);
            RequireSilent(Vector2.Distance(new Vector2(x,z),new Vector2(movement.transform.position.x,movement.transform.position.z))<.5f,"Settled at waypoint "+target+" actual "+movement.transform.position);
        }
        static void Aim(Vector3 point)
        {
            camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position);
            Refresh();
        }
        static TMP_Text Label()=> (TMP_Text)new SerializedObject(presenter).FindProperty("_label").objectReferenceValue;
        static void Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
            InputSystem.Update();
            Refresh();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            InputSystem.Update();
            Refresh();
        }
        static void Validate()
        {
            movement=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            reader=movement.GetComponent<PlayerInputReader>();
            interaction=movement.GetComponent<PlayerInteraction>();
            camera=movement.GetComponentInChildren<Camera>();
            door=UnityEngine.Object.FindAnyObjectByType<Door>();
            presenter=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();
            movement.GetComponent<PlayerController>().enabled=false;
            reader.EnableGameplay();
            var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);
            var settings=InputSystem.settings;
            var oldBackground=settings.backgroundBehavior;
            var oldEditor=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();
            pad=InputSystem.AddDevice<XInputController>();
            var ps=InputSystem.AddDevice<DualShockGamepad>();
            var oldDevices=actions.devices;
            actions.devices=new InputDevice[]
            {
                keyboard,pad,ps
            }
            ;
            try
            {
                elapsed=distance=0;
                for(int i=0;i<30;i++)Step(Vector2.zero,false);
                Require(movement.IsGrounded,"Valid grounded spawn");
                Require(interaction.CurrentInteractable==null,"No prompt outside range at spawn");
                elapsed=distance=0;
                Follow(-6,-10);
                Follow(-6,-3);
                Follow(0,-3);
                Follow(0,-2.6f);
                Aim(new Vector3(0,1.62f,0));
                Require(ReferenceEquals(interaction.CurrentInteractable,door),"Door reachable through approach");
                Debug.Log($"LEVEL01_METRIC approach_to_interact distance={distance:F2}m time={elapsed:F2}s");
                Press(Key.H);
                Require(Label().text.Contains("E")&&Label().text.Contains("Abrir"),"Keyboard E Abrir prompt");
                var original=door.State;
                Press(Key.G);
                Require(door.State==original&&Label().text.Contains("Abrir"),"G has no gameplay or prompt effect");
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North));
                InputSystem.Update();
                Refresh();
                Require(door.State==original&&Label().text.Contains("X"),"Xbox Y inert; X prompt");
                InputSystem.QueueStateEvent(pad,new GamepadState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(ps,new GamepadState().WithButton(GamepadButton.North));
                InputSystem.Update();
                Refresh();
                Require(door.State==original&&Label().text.Contains("□"),"PlayStation triangle inert; square prompt");
                InputSystem.QueueStateEvent(ps,new GamepadState());
                InputSystem.Update();
                Press(Key.E);
                Require(door.State==Door.DoorState.Opening,"Interact opens Door");
                for(int i=0;i<45;i++)Step(Vector2.zero,false);
                Require(door.State==Door.DoorState.Open&&door.InteractionText=="Cerrar","Smooth hinge opens conventional Door");
                camera.transform.localRotation=Quaternion.identity;
                Follow(0,0);
                elapsed=distance=0;
                Follow(0,12);
                Debug.Log($"LEVEL01_METRIC Door_Zone01 distance={distance:F2}m time={elapsed:F2}s");
                float lastD=distance,lastT=elapsed;
                Follow(29,12);
                Require(true,"Zone02 accessible");
                Debug.Log($"LEVEL01_METRIC Zone01_Zone02 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(29,39);
                Require(true,"Zone03 accessible");
                Debug.Log($"LEVEL01_METRIC Zone02_Zone03 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(0,39);
                Require(true,"Zone04 accessible");
                Debug.Log($"LEVEL01_METRIC Zone03_Zone04 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(0,67);
                Require(true,"Zone05 accessible");
                Debug.Log($"LEVEL01_METRIC Zone04_Zone05 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                Follow(0,74);
                Require(movement.transform.position.y>.1f,"Low terminal dais traversable without jump");
                Require(Physics.Raycast(new Vector3(0,1.5f,74),Vector3.forward,5),"Terminal chamber solid, no exit");
                Debug.Log($"LEVEL01_METRIC Door_terminal distance={distance:F2}m walking={elapsed:F2}s");
                elapsed=distance=0;
                Follow(0,67,true);
                Follow(0,39,true);
                Follow(29,39,true);
                Follow(29,12,true);
                Follow(0,12,true);
                Follow(0,0,true);
                Debug.Log($"LEVEL01_METRIC terminal_Door distance={distance:F2}m sprint={elapsed:F2}s");
                Require(elapsed>=20&&elapsed<=40,"Sprint return within 20-40 seconds");
                Follow(0,-1.5f);
                Aim(new Vector3(-2,1.62f,0));
                Require(ReferenceEquals(interaction.CurrentInteractable,door),"Physical return and reacquire original Door");
                Press(Key.H);
                Require(Label().text.Contains("Cerrar"),"Contextual close prompt on return");
                Press(Key.G);
                Require(door.State==Door.DoorState.Open,"Secret remains inert on return");
                Press(Key.E);
                for(int i=0;i<45;i++)Step(Vector2.zero,false);
                Require(door.State==Door.DoorState.Closed,"Original Door closes normally after round trip");
                camera.transform.localRotation=Quaternion.identity;
                Follow(0,-3);
                Follow(-6,-3);
                Follow(-6,-10);
                Follow(0,-10);
                Require(true,"Returned to spawn through same reversible geometry");
            }
            finally
            {
                actions.devices=oldDevices;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(pad);
                InputSystem.RemoveDevice(ps);
                settings.backgroundBehavior=oldBackground;
                settings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
    }
}
