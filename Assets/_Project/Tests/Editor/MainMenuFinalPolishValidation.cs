using System;
using System.Linq;
using Tikal.Core;
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
    public static class MainMenuFinalPolishValidation
    {
        static void Check(bool ok,string message)=>MainMenuFinalPolishAuthoring.Check(ok,"MainMenu final polish: "+message);
        [MenuItem("Tikal/Validation/Validate Main Menu Final Polish")]
        public static void Run()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorSceneManager.GetActiveScene().isDirty,"run from Edit Mode with the current scene saved");
            var scene=EditorSceneManager.OpenPreviewScene(MainMenuFinalPolishAuthoring.ScenePath);
            try{ValidateScene(scene);Debug.Log("TIKAL_MAIN_MENU_FINAL_STATIC_VALID");}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            if(EditorSceneManager.GetActiveScene().path!=MainMenuFinalPolishAuthoring.ScenePath)EditorSceneManager.OpenScene(MainMenuFinalPolishAuthoring.ScenePath);
            System.IO.File.WriteAllText("Temp/MainMenuFinalPlay.request","Menu command: full native navigation, selection and isolated persistence validation");
        }
        public static void ValidateScene(Scene scene)
        {
            var all=MainMenuFinalPolishAuthoring.All(scene);Check(all.All(c=>c!=null),"no Missing Scripts");var bg=all.OfType<Canvas>().Single().transform.Find("Background");Check(bg.Find("Title").GetComponent<TMP_Text>().text=="TIKAL"&&bg.Find("BrandSubtitle").GetComponent<TMP_Text>().text=="EL EXPLORADOR","canonical branding");Check(!all.OfType<TMP_Text>().Any(t=>System.Text.RegularExpressions.Regex.IsMatch(t.text,@"TIKAL\s*:?\s*EL\s+PORTAL",System.Text.RegularExpressions.RegexOptions.IgnoreCase)),"no obsolete visible branding");
            foreach(var name in new[]{"Play","Levels","Character","Cinematics","Tutorial","Settings","Credits","Quit"})Check(bg.Find("Main/"+name).GetComponent<Button>()!=null,"main action retained "+name);
            foreach(var t in all.OfType<Transform>())Check(t.Cast<Transform>().GroupBy(c=>c.name).All(g=>g.Count()==1),"no duplicate children "+t.name);
            foreach(var visual in all.OfType<MenuSelectableVisual>().Where(v=>v.enabled))
            {var s=new SerializedObject(visual);Check(s.FindProperty("_finalPolish").boolValue,"opt-in presentation "+visual.name);var f=(GameObject)s.FindProperty("_focusOutline").objectReferenceValue;Check(f!=null&&s.FindProperty("_focusIndicator").objectReferenceValue!=null,"frame and cursor "+visual.name);var saved=(GameObject)s.FindProperty("_savedOutline").objectReferenceValue;if(saved!=null)Check(saved!=f&&saved.GetComponentInChildren<Image>().color!=f.GetComponentInChildren<Image>().color,"focus distinct from selected "+visual.name);}
            for(int i=0;i<4;i++)
            {var card=bg.Find("CharacterSelection/Character0"+(i+1));var image=card.Find("PreviewSurface").GetComponent<Image>();Check(image.sprite!=null,"portrait sprite "+i);Check(card.Find("SavedMarker")!=null,"persistent marker "+i);if(i<3)Check(image.sprite.texture.width==768&&image.sprite.texture.height==960,"consistent portrait resolution "+i);else Check(!card.GetComponent<Button>().interactable&&card.Find("PreviewSurface/UnknownLock")!=null,"unknown locked, no selectable model");}
            foreach(var name in new[]{"Dwight","Axel","Mario"})
            {var texture=new Texture2D(2,2);try{texture.LoadImage(System.IO.File.ReadAllBytes(MainMenuFinalPolishAuthoring.Portraits+"/UI_Portrait_"+name+".png"));var pixels=texture.GetPixels32();Check(pixels.Count(p=>p.r+p.g+p.b>160)>pixels.Length*.12f,"Portrait contains rendered character, not blank background "+name);}finally{UnityEngine.Object.DestroyImmediate(texture);}}
            var settings=bg.Find("Settings");foreach(var name in new[]{"CameraMode","Reticle"}){var choice=settings.Find(name).GetComponent<MenuSegmentedChoice>();Check(choice!=null&&choice.Source==settings.Find(name).GetComponent<TMP_Dropdown>(),"original source reference "+name);Check(choice.Segments.Length==(name=="CameraMode"?2:3),"correct segment count "+name);Check(choice.Segments.All(b=>b!=null&&b.GetComponent<MenuSelectableVisual>()!=null&&b.navigation.mode==Navigation.Mode.Explicit),"native explicit navigation "+name);}
            Check(CharacterCatalog.All.Select(c=>c.DisplayName).SequenceEqual(new[]{"Dwight","Axel","Mario","DESCONOCIDO"})&&CharacterCatalog.All[3].IsLocked,"canonical roster untouched");Check(all.OfType<EventSystem>().Count()==1,"single EventSystem");Check(all.OfType<AudioListener>().Count()<=1,"no duplicate AudioListener");var module=all.OfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().Single();Check(module.move!=null&&module.submit!=null&&module.cancel!=null,"existing native UI actions");Check(all.OfType<MainMenuController>().Count()==1&&all.OfType<CharacterSelectionController>().Count()==1&&all.OfType<SettingsMenuController>().Count()==1,"sole original controllers");
            Check(all.OfType<Image>().Where(i=>i.name=="PreviewSurface").All(i=>i.sprite!=null),"no missing portrait sprites");Check(all.OfType<TMP_Text>().All(t=>t.font!=null&&t.fontSharedMaterial!=null),"no missing font materials");
        }
    }
}
