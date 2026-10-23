using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.Editor.Art
{
    // Decorative artwork only. Does not enter Play Mode or run validation suites.
    [InitializeOnLoad]
    public static class MainMenuBrandArtworkAuthoring
    {
        const string ScenePath="Assets/_Project/Scenes/Menus/MainMenu.unity";
        const string Art="Assets/_Project/Art/UI/MainMenu/";
        const string Logo=Art+"UI_Tikal_StoneGold_Logo.png";
        const string Plaque=Art+"UI_Level01_Illustrated_Plaque.png";
        static MainMenuBrandArtworkAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/MainMenuBrandArt.request"))return;
            File.Delete("Temp/MainMenuBrandArt.request");
            try{Apply();}catch(Exception e){Debug.LogException(e);}
        }
        [MenuItem("Tikal/Authoring/Apply Main Menu Stone Gold Artwork")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply artwork in Edit Mode.");
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath)
            {
                if(scene.isDirty)throw new InvalidOperationException("Save the current scene before opening MainMenu.");
                scene=EditorSceneManager.OpenScene(ScenePath);
            }
            if(scene.isDirty)EditorSceneManager.SaveScene(scene);
            Import(Logo);Import(Plaque);
            Transform background=null;foreach(var root in scene.GetRootGameObjects())if(root.name=="MainMenu")background=root.transform.Find("Canvas/Background");
            if(background==null)throw new InvalidOperationException("Existing MainMenu canvas is required.");
            ApplyVisuals(background);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log("MAIN_MENU_STONE_GOLD_ARTWORK_SAVED — visual artwork only; no tests or Play Mode.");
        }
        static void Import(string path)
        {
            AssetDatabase.ImportAsset(path);var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Artwork missing: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.sRGBTexture=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        }
        public static void ApplyVisuals(Transform background)
        {
            var logoSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Logo);var cardSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Plaque);
            if(logoSprite==null||cardSprite==null)return;
            // Keep the canonical TMP content and all functional objects; replace their rendering only.
            foreach(var name in new[]{"Title","BrandSubtitle","BrandEyebrow","TitleStoneDepth","TitleMasonryFrame","SubtitleGoldRule","TitleDiamond"})
            {
                var old=background.Find(name);if(old==null)continue;
                foreach(var graphic in old.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
            }
            var existing=background.Find("StoneGoldTitleArtwork");
            var image=existing!=null?existing.GetComponent<Image>():null;
            if(image==null){var go=new GameObject("StoneGoldTitleArtwork",typeof(RectTransform),typeof(Image));go.transform.SetParent(background,false);image=go.GetComponent<Image>();}
            image.sprite=logoSprite;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(78,-35);rect.sizeDelta=new Vector2(590,590*logoSprite.rect.height/logoSprite.rect.width);
            var main=background.Find("Main");if(image.transform.GetSiblingIndex()>main.GetSiblingIndex())image.transform.SetSiblingIndex(main.GetSiblingIndex());
            var card=main.Find("JourneyCard").GetComponent<Image>();
            foreach(var graphic in card.GetComponentsInChildren<Graphic>(true))if(graphic!=card)graphic.enabled=false;
            card.sprite=cardSprite;card.color=Color.white;card.type=Image.Type.Simple;card.preserveAspect=true;card.raycastTarget=false;
            var cr=card.rectTransform;cr.anchorMin=cr.anchorMax=new Vector2(1,0);cr.pivot=new Vector2(1,0);cr.anchoredPosition=new Vector2(-64,88);cr.sizeDelta=new Vector2(700,700*cardSprite.rect.height/cardSprite.rect.width);
        }
    }
}
