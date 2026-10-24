using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Tikal.Cinematics;
using Tikal.Core;
using Tikal.Persistence;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Animations;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class IntroFinalVoiceValidation
    {
        const string Key="Tikal.FinalVO.Validation",Evidence=IntroFinalVoiceIntegration.Evidence;
        [Serializable] class Setup { public SceneSetup[] scenes; }
        [Serializable] public class Sample { public string file;public int frames;public float voicePeak,voiceRms,musicRms;public bool subtitle,activeSource; }
        [Serializable] class Result { public bool passed;public string scope;public double duration;public string[] checks,errors;public Sample[] samples; }
        static List<string> checks=new List<string>(),errors=new List<string>();
        static Sample[] samples;static IEnumerator routine;static Keyboard keyboard;static GamePersistenceService save;
        static IntroFinalVoiceIntegration.Report report;static double deadline;static int frame,oldCapture;static bool oldBackground;static float oldListener;
        static readonly float[] pcm=new float[1024],musicPcm=new float[1024];
        static IntroFinalVoiceValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/IntroFinalVOPlay.request"))return;
            File.Delete("Temp/IntroFinalVOPlay.request");
            try{Run();}catch(Exception e){File.WriteAllText(Evidence+"/PlayStartError.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tikal/Validation/Validate Final Intro Voices")]
        public static void Run()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required.");
            if(File.Exists("Temp/IntroFinalVOMix.request"))
            {
                IntroFinalVoiceIntegration.FinalizeMix();File.Delete("Temp/IntroFinalVOMix.request");
            }
            SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup()}));
            SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Mode(PlayModeStateChange mode)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode)
            {
                checks.Clear();errors.Clear();report=JsonUtility.FromJson<IntroFinalVoiceIntegration.Report>(File.ReadAllText(Evidence+"/FinalIntegration.json"));
                samples=report.lines.Select(l=>new Sample{file=l.file}).ToArray();
                oldCapture=Time.captureFramerate;Time.captureFramerate=0;oldBackground=Application.runInBackground;Application.runInBackground=true;oldListener=AudioListener.volume;
                save=new GamePersistenceService(Path.Combine(Path.GetTempPath(),"TikalFinalVO-"+Guid.NewGuid().ToString("N")));save.SaveProgress(GameProgressData.CreateDefault());
                SceneManager.sceneLoaded+=Inject;Application.logMessageReceived+=Log;
                Replay(false);frame=-1;deadline=EditorApplication.timeSinceStartup+240;routine=Tests();EditorApplication.update+=Tick;
            }
            if(mode==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key,false);EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup", "")).scenes);
                Debug.Log("INTRO_FINAL_VO_VALIDATION_FINISHED");
            }
        }
        static void Replay(bool value)=>typeof(GameFlowController).GetField("_introReplayToMenu",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,value);
        static void Set(object target,string key,object value)=>target.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static T Field<T>(object target,string key)=>(T)target.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Inject(Scene scene,LoadSceneMode mode)
        {
            foreach(var flow in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameFlowController>(true))){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Final VO playback timeout");if(!routine.MoveNext())Finish();}
            catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;Application.logMessageReceived-=Log;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}Replay(false);
            Time.captureFramerate=oldCapture;Application.runInBackground=oldBackground;AudioListener.volume=oldListener;
            File.WriteAllText(Evidence+"/PlayValidation.json",JsonUtility.ToJson(new Result{passed=errors.Count==0,scope="Normal interactive Play Mode at real-time 1x; complete Intro; PCM output sampling; original skip actions first-run/replay; isolated save. Intelligibility and subjective acting approval require human listening.",duration=report.finalDuration,checks=checks.ToArray(),errors=errors.ToArray(),samples=samples},true));
            routine=null;EditorApplication.ExitPlaymode();
        }
        static float Rms(float[] data)=>Mathf.Sqrt(data.Sum(x=>x*x)/data.Length);
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode,"Interactive Editor; no batch playback");
            EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var sequence=UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>();
            var timeline=(TimelineAsset)director.playableAsset;var tracks=ElderCinematicAuthoring.Tracks(timeline).ToArray();
            var voTracks=tracks.OfType<AudioTrack>().Where(t=>t.name=="VO_Dwight"||t.name=="VO_Elder").ToArray();
            var music=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var musicSource=music.GetComponent<AudioSource>();
            var settings=GameSettingsData.CreateDefault();music.ApplySettings(settings);
            foreach(var source in UnityEngine.Object.FindObjectsByType<IntroVoiceSettings>())source.ApplySettings(settings);
            Check(voTracks.Length==2 && voTracks.Sum(t=>t.GetClips().Count())==14,"Two voice tracks; all 14 AudioClips");
            Check(voTracks.Select(t=>director.GetGenericBinding(t)).Distinct().Count()==2,"One distinct existing/new AudioSource per actor; no duplicated binding");
            Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length==5,"Five sources: four original plus Elder VO only");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(x=>x.isActiveAndEnabled)==1,"Exactly one active AudioListener");
            Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts");
            Check(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null)),"No Missing Materials");
            Check(tracks.Where(t=>t.GetClips().Any()).All(t=>director.GetGenericBinding(t)!=null),"All populated Timeline bindings valid");
            Check(tracks.OfType<AudioTrack>().SelectMany(t=>t.GetClips()).All(c=>((AudioPlayableAsset)c.asset).clip!=null),"No Missing AudioClips");
            Check(Math.Abs(director.duration-report.finalDuration)<.001,"Final duration matches integrated WAV timing");
            var clips=voTracks.SelectMany(t=>t.GetClips()).OrderBy(c=>c.start).ToArray();
            for(int i=0;i<clips.Length;i++)
            {
                var c=clips[i];var l=report.lines[i];var a=(AudioPlayableAsset)c.asset;
                Check(Math.Abs(c.duration-(double)a.clip.samples/a.clip.frequency)<.00001 && c.timeScale==1 && c.clipIn==0 && !a.loop,"Uncut natural-speed WAV: "+l.file);
                if(i>0)Check(clips[i-1].end<c.start,"No accidental VO overlap: "+l.file);
                Check(l.finalStart<=c.start && l.finalEnd-c.end>=.1999,"Subtitle leads and retains 0.20s tail: "+l.file);
            }
            var cues=tracks.OfType<CinematicCueTrack>().SelectMany(t=>t.GetClips()).ToArray();var title=cues.Single(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.Title);
            Check(((CinematicCueAsset)title.asset).Text=="TIKAL EL EXPLORADOR" && Math.Abs(title.duration-3.3)<.001,"Exact branding and approved 3.3s title span intact");
            var presenter=UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>();var black=ElderCinematicAuthoring.Find("Final transition black").GetComponent<CanvasGroup>();
            var map=ElderCinematicAuthoring.Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();int received=0;sequence.AncientMapReceived+=()=>received++;
            bool musicLate=false,hold=false,fade=false,handoff=false;
            while(SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath)
            {
                double time=director.time;
                for(int i=0;i<report.lines.Length;i++)
                {
                    var line=report.lines[i];if(time<line.voiceStart+.05||time>=line.voiceEnd-.05)continue;
                    var source=(AudioSource)director.GetGenericBinding(voTracks.Single(t=>t.name==(line.speaker=="EXPLORADOR"?"VO_Dwight":"VO_Elder")));
                    source.GetOutputData(pcm,0);musicSource.GetOutputData(musicPcm,0);var sample=samples[i];sample.frames++;
                    sample.voicePeak=Mathf.Max(sample.voicePeak,pcm.Max(Mathf.Abs));sample.voiceRms=Mathf.Max(sample.voiceRms,Rms(pcm));sample.musicRms=Mathf.Max(sample.musicRms,Rms(musicPcm));sample.activeSource|=source.isPlaying;
                    sample.subtitle|=Field<GameObject>(presenter,"_subtitlePanel").activeSelf && Field<TMP_Text>(presenter,"_dialogueText").text==line.text;
                    if(time>line.voiceStart+.2 && time<line.voiceEnd-.2 && Math.Abs(music.MixAt(time)-.018f)>.0001)throw new Exception("Deep continuous voice duck: "+line.file);
                }
                if(time>87.7){musicSource.GetOutputData(musicPcm,0);musicLate|=musicPcm.Any(x=>Math.Abs(x)>.000001);}
                if(!handoff && time>report.lines[8].voiceStart+.5)
                {
                    var cam=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.isActiveAndEnabled);var vp=cam.WorldToViewportPoint(map.position);
                    Check(cam.name=="CAM_MapHandoff" && vp.z>0 && vp.x>0 && vp.x<1 && vp.y>0 && vp.y<1,"Map visible in approved handoff camera during Elder_07");handoff=true;
                    ScreenCapture.CaptureScreenshot(Evidence+"/Handoff.png");
                }
                if(time>title.start+.4 && time<title.end-.7)
                {
                    Check(Field<GameObject>(presenter,"_titleCard").activeSelf && black.alpha<.001,"Brand fully readable before final fade");hold=true;
                }
                if(!fade && time>title.end-.3 && time<title.end-.15){Check(black.alpha>.3f&&black.alpha<1 && music.MixAt(time)<.08f,"Approved 0.6s visual/music fade");fade=true;}
                if(time>title.end-1.1 && time<title.end-.8)Check(music.MixAt(time)>.099f,"Ambient music recovers through title hold");
                if(time>=report.lines[13].voiceEnd+.1)Check(constraint.GetSource(1).weight>.99f,"Map remains with Explorer after handoff");
                if(Time.frameCount%120==0)File.WriteAllText(Evidence+"/Progress.txt",time.ToString("F2"));
                yield return null;
            }
            // Completion is proved by the actual destination/progress below, not by assuming
            // an Editor update lands within 150 ms of a scene unload on every machine.
            Check(hold&&fade&&handoff,"Complete real-time Intro through handoff, warning, title and fade");
            for(int i=0;i<samples.Length;i++)Check(samples[i].frames>10&&samples[i].voicePeak>.0001&&samples[i].musicRms>0&&samples[i].subtitle&&samples[i].activeSource,"VO PCM, background PCM and exact subtitle observed: "+samples[i].file);
            Check(musicLate,"Music continues beyond original 87.6s without silence");Check(received==1,"AncientMapReceived exactly once");
            Check(SceneManager.GetActiveScene().name=="TUT_ExplorerBasics"&&save.LoadProgress().IntroSeen,"Natural completion -> Tutorial with isolated progress");
            for(int run=0;run<2;run++)
            {
                Replay(run==1);save.SaveProgress(GameProgressData.CreateDefault());
                EditorSceneManager.LoadSceneInPlayMode(AncientMapIntroAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();director.time=report.lines[0].voiceStart+.5;director.Evaluate();yield return null;
                var voices=UnityEngine.Object.FindObjectsByType<IntroVoiceSettings>();
                keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                double until=EditorApplication.timeSinceStartup+4;
                while(SceneManager.GetActiveScene().path==AncientMapIntroAuthoring.ScenePath&&EditorApplication.timeSinceStartup<until)yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.RemoveDevice(keyboard);keyboard=null;
                Check(SceneManager.GetActiveScene().name==(run==1?"MainMenu":"TUT_ExplorerBasics"),"Original held Cancel skip destination: "+(run==1?"replay MainMenu":"first-run Tutorial"));
                Check(voices.All(v=>v==null) && UnityEngine.Object.FindObjectsByType<IntroVoiceSettings>().Length==0,"No Intro VO survives skip: "+run);
            }
        }
    }
}
