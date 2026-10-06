using System;
using System.Collections.Generic;
using System.Linq;
using Tikal.Cinematics;
using Tikal.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Tests.Editor
{
    public static class AncientMapIntroValidation
    {
        static readonly string[] CameraNames =
        {
            "CAM_Establishing", "CAM_ExplorerWalk", "CAM_TwoShot", "CAM_ExplorerDialogue",
            "CAM_ElderDialogue", "CAM_MapHandoff", "CAM_FinalWarning", "CAM_ExplorerDeparture"
        };

        [MenuItem("Tikal/Validation/Validate Ancient Map Intro")]
        public static void RunFromMenu()
        {
            try
            {
                Validate();
                Debug.Log("TIKAL_ANCIENT_MAP_INTRO_VALID");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void Validate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(AncientMapIntroAuthoring.ScenePath) != null, "Intro scene exists");
            Require(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(), "Current modified scenes were handled before validation");
            var scene = EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var components = roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            Require(components.All(component => component != null), "Scene has no Missing Scripts");

            var director = components.OfType<PlayableDirector>().Single();
            var sequence = components.OfType<IntroSequenceController>().Single();
            var subtitles = components.OfType<CinematicSubtitlePresenter>().Single();
            var skip = components.OfType<CinematicSkipPromptPresenter>().Single();
            Require(director.playableAsset != null, "PlayableDirector has a playable asset");
            Require(sequence.Director == director && sequence.HasRequiredReferences, "IntroSequenceController has all critical references");
            Require(subtitles.HasRequiredReferences, "Subtitle presenter has all critical references");
            Require(skip.HasRequiredReferences, "Skip prompt has all critical references");
            Require(sequence.SkipAction.name == "Cancel", "Skip uses the semantic UI Cancel action");

            var timeline = director.playableAsset as TimelineAsset;
            Require(timeline != null, "PlayableDirector uses a TimelineAsset");
            Require(AssetDatabase.GetAssetPath(timeline) == AncientMapIntroAuthoring.TimelinePath, "Timeline uses the canonical asset path");
            Require(timeline.name == "TL_Intro_AncientMap", "Timeline uses the canonical name");
            Require(timeline.duration >= 95 && timeline.duration <= 105, "Timeline duration is between 95 and 105 seconds");

            var transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            RequireNamed(transforms, "ExplorerAnchor");
            RequireNamed(transforms, "CharacterVisualRoot");
            RequireNamed(transforms, "ExplorerPlaceholder");
            RequireNamed(transforms, "ElderAnchor");
            RequireNamed(transforms, "ElderPlaceholder");
            RequireNamed(transforms, "AncientMap_Proxy");
            RequireNamed(transforms, "FadeCanvas");
            RequireNamed(transforms, "DateCard");
            RequireNamed(transforms, "NarrativeText");
            RequireNamed(transforms, "SubtitlePanel");
            RequireNamed(transforms, "SkipPrompt");
            foreach (var cameraName in CameraNames) RequireNamed(transforms, cameraName);
            Require(components.OfType<Camera>().Count(camera => CameraNames.Contains(camera.name)) == CameraNames.Length, "All eight required cameras exist");
            Require(!components.Any(component => component.GetType().Name == "PlayerMovement" || component.GetType().Name == "PlayerInputReader"), "No gameplay controller drives the cinematic ExplorerAnchor");

            var explorerAnchor = transforms.Single(transform => transform.name == "ExplorerAnchor");
            var elderAnchor = transforms.Single(transform => transform.name == "ElderAnchor");
            var explorerHead = explorerAnchor.Find("CinematicTargets/HeadLookTarget");
            var explorerChest = explorerAnchor.Find("CinematicTargets/ChestLookTarget");
            var elderHead = elderAnchor.Find("CinematicTargets/HeadLookTarget");
            var elderChest = elderAnchor.Find("CinematicTargets/ChestLookTarget");
            Require(explorerHead != null, "Explorer HeadLookTarget exists");
            Require(explorerChest != null, "Explorer ChestLookTarget exists");
            Require(elderHead != null, "Elder HeadLookTarget exists");
            Require(elderChest != null, "Elder ChestLookTarget exists");
            Require(explorerHead.localPosition.y > explorerChest.localPosition.y, "Explorer facial target is above its upper-torso target");
            Require(elderHead.localPosition.y > elderChest.localPosition.y && elderHead.localPosition.y < explorerHead.localPosition.y, "Elder targets respect the seated pose");

            var tracks = AllTracks(timeline).ToArray();
            var cueClips = tracks.OfType<CinematicCueTrack>().SelectMany(track => track.GetClips()).Where(clip => clip.asset is CinematicCueAsset).ToArray();
            var cues = cueClips.Select(clip => (CinematicCueAsset)clip.asset).ToArray();
            Require(cues.Length >= 18, "Timeline contains the complete narrative cue sequence");
            var date = cueClips.Single(clip => ((CinematicCueAsset)clip.asset).CueId == "date");
            var narratorOne = cueClips.Single(clip => ((CinematicCueAsset)clip.asset).CueId == AncientMapIntroAuthoring.NarratorCueOneId);
            var narratorTwo = cueClips.Single(clip => ((CinematicCueAsset)clip.asset).CueId == AncientMapIntroAuthoring.NarratorCueTwoId);
            Require(date.start >= 1.4 && date.duration >= 3, "DateCard has a readable three-second hold after the opening pause");
            Require(narratorOne.start >= 6 && narratorOne.duration >= 7.5, "NarrativeText 1 allows slow narration and retained reading time");
            Require(narratorTwo.start >= 15 && narratorTwo.duration >= 8.5, "NarrativeText 2 allows slow narration and retained reading time");
            Require(narratorTwo.end >= 23.5, "Narrative prologue extends beyond the former seven-second opening");
            var visibleText = string.Join("\n", cues.Select(cue => (cue.Speaker ?? string.Empty) + " " + (cue.Text ?? string.Empty)));
            Require(visibleText.Contains("1500 d.C."), "Date cue contains 1500 d.C.");
            Require(visibleText.Contains("Fuente del Conocimiento"), "Dialogue contains Fuente del Conocimiento");
            Require(visibleText.Contains("entrada oculta"), "Dialogue contains entrada oculta");
            Require(visibleText.Contains("No confíes en el sistema."), "Final warning is configured exactly");
            Require(!visibleText.Contains("SecretCommand"), "SecretCommand is absent from the cinematic");
            Require(cues.Count(cue => cue.Type == CinematicCueType.AncientMapReceived) == 1, "AncientMapReceived has one conceptual event cue");

            var cameraTracks = tracks.OfType<ActivationTrack>().Where(track => CameraNames.Contains(track.name)).ToArray();
            Require(cameraTracks.Length == CameraNames.Length, "Timeline has one activation track for every required camera");
            Require(cameraTracks.All(track => director.GetGenericBinding(track) != null), "Every camera activation track is bound");
            var audioNames = tracks.OfType<AudioTrack>().Select(track => track.name).ToArray();
            foreach (var audioName in new[] { "Ambience", "Music", "Voice", "SFX" }) Require(audioNames.Contains(audioName), "Timeline has the " + audioName + " audio slot");
            Require(audioNames.Contains("VOICE_NARRATOR"), "Timeline has the VOICE_NARRATOR slot");
            Require(cues.Any(cue => cue.CueId == AncientMapIntroAuthoring.NarratorCueOneId), "VO_NARRATOR_INTRO_001 hook exists");
            Require(cues.Any(cue => cue.CueId == AncientMapIntroAuthoring.NarratorCueTwoId), "VO_NARRATOR_INTRO_002 hook exists");
            var ambienceTrack = tracks.OfType<AudioTrack>().Single(track => track.name == "Ambience");
            Require(director.GetGenericBinding(ambienceTrack) is AudioSource, "Ambience Timeline track is bound to its dedicated AudioSource");
            var ambienceExists = AssetDatabase.FindAssets("AUD_AMB_Jungle_Main_01 t:AudioClip").Length > 0;
            Require(!ambienceExists || ambienceTrack.GetClips().Any(), "Existing jungle ambience is placed on its Timeline track");

            var cameras = components.OfType<Camera>().ToDictionary(camera => camera.name);
            var previousTime = director.time;
            director.time = 54; director.Evaluate();
            RequireFacing(cameras["CAM_ExplorerDialogue"], explorerHead, 5, "ExplorerDialogue frames the Explorer face");
            director.time = 60; director.Evaluate();
            RequireFacing(cameras["CAM_ElderDialogue"], elderHead, 5, "ElderDialogue frames the seated Elder face");
            director.time = 92; director.Evaluate();
            RequireFacing(cameras["CAM_FinalWarning"], elderHead, 4, "FinalWarning maintains facial framing");
            Require(cameras["CAM_FinalWarning"].fieldOfView <= 30, "FinalWarning is the intimate close framing");
            director.time = 67; director.Evaluate();
            var map = transforms.Single(transform => transform.name == "AncientMap_Proxy");
            RequireFacing(cameras["CAM_MapHandoff"], map, 8, "MapHandoff intentionally frames the map");
            Require(cameras["CAM_MapHandoff"].fieldOfView <= 32, "MapHandoff keeps its deliberate detail framing");
            director.time = 46.5; director.Evaluate();
            RequireVisible(cameras["CAM_TwoShot"], explorerHead, "TwoShot keeps the Explorer head visible");
            RequireVisible(cameras["CAM_TwoShot"], elderHead, "TwoShot keeps the Elder head visible");
            director.time = previousTime; director.Evaluate();

            Require(SceneCatalog.Get(GameSceneIds.Intro).Path == AncientMapIntroAuthoring.ScenePath, "SceneCatalog routes Intro to Intro_AncientMap");
            Require(GameFlowController.GetIntroCompletionDestination(false) == GameSceneIds.Tutorial, "First Run completion routes to Tutorial");
            Require(GameFlowController.GetIntroCompletionDestination(false) == GameSceneIds.Tutorial, "First Run skip routes to Tutorial through the shared completion path");
            Require(GameFlowController.GetIntroCompletionDestination(true) == GameSceneIds.MainMenu, "Replay completion routes to MainMenu");
            Require(GameFlowController.GetIntroCompletionDestination(true) == GameSceneIds.MainMenu, "Replay skip routes to MainMenu through the shared completion path");

            var buildScenes = EditorBuildSettings.scenes;
            Require(buildScenes.Any(item => item.enabled && item.path == AncientMapIntroAuthoring.ScenePath), "Build Settings enables Intro_AncientMap");
            Require(buildScenes.Any(item => item.enabled && item.path == SceneCatalog.Get(GameSceneIds.MainMenu).Path), "Build Settings retains MainMenu");
            Require(buildScenes.Any(item => item.enabled && item.path == SceneCatalog.Get(GameSceneIds.Tutorial).Path), "Build Settings retains Tutorial");
            Require(buildScenes.Any(item => item.enabled && item.path == SceneCatalog.Get(GameSceneIds.Level01).Path), "Build Settings retains Level01");
            Require(buildScenes.Any(item => !item.enabled && item.path == "Assets/_Project/Scenes/Core/IntroPlaceholder.unity"), "Deprecated IntroPlaceholder remains as a disabled technical fallback");
        }

        static IEnumerable<TrackAsset> AllTracks(TimelineAsset timeline)
        {
            foreach (var root in timeline.GetRootTracks())
            {
                yield return root;
                foreach (var child in Descendants(root)) yield return child;
            }
        }

        static IEnumerable<TrackAsset> Descendants(TrackAsset parent)
        {
            foreach (var child in parent.GetChildTracks())
            {
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }

        static void RequireNamed(IEnumerable<Transform> transforms, string name) =>
            Require(transforms.Count(transform => transform.name == name) == 1, "Scene contains exactly one " + name);

        static void RequireFacing(Camera camera, Transform target, float maxAngle, string message)
        {
            var direction = (target.position - camera.transform.position).normalized;
            Require(Vector3.Angle(camera.transform.forward, direction) <= maxAngle, message);
        }

        static void RequireVisible(Camera camera, Transform target, string message)
        {
            var point = camera.WorldToViewportPoint(target.position);
            Require(point.z > 0 && point.x > .08f && point.x < .92f && point.y > .12f && point.y < .92f, message);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Ancient Map Intro validation failed: " + message);
            Debug.Log("PASS: " + message);
        }
    }
}
