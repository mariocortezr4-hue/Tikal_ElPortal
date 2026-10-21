using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Cinematics;
using Tikal.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Animations;
using TMPro;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class ElderCinematicPlayValidation
    {
        const string Key="Tikal.Elder.CinematicPlay",Evidence="Temp/ElderCinematicPlay";
        [Serializable]class Setup{public SceneSetup[] scenes;}
        [Serializable]public class Sample{public double time;public Vector3 root,hips,head,map;public float receiverWeight,donorWeight,footMin,handDistance;public string camera,subtitle;}
        [Serializable]public class Report{public bool passed,enteredPlayMode,idempotent;public string scope;public string[] checks,errors;public Sample[] samples;}
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly List<Sample> samples=new List<Sample>();
        static IEnumerator routine;static double deadline;static int lastFrame,oldCapture;static bool oldBackground;static GamePersistenceService save;static bool idempotent;
        static ElderCinematicPlayValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;EditorApplication.delayCall+=Resume;}
        static void Poll(){if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/ElderPlay.request")){File.Delete("Temp/ElderPlay.request");Run();}}
        [MenuItem("Tikal/Validation/Run Elder Intro Play Tests")]
        public static void Run()
        {
            ElderCinematicAuthoring.Apply();var paths=new[]{AncientMapIntroAuthoring.ScenePath,AncientMapIntroAuthoring.TimelinePath,ElderCinematicPilot.Prefab,ElderCinematicPilot.Model+".meta"};var before=paths.Select(File.ReadAllBytes).ToArray();ElderCinematicAuthoring.Apply();idempotent=paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(before[i])).All(v=>v);
            File.WriteAllText("Temp/ElderAuthoringIdempotence.txt",idempotent.ToString());
            if(!idempotent){Directory.CreateDirectory("Temp/ElderIdempotence");for(int i=0;i<paths.Length;i++)if(!File.ReadAllBytes(paths[i]).SequenceEqual(before[i])){File.WriteAllBytes("Temp/ElderIdempotence/Before_"+i+".txt",before[i]);File.WriteAllBytes("Temp/ElderIdempotence/After_"+i+".txt",File.ReadAllBytes(paths[i]));}Debug.LogError("Elder authoring is not idempotent; inspect Temp/ElderIdempotence");return;}
            SessionState.SetBool(Key+"Idempotent",idempotent);SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup()}));EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Resume(){if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying)Start();}
        static void Mode(PlayModeStateChange mode)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode)Start();
            if(mode==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup","")).scenes);AssetDatabase.Refresh();Debug.Log("ELDER_PLAY_FINISHED");}
        }
        static void Start()
        {
            if(routine!=null)return;checks.Clear();errors.Clear();samples.Clear();idempotent=SessionState.GetBool(Key+"Idempotent",false);oldCapture=Time.captureFramerate;Time.captureFramerate=30;oldBackground=Application.runInBackground;Application.runInBackground=true;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+360;
            save=new GamePersistenceService(Path.Combine(Path.GetTempPath(),"TikalElderValidation-"+Guid.NewGuid().ToString("N")));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=InjectSave;typeof(GameFlowController).GetField("_introReplayToMenu",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);Application.logMessageReceived+=Log;routine=Tests();EditorApplication.update+=Tick;
        }
        static void InjectSave(Scene scene,LoadSceneMode mode)
        {
            foreach(var flow in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameFlowController>(true))){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}
        }
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+stack);}
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add(message);Debug.Log("ELDER_PLAY_CHECK "+message);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Elder Play validation deadline");if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;if(!routine.MoveNext())Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=InjectSave;routine=null;
            var report=new Report{passed=errors.Count==0,enteredPlayMode=Application.isPlaying,idempotent=idempotent,checks=checks.ToArray(),errors=errors.ToArray(),samples=samples.ToArray(),scope="Normal interactive Editor Play Mode, natural complete 97-second Timeline playback with existing GameFlow and isolated save. Frame observations/captures are automated, not a human artistic acceptance test."};Directory.CreateDirectory(ElderCinematicAuthoring.Reports);File.WriteAllText(ElderCinematicAuthoring.Reports+"/ElderPlayValidation.json",JsonUtility.ToJson(report,true));Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;EditorApplication.ExitPlaymode();
        }
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode,"Normal interactive Editor / no batchmode");
            EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var sequence=UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();var timeline=(TimelineAsset)director.playableAsset;var elder=ElderCinematicAuthoring.Find("ElderAnchor");var a=elder.GetComponentsInChildren<Animator>(true).Single(x=>x.avatar!=null);var dwight=ElderCinematicAuthoring.Find("ExplorerAnchor").GetComponentsInChildren<Animator>(true).Single(x=>x.avatar!=null);var map=ElderCinematicAuthoring.Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();int received=0;sequence.AncientMapReceived+=()=>received++;
            Check(a.avatar.isHuman&&a.avatar.isValid&&a.runtimeAnimatorController==null&&!a.applyRootMotion,"Elder own Humanoid / Timeline Animator / root motion false");Check(dwight.avatar==AssetDatabase.LoadAssetAtPath<GameObject>(DwightGameplayAuthoring.Prefab).GetComponentInChildren<Animator>(true).avatar&&dwight.runtimeAnimatorController==null,"Canonical Dwight visual without gameplay controller");Check(!elder.Find("ElderPlaceholder").gameObject.activeSelf,"Elder placeholder hidden");
            Directory.CreateDirectory(Evidence);double[] moments={1,25,36,46,49,54,60,65.5,67,70.8,71.2,73,78,82,88,91.5,94.5};int next=0;Vector3 anchor=elder.position;var previousPelvis=a.GetBoneTransform(HumanBodyBones.Hips).position;float maxStep=0;
            while(SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath)
            {
                double t=director.time;
                var pelvis=a.GetBoneTransform(HumanBodyBones.Hips).position;maxStep=Mathf.Max(maxStep,Vector3.Distance(previousPelvis,pelvis));previousPelvis=pelvis;
                if(next<moments.Length&&t>=moments[next])
                {
                    var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.isActiveAndEnabled);var bounds=AxelGameplayAuthoring.GeometryBounds(a.gameObject);var head=a.GetBoneTransform(HumanBodyBones.Head).position;var subtitle=(TMP_Text)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_dialogueText");
                    var sample=new Sample{time=t,root=a.transform.localPosition,hips=pelvis,head=head,map=map.position,donorWeight=constraint.GetSource(0).weight,receiverWeight=constraint.GetSource(1).weight,footMin=bounds.min.y,handDistance=Vector3.Distance(constraint.GetSource(0).sourceTransform.position,constraint.GetSource(1).sourceTransform.position),camera=camera.name,subtitle=((GameObject)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_subtitlePanel")).activeSelf?subtitle.text:string.Empty};samples.Add(sample);
                    Debug.Log("ELDER_POSE "+JsonUtility.ToJson(sample));
                    var activeCues=ElderCinematicAuthoring.Tracks(timeline).OfType<CinematicCueTrack>().SelectMany(track=>track.GetClips()).Where(c=>c.start<=t&&t<c.end&&((CinematicCueAsset)c.asset).Type==CinematicCueType.Subtitle).ToArray();
                    Check(activeCues.Length==0?string.IsNullOrEmpty(sample.subtitle):sample.subtitle==((CinematicCueAsset)activeCues.Last().asset).Text,"Visible subtitles match original cue timing @ "+t.ToString("F2"));
                    Check(Vector3.Distance(elder.position,anchor)<.0001f,"ElderAnchor remains fixed @ "+t.ToString("F2"));
                    Check(Vector2.Distance(new Vector2(head.x,head.z),new Vector2(anchor.x,anchor.z))<1.2f,"Elder remains on seat without horizontal drift @ "+t.ToString("F2"));
                    Check(pelvis.y>.65f&&pelvis.y<1.15f,"Seated pelvis height @ "+t.ToString("F2"));
                    if(camera.name=="CAM_ExplorerDialogue"){var vp=camera.WorldToViewportPoint(dwight.GetBoneTransform(HumanBodyBones.Head).position);Check(vp.z>.4f&&vp.x>.08f&&vp.x<.92f&&vp.y>.12f&&vp.y<.92f,"Dwight head visible in ExplorerDialogue");}
                    if(camera.name=="CAM_ElderDialogue"||camera.name=="CAM_FinalWarning")
                    {var viewport=camera.WorldToViewportPoint(head);Check(!map.GetComponent<Renderer>().bounds.IntersectRay(new Ray(camera.transform.position,(head-camera.transform.position).normalized),out float hitDistance)||hitDistance>Vector3.Distance(camera.transform.position,head),"Map does not occlude face @ "+t.ToString("F2"));Check(viewport.z>.4f&&viewport.x>.08f&&viewport.x<.92f&&viewport.y>.12f&&viewport.y<.92f,"Head visible in "+camera.name+" @ "+t.ToString("F2"));}
                    if(t>65&&t<80){Check(sample.handDistance<1.1f,"Handoff hands within practical sheet reach @ "+t.ToString("F2"));var vp=camera.WorldToViewportPoint(map.position);Check(camera.name=="CAM_MapHandoff"&&vp.z>.4f&&vp.x>.05f&&vp.x<.95f&&vp.y>.30f&&vp.y<.95f,"Map visible in approved handoff camera @ "+t.ToString("F2"));}
                    if(t>71&&t<83)Check(sample.receiverWeight>.99f&&sequence.AncientMapWasReceived,"Map attached to Explorer after original AncientMapReceived @ "+t.ToString("F2"));
                    ScreenCapture.CaptureScreenshot(Evidence+"/Intro_"+moments[next].ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".png");next++;
                }
                yield return null;
            }
            for(int i=0;i<5;i++)yield return null;
            Check(next==moments.Length,"Observed complete Timeline from opening through final departure");Check(received==1,"AncientMapReceived fired exactly once");Check(SceneManager.GetActiveScene().name=="TUT_ExplorerBasics"&&save.LoadProgress().IntroSeen,"Natural Intro completion persists IntroSeen and enters existing Tutorial");Check(maxStep<.12f,"No seated pelvis teleport larger than 12 cm/frame");Check(idempotent,"Authoring repeated byte-for-byte without scene/Timeline duplication");
        }
    }
}
