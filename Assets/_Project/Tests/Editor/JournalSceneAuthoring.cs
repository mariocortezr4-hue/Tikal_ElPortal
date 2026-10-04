using System;
using System.Linq;
using Tikal.Input;
using Tikal.Player;
using Tikal.Puzzles.Clues;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Tikal.Tests.Editor
{
    public static class JournalSceneAuthoring
    {
        internal static void Set(UnityEngine.Object o,string field,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var g=new GameObject(name,typeof(RectTransform));if(parent!=null)g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return g;
        }
        static void Colour(GameObject g,Color colour){var image=g.AddComponent<Image>();image.color=colour;image.raycastTarget=false;}
        static TMP_Text Text(string name,Transform parent,Vector2 min,Vector2 max,string content,int size,Color colour)
        {
            var g=Rect(name,parent,min,max);var t=g.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");t.text=content;t.fontSize=size;t.color=colour;t.richText=false;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Left;return t;
        }
        public static void Preview()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                var journal=all.OfType<ClueJournalPresenter>().Single();var s=new SerializedObject(journal);var body=(TMP_Text)s.FindProperty("_content").objectReferenceValue;
                var clues=new[]{"mural_fragments","path_guardians","echo_chamber"}.Select(n=>AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01."+n+".asset"));
                body.text=string.Join("\n\n\n",clues.OrderBy(c=>c.Order).Select(c=>c.Title.ToUpperInvariant()+"\n\n"+c.Description));
                var action=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions").FindAction("Global/Journal");
                ((TMP_Text)s.FindProperty("_closeHint").objectReferenceValue).text="[ "+InputPromptResolver.Resolve(action,InputDeviceFamily.KeyboardMouse).DisplayText+" ]  Cerrar diario";
                ((GameObject)s.FindProperty("_panel").objectReferenceValue).SetActive(true);
                var canvas=all.OfType<Canvas>().Single();var camera=all.OfType<Camera>().Single();var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();body.ForceMeshUpdate();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target});
                RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TikalJournalPreview.png"),image.EncodeToPNG());
                RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
                Debug.Log("TIKAL_JOURNAL_PREVIEW_RENDERED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }

        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                string promptPath="Assets/_Project/Prefabs/UI/InteractionPrompt.prefab";
                var prefab=PrefabUtility.LoadPrefabContents(promptPath);
                try
                {
                    var context=(RectTransform)prefab.transform.Find("Context");context.anchorMin=context.anchorMax=Vector2.zero;context.pivot=Vector2.zero;context.anchoredPosition=new Vector2(50,50);
                    prefab.GetComponentInChildren<TMP_Text>(true).alignment=TextAlignmentOptions.Left;PrefabUtility.SaveAsPrefabAsset(prefab,promptPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                string path="Assets/_Project/Prefabs/UI/ClueJournal.prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)throw new Exception("Journal already exists.");
                var root=Rect("ClueJournal",null,Vector2.zero,Vector2.one);var presenter=root.AddComponent<ClueJournalPresenter>();
                var panel=Rect("Journal panel",root.transform,Vector2.zero,Vector2.one);Colour(panel,new Color(.012f,.02f,.016f,.85f));
                var card=Rect("Journal card",panel.transform,new Vector2(.19f,.08f),new Vector2(.81f,.92f));Colour(card,new Color(.04f,.055f,.047f,.99f));
                var title=Text("Journal title",card.transform,new Vector2(.07f,.85f),new Vector2(.93f,.94f),"PISTAS",34,new Color(.88f,.7f,.38f));title.characterSpacing=5;
                var line=Rect("Header divider",card.transform,new Vector2(.07f,.825f),new Vector2(.93f,.828f));Colour(line,new Color(.6f,.48f,.25f,.75f));
                var content=Text("Discovered clues",card.transform,new Vector2(.07f,.16f),new Vector2(.93f,.78f),"",24,new Color(.9f,.91f,.86f));content.alignment=TextAlignmentOptions.TopLeft;content.enableAutoSizing=true;content.fontSizeMin=18;content.fontSizeMax=24;content.lineSpacing=6;
                var footer=Text("Close hint",card.transform,new Vector2(.07f,.055f),new Vector2(.93f,.11f),"",19,new Color(.71f,.74f,.68f));
                Set(presenter,"_panel",panel);Set(presenter,"_content",content);Set(presenter,"_closeHint",footer);panel.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                var canvas=all.OfType<Canvas>().Single();var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),canvas.transform);var r=(RectTransform)instance.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                presenter=instance.GetComponent<ClueJournalPresenter>();Set(presenter,"_tracker",all.OfType<ClueTracker>().Single());Set(presenter,"_inputReader",all.OfType<PlayerInputReader>().Single());Set(presenter,"_player",all.OfType<PlayerController>().Single());Set(presenter,"_devices",all.OfType<InputDeviceManager>().Single());
                var ds=new SerializedObject(all.OfType<ClueDiscoveryPresenter>().Single());var discovery=(RectTransform)((GameObject)ds.FindProperty("_panel").objectReferenceValue).transform;discovery.anchorMin=discovery.anchorMax=new Vector2(.5f,1);discovery.pivot=new Vector2(.5f,1);discovery.anchoredPosition=new Vector2(0,-50);
                var prompt=all.OfType<InteractionPromptPresenter>().Single();var ps=new SerializedObject(prompt);var contextScene=(RectTransform)((GameObject)ps.FindProperty("_promptRoot").objectReferenceValue).transform;
                // Remove obsolete position overrides so future instances inherit the prefab's permanent location.
                foreach(string field in new[]{"m_AnchorMin","m_AnchorMax","m_Pivot","m_AnchoredPosition"})PrefabUtility.RevertPropertyOverride(new SerializedObject(contextScene).FindProperty(field),InteractionMode.AutomatedAction);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TIKAL_JOURNAL_UI_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
