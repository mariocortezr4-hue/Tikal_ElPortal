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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class IntroFinalPlayValidation
    {
        const string Key="Tikal.IntroFinal.Play",Evidence="../_IntroFinalPolish_20261023/Play";
        [Serializable]class Setup{public SceneSetup[] scenes;}
        [Serializable]public class Sample{public double time;public Vector3 root,hips,head,map;public float receiverWeight,donorWeight,footMin,handDistance;public string camera,subtitle;}
        [Serializable]public class Report{public bool passed,enteredPlayMode,idempotent;public double firstTextVisible,firstWalkDisplacement;public string scope;public string[] checks,errors;public Sample[] samples;}
        static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly List<Sample> samples=new List<Sample>();
        static double firstText=-1,firstWalk=-1;static Keyboard keyboard; static float oldListener; static IEnumerator routine;static double deadline;static int lastFrame,oldCapture;static bool oldBackground;static GamePersistenceService save;static bool idempotent;
        static IntroFinalPlayValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;EditorApplication.delayCall+=Resume;}
        static void Poll(){if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/IntroFinalBrandPreview.request")){File.Delete("Temp/IntroFinalBrandPreview.request");SessionState.SetBool(Key+"Preview",true);Run();return;}if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/IntroFinalPlay.request")){File.Delete("Temp/IntroFinalPlay.request");Run();}}
        [MenuItem("Tikal/Validation/Run Intro Final Polish Play Tests")]
        public static void Run()
        {
            IntroCinematicFinalPolish.Apply();var paths=new[]{AncientMapIntroAuthoring.ScenePath,AncientMapIntroAuthoring.TimelinePath,ElderCinematicPilot.Prefab,ElderCinematicPilot.Model+".meta"};var before=paths.Select(File.ReadAllBytes).ToArray();IntroCinematicFinalPolish.Apply();idempotent=paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(before[i])).All(v=>v);
            File.WriteAllText("Temp/IntroPolishIdempotence.txt",idempotent.ToString());
            if(!idempotent){Directory.CreateDirectory("Temp/IntroPolishIdempotence");for(int i=0;i<paths.Length;i++)if(!File.ReadAllBytes(paths[i]).SequenceEqual(before[i])){File.WriteAllBytes("Temp/IntroPolishIdempotence/Before_"+i+".txt",before[i]);File.WriteAllBytes("Temp/IntroPolishIdempotence/After_"+i+".txt",File.ReadAllBytes(paths[i]));}Debug.LogError("Intro polish authoring is not idempotent; inspect Temp/IntroPolishIdempotence");return;}
            SessionState.SetBool(Key+"Idempotent",idempotent);SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup()}));EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Resume(){if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying)Start();}
        static void Mode(PlayModeStateChange mode)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode)Start();
            if(mode==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup","")).scenes);AssetDatabase.Refresh();Debug.Log("INTRO_FINAL_PLAY_FINISHED");}
        }
        static void Start()
        {
            if(routine!=null)return;checks.Clear();errors.Clear();samples.Clear();idempotent=SessionState.GetBool(Key+"Idempotent",false);oldListener=AudioListener.volume;oldCapture=Time.captureFramerate;Time.captureFramerate=30;oldBackground=Application.runInBackground;Application.runInBackground=true;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+360;
            save=new GamePersistenceService(Path.Combine(Path.GetTempPath(),"TikalIntroPolishValidation-"+Guid.NewGuid().ToString("N")));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=InjectSave;typeof(GameFlowController).GetField("_introReplayToMenu",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);Application.logMessageReceived+=Log;routine=Tests();EditorApplication.update+=Tick;
        }
        static void InjectSave(Scene scene,LoadSceneMode mode)
        {
            foreach(var flow in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameFlowController>(true))){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}
        }
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+stack);}
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add(message);Debug.Log("INTRO_POLISH_PLAY_CHECK "+message);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Intro polish Play validation deadline");if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;if(!routine.MoveNext())Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=InjectSave;routine=null;
            var report=new Report{firstTextVisible=firstText,firstWalkDisplacement=firstWalk,passed=errors.Count==0,enteredPlayMode=Application.isPlaying,idempotent=idempotent,checks=checks.ToArray(),errors=errors.ToArray(),samples=samples.ToArray(),scope=SessionState.GetBool(Key+"Preview",false)?"Normal interactive Play Mode: final title framing snapshot only; isolated save.":"Normal interactive Play Mode: complete 85.5-second Intro, real audio output, existing GameFlow and native keyboard skip; isolated save. No subjective human audio/art approval claimed."};Directory.CreateDirectory(IntroCinematicFinalPolish.Evidence);File.WriteAllText(IntroCinematicFinalPolish.Evidence+(SessionState.GetBool(Key+"Preview",false)?"/BrandPreviewValidation.json":"/PlayValidation.json"),JsonUtility.ToJson(report,true));if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}AudioListener.volume=oldListener;Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;SessionState.SetBool(Key+"Preview",false);EditorApplication.ExitPlaymode();
        }
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode,"Normal interactive Editor / no batchmode");
            EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var sequence=UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();var timeline=(TimelineAsset)director.playableAsset;var elder=ElderCinematicAuthoring.Find("ElderAnchor");var a=elder.GetComponentsInChildren<Animator>(true).Single(x=>x.avatar!=null);var dwight=ElderCinematicAuthoring.Find("ExplorerAnchor").GetComponentsInChildren<Animator>(true).Single(x=>x.avatar!=null);var map=ElderCinematicAuthoring.Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();int received=0;sequence.AncientMapReceived+=()=>received++;
            if(SessionState.GetBool(Key+"Preview",false))
            {
                director.time=84.5;director.Evaluate();yield return null;yield return null;
                var logo=ElderCinematicAuthoring.Find("TitleCard").Find("Intro final brand").GetComponent<UnityEngine.UI.Image>();
                Check(logo.gameObject.activeInHierarchy&&logo.rectTransform.rect.width>800&&logo.rectTransform.rect.height>300,"Final brand has cinematic readable framing");
                Check(((TMP_Text)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_titleText")).text==IntroCinematicFinalPolish.Title,"Final title exact after visual resize");
                ScreenCapture.CaptureScreenshot(IntroCinematicFinalPolish.Evidence+"/Intro_FinalBrand.png");yield return null;yield return null;yield break;
            }
            Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts in Intro");
            Check(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null)),"No Missing Materials in Intro");
            Check(a.avatar.isHuman&&a.avatar.isValid&&a.runtimeAnimatorController==null&&!a.applyRootMotion,"Elder own Humanoid / Timeline Animator / root motion false");Check(dwight.avatar==AssetDatabase.LoadAssetAtPath<GameObject>(DwightGameplayAuthoring.Prefab).GetComponentInChildren<Animator>(true).avatar&&dwight.runtimeAnimatorController==null,"Canonical Dwight visual without gameplay controller");Check(!elder.Find("ElderPlaceholder").gameObject.activeSelf,"Elder placeholder hidden");
            var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var music=mix.GetComponent<AudioSource>();
            var originalSettings=new GamePersistenceService().LoadSettings();
            var probe=GameSettingsData.CreateDefault();probe.MasterVolume=.4f;probe.MusicVolume=.25f;mix.ApplySettings(probe);yield return null;
            Check(Mathf.Abs(AudioListener.volume-.4f)<.001f,"Master setting uses existing AudioListener convention");
            Check(Mathf.Abs(music.volume-.25f*mix.MixAt(director.time))<.001f,"Music setting multiplied by envelope");
            probe.MusicVolume=0;mix.ApplySettings(probe);yield return null;Check(music.volume==0,"Music mute honored without persistence writes");
            Check(mix.MixAt(10)<=.0061f&&mix.MixAt(40)<=.0061f,"Deep narration/dialogue duck at 0.006");
            Check(mix.MixAt(28)>.09f&&mix.MixAt(84)>.09f,"Low ambient recovery between dialogue sections and after final warning");
            var renderers=map.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();Check(renderers.Length==1&&renderers.All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.name=="Universal Render Pipeline/Lit")),"Actual parchment visual with valid URP material");
            Check(map.Find("Ancient map final visual")!=null&&!map.GetComponent<MeshRenderer>().enabled,"Final map replaces visible proxy without replacing carrier");
            mix.ApplySettings(originalSettings);bool heardSignal=false,lateSignal=false,brandObserved=false;
            Directory.CreateDirectory(Evidence);double[] moments={.6,3,5.5,14,22.6,24.5,34.5,42.5,54,59.3,59.7,61.5,66.5,70.5,76.5,80,83,84.6};int next=0;Vector3 anchor=elder.position;var previousPelvis=a.GetBoneTransform(HumanBodyBones.Hips).position;float maxStep=0;
            while(SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath)
            {
                if(director.time>2&&director.time<12&&AudioListener.volume>0&&music.volume>0){var signal=new float[1024];music.GetOutputData(signal,0);if(signal.Any(v=>Mathf.Abs(v)>.00001f))heardSignal=true;}
                if(director.time>63&&director.time<84&&music.volume>0){var signal=new float[1024];music.GetOutputData(signal,0);if(signal.Any(v=>Mathf.Abs(v)>.000001f))lateSignal=true;}
                var titleCard=ElderCinematicAuthoring.Find("TitleCard");if(titleCard.gameObject.activeInHierarchy){brandObserved=true;Check(((TMP_Text)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_titleText")).text==IntroCinematicFinalPolish.Title,"Final brand cue is TIKAL EL EXPLORADOR");}
                var datePanel=ElderCinematicAuthoring.Find("DateCard");
                if(firstText<0&&datePanel.gameObject.activeInHierarchy&&datePanel.GetComponent<CanvasGroup>().alpha>.001f)firstText=director.time;
                var explorerPosition=ElderCinematicAuthoring.Find("ExplorerAnchor").position;
                if(firstWalk<0&&Vector3.Distance(explorerPosition,new Vector3(0,0,-5))>.01f)firstWalk=director.time;
                double actual=director.time; double t=actual>=23.5?actual+11.5:actual+1;
                var pelvis=a.GetBoneTransform(HumanBodyBones.Hips).position;maxStep=Mathf.Max(maxStep,Vector3.Distance(previousPelvis,pelvis));previousPelvis=pelvis;
                if(next<moments.Length&&actual>=moments[next])
                {
                    var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.isActiveAndEnabled);var bounds=AxelGameplayAuthoring.GeometryBounds(a.gameObject);var head=a.GetBoneTransform(HumanBodyBones.Head).position;var subtitle=(TMP_Text)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_dialogueText");
                    var sample=new Sample{time=actual,root=a.transform.localPosition,hips=pelvis,head=head,map=map.position,donorWeight=constraint.GetSource(0).weight,receiverWeight=constraint.GetSource(1).weight,footMin=bounds.min.y,handDistance=Vector3.Distance(constraint.GetSource(0).sourceTransform.position,constraint.GetSource(1).sourceTransform.position),camera=camera.name,subtitle=((GameObject)Field(UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>(),"_subtitlePanel")).activeSelf?subtitle.text:string.Empty};samples.Add(sample);
                    Check(Mathf.Abs(music.volume-originalSettings.MusicVolume*mix.MixAt(actual))<.015f,"Music follows settings and cinematic envelope @ "+actual.ToString("F2"));
                    Debug.Log("INTRO_POLISH_POSE "+JsonUtility.ToJson(sample));
                    var activeCues=ElderCinematicAuthoring.Tracks(timeline).OfType<CinematicCueTrack>().SelectMany(track=>track.GetClips()).Where(c=>c.start<=actual&&actual<c.end&&((CinematicCueAsset)c.asset).Type==CinematicCueType.Subtitle).ToArray();
                    Check(activeCues.Length==0?string.IsNullOrEmpty(sample.subtitle):sample.subtitle==((CinematicCueAsset)activeCues.Last().asset).Text,"Visible subtitles match original cue timing @ "+t.ToString("F2"));
                    Check(Vector3.Distance(elder.position,anchor)<.0001f,"ElderAnchor remains fixed @ "+t.ToString("F2"));
                    Check(Vector2.Distance(new Vector2(head.x,head.z),new Vector2(anchor.x,anchor.z))<1.2f,"Elder remains on seat without horizontal drift @ "+t.ToString("F2"));
                    Check(pelvis.y>.65f&&pelvis.y<1.15f,"Seated pelvis height @ "+t.ToString("F2"));
                    if(camera.name=="CAM_ExplorerDialogue"){var vp=camera.WorldToViewportPoint(dwight.GetBoneTransform(HumanBodyBones.Head).position);Check(vp.z>.4f&&vp.x>.08f&&vp.x<.92f&&vp.y>.12f&&vp.y<.92f,"Dwight head visible in ExplorerDialogue");}
                    if(camera.name=="CAM_ElderDialogue"||camera.name=="CAM_FinalWarning")
                    {var viewport=camera.WorldToViewportPoint(head);Check(!map.GetComponentsInChildren<Renderer>().First(r=>r.enabled).bounds.IntersectRay(new Ray(camera.transform.position,(head-camera.transform.position).normalized),out float hitDistance)||hitDistance>Vector3.Distance(camera.transform.position,head),"Map does not occlude face @ "+t.ToString("F2"));Check(viewport.z>.4f&&viewport.x>.08f&&viewport.x<.92f&&viewport.y>.12f&&viewport.y<.92f,"Head visible in "+camera.name+" @ "+t.ToString("F2"));}
                    if(t>65&&t<80){Check(sample.handDistance<1.1f,"Handoff hands within practical sheet reach @ "+t.ToString("F2"));var vp=camera.WorldToViewportPoint(map.position);Check(camera.name=="CAM_MapHandoff"&&vp.z>.4f&&vp.x>.05f&&vp.x<.95f&&vp.y>.30f&&vp.y<.95f,"Map visible in approved handoff camera @ "+t.ToString("F2"));}
                    if(t>71&&t<83)Check(sample.receiverWeight>.99f&&sequence.AncientMapWasReceived,"Map attached to Explorer after original AncientMapReceived @ "+t.ToString("F2"));
                    ScreenCapture.CaptureScreenshot(Evidence+"/Intro_"+moments[next].ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".png");next++;
                }
                yield return null;
            }
            for(int i=0;i<5;i++)yield return null;
            Check(heardSignal||originalSettings.MusicVolume==0||originalSettings.MasterVolume==0,"Real audio output observed (or user muted)");Check(lateSignal||originalSettings.MusicVolume==0||originalSettings.MasterVolume==0,"Real audio output continues beyond old 61.504 second cutoff");Check(brandObserved,"Final title presented during complete playback");Check(next==moments.Length,"Observed complete Timeline from opening through final departure");Check(received==1,"AncientMapReceived fired exactly once");Check(SceneManager.GetActiveScene().name=="TUT_ExplorerBasics"&&save.LoadProgress().IntroSeen,"Natural Intro completion persists IntroSeen and enters existing Tutorial");Check(maxStep<.12f,"No seated pelvis teleport larger than 12 cm/frame");Check(idempotent,"Authoring repeated byte-for-byte without scene/Timeline duplication");
            for(int run=0;run<2;run++)
            {
                bool replay=run==1;
                typeof(GameFlowController).GetField("_introReplayToMenu",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,replay);
                save.SaveProgress(GameProgressData.CreateDefault());
                EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                var skip=UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();float progress=0;skip.SkipProgressChanged+=v=>progress=Mathf.Max(progress,v);
                Check(skip.IsReplay==replay,"Skip context: "+(replay?"Replay":"First Run"));
                keyboard=InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                for(int frame=0;frame<100&&SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath;frame++)yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(keyboard);keyboard=null;
                for(int frame=0;frame<5;frame++)yield return null;
                Check(progress>=.99f,"Semantic UI.Cancel held for full skip threshold");
                Check(SceneManager.GetActiveScene().name==(replay?"MainMenu":"TUT_ExplorerBasics"),"Actual skip destination: "+(replay?"MainMenu":"Tutorial"));
                Check(save.LoadProgress().IntroSeen==!replay,"Skip progress isolated and correct");
                Check(UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>()==null,"Intro audio removed after skip");
            }
        }
    }
}
