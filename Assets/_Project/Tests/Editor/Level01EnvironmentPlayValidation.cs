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
        static bool shell=>SessionState.GetBool(Key+"Shell",false);
        static bool before=>SessionState.GetBool(Key+"Before",false);
        static bool golden=>SessionState.GetBool(Key+"Golden",false);
        static bool entrance=>SessionState.GetBool(Key+"Entrance",false);
        static bool guardianViewsOnly=>SessionState.GetBool(Key+"GuardianViewsOnly",false);
        static bool guardianVisuals=>SessionState.GetBool(Key+"GuardianVisuals",false);
        static bool zona02=>SessionState.GetBool(Key+"Zona02",false);
        static bool access=>SessionState.GetBool(Key+"Access",false);
        static bool puzzle01=>SessionState.GetBool(Key+"Puzzle01",false);
        static bool expansion=>SessionState.GetBool(Key+"Expansion",false);
        static string Folder=>shell?Zona02DenseShell.Evidence+(SessionState.GetBool(Key+"ShellViews",false)?"/FinalViews":"/Play"):guardianVisuals?Puzzle02GuardianVisualIntegration.Evidence+(guardianViewsOnly?"/FinalViews":"/Play"):zona02?(SessionState.GetBool(Key+"Zona02Mass",false)?Zona02RuinsArtPass.MassEvidence:SessionState.GetBool(Key+"Zona02Second",false)?Zona02RuinsArtPass.Evidence:Zona02JungleArtPass.Evidence)+(SessionState.GetBool(Key+"Zona02Views",false)?"/FinalViews":"/After"):access?Puzzle02AccessArtPass.Evidence+(SessionState.GetBool(Key+"AccessBefore",false)?"/Before":"/After"):puzzle01?(SessionState.GetBool(Key+"LeftPolish",false)?Puzzle01LeftPolish.Evidence:Puzzle01EnclosureArtPass.Evidence)+(SessionState.GetBool(Key+"PuzzleBefore",false)?"/Before":"/After"):expansion?EntranceExpansionArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.EntranceExpansion.Pass",0):entrance?EntranceHeroArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.EntranceHero.Pass",0):golden?GoldenSliceArtPass.Evidence+"/Pass"+SessionState.GetInt("Tikal.Golden.Pass",0):Level01EnvironmentArtPass.Evidence+(before?"/Before":"/After");
        static Level01EnvironmentPlayValidation()
        {
            EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;
            if(SessionState.GetBool(Key+"Native",false)){EnsureSave();SceneManager.sceneLoaded+=Inject;Application.logMessageReceived+=NativeLog;}
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/Zona02ShellPlay.request")||File.Exists("Temp/Zona02ShellViews.request"))
            {
                bool shellViews=File.Exists("Temp/Zona02ShellViews.request");File.Delete(shellViews?"Temp/Zona02ShellViews.request":"Temp/Zona02ShellPlay.request");SessionState.SetBool(Key+"ShellViews",shellViews);SessionState.SetBool(Key+"Shell",true);foreach(var flag in new[]{"GuardianVisuals","Zona02","Access","Puzzle01","Entrance","Golden","Expansion","Metrics"})SessionState.SetBool(Key+flag,false);Begin();return;
            }
            if(File.Exists("Temp/Puzzle02FinalVisualPlay.request")||File.Exists("Temp/Puzzle02FinalVisualViews.request"))
            {
                bool onlyViews=File.Exists("Temp/Puzzle02FinalVisualViews.request");File.Delete(onlyViews?"Temp/Puzzle02FinalVisualViews.request":"Temp/Puzzle02FinalVisualPlay.request");SessionState.SetBool(Key+"GuardianViewsOnly",onlyViews);SessionState.SetBool(Key+"GuardianVisuals",true);foreach(var flag in new[]{"Zona02","Access","Puzzle01","Entrance","Golden","Expansion","Metrics"})SessionState.SetBool(Key+flag,false);Begin();return;
            }
            if(File.Exists("Temp/Zona02MassPlay.request")||File.Exists("Temp/Zona02MassViews.request"))
            {
                bool viewsOnly=File.Exists("Temp/Zona02MassViews.request");File.Delete(viewsOnly?"Temp/Zona02MassViews.request":"Temp/Zona02MassPlay.request");SessionState.SetBool(Key+"Zona02Mass",true);File.WriteAllText(viewsOnly?"Temp/Zona02SecondViews.request":"Temp/Zona02SecondPlay.request","Zone02 mass composition local review");
            }
            if(File.Exists("Temp/Zona02SecondPlay.request")||File.Exists("Temp/Zona02SecondViews.request"))
            {
                bool viewsOnly=File.Exists("Temp/Zona02SecondViews.request");File.Delete(viewsOnly?"Temp/Zona02SecondViews.request":"Temp/Zona02SecondPlay.request");SessionState.SetBool(Key+"Zona02Second",true);File.WriteAllText(viewsOnly?"Temp/Zona02LocalViews.request":"Temp/Zona02LocalPlay.request","Local Zone02 only");
            }
            if(File.Exists("Temp/Zona02LocalPlay.request")||File.Exists("Temp/Zona02LocalViews.request")){bool viewOnly=File.Exists("Temp/Zona02LocalViews.request");File.Delete(viewOnly?"Temp/Zona02LocalViews.request":"Temp/Zona02LocalPlay.request");SessionState.SetBool(Key+"Zona02Views",viewOnly);SessionState.SetBool(Key+"Zona02",true);SessionState.SetBool(Key+"Access",false);SessionState.SetBool(Key+"Puzzle01",false);SessionState.SetBool(Key+"Entrance",false);SessionState.SetBool(Key+"Golden",false);SessionState.SetBool(Key+"Expansion",false);SessionState.SetBool(Key+"Metrics",false);Begin();return;}
            SessionState.SetBool(Key+"Zona02",false);
            var accessRequest=new[]{"Temp/Puzzle02AccessBefore.request","Temp/Puzzle02AccessPlay.request"}.FirstOrDefault(File.Exists);
            if(accessRequest!=null){File.Delete(accessRequest);SessionState.SetBool(Key+"Access",true);SessionState.SetBool(Key+"AccessBefore",accessRequest.Contains("Before"));SessionState.SetBool(Key+"Puzzle01",false);SessionState.SetBool(Key+"Entrance",false);SessionState.SetBool(Key+"Expansion",false);SessionState.SetBool(Key+"Golden",false);SessionState.SetBool(Key+"Metrics",false);Begin();return;}
            if(Directory.GetFiles("Temp","*.request").Any(f=>Path.GetFileName(f).Contains("Play")||Path.GetFileName(f).Contains("Views")||Path.GetFileName(f).Contains("Before")))SessionState.SetBool(Key+"Access",false);
            if(new[]{"Temp/EntranceExpansionPlay.request","Temp/EntranceExpansionViews.request","Temp/EntranceHeroPlay.request","Temp/EntranceHeroViews.request","Temp/GoldenSliceMetrics.request","Temp/GoldenSlicePlay.request","Temp/Level01ArtPlayBefore.request","Temp/Level01ArtPlay.request","Temp/Level01ArtSurvey.request","Temp/Level01ArtRegression.request"}.Any(File.Exists))SessionState.SetBool(Key+"Puzzle01",false);
            var puzzleRequest=new[]{"Temp/Puzzle01LeftPlay.request","Temp/Puzzle01LeftBefore.request","Temp/Puzzle01Play.request","Temp/Puzzle01BeforeViews.request"}.FirstOrDefault(File.Exists);
            if(puzzleRequest!=null){bool beforeViews=puzzleRequest.Contains("Before");File.Delete(puzzleRequest);SessionState.SetBool(Key+"LeftPolish",puzzleRequest.Contains("Left"));SessionState.SetBool(Key+"Puzzle01",true);SessionState.SetBool(Key+"PuzzleBefore",beforeViews);SessionState.SetBool(Key+"Entrance",false);SessionState.SetBool(Key+"Expansion",false);SessionState.SetBool(Key+"Golden",false);SessionState.SetBool(Key+"Metrics",false);Begin();}
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
                SessionState.SetBool(Key,false);SessionState.SetBool(Key+"Shell",false);SessionState.SetBool(Key+"Zona02Second",false);SessionState.SetBool(Key+"Zona02Mass",false);SessionState.SetBool(Key+"GuardianVisuals",false);EditorSceneManager.OpenScene(Level01EnvironmentArtPass.ScenePath);AssetDatabase.Refresh();Debug.Log("LEVEL01_ART_PLAY_FINISHED");
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
            if(access)scope="Puzzle01 east outlet, connector and Puzzle02 base access only. Native FPS/TPS central and edge-lane walking; torso capsule overlap probes; Puzzle01 original solve and single reward. No final Puzzle02 art approval or build performance claim.";
            if(zona02)scope=SessionState.GetBool(Key+"Zona02Views",false)?"Zone02 final visual captures only; physical route evidence retained separately in After/PlayValidation.json.":"Zone02 only: scene load, short native FPS/TPS access route and four local views. No puzzle interactions, clue checks, outcome or global validation.";
            if(guardianVisuals)scope=guardianViewsOnly?"Puzzle02 final visual captures only. Native interaction, rotation, clue-once and physical FPS/TPS evidence retained in Play/PlayValidation.json.":"Puzzle02 final visuals only: original three guardians, native gamepad focus/interact and coroutines, pivot stability, initial 3/0/0, solution 3/1/2, three-turn solve, single Clue02, six emission marks, local FPS/TPS access and exit. No Puzzle01, global progression or outcome tests.";
            if(shell){metrics=false;scope=(SessionState.GetBool(Key+"ShellViews",false)?"Final views only; physical FPS/TPS evidence in Play/PlayValidation.json. ":"")+"Zona02 environment shell only: five views, short native FPS/TPS walking, access to three guardians and north exit. No interaction, puzzle solve, clue, Puzzle01 or global regression.";}
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
            if(shell){yield return ShellTests();yield break;}
            if(guardianVisuals){yield return GuardianVisualTests();yield break;}
            if(zona02){yield return Zona02LocalTests();yield break;}
            if(access){yield return AccessTests();yield break;}
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
        static void RouteContact(string name)
        {
            var cc=player.GetComponent<CharacterController>();var b=cc.bounds;float radius=Mathf.Max(.05f,cc.radius-.04f);
            var hits=Physics.OverlapCapsule(new Vector3(b.center.x,b.min.y+radius+.16f,b.center.z),new Vector3(b.center.x,b.max.y-radius-.1f,b.center.z),radius,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(player.transform)).ToArray();
            Check(hits.Length==0,"No torso solid overlap "+name+(hits.Length>0?": "+string.Join(",",hits.Select(c=>c.name)):""));
            float floor=Floor(player.transform.position.x,player.transform.position.z)-.11f;Check(player.transform.position.y>=floor-.08f&&player.transform.position.y<floor+.6f,"Feet on original floor "+name);
        }
        static void GuardianFeedback(Tikal.Puzzles.Level01.PathGuardiansPuzzle p)
        {
            var f=new SerializedObject(p.GetComponent<Tikal.Puzzles.Level01.GuardiansAlignmentFeedback>());
            foreach(var field in new[]{"_individualMarks","_centralMarks"})for(int i=0;i<3;i++)
            {
                var r=(Renderer)f.FindProperty(field).GetArrayElementAtIndex(i).objectReferenceValue;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);var expected=p.IsGuardianAligned(i)?new Color(.8f,.48f,.12f):Color.black;var actual=block.GetColor("_EmissionColor");
                Check(r.enabled&&r.sharedMaterial.IsKeywordEnabled("_EMISSION")&&Vector3.Distance(new Vector3(actual.r,actual.g,actual.b),new Vector3(expected.r,expected.g,expected.b))<.001f,"Original feedback mark "+field+" "+i+" aligned="+p.IsGuardianAligned(i));
            }
        }
        static IEnumerator FocusGuardian(Tikal.Puzzles.Level01.PathGuardian g)
        {
            var interaction=player.GetComponent<PlayerInteraction>();var pos=g.transform.position+Vector3.back*1.8f;Place(new Vector3(pos.x,Floor(pos.x,pos.z),pos.z),0);yield return Frames(8);
            for(int frame=0;frame<180;frame++)
            {
                if(ReferenceEquals(interaction.CurrentInteractable,g)){Input(Vector2.zero);yield return Frames(2);Check(ReferenceEquals(interaction.CurrentInteractable,g),"Native raycast focus "+g.name+" "+camera.Mode);yield break;}
                var direction=(g.GetComponent<Collider>().bounds.center-ViewCamera.transform.position).normalized;
                float yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(ViewCamera.transform.forward,Vector3.up),Vector3.ProjectOnPlane(direction,Vector3.up),Vector3.up);
                float desiredPitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg, pitch=Mathf.DeltaAngle(ViewCamera.transform.eulerAngles.x,desiredPitch);
                float Axis(float e)=>Mathf.Abs(e)<.6f?0:Mathf.Sign(e)*Mathf.Clamp(Mathf.Abs(e)*.04f,.22f,.65f);
                Input(Vector2.zero,new Vector2(Axis(yaw),-Axis(pitch)));yield return null;
            }
            throw new InvalidOperationException("Unable to focus original guardian "+g.name+" "+camera.Mode+" at "+player.transform.position);
        }
        static IEnumerator TurnGuardian(Tikal.Puzzles.Level01.PathGuardian g)
        {
            var pivot=(Transform)new SerializedObject(g).FindProperty("_visual").objectReferenceValue;var art=pivot.Find("VisualRoot_FinalKit");var position=pivot.position;int expected=(g.CurrentOrientation+1)%4;
            Check(ReferenceEquals(player.GetComponent<PlayerInteraction>().CurrentInteractable,g),"Focused before native Interact "+g.name);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));yield return null;yield return Frames(2);
            Check(g.IsRotating,"Original coroutine rotates final art "+g.name);int frames=0;while(g.IsRotating&&frames++<40)yield return Frames(1);yield return Frames(3);
            Check(g.CurrentOrientation==expected&&!g.IsRotating,"Rotation completes +90 Y "+g.name);Check(Vector3.Distance(position,pivot.position)<.001f&&art.localPosition.sqrMagnitude<.000001f&&Quaternion.Angle(art.localRotation,Quaternion.identity)<.01f,"Centered final visual pivot without orbit "+g.name);
            Check(Quaternion.Angle(pivot.localRotation,Quaternion.Euler(0,expected*90,0))<.05f,"Exact discrete visual orientation "+g.name);
        }
        static IEnumerator GuardianVisualTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(18);Bind();CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("Puzzle02_Frontal",new Vector2(29,12),90f),("Puzzle02_Left",new Vector2(30,6.5f),20f),("Puzzle02_Right",new Vector2(36,15),220f),("Puzzle02_Wide",new Vector2(23.5f,12),90f)})
            {Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);}
            if(guardianViewsOnly)yield break;
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();CameraMode(mode);
                var p=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.PathGuardiansPuzzle>();var ps=new SerializedObject(p);var gs=Enumerable.Range(0,3).Select(i=>(Tikal.Puzzles.Level01.PathGuardian)ps.FindProperty("_guardians").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var reward=p.GetComponent<Tikal.Puzzles.Clues.PuzzleClueReward>();var tracker=(Tikal.Puzzles.Clues.ClueTracker)new SerializedObject(reward).FindProperty("_tracker").objectReferenceValue;int solves=0,clues=0;p.PuzzleSolved+=_=>solves++;tracker.ClueDiscovered+=c=>{if(c.Id=="level01.clue02")clues++;};
                Check(gs.Length==3&&gs.Select(g=>g.CurrentOrientation).SequenceEqual(new[]{3,0,0}),"Original initial [3,0,0] "+mode);Check(tracker.Count==0,"Isolated clue state "+mode);Check(gs.All(g=>g.transform.Find("Rotation pivot/VisualRoot_FinalKit")!=null),"Three final guardian visuals assigned "+mode);GuardianFeedback(p);
                var fixedParts=gs.Select(g=>g.transform.Find("Fixed plinth")).Concat(Enumerable.Range(0,3).Select(i=>((Tikal.Puzzles.Level01.DestinationMarker)ps.FindProperty("_destinations").GetArrayElementAtIndex(i).objectReferenceValue).transform)).ToDictionary(t=>t,t=>t.position);
                Place(new Vector3(19.4f,Floor(19.4f,12),12),0);yield return Frames(5);yield return WalkTo(24,12);yield return WalkTo(29,12);
                yield return FocusGuardian(gs[0]);yield return TurnGuardian(gs[0]);Check(!p.IsSolved,"Turning A away does not solve");GuardianFeedback(p);Check(p.ResetPuzzle(),"Local reset restores canonical orientation after A interaction");yield return Frames(3);
                Check(gs.Select(g=>g.CurrentOrientation).SequenceEqual(new[]{3,0,0}),"Canonical state restored before minimum-turn solve");
                yield return FocusGuardian(gs[1]);yield return TurnGuardian(gs[1]);Check(!p.IsSolved&&p.AlignedCount==2,"B gives two aligned guardians");GuardianFeedback(p);
                yield return FocusGuardian(gs[2]);yield return TurnGuardian(gs[2]);Check(!p.IsSolved,"First C turn does not solve");yield return TurnGuardian(gs[2]);GuardianFeedback(p);
                Check(p.IsSolved&&solves==1&&p.RotationCount==3&&gs.Select(g=>g.CurrentOrientation).SequenceEqual(new[]{3,1,2}),"Canonical [3,1,2] solves with MinimumTurns 3 "+mode);
                Check(tracker.Count==1&&tracker.HasClue("level01.clue02")&&clues==1,"Clue02 delivered exactly once "+mode);foreach(var g in gs)g.Interact();yield return Frames(3);Check(solves==1&&clues==1&&tracker.Count==1&&gs.All(g=>!g.CanInteract),"Solved interactions do not repeat clue or rotations "+mode);
                Check(fixedParts.All(k=>Vector3.Distance(k.Key.position,k.Value)<.001f),"Pedestals and destination markers remain fixed "+mode);Check(p.GetComponentInChildren<Light>(true).enabled,"Original solved light retained");
                Place(new Vector3(29,Floor(29,12),12),0);yield return Frames(4);yield return WalkTo(29,19);yield return WalkTo(29,22);Check(player.GameplayActive,"Local Zone02 exit accessible "+mode);
            }
        }
        static IEnumerator ShellTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(18);Bind();Check(player!=null,"Saved Level01 opens");CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("A_Frontal",new Vector2(29,12),90f),("B_Left",new Vector2(30,6.5f),20f),("C_Right",new Vector2(36,15),220f),("D_Background",new Vector2(29,14),0f)})
            {Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);}
            CameraMode(Tikal.Persistence.CameraMode.FirstPerson);Place(new Vector3(29,Floor(29,12),12),70);yield return Frames(8);
            for(int i=0;i<20;i++){Input(Vector2.zero,new Vector2(0,.7f));yield return null;}yield return Frames(15);ScreenCapture.CaptureScreenshot(Folder+"/E_Canopy.png");yield return Frames(5);
            if(SessionState.GetBool(Key+"ShellViews",false))yield break;
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                CameraMode(mode);Place(new Vector3(19.4f,Floor(19.4f,12),12),0);yield return Frames(6);
                foreach(var p in new[]{new Vector2(24,12),new Vector2(29,12),new Vector2(31,12)})yield return WalkTo(p.x,p.y);
                // Approach points adjacent to the three unchanged guardian colliders; no interaction.
                foreach(var p in new[]{new Vector2(31,9),new Vector2(31,12),new Vector2(33,12),new Vector2(29,12),new Vector2(29,16),new Vector2(29,19),new Vector2(29,22)})yield return WalkTo(p.x,p.y);
                Check(player.GameplayActive,"Guardian approaches and north exit accessible "+mode);
            }
        }
        static IEnumerator Zona02LocalTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();Check(player!=null,"Level01 loads with original Player");
            CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("01_Access",new Vector2(20,12),90f),("02_Guardians",new Vector2(28,12),115f),("03_NorthBank",new Vector2(29,14),0f),("04_SouthBank",new Vector2(27,10),180f)})
            {
                Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(20);ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(4);
            }
            if(SessionState.GetBool(Key+"Zona02Views",false))yield break;
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                CameraMode(mode);Place(new Vector3(19.4f,Floor(19.4f,12),12),0);yield return Frames(6);
                foreach(var p in new[]{new Vector2(24,12),new Vector2(29,12),new Vector2(31,12),new Vector2(29,15),new Vector2(29,19)})yield return WalkTo(p.x,p.y);
                Check(player.GameplayActive,"Local Zone02 access clear "+mode);
            }
        }
        static IEnumerator AccessTests()
        {
            EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);Bind();CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("01_Puzzle01_EastExit",new Vector2(4,12),90f),("02_Connector",new Vector2(12,12),90f),("03_Puzzle02_Arrival",new Vector2(20,12),90f),("04_Puzzle02_Base",new Vector2(26,12),110f)})
            {
                Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);views.Add(Measure(pose.Item1,camera.Mode.ToString()));ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);
            }
            if(SessionState.GetBool(Key+"AccessBefore",false))yield break;
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();CameraMode(mode);Place(new Vector3(-5.8f,Floor(-5.8f,14),14),0);yield return Frames(8);
                Check(player.GameplayActive,"Gameplay active "+mode);
                foreach(var p in new[]{new Vector2(0,12),new Vector2(5,12),new Vector2(9,12),new Vector2(14,12),new Vector2(19,12),new Vector2(24,12),new Vector2(29,12)}){yield return WalkTo(p.x,p.y);RouteContact(mode+" center "+p);}
                foreach(float z in new[]{10.55f,13.45f})
                {
                    Place(new Vector3(5,Floor(5,z),z),0);yield return Frames(5);
                    foreach(float x in new[]{8f,11,14,17,20,24,27}){yield return WalkTo(x,z);RouteContact(mode+" edge "+x+"/"+z);}
                }
                Place(new Vector3(29,Floor(29,12),12),0);yield return Frames(5);
                foreach(var p in new[]{new Vector2(24,12),new Vector2(19,12),new Vector2(9,12),new Vector2(0,12),new Vector2(-5.8f,14)})yield return WalkTo(p.x,p.y);
                var mural=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.MuralFragmentsPuzzle>();var ms=new SerializedObject(mural);var refs=ms.FindProperty("_fragments");var answers=ms.FindProperty("_solution");int solves=0;mural.PuzzleSolved+=_=>solves++;
                for(int i=0;i<4;i++)
                {
                    var f=(Tikal.Puzzles.Level01.MuralFragment)refs.GetArrayElementAtIndex(i).objectReferenceValue;var target=f.GetComponentInChildren<Collider>().bounds.center;var origin=player.GetComponent<PlayerInteraction>().InteractionOrigin.position;
                    Check(Physics.Raycast(origin,(target-origin).normalized,out var hit,Vector3.Distance(origin,target)+.05f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<Tikal.Puzzles.Level01.MuralFragment>()==f,"Mural fragment remains reachable "+i+" "+mode);
                    for(int j=0;j<4&&f.CurrentOrientation!=answers.GetArrayElementAtIndex(i).intValue;j++){f.Interact();yield return Frames(14);}
                }
                Check(mural.IsSolved&&solves==1,"Puzzle01 solves once after return "+mode);var tracker=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Clues.ClueTracker>();Check(tracker.Count==1,"Clue01 once after return "+mode);foreach(var f in mural.GetComponentsInChildren<Tikal.Puzzles.Level01.MuralFragment>())f.Interact();yield return Frames(3);Check(solves==1&&tracker.Count==1,"No repeated clue on solved interaction "+mode);
            }
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
            if(SessionState.GetBool(Key+"LeftPolish",false))
            {
                foreach(var pose in new[]{("06_Left_Enclosure",new Vector2(-2,10),285f),("07_Left_Background",new Vector2(-2,12),305f),("08_Left_FPS",new Vector2(-5.5f,11.4f),285f)})
                {
                    CameraMode(pose.Item1.Contains("FPS")?Tikal.Persistence.CameraMode.FirstPerson:Tikal.Persistence.CameraMode.ThirdPerson);
                    Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);
                    Check(player.transform.position.y>0,"Stable left view "+pose.Item1);views.Add(Measure(pose.Item1,camera.Mode.ToString()));ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);
                }
            }
            if(SessionState.GetBool(Key+"PuzzleBefore",false))yield break;
            Place(new Vector3(0,Floor(0,-10),-10),0);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/05_Entrance_Preserved.png");yield return Frames(5);
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(12);Bind();CameraMode(mode);
                Place(new Vector3(0,Floor(0,4),4),0);yield return Frames(6);
                yield return WalkTo(0,12);yield return WalkTo(-5.8f,14);
                var mural=UnityEngine.Object.FindAnyObjectByType<Tikal.Puzzles.Level01.MuralFragmentsPuzzle>();
                int solveEvents=0;mural.PuzzleSolved+=_=>solveEvents++;
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
                Check(solveEvents==1,"One original PuzzleSolved event "+mode);
                foreach(var f in mural.GetComponentsInChildren<Tikal.Puzzles.Level01.MuralFragment>())f.Interact();yield return Frames(3);Check(solveEvents==1&&tracker.Count==1,"Repeated solved-fragment interactions do not grant another clue "+mode);
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
