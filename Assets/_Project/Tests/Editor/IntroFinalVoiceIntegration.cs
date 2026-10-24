using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Tests.Editor
{
    // One-shot authoring for the explicitly authorized final VO pass. Never runs on load alone.
    [InitializeOnLoad]
    public static class IntroFinalVoiceIntegration
    {
        public const string Folder = "Assets/_Project/Audio/VO/Intro/";
        public const string Evidence = "Temp/IntroFinalVO";
        public const string MusicPath = "Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso_FinalVO.wav";
        [Serializable] public class Line
        {
            public string cueId, speaker, text, file, status, sha256;
            public int number, hz, channels, bits;
            public double start, window, seconds, peakDBFS, rmsDBFS, initialSilence, finalSilence, delta, gainDB, gain;
            public double finalStart, finalEnd, voiceStart, voiceEnd;
        }
        [Serializable] public class Report { public double oldDuration=87.6, finalDuration; public Line[] lines; }
        static IntroFinalVoiceIntegration() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("Temp/IntroFinalVOMix.request"))
            {
                File.Delete("Temp/IntroFinalVOMix.request");
                try { FinalizeMix(); } catch(Exception e) { File.WriteAllText(Evidence+"/MixError.txt",e.ToString());Debug.LogException(e); }
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/IntroFinalVOApply.request")) return;
            File.Delete("Temp/IntroFinalVOApply.request");
            try { Apply(); File.WriteAllText(Evidence+"/Apply.txt","PASS"); }
            catch(Exception e) { File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Tikal/Authoring/Integrate Final Intro Voices")]
        public static void Apply()
        {
            if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Normal Edit Mode required.");
            if(EditorSceneManager.GetSceneManagerSetup().Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)) throw new Exception("Save current scenes first.");
            var t=AssetDatabase.LoadAssetAtPath<TimelineAsset>(AncientMapIntroAuthoring.TimelinePath);
            if(ElderCinematicAuthoring.Tracks(t).Any(x=>x.name=="VO_Dwight")) throw new Exception("Already integrated: do not retime twice.");
            if(Math.Abs(t.duration-87.6)>.001) throw new Exception("Unexpected approved baseline.");
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/SourceAudit.json"));
            if(report.lines.Length!=14) throw new Exception("Expected 14 audited WAV.");
            foreach(var line in report.lines)
            {
                string path=Folder+line.file;
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=false;importer.SaveAndReimport();
                var audio=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(audio==null || audio.channels!=1 || audio.frequency!=48000 || Math.Abs((double)audio.samples/audio.frequency-line.seconds)>.0001) throw new Exception("WAV audit/import mismatch: "+line.file);
            }
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);
            var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
            var tracks=ElderCinematicAuthoring.Tracks(t).ToArray();
            var cues=tracks.OfType<CinematicCueTrack>().SelectMany(x=>x.GetClips()).ToArray();
            // Insert only missing time at each original subtitle end. Existing inter-line gaps survive.
            var inserts=report.lines.Select(l=>new Vector2((float)(l.start+l.window),(float)Math.Max(0,l.seconds+.28-l.window))).ToArray();
            Func<double,double> shift=time=>time+inserts.Where(x=>x.x<=time+.00001).Sum(x=>(double)x.y);
            var native=new HashSet<AnimationClip>();
            foreach(var track in tracks)
            {
                foreach(var clip in track.GetClips())
                {
                    double start=clip.start,duration=clip.duration,end=clip.end;
                    clip.start=shift(start);clip.duration=shift(end)-shift(start);
                    if(clip.asset is AnimationPlayableAsset a && a.clip!=null)
                    {
                        if(AssetDatabase.GetAssetPath(a.clip)==AncientMapIntroAuthoring.TimelinePath) native.Add(a.clip);
                        else if(Math.Abs(clip.duration-duration)>.00001) clip.timeScale*=duration/clip.duration;
                    }
                }
                foreach(var marker in track.GetMarkers()) marker.time=shift(marker.time);
                EditorUtility.SetDirty(track);
            }
            if(t.markerTrack!=null)foreach(var marker in t.markerTrack.GetMarkers())marker.time=shift(marker.time);
            foreach(var animation in native)
            {
                foreach(var binding in AnimationUtility.GetCurveBindings(animation))
                {
                    var curve=AnimationUtility.GetEditorCurve(animation,binding);var keys=curve.keys;
                    for(int i=0;i<keys.Length;i++)keys[i].time=(float)shift(keys[i].time);
                    curve.keys=keys;AnimationUtility.SetEditorCurve(animation,binding,curve);
                }
                EditorUtility.SetDirty(animation);
            }
            report.finalDuration=shift(87.6);
            var dwight=tracks.OfType<AudioTrack>().Single(x=>x.name=="Voice");
            if(dwight.GetClips().Any())throw new Exception("Preserve unexpected existing voice clips.");
            dwight.name="VO_Dwight";var elder=t.CreateTrack<AudioTrack>(dwight.parent as TrackAsset,"VO_Elder");
            var originalVoice=ElderCinematicAuthoring.Find("Voice").GetComponent<AudioSource>();
            var elderObject=new GameObject("VO_Elder");elderObject.transform.SetParent(originalVoice.transform.parent,false);
            var elderSource=elderObject.AddComponent<AudioSource>();
            foreach(var pair in new[]{(track:dwight,source:originalVoice),(track:elder,source:elderSource)})
            {
                pair.source.playOnAwake=false;pair.source.loop=false;pair.source.spatialBlend=0;pair.source.volume=1;pair.source.clip=null;
                director.SetGenericBinding(pair.track,pair.source);
                var voice=pair.source.gameObject.AddComponent<IntroVoiceSettings>();var data=new SerializedObject(voice);
                data.FindProperty("_director").objectReferenceValue=director;data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var line in report.lines)
            {
                var cue=cues.Single(c=>c.asset is CinematicCueAsset a && a.CueId==line.cueId);
                if(((CinematicCueAsset)cue.asset).Text!=line.text)throw new Exception("Preserve exact subtitle: "+line.cueId);
                line.finalStart=cue.start;line.finalEnd=cue.end;line.voiceStart=cue.start+.08;line.voiceEnd=line.voiceStart+line.seconds;
                var track=line.speaker=="EXPLORADOR"?dwight:elder;
                var clip=track.CreateClip(AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+line.file));
                clip.start=line.voiceStart;clip.duration=line.seconds;clip.clipIn=0;clip.timeScale=1;clip.easeInDuration=0;clip.easeOutDuration=0;
                var audio=(AudioPlayableAsset)clip.asset;audio.loop=false;
                var data=new SerializedObject(audio);data.FindProperty("m_ClipProperties.volume").floatValue=(float)line.gain;data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(audio);EditorUtility.SetDirty(track);
            }
            AssetDatabase.ImportAsset(MusicPath,ImportAssetOptions.ForceSynchronousImport);
            var musicImporter=(AudioImporter)AssetImporter.GetAtPath(MusicPath);var musicSettings=musicImporter.defaultSampleSettings;
            musicSettings.loadType=AudioClipLoadType.Streaming;musicSettings.compressionFormat=AudioCompressionFormat.Vorbis;musicSettings.quality=.85f;
            musicSettings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;musicSettings.preloadAudioData=false;
            musicImporter.defaultSampleSettings=musicSettings;musicImporter.forceToMono=false;musicImporter.SaveAndReimport();
            var musicTrack=tracks.OfType<AudioTrack>().Single(x=>x.name=="Music");var musicClip=musicTrack.GetClips().Single();
            var musicAsset=(AudioPlayableAsset)musicClip.asset;musicAsset.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);musicAsset.loop=false;musicClip.duration=report.finalDuration;
            EditorUtility.SetDirty(musicAsset);EditorUtility.SetDirty(musicTrack);
            var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var mixData=new SerializedObject(mix);
            var oldCurve=mixData.FindProperty("_envelope").animationCurveValue;var mixKeys=oldCurve.keys;
            for(int i=0;i<mixKeys.Length;i++)
            {
                if(mixKeys[i].time>=33.9f && Math.Abs(mixKeys[i].value-.006f)<.00001)mixKeys[i].value=.018f;
                mixKeys[i].time=(float)shift(mixKeys[i].time);
            }
            oldCurve.keys=mixKeys;mixData.FindProperty("_envelope").animationCurveValue=oldCurve;mixData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mix);EditorUtility.SetDirty(t);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssetIfDirty(t);
            File.WriteAllText(Evidence+"/FinalIntegration.json",JsonUtility.ToJson(report,true));
            Debug.Log("INTRO_FINAL_VO_APPLIED "+report.finalDuration.ToString("F3"));
        }
        [MenuItem("Tikal/Authoring/Finalize Intro VO Music Balance")]
        public static void FinalizeMix()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene first.");
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);
            var mix=UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();var data=new SerializedObject(mix);
            var curve=data.FindProperty("_envelope").animationCurveValue;var keys=curve.keys;
            for(int i=0;i<keys.Length;i++)if(keys[i].time>=33.9f && Math.Abs(keys[i].value-.006f)<.00001)keys[i].value=.018f;
            curve.keys=keys;data.FindProperty("_envelope").animationCurveValue=curve;data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/Mix.txt","Dialogue duck 0.018; opening narration unchanged; original ambient recovery and ramps preserved.");
        }
    }
}
