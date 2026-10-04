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
        static float elapsed,distance,maxVerticalStep;
        static int airborneFrames,maxAirborneFrames;
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
                ValidateEnvironment(env);
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
            }.WithButton(GamepadButton.LeftStick,sprint)
            );
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            InputSystem.Update();
            var before=movement.transform.position;
            movement.Tick(reader.Move,reader.SprintRequested,false,.02f);
            Method(typeof(Door),"Tick").Invoke(door,new object[]
            {
                .02f
            }
            );
            elapsed+=.02f;
            var delta=movement.transform.position-before;
            delta.y=0;
            distance+=delta.magnitude;
            RequireSilent(movement.transform.position.y>-.55f&&movement.transform.position.y<3f,"Player stayed within terrain elevation range");
            maxVerticalStep=Mathf.Max(maxVerticalStep,Mathf.Abs(movement.transform.position.y-before.y));
            airborneFrames=movement.IsGrounded?0:airborneFrames+1;maxAirborneFrames=Mathf.Max(maxAirborneFrames,airborneFrames);
            RequireSilent(airborneFrames<=8,"Grounded remained stable on traversable slopes");
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
                // Brake with the analog stick near a waypoint; sprint ends when processed Move reaches neutral.
                Step(new Vector2(delta.x,delta.z).normalized*Mathf.Clamp01(delta.magnitude),sprint && delta.magnitude>1.2f);
            }
            for(int i=0;i<12;i++)Step(Vector2.zero,false);
            RequireSilent(Vector2.Distance(new Vector2(x,z),new Vector2(movement.transform.position.x,movement.transform.position.z))<.5f,"Settled at waypoint "+target+" actual "+movement.transform.position);
            RequireSilent(movement.IsGrounded,"Grounded at waypoint "+target);
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
        static void ValidateEnvironment(Transform env)
        {
            foreach(var name in new[]{"Terrain","Vegetation_Blockout","Ruins_Blockout","NaturalBoundaries"})Require(env.Find(name)!=null,"Environmental hierarchy: "+name);
            var surfaces=env.Find("Terrain").GetComponentsInChildren<MeshFilter>();
            Require(surfaces.Length==11,"Eleven connected terrain surfaces");
            float min=float.MaxValue,max=float.MinValue,maxSlope=0;
            foreach(var filter in surfaces){Require(filter.GetComponent<MeshCollider>().sharedMesh==filter.sharedMesh,"Surface collider matches mesh");var mesh=filter.sharedMesh;foreach(var p in mesh.vertices){min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}var vertices=mesh.vertices;var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){var normal=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).normalized;maxSlope=Mathf.Max(maxSlope,Vector3.Angle(normal,Vector3.up));}}
            Require(maxSlope<45,"Playable terrain slope below CharacterController limit");
            Debug.Log($"LEVEL01_ENVIRONMENT terrainMin={min:F2}m terrainMax={max:F2}m maxSurfaceSlope={maxSlope:F2}deg");
            var masses=env.Find("Vegetation_Blockout").GetComponentsInChildren<MeshRenderer>();Require(masses.Length==68&&env.Find("Vegetation_Blockout").GetComponentsInChildren<Collider>().Length==0,"68 non-colliding vegetation masses");
            Require(env.Find("NaturalBoundaries").GetComponentsInChildren<MeshCollider>().Length==42,"Natural shell retains solid terrain collision");
            Require(env.Find("NaturalBoundaries").GetComponentsInChildren<MonoBehaviour>().Length==0,"No new runtime boundary behaviors");
            foreach(var renderer in env.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Boundary"||r.name=="SolidBoundary"||r.name=="ThresholdBoundary"))Require(!renderer.enabled&&renderer.GetComponent<Collider>().enabled,"Hidden safety boundary still collides");
            Physics.SyncTransforms();
            foreach(var pair in new[]{new[]{new Vector3(0,2.02f,12),new Vector3(29,1.92f,39)},new[]{new Vector3(29,2.82f,12),new Vector3(0,3.22f,39)},new[]{new Vector3(0,2.22f,67),new Vector3(29,2.82f,12)}}){var delta=pair[1]-pair[0];Require(Physics.RaycastAll(pair[0],delta.normalized,delta.magnitude).Any(hit=>hit.collider.GetComponent<Renderer>()!=null&&hit.collider.GetComponent<Renderer>().enabled),"Visible geometry blocks non-adjacent zone sightline");}
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
                var savedDistance=distance;var savedTime=elapsed;Follow(-5.8f,14);Require(movement.transform.position.y>.9f,"Ceremonial platform steps traversable without jump");Follow(0,12);distance=savedDistance;elapsed=savedTime;
                Debug.Log($"LEVEL01_METRIC Door_Zone01 distance={distance:F2}m time={elapsed:F2}s");
                float lastD=distance,lastT=elapsed;
                Follow(29,12);
                Require(true,"Zone02 accessible");
                savedDistance=distance;savedTime=elapsed;Follow(24,12);Follow(24,8);Require(movement.transform.position.y>1.9f,"Zone02 local mound uphill reachable");Follow(24,12);Follow(29,12);distance=savedDistance;elapsed=savedTime;
                Debug.Log($"LEVEL01_METRIC Zone01_Zone02 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(29,39);
                Require(true,"Zone03 accessible");
                var echo=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.EchoSequencePuzzle>();
                if(echo!=null)
                {
                    savedDistance=distance;savedTime=elapsed;
                    Follow(29.5f,37);
                    var inscription=UnityEngine.Object.FindObjectsByType<Tikal.Interaction.ReadableInscription>().Single(r=>r.Text=="Escucha. Después, devuelve el eco.");
                    Aim(inscription.GetComponentInChildren<Collider>().bounds.center);Require(ReferenceEquals(interaction.CurrentInteractable,inscription),"Echo inscription reachable by physical traversal");
                    Follow(30,39);Follow(30,41.6f);Follow(33,41.6f);
                    var source=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.EchoSource>();Aim(source.transform.position+Vector3.up*1.2f);Require(ReferenceEquals(interaction.CurrentInteractable,source),"EchoSource reachable around existing alcove divider");
                    Follow(31.5f,41.6f);Follow(31.5f,43.2f);
                    var stones=UnityEngine.Object.FindObjectsByType<Tikal.Puzzles.Level01.EchoStone>().OrderBy(s=>s.Note).ToArray();
                    Aim(stones[0].transform.position+Vector3.up*.8f);Require(ReferenceEquals(interaction.CurrentInteractable,stones[0]),"Stone A physically reachable");
                    Follow(31.5f,41.6f);Follow(33,41.6f);Follow(35,41.2f);
                    Aim(stones[2].transform.position+Vector3.up*1.2f);Require(ReferenceEquals(interaction.CurrentInteractable,stones[2]),"Stone C physically reachable");
                    Follow(34.1f,41.2f);Follow(34.1f,43.4f);Follow(34.7f,43.4f);
                    Aim(stones[1].transform.position+Vector3.up*.8f);Require(ReferenceEquals(interaction.CurrentInteractable,stones[1]),"Stone B physically reachable");
                    Follow(34.1f,43.4f);Follow(34.1f,41.2f);Follow(33,41.6f);Follow(30,41.6f);Follow(29,39);
                    camera.transform.localRotation=Quaternion.identity;distance=savedDistance;elapsed=savedTime;
                }
                Debug.Log($"LEVEL01_METRIC Zone02_Zone03 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(0,39);
                Require(true,"Zone04 accessible");
                savedDistance=distance;savedTime=elapsed;Follow(-6,39);Require(movement.transform.position.y>1.9f,"Gallery terrace local elevation reachable");Follow(0,39);distance=savedDistance;elapsed=savedTime;
                var glyph = UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.GlyphSequencePuzzle>();
                if(glyph!=null)
                {
                    savedDistance=distance;savedTime=elapsed;
                    Follow(0,37.2f);
                    foreach(var stone in UnityEngine.Object.FindObjectsByType<Tikal.Puzzles.Level01.GlyphStone>().OrderBy(s=>s.transform.position.x))
                    {
                        Follow(stone.transform.position.x,37.2f);
                        Aim(((Renderer)new SerializedObject(stone).FindProperty("_visual").objectReferenceValue).bounds.center);
                        Require(ReferenceEquals(interaction.CurrentInteractable,stone),"Glyph "+stone.SequenceValue+" reachable by physical traversal without teleportation");
                    }
                    Follow(0,37.2f);Follow(0,39);
                    var readableGlyph=UnityEngine.Object.FindObjectsByType<Tikal.Interaction.ReadableInscription>().Single(r=>r.Text=="Cada marca sigue a la anterior.");
                    Follow(2.8f,39);
                    Aim(readableGlyph.GetComponentInChildren<Collider>().bounds.center);
                    Require(ReferenceEquals(interaction.CurrentInteractable,readableGlyph),"Gallery guidance inscription physically reachable");
                    Follow(0,39);
                    camera.transform.localRotation=Quaternion.identity;distance=savedDistance;elapsed=savedTime;
                }
                Debug.Log($"LEVEL01_METRIC Zone03_Zone04 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                lastD=distance;
                lastT=elapsed;
                Follow(0,67);
                Require(true,"Zone05 accessible");
                Debug.Log($"LEVEL01_METRIC Zone04_Zone05 distance={distance-lastD:F2}m time={elapsed-lastT:F2}s");
                var revelation = UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.RevelationPuzzle>();
                if(revelation!=null)
                {
                    savedDistance=distance;savedTime=elapsed;
                    Follow(0,68.5f);
                    foreach(var stele in UnityEngine.Object.FindObjectsByType<Tikal.Puzzles.Level01.RevelationStele>().OrderBy(s=>s.transform.position.x))
                    {
                        Follow(stele.transform.position.x,68.5f);
                        Aim(stele.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Upright stele").bounds.center);
                        Require(ReferenceEquals(interaction.CurrentInteractable,stele),"Revelation "+stele.name+" physically reachable without teleportation");
                    }
                    Follow(6,68.5f);
                    var help=UnityEngine.Object.FindObjectsByType<Tikal.Interaction.ReadableInscription>().Single(r=>r.Text=="Lo que aparece al principio vuelve al final.");
                    Aim(help.GetComponentInChildren<Collider>().bounds.center);
                    Require(ReferenceEquals(interaction.CurrentInteractable,help),"Revelation help physically reachable");
                    Follow(0,68.5f);camera.transform.localRotation=Quaternion.identity;
                    distance=savedDistance;elapsed=savedTime;
                }
                Follow(0,74);
                Require(movement.transform.position.y>.7f,"Low terminal dais traversable without jump");
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
                Debug.Log($"LEVEL01_TERRAIN maxVerticalFrameStep={maxVerticalStep:F3}m maxAirborneFrames={maxAirborneFrames}");
                Require(maxVerticalStep<.35f,"No large vertical discontinuity on ramps or 0.2m steps");
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
