using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tutorial;
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
    public static class GameplayPauseMenuAuthoring
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/GameplayPauseMenu.prefab";
        public static readonly string[] Scenes = {
            "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity",
            "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity" };
        [MenuItem("Tikal/Authoring/Apply Gameplay Pause Menu")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Apply Pause authoring outside Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildPrefab();
                foreach (var path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    Apply(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_GAMEPLAY_PAUSE_AUTHORED: Tutorial + Level01. Run validation and interactive playtest.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        internal static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        internal static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static void Apply(Scene scene)
        {
            var originalCanvasCount = Components<Canvas>(scene).Length;
            foreach (var old in Components<PauseMenuController>(scene)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var player = Components<PlayerController>(scene).Single();
            var canvas = Components<ReticlePresenter>(scene).Single().GetComponentInParent<Canvas>();
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform);
            Stretch((RectTransform)root.transform);
            root.transform.SetAsLastSibling();
            var pause = root.GetComponent<PauseMenuController>();
            var flows = Components<GameFlowController>(scene);
            var flow = flows.Length > 0 ? flows.Single() : root.AddComponent<GameFlowController>();
            var events = Components<EventSystem>(scene);
            if (events.Length > 1) throw new InvalidOperationException("Resolve duplicated EventSystems before Pause authoring.");
            var eventSystem = events.Length == 1 ? events[0] : new GameObject("Gameplay EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) ConfigureInput(eventSystem);
            Set(pause, "_player", player); Set(pause, "_input", player.GetComponent<PlayerInputReader>());
            Set(pause, "_devices", player.GetComponent<InputDeviceManager>()); Set(pause, "_flow", flow);
            Set(pause, "_events", eventSystem); Set(pause, "_journal", Components<ClueJournalPresenter>(scene).SingleOrDefault());
            Set(pause, "_outcome", Components<LevelOutcomeController>(scene).SingleOrDefault());
            var tutorial = Components<TutorialSequenceController>(scene).SingleOrDefault();
            Set(pause, "_tutorial", tutorial);
            var serialized = new SerializedObject(pause);
            serialized.FindProperty("_restartTutorialFromStart").boolValue = tutorial != null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var settings = root.GetComponentInChildren<SettingsMenuController>(true);
            Set(settings, "_runtimeApplier", Components<PersistentSettingsApplier>(scene).Single());
            if (Components<Canvas>(scene).Length != originalCanvasCount)
                throw new InvalidOperationException("Pause authoring must reuse the existing Canvas count.");
        }
        private static void ConfigureInput(EventSystem events)
        {
            var module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            const string path = "Assets/_Project/Input/TikalInputActions.inputactions";
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            InputActionReference Ref(string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>()
                .Single(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == name);
            module.move = Ref("Navigate"); module.submit = Ref("Submit"); module.cancel = Ref("Cancel");
            module.point = Ref("Point"); module.leftClick = Ref("Click"); module.scrollWheel = Ref("ScrollWheel");
            module.rightClick = module.middleClick = module.trackedDevicePosition = module.trackedDeviceOrientation = null;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        }
        private static GameObject Rect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false); Stretch((RectTransform)root.transform); return root;
        }
        private static TMP_Text Label(string name, Transform parent, string value, float y, float height, int size = 26)
        {
            var root = Rect(name, parent);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(900, height); rect.anchoredPosition = new Vector2(0, y);
            var text = root.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");
            text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.96f, .93f, .84f); text.raycastTarget = false; text.richText = false;
            return text;
        }
        private static Button Button(Transform parent, string name, string title, float y)
        {
            var root = Rect(name, parent);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(700, 50); rect.anchoredPosition = new Vector2(0, y);
            var image = root.AddComponent<Image>(); var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Label("Label", root.transform, title, 0, 50, 22);
            text.rectTransform.sizeDelta = new Vector2(660, 50);
            var focus = Rect("Focus", root.transform); focus.AddComponent<Image>().color = new Color(.86f, .62f, .27f);
            var f = (RectTransform)focus.transform; f.anchorMin = new Vector2(0, 0); f.anchorMax = new Vector2(0, 1); f.sizeDelta = new Vector2(4, 0);
            root.AddComponent<MenuSelectableVisual>().Configure(button, image, text, focus, null, name == "Continue");
            return button;
        }
        private static void BuildPrefab()
        {
            var mainMenu = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Menus/MainMenu.unity");
            var root = Rect("GameplayPauseMenu", null);
            root.SetActive(false);
            try
            {
                var pause = root.AddComponent<PauseMenuController>();
                var overlay = Rect("PauseOverlay", root.transform);
                overlay.AddComponent<Image>().color = new Color(.018f, .035f, .055f, .98f);
                var main = Rect("PauseMain", overlay.transform);
                Label("Title", main.transform, "PAUSA", 310, 70, 42).color = new Color(.86f, .62f, .27f);
                var fields = new[] { "_continue", "_configuration", "_controls", "_restart", "_mainMenu", "_quit" };
                var names = new[] { "Continue", "Settings", "Controls", "Restart", "MainMenu", "Exit" };
                var titles = new[] { "CONTINUAR", "CONFIGURACIÓN", "CONTROLES", "REINICIAR DESDE CHECKPOINT", "VOLVER AL MENÚ PRINCIPAL", "SALIR" };
                for (var i = 0; i < fields.Length; i++) Set(pause, fields[i], Button(main.transform, names[i], titles[i], 200-i*64));
                Set(pause, "_restartHint", Label("RestartHint", main.transform, string.Empty, -210, 60, 18));
                // Reuse the materialized Main Menu controls, dropdown templates and visual language.
                var original = Components<SettingsMenuController>(mainMenu).Single();
                var settings = UnityEngine.Object.Instantiate(original.gameObject, overlay.transform, false);
                settings.name = "RuntimeSettings"; Stretch((RectTransform)settings.transform);
                var settingsController = settings.GetComponent<SettingsMenuController>();
                Set(settingsController, "_previewReticle", null); Set(settingsController, "_runtimeApplier", null);
                var back = settings.GetComponentsInChildren<Button>(true).Single(b => b.name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0);
                back.onClick.RemoveAllListeners();
                // Sliders consume Left/Right for adjustment; Up/Down visits every setting.
                // Do not inherit Main Menu links that route slider arrows to another column.
                var settingsSerialized = new SerializedObject(settingsController);
                var selections = new[] { "_masterVolume", "_musicVolume", "_sfxVolume", "_mouseSensitivity", "_gamepadSensitivity", "_invertY", "_cameraMode", "_reticleMode" }
                    .Select(field => (Selectable)settingsSerialized.FindProperty(field).objectReferenceValue).Concat(new Selectable[] { back }).ToArray();
                for (var i = 0; i < selections.Length; i++)
                    selections[i].navigation = new Navigation {
                        mode = Navigation.Mode.Explicit,
                        selectOnUp = selections[Mathf.Max(0, i-1)], selectOnDown = selections[Mathf.Min(selections.Length-1, i+1)],
                        selectOnLeft = selections[i] is Slider ? null : selections[i],
                        selectOnRight = selections[i] is Slider ? null : selections[i] };
                Set(pause, "_settings", settingsController); Set(pause, "_settingsBack", back);
                var controls = Rect("ControlsPanel", overlay.transform);
                Label("Title", controls.transform, "CONTROLES", 310, 70, 38).color = new Color(.86f, .62f, .27f);
                Set(pause, "_controlsText", Label("ControlsBody", controls.transform, string.Empty, 35, 440));
                Set(pause, "_controlsBack", Button(controls.transform, "ControlsBack", "VOLVER", -250));
                var hints = Label("Hints", overlay.transform, string.Empty, 0, 46, 20);
                hints.rectTransform.anchorMin = hints.rectTransform.anchorMax = new Vector2(.5f, .06f);
                Set(pause, "_hints", hints);
                Set(pause, "_overlay", overlay); Set(pause, "_mainPanel", main); Set(pause, "_settingsPanel", settings); Set(pause, "_controlsPanel", controls);
                settings.SetActive(false); controls.SetActive(false); overlay.SetActive(false);
                root.SetActive(true);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(mainMenu); }
        }
    }
}
