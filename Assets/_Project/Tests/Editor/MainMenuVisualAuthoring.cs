using System;
using System.Linq;
using Tikal.Input;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    // Applies the visual pass to the existing scene without replacing its controllers or button wiring.
    public static class MainMenuVisualAuthoring
    {
        const string ScenePath = "Assets/_Project/Scenes/Menus/MainMenu.unity";
        const string BackdropPath = "Assets/_Project/UI/Sprites/MainMenuTikalBackdrop.png";

        static readonly Color Ink = MenuVisualPalette.Ink;
        static readonly Color Stone = MenuVisualPalette.Stone;
        static readonly Color Gold = MenuVisualPalette.Gold;
        static readonly Color Ivory = MenuVisualPalette.Ivory;
        static readonly Color Muted = MenuVisualPalette.Muted;

        [MenuItem("Tikal/Authoring/Polish Existing Main Menu")]
        public static void RunFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Polish Main Menu outside Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Apply();
                Debug.Log("TIKAL_MAIN_MENU_VISUALS_AUTHORED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Polish Main Menu outside Play Mode.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(item => item.name == "MainMenu").transform;
            var cameraTransform = root.Find("MenuCamera");
            if (cameraTransform == null)
            {
                var cameraObject = new GameObject("MenuCamera", typeof(Camera));
                cameraObject.transform.SetParent(root, false);
                cameraTransform = cameraObject.transform;
            }
            var menuCamera = cameraTransform.GetComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = Ink;
            menuCamera.cullingMask = 0;
            menuCamera.depth = -100;
            cameraTransform.localPosition = new Vector3(0, 0, -10);
            var background = Find(root, "Canvas/Background").GetComponent<RectTransform>();
            var image = background.GetComponent<Image>();
            image.color = Color.white;
            image.sprite = Backdrop();
            image.raycastTarget = false;

            var shade = Image("LeftShade", background, new Color(.018f, .028f, .021f, .78f));
            Frame(shade.rectTransform, new Vector2(0, 0), new Vector2(.43f, 1));
            shade.transform.SetSiblingIndex(0);
            var bottom = Image("BottomShade", background, new Color(.01f, .025f, .04f, .55f));
            Frame(bottom.rectTransform, Vector2.zero, new Vector2(1, .12f));
            bottom.transform.SetSiblingIndex(1);
            var top = Image("TopRule", background, Gold);
            Frame(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            top.rectTransform.pivot = new Vector2(.5f, 1);
            top.rectTransform.sizeDelta = new Vector2(0, 3);
            top.transform.SetSiblingIndex(2);

            var title = Find(background, "Title").GetComponent<TMP_Text>();
            title.text = "TIKAL";
            Style(title, 80, Gold, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 9;
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(95, -85), new Vector2(640, 100), new Vector2(0, 1));
            var brandSubtitle = Text("BrandSubtitle", background, "EL EXPLORADOR", 32, Ivory);
            brandSubtitle.characterSpacing = 7;
            Anchor(brandSubtitle.rectTransform, new Vector2(0, 1), new Vector2(99, -185), new Vector2(640, 50), new Vector2(0, 1));
            var eyebrow = Text("BrandEyebrow", background, "UNA AVENTURA EN LAS SOMBRAS", 16, Gold);
            Anchor(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(97, -61), new Vector2(610, 24), new Vector2(0, 1));
            var footer = Text("Footer", background, "TIKAL  /  EL EXPLORADOR", 16, Muted);
            Anchor(footer.rectTransform, new Vector2(0, 0), new Vector2(96, 43), new Vector2(550, 25), Vector2.zero);

            var main = Find(background, "Main");
            string[] names = { "Play", "Levels", "Character", "Cinematics", "Tutorial", "Settings", "Credits", "Quit" };
            string[] labels = { null, "NIVELES", "PERSONAJE", "CINEMÁTICAS", "TUTORIAL", "CONFIGURACIÓN", "CRÉDITOS", "SALIR" };
            for (var i = 0; i < names.Length; i++)
            {
                var button = Find(main, names[i]).GetComponent<Button>();
                if (labels[i] != null) button.GetComponentInChildren<TMP_Text>().text = labels[i];
                Anchor((RectTransform)button.transform, new Vector2(0, .5f), new Vector2(95, 145 - i * 61), new Vector2(430, 54), new Vector2(0, .5f));
                StyleButton(button, i == 0);
            }
            var subtitle = Find(main, "ContinueSubtitle").GetComponent<TMP_Text>();
            Style(subtitle, 17, Muted, TextAlignmentOptions.Left);
            Anchor(subtitle.rectTransform, new Vector2(0, .5f), new Vector2(99, 202), new Vector2(520, 30), new Vector2(0, .5f));

            var journey = Image("JourneyCard", main, new Color(.055f, .085f, .065f, .91f));
            Anchor(journey.rectTransform, new Vector2(1, 0), new Vector2(-80, 92), new Vector2(560, 170), new Vector2(1, 0));
            var accent = Image("Accent", journey.transform, Gold);
            Frame(accent.rectTransform, Vector2.zero, new Vector2(0, 1));
            accent.rectTransform.pivot = new Vector2(0, .5f);
            accent.rectTransform.sizeDelta = new Vector2(4, 0);
            var cardKicker = Text("Kicker", journey.transform, "NIVEL 1", 14, Muted);
            cardKicker.characterSpacing = 8;
            Anchor(cardKicker.rectTransform, new Vector2(0, 1), new Vector2(28, -20), new Vector2(505, 24), new Vector2(0, 1));
            var cardHeading = Text("Heading", journey.transform, "LA ENTRADA SECRETA", 28, Gold);
            cardHeading.fontStyle = FontStyles.Bold;
            Anchor(cardHeading.rectTransform, new Vector2(0, 1), new Vector2(28, -48), new Vector2(505, 44), new Vector2(0, 1));
            var cardBody = Text("Body", journey.transform, "Explora las ruinas. Sigue las pistas.\nDescubre lo que duerme tras el portal.", 19, Ivory);
            Anchor(cardBody.rectTransform, new Vector2(0, 1), new Vector2(28, -94), new Vector2(505, 62), new Vector2(0, 1));
            CarvedFrame(journey.transform);

            DecoratePanel(Find(background, "Levels"), "LevelsTitle", "NIVELES");
            DecoratePanel(Find(background, "CharacterSelection"), "CharacterTitle", "PERSONAJE");
            DecoratePanel(Find(background, "Cinematics"), "CinematicsTitle", "CINEMÁTICAS");
            DecoratePanel(Find(background, "Tutorial"), "TutorialTitle", "TUTORIAL");
            DecoratePanel(Find(background, "Settings"), "SettingsTitle", "CONFIGURACIÓN");
            DecoratePanel(Find(background, "Credits"), "CreditsTitle", "CRÉDITOS");

            PolishLevels(Find(background, "Levels"));
            var menu = root.GetComponent<MainMenuController>();
            var menuProperties = new SerializedObject(menu);
            menuProperties.FindProperty("_levelHint").objectReferenceValue = Find(background, "Levels/LevelHint").GetComponent<TMP_Text>();
            menuProperties.ApplyModifiedPropertiesWithoutUndo();
            PolishCharacters(Find(background, "CharacterSelection"));
            PolishSimple(Find(background, "Cinematics"), "Intro");
            PolishSimple(Find(background, "Tutorial"), "ReplayTutorial");
            PolishSettings(Find(background, "Settings"));
            PolishCredits(Find(background, "Credits"));
            ConfigureNavigation(background);
            SetupControlHints(root, background, menu);

            EditorSceneManager.MarkSceneDirty(scene);
            MainMenuVisualValidation.ValidateScene(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Failed to save " + ScenePath);
        }

        static Sprite Backdrop()
        {
            var importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
            if (importer == null) throw new Exception("Main Menu backdrop is missing: " + BackdropPath);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath)
                ?? throw new Exception("Main Menu backdrop did not import as a Sprite");
        }

        static Transform Find(Transform parent, string path) => parent.Find(path)
            ?? throw new Exception("Main Menu element missing: " + path);

        static void Frame(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        static Image Image(string name, Transform parent, Color color)
        {
            var child = parent.Find(name);
            var rect = child != null ? child.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                rect = (RectTransform)go.transform;
            }
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Text(string name, Transform parent, string value, int size, Color color)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            var text = child.GetComponent<TMP_Text>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");
            text.text = value;
            Style(text, size, color, TextAlignmentOptions.Left);
            return text;
        }

        static void Style(TMP_Text text, int size, Color color, TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
        }

        static void StyleButton(Button button, bool primary = false)
        {
            var image = button.GetComponent<Image>();
            image.color = primary ? Gold : Stone;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .89f, .68f);
            colors.selectedColor = new Color(1f, .9f, .72f);
            colors.pressedColor = new Color(.75f, .65f, .53f);
            colors.disabledColor = new Color(.45f, .48f, .5f, .55f);
            colors.colorMultiplier = 1;
            button.colors = colors;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                Style(label, primary ? 24 : 20, primary ? Ink : Ivory, TextAlignmentOptions.Left);
                Frame(label.rectTransform, Vector2.zero, Vector2.one);
                label.rectTransform.offsetMin = new Vector2(25, 0);
                label.rectTransform.offsetMax = new Vector2(-18, 0);
            }
            AttachSelectableVisual(button, image, label, primary, button.name.StartsWith("Character0", StringComparison.Ordinal));
        }

        static void AttachSelectableVisual(Selectable selectable, Graphic background, TMP_Text label, bool primary, bool persistent)
        {
            var focus = Image("FocusIndicator", selectable.transform, Gold);
            Anchor(focus.rectTransform, new Vector2(0, .5f), new Vector2(0, 0), new Vector2(5, 32), new Vector2(0, .5f));
            focus.gameObject.SetActive(false);
            GameObject savedMarker = null;
            if (persistent)
            {
                var saved = Text("SavedMarker", selectable.transform, "SELECCIONADO", 13, Ivory);
                saved.fontStyle = FontStyles.Bold;
                saved.alignment = TextAlignmentOptions.Right;
                Anchor(saved.rectTransform, new Vector2(1, 0), new Vector2(-16, 14), new Vector2(188, 22), new Vector2(1, 0));
                CheckIcon(saved.transform);
                savedMarker = saved.gameObject;
            }
            var visual = selectable.GetComponent<MenuSelectableVisual>() ?? selectable.gameObject.AddComponent<MenuSelectableVisual>();
            visual.Configure(selectable, background, label, focus.gameObject, savedMarker, primary);
            Outline(selectable.transform, "NormalOutline", new Color(.30f, .33f, .26f), 1, 0);
            visual.ConfigureBorders(Outline(selectable.transform, "FocusOutline", Gold, 2, 3),
                persistent ? Outline(selectable.transform, "SavedOutline", MenuVisualPalette.JadeEdge, 2, 0) : null);
        }

        static void AttachControlVisual(Selectable selectable, TMP_Text label)
        {
            var focus = Image("FocusIndicator", selectable.transform, Gold);
            Anchor(focus.rectTransform, new Vector2(0, .5f), new Vector2(-12, 0), new Vector2(5, 34), new Vector2(.5f, .5f));
            focus.gameObject.SetActive(false);
            var visual = selectable.GetComponent<MenuSelectableVisual>() ?? selectable.gameObject.AddComponent<MenuSelectableVisual>();
            visual.Configure(selectable, null, label, focus.gameObject, null, false);
            visual.ConfigureBorders(Outline(selectable.transform, "FocusOutline", Gold, 2, 4), null);
        }

        static void DecoratePanel(Transform panel, string titleName, string title)
        {
            var veil = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            veil.color = new Color(.018f, .028f, .021f, .8f);
            veil.raycastTarget = false;
            var card = Image("ContentCard", panel, Ink);
            Anchor(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1200, 840), new Vector2(.5f, .5f));
            card.transform.SetSiblingIndex(0);
            CarvedFrame(card.transform);
            var rule = Image("CardRule", panel, Gold);
            Anchor(rule.rectTransform, new Vector2(.5f, .5f), new Vector2(-598, 419), new Vector2(1196, 3), new Vector2(0, 1));
            rule.transform.SetSiblingIndex(1);
            var heading = Find(panel, titleName).GetComponent<TMP_Text>();
            heading.text = title;
            Style(heading, 38, Gold, TextAlignmentOptions.Left);
            Anchor(heading.rectTransform, new Vector2(.5f, .5f), new Vector2(-520, 315), new Vector2(1040, 62), new Vector2(0, .5f));
            var back = Find(panel, "Back").GetComponent<Button>();
            Anchor((RectTransform)back.transform, new Vector2(.5f, .5f), new Vector2(-420, -340), new Vector2(210, 52), new Vector2(0, .5f));
            StyleButton(back);
        }

        static void PolishLevels(Transform panel)
        {
            var level = Find(panel, "Level01").GetComponent<Button>();
            Anchor((RectTransform)level.transform, new Vector2(.5f, .5f), new Vector2(-450, 120), new Vector2(900, 72), new Vector2(0, .5f));
            StyleButton(level);
            level.GetComponentInChildren<TMP_Text>().text = "NIVEL 1   ·   LA ENTRADA SECRETA";
            var hint = Text("LevelHint", panel, "Completa la introducción y el tutorial para desbloquear el acceso.", 19, Muted);
            Anchor(hint.rectTransform, new Vector2(.5f, .5f), new Vector2(-450, 40), new Vector2(900, 40), new Vector2(0, .5f));
        }

        static void PolishCharacters(Transform panel)
        {
            var selected = Find(panel, "SelectedCharacter").GetComponent<TMP_Text>();
            Style(selected, 22, Ivory, TextAlignmentOptions.Left);
            Anchor(selected.rectTransform, new Vector2(.5f, .5f), new Vector2(-450, 224), new Vector2(900, 45), new Vector2(0, .5f));
            for (var i = 0; i < 4; i++)
            {
                var button = Find(panel, "Character0" + (i + 1)).GetComponent<Button>();
                Anchor((RectTransform)button.transform, new Vector2(.5f, .5f),
                    new Vector2(-450 + (i % 2) * 460, 95 - (i / 2) * 170), new Vector2(420, 142), new Vector2(0, .5f));
                StyleButton(button);
                var label = button.transform.Find("Label").GetComponent<TMP_Text>();
                label.rectTransform.offsetMin = new Vector2(104, 60);
                label.rectTransform.offsetMax = new Vector2(-16, -24);
                var preview = Image("PreviewSurface", button.transform, Ink);
                Anchor(preview.rectTransform, new Vector2(0, .5f), new Vector2(18, 0), new Vector2(68, 94), new Vector2(0, .5f));
                Outline(preview.transform, "PreviewBorder", new Color(.30f, .33f, .26f), 1, 0);
                var placeholder = Text("Placeholder", preview.transform, "PREVIEW", 10, Muted);
                Frame(placeholder.rectTransform, Vector2.zero, Vector2.one); placeholder.alignment = TextAlignmentOptions.Center;
            }
            var confirm = Find(panel, "ConfirmCharacter").GetComponent<Button>();
            Anchor((RectTransform)confirm.transform, new Vector2(.5f, .5f), new Vector2(-450, -215), new Vector2(420, 58), new Vector2(0, .5f));
            StyleButton(confirm, true);
            var hint = Text("SelectionHint", panel, "ELEGIR propone el personaje. CONFIRMAR guarda la elección.", 17, Muted);
            Anchor(hint.rectTransform, new Vector2(.5f, .5f), new Vector2(-450, -278), new Vector2(900, 30), new Vector2(0, .5f));
        }

        static void PolishSimple(Transform panel, string buttonName)
        {
            var button = Find(panel, buttonName).GetComponent<Button>();
            if (buttonName == "Intro") button.GetComponentInChildren<TMP_Text>().text = "REPETIR INTRODUCCIÓN";
            else if (buttonName == "ReplayTutorial") button.GetComponentInChildren<TMP_Text>().text = "REPETIR TUTORIAL";
            Anchor((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(-450, 85), new Vector2(700, 70), new Vector2(0, .5f));
            StyleButton(button);
        }

        static void PolishCredits(Transform panel)
        {
            var body = Find(panel, "CreditsBody").GetComponent<TMP_Text>();
            body.text = "CRÉDITOS PROVISIONALES";
            Style(body, 23, Ivory, TextAlignmentOptions.Left);
            Anchor(body.rectTransform, new Vector2(.5f, .5f), new Vector2(-450, 110), new Vector2(900, 160), new Vector2(0, .5f));
        }

        static void PolishSettings(Transform panel)
        {
            var audio = Text("AudioSection", panel, "AUDIO", 20, Gold);
            Anchor(audio.rectTransform, new Vector2(.5f, .5f), new Vector2(-490, 245), new Vector2(430, 35), new Vector2(0, .5f));
            var controls = Text("ControlsSection", panel, "CONTROLES", 20, Gold);
            Anchor(controls.rectTransform, new Vector2(.5f, .5f), new Vector2(50, 245), new Vector2(450, 35), new Vector2(0, .5f));
            var interfaceHeader = Text("InterfaceSection", panel, "INTERFAZ", 20, Gold);
            Anchor(interfaceHeader.rectTransform, new Vector2(.5f, .5f), new Vector2(-490, -115), new Vector2(430, 35), new Vector2(0, .5f));

            PlaceSlider(panel, "Master", -490, 175);
            PlaceSlider(panel, "Music", -490, 70);
            PlaceSlider(panel, "SFX", -490, -35);
            PlaceSlider(panel, "Mouse", 50, 175);
            PlaceSlider(panel, "Gamepad", 50, 70);

            var invert = Find(panel, "InvertY").GetComponent<Toggle>();
            Anchor((RectTransform)invert.transform, new Vector2(.5f, .5f), new Vector2(50, -35), new Vector2(430, 36), new Vector2(0, .5f));
            var invertLabel = invert.GetComponentInChildren<TMP_Text>();
            invertLabel.text = "INVERTIR EJE Y";
            invertLabel.color = Ivory;
            var box = Find(invert.transform, "Box").GetComponent<RectTransform>();
            Anchor(box, new Vector2(0, .5f), Vector2.zero, new Vector2(30, 30), new Vector2(0, .5f));

            var camera = EnsureDropdown(panel, "CameraMode");
            ConfigureDropdown(camera, new[] { "PRIMERA PERSONA", "TERCERA PERSONA" }, 0);
            PlaceDropdown(panel, camera, "MODO DE CÁMARA", 50, -155);
            var reticle = Find(panel, "Reticle").GetComponent<TMP_Dropdown>();
            ConfigureDropdown(reticle, new[] { "NINGUNA", "PUNTO", "CRUCETA" }, 1);
            PlaceDropdown(panel, reticle, "RETÍCULO", -490, -200);

            var controller = panel.GetComponent<SettingsMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_cameraMode").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AttachControlVisual(invert, invertLabel);
            AttachControlVisual(camera, camera.captionText);
            AttachControlVisual(reticle, reticle.captionText);
            ChoiceState(camera, new[] { "PRIMERA\nPERSONA", "TERCERA\nPERSONA" }, "ACTIVA");
            ChoiceState(reticle, new[] { "NINGUNA", "PUNTO", "CRUCETA" }, "ACTIVO");
            var invertState = Text("ValueState", invert.transform, "DESACTIVADO", 14, Ivory);
            Anchor(invertState.rectTransform, new Vector2(1, .5f), Vector2.zero, new Vector2(156, 30), new Vector2(1, .5f));
            invertLabel.rectTransform.sizeDelta = new Vector2(225, 34);
            invertLabel.rectTransform.anchoredPosition = new Vector2(148, 0);
            Readout(invert, invertState, false);
        }

        static void PlaceSlider(Transform panel, string name, float x, float y)
        {
            var slider = Find(panel, name).GetComponent<Slider>();
            Anchor((RectTransform)slider.transform, new Vector2(.5f, .5f), new Vector2(x, y), new Vector2(430, 24), new Vector2(0, .5f));
            var label = Find(panel, name + "Label").GetComponent<TMP_Text>();
            if (name == "Master") label.text = "VOLUMEN GENERAL";
            else if (name == "Music") label.text = "VOLUMEN DE MÚSICA";
            else if (name == "SFX") label.text = "VOLUMEN DE EFECTOS";
            else if (name == "Mouse") label.text = "SENSIBILIDAD DEL MOUSE";
            else if (name == "Gamepad") label.text = "SENSIBILIDAD DEL GAMEPAD";
            Style(label, 19, Ivory, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, new Vector2(.5f, .5f), new Vector2(x, y + 31), new Vector2(430, 26), new Vector2(0, .5f));
            Find(slider.transform, "Background").GetComponent<Image>().color = new Color(.15f, .23f, .26f);
            Find(slider.transform, "Background/Fill").GetComponent<Image>().color = Gold;
            Find(slider.transform, "Handle").GetComponent<Image>().color = Ivory;
            AttachControlVisual(slider, label);
            label.rectTransform.sizeDelta = new Vector2(335, 26);
            var value = Text(name + "Value", panel, "", 17, Ivory);
            value.alignment = TextAlignmentOptions.Right;
            Anchor(value.rectTransform, new Vector2(.5f, .5f), new Vector2(x + 340, y + 31), new Vector2(90, 26), new Vector2(0, .5f));
            Readout(slider, value, name == "Master" || name == "Music" || name == "SFX");
        }

        static GameObject Outline(Transform parent, string name, Color color, float thickness, float outset)
        {
            var root = parent.Find(name) as RectTransform;
            if (root == null)
            {
                root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(parent, false);
            }
            Frame(root, Vector2.zero, Vector2.one);
            root.offsetMin = Vector2.one * -outset; root.offsetMax = Vector2.one * outset;
            for (int i = 0; i < 4; i++)
            {
                var line = Image("Edge" + i, root, color).rectTransform;
                bool horizontal = i < 2;
                Frame(line, horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0),
                    horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1));
                line.sizeDelta = horizontal ? new Vector2(0, thickness) : new Vector2(thickness, 0);
                line.pivot = horizontal ? new Vector2(.5f, i) : new Vector2(i - 2, .5f);
            }
            return root.gameObject;
        }

        static void CheckIcon(Transform parent)
        {
            // Geometric check avoids dependence on a font's Unicode checkmark coverage.
            var old = parent.Find("SavedCheck"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var shortStroke = Image("CheckShort", parent, MenuVisualPalette.JadeEdge).rectTransform;
            Anchor(shortStroke, new Vector2(0, .5f), new Vector2(14, -2), new Vector2(8, 3), new Vector2(.5f, .5f));
            shortStroke.localRotation = Quaternion.Euler(0, 0, -45);
            var longStroke = Image("CheckLong", parent, MenuVisualPalette.JadeEdge).rectTransform;
            Anchor(longStroke, new Vector2(0, .5f), new Vector2(21, 1), new Vector2(15, 3), new Vector2(.5f, .5f));
            longStroke.localRotation = Quaternion.Euler(0, 0, 45);
        }

        static void CarvedFrame(Transform panel)
        {
            Outline(panel, "CarvedBorder", new Color(.38f, .34f, .23f, .75f), 1, -10);
            // Abstract stepped masonry corners: architectural geometry, no invented writing.
            for (int corner = 0; corner < 4; corner++)
            {
                var anchor = new Vector2(corner % 2, corner / 2);
                float x = anchor.x == 0 ? 1 : -1, y = anchor.y == 0 ? 1 : -1;
                for (int step = 0; step < 3; step++)
                {
                    var line = Image("StoneStep" + corner + "_" + step, panel, step == 2 ? MenuVisualPalette.JadeEdge : Gold).rectTransform;
                    Anchor(line, anchor, new Vector2(x * (16 + step * 7), y * (16 + step * 7)),
                        new Vector2(36 - step * 8, 2), anchor);
                }
            }
            // Sprite slot for a future supplied stone texture, preserving today's procedural fill.
            var surface = Image("StoneSurface", panel, new Color(.22f, .26f, .19f, .08f));
            Frame(surface.rectTransform, new Vector2(.03f, .06f), new Vector2(.97f, .94f));
            surface.transform.SetAsFirstSibling();
        }

        static void Readout(Selectable control, TMP_Text text, bool percentage)
        {
            var presenter = control.GetComponent<MenuSettingStatePresenter>() ?? control.gameObject.AddComponent<MenuSettingStatePresenter>();
            var data = new SerializedObject(presenter);
            data.FindProperty("_slider").objectReferenceValue = control as Slider;
            data.FindProperty("_toggle").objectReferenceValue = control as Toggle;
            data.FindProperty("_valueText").objectReferenceValue = text;
            data.FindProperty("_percentage").boolValue = percentage;
            data.ApplyModifiedPropertiesWithoutUndo(); presenter.RefreshVisual();
        }

        static void ChoiceState(TMP_Dropdown control, string[] names, string state)
        {
            var presenter = control.GetComponent<MenuSettingStatePresenter>() ?? control.gameObject.AddComponent<MenuSettingStatePresenter>();
            var data = new SerializedObject(presenter); data.FindProperty("_choice").objectReferenceValue = control;
            var fills = data.FindProperty("_optionFills"); fills.arraySize = names.Length;
            var markers = data.FindProperty("_activeMarkers"); markers.arraySize = names.Length;
            float width = (430f - (names.Length - 1) * 6) / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var chip = Image("ActiveOption" + i, control.transform, Stone);
                Anchor(chip.rectTransform, new Vector2(0, 0), new Vector2(i * (width + 6), -9), new Vector2(width, 64), new Vector2(0, 1));
                Outline(chip.transform, "Border", new Color(.30f, .33f, .26f), 1, 0);
                var label = Text("OptionName", chip.transform, names[i], 13, Ivory);
                label.alignment = TextAlignmentOptions.Center;
                Anchor(label.rectTransform, new Vector2(.5f, 1), new Vector2(0, -5), new Vector2(width - 16, 32), new Vector2(.5f, 1));
                var marker = Text("ActiveMarker", chip.transform, state, 12, Ivory); marker.alignment = TextAlignmentOptions.Center;
                Anchor(marker.rectTransform, new Vector2(.5f, 0), new Vector2(0, 5), new Vector2(100, 19), new Vector2(.5f, 0));
                CheckIcon(marker.transform);
                Outline(marker.transform, "ActiveBorder", MenuVisualPalette.JadeEdge, 1, 0);
                fills.GetArrayElementAtIndex(i).objectReferenceValue = chip;
                markers.GetArrayElementAtIndex(i).objectReferenceValue = marker.gameObject;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); presenter.RefreshVisual();
        }

        static void ConfigureNavigation(Transform background)
        {
            LinkVertical(Find(background, "Main"), new[] { "Play", "Levels", "Character", "Cinematics", "Tutorial", "Settings", "Credits", "Quit" });
            LinkVertical(Find(background, "Levels"), new[] { "Level01", "Back" });
            LinkVertical(Find(background, "Cinematics"), new[] { "Intro", "Back" });
            LinkVertical(Find(background, "Tutorial"), new[] { "ReplayTutorial", "Back" });
            LinkVertical(Find(background, "Credits"), new[] { "Back" });

            var characters = Find(background, "CharacterSelection");
            var c1 = Find(characters, "Character01").GetComponent<Selectable>();
            var c2 = Find(characters, "Character02").GetComponent<Selectable>();
            var c3 = Find(characters, "Character03").GetComponent<Selectable>();
            var c4 = Find(characters, "Character04").GetComponent<Selectable>();
            var confirm = Find(characters, "ConfirmCharacter").GetComponent<Selectable>();
            var back = Find(characters, "Back").GetComponent<Selectable>();
            Explicit(c1, c1, c3, c1, c2); Explicit(c2, c2, c4, c1, c2);
            Explicit(c3, c1, confirm, c3, c4); Explicit(c4, c2, confirm, c3, c4);
            Explicit(confirm, c3, back, confirm, confirm); Explicit(back, confirm, back, back, back);

            var settings = Find(background, "Settings");
            var master = Find(settings, "Master").GetComponent<Selectable>();
            var music = Find(settings, "Music").GetComponent<Selectable>();
            var sfx = Find(settings, "SFX").GetComponent<Selectable>();
            var mouse = Find(settings, "Mouse").GetComponent<Selectable>();
            var gamepad = Find(settings, "Gamepad").GetComponent<Selectable>();
            var invert = Find(settings, "InvertY").GetComponent<Selectable>();
            var camera = Find(settings, "CameraMode").GetComponent<Selectable>();
            var reticle = Find(settings, "Reticle").GetComponent<Selectable>();
            var settingsBack = Find(settings, "Back").GetComponent<Selectable>();
            Explicit(master, master, music, master, mouse); Explicit(music, master, sfx, music, gamepad);
            Explicit(sfx, music, reticle, sfx, invert); Explicit(reticle, sfx, settingsBack, reticle, camera);
            Explicit(mouse, mouse, gamepad, master, mouse); Explicit(gamepad, mouse, invert, music, gamepad);
            Explicit(invert, gamepad, camera, sfx, invert); Explicit(camera, invert, settingsBack, reticle, camera);
            Explicit(settingsBack, reticle, settingsBack, settingsBack, settingsBack);
        }

        static void LinkVertical(Transform panel, string[] names)
        {
            var items = names.Select(name => Find(panel, name).GetComponent<Selectable>()).ToArray();
            for (var index = 0; index < items.Length; index++)
                Explicit(items[index], items[Mathf.Max(0, index - 1)], items[Mathf.Min(items.Length - 1, index + 1)], items[index], items[index]);
        }

        static void Explicit(Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            selectable.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        static void SetupControlHints(Transform root, Transform background, MainMenuController menu)
        {
            var devices = root.GetComponent<InputDeviceManager>() ?? root.gameObject.AddComponent<InputDeviceManager>();
            var bar = Image("ControlHints", background, new Color(.01f, .025f, .04f, .88f));
            Anchor(bar.rectTransform, new Vector2(1, 0), new Vector2(-64, 34), new Vector2(900, 48), new Vector2(1, 0));
            var navigate = Text("Navigate", bar.transform, "WASD / FLECHAS  NAVEGAR", 16, Ivory);
            var submit = Text("Submit", bar.transform, "ENTER  SELECCIONAR", 16, Ivory);
            var cancel = Text("Cancel", bar.transform, "ESC  VOLVER", 16, Ivory);
            Anchor(navigate.rectTransform, new Vector2(0, .5f), new Vector2(24, 0), new Vector2(330, 30), new Vector2(0, .5f));
            Anchor(submit.rectTransform, new Vector2(.5f, .5f), new Vector2(-5, 0), new Vector2(280, 30), new Vector2(.5f, .5f));
            Anchor(cancel.rectTransform, new Vector2(1, .5f), new Vector2(-24, 0), new Vector2(230, 30), new Vector2(1, .5f));
            var presenter = root.GetComponent<MenuControlHintsPresenter>() ?? root.gameObject.AddComponent<MenuControlHintsPresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_devices").objectReferenceValue = devices;
            serialized.FindProperty("_menu").objectReferenceValue = menu;
            serialized.FindProperty("_navigateHint").objectReferenceValue = navigate;
            serialized.FindProperty("_submitHint").objectReferenceValue = submit;
            serialized.FindProperty("_cancelHint").objectReferenceValue = cancel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Dropdown EnsureDropdown(Transform panel, string name)
        {
            var child = panel.Find(name);
            if (child != null) return child.GetComponent<TMP_Dropdown>();
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(panel, false);
            return go.GetComponent<TMP_Dropdown>();
        }

        static void PlaceDropdown(Transform panel, TMP_Dropdown dropdown, string caption, float x, float y)
        {
            Anchor((RectTransform)dropdown.transform, new Vector2(.5f, .5f), new Vector2(x, y), new Vector2(430, 48), new Vector2(0, .5f));
            var label = Text(dropdown.name + "Label", panel, caption, 19, Ivory);
            Anchor(label.rectTransform, new Vector2(.5f, .5f), new Vector2(x, y + 39), new Vector2(430, 27), new Vector2(0, .5f));
        }

        static void ConfigureDropdown(TMP_Dropdown dropdown, string[] options, int defaultValue)
        {
            var shownValue = dropdown.options.Count > 0 ? dropdown.value : defaultValue;
            var image = dropdown.GetComponent<Image>();
            image.color = Stone;
            var caption = Text("Label", dropdown.transform, string.Empty, 20, Ivory);
            Frame(caption.rectTransform, Vector2.zero, Vector2.one);
            caption.rectTransform.offsetMin = new Vector2(18, 2);
            caption.rectTransform.offsetMax = new Vector2(-40, -2);
            var arrow = Text("Arrow", dropdown.transform, "V", 17, Gold);
            Anchor(arrow.rectTransform, new Vector2(1, .5f), new Vector2(-22, 0), new Vector2(20, 28), new Vector2(.5f, .5f));

            var template = Image("Template", dropdown.transform, Stone).rectTransform;
            template.GetComponent<Image>().raycastTarget = true;
            Anchor(template, new Vector2(.5f, 0), new Vector2(0, -3), new Vector2(0, 136), new Vector2(.5f, 1));
            template.anchorMin = new Vector2(0, 0);
            template.anchorMax = new Vector2(1, 0);
            var scroll = template.GetComponent<ScrollRect>() ?? template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            var viewport = Image("Viewport", template, Stone).rectTransform;
            viewport.GetComponent<Image>().raycastTarget = true;
            Frame(viewport, Vector2.zero, Vector2.one);
            var mask = viewport.GetComponent<Mask>() ?? viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var content = viewport.Find("Content") as RectTransform;
            if (content == null)
            {
                content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(viewport, false);
            }
            Anchor(content, new Vector2(.5f, 1), Vector2.zero, new Vector2(0, 38), new Vector2(.5f, 1));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            var item = Image("Item", content, new Color(.1f, .18f, .2f, 1)).rectTransform;
            item.GetComponent<Image>().raycastTarget = true;
            Anchor(item, new Vector2(.5f, 1), Vector2.zero, new Vector2(0, 38), new Vector2(.5f, 1));
            item.anchorMin = new Vector2(0, 1);
            item.anchorMax = new Vector2(1, 1);
            var toggle = item.GetComponent<Toggle>() ?? item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = item.GetComponent<Image>();
            var check = Image("Checkmark", item, Gold);
            Anchor(check.rectTransform, new Vector2(0, .5f), new Vector2(12, 0), new Vector2(5, 24), new Vector2(0, .5f));
            toggle.graphic = check;
            var itemLabel = Text("Item Label", item, "Option", 18, Ivory);
            Frame(itemLabel.rectTransform, Vector2.zero, Vector2.one);
            itemLabel.rectTransform.offsetMin = new Vector2(30, 0);
            itemLabel.rectTransform.offsetMax = new Vector2(-10, 0);
            scroll.viewport = viewport;
            scroll.content = content;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.options = options.Select(value => new TMP_Dropdown.OptionData(value)).ToList();
            dropdown.SetValueWithoutNotify(Mathf.Clamp(shownValue, 0, options.Length - 1));
            dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
        }
    }
}
