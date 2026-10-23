using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Editor.Art
{
    // Short visual route only. No puzzle solving or gameplay regression suite.
    [InitializeOnLoad]
    public static class Level01FinalVisualWalkthrough
    {
        const string Key="Tikal.FinalVisualWalk";
        static readonly Stack<IEnumerator> work=new Stack<IEnumerator>();
        static readonly List<string> notes=new List<string>(),errors=new List<string>();
        static PlayerController player;static PlayerCameraController rig;static Gamepad pad;static GamePersistenceService save;
        static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        static int frame,oldCapture;static bool oldRun;static double deadline;
        static string Folder=>Level01FinalVisualCleanup.Evidence+"/Play";
        static Level01FinalVisualWalkthrough(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Level01FinalWalk.request"))return;
            var request=File.ReadAllText("Temp/Level01FinalWalk.request");SessionState.SetBool(Key+"Resume",request.Contains("ResumeFalsePath"));SessionState.SetBool(Key+"ViewsOnly",request.Contains("SeamViewsOnly"));File.Delete("Temp/Level01FinalWalk.request");var current=EditorSceneManager.GetActiveScene();if(current.isDirty&&!EditorSceneManager.SaveScene(current))throw new Exception("Could not preserve saved visual scene");
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        static void Inject(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var flow in root.GetComponentsInChildren<GameFlowController>(true)){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}
                foreach(var settings in root.GetComponentsInChildren<PersistentSettingsApplier>(true)){Set(settings,"_service",save);settings.Apply(GameSettingsData.CreateDefault());}
            }
        }
        static void Mode(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                notes.Clear();errors.Clear();work.Clear();save=new GamePersistenceService(Path.GetFullPath(Folder+"/IsolatedSave"));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=Inject;
                oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;oldRun=Application.runInBackground;oldCapture=Time.captureFramerate;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;Time.captureFramerate=30;
                pad=InputSystem.AddDevice<Gamepad>();frame=-1;deadline=EditorApplication.timeSinceStartup+480;work.Push(Route());EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(Level01FinalVisualCleanup.ScenePath);Debug.Log("LEVEL01_FINAL_VISUAL_WALK_FINISHED");}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Visual route deadline");while(work.Count>0){var e=work.Peek();if(!e.MoveNext()){work.Pop();continue;}if(e.Current is IEnumerator child){work.Push(child);continue;}return;}Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;if(pad!=null)InputSystem.RemoveDevice(pad);pad=null;
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Application.runInBackground=oldRun;Time.captureFramerate=oldCapture;
            File.WriteAllLines(Folder+(SessionState.GetBool(Key+"ViewsOnly",false)?"/SeamViews.txt":"/VisualWalk.txt"),new[]{"Scope: saved real Level01; native input/Player locomotion and production FPS/TPS cameras; no puzzles solved. False Path exposed only in memory as visual prerequisite; stop before point of no return.","Passed="+(errors.Count==0)}.Concat(notes).Concat(errors));EditorApplication.ExitPlaymode();
        }
        static void Input(Vector2 move)=>InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=move});
        static IEnumerator Frames(int n){for(int i=0;i<n;i++){Input(Vector2.zero);yield return null;}}
        static float Ground(float x,float z)=>Physics.RaycastAll(new Vector3(x,25,z),Vector3.down,50,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is MeshCollider&&h.collider.transform.parent!=null&&h.collider.transform.parent.name=="Terrain").Select(h=>h.point.y).DefaultIfEmpty(.6f).Max()+.11f;
        static void Place(float x,float z,float yaw)
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(x,Ground(x,z),z),Quaternion.Euler(0,yaw,0));cc.enabled=true;var movement=player.GetComponent<PlayerMovement>();movement.ResetForRespawn();var look=player.GetComponent<PlayerLook>();look.ResetAfterRespawn(movement.EyeHeight,movement.MaximumEyeHeight);look.SynchronizePlayerYaw();Physics.SyncTransforms();rig.SnapAfterTeleport();
        }
        static void CameraMode(Tikal.Persistence.CameraMode mode)
        {var s=player.GetComponent<PersistentSettingsApplier>();var data=JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(s.CurrentSettings));data.CameraMode=mode;s.Apply(data);rig.ApplyCameraMode(mode);}
        static IEnumerator Walk(float x,float z)
        {
            double limit=EditorApplication.timeSinceStartup+25;float best=float.PositiveInfinity;int stalled=0;
            while(EditorApplication.timeSinceStartup<limit)
            {
                var d=new Vector3(x-player.transform.position.x,0,z-player.transform.position.z);if(d.magnitude<.26f){notes.Add(rig.Mode+" reached "+x+","+z+" at "+player.transform.position);yield return Frames(2);yield break;}
                if(player.transform.position.y< -1)throw new Exception("Ground lost at "+player.transform.position);
                if(d.magnitude<best-.01f){best=d.magnitude;stalled=0;}else if(++stalled>180)throw new Exception("Route obstructed "+rig.Mode+" at "+player.transform.position+" toward "+x+","+z);
                var view=player.GetComponent<PlayerInteraction>().ViewCamera;var forward=Vector3.ProjectOnPlane(rig.Mode==Tikal.Persistence.CameraMode.ThirdPerson?view.transform.forward:player.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);var dir=d.normalized;Input(new Vector2(Vector3.Dot(dir,right),Vector3.Dot(dir,forward))*Mathf.Clamp01(d.magnitude));yield return null;
            }
            throw new Exception("Route timed out "+rig.Mode+" at "+player.transform.position);
        }
        static IEnumerator Shot(string name,float x,float z,float yaw)
        {Place(x,z,yaw);yield return Frames(15);string folder=Folder+"/"+rig.Mode;Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return Frames(3);}
        static IEnumerator Route()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01FinalVisualCleanup.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(20);player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();if(player==null)throw new Exception("Player missing");rig=player.GetComponent<PlayerCameraController>();notes.Add("Scene loaded; original player and camera active.");
            if(SessionState.GetBool(Key+"ViewsOnly",false))
            {foreach(var m in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson}){CameraMode(m);yield return Shot("Final_Puzzle02",29,12,110);yield return Shot("Final_Zone03",29,35,0);yield return Shot("Final_Zone04",0,40,235);}notes.Add("Only affected cleanup views refreshed; full route evidence remains in VisualWalk.txt.");yield break;}
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                CameraMode(mode);
                if(mode!=Tikal.Persistence.CameraMode.FirstPerson||!SessionState.GetBool(Key+"Resume",false))
                {
                Place(0,-10,0);yield return Frames(10);yield return Shot("Final_Entrance",0,-10,0);
                yield return Walk(-6,-10);yield return Walk(-6,-3);yield return Walk(0,-3);
                var door=UnityEngine.Object.FindAnyObjectByType<Door>();if(door.State!=Door.DoorState.Open){door.Interact();yield return Frames(40);}yield return Walk(0,2);yield return Walk(0,8);yield return Shot("Final_Puzzle01",0,8,0);
                yield return Walk(0,12);yield return Walk(12,12);yield return Walk(20,12);yield return Walk(29,12);yield return Shot("Final_Puzzle02",29,12,110);
                yield return Walk(29,19);yield return Walk(29,27);yield return Walk(29,35);yield return Shot("Final_Zone03",29,35,0);yield return Walk(29,42);yield return Walk(24,42);yield return Walk(24,39);yield return Walk(17,39);yield return Walk(0,39);yield return Shot("Final_Zone04",0,40,235);
                yield return Walk(0,45);yield return Walk(0,53);yield return Walk(0,60);yield return Shot("Final_Zone05",0,60,0);yield return Walk(0,65);yield return Walk(6,65);yield return Walk(6,74);
                }
                else{Place(6,74,0);yield return Frames(10);notes.Add("FirstPerson Spawn-to-Zone05 route already completed in VisualWalk_Attempt01.txt; resuming only gated False Path.");}
                var reveal=UnityEngine.Object.FindAnyObjectByType<FalsePathReveal>();var fixture=new SerializedObject(reveal);((GameObject)fixture.FindProperty("_gate").objectReferenceValue).SetActive(false);((GameObject)fixture.FindProperty("_path").objectReferenceValue).SetActive(true);Physics.SyncTransforms();yield return Frames(10);
                yield return Walk(6,80);yield return Walk(6,85);yield return Shot("Final_FalsePath",6,82,0);yield return Walk(6,89);yield return Walk(6,80);notes.Add(mode+" complete visual route; no puzzle interactions; stopped before PONR.");
            }
        }
    }
}
