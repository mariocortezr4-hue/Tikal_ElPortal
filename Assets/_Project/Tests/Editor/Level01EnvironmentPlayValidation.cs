using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Level01EnvironmentPlayValidation
    {
        const string Key="Tikal.Level01Art.Play";
        [Serializable] public class View { public string zone,mode;public Vector3 player,camera;public long visibleTriangles,goldenSliceTriangles;public int instances,lodGroups,shadowCasters;public double meanFrameMs,p95FrameMs; }
        [Serializable] public class Report {public bool passed;public string scope;public string[] checks,errors;public View[] views;}
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly List<View> views=new List<View>();static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static Gamepad pad;static GamePersistenceService save;static PlayerController player;static PlayerCameraController camera;static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldEditor;static int oldCapture,lastFrame;static bool oldRun;static float oldVolume;static double deadline;
        static bool before=>SessionState.GetBool(Key+"Before",false);
        static bool golden=>SessionState.GetBool(Key+"Golden",false);
        static bool entrance=>SessionState.GetBool(Key+"Entrance",false);
        static bool puzzle01=>SessionState.GetBool(Key+"Puzzle01",false);
        static bool expansion=>SessionState.GetBool(Key+"Expansion",false);
        static string Folder=>puzzle01?Puzzle01EnclosureArtPass.Evidence+(SessionState.GetBool(Key+"PuzzleBefore",false)?"/Before":"/After"):expansion?EntranceExpansionArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.EntranceExpansion.Pass",0):entrance?EntranceHeroArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.EntranceHero.Pass",0):golden?GoldenSliceArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.Golden.Pass",0):Level01EnvironmentArtPass.Evidence+(before?"/Before":"/After");
        static Level01EnvironmentPlayValidation()
        {
            EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;
            if(SessionState.GetBool(Key+"Native",false)){EnsureSave();SceneManager.sceneLoaded+=Inject;Application.logMessageReceived+=NativeLog;}
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(new[]{"Temp/EntranceExpansionPlay.request","Temp/EntranceExpansionViews.request","Temp/EntranceHeroPlay.request","Temp/EntranceHeroViews.request","Temp/GoldenSliceMetrics.request","Temp/GoldenSlicePlay.request","Temp/Level01ArtPlayBefore.request","Temp/Level01ArtPlay.request","Temp/Level01ArtSurvey.request","Temp/Level01ArtRegression.request"}.Any(File.Exists))SessionState.SetBool(Key+"Puzzle01",false);
            if(File.Exists("Temp/Puzzle01Play.request")||File.Exists("Temp/Puzzle01BeforeViews.request")){bool beforeViews=File.Exists("Temp/Puzzle01BeforeViews.request");File.Delete(beforeViews?"Temp/Puzzle01BeforeViews.request":"Temp/Puzzle01Play.request");SessionState.SetBool(Key+"Puzzle01",true);SessionState.SetBool(Key+"PuzzleBefore",beforeViews);SessionState.SetBool(Key+"Entrance",false);SessionState.SetBool(Key+"Expansion",false);SessionState.SetBool(Key+"Golden",false);SessionState.SetBool(Key+"Metrics",false);Begin();}
            else if(File.Exists("Temp/EntranceExpansionPlay.request")){File.Delete("Temp/EntranceExpansionPlay.request");SessionState.SetBool(Key+"Entrance",true);SessionState.SetBool(Key+"Expansion",true);SessionState.SetBool(Key+"Metrics",false);Begin();}
            else if(File.Exists("Temp/EntranceExpansionViews.request")){File.Delete("Temp/EntranceExpansionViews.request");SessionState.SetBool(Key+"Entrance",true);SessionState.SetBool(Key+"Expansion",true);SessionState.SetBool(Key+"Metrics",true);Begin();}
            else if(File.Exists("Temp/EntranceHeroPlay.request")){File.Delete("Temp/EntranceHeroPlay.request");SessionState.SetBool(Key+"Entrance",true);SessionState.SetBool(Key+"Expansion",false);SessionState.SetBool(Key+"Metrics",false);Begin();}
            else if(File.Exists("Temp/EntranceHeroViews.request")){File.Delete("Temp/EntranceHeroViews.request");SessionState.SetBool(Key+"Entrance",true);SessionState.SetBool(Key+"Metrics",true);Begin();}
            else if(File.Exists("Temp/GoldenSliceMetrics.request")){File.Delete("Temp/GoldenSliceMetrics.request");SessionState.SetBool(Key+"Metrics",true);SessionState.SetBool(Key+"Golden",true);SessionState.SetBool(Key+"Before",false);Begin();}
            else if(File.Exists("Temp/GoldenSlicePlay.request")){File.Delete("Temp/GoldenSlicePlay.request");SessionState.SetBool(Key+"Metrics",false);SessionState.SetBool(Key+"Golden",true);SessionState.SetBool(Key+"Before",false);Begin();}
            else if(File.Exists("Temp/Level01ArtPlayBefore.request")){File.Delete("Temp/Level01ArtPlayBefore.request");SessionState.SetBool(Key+"Before",true);SessionState.SetBool(Key+"SurveyOnly",false);Begin();}
            else if(File.Exists("Temp/Level01ArtPlay.request")){File.Delete("Temp/Level01ArtPlay.request");SessionState.SetBool(Key+"Before",false);SessionState.SetBool(Key+"SurveyOnly",false);Begin();}
            else if(File.Exists("Temp/Level01ArtSurvey.request")){File.Delete("Temp/Level01ArtSurvey.request");SessionState.SetBool(Key+"Before",false);SessionState.SetBool(Key+"SurveyOnly",true);Begin();}
            else if(File.Exists("Temp/Level01ArtRegression.request")){File.Delete("Temp/Level01ArtRegression.request");RunNative();}
        }
        [MenuItem("Tikal/Validation/Run Level01 Environment Play Tests")]
        public static void Run()
        {SessionState.SetBool(Key+"Before",false);SessionState.SetBool(Key+"SurveyOnly",false);Begin();}
        static void Begin()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene before validation");
            Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void EnsureSave(){if(save==null)save=new GamePersistenceService(Path.Combine(Path.GetTempPath(),"TikalLevel01Art-"+Guid.NewGuid().ToString("N")));}
        static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        static void Inject(Scene scene,LoadSceneMode mode)
        {
            EnsureSave();foreach(var flow in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameFlowController>(true))){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}
            foreach(var settings in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PersistentSettingsApplier>(true))) {Set(settings,"_service",save);settings.Apply(GameSettingsData.CreateDefault());}
        }
        static void Mode(PlayModeStateChange state)
        {
            if(SessionState.GetBool(Key+"Native",false)&&state==PlayModeStateChange.EnteredEditMode)
            {
                bool passed=SessionState.GetInt("Tikal.FinalRedesign.Result",1)==0&&SessionState.GetString(Key+"NativeErrors","")==""&&!string.IsNullOrEmpty(SessionState.GetString(Key+"NativeSummary",""));
                File.WriteAllText(Level01EnvironmentArtPass.Evidence+"/GameplayRegression.json",JsonUtility.ToJson(new Report{passed=passed,scope="Existing FinalPuzzlesRedesignValidation: real physical five-puzzle journeys, warning/backtrack/victory, defeat/local checkpoint respawn and fresh speedrun. No new puzzle logic. Save service isolated by scene-load injection.",checks=new[]{SessionState.GetString(Key+"NativeSummary","")},errors=passed?Array.Empty<string>():new[]{SessionState.GetString(Key+"NativeErrors","Native suite did not complete")}},true));
                SceneManager.sceneLoaded-=Inject;Application.logMessageReceived-=NativeLog;SessionState.SetBool(Key+"Native",false);Debug.Log("LEVEL01_ART_NATIVE_FINISHED "+passed);return;
            }
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)Start();
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(Level01EnvironmentArtPass.ScenePath);AssetDatabase.Refresh();Debug.Log("LEVEL01_ART_PLAY_FINISHED");
            }
        }
        static void Start()
        {
            checks.Clear();errors.Clear();views.Clear();stack.Clear();EnsureSave();save.SaveProgress(GameProgressData.CreateDefault());
            SceneManager.sceneLoaded+=Inject;Application.logMessageReceived+=Log;oldCapture=Time.captureFramerate;Time.captureFramerate=30;oldRun=Application.runInBackground;Application.runInBackground=true;oldVolume=AudioListener.volume;
            oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad=InputSystem.AddDevice<Gamepad>();lastFrame=-1;deadline=EditorApplication.timeSinceStartup+900;stack.Push(Tests());EditorApplication.update+=Tick;
        }
        static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException(message);checks.Add(message);Debug.Log("LEVEL01_ART_PLAY_CHECK "+message);}
        static void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+trace);}
        static void NativeLog(string text,string trace,LogType type){if(text.StartsWith("TIKAL_REDESIGN_VALIDATION_PASSED"))SessionState.SetString(Key+"NativeSummary",text);if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetString(Key+"NativeErrors",SessionState.GetString(Key+"NativeErrors","")+text+"\n"+trace+"\n");}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Level01 art test deadline");while(stack.Count>0){var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}if(top.Current is IEnumerator child){stack.Push(child);continue;}return;}Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;Application.logMessageReceived-=Log;if(pad!=null)InputSystem.RemoveDevice(pad);pad=null;
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;Time.captureFramerate=oldCapture;Application.runInBackground=oldRun;AudioListener.volume=oldVolume;
            string scope=golden?"Golden Slice only: six A-F views, native FPS/TPS Entry-to-Plaza walking, Door Interact, checkpoint and SecretCommand runtime matrix. Triangle estimates use selected LOD/frustum, exclude occlusion/shadow passes and are not a build benchmark.":"Global survey: fixed physical views across Level01, native locomotion and checkpoint/secret matrix. Frustum and selected LOD estimates exclude GPU occlusion and shadow passes; Editor frame times are not build FPS.";
            bool metrics=golden&&SessionState.GetBool(Key+"Metrics",false);if(metrics)scope="Golden Slice A-F metrics only. Static-batched renderers counted by their own submesh range; selected LOD/frustum estimates exclude occlusion/shadow passes. No placement or scene changes. Functional results remain in PlayValidation.json.";
            if(entrance){metrics=SessionState.GetBool(Key+"Metrics",false);scope="Entrance only: three production camera views, FPS/TPS physical Spawn-to-Door route, Interact and secret Closed/range/camera/zero-clue matrix. No plaza or puzzle regression. Measurements are frustum/LOD estimates, not build performance.";}
            if(expansion)scope="Entrance expansion: four production camera views (frontal, left, right, further back), FPS/TPS native Spawn-to-Door route, Interact, secret entrance matrix. No plaza/puzzle regression or gameplay edits. Frustum/LOD estimates are not build performance.";
            if(puzzle01)scope="Puzzle01 only: four gameplay views, physical FPS/TPS plaza-to-mural and east-exit routes, fragment line-of-sight, original mural rotations and Clue01 reward. No global regression or build performance benchmark.";
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+(metrics?"/MetricsValidation.json":"/PlayValidation.json"),JsonUtility.ToJson(new Report{passed=errors.Count==0,checks=checks.ToArray(),errors=errors.ToArray(),views=views.ToArray(),scope=scope},true));EditorApplication.ExitPlaymode();
        }
        static void Input(Vector2 move,Vector2 look=default,bool jump=false,bool sprint=false)=>InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=move,rightStick=look}.WithButton(GamepadButton.South,jump).WithButton(GamepadButton.LeftStick,sprint));
        static IEnumerator Frames(int count){for(int i=0;i<count;i++){Input(Vector2.zero);yield return null;}}
        static void Bind(){player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();camera=player.GetComponent<PlayerCameraController>();}
        static void Place(Vector3 position,float yaw)
        {
            var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));capsule.enabled=true;var movement=player.GetComponent<PlayerMovement>();movement.ResetForRespawn();var look=player.GetComponent<PlayerLook>();look.ResetAfterRespawn(movement.EyeHeight,movement.MaximumEyeHeight);look.SynchronizePlayerYaw();Physics.SyncTransforms();camera.SnapAfterTeleport();
        }
        static void CameraMode(Tikal.Persistence.CameraMode mode)
        {var settings=player.GetComponent<PersistentSettingsApplier>();var data=JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(settings.CurrentSettings));data.CameraMode=mode;settings.Apply(data);camera.ApplyCameraMode(mode);}
        static float Floor(float x,float z)=>Physics.RaycastAll(new Vector3(x,20,z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is MeshCollider&&h.collider.transform.parent.name=="Terrain").Select(h=>h.point.y).DefaultIfEmpty(.6f).Max()+.11f;
        static Camera ViewCamera=>player.GetComponent<PlayerInteraction>().ViewCamera;
        static IEnumerator WalkTo(float x,float z)
        {
            for(int frame=0;frame<1800;frame++)
            {
                var delta=new Vector2(x-player.transform.position.x,z-player.transform.position.z);
                if(delta.magnitude<.23f){Input(Vector2.zero);yield return Frames(4);Check(player.transform.position.y>-.8f,"Physical route reaches "+x+"/"+z);yield break;}
                Input(delta.normalized*Mathf.Clamp01(delta.magnitude));yield return null;
            }
            throw new InvalidOperationException("Physical route stuck at "+player.transform.position+" toward "+x+"/"+z);
        }
        static IEnumerator Route(Tikal.Persistence.CameraMode mode)
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();CameraMode(mode);Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(10);
            yield return WalkTo(-6,-10);yield return WalkTo(-6,-3);yield return WalkTo(0,-3);yield return WalkTo(0,-2.6f);
            var door=UnityEngine.Object.FindAnyObjectByType<Door>();door.Interact();yield return Frames(35);Check(door.State==Door.DoorState.Open,"Original Door opens for physical "+mode+" route");
            foreach(var point in new[]{new Vector2(0,2),new Vector2(0,12),new Vector2(29,12),new Vector2(29,39),new Vector2(0,39),new Vector2(0,67),new Vector2(0,68.5f)})yield return WalkTo(point.x,point.y);
            Check(UnityEngine.Object.FindAnyObjectByType<LevelOutcomeController>().State==LevelOutcome.Playing,"Full real locomotion route retains Playing "+mode);
        }
        static IEnumerator Boundaries()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var probe in new[]{(new Vector2(-6,-8),Vector2.left,-8f),(new Vector2(6,-8),Vector2.right,8f),(new Vector2(-6.8f,10),Vector2.left,-9f),(new Vector2(36.5f,12),Vector2.right,39f),(new Vector2(34.5f,39),Vector2.right,37f),(new Vector2(-8.5f,41),Vector2.left,-11f),(new Vector2(-8.5f,67),Vector2.left,-11f)})
            {
                Place(new Vector3(probe.Item1.x,Floor(probe.Item1.x,probe.Item1.y),probe.Item1.y),0);yield return Frames(12);
                for(int i=0;i<90;i++){Input(probe.Item2,default,i==0,true);yield return null;}
                Input(Vector2.zero);Check(player.transform.position.y>-.8f&&(probe.Item2.x<0?player.transform.position.x>probe.Item3:player.transform.position.x<probe.Item3),"Real sprint/jump contained at "+probe.Item1);
            }
        }
        static long Triangles(Renderer renderer,MeshFilter filter)
        {
            var mesh=filter.sharedMesh;
            // Static batching shares a combined mesh. Counting mesh.triangles once per Renderer overcounts that batch.
            if(renderer.isPartOfStaticBatch&&renderer is MeshRenderer mr)
            {
                long count=0;int start=mr.subMeshStartIndex;
                for(int i=start;i<Math.Min(mesh.subMeshCount,start+renderer.sharedMaterials.Length);i++)count+=(long)mesh.GetIndexCount(i)/3;
                return count;
            }
            return mesh.triangles.Length/3;
        }
        static View Measure(string zone,string mode)
        {
            var env=GameObject.Find("Level01_SecretEntrance/Environment").transform;var cam=ViewCamera;var planes=GeometryUtility.CalculateFrustumPlanes(cam);long triangles=0;int groups=0,shadows=0;
            foreach(var group in env.GetComponentsInChildren<LODGroup>())
            {
                float distance=Vector3.Distance(cam.transform.position,group.transform.TransformPoint(group.localReferencePoint));var scale=group.transform.lossyScale;float height=group.size*Mathf.Max(scale.x,scale.y,scale.z)*QualitySettings.lodBias/(2*Mathf.Max(.01f,distance)*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad/2));var lod=group.GetLODs().FirstOrDefault(l=>height>=l.screenRelativeTransitionHeight);
                if(lod.renderers==null)continue;bool visible=false;foreach(var r in lod.renderers)if(r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)){var f=r.GetComponent<MeshFilter>();if(f!=null){triangles+=Triangles(r,f);visible=true;if(r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off)shadows++;}}if(visible)groups++;
            }
            long goldenTriangles=triangles;
            foreach(var f in env.GetComponentsInChildren<MeshFilter>()){var r=f.GetComponent<Renderer>();if(f.GetComponentInParent<LODGroup>()==null&&r!=null&&r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)){triangles+=Triangles(r,f);if(r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off)shadows++;}}
            int logs=env.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("FallenLog_")&&t.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)));
            return new View{zone=zone,mode=mode,player=player.transform.position,camera=cam.transform.position,visibleTriangles=triangles,goldenSliceTriangles=goldenTriangles,lodGroups=groups,instances=groups+logs,shadowCasters=shadows};
        }
        static IEnumerator Tests()
        {
            if(puzzle01){yield return Puzzle01Tests();yield break;}
            if(entrance){yield return EntranceTests();yield break;}
            if(golden){yield return GoldenTests();yield break;}
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();
            Check(player.GameplayActive,"Fresh Level01 gameplay active");var capsules=UnityEngine.Object.FindObjectsByType<CharacterController>();Check(capsules.Length==1,"One canonical Player capsule");
            var positions=new[]{("Entry",new Vector3(0,.11f,-10),0f),("Plaza",new Vector3(0,.51f,8),0f),("Guardians",new Vector3(29,1.31f,12),135f),("Echo",new Vector3(29,.41f,35),0f),("Observation",new Vector3(0,1.71f,40),235f),("Revelation",new Vector3(0,.71f,65),0f),("FinalPath",new Vector3(6,.71f,82),0f)};
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.ThirdPerson,Tikal.Persistence.CameraMode.FirstPerson})
            {
                CameraMode(mode);
                foreach(var pose in positions)
                {
                    if(pose.Item1=="FinalPath")GameObject.Find("Level01_SecretEntrance/Environment/Zone05_Revelation").transform.Find("FalsePath").gameObject.SetActive(true);
                    Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.z),pose.Item2.z),pose.Item3);yield return Frames(30);
                    Check(player.transform.position.y>-1,"Stable ground "+mode+"/"+pose.Item1);
                    var sample=Measure(pose.Item1,mode.ToString());var times=new List<double>();double last=Time.realtimeSinceStartupAsDouble;
                    for(int i=0;i<60;i++){Input(Vector2.zero);yield return null;double now=Time.realtimeSinceStartupAsDouble;times.Add((now-last)*1000);last=now;}
                    times.Sort();sample.meanFrameMs=times.Average();sample.p95FrameMs=times[(int)((times.Count-1)*.95)];views.Add(sample);ScreenCapture.CaptureScreenshot(Folder+"/"+mode+"_"+pose.Item1+".png");yield return Frames(6);
                    if(pose.Item1=="FinalPath")GameObject.Find("Level01_SecretEntrance/Environment/Zone05_Revelation").transform.Find("FalsePath").gameObject.SetActive(false);
                }
            }
            if(before)yield break;
            CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(15);
            float previous=ViewCamera.transform.eulerAngles.y,orbit=0;
            for(int i=0;i<360&&orbit<360;i++){Input(Vector2.zero,new Vector2(.7f,0));yield return null;float yaw=ViewCamera.transform.eulerAngles.y;orbit+=Mathf.DeltaAngle(previous,yaw);previous=yaw;if(i%40==0){ScreenCapture.CaptureScreenshot(Folder+"/Orbit_"+i+".png");yield return null;}}
            Input(Vector2.zero);Check(orbit>=359,"Real TPS input completes full orbit at spawn");
            if(!SessionState.GetBool(Key+"SurveyOnly",false)){yield return Route(Tikal.Persistence.CameraMode.ThirdPerson);yield return Route(Tikal.Persistence.CameraMode.FirstPerson);yield return Boundaries();}
            foreach(string id in new[]{CharacterIds.Default,CharacterIds.Character02,CharacterIds.Character03})
            {
                player.GetComponent<CharacterVisualSpawner>().Apply(id);Place(new Vector3(0,Floor(0,8),8),0);CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);yield return Frames(12);
                Check(player.GetComponent<CharacterVisualSpawner>().CurrentVisual.activeInHierarchy,"TPS visual "+id);ScreenCapture.CaptureScreenshot(Folder+"/Character_"+id+".png");yield return null;
                var start=player.transform.position;for(int i=0;i<20;i++){Input(Vector2.up);yield return null;}Input(Vector2.zero);Check(Vector3.Distance(start,player.transform.position)>1,"Real movement clear "+id);
                CameraMode(Tikal.Persistence.CameraMode.FirstPerson);yield return Frames(8);Check(!player.GetComponent<CharacterVisualSpawner>().CurrentVisual.activeInHierarchy,"FPS hides body "+id);
            }
            // Fresh fixture for existing native validators; survey activation is never used as gameplay approval.
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();
            Level01CheckpointValidation.Run();yield return Frames(10);
            while((bool)typeof(Level01CheckpointValidation).GetField("_running",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null))yield return Frames(1);
            Check(errors.Count==0,"Existing checkpoint runtime matrix reports no errors");
            SecretEntranceValidation.Run();yield return Frames(3);Check(errors.Count==0,"Existing Door closed/open/outside and FPS/TPS secret matrix reports no errors");
        }
        static IEnumerator Puzzle01Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();
            Check(player.GameplayActive,"Puzzle01 gameplay active");Check(UnityEngine.Object.FindObjectsByType<CharacterController>().Length==1,"Original Player only");
            CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("01_Mural_Frontal",new Vector2(-6,11.4f),0f),("02_Mural_Left",new Vector2(-7.2f,11),16f),("03_Mural_Right",new Vector2(-1,12),300f),("04_Mural_Wide",new Vector2(0,6),327f)})
            {
                Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);
                Check(player.transform.position.y>0,"Stable ground "+pose.Item1);views.Add(Measure(pose.Item1,camera.Mode.ToString()));ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);
            }
            if(SessionState.GetBool(Key+"PuzzleBefore",false))yield break;
            Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/05_Entrance_Preserved.png");yield return Frames(5);
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();CameraMode(mode);
                Place(new Vector3(0,Floor(0,4),4),0);yield return Frames(6);
                yield return WalkTo(0,12);yield return WalkTo(-5.8f,14);
                var mural=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.MuralFragmentsPuzzle>();
                var ms=new SerializedObject(mural);var fragments=ms.FindProperty("_fragments");var solution=ms.FindProperty("_solution");
                var interaction=player.GetComponent<PlayerInteraction>();
                for(int i=0;i<4;i++)
                {
                    var fragment=(Tikal.Puzzles.Level01.MuralFragment)fragments.GetArrayElementAtIndex(i).objectReferenceValue;
                    var center=fragment.GetComponentInChildren<Collider>().bounds.center;
                    var delta=center-interaction.InteractionOrigin.position;
                    Check(Physics.Raycast(interaction.InteractionOrigin.position,delta.normalized,out var hit,delta.magnitude+.05f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<Tikal.Puzzles.Level01.MuralFragment>()==fragment,"Unobstructed fragment reach "+i+" "+mode);
                    for(int turn=0;fragment.CurrentOrientation!=solution.GetArrayElementAtIndex(i).intValue&&turn<4;turn++)
                    {
                        Check(fragment.CanInteract,"Original fragment interactable "+i+" "+mode);fragment.Interact();yield return Frames(14);
                    }
                    Check(fragment.CurrentOrientation==solution.GetArrayElementAtIndex(i).intValue,"Original fragment rotation completes "+i+" "+mode);
                }
                Check(mural.IsSolved,"Original mural solves "+mode);
                var tracker=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Clues.ClueTracker>();Check(tracker.Count==1,"Clue01 rewarded exactly once "+mode);
                // The discovery panel dismisses automatically after six realtime seconds.
                double until=EditorApplication.timeSinceStartup+6.6;while(EditorApplication.timeSinceStartup<until)yield return Frames(1);
                yield return WalkTo(0,12);yield return WalkTo(11,12);Check(player.GameplayActive,"Eastern route clear "+mode);
            }
        }
        static IEnumerator EntranceTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();
            Check(player.GameplayActive,"Entrance gameplay active");Check(UnityEngine.Object.FindObjectsByType<CharacterController>().Length==1,"Original Player capsule only");
            var shots=expansion?new[]{("01_Frontal_ToDoor",new Vector2(0,-10),0f),("02_Left_Jungle",new Vector2(-5.5f,-10),290f),("03_Right_Jungle",new Vector2(3,-10),68f),("04_Rear_DeepView",new Vector2(0,-14),0f)}:new[]{("01_Spawn_ToDoor",new Vector2(0,-10),0f),("02_Path_Midpoint",new Vector2(-6,-4),48f),("03_Door_Approach",new Vector2(0,-2.6f),0f)};
            CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var shot in shots)
            {
                if(expansion)CameraMode(shot.Item1=="04_Rear_DeepView"?Tikal.Persistence.CameraMode.FirstPerson:Tikal.Persistence.CameraMode.ThirdPerson);
                Place(new Vector3(shot.Item2.x,Floor(shot.Item2.x,shot.Item2.y),shot.Item2.y),shot.Item3);yield return Frames(24);
                Check(player.transform.position.y>-.8f,"Stable ground "+shot.Item1);views.Add(Measure(shot.Item1,camera.Mode.ToString()));
                ScreenCapture.CaptureScreenshot(Folder+"/"+shot.Item1+".png");yield return Frames(5);
            }
            if(SessionState.GetBool(Key+"Metrics",false))yield break;
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();CameraMode(mode);Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(5);
                yield return WalkTo(-6,-10);yield return WalkTo(-6,-3);yield return WalkTo(0,-3);yield return WalkTo(0,-2.6f);
                var door=UnityEngine.Object.FindAnyObjectByType<Door>();Check(door.State==Door.DoorState.Closed,"Main Door Closed before Interact "+mode);door.Interact();yield return Frames(35);Check(door.State==Door.DoorState.Open,"Main Door opens "+mode);
                Check(player.GameplayActive,"Spawn-to-Door locomotion clear "+mode);
            }
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();SecretEntranceValidation.Run();yield return Frames(3);
            Check(errors.Count==0,"SecretCommand native Closed/Open/range/FPS/TPS/zero-clue victory remains valid");
        }
        static IEnumerator GoldenTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(18);Bind();
            Check(player.GameplayActive,"Golden Slice fresh gameplay active");
            Check(UnityEngine.Object.FindObjectsByType<CharacterController>().Length==1,"One original Player capsule");
            var shots=new[]{("A_Spawn_MainDoor",new Vector2(0,-10),0f,Tikal.Persistence.CameraMode.ThirdPerson),
                ("B_Approach",new Vector2(-5.6f,-3.5f),40f,Tikal.Persistence.CameraMode.ThirdPerson),
                ("C_PlazaArrival",new Vector2(0,4),0f,Tikal.Persistence.CameraMode.ThirdPerson),
                ("D_PlazaCenter",new Vector2(0,12),55f,Tikal.Persistence.CameraMode.ThirdPerson),
                ("E_TPS_Lateral",new Vector2(0,8),270f,Tikal.Persistence.CameraMode.ThirdPerson),
                ("F_FPS_Spawn",new Vector2(0,-10),0f,Tikal.Persistence.CameraMode.FirstPerson)};
            foreach(var pose in shots)
            {
                CameraMode(pose.Item4);Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(25);
                Check(player.transform.position.y>-.8f,"Stable grounded "+pose.Item1);Check(camera.Mode==pose.Item4,"Actual camera mode "+pose.Item1);
                var sample=Measure(pose.Item1,pose.Item4.ToString());views.Add(sample);
                ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);
            }
            if(SessionState.GetBool(Key+"Metrics",false))yield break;
            // Directed physical route, deliberately ends in the first plaza. No later puzzle routes.
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.ThirdPerson,Tikal.Persistence.CameraMode.FirstPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();CameraMode(mode);Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(6);
                yield return WalkTo(-6,-10);yield return WalkTo(-6,-3);yield return WalkTo(0,-3);yield return WalkTo(0,-2.6f);
                var door=UnityEngine.Object.FindAnyObjectByType<Door>();Check(door.State==Door.DoorState.Closed,"Main Door initially Closed "+mode);door.Interact();yield return Frames(35);Check(door.State==Door.DoorState.Open,"Interact opens Main Door "+mode);
                yield return WalkTo(0,4);yield return WalkTo(0,12);Check(UnityEngine.Object.FindAnyObjectByType<LevelOutcomeController>().State==LevelOutcome.Playing,"Entry to Plaza real locomotion "+mode);
            }
            // Orbit uses actual input with the original TPS controller; no permanent camera changes.
            Place(new Vector3(0,Floor(0,8),8),0);CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);yield return Frames(8);
            for(int i=0;i<110;i++){Input(Vector2.zero,new Vector2(.7f,0));yield return null;}Input(Vector2.zero);Check(camera.Mode==Tikal.Persistence.CameraMode.ThirdPerson,"TPS orbit remains functional");
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();
            Level01CheckpointValidation.Run();yield return Frames(8);while((bool)typeof(Level01CheckpointValidation).GetField("_running",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null))yield return Frames(1);Check(errors.Count==0,"Existing checkpoint runtime validation passed");
            SecretEntranceValidation.Run();yield return Frames(3);Check(errors.Count==0,"SecretCommand Closed/Open/range/FPS/TPS/zero-clue victory passed");
        }
        [MenuItem("Tikal/Validation/Run Level01 Full Gameplay Regression")]
        public static void RunNative()
        {
            Directory.CreateDirectory(Level01EnvironmentArtPass.Evidence);SessionState.SetString(Key+"NativeSummary","");SessionState.SetString(Key+"NativeErrors","");SessionState.SetBool(Key+"Native",true);EnsureSave();SceneManager.sceneLoaded-=Inject;SceneManager.sceneLoaded+=Inject;Application.logMessageReceived-=NativeLog;Application.logMessageReceived+=NativeLog;FinalPuzzlesRedesignValidation.Run();
        }
    }
}
