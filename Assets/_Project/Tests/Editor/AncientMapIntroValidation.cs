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
            Require(timeline.duration >= 75 && timeline.duration <= 85, "Timeline duration is between 75 and 85 seconds");

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

            var tracks = AllTracks(timeline).ToArray();
            var cues = tracks.OfType<CinematicCueTrack>().SelectMany(track => track.GetClips()).Select(clip => clip.asset as CinematicCueAsset).Where(cue => cue != null).ToArray();
            Require(cues.Length >= 18, "Timeline contains the complete narrative cue sequence");
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

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Ancient Map Intro validation failed: " + message);
            Debug.Log("PASS: " + message);
        }
    }
}
