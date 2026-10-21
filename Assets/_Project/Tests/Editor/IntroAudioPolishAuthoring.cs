using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Cinematics;
using Tikal.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class IntroAudioPolishAuthoring
    {
        public const string AudioPath = "Assets/_Project/Audio/Music/Cinematics/MUS_Intro_Suspenso.wav";
        public const double Duration = 85.5;
        public const string Evidence = "Temp/IntroAudioPolish";
        public static double Shift(double t) => t >= 35 ? t - 11.5 : t >= 23.5 ? 22.5 + (t - 23.5) / 11.5 : t >= 1.5 ? t - 1 : t / 3;
        static IntroAudioPolishAuthoring() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists("Temp/IntroPolishApply.request")) return;
            File.Delete("Temp/IntroPolishApply.request");
            Directory.CreateDirectory(Evidence);
            try
            {
                Apply();
                var paths = new[] { AncientMapIntroAuthoring.ScenePath, AncientMapIntroAuthoring.TimelinePath, AudioPath + ".meta" };
                var bytes = paths.Select(File.ReadAllBytes).ToArray();
                Apply();
                if (!paths.Select((p, i) => bytes[i].SequenceEqual(File.ReadAllBytes(p))).All(v => v))
                    throw new InvalidOperationException("Polish authoring is not byte-idempotent");
                AncientMapIntroValidation.Validate();
                EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);
                File.WriteAllText(Evidence + "/Apply.txt", "PASS: authoring twice byte-identical; canonical validation passed");
            }
            catch (Exception e) { File.WriteAllText(Evidence + "/Apply.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Tikal/Authoring/Apply Intro Audio And Pacing Polish")]
        public static void Apply()
        {
            Require(!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode, "Normal interactive Edit Mode");
            Require(!EditorSceneManager.GetActiveScene().isDirty, "Save current scene before authoring");
            var scene = EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);
            var director = UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
            var timeline = (TimelineAsset)director.playableAsset;
            var tracks = ElderCinematicAuthoring.Tracks(timeline).ToArray();
            bool already = UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>() != null;
            if (!already)
            {
                Require(Math.Abs(timeline.duration - 97) < .01, "Expected Elder-integrated 97-second baseline");
                var native = new HashSet<AnimationClip>();
                foreach (var track in tracks)
                {
                    foreach (var clip in track.GetClips())
                    {
                        double start = clip.start, end = clip.end;
                        clip.start = Shift(start); clip.duration = Shift(end) - Shift(start);
                        if (clip.asset is AnimationPlayableAsset a && a.clip != null && AssetDatabase.GetAssetPath(a.clip) == AncientMapIntroAuthoring.TimelinePath)
                            native.Add(a.clip);
                    }
                    foreach (var marker in track.GetMarkers()) marker.time = Shift(marker.time);
                    EditorUtility.SetDirty(track);
                }
                if (timeline.markerTrack != null)
                    foreach (var marker in timeline.markerTrack.GetMarkers()) marker.time = Shift(marker.time);
                foreach (var animation in native)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(animation))
                    {
                        var curve = AnimationUtility.GetEditorCurve(animation, binding);
                        var keys = curve.keys;
                        for (int i = 0; i < keys.Length; i++)
                        {
                            float old = keys[i].time;
                            keys[i].time = (float)(animation.name == "Cinematic_Fade" && Math.Abs(old - 25) < .01 ? 23.5 : Shift(old));
                        }
                        curve.keys = keys;
                        AnimationUtility.SetEditorCurve(animation, binding, curve);
                    }
                    EditorUtility.SetDirty(animation);
                }
            }
            var importer = (AudioImporter)AssetImporter.GetAtPath(AudioPath);
            Require(importer != null, "Converted music is importable");
            var settings = importer.defaultSampleSettings;
            bool reimport = settings.loadType != AudioClipLoadType.Streaming || settings.compressionFormat != AudioCompressionFormat.Vorbis || Math.Abs(settings.quality - .85f) > .001 || settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate || importer.forceToMono;
            settings.loadType = AudioClipLoadType.Streaming; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .85f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            if (reimport) { importer.defaultSampleSettings = settings; importer.forceToMono = false; importer.SaveAndReimport(); }
            var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            Require(audio != null && audio.length > 60 && audio.channels == 2, "Stereo music imported");
            var trackMusic = tracks.OfType<AudioTrack>().Single(t => t.name == "Music");
            var source = ElderCinematicAuthoring.Find("Music").GetComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; source.volume = 0;
            var musicClip = trackMusic.GetClips().SingleOrDefault() ?? trackMusic.CreateClip(audio);
            var musicAsset = (AudioPlayableAsset)musicClip.asset;
            musicAsset.clip = audio; musicAsset.loop = false;
            musicClip.start = 0; musicClip.clipIn = 0; musicClip.timeScale = 1; musicClip.duration = Math.Min(audio.length, Duration);
            musicClip.easeInDuration = 0; musicClip.easeOutDuration = 0;
            director.SetGenericBinding(trackMusic, source);
            var envelope = source.GetComponent<IntroMusicEnvelope>() ?? source.gameObject.AddComponent<IntroMusicEnvelope>();
            var data = new SerializedObject(envelope);
            data.FindProperty("_director").objectReferenceValue = director;
            float endMusic = (float)musicClip.end;
            data.FindProperty("_envelope").animationCurveValue = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.75f, .26f), new Keyframe(14, .26f), new Keyframe(16, .039f), new Keyframe(endMusic - 2.5f, .039f), new Keyframe(endMusic, 0), new Keyframe((float)Duration, 0));
            data.ApplyModifiedPropertiesWithoutUndo();
            var camera = ElderCinematicAuthoring.Find("CAM_MapHandoff").GetComponent<Camera>();
            camera.transform.position = new Vector3(-2.3f, 2.15f, 12.4f);
            camera.transform.LookAt(new Vector3(1.7f, 1.25f, 11.3f));
            camera.fieldOfView = 40;
            EditorUtility.SetDirty(trackMusic); EditorUtility.SetDirty(musicAsset); EditorUtility.SetDirty(timeline);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            ValidateLoaded();
        }
        public static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("Intro polish: " + message); Debug.Log("INTRO_POLISH_PASS " + message); }

        [Serializable] public class Report { public bool passed; public double duration; public string[] checks; }
        public static void ValidateLoaded()
        {
            var checks = new List<string>();
            Action<bool, string> check = (v, m) => { Require(v, m); checks.Add(m); };
            var director = UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
            var timeline = (TimelineAsset)director.playableAsset;
            var tracks = ElderCinematicAuthoring.Tracks(timeline).ToArray();
            var mix = UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>();
            var source = mix.GetComponent<AudioSource>();
            var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            var music = tracks.OfType<AudioTrack>().Single(t => t.name == "Music").GetClips().Single();
            var cues = tracks.OfType<CinematicCueTrack>().SelectMany(t => t.GetClips()).ToArray();
            Func<string, TimelineClip> cue = id => cues.Single(c => ((CinematicCueAsset)c.asset).CueId == id);
            check(audio != null && ((AudioPlayableAsset)music.asset).clip == audio, "1-2 Music present and assigned");
            check(director.GetGenericBinding(tracks.Single(t => t.name == "Music")) == source && !source.loop && source.spatialBlend == 0, "3 Music routing / existing settings multiplier / no loop");
            check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length == 4 && UnityEngine.Object.FindObjectsByType<IntroMusicEnvelope>().Length == 1, "4 Four original AudioSources, one music envelope");
            check(music.start == 0 && mix.MixAt(.75) > .25f, "5 Music starts with Intro");
            check(mix.MixAt(14) > mix.MixAt(15) && mix.MixAt(15) > mix.MixAt(16), "6 Smooth duck exists");
            check(cue(AncientMapIntroAuthoring.NarratorCueTwoId).start == 14 && Math.Abs(mix.MixAt(14) - .26f) < .001, "7 Duck begins on final narrative phrase");
            check(Math.Abs(mix.MixAt(20) / mix.MixAt(10) - .15f) < .001, "8 Dialogue level -16.48 dB, no pumping");
            check(mix.MixAt(music.end) == 0 && mix.MixAt(Duration) == 0 && music.end <= audio.length + .01, "9 Final 2.5-second audio fade, silence through scene exit");
            check(cue("date").start == .5, "10 Earlier date with half-second breath");
            check(cue("date").duration == 3 && cue(AncientMapIntroAuthoring.NarratorCueOneId).duration == 7.5 && cue(AncientMapIntroAuthoring.NarratorCueTwoId).duration == 8.5, "11 Reading durations preserved");
            var walk = tracks.Single(t => t.name == ElderCinematicAuthoring.ExplorerTrack).GetClips().Single(c => c.displayName == "Dwight approach");
            check(Math.Abs(walk.start - cue(AncientMapIntroAuthoring.NarratorCueTwoId).end - 1) < .001, "12 Text-to-walk gap one second");
            check(Math.Abs(timeline.duration - Duration) < .001, "13 Duration 85.5 seconds");
            var camera = ElderCinematicAuthoring.Find("CAM_MapHandoff").GetComponent<Camera>();
            check(camera.transform.position.y > 1.7 && Vector3.Distance(camera.transform.position, new Vector3(1.7f, 1.25f, 11.3f)) > 4, "14 Raised and withdrawn MapHandoff camera");
            check(camera.fieldOfView == 40, "15 Wider FOV 31 to 40");
            check(ElderCinematicAuthoring.Find("AncientMap_Proxy").GetComponent<UnityEngine.Animations.ParentConstraint>().sourceCount == 2, "16 Independent map and hand attachment preserved");
            check(cues.Count(c => ((CinematicCueAsset)c.asset).Type == CinematicCueType.AncientMapReceived) == 1 && cue("map-received").start == 59.5, "17 One AncientMapReceived at shifted handoff");
            foreach (var name in new[] { "ElderAnchor", "ExplorerAnchor" })
            {
                var actor = ElderCinematicAuthoring.Find(name).GetComponentsInChildren<Animator>(true).Single(a => a.avatar != null);
                check(actor.avatar.isHuman && actor.avatar.isValid && !actor.applyRootMotion && actor.runtimeAnimatorController == null, "18-19 Actor Humanoid / Timeline preserved: " + name);
            }
            check(cues.Length == 19 && Math.Abs(cue("elder-remember").duration - 2.1) < .001 && Math.Abs(cue("elder-warning").duration - 3.8) < .001 && ((CinematicCueAsset)cue("elder-warning").asset).Text == "No confíes en el sistema.", "20 All nineteen cues; warning text and durations preserved");
            check(UnityEngine.Object.FindAnyObjectByType<IntroSequenceController>().SkipHoldSeconds == 1.25f && GameFlowController.GetIntroCompletionDestination(true) == GameSceneIds.MainMenu, "21 Existing skip and replay routing");
            check(GameFlowController.GetIntroCompletionDestination(false) == GameSceneIds.Tutorial, "22 Existing first-run Tutorial routing");
            check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "23 No Missing Scripts");
            foreach (var track in tracks.Where(t => t.GetClips().Any())) check(director.GetGenericBinding(track) != null, "24 Binding: " + track.name);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/EditorValidation.json", JsonUtility.ToJson(new Report { passed = true, duration = timeline.duration, checks = checks.ToArray() }, true));
        }
    }
}
