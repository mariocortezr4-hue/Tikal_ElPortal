using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Player;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Level01MapHUDAuthoring
    {
        public const string ScenePath="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        public const string DefinitionPath="Assets/_Project/Data/Level01ExplorationMap.asset";
        public const string Evidence="../_Level01MapHUD_20261023";
        const string Root="Level01 Map HUD";
        static readonly Color Gold=new Color(.7f,.56f,.3f,.85f),TextColor=new Color(.94f,.92f,.82f);
        static Level01MapHUDAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!File.Exists("Temp/Level01MapHUDApply.request"))return;File.Delete("Temp/Level01MapHUDApply.request");try{Apply();Apply();Level01MapPresentationValidation.ValidateStatic();}catch(Exception e){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/AuthoringError.txt",e.ToString());Debug.LogException(e);}
        }
        public static T[] All<T>() where T:Component=>EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
        {var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
        [MenuItem("Tikal/Authoring/Apply Level01 Map HUD")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Application.isBatchMode)throw new Exception("Use normal Editor Edit Mode.");
            var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save pending user scene edits before Map HUD authoring.");if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(Evidence);if(!File.Exists(Evidence+"/Before.unity"))File.Copy(ScenePath,Evidence+"/Before.unity");
            var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");var global=asset.FindActionMap("Global",true);var action=global.FindAction("Map");
            if(action==null)
            {
                action=global.AddAction("Map",InputActionType.Button,expectedControlLayout:"Button");action.AddBinding("<Keyboard>/m",groups:"KeyboardMouse");action.AddBinding("<Gamepad>/dpad/up",groups:"Gamepad");File.WriteAllText(AssetDatabase.GetAssetPath(asset),asset.ToJson());AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));File.WriteAllText("Temp/Level01MapHUDApply.request","Resume after wrapper regeneration");return;
            }
            if(All<LevelMapHUD>().Length>0){if(All<LevelMapHUD>().Length!=1||!All<LevelMapHUD>().Single().IsConfigured)throw new Exception("Existing Map HUD is not valid; preserve for inspection.");Level01MapHUDStyling.Apply(All<LevelMapHUD>().Single());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("LEVEL01_MAP_HUD_IDEMPOTENT");return;}
            var player=All<PlayerController>().Single();var canvas=All<ReticlePresenter>().Single().GetComponentInParent<Canvas>();var pause=All<PauseMenuController>().Single();
            var definition=AssetDatabase.LoadAssetAtPath<LevelMapDefinition>(DefinitionPath);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<LevelMapDefinition>();var env=scene.GetRootGameObjects().Single().transform.Find("Environment/Terrain");var names=new[]{"Surface_SpawnArea","Surface_Entrance","Surface_Zone01_MuralFragments","Surface_Zone02_PathGuardians","Surface_Zone03_EchoChamber","Surface_Zone04_GlyphGallery","Surface_Zone05_Revelation","Surface_01_to_02","Surface_02_to_03","Surface_03_to_04","Surface_04_to_05"};
                definition.publicAreas=names.Select(n=>{var b=env.Find(n).GetComponent<Renderer>().bounds;return new LevelMapDefinition.Area{id=n,worldXZ=UnityEngine.Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z)};}).ToArray();
                var areas=definition.publicAreas;definition.worldBounds=UnityEngine.Rect.MinMaxRect(areas.Min(a=>a.worldXZ.xMin)-2,areas.Min(a=>a.worldXZ.yMin)-2,areas.Max(a=>a.worldXZ.xMax)+2,areas.Max(a=>a.worldXZ.yMax)+2);AssetDatabase.CreateAsset(definition,DefinitionPath);
            }
            var root=Rect(Root,canvas.transform,Vector2.zero,Vector2.one);var hud=root.gameObject.AddComponent<LevelMapHUD>();
            // Keep the listener independent from its two visibility panels.
            var small=Fixed("Minimap and hints",root,new Vector2(-32,-32),new Vector2(224,290),Vector2.one,Vector2.one);
            var stone=Fixed("Minimap stone",small,Vector2.zero,new Vector2(224,224),new Vector2(0,1),new Vector2(0,1));Panel(stone,true);
            Label("Minimap title",stone,"EXPLORACIÓN",new Vector2(14,-9),new Vector2(170,24),15,TextAlignmentOptions.Left);
            Label("North",stone,"N",new Vector2(192,-9),new Vector2(20,24),15,TextAlignmentOptions.Center).color=Gold;
            var smallMap=Map("Public map",stone,definition,new Vector2(10,8),new Vector2(-10,-31));var smallMarker=Marker(smallMap.transform,13);
            var mapHint=Label("Map binding hint",small,"[M] MAPA",new Vector2(0,-232),new Vector2(224,24),17,TextAlignmentOptions.Right);
            var journalHint=Label("Journal binding hint",small,"[J] VER DIARIO",new Vector2(0,-258),new Vector2(224,24),17,TextAlignmentOptions.Right);
            var expanded=Rect("Expanded map overlay",root,Vector2.zero,Vector2.one);var dim=expanded.gameObject.AddComponent<Image>();dim.color=new Color(.006f,.012f,.009f,.82f);dim.raycastTarget=true;
            var card=Rect("Map stone card",expanded,new Vector2(.12f,.12f),new Vector2(.88f,.88f));Panel(card,true);
            var title=Rect("Map heading",card,new Vector2(.08f,.9f),new Vector2(.92f,.98f));Text(title,"LEVEL 1 · MAPA",30,TextAlignmentOptions.Center).color=Gold;
            var largeMap=Map("Public map",card,definition,new Vector2(28,76),new Vector2(-28,-82));var largeMarker=Marker(largeMap.transform,17);
            var footer=Rect("Close hint",card,new Vector2(.04f,.025f),new Vector2(.96f,.085f));var closeHint=Text(footer,"[M] CERRAR",19,TextAlignmentOptions.Center);
            var legend=Rect("Map legend",card,new Vector2(.1f,.085f),new Vector2(.9f,.13f));Text(legend,"TU POSICIÓN · NORTE ARRIBA",14,TextAlignmentOptions.Center).color=new Color(.7f,.74f,.65f);
            Set(hud,"_player",player);Set(hud,"_input",player.GetComponent<PlayerInputReader>());Set(hud,"_devices",player.GetComponent<InputDeviceManager>());Set(hud,"_journal",All<ClueJournalPresenter>().Single());Set(hud,"_pause",pause);Set(hud,"_outcome",All<LevelOutcomeController>().Single());Set(hud,"_checkpoints",All<CheckpointManager>().Single());Set(hud,"_flow",All<GameFlowController>().Single());Set(hud,"_definition",definition);Set(hud,"_small",small.gameObject);Set(hud,"_expanded",expanded.gameObject);Set(hud,"_smallMap",smallMap);Set(hud,"_largeMap",largeMap);Set(hud,"_smallMarker",smallMarker);Set(hud,"_largeMarker",largeMarker);Set(hud,"_mapHint",mapHint);Set(hud,"_journalHint",journalHint);Set(hud,"_closeHint",closeHint);
            var pauseRoot=pause.transform;while(pauseRoot.parent!=canvas.transform)pauseRoot=pauseRoot.parent;root.SetSiblingIndex(pauseRoot.GetSiblingIndex());expanded.gameObject.SetActive(false);
            if(!hud.IsConfigured)throw new Exception("Map HUD references incomplete");Level01MapHUDStyling.Apply(hud);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Map HUD scene save failed");AssetDatabase.SaveAssets();Debug.Log("LEVEL01_MAP_HUD_AUTHORED");
        }
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        static RectTransform Fixed(string name,Transform parent,Vector2 pos,Vector2 size,Vector2 anchor,Vector2 pivot)
        {var r=Rect(name,parent,anchor,anchor);r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=pos;return r;}
        static void Panel(RectTransform rect,bool border)
        {var image=rect.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/MainMenu/UI_Menu_Stone.png");image.type=Image.Type.Tiled;image.color=new Color(.18f,.23f,.18f,.97f);image.raycastTarget=false;if(border){var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(1.2f,-1.2f);}}
        static TMP_Text Text(RectTransform rect,string value,int size,TextAlignmentOptions alignment)
        {var t=rect.gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");t.text=value;t.fontSize=size;t.color=TextColor;t.alignment=alignment;t.raycastTarget=false;t.richText=false;return t;}
        static TMP_Text Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,TextAlignmentOptions align)
        =>Text(Fixed(name,parent,pos,size,new Vector2(0,1),new Vector2(0,1)),value,fontSize,align);
        static ExplorationMapGraphic Map(string name,Transform parent,LevelMapDefinition definition,Vector2 min,Vector2 max)
        {var r=Rect(name,parent,Vector2.zero,Vector2.one);r.offsetMin=min;r.offsetMax=max;var g=r.gameObject.AddComponent<ExplorationMapGraphic>();g.definition=definition;g.raycastTarget=false;return g;}
        static RectTransform Marker(Transform parent,float size)
        {var r=Fixed("Player marker",parent,Vector2.zero,new Vector2(size,size),new Vector2(.5f,.5f),new Vector2(.5f,.5f));r.gameObject.AddComponent<MapPlayerMarkerGraphic>().raycastTarget=false;return r;}
    }
}
