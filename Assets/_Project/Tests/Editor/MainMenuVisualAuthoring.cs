using System;
using System.Linq;
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

        static readonly Color Ink = new Color(.018f, .035f, .055f, .92f);
        static readonly Color Stone = new Color(.055f, .105f, .135f, .94f);
        static readonly Color Gold = new Color(.86f, .62f, .27f);
        static readonly Color Ivory = new Color(.96f, .93f, .84f);
        static readonly Color Muted = new Color(.7f, .76f, .75f);

        [MenuItem("Tikal/Authoring/Polish Existing Main Menu")]
        public static void RunFromMenu()
        {
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

            var shade = Image("LeftShade", background, new Color(.008f, .02f, .035f, .78f));
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
            title.text = "TIKAL\nEL PORTAL";
            Style(title, 59, Gold, TextAlignmentOptions.Left);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(95, -90), new Vector2(640, 160), new Vector2(0, 1));
            var eyebrow = Text("BrandEyebrow", background, "UNA AVENTURA EN LAS SOMBRAS", 16, Gold);
            Anchor(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(97, -61), new Vector2(610, 24), new Vector2(0, 1));
            var footer = Text("Footer", background, "TIKAL  /  EL PORTAL", 16, Muted);
            Anchor(footer.rectTransform, new Vector2(0, 0), new Vector2(96, 43), new Vector2(550, 25), Vector2.zero);

            var main = Find(background, "Main");
            string[] names = { "Play", "Levels", "Character", "Cinematics", "Tutorial", "Settings", "Credits", "Quit" };
            for (var i = 0; i < names.Length; i++)
            {
                var button = Find(main, names[i]).GetComponent<Button>();
                Anchor((RectTransform)button.transform, new Vector2(0, .5f), new Vector2(95, 145 - i * 61), new Vector2(430, 54), new Vector2(0, .5f));
                StyleButton(button, i == 0);
            }
            var subtitle = Find(main, "ContinueSubtitle").GetComponent<TMP_Text>();
            Style(subtitle, 17, Muted, TextAlignmentOptions.Left);
            Anchor(subtitle.rectTransform, new Vector2(0, .5f), new Vector2(99, 202), new Vector2(520, 30), new Vector2(0, .5f));

            var journey = Image("JourneyCard", main, new Color(.02f, .055f, .075f, .87f));
            Anchor(journey.rectTransform, new Vector2(1, 0), new Vector2(-80, 92), new Vector2(560, 170), new Vector2(1, 0));
            var accent = Image("Accent", journey.transform, Gold);
            Frame(accent.rectTransform, Vector2.zero, new Vector2(0, 1));
            accent.rectTransform.pivot = new Vector2(0, .5f);
            accent.rectTransform.sizeDelta = new Vector2(4, 0);
            var cardHeading = Text("Heading", journey.transform, "LA ENTRADA SECRETA", 28, Gold);
            Anchor(cardHeading.rectTransform, new Vector2(0, 1), new Vector2(28, -28), new Vector2(505, 44), new Vector2(0, 1));
            var cardBody = Text("Body", journey.transform, "Explora las ruinas. Sigue las pistas.\nDescubre lo que duerme tras el portal.", 19, Ivory);
            Anchor(cardBody.rectTransform, new Vector2(0, 1), new Vector2(28, -80), new Vector2(505, 72), new Vector2(0, 1));

            DecoratePanel(Find(background, "Levels"), "LevelsTitle", "NIVELES");
            DecoratePanel(Find(background, "CharacterSelection"), "CharacterTitle", "PERSONAJE");
            DecoratePanel(Find(background, "Cinematics"), "CinematicsTitle", "CINEMATICAS");
            DecoratePanel(Find(background, "Tutorial"), "TutorialTitle", "TUTORIAL");
            DecoratePanel(Find(background, "Settings"), "SettingsTitle", "CONFIGURACION");
            DecoratePanel(Find(background, "Credits"), "CreditsTitle", "CREDITOS");

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

            EditorSceneManager.MarkSceneDirty(scene);
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
        }

        static void DecoratePanel(Transform panel, string titleName, string title)
        {
            var veil = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            veil.color = new Color(.008f, .02f, .035f, .8f);
            veil.raycastTarget = false;
            var card = Image("ContentCard", panel, Ink);
            Anchor(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1200, 840), new Vector2(.5f, .5f));
            card.transform.SetSiblingIndex(0);
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
            var hint = Text("LevelHint", panel, "Completa la introduccion y el tutorial para desbloquear el acceso.", 19, Muted);
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
                    new Vector2(-450 + (i % 2) * 460, 105 - (i / 2) * 100), new Vector2(420, 82), new Vector2(0, .5f));
                StyleButton(button);
            }
            var confirm = Find(panel, "ConfirmCharacter").GetComponent<Button>();
            Anchor((RectTransform)confirm.transform, new Vector2(.5f, .5f), new Vector2(-450, -160), new Vector2(420, 58), new Vector2(0, .5f));
            StyleButton(confirm, true);
        }

        static void PolishSimple(Transform panel, string buttonName)
        {
            var button = Find(panel, buttonName).GetComponent<Button>();
            Anchor((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(-450, 85), new Vector2(700, 70), new Vector2(0, .5f));
            StyleButton(button);
        }

        static void PolishCredits(Transform panel)
        {
            var body = Find(panel, "CreditsBody").GetComponent<TMP_Text>();
            Style(body, 23, Ivory, TextAlignmentOptions.Left);
            Anchor(body.rectTransform, new Vector2(.5f, .5f), new Vector2(-450, 110), new Vector2(900, 160), new Vector2(0, .5f));
        }

        static void PolishSettings(Transform panel)
        {
            var audio = Text("AudioSection", panel, "AUDIO", 20, Gold);
            Anchor(audio.rectTransform, new Vector2(.5f, .5f), new Vector2(-490, 245), new Vector2(430, 35), new Vector2(0, .5f));
            var controls = Text("ControlsSection", panel, "CONTROLES / GAMEPLAY", 20, Gold);
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
            invert.GetComponentInChildren<TMP_Text>().color = Ivory;
            var box = Find(invert.transform, "Box").GetComponent<RectTransform>();
            Anchor(box, new Vector2(0, .5f), Vector2.zero, new Vector2(30, 30), new Vector2(0, .5f));

            var camera = EnsureDropdown(panel, "CameraMode");
            ConfigureDropdown(camera, new[] { "Primera Persona", "Tercera Persona" }, 0);
            PlaceDropdown(panel, camera, "Camera Mode", 50, -155);
            var reticle = Find(panel, "Reticle").GetComponent<TMP_Dropdown>();
            ConfigureDropdown(reticle, new[] { "Ninguno", "Punto", "Cruz" }, 1);
            PlaceDropdown(panel, reticle, "Reticle", -490, -200);

            var controller = panel.GetComponent<SettingsMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_cameraMode").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void PlaceSlider(Transform panel, string name, float x, float y)
        {
            var slider = Find(panel, name).GetComponent<Slider>();
            Anchor((RectTransform)slider.transform, new Vector2(.5f, .5f), new Vector2(x, y), new Vector2(430, 24), new Vector2(0, .5f));
            var label = Find(panel, name + "Label").GetComponent<TMP_Text>();
            if (name == "Master") label.text = "Master Volume";
            else if (name == "Music") label.text = "Music Volume";
            else if (name == "SFX") label.text = "SFX Volume";
            Style(label, 19, Ivory, TextAlignmentOptions.Left);
            Anchor(label.rectTransform, new Vector2(.5f, .5f), new Vector2(x, y + 31), new Vector2(430, 26), new Vector2(0, .5f));
            Find(slider.transform, "Background").GetComponent<Image>().color = new Color(.15f, .23f, .26f);
            Find(slider.transform, "Background/Fill").GetComponent<Image>().color = Gold;
            Find(slider.transform, "Handle").GetComponent<Image>().color = Ivory;
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
            dropdown.SetValueWithoutNotify(defaultValue);
            dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
        }
    }
}
