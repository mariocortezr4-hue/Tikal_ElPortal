using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tikal.Cinematics;
using Tikal.Core;
using Tikal.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class IntroTitleTiming
    {
        const double OldEnd=85.5,HoldEnd=87,End=87.6;
        const string Evidence="../_IntroTitleTiming_20261023",Key="Tikal.IntroTitleTiming";
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static IEnumerator routine;static int frame,oldCapture;static double deadline;static bool oldBackground;static Keyboard keyboard;static GamePersistenceService save;static float oldListener;
        static IntroTitleTiming(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!File.Exists("Temp/IntroTitleTiming.request"))return;File.Delete("Temp/IntroTitleTiming.request");
            try{Run();}catch(Exception e){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tikal/Authoring/Apply Intro Title Hold Timing")]
        public static void Apply()
        {
            if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved normal Edit Mode required.");
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);var d=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var t=(TimelineAsset)d.playableAsset;
            if(Math.Abs(t.duration-End)<.001)return;
            if(Math.Abs(t.duration-OldEnd)>.001)throw new Exception("Unexpected baseline duration; preserve scene.");
            var tracks=ElderCinematicAuthoring.Tracks(t).ToArray();var cue=tracks.OfType<CinematicCueTrack>().SelectMany(x=>x.GetClips()).Single(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.Title);
            if(Math.Abs(cue.start-84.3)>.001)throw new Exception("Unexpected title start");cue.duration=End-cue.start;EditorUtility.SetDirty(cue.GetParentTrack());
            var music=tracks.OfType<AudioTrack>().Single(x=>x.name=="Music").GetClips().Single();music.duration=End;EditorUtility.SetDirty(music.GetParentTrack());
            var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var data=new SerializedObject(mix);var curve=data.FindProperty("_envelope").animationCurveValue;var keys=curve.keys;
            for(int i=0;i<keys.Length;i++){if(Math.Abs(keys[i].time-84.9)<.001)keys[i].time=(float)HoldEnd;else if(Math.Abs(keys[i].time-OldEnd)<.001)keys[i].time=(float)End;}curve.keys=keys;data.FindProperty("_envelope").animationCurveValue=curve;data.ApplyModifiedPropertiesWithoutUndo();
            // Keep the already approved world fade: extend its held-black tail only.
            tracks.OfType<AnimationTrack>().Single(x=>x.name=="Fade").GetClips().Single().duration=End;
            foreach(var track in tracks.OfType<ActivationTrack>())foreach(var clip in track.GetClips())if(Math.Abs(clip.end-OldEnd)<.001){clip.duration=End-clip.start;EditorUtility.SetDirty(track);}
            var canvas=ElderCinematicAuthoring.Find("CinematicCanvas");var obj=new GameObject("Final transition black",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup),typeof(Animator));obj.transform.SetParent(canvas,false);
            var r=obj.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;obj.GetComponent<Image>().color=Color.black;obj.GetComponent<Image>().raycastTarget=false;obj.GetComponent<CanvasGroup>().alpha=0;
            var fade=new AnimationClip{name="Intro_FinalTransition",frameRate=30,wrapMode=WrapMode.ClampForever};AnimationUtility.SetEditorCurve(fade,EditorCurveBinding.FloatCurve("",typeof(CanvasGroup),"m_Alpha"),new AnimationCurve(new Keyframe(0,0),new Keyframe((float)HoldEnd,0),new Keyframe((float)End,1)));AssetDatabase.AddObjectToAsset(fade,t);
            var finalTrack=t.CreateTrack<AnimationTrack>(null,"Final transition black");var finalClip=finalTrack.CreateClip(fade);finalClip.start=0;finalClip.duration=End;d.SetGenericBinding(finalTrack,obj.GetComponent<Animator>());
            EditorUtility.SetDirty(t);EditorUtility.SetDirty(mix);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tikal/Validation/Validate Intro Title Hold Timing")]
        public static void Run()
        {
            Directory.CreateDirectory(Evidence);Apply();var sceneBytes=File.ReadAllBytes(AncientMapIntroAuthoring.ScenePath);var timelineBytes=File.ReadAllBytes(AncientMapIntroAuthoring.TimelinePath);Apply();if(!sceneBytes.SequenceEqual(File.ReadAllBytes(AncientMapIntroAuthoring.ScenePath))||!timelineBytes.SequenceEqual(File.ReadAllBytes(AncientMapIntroAuthoring.TimelinePath)))throw new Exception("Timing authoring not idempotent");
            SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Mode(PlayModeStateChange mode)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode)
            {
                checks.Clear();errors.Clear();oldCapture=Time.captureFramerate;Time.captureFramerate=30;oldBackground=Application.runInBackground;Application.runInBackground=true;oldListener=AudioListener.volume;
                save=new GamePersistenceService(Path.GetFullPath(Evidence+"/IsolatedSave"));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=Inject;Application.logMessageReceived+=Log;frame=-1;deadline=EditorApplication.timeSinceStartup+100;routine=Tests();EditorApplication.update+=Tick;
            }
            if(mode==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene(SessionState.GetString(Key+"Scene",AncientMapIntroAuthoring.ScenePath));Debug.Log("INTRO_TITLE_TIMING_FINISHED");}
        }
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        static void Inject(Scene scene,LoadSceneMode mode){foreach(var flow in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameFlowController>(true))){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}}
        static void Log(string m,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(m+stack);}
        static void Check(bool v,string m){if(!v)throw new Exception(m);checks.Add(m);}
        static void Tick(){if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Local Intro timing validation");if(!routine.MoveNext())Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}}
        [Serializable]class Report{public bool passed;public double oldDuration=OldEnd,newDuration=End,addedHold=1.5,fade=.6;public string[] checks,errors;}
        static void Finish(){EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;Application.logMessageReceived-=Log;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;AudioListener.volume=oldListener;File.WriteAllText(Evidence+"/Validation.json",JsonUtility.ToJson(new Report{passed=errors.Count==0,checks=checks.ToArray(),errors=errors.ToArray()},true));EditorApplication.ExitPlaymode();}
        static IEnumerator Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var d=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var t=(TimelineAsset)d.playableAsset;var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var music=mix.GetComponent<AudioSource>();var title=ElderCinematicAuthoring.Find("TitleCard");var black=ElderCinematicAuthoring.Find("Final transition black").GetComponent<CanvasGroup>();
            Check(Math.Abs(d.duration-End)<.001,"Timeline 87.6 seconds");Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).All(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)==0),"No Missing Scripts");Check(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).All(x=>x.sharedMaterials.All(m=>m!=null&&m.shader!=null)),"No Missing Materials");
            d.time=83.9;d.Evaluate();bool start=false,hold=false,fade=false,nearBlack=false,signal=false;var settings=new GamePersistenceService().LoadSettings();bool muted=settings.MusicVolume==0||settings.MasterVolume==0;double holdStart=-1,lastClear=-1;
            while(SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath)
            {
                double now=d.time;
                if(now>=84.3&&now<HoldEnd){Check(title.gameObject.activeInHierarchy&&title.Find("Intro final brand").gameObject.activeInHierarchy,"Brand visible @ "+now.ToString("F2"));Check(black.alpha<.001f,"No transition during readable hold");lastClear=now;if(now>=85.5&&holdStart<0)holdStart=now;}
                if(!start&&now>=84.4){start=true;var cue=ElderCinematicAuthoring.Tracks(t).OfType<CinematicCueTrack>().SelectMany(x=>x.GetClips()).Single(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.Title);Check(((CinematicCueAsset)cue.asset).Text==IntroCinematicFinalPolish.Title,"Exact approved branding");}
                if(!hold&&now>=86.8){hold=true;Check(mix.MixAt(now)>.099f,"Ambient music retained through extra hold");ScreenCapture.CaptureScreenshot(Evidence+"/Title_Hold.png");}
                if(now>=85.6&&now<86.9){var pcm=new float[512];music.GetOutputData(pcm,0);if(pcm.Any(v=>Math.Abs(v)>.000001f))signal=true;}
                if(!fade&&now>=87.3){fade=true;Check(black.alpha>.35f&&black.alpha<.75f,"Smooth black fade midpoint");Check(mix.MixAt(now)>.02f&&mix.MixAt(now)<.08f,"Music fades smoothly with final transition");ScreenCapture.CaptureScreenshot(Evidence+"/Title_Fade.png");}
                if(now>=87.55){nearBlack=true;Check(black.alpha>.97f,"Fade reaches black immediately before Tutorial");}
                yield return null;
            }
            Check(start&&hold&&fade&&nearBlack,"Observed complete final section only");Check(lastClear-holdStart>=1.43,"Additional fully readable hold approximately 1.5 seconds at 30 fps");Check(signal||muted,"Music PCM sampled during extra hold (or existing user mute)");Check(SceneManager.GetActiveScene().name=="TUT_ExplorerBasics"&&save.LoadProgress().IntroSeen,"Natural Intro -> Tutorial unchanged");
            save.SaveProgress(GameProgressData.CreateDefault());EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;d=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();d.time=84.4;d.Evaluate();
            var sequence=UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();float progress=0;sequence.SkipProgressChanged+=v=>progress=Mathf.Max(progress,v);keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));
            for(int i=0;i<80&&SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath;i++)yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;Check(progress>=.99f,"Existing semantic skip hold works during title");Check(SceneManager.GetActiveScene().name=="TUT_ExplorerBasics","Skip reaches original Tutorial destination");
        }
    }
}
