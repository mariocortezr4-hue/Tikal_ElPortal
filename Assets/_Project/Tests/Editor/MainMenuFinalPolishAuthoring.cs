using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class MainMenuFinalPolishAuthoring
    {
        public const string ScenePath="Assets/_Project/Scenes/Menus/MainMenu.unity";
        public const string Evidence="../_MainMenuFinalPolish_20261022";
        public const string Art="Assets/_Project/Art/UI/MainMenu";
        public const string Portraits="Assets/_Project/Art/UI/Characters/Portraits";
        static readonly Color Gold=MenuVisualPalette.Gold, Cream=MenuVisualPalette.Ivory;
        public static readonly Color Focus=new Color(1,.82f,.40f), Saved=new Color(.65f,.46f,.20f);
        static MainMenuFinalPolishAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/MainMenuFinalValidate.request")){File.Delete("Temp/MainMenuFinalValidate.request");MainMenuFinalPolishValidation.Run();return;}
            bool portraits=File.Exists("Temp/MainMenuPortraits.request");
            if(!portraits&&!File.Exists("Temp/MainMenuFinalApply.request"))return;File.Delete(portraits?"Temp/MainMenuPortraits.request":"Temp/MainMenuFinalApply.request");
            try{if(portraits)foreach(var c in CharacterCatalog.All.Where(c=>!c.IsLocked))Portrait(c);Apply();int count=All(EditorSceneManager.GetActiveScene()).Length;Apply();Check(All(EditorSceneManager.GetActiveScene()).Length==count,"Idempotent object/component count");File.AppendAllText(Evidence+"/Authoring.txt","\nSecond apply: same object/component count; no duplication.\n");}
            catch(Exception e){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tikal/Authoring/Apply Final Main Menu Polish")]
        public static void Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");Directory.CreateDirectory(Evidence);
            var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve pending scene changes");
            if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
            if(!File.Exists(Evidence+"/Before.unity"))File.Copy(ScenePath,Evidence+"/Before.unity");
            var original=All(scene).Where(c=>c is MainMenuController||c is CharacterSelectionController||c is SettingsMenuController||c is GameFlowController||c is UnityEngine.EventSystems.EventSystem||c is UnityEngine.InputSystem.UI.InputSystemUIInputModule).ToDictionary(c=>c,EditorJsonUtility.ToJson);
            var root=scene.GetRootGameObjects().Single(g=>g.name=="MainMenu").transform;var bg=root.Find("Canvas/Background");
            Assets();
            foreach(var selectable in bg.GetComponentsInChildren<Selectable>(true).Where(s=>s.GetComponent<MenuSelectableVisual>()!=null))Polish(selectable);
            Title(bg);Characters(bg.Find("CharacterSelection"));Settings(bg.Find("Settings"));
            var shade=bg.Find("LeftShade").GetComponent<Image>();shade.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/UI_Menu_Shade.png");shade.color=new Color(.01f,.025f,.016f,.95f);shade.rectTransform.anchorMax=new Vector2(.56f,1);
            foreach(var t in bg.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ContentCard"||t.name=="JourneyCard")){var image=t.GetComponent<Image>();image.sprite=Stone();image.type=UnityEngine.UI.Image.Type.Tiled;}
            Tikal.Editor.Art.MainMenuBrandArtworkAuthoring.ApplyVisuals(bg);
            Check(original.All(k=>EditorJsonUtility.ToJson(k.Key)==k.Value),"Original controllers, input actions, flow and persistence references unchanged");
            MainMenuFinalPolishValidation.ValidateScene(scene);EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Save MainMenu");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/Authoring.txt","Reference inspected: ART_REFERENCES/Menu/REFERENCE_MENU.png. Original backdrop/flow/controllers/settings persistence retained.\nTitle: layered TIKAL / EL EXPLORADOR, stepped masonry frame, aged-gold gradient; procedural local stone grain and soft left shade.\nFocus=bright gold outer frame+geometric cursor; saved=deep gold fill+inset frame+SELECCIONADO/check; hover=muted thin edge; locked=dark silhouette+geometric lock. Opt-in only for MainMenu; pause visuals retain legacy behavior.\nCamera/Reticle presentation adapts original TMP_Dropdown values/events, no settings service or enum change. Character selection still requires existing Confirm.\nPortraits rendered from CharacterCatalog real prefabs in isolated preview scenes, same 768x960 framing and three-light rig; no source prefab/material modifications.\n");Debug.Log("TIKAL_MAIN_MENU_FINAL_POLISH_SAVED");
        }
        public static Component[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
        public static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
        static Sprite Stone()=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/UI_Menu_Stone.png");
        static void Texture(string path,int width,int height,Func<int,int,Color> color)
        {
            if(File.Exists(path))return;var t=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color[width*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=color(x,y);t.SetPixels(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);ImportSprite(path);
        }
        static void ImportSprite(string path)
        {AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.mipmapEnabled=false;i.alphaIsTransparency=true;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=1024;i.wrapMode=path.Contains("Stone")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;i.SaveAndReimport();}
        static void Assets()
        {
            Folder(Art);Folder(Portraits);
            Texture(Art+"/UI_Menu_Stone.png",256,256,(x,y)=>{float n=Mathf.PerlinNoise(x*.065f,y*.065f)*.2f+Mathf.PerlinNoise(x*.63f,y*.63f)*.08f;return new Color(.79f+n,.81f+n,.78f+n,1);});
            Texture(Art+"/UI_Menu_Shade.png",256,4,(x,y)=>new Color(1,1,1,1-Mathf.SmoothStep(0,1,x/255f)));
            Texture(Portraits+"/UI_Portrait_Unknown.png",256,320,(x,y)=>{float xx=(x-128)/128f,yy=y/320f;bool head=xx*xx+Mathf.Pow((yy-.68f)*2.4f,2)<.22f*.22f;bool torso=xx*xx/.58f/.58f+Mathf.Pow((yy-.15f)/.37f,2)<1;return head||torso?new Color(.085f,.11f,.10f):new Color(.025f,.04f,.035f);});
            foreach(var c in CharacterCatalog.All.Where(c=>!c.IsLocked))if(!File.Exists(Portraits+"/UI_Portrait_"+c.DisplayName+".png"))Portrait(c);
        }
        static void Portrait(CharacterDefinition character)
        {
            Check(character.VisualPrefab!=null,"Real prefab "+character.DisplayName);var preview=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D output=null;bool animation=false;
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(character.VisualPrefab,preview);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var animator=model.GetComponentInChildren<Animator>(true);Check(animator!=null&&animator.isHuman,"Real Humanoid portrait");
                var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0);
                if(clip!=null){AnimationMode.StartAnimationMode();animation=true;AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(animator.gameObject,clip,.15f);AnimationMode.EndSampling();}
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);foreach(var r in renderers)r.updateWhenOffscreen=true;var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);float h=b.size.y;var head=animator.GetBoneTransform(HumanBodyBones.Head);var target=new Vector3(head.position.x,b.max.y-h*.16f,head.position.z);
                var go=new GameObject("Portrait Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(go,preview);var camera=go.GetComponent<Camera>();camera.scene=preview;camera.cameraType=CameraType.Preview;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.035f);camera.orthographic=true;camera.orthographicSize=h*.20f;camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.transform.position=target+new Vector3(h*.17f,h*.025f,h*2.2f);camera.transform.LookAt(target);camera.allowHDR=false;camera.allowMSAA=true;
                foreach(var q in new[]{(new Vector3(35,160,0),1.5f,new Color(1,.86f,.68f)),(new Vector3(10,225,0),.55f,new Color(.67f,.80f,.84f)),(new Vector3(20,20,0),.8f,new Color(.65f,.78f,.65f))}){var light=new GameObject("Portrait studio light",typeof(Light));SceneManager.MoveGameObjectToScene(light,preview);var l=light.GetComponent<Light>();l.type=LightType.Directional;l.intensity=q.Item2;l.color=q.Item3;l.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(q.Item1);}
                rt=new RenderTexture(768,960,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});var previous=RenderTexture.active;try{RenderTexture.active=rt;output=new Texture2D(768,960,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,768,960),0,0);output.Apply();File.WriteAllBytes(Portraits+"/UI_Portrait_"+character.DisplayName+".png",output.EncodeToPNG());}finally{RenderTexture.active=previous;}
                File.AppendAllText(Evidence+"/PortraitSources.txt",character.Id+" | "+AssetDatabase.GetAssetPath(character.VisualPrefab)+" | sourceGUID="+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(character.VisualPrefab))+" | bounds="+b+"\n");
            }
            finally{if(animation)AnimationMode.StopAnimationMode();if(output!=null)UnityEngine.Object.DestroyImmediate(output);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(preview);}
            ImportSprite(Portraits+"/UI_Portrait_"+character.DisplayName+".png");
        }
        static RectTransform Rect(string name,Transform parent)
        {var child=parent.Find(name);if(child!=null)return(RectTransform)child;var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return(RectTransform)go.transform;}
        static void Anchor(RectTransform r,Vector2 anchor,Vector2 p,Vector2 size,Vector2 pivot){r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=p;r.sizeDelta=size;}
        static void Stretch(RectTransform r,float inset=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
        static Image Image(string name,Transform parent,Color color){var r=Rect(name,parent);var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
        static TMP_Text Text(string name,Transform parent,string value,float size,Color color)
        {var r=Rect(name,parent);var t=r.GetComponent<TMP_Text>()??r.gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;return t;}
        static GameObject Border(Transform parent,string name,Color color,float thickness,float inset)
        {var r=Rect(name,parent);Stretch(r,inset);for(int i=0;i<4;i++){var line=Image("Edge"+i,r,color).rectTransform;bool hz=i<2;line.anchorMin=line.anchorMax=hz?new Vector2(.5f,i):new Vector2(i-2,.5f);if(hz){line.anchorMin=new Vector2(0,i);line.anchorMax=new Vector2(1,i);}else{line.anchorMin=new Vector2(i-2,0);line.anchorMax=new Vector2(i-2,1);}line.anchoredPosition=Vector2.zero;line.sizeDelta=hz?new Vector2(0,thickness):new Vector2(thickness,0);}return r.gameObject;}
        static void Polish(Selectable control,bool persistent=false)
        {
            var visual=control.GetComponent<MenuSelectableVisual>()??control.gameObject.AddComponent<MenuSelectableVisual>();var bg=control.GetComponent<Image>();if(control is Button&&bg!=null){bg.sprite=Stone();bg.type=UnityEngine.UI.Image.Type.Tiled;}
            var label=control.transform.Find("Label")?.GetComponent<TMP_Text>()??control.GetComponentInChildren<TMP_Text>(true);
            var cursor=Rect("FocusIndicator",control.transform);Anchor(cursor,new Vector2(0,.5f),new Vector2(10,0),new Vector2(7,7),new Vector2(.5f,.5f));var ci=cursor.GetComponent<Image>()??cursor.gameObject.AddComponent<Image>();ci.color=Focus;ci.raycastTarget=false;cursor.localRotation=Quaternion.Euler(0,0,45);
            GameObject marker=control.transform.Find("SavedMarker")?.gameObject;
            if(persistent&&marker==null){var m=Text("SavedMarker",control.transform,"SELECCIONADO",13,Gold);Anchor(m.rectTransform,new Vector2(.5f,0),new Vector2(0,10),new Vector2(170,22),new Vector2(.5f,0));marker=m.gameObject;}
            if(marker!=null){var text=marker.GetComponent<TMP_Text>();text.color=Gold;foreach(var g in marker.GetComponentsInChildren<Graphic>(true))g.color=Gold;}
            visual.Configure(control,bg,label,cursor.gameObject,marker,control.name=="Play"||control.name=="ConfirmCharacter");visual.ConfigureBorders(Border(control.transform,"FocusOutline",Focus,3,-5),marker!=null?Border(control.transform,"SavedOutline",Saved,2,4):null);visual.ConfigureFinalPolish(Border(control.transform,"HoverOutline",new Color(.65f,.60f,.40f),1,-2));
            Border(control.transform,"NormalOutline",new Color(.27f,.28f,.20f),1,0);
        }
        static void Title(Transform bg)
        {
            var title=bg.Find("Title").GetComponent<TMP_Text>();title.text="TIKAL";title.fontSize=118;title.characterSpacing=14;title.fontStyle=FontStyles.Bold;title.enableVertexGradient=true;title.color=Color.white;title.colorGradient=new VertexGradient(new Color(1,.90f,.64f),new Color(.90f,.73f,.40f),new Color(.49f,.34f,.16f),new Color(.77f,.57f,.27f));Anchor(title.rectTransform,new Vector2(0,1),new Vector2(96,-94),new Vector2(540,132),new Vector2(0,1));
            var depth=Text("TitleStoneDepth",bg,"TIKAL",118,new Color(.025f,.027f,.018f));depth.fontStyle=title.fontStyle;depth.characterSpacing=title.characterSpacing;depth.alignment=title.alignment;Anchor(depth.rectTransform,new Vector2(0,1),new Vector2(99,-99),new Vector2(540,132),new Vector2(0,1));if(depth.transform.GetSiblingIndex()>title.transform.GetSiblingIndex())depth.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
            var sub=bg.Find("BrandSubtitle").GetComponent<TMP_Text>();sub.text="EL EXPLORADOR";sub.fontSize=29;sub.characterSpacing=8;sub.color=Cream;Anchor(sub.rectTransform,new Vector2(0,1),new Vector2(100,-224),new Vector2(540,47),new Vector2(0,1));
            var frame=Rect("TitleMasonryFrame",bg);Anchor(frame,new Vector2(0,1),new Vector2(80,-84),new Vector2(590,202),new Vector2(0,1));Border(frame,"Frame",new Color(.43f,.33f,.17f,.7f),1,0);if(frame.GetSiblingIndex()>depth.transform.GetSiblingIndex())frame.SetSiblingIndex(depth.transform.GetSiblingIndex());
            for(int corner=0;corner<4;corner++)for(int step=0;step<3;step++){var a=new Vector2(corner%2,corner/2);float x=a.x==0?1:-1,y=a.y==0?1:-1;var r=Image("Step"+corner+"_"+step,frame,Gold).rectTransform;Anchor(r,a,new Vector2(x*(5+step*6),y*(5+step*6)),new Vector2(28-step*6,2),a);}
            var continueLine=bg.Find("Main/ContinueSubtitle").GetComponent<TMP_Text>();Anchor(continueLine.rectTransform,new Vector2(0,.5f),new Vector2(99,-342),new Vector2(520,30),new Vector2(0,.5f));
            var rule=Image("SubtitleGoldRule",bg,Gold).rectTransform;Anchor(rule,new Vector2(0,1),new Vector2(99,-219),new Vector2(515,2),new Vector2(0,1));var diamond=Image("TitleDiamond",bg,Gold).rectTransform;Anchor(diamond,new Vector2(0,1),new Vector2(357,-219),new Vector2(9,9),new Vector2(.5f,.5f));diamond.localRotation=Quaternion.Euler(0,0,45);
        }
        static void Characters(Transform panel)
        {
            for(int i=0;i<4;i++)
            {
                var card=panel.Find("Character0"+(i+1));var button=card.GetComponent<Button>();Anchor((RectTransform)card,new Vector2(.5f,.5f),new Vector2(-520+i*266,33),new Vector2(244,336),new Vector2(0,.5f));
                var label=card.Find("Label").GetComponent<TMP_Text>();label.text=CharacterCatalog.All[i].DisplayName.ToUpperInvariant();label.fontSize=i==3?19:24;label.enableAutoSizing=i==3;label.fontSizeMin=14;label.fontSizeMax=19;label.alignment=TextAlignmentOptions.Center;label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,0);label.rectTransform.pivot=new Vector2(.5f,0);label.rectTransform.sizeDelta=new Vector2(220,42);label.rectTransform.anchoredPosition=new Vector2(0,47);
                var preview=Image("PreviewSurface",card,Color.white);Anchor(preview.rectTransform,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(218,232),new Vector2(.5f,1));preview.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Portraits+"/UI_Portrait_"+(i==3?"Unknown":CharacterCatalog.All[i].DisplayName)+".png");preview.preserveAspect=true;var old=preview.transform.Find("Placeholder");if(old!=null)old.gameObject.SetActive(false);
                var marker=card.Find("SavedMarker").GetComponent<TMP_Text>();marker.alignment=TextAlignmentOptions.Center;Anchor(marker.rectTransform,new Vector2(.5f,0),new Vector2(0,14),new Vector2(200,24),new Vector2(.5f,0));
                Polish(button,true);var cursor=(RectTransform)card.Find("FocusIndicator");Anchor(cursor,new Vector2(0,1),new Vector2(14,-14),new Vector2(8,8),new Vector2(.5f,.5f));cursor.SetAsLastSibling();
                if(i==3){button.interactable=false;var lockRoot=Rect("UnknownLock",preview.transform);Anchor(lockRoot,new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,50),new Vector2(.5f,.5f));var box=Image("Body",lockRoot,MenuVisualPalette.Muted).rectTransform;Anchor(box,new Vector2(.5f,0),Vector2.zero,new Vector2(38,26),new Vector2(.5f,0));var ring=Rect("Shackle",lockRoot);Anchor(ring,new Vector2(.5f,1),Vector2.zero,new Vector2(25,28),new Vector2(.5f,1));Border(ring,"Ring",MenuVisualPalette.Muted,4,0);var key=Image("Keyhole",box,MenuVisualPalette.Ink).rectTransform;Anchor(key,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4,10),new Vector2(.5f,.5f));marker.gameObject.SetActive(false);}
            }
            var confirm=panel.Find("ConfirmCharacter").GetComponent<Selectable>();var back=panel.Find("Back").GetComponent<Selectable>();var cs=Enumerable.Range(1,3).Select(i=>panel.Find("Character0"+i).GetComponent<Selectable>()).ToArray();for(int i=0;i<3;i++)Nav(cs[i],cs[i],confirm,cs[Mathf.Max(0,i-1)],cs[Mathf.Min(2,i+1)]);Nav(confirm,cs[2],back,confirm,confirm);Nav(back,confirm,back,back,back);
        }
        static void Settings(Transform panel)
        {
            Segment(panel,"CameraMode",new[]{"PRIMERA\nPERSONA","TERCERA\nPERSONA"},50,-180);
            Segment(panel,"Reticle",new[]{"SIN\nPUNTERO","PUNTO","CRUZ"},-490,-180);
            var camera=panel.Find("CameraMode").GetComponent<MenuSegmentedChoice>().Segments;var reticle=panel.Find("Reticle").GetComponent<MenuSegmentedChoice>().Segments;var back=panel.Find("Back").GetComponent<Selectable>();var sfx=panel.Find("SFX").GetComponent<Selectable>();var invert=panel.Find("InvertY").GetComponent<Selectable>();var mouse=panel.Find("Mouse").GetComponent<Selectable>();
            var sfxNav=sfx.navigation;sfxNav.selectOnDown=reticle[0];sfx.navigation=sfxNav;var inv=invert.navigation;inv.selectOnDown=camera[0];invert.navigation=inv;
            for(int i=0;i<3;i++)Nav(reticle[i],sfx,back,reticle[Mathf.Max(0,i-1)],i<2?reticle[i+1]:camera[0]);for(int i=0;i<2;i++)Nav(camera[i],invert,back,i==0?reticle[2]:camera[0],camera[Mathf.Min(1,i+1)]);Nav(back,reticle[0],back,back,back);
            Anchor((RectTransform)panel.Find("CameraModeLabel"),new Vector2(.5f,.5f),new Vector2(50,-118),new Vector2(430,30),new Vector2(0,.5f));Anchor((RectTransform)panel.Find("ReticleLabel"),new Vector2(.5f,.5f),new Vector2(-490,-152),new Vector2(430,28),new Vector2(0,.5f));panel.Find("InterfaceSection").GetComponent<TMP_Text>().text="PUNTERO";
        }
        static void Segment(Transform panel,string name,string[] labels,float x,float y)
        {
            var dropdown=panel.Find(name).GetComponent<TMP_Dropdown>();var r=(RectTransform)dropdown.transform;Anchor(r,new Vector2(.5f,.5f),new Vector2(x,y),new Vector2(430,91),new Vector2(0,1));dropdown.enabled=false;dropdown.GetComponent<Image>().enabled=false;dropdown.GetComponent<MenuSelectableVisual>().enabled=false;dropdown.GetComponent<MenuSettingStatePresenter>().enabled=false;
            foreach(var n in new[]{"Label","Arrow","FocusIndicator","FocusOutline","HoverOutline","NormalOutline"}){var t=r.Find(n);if(t!=null)t.gameObject.SetActive(false);}
            var buttons=new List<Button>();float width=(430-(labels.Length-1)*8)/(float)labels.Length;
            for(int i=0;i<labels.Length;i++)
            {var t=Image("ActiveOption"+i,r,MenuVisualPalette.Stone);Anchor(t.rectTransform,new Vector2(0,1),new Vector2(i*(width+8),0),new Vector2(width,91),new Vector2(0,1));t.raycastTarget=true;var button=t.GetComponent<Button>()??t.gameObject.AddComponent<Button>();button.targetGraphic=t;var text=Text("OptionName",t.transform,labels[i],16,Cream);Anchor(text.rectTransform,new Vector2(.5f,1),new Vector2(0,-8),new Vector2(width-25,43),new Vector2(.5f,1));var old=t.transform.Find("ActiveMarker");if(old!=null)old.gameObject.SetActive(false);Polish(button,true);var visual=button.GetComponent<MenuSelectableVisual>();var data=new SerializedObject(visual);data.FindProperty("_label").objectReferenceValue=text;data.ApplyModifiedPropertiesWithoutUndo();var marker=t.transform.Find("SavedMarker").GetComponent<TMP_Text>();marker.fontSize=11;marker.rectTransform.sizeDelta=new Vector2(width-8,20);marker.rectTransform.anchoredPosition=new Vector2(0,7);buttons.Add(button);}
            var presenter=dropdown.GetComponent<MenuSegmentedChoice>()??dropdown.gameObject.AddComponent<MenuSegmentedChoice>();var so=new SerializedObject(presenter);so.FindProperty("_source").objectReferenceValue=dropdown;var array=so.FindProperty("_segments");array.arraySize=buttons.Count;for(int i=0;i<buttons.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];so.ApplyModifiedPropertiesWithoutUndo();presenter.RefreshVisual();
        }
        static void Nav(Selectable c,Selectable up,Selectable down,Selectable left,Selectable right)=>c.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down,selectOnLeft=left,selectOnRight=right};
    }
}
