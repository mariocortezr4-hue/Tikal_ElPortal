using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Persistence;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class MainMenuFlowAuthoring
    {
        const string MainMenuPath = "Assets/_Project/Scenes/Menus/MainMenu.unity";
        const string IntroPath = "Assets/_Project/Scenes/Core/IntroPlaceholder.unity";
        const string TutorialPath = "Assets/_Project/Scenes/Core/TutorialPlaceholder.unity";
        const string Level01Path = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";

        [MenuItem("Tikal/Authoring/Generate Main Menu Flow")]
        public static void RunFromMenu() => Run();

        static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new Exception("Unknown property " + field);
            switch (value)
            {
                case string text:
                    property.stringValue = text;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float scalar:
                    property.floatValue = scalar;
                    break;
                case UnityEngine.Object[] objects:
                    property.arraySize = objects.Length;
                    for (var index = 0; index < objects.Length; index++)
                        property.GetArrayElementAtIndex(index).objectReferenceValue = objects[index];
                    break;
                default:
                    property.objectReferenceValue = (UnityEngine.Object)value;
                    break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");

        static GameObject Root(string name)
        {
            var root = new GameObject(name);
            root.transform.position = Vector3.zero;
            return root;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static TMP_Text Text(string name, Transform parent, string value, int size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.font = Font();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(.92f, .88f, .76f);
            text.raycastTarget = false;
            return text;
        }

        static Button Button(string name, Transform parent, string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.color = new Color(.12f, .14f, .12f, .92f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", root.transform, label, 22);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return button;
        }

        static void Place(RectTransform rect, float y, float height = 48, float width = 360)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0, y);
        }

        static InputActionReference ActionReference(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Input/TikalInputActions.inputactions")
            .OfType<InputActionReference>().First(reference => reference.action != null && reference.action.actionMap.name == "UI" && reference.action.name == name);

        static EventSystem EventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            var events = go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");
            module.move = ActionReference("Navigate");
            module.submit = ActionReference("Submit");
            module.cancel = ActionReference("Cancel");
            module.point = ActionReference("Point");
            module.leftClick = ActionReference("Click");
            module.scrollWheel = ActionReference("ScrollWheel");
            module.rightClick = null;
            module.middleClick = null;
            module.trackedDevicePosition = null;
            module.trackedDeviceOrientation = null;
            return events;
        }

        public static void Run()
        {
            try
            {
                Directory.CreateDirectory("Assets/_Project/Scenes/Menus");
                Directory.CreateDirectory("Assets/_Project/Scenes/Core");
                BuildMainMenu();
                BuildPlaceholder(IntroPath, "Intro Placeholder", "INTRO PLACEHOLDER", "Intro terminada", true);
                BuildPlaceholder(TutorialPath, "Tutorial Placeholder", "TUTORIAL PLACEHOLDER", "Tutorial terminado", false);
                RegisterScenes();
                AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_MAIN_MENU_FLOW_AUTHORED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Root("MainMenu");
            var flow = root.AddComponent<GameFlowController>();
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = new Color(.03f, .04f, .035f, 1);
            Text("Title", background, "TIKAL: EL PORTAL", 48).rectTransform.anchorMin = new Vector2(.1f, .82f);
            var events = EventSystem(root.transform);
            var main = Panel("Main", background, true);
            var levels = Panel("Levels", background, false);
            var character = Panel("CharacterSelection", background, false);
            var cinematics = Panel("Cinematics", background, false);
            var tutorial = Panel("Tutorial", background, false);
            var settings = Panel("Settings", background, false);
            var credits = Panel("Credits", background, false);

            var play = MenuButton(main, "Play", "JUGAR", 220);
            var playText = play.GetComponentInChildren<TMP_Text>();
            var subtitle = Text("ContinueSubtitle", main, "", 18);
            Place(subtitle.rectTransform, 170, 32, 420);
            var levelsButton = MenuButton(main, "Levels", "NIVELES", 105);
            var characterButton = MenuButton(main, "Character", "PERSONAJE", 45);
            var cinematicsButton = MenuButton(main, "Cinematics", "CINEMATICAS", -15);
            var tutorialButton = MenuButton(main, "Tutorial", "TUTORIAL", -75);
            var settingsButton = MenuButton(main, "Settings", "CONFIGURACION", -135);
            var creditsButton = MenuButton(main, "Credits", "CREDITOS", -195);
            var quitButton = MenuButton(main, "Quit", "SALIR", -255);

            Text("LevelsTitle", levels, "NIVELES", 32);
            var level01 = MenuButton(levels, "Level01", "Nivel 1 - La Entrada Secreta", 80, 460);
            var level02Row = MenuButton(levels, "Level02", "Nivel 2", 15, 460).gameObject;
            var levelsBack = BackButton(levels, -220);

            Text("CharacterTitle", character, "PERSONAJE", 32);
            var selectedCharacter = Text("SelectedCharacter", character, "Seleccionado: Personaje 1", 20);
            Place(selectedCharacter.rectTransform, 150, 34, 460);
            var character01 = CharacterButton(character, "Character01", "Personaje 1", -150, 70);
            var character02 = CharacterButton(character, "Character02", "Personaje 2", -50, 70);
            var character03 = CharacterButton(character, "Character03", "Personaje 3", 50, 70);
            var character04 = CharacterButton(character, "Character04", "Personaje 4", 150, 70);
            var confirmCharacter = MenuButton(character, "ConfirmCharacter", "CONFIRMAR", 0, 280);
            Place((RectTransform)confirmCharacter.transform, -105, 48, 280);
            var characterBack = BackButton(character, -220);
            var characterController = character.gameObject.AddComponent<CharacterSelectionController>();
            Set(characterController, "_characterButtons", new UnityEngine.Object[] { character01, character02, character03, character04 });
            Set(characterController, "_characterLabels", new UnityEngine.Object[]
            {
                character01.GetComponentInChildren<TMP_Text>(),
                character02.GetComponentInChildren<TMP_Text>(),
                character03.GetComponentInChildren<TMP_Text>(),
                character04.GetComponentInChildren<TMP_Text>()
            });
            Set(characterController, "_selectionFrames", new UnityEngine.Object[]
            {
                character01.GetComponent<Image>(),
                character02.GetComponent<Image>(),
                character03.GetComponent<Image>(),
                character04.GetComponent<Image>()
            });
            Set(characterController, "_currentSelectionLabel", selectedCharacter);
            Set(characterController, "_confirmButton", confirmCharacter);

            Text("CinematicsTitle", cinematics, "CINEMATICAS", 32);
            var intro = MenuButton(cinematics, "Intro", "Introduccion", 80, 360);
            var cinematicsBack = BackButton(cinematics, -220);

            Text("TutorialTitle", tutorial, "TUTORIAL", 32);
            var replayTutorial = MenuButton(tutorial, "ReplayTutorial", "Repetir tutorial", 80, 360);
            var tutorialBack = BackButton(tutorial, -220);

            Text("SettingsTitle", settings, "CONFIGURACION", 32);
            var settingsController = settings.gameObject.AddComponent<SettingsMenuController>();
            var master = Slider(settings, "Master", "Master", 120, 0, 1, 1);
            var music = Slider(settings, "Music", "Music", 65, 0, 1, .8f);
            var sfx = Slider(settings, "SFX", "SFX", 10, 0, 1, 1);
            var mouse = Slider(settings, "Mouse", "Mouse Sensitivity", -45, .01f, .5f, .12f);
            var gamepad = Slider(settings, "Gamepad", "Gamepad Sensitivity", -100, 40, 260, 150);
            var invert = Toggle(settings, "InvertY", "Invert Y", -155);
            var reticle = Dropdown(settings, "Reticle", "Reticle", -210, new[] { "None", "Dot", "Cross" }, 1);
            var settingsBack = BackButton(settings, -280);
            Set(settingsController, "_masterVolume", master);
            Set(settingsController, "_musicVolume", music);
            Set(settingsController, "_sfxVolume", sfx);
            Set(settingsController, "_mouseSensitivity", mouse);
            Set(settingsController, "_gamepadSensitivity", gamepad);
            Set(settingsController, "_invertY", invert);
            Set(settingsController, "_reticleMode", reticle);

            Text("CreditsTitle", credits, "CREDITOS", 32);
            var creditsBody = Text("CreditsBody", credits, "Creditos provisionales", 22);
            Place(creditsBody.rectTransform, 80, 80, 500);
            var creditsBack = BackButton(credits, -220);

            var menu = root.AddComponent<MainMenuController>();
            Set(menu, "_flow", flow);
            Set(menu, "_eventSystem", events);
            Set(menu, "_mainPanel", main.gameObject);
            Set(menu, "_levelsPanel", levels.gameObject);
            Set(menu, "_characterPanel", character.gameObject);
            Set(menu, "_cinematicsPanel", cinematics.gameObject);
            Set(menu, "_tutorialPanel", tutorial.gameObject);
            Set(menu, "_settingsPanel", settings.gameObject);
            Set(menu, "_creditsPanel", credits.gameObject);
            Set(menu, "_playLabel", playText);
            Set(menu, "_continueSubtitle", subtitle);
            Set(menu, "_playButton", play);
            Set(menu, "_levelsButton", levelsButton);
            Set(menu, "_characterButton", characterButton);
            Set(menu, "_cinematicsButton", cinematicsButton);
            Set(menu, "_tutorialButton", tutorialButton);
            Set(menu, "_settingsButton", settingsButton);
            Set(menu, "_creditsButton", creditsButton);
            Set(menu, "_quitButton", quitButton);
            Set(menu, "_level01Button", level01);
            Set(menu, "_level02Row", level02Row);
            Set(menu, "_introReplayButton", intro);
            Set(menu, "_tutorialReplayButton", replayTutorial);
            Set(menu, "_backButtons", new UnityEngine.Object[] { levelsBack, characterBack, cinematicsBack, tutorialBack, settingsBack, creditsBack });
            Set(menu, "_settings", settingsController);
            Set(menu, "_characterSelection", characterController);
            var cancel = root.AddComponent<MenuCancelRouter>();
            Set(cancel, "_menu", menu);
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        static RectTransform Panel(string name, RectTransform parent, bool active)
        {
            var panel = Rect(name, parent, Vector2.zero, Vector2.one);
            panel.gameObject.SetActive(active);
            return panel;
        }

        static Button MenuButton(Transform parent, string name, string label, float y, float width = 360)
        {
            var button = Button(name, parent, label);
            Place((RectTransform)button.transform, y, 48, width);
            return button;
        }

        static Button BackButton(Transform parent, float y) => MenuButton(parent, "Back", "VOLVER", y, 260);

        static Button CharacterButton(Transform parent, string name, string label, float x, float y)
        {
            var button = Button(name, parent, label);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(180, 82);
            rect.anchoredPosition = new Vector2(x, y);
            return button;
        }

        static Slider Slider(Transform parent, string name, string label, float y, float min, float max, float value)
        {
            Text(name + "Label", parent, label, 18, TextAlignmentOptions.Left).rectTransform.anchoredPosition = new Vector2(-350, y);
            var slider = new GameObject(name, typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
            slider.transform.SetParent(parent, false);
            Place((RectTransform)slider.transform, y, 34, 340);
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            var background = Rect("Background", slider.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = new Color(.08f, .09f, .08f);
            var fill = Rect("Fill", background, Vector2.zero, new Vector2(.6f, 1));
            fill.gameObject.AddComponent<Image>().color = new Color(.45f, .58f, .38f);
            slider.fillRect = fill;
            var handle = Rect("Handle", slider.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            handle.sizeDelta = new Vector2(20, 30);
            slider.targetGraphic = handle.gameObject.AddComponent<Image>();
            slider.handleRect = handle;
            return slider;
        }

        static Toggle Toggle(Transform parent, string name, string label, float y)
        {
            var toggle = new GameObject(name, typeof(RectTransform), typeof(Toggle)).GetComponent<Toggle>();
            toggle.transform.SetParent(parent, false);
            Place((RectTransform)toggle.transform, y, 34, 340);
            var background = Rect("Box", toggle.transform, new Vector2(0, .5f), new Vector2(0, .5f));
            background.sizeDelta = new Vector2(28, 28);
            var box = background.gameObject.AddComponent<Image>();
            box.color = new Color(.08f, .09f, .08f);
            var check = Rect("Check", background, Vector2.zero, Vector2.one);
            check.offsetMin = new Vector2(5, 5);
            check.offsetMax = new Vector2(-5, -5);
            check.gameObject.AddComponent<Image>().color = new Color(.45f, .58f, .38f);
            toggle.targetGraphic = box;
            toggle.graphic = check.GetComponent<Image>();
            var text = Text("Label", toggle.transform, label, 18, TextAlignmentOptions.Left);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(280, 34);
            rect.anchoredPosition = new Vector2(180, 0);
            return toggle;
        }

        static TMP_Dropdown Dropdown(Transform parent, string name, string label, float y, string[] options, int value)
        {
            Text(name + "Label", parent, label, 18, TextAlignmentOptions.Left).rectTransform.anchoredPosition = new Vector2(-350, y);
            var dropdown = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown)).GetComponent<TMP_Dropdown>();
            dropdown.transform.SetParent(parent, false);
            Place((RectTransform)dropdown.transform, y, 42, 340);
            dropdown.GetComponent<Image>().color = new Color(.08f, .09f, .08f);
            var labelText = Text("Label", dropdown.transform, string.Empty, 18);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = labelText.rectTransform.offsetMax = Vector2.zero;
            dropdown.captionText = labelText;
            dropdown.options = options.Select(option => new TMP_Dropdown.OptionData(option)).ToList();
            dropdown.value = value;
            return dropdown;
        }

        static void BuildPlaceholder(string path, string rootName, string title, string buttonLabel, bool intro)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Root(rootName);
            var flow = root.AddComponent<GameFlowController>();
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Image>().color = new Color(.025f, .03f, .028f, 1);
            var text = Text("Title", background, title, 38);
            Place(text.rectTransform, 90, 80, 600);
            var button = Button("Complete", background, buttonLabel);
            Place((RectTransform)button.transform, -20, 54, 360);
            var action = button.gameObject.AddComponent<PlaceholderFlowButton>();
            Set(action, "_flow", flow);
            Set(action, "_completeIntro", intro);
            var events = EventSystem(root.transform);
            events.SetSelectedGameObject(button.gameObject);
            EditorSceneManager.SaveScene(scene, path);
        }

        static void RegisterScenes()
        {
            var desired = new[] { MainMenuPath, IntroPath, TutorialPath, Level01Path };
            var current = EditorBuildSettings.scenes.ToList();
            var ordered = desired.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
            ordered.AddRange(current.Where(scene => !desired.Contains(scene.path)));
            EditorBuildSettings.scenes = ordered.ToArray();
        }
    }
}
