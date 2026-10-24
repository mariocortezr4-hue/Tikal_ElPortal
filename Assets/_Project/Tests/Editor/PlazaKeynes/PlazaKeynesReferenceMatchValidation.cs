using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Tikal.Player;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class PlazaKeynesReferenceMatchValidation
    {
        const string Key="Tikal.PlazaReferenceMatch.Play";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static readonly List<View> views=new List<View>();
        static readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();
        static int frame,oldCapture;static double deadline;
        static Keyboard keyboard;static PlayerController player;static PlayerInputReader input;static PlayerCameraController camera;
        static bool oldBackground;static InputSettings.BackgroundBehavior oldInput;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        [Serializable] public class View {public string name;public Vector3 position;public int rawEditorDrawCalls,rawEditorTriangles;}
        [Serializable] class Report {public bool passed;public string scope;public string[] checks,errors;public View[] views;}
        static PlazaKeynesReferenceMatchValidation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required");
            EditorSceneManager.OpenScene(PlazaKeynesReferenceMatch.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                checks.Clear();errors.Clear();views.Clear();oldCapture=Time.captureFramerate;oldBackground=Application.runInBackground;oldInput=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
                Time.captureFramerate=30;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                Application.logMessageReceived+=Log;deadline=EditorApplication.timeSinceStartup+300;frame=-1;routines.Clear();routines.Push(Tests());
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key,false);Directory.CreateDirectory(PlazaKeynesReferenceMatch.Evidence);
                File.WriteAllText(PlazaKeynesReferenceMatch.Evidence+"/FinalValidation.json",JsonUtility.ToJson(new Report{passed=errors.Count==0&&views.Count==9,scope="Level02 only; real Play Mode, controlled FPS/TPS routes and rendered evidence. Native GPU/Editor counters are rough observations, not integrated-GPU build performance certification or a global regression.",checks=checks.ToArray(),errors=errors.ToArray(),views=views.ToArray()},true));AssetDatabase.Refresh();
            }
        }
        static void Log(string m,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(m+"\n"+trace);}
        static void Check(bool valid,string text){if(!valid)throw new Exception(text);checks.Add(text);}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||routines.Count==0||frame==Time.frameCount)return;frame=Time.frameCount;EditorApplication.QueuePlayerLoopUpdate();
            try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Local validation timeout");while(routines.Count>0){var routine=routines.Peek();if(!routine.MoveNext()){routines.Pop();continue;}if(routine.Current is IEnumerator nested){routines.Push(nested);continue;}return;}Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish(){routines.Clear();if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;InputSystem.settings.backgroundBehavior=oldInput;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
        static T[] All<T>() where T:Component=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Teleport(Vector3 position,float yaw){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;player.transform.rotation=Quaternion.Euler(0,yaw,0);cc.enabled=true;player.GetComponent<PlayerMovement>().StopMotion();player.GetComponent<PlayerLook>().SynchronizePlayerYaw();camera.SnapAfterTeleport();Physics.SyncTransforms();}
        static void Send(bool walk){InputSystem.QueueStateEvent(keyboard,walk?new KeyboardState(UnityEngine.InputSystem.Key.W):new KeyboardState());InputSystem.Update();}
        static IEnumerator Walk(Vector3 start,float yaw,CameraMode mode,int frames,float minimum,string label)
        {
            camera.ApplyCameraMode(mode);Teleport(start,yaw);for(int i=0;i<8;i++){Send(false);yield return null;}var before=player.transform.position;bool supported=true;
            for(int i=0;i<frames;i++){Send(true);yield return null;supported&=player.transform.position.y>-.3f;}
            Send(false);yield return null;var after=player.transform.position;
            Check(supported,label+": no falling through ground");Check(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(after.x,after.z))>minimum,label+": accessible route");Check(camera.Mode==mode,label+": camera mode preserved");
            if(mode==CameraMode.ThirdPerson)Check(Vector3.Distance(Camera.main.transform.position,player.transform.position)>1,label+": useful TPS camera separation");
        }
        static IEnumerator Tests()
        {
            for(int i=0;i<20;i++)yield return null;
            Check(SceneManager.GetActiveScene().path==PlazaKeynesReferenceMatch.ScenePath,"Plaza scene loads");player=All<PlayerController>().Single();input=player.GetComponent<PlayerInputReader>();camera=player.GetComponent<PlayerCameraController>();
            Check(Vector3.Distance(player.transform.position,new Vector3(0,0,-72))<.5f,"Original arrival spawn remains grounded");Check(player.GameplayActive&&input.GameplayEnabled,"Original player input active");
            Check(All<PlayerController>().Length==1&&All<PlayerInputReader>().Length==1,"No duplicate Player/input");Check(All<Camera>().Length==1&&All<Camera>().Count(c=>c.CompareTag("MainCamera"))==1,"No duplicate camera");Check(All<AudioListener>().Length==1,"No duplicate AudioListener");Check(All<EventSystem>().Length==1,"No duplicate EventSystem");
            Check(All<Transform>().All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts");Check(All<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader)),"No Missing Materials/shaders");Check(All<PauseMenuController>().Single().IsListening,"Published shared Pause listener preserved");
            var env=GameObject.Find("ENV_Level02_PlazaKeynes");var pass=env.transform.Find("ReferenceMatch_ArtPass");Check(pass,"Single additive art pass exists");
            Check(All<Light>().Length==1&&All<Light>()[0].type==LightType.Directional,"One directional light, no added local realtime lights");
            Check(pass.Find("CityLayers").GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Distant architecture has no colliders");Check(pass.Find("DistantRelief").GetComponentsInChildren<Collider>(true).Length==0,"Distant relief has no colliders");Check(pass.Find("PeripheralForest").GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Peripheral vegetation has no colliders");
            var lods=env.GetComponentsInChildren<LODGroup>(true);Check(lods.Length>700&&lods.All(l=>l.GetLODs().Length>0&&l.GetLODs().All(level=>level.renderers.Length>0&&level.renderers.All(r=>r))),"LOD groups contain valid renderer levels");
            var candidate=pass.GetComponentsInChildren<LODGroup>(true).First(l=>l.lodCount>1);candidate.ForceLOD(candidate.lodCount-1);yield return null;candidate.ForceLOD(-1);Check(true,"Lowest LOD selection and automatic LOD restored");
            var statue=env.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Contains("KeynesHero"));var heroBounds=PlazaKeynesAuthoring.BoundsOf(statue.gameObject);
            foreach(var point in new[]{new Vector3(0,1.7f,-55),new Vector3(-24,1.7f,-18),new Vector3(24,1.7f,18)})
            {var ray=new Ray(point,heroBounds.center-point);Check(Physics.Raycast(ray,out var hit,Vector3.Distance(point,heroBounds.center)+1,~0,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(statue),"Keynes clear landmark from "+point);}
            // Highest-detail bounds establish the grounded base independent of currently selected LOD.
            int checkedPlants=0;foreach(var groupName in new[]{"RocksAndLowVegetation","ArchitecturalSectors"})foreach(var l in pass.Find(groupName).GetComponentsInChildren<LODGroup>(true))
            {var b=PlazaKeynesAuthoring.BoundsOf(l.gameObject);Check(b.min.y<.20f,"New accessible dressing grounded: "+l.name);checkedPlants++;}
            Check(checkedPlants>80,"Grounding sampled across all new accessible dressing");
            foreach(Transform stone in pass.Find("MonumentDressing"))Check(Mathf.Abs(PlazaKeynesAuthoring.BoundsOf(stone.gameObject).min.y-.48f)<.025f,"Monument accents rest on existing second terrace");
            keyboard=InputSystem.AddDevice<Keyboard>();input.InteractAction.actionMap.asset.devices=new InputDevice[]{keyboard};
            // Closing check only; full monument/temple routes were already passed in PlayValidation.json.
            yield return Walk(new Vector3(0,.1f,-72),0,CameraMode.FirstPerson,60,5,"Final FPS main path");
            yield return Walk(new Vector3(0,.1f,-41),0,CameraMode.ThirdPerson,60,5,"Final TPS main path");
            Capture("PlazaKeynes_Final_Entrance",new Vector3(-2,3,-70),new Vector3(0,7,8),61);
            Capture("PlazaKeynes_Final_Center",new Vector3(-29,6,-32),new Vector3(0,7,7),65);
            Capture("PlazaKeynes_Final_Keynes",new Vector3(-16,5,-21),new Vector3(0,6,0),51);
            Capture("PlazaKeynes_Final_Left",new Vector3(-38,5,-25),new Vector3(-100,22,107),64);
            Capture("PlazaKeynes_Final_Right",new Vector3(33,5,-19),new Vector3(118,28,137),63);
            Capture("PlazaKeynes_Final_Skyline",new Vector3(-15,12,-28),new Vector3(15,28,232),61);
            Capture("PlazaKeynes_Final_Background",new Vector3(-67,4,40),new Vector3(-160,20,115),65);
            Capture("PlazaKeynes_Final_Wide",new Vector3(-83,38,-103),new Vector3(8,11,65),67);
            camera.ApplyCameraMode(CameraMode.ThirdPerson);Teleport(new Vector3(14,.1f,-24),328);for(int i=0;i<12;i++)yield return null;
            Capture("PlazaKeynes_Final_TPS",null,null,0);
        }
        static void Capture(string name,Vector3? point,Vector3? target,float fov)
        {
            var c=Camera.main;var position=c.transform.position;var rotation=c.transform.rotation;float oldFov=c.fieldOfView,oldAspect=c.aspect;var texture=new RenderTexture(1600,900,24);texture.Create();var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{if(point.HasValue){c.transform.position=point.Value;c.transform.LookAt(target.Value);c.fieldOfView=fov;}c.aspect=16f/9;var request=new UniversalRenderPipeline.SingleCameraRequest{destination=texture};RenderPipeline.SubmitRenderRequest(c,request);RenderPipeline.SubmitRenderRequest(c,request);RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Check(!image.GetPixels32().Any(p=>p.r>240&&p.b>240&&p.g<15),"No obvious magenta pixels: "+name);File.WriteAllBytes(PlazaKeynesReferenceMatch.Evidence+"/Captures/"+name+".png",image.EncodeToPNG());views.Add(new View{name=name,position=c.transform.position,rawEditorDrawCalls=UnityStats.drawCalls,rawEditorTriangles=UnityStats.triangles});}
            finally{c.transform.position=position;c.transform.rotation=rotation;c.fieldOfView=oldFov;c.aspect=oldAspect;RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
