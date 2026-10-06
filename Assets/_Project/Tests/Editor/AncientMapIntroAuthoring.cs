using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tikal.Cinematics;
using Tikal.Core;
using Tikal.Input;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class AncientMapIntroAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Cinematics/Intro_AncientMap.unity";
        public const string TimelinePath = "Assets/_Project/Timelines/Intro/TL_Intro_AncientMap.playable";
        public const double Duration = 97d;
        public const string NarratorCueOneId = "VO_NARRATOR_INTRO_001";
        public const string NarratorCueTwoId = "VO_NARRATOR_INTRO_002";
        const string PlaceholderPath = "Assets/_Project/Scenes/Core/IntroPlaceholder.unity";
        const string FontPath = "Assets/_Project/UI/Fonts/InteractionPromptFont.asset";

        [MenuItem("Tikal/Authoring/Build Ancient Map Intro")]
        public static void RunFromMenu()
        {
            try
            {
                if (Build()) Debug.Log("TIKAL_ANCIENT_MAP_INTRO_AUTHORED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static bool Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EnsureFolder("Assets/_Project/Scenes/Cinematics");
            EnsureFolder("Assets/_Project/Timelines/Intro");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Intro_AncientMap");
            var systems = Child("Systems", root.transform);
            var environment = Child("Environment", root.transform);
            var characters = Child("Characters", root.transform);
            var props = Child("Props", root.transform);
            var cameras = Child("Cameras", root.transform);
            var ui = Child("UI", root.transform);
            var lighting = Child("Lighting", root.transform);

            var flow = systems.gameObject.AddComponent<GameFlowController>();
            var devices = systems.gameObject.AddComponent<InputDeviceManager>();
            var director = systems.gameObject.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            var sequence = systems.gameObject.AddComponent<IntroSequenceController>();
            var audio = BuildAudio(systems);

            BuildEnvironment(environment);
            var explorer = BuildExplorer(characters);
            BuildElder(characters);
            var map = BuildMap(props);
            var cameraObjects = BuildCameras(cameras);
            var uiObjects = BuildUi(ui, sequence, devices);
            BuildLighting(lighting);

            var timeline = RebuildTimeline(director, sequence, explorer, map, uiObjects, cameraObjects, audio);
            director.playableAsset = timeline;
            Set(sequence, "_flow", flow);
            Set(sequence, "_director", director);
            Set(sequence, "_subtitles", uiObjects.Subtitles);
            Set(sequence, "_skipHoldSeconds", 1.25f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
            RegisterScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        static AudioObjects BuildAudio(Transform systems)
        {
            var root = Child("Audio", systems);
            AudioSource CreateSource(string name)
            {
                var source = Child(name, root).gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0;
                return source;
            }
            var ambience = CreateSource("Ambience");
            ambience.volume = .55f;
            ambience.gameObject.AddComponent<Animator>();
            return new AudioObjects(ambience, CreateSource("Music"), CreateSource("Voice"), CreateSource("SFX"));
        }

        static void BuildEnvironment(Transform parent)
        {
            var floor = Material("Floor.mat");
            var route = Material("Route.mat");
            var stone = Material("RuinProxy.mat");
            var vegetation = Material("VegetationMediumProxy.mat");
            Shape("Jungle_Blockout", parent, PrimitiveType.Cube, new Vector3(0, -.35f, 15), new Vector3(34, .7f, 55), floor);
            Shape("Path", parent, PrimitiveType.Cube, new Vector3(0, .04f, 13), new Vector3(4.5f, .08f, 48), route);
            var ruins = Child("Ruins", parent);
            Shape("Ruin wall left", ruins, PrimitiveType.Cube, new Vector3(-5.5f, 1.7f, 18), new Vector3(3, 3.4f, 1), stone);
            Shape("Ruin wall right", ruins, PrimitiveType.Cube, new Vector3(5.5f, 1.2f, 22), new Vector3(2.4f, 2.4f, 1.2f), stone);
            Shape("Ruin arch left", ruins, PrimitiveType.Cube, new Vector3(-2.4f, 1.4f, 30), new Vector3(.8f, 2.8f, 1), stone);
            Shape("Ruin arch right", ruins, PrimitiveType.Cube, new Vector3(2.4f, 1.4f, 30), new Vector3(.8f, 2.8f, 1), stone);
            Shape("Ruin arch lintel", ruins, PrimitiveType.Cube, new Vector3(0, 3f, 30), new Vector3(5.6f, .5f, 1), stone);
            var rest = Child("ElderRestArea", parent);
            Shape("Stone seat", rest, PrimitiveType.Cube, new Vector3(2.3f, .4f, 12), new Vector3(2.2f, .8f, 1.2f), stone);
            var background = Child("Background", parent);
            for (var index = 0; index < 28; index++)
            {
                var side = index % 2 == 0 ? -1f : 1f;
                var z = -5f + index * 1.65f;
                var distance = 5.5f + index % 4;
                Shape("Vegetation proxy " + index, background, PrimitiveType.Cylinder,
                    new Vector3(side * distance, 1.4f + index % 3 * .2f, z),
                    new Vector3(.5f + index % 3 * .16f, 2.5f + index % 4 * .45f, .5f + index % 2 * .2f), vegetation);
            }
        }

        static Animator BuildExplorer(Transform parent)
        {
            var anchor = Child("ExplorerAnchor", parent);
            anchor.localPosition = new Vector3(0, 0, -5);
            var animator = anchor.gameObject.AddComponent<Animator>();
            BuildCinematicTargets(anchor, 2.08f, 1.42f);
            var visualRoot = Child("CharacterVisualRoot", anchor);
            var visual = Child("ExplorerPlaceholder", visualRoot);
            Humanoid(visual, Material("Route.mat"), false);
            return animator;
        }

        static void BuildCinematicTargets(Transform anchor, float headHeight, float chestHeight)
        {
            var targets = Child("CinematicTargets", anchor);
            Child("HeadLookTarget", targets).localPosition = new Vector3(0, headHeight, 0);
            Child("ChestLookTarget", targets).localPosition = new Vector3(0, chestHeight, 0);
        }

        static void BuildElder(Transform parent)
        {
            var anchor = Child("ElderAnchor", parent);
            anchor.localPosition = new Vector3(2.3f, .8f, 12f);
            anchor.localRotation = Quaternion.Euler(0, 205, 0);
            BuildCinematicTargets(anchor, 1.65f, 1.02f);
            var visual = Child("ElderPlaceholder", anchor);
            Humanoid(visual, Material("EarthProxy.mat"), true);
        }

        static Animator BuildMap(Transform parent)
        {
            var map = Shape("AncientMap_Proxy", parent, PrimitiveType.Cube, new Vector3(1.75f, 1.55f, 11.55f), new Vector3(.62f, .04f, .42f), Material("EarthProxy.mat"));
            map.transform.localRotation = Quaternion.Euler(18, 24, 4);
            return map.gameObject.AddComponent<Animator>();
        }

        static Camera[] BuildCameras(Transform parent)
        {
            var definitions = new[]
            {
                new CameraDefinition("CAM_Establishing", new Vector3(-13, 8, -10), new Vector3(0, 1, 15), 48),
                new CameraDefinition("CAM_ExplorerWalk", new Vector3(-5, 2.2f, -1), new Vector3(0, 1.2f, 8), 42),
                new CameraDefinition("CAM_TwoShot", new Vector3(-4.2f, 3f, 8.4f), new Vector3(1.15f, 2.25f, 11), 45),
                new CameraDefinition("CAM_ExplorerDialogue", new Vector3(3.8f, 2.2f, 12.9f), new Vector3(.1f, 2.08f, 9.8f), 37),
                new CameraDefinition("CAM_ElderDialogue", new Vector3(-.2f, 2.2f, 10.2f), new Vector3(2.3f, 2.45f, 12), 35),
                new CameraDefinition("CAM_MapHandoff", new Vector3(-.2f, 1.7f, 10.6f), new Vector3(1.35f, 1.5f, 11.5f), 31),
                new CameraDefinition("CAM_FinalWarning", new Vector3(.25f, 2.35f, 10.2f), new Vector3(2.3f, 2.45f, 12), 28),
                new CameraDefinition("CAM_ExplorerDeparture", new Vector3(8, 4.2f, 6), new Vector3(0, 1.5f, 20), 52)
            };
            var result = new List<Camera>();
            foreach (var definition in definitions)
            {
                var go = new GameObject(definition.Name, typeof(Camera), typeof(AudioListener));
                go.transform.SetParent(parent, false);
                go.transform.localPosition = definition.Position;
                go.transform.LookAt(definition.Target);
                var camera = go.GetComponent<Camera>();
                camera.fieldOfView = definition.FieldOfView;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .055f, .045f);
                result.Add(camera);
            }
            return result.ToArray();
        }

        static UiObjects BuildUi(Transform parent, IntroSequenceController sequence, InputDeviceManager devices)
        {
            var canvasObject = new GameObject("CinematicCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var fade = FullPanel("FadeCanvas", canvasObject.transform, Color.black);
            fade.transform.SetAsFirstSibling();
            var fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1;
            fade.AddComponent<Animator>();

            var date = CenterPanel("DateCard", canvasObject.transform, new Vector2(700, 150));
            var dateGroup = date.AddComponent<CanvasGroup>(); dateGroup.alpha = 0; date.AddComponent<Animator>();
            var dateText = Text("DateText", date.transform, "1500 d.C.", 54, TextAlignmentOptions.Center);
            Stretch(dateText.rectTransform, 30);
            var narrative = CenterPanel("NarrativeText", canvasObject.transform, new Vector2(1180, 220));
            var narrativeGroup = narrative.AddComponent<CanvasGroup>(); narrativeGroup.alpha = 0; narrative.AddComponent<Animator>();
            var narrativeText = Text("NarrativeText", narrative.transform, string.Empty, 34, TextAlignmentOptions.Center);
            Stretch(narrativeText.rectTransform, 36);
            var subtitle = BottomPanel("SubtitlePanel", canvasObject.transform, new Vector2(1320, 180), 90);
            var speaker = Text("SpeakerName", subtitle.transform, string.Empty, 22, TextAlignmentOptions.Left);
            Place(speaker.rectTransform, new Vector2(36, 116), new Vector2(1248, 34), new Vector2(0, 0));
            speaker.color = new Color(.78f, .66f, .38f);
            var dialogue = Text("Dialogue", subtitle.transform, string.Empty, 34, TextAlignmentOptions.Left);
            Place(dialogue.rectTransform, new Vector2(36, 26), new Vector2(1248, 84), new Vector2(0, 0));
            var title = CenterPanel("TitleCard", canvasObject.transform, new Vector2(900, 180));
            var titleText = Text("TitleText", title.transform, "TIKAL: EL PORTAL", 60, TextAlignmentOptions.Center);
            Stretch(titleText.rectTransform, 30);

            var presenter = canvasObject.AddComponent<CinematicSubtitlePresenter>();
            Set(presenter, "_dateCard", date); Set(presenter, "_dateText", dateText);
            Set(presenter, "_narrativePanel", narrative); Set(presenter, "_narrativeText", narrativeText);
            Set(presenter, "_subtitlePanel", subtitle); Set(presenter, "_speakerText", speaker); Set(presenter, "_dialogueText", dialogue);
            Set(presenter, "_titleCard", title); Set(presenter, "_titleText", titleText);

            var skip = BottomPanel("SkipPrompt", canvasObject.transform, new Vector2(620, 62), 24);
            var skipLabel = Text("Label", skip.transform, "MANTENER [ ESC ] PARA OMITIR", 18, TextAlignmentOptions.Center);
            Stretch(skipLabel.rectTransform, 10);
            var progressObject = new GameObject("HoldProgress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(skip.transform, false);
            var progressRect = (RectTransform)progressObject.transform;
            progressRect.anchorMin = new Vector2(0, 0); progressRect.anchorMax = new Vector2(1, 0);
            progressRect.pivot = new Vector2(.5f, 0); progressRect.anchoredPosition = new Vector2(0, 2); progressRect.sizeDelta = new Vector2(-18, 4);
            var progress = progressObject.GetComponent<Image>();
            progress.color = new Color(.75f, .6f, .25f); progress.type = Image.Type.Filled; progress.fillMethod = Image.FillMethod.Horizontal; progress.fillAmount = 0;
            var skipPresenter = skip.AddComponent<CinematicSkipPromptPresenter>();
            Set(skipPresenter, "_sequence", sequence); Set(skipPresenter, "_devices", devices); Set(skipPresenter, "_label", skipLabel); Set(skipPresenter, "_progress", progress);

            date.SetActive(false); narrative.SetActive(false); subtitle.SetActive(false); title.SetActive(false);
            return new UiObjects(fadeGroup, dateGroup, narrativeGroup, presenter, skipPresenter);
        }

        static TimelineAsset RebuildTimeline(PlayableDirector director, IntroSequenceController sequence, Animator explorer, Animator map, UiObjects ui, Camera[] cameras, AudioObjects audio)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = "TL_Intro_AncientMap";
                AssetDatabase.CreateAsset(timeline, TimelinePath);
            }
            else
            {
                foreach (var track in timeline.GetRootTracks().ToArray()) timeline.DeleteTrack(track);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(TimelinePath))
                    if (asset != null && asset != timeline) UnityEngine.Object.DestroyImmediate(asset, true);
            }
            timeline.editorSettings.frameRate = 30;
            director.playableAsset = timeline;

            var cameraGroup = timeline.CreateTrack<GroupTrack>(null, "Cameras");
            var ranges = new[]
            {
                new[] { new Range(0, 35) },
                new[] { new Range(35, 45) },
                new[] { new Range(45, 48) },
                new[] { new Range(53, 58) },
                new[] { new Range(48, 53), new Range(58, 65) },
                new[] { new Range(65, 80) },
                new[] { new Range(87, 94) },
                new[] { new Range(80, 87), new Range(94, 97) }
            };
            for (var index = 0; index < cameras.Length; index++)
            {
                var track = timeline.CreateTrack<ActivationTrack>(cameraGroup, cameras[index].name);
                foreach (var range in ranges[index])
                {
                    var clip = track.CreateDefaultClip();
                    clip.start = range.Start;
                    clip.duration = range.End - range.Start;
                }
                director.SetGenericBinding(track, cameras[index].gameObject);
            }

            var blockingGroup = timeline.CreateTrack<GroupTrack>(null, "Blocking");
            AddAnimation(timeline, director, blockingGroup, "ExplorerAnchor Movement", explorer,
                PositionClip("Explorer_Blocking", new[]
                {
                    Key(0, new Vector3(0, 0, -5)), Key(35, new Vector3(0, 0, -5)), Key(45, new Vector3(.1f, 0, 9.8f)),
                    Key(80, new Vector3(.1f, 0, 9.8f)), Key(87, new Vector3(0, 0, 15)), Key(91, new Vector3(0, 0, 15)), Key(97, new Vector3(0, 0, 24))
                }));
            AddAnimation(timeline, director, blockingGroup, "AncientMap Handoff", map,
                PositionClip("AncientMap_Handoff", new[]
                {
                    Key(0, new Vector3(1.75f, 1.55f, 11.55f)), Key(65, new Vector3(1.75f, 1.55f, 11.55f)),
                    Key(71, new Vector3(.55f, 1.48f, 10.8f)), Key(80, new Vector3(.2f, 1.3f, 10.3f)), Key(85, new Vector3(.2f, -.5f, 10.3f)), Key(97, new Vector3(.2f, -.5f, 10.3f))
                }));
            var fadeClip = ScalarClip("Cinematic_Fade", typeof(CanvasGroup), "m_Alpha", new[]
            {
                new Keyframe(0, 1), new Keyframe(23.5f, 1), new Keyframe(25, 0), new Keyframe(95.5f, 0), new Keyframe(97, 1)
            });
            AddAnimation(timeline, director, blockingGroup, "Fade", ui.Fade.GetComponent<Animator>(), fadeClip);
            AddAnimation(timeline, director, blockingGroup, "DateCard Fade", ui.Date.GetComponent<Animator>(),
                ScalarClip("DateCard_Fade", typeof(CanvasGroup), "m_Alpha", new[]
                {
                    new Keyframe(0, 0), new Keyframe(1.5f, 0), new Keyframe(1.9f, 1),
                    new Keyframe(4.1f, 1), new Keyframe(4.5f, 0), new Keyframe(97, 0)
                }));
            AddAnimation(timeline, director, blockingGroup, "NarrativeText Fade", ui.Narrative.GetComponent<Animator>(),
                ScalarClip("NarrativeText_Fade", typeof(CanvasGroup), "m_Alpha", new[]
                {
                    new Keyframe(0, 0), new Keyframe(6, 0), new Keyframe(6.6f, 1),
                    new Keyframe(12.5f, 1), new Keyframe(13.5f, 0), new Keyframe(15, 0),
                    new Keyframe(15.6f, 1), new Keyframe(22.5f, 1), new Keyframe(23.5f, 0), new Keyframe(97, 0)
                }));

            var narrativeGroup = timeline.CreateTrack<GroupTrack>(null, "Narrative");
            var cues = timeline.CreateTrack<CinematicCueTrack>(narrativeGroup, "Cinematic Cues");
            director.SetGenericBinding(cues, sequence);
            AddCue(cues, "date", CinematicCueType.Date, 1.5, 3, string.Empty, "1500 d.C.");
            AddCue(cues, NarratorCueOneId, CinematicCueType.Narrative, 6, 7.5, string.Empty, "Bajo la inmensidad de la selva descansaban\nlas ruinas de una ciudad olvidada.");
            AddCue(cues, NarratorCueTwoId, CinematicCueType.Narrative, 15, 8.5, string.Empty, "Se decía que en algún lugar de ella permanecía\noculta una fuente de conocimiento.");
            AddCue(cues, "explorer-question", CinematicCueType.Subtitle, 45.4, 2.3, "EXPLORADOR", "¿Qué hace aquí?");
            AddCue(cues, "elder-same", CinematicCueType.Subtitle, 48.2, 2.1, "ANCIANO", "Podría preguntarte lo mismo.");
            AddCue(cues, "elder-destination", CinematicCueType.Subtitle, 50.6, 2.2, "ANCIANO", "¿A dónde te diriges?");
            AddCue(cues, "explorer-source", CinematicCueType.Subtitle, 53.4, 3.1, "EXPLORADOR", "Busco la Fuente del Conocimiento.");
            AddCue(cues, "elder-stories", CinematicCueType.Subtitle, 57.4, 2.4, "ANCIANO", "He oído historias sobre ella.");
            AddCue(cues, "elder-brave", CinematicCueType.Subtitle, 60.1, 2.5, "ANCIANO", "Pocos valientes se atreven a buscarla.");
            AddCue(cues, "elder-way", CinematicCueType.Subtitle, 63, 1.8, "ANCIANO", "Y pocos conocen el camino.");
            AddCue(cues, "elder-take", CinematicCueType.Subtitle, 66, 1.4, "ANCIANO", "Toma.");
            AddCue(cues, "map-received", CinematicCueType.AncientMapReceived, 71, .25, string.Empty, string.Empty);
            AddCue(cues, "elder-map", CinematicCueType.Subtitle, 69, 2.3, "ANCIANO", "Te ayudará a no perderte.");
            AddCue(cues, "elder-hidden-path", CinematicCueType.Subtitle, 72, 3.4, "ANCIANO", "Pero el camino que buscas no está a simple vista.");
            AddCue(cues, "elder-hidden-entrance", CinematicCueType.Subtitle, 76, 3.2, "ANCIANO", "Debes encontrar una entrada oculta.");
            AddCue(cues, "explorer-thanks", CinematicCueType.Subtitle, 80.8, 1.5, "EXPLORADOR", "Gracias.");
            AddCue(cues, "elder-remember", CinematicCueType.Subtitle, 87.4, 2.1, "ANCIANO", "Y recuerda...");
            AddCue(cues, "elder-warning", CinematicCueType.Subtitle, 90.2, 3.8, "ANCIANO", "No confíes en el sistema.");
            AddCue(cues, "title", CinematicCueType.Title, 95.8, 1.2, string.Empty, "TIKAL: EL PORTAL");

            var audioGroup = timeline.CreateTrack<GroupTrack>(null, "Audio Slots");
            var ambience = timeline.CreateTrack<AudioTrack>(audioGroup, "Ambience");
            director.SetGenericBinding(ambience, audio.Ambience);
            var ambienceClip = FindJungleAmbience();
            if (ambienceClip != null)
            {
                var clip = ambience.CreateClip(ambienceClip);
                clip.start = 0; clip.duration = Duration; clip.easeInDuration = 4; clip.easeOutDuration = 2;
                ((AudioPlayableAsset)clip.asset).loop = true;
            }
            timeline.CreateTrack<AudioTrack>(audioGroup, "Music");
            timeline.CreateTrack<AudioTrack>(audioGroup, "Voice");
            timeline.CreateTrack<AudioTrack>(audioGroup, "VOICE_NARRATOR");
            timeline.CreateTrack<AudioTrack>(audioGroup, "SFX");
            AddAnimation(timeline, director, audioGroup, "Ambience Mix", audio.Ambience.GetComponent<Animator>(),
                ScalarClip("Ambience_Mix", typeof(AudioSource), "m_Volume", new[]
                {
                    new Keyframe(0, 0), new Keyframe(4, .55f), new Keyframe(87, .55f),
                    new Keyframe(89.5f, .35f), new Keyframe(94, .35f), new Keyframe(95, .5f), new Keyframe(97, 0)
                }));
            EditorUtility.SetDirty(timeline);
            return timeline;
        }

        static void AddCue(CinematicCueTrack track, string id, CinematicCueType type, double start, double duration, string speaker, string text)
        {
            var clip = track.CreateDefaultClip();
            clip.displayName = string.IsNullOrEmpty(speaker) ? id : speaker + ": " + text;
            clip.start = start;
            clip.duration = duration;
            var cue = (CinematicCueAsset)clip.asset;
            Set(cue, "_cueId", id); Set(cue, "_type", (int)type); Set(cue, "_speaker", speaker); Set(cue, "_text", text);
        }

        static void AddAnimation(TimelineAsset timeline, PlayableDirector director, TrackAsset parent, string name, Animator animator, AnimationClip animation)
        {
            animation.frameRate = 30;
            animation.wrapMode = WrapMode.ClampForever;
            AssetDatabase.AddObjectToAsset(animation, timeline);
            var track = timeline.CreateTrack<AnimationTrack>(parent, name);
            var clip = track.CreateClip(animation);
            clip.start = 0;
            clip.duration = Duration;
            director.SetGenericBinding(track, animator);
        }

        static AnimationClip PositionClip(string name, PositionKey[] keys)
        {
            var clip = new AnimationClip { name = name };
            SetCurve(clip, typeof(Transform), "m_LocalPosition.x", keys.Select(item => new Keyframe(item.Time, item.Value.x)).ToArray());
            SetCurve(clip, typeof(Transform), "m_LocalPosition.y", keys.Select(item => new Keyframe(item.Time, item.Value.y)).ToArray());
            SetCurve(clip, typeof(Transform), "m_LocalPosition.z", keys.Select(item => new Keyframe(item.Time, item.Value.z)).ToArray());
            return clip;
        }

        static AnimationClip ScalarClip(string name, Type type, string property, Keyframe[] keys)
        {
            var clip = new AnimationClip { name = name };
            SetCurve(clip, type, property, keys);
            return clip;
        }

        static void SetCurve(AnimationClip clip, Type type, string property, Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            foreach (var key in keys) AnimationUtility.SetKeyLeftTangentMode(curve, Array.IndexOf(keys, key), AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, type, property), curve);
        }

        static void BuildLighting(Transform parent)
        {
            var sun = new GameObject("Soft Jungle Light", typeof(Light));
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(48, -34, 0);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.05f; light.color = new Color(.84f, .9f, .75f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.18f, .25f, .22f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.18f, .26f, .23f); RenderSettings.fogDensity = .012f;
        }

        static void Humanoid(Transform parent, Material material, bool seated)
        {
            Shape("Torso", parent, PrimitiveType.Capsule, new Vector3(0, seated ? .8f : 1.05f, 0), new Vector3(.55f, seated ? .58f : .75f, .42f), material);
            Shape("Head", parent, PrimitiveType.Sphere, new Vector3(0, seated ? 1.65f : 2.05f, 0), new Vector3(.42f, .48f, .42f), material);
            Shape("Left arm", parent, PrimitiveType.Capsule, new Vector3(-.48f, seated ? .9f : 1.1f, .05f), new Vector3(.2f, .55f, .2f), material);
            Shape("Right arm", parent, PrimitiveType.Capsule, new Vector3(.48f, seated ? .9f : 1.1f, .05f), new Vector3(.2f, .55f, .2f), material);
            if (!seated)
            {
                Shape("Left leg", parent, PrimitiveType.Capsule, new Vector3(-.22f, .35f, 0), new Vector3(.22f, .65f, .22f), material);
                Shape("Right leg", parent, PrimitiveType.Capsule, new Vector3(.22f, .35f, 0), new Vector3(.22f, .65f, .22f), material);
            }
        }

        static GameObject FullPanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return go;
        }

        static GameObject CenterPanel(string name, Transform parent, Vector2 size)
        {
            var panel = Panel(name, parent, new Vector2(.5f, .5f), Vector2.zero, size);
            panel.GetComponent<Image>().color = new Color(.015f, .025f, .02f, .72f);
            return panel;
        }

        static GameObject BottomPanel(string name, Transform parent, Vector2 size, float bottom)
        {
            var panel = Panel(name, parent, new Vector2(.5f, 0), new Vector2(0, bottom), size);
            panel.GetComponent<Image>().color = new Color(.015f, .025f, .02f, .84f);
            return panel;
        }

        static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        static TMP_Text Text(string name, Transform parent, string content, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>(); text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath); text.text = content; text.fontSize = size;
            text.color = new Color(.96f, .94f, .86f); text.alignment = alignment; text.raycastTarget = false; text.richText = false;
            return text;
        }

        static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        static Renderer Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; return renderer;
        }

        static Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }

        static Material Material(string file) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/" + file);
        static PositionKey Key(float time, Vector3 value) => new PositionKey(time, value);

        static AudioClip FindJungleAmbience()
        {
            return AssetDatabase.FindAssets("AUD_AMB_Jungle_Main_01 t:AudioClip")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), "AUD_AMB_Jungle_Main_01", StringComparison.OrdinalIgnoreCase))
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .FirstOrDefault(clip => clip != null);
        }

        static void RegisterScene()
        {
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath && item.path != PlaceholderPath).ToList();
            var mainIndex = scenes.FindIndex(item => item.path == SceneCatalog.Get(GameSceneIds.MainMenu).Path);
            var insert = mainIndex < 0 ? 0 : mainIndex + 1;
            scenes.Insert(insert, new EditorBuildSettingsScene(ScenePath, true));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlaceholderPath) != null)
                scenes.Insert(insert + 1, new EditorBuildSettingsScene(PlaceholderPath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Set(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException("Unknown serialized field " + field + " on " + target.GetType().Name);
            switch (value)
            {
                case string text: property.stringValue = text; break;
                case int number: property.intValue = number; break;
                case float scalar: property.floatValue = scalar; break;
                default: property.objectReferenceValue = (UnityEngine.Object)value; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        readonly struct CameraDefinition
        {
            public CameraDefinition(string name, Vector3 position, Vector3 target, float fieldOfView) { Name = name; Position = position; Target = target; FieldOfView = fieldOfView; }
            public string Name { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
        }

        readonly struct Range
        {
            public Range(double start, double end) { Start = start; End = end; }
            public double Start { get; }
            public double End { get; }
        }

        readonly struct PositionKey
        {
            public PositionKey(float time, Vector3 value) { Time = time; Value = value; }
            public float Time { get; }
            public Vector3 Value { get; }
        }

        readonly struct UiObjects
        {
            public UiObjects(CanvasGroup fade, CanvasGroup date, CanvasGroup narrative, CinematicSubtitlePresenter subtitles, CinematicSkipPromptPresenter skip)
            {
                Fade = fade; Date = date; Narrative = narrative; Subtitles = subtitles; Skip = skip;
            }
            public CanvasGroup Fade { get; }
            public CanvasGroup Date { get; }
            public CanvasGroup Narrative { get; }
            public CinematicSubtitlePresenter Subtitles { get; }
            public CinematicSkipPromptPresenter Skip { get; }
        }

        readonly struct AudioObjects
        {
            public AudioObjects(AudioSource ambience, AudioSource music, AudioSource voice, AudioSource sfx)
            {
                Ambience = ambience; Music = music; Voice = voice; Sfx = sfx;
            }
            public AudioSource Ambience { get; }
            public AudioSource Music { get; }
            public AudioSource Voice { get; }
            public AudioSource Sfx { get; }
        }
    }
}
