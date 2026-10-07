using System;
using System.Linq;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class MainMenuVisualValidation
    {
        private static int _checks;
        private static void Check(bool value, string message)
        { if (!value) throw new Exception("Main Menu visuals: " + message); _checks++; }
        [MenuItem("Tikal/Validation/Validate Main Menu Visual Identity")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Validate visual identity outside Play Mode; use the manual navigation checklist in Play Mode."); return; }
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Menus/MainMenu.unity");
            try { ValidateScene(scene); Debug.Log("TIKAL_MAIN_MENU_VISUAL_IDENTITY_VALID checks=" + _checks + "; native rendered QA still required."); }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static UnityEngine.Object Reference(UnityEngine.Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;
        internal static void ValidateScene(Scene scene)
        {
            _checks = 0;
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
            Check(all.All(c => c != null), "no Missing Scripts");
            var canvas = all.OfType<Canvas>().Single();
            var background = canvas.transform.Find("Background");
            Check(background.Find("Title").GetComponent<TMP_Text>().text == "TIKAL" &&
                background.Find("BrandSubtitle").GetComponent<TMP_Text>().text == "EL EXPLORADOR", "canonical title/subtitle");
            Check(!all.OfType<TMP_Text>().Any(t => t.text.ToUpperInvariant().Contains("EL PORTAL")), "no old Main Menu branding");
            Check(all.OfType<EventSystem>().Count() == 1 && all.OfType<MainMenuController>().Count() == 1 &&
                all.OfType<CharacterSelectionController>().Count() == 1 && all.OfType<SettingsMenuController>().Count() == 1 &&
                all.OfType<MenuControlHintsPresenter>().Count() == 1, "existing sole controllers and hints");
            var hints = all.OfType<MenuControlHintsPresenter>().Single();
            Check(new[] {"_devices","_menu","_navigateHint","_submitHint","_cancelHint"}.All(f => Reference(hints,f) != null), "device hint references preserved");
            var input = all.OfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().Single();
            Check(input.move != null && input.submit != null && input.cancel != null, "native navigation actions remain connected");
            foreach (var parent in all.OfType<Transform>())
                Check(parent.Cast<Transform>().GroupBy(t => t.name).All(g => g.Count() == 1), "no duplicate named children: " + parent.name);
            var controls = all.OfType<Selectable>().Where(c => !c.transform.IsChildOf(background.Find("Settings/CameraMode/Template")) &&
                !c.transform.IsChildOf(background.Find("Settings/Reticle/Template"))).ToArray();
            foreach (var control in controls)
            {
                var visual = control.GetComponent<MenuSelectableVisual>();
                Check(visual != null && Reference(visual,"_focusOutline") != null && Reference(visual,"_focusIndicator") != null, "separate focus border: " + control.name);
                Check(control.navigation.mode == Navigation.Mode.Explicit && control.navigation.selectOnUp != null && control.navigation.selectOnDown != null,
                    "existing explicit navigation: " + control.name);
            }
            foreach (var card in controls.Where(c => c.name.StartsWith("Character0", StringComparison.Ordinal)))
            {
                var visual = card.GetComponent<MenuSelectableVisual>();
                var marker = card.transform.Find("SavedMarker").gameObject;
                Check(marker.GetComponent<TMP_Text>().text == "SELECCIONADO" && marker.transform.Find("CheckLong") != null &&
                    Reference(visual,"_savedOutline") != null && card.transform.Find("PreviewSurface") != null, "saved card has text/check/border/preview slot");
                bool saved = marker.activeSelf; bool interactable = card.interactable;
                try
                {
                    card.interactable = true; visual.SetPersistent(true); visual.OnSelect(null);
                    Check(card.transform.Find("FocusOutline").gameObject.activeSelf && card.transform.Find("SavedOutline").gameObject.activeSelf && marker.activeSelf,
                        "focused plus saved are simultaneous");
                    visual.OnDeselect(null);
                    Check(marker.activeSelf && !card.transform.Find("FocusOutline").gameObject.activeSelf, "deselect preserves saved marker");
                    card.interactable = false; visual.RefreshVisual();
                    Check(card.GetComponentInChildren<TMP_Text>().color == MenuVisualPalette.Muted, "disabled label remains readable");
                }
                finally { card.interactable = interactable; visual.SetPersistent(saved); }
            }
            var settings = background.Find("Settings");
            foreach (var name in new[] { "CameraMode", "Reticle" })
            {
                var dropdown = settings.Find(name).GetComponent<TMP_Dropdown>();
                var presenter = dropdown.GetComponent<MenuSettingStatePresenter>();
                Check(presenter != null && presenter.IsConfigured && dropdown.template != null, "active choices connected: " + name);
                int old = dropdown.value;
                try
                {
                    for (int i = 0; i < dropdown.options.Count; i++)
                    {
                        dropdown.SetValueWithoutNotify(i); presenter.RefreshVisual();
                        for (int j = 0; j < dropdown.options.Count; j++)
                            Check(dropdown.transform.Find("ActiveOption" + j + "/ActiveMarker").gameObject.activeSelf == (i == j), "only active option owns check/text marker");
                    }
                }
                finally { dropdown.SetValueWithoutNotify(old); presenter.RefreshVisual(); }
            }
            Check(settings.Find("CameraMode").GetComponent<TMP_Dropdown>().options.Select(o => o.text).SequenceEqual(new[] {"PRIMERA PERSONA","TERCERA PERSONA"}), "camera names/indices preserved");
            Check(settings.Find("Reticle").GetComponent<TMP_Dropdown>().options.Count == 3, "reticle retains three indices");
            foreach (var name in new[] {"Master","Music","SFX","Mouse","Gamepad","InvertY"})
                Check(settings.Find(name).GetComponent<MenuSettingStatePresenter>().IsConfigured, "readout connected: " + name);
            Check(all.OfType<Image>().Where(i => i.name.StartsWith("StoneStep",StringComparison.Ordinal)).Any() &&
                all.OfType<Image>().Where(i => i.name.StartsWith("Check",StringComparison.Ordinal)).All(i => !i.raycastTarget), "procedural noninteractive decoration");
            var scaler = canvas.GetComponent<CanvasScaler>();
            Check(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1920,1080) &&
                Mathf.Approximately(scaler.matchWidthOrHeight,.5f), "canonical responsive canvas");
            foreach (var size in new[] {new Vector2(1920,1080),new Vector2(1280,720),new Vector2(1920,1200),new Vector2(1280,800)})
            {
                float scale = Mathf.Sqrt(size.x / 1920 * size.y / 1080);
                var viewport = new Rect(-size.x / scale / 2, -size.y / scale / 2, size.x / scale, size.y / scale);
                foreach (var rect in controls.Select(c => (RectTransform)c.transform).Concat(all.OfType<TMP_Text>()
                    .Where(t => t.name == "Title" || t.name == "BrandSubtitle" || t.name == "SavedMarker" || t.name == "ActiveMarker").Select(t => t.rectTransform)))
                {
                    var bounds = LayoutRect(rect, (RectTransform)canvas.transform, viewport);
                    Check(viewport.Contains(bounds.min + Vector2.one * .1f) && viewport.Contains(bounds.max - Vector2.one * .1f), size + " layout bounds: " + rect.name);
                }
                // Main navigation and journey card must retain a gap at narrower 16:10 widths.
                var nav = LayoutRect((RectTransform)background.Find("Main/Play"),(RectTransform)canvas.transform,viewport);
                var journey = LayoutRect((RectTransform)background.Find("Main/JourneyCard"),(RectTransform)canvas.transform,viewport);
                Check(nav.xMax < journey.xMin, size + " main actions do not overlap journey card");
                foreach (var panelName in new[] {"Main","Levels","CharacterSelection","Cinematics","Tutorial","Settings","Credits"})
                {
                    var panelControls = controls.Where(c => c.transform.IsChildOf(background.Find(panelName))).ToArray();
                    for (int a = 0; a < panelControls.Length; a++)
                        for (int b = a + 1; b < panelControls.Length; b++)
                            Check(!LayoutRect((RectTransform)panelControls[a].transform,(RectTransform)canvas.transform,viewport)
                                .Overlaps(LayoutRect((RectTransform)panelControls[b].transform,(RectTransform)canvas.transform,viewport)),
                                size + " no overlapping controls: " + panelName);
                }
            }
        }
        private static Rect LayoutRect(RectTransform rect, RectTransform canvas, Rect viewport)
        {
            if (rect == canvas) return viewport;
            var parent = LayoutRect((RectTransform)rect.parent, canvas, viewport);
            var size = Vector2.Scale(parent.size, rect.anchorMax - rect.anchorMin) + rect.sizeDelta;
            var anchor = rect.anchorMin + Vector2.Scale(rect.anchorMax - rect.anchorMin, rect.pivot);
            var pivot = parent.min + Vector2.Scale(parent.size, anchor) + rect.anchoredPosition;
            return new Rect(pivot - Vector2.Scale(size,rect.pivot), size);
        }
    }
}
