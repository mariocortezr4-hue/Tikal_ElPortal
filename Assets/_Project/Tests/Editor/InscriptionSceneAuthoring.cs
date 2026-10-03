using System;
using System.Linq;
using Tikal.Interaction;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Tikal.Tests.Editor
{
    public static class InscriptionSceneAuthoring
    {
        internal static void Set(UnityEngine.Object target,string field,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new Exception("Unknown property "+field);
            if(value is string s)p.stringValue=s;
            else if(value is UnityEngine.Object[] a){p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];}
            else p.objectReferenceValue=(UnityEngine.Object)value;so.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static ReadableInscription Build(Transform parent,string name,Vector3 position,string text)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=position;g.transform.rotation=Quaternion.Euler(0,270,0);
            var readable=g.AddComponent<ReadableInscription>();Set(readable,"_title","Inscripción");Set(readable,"_text",text);
            var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="Provisional inscription stone";stone.transform.SetParent(g.transform,false);stone.transform.localPosition=new Vector3(0,.65f,0);stone.transform.localScale=new Vector3(.8f,1.3f,.3f);stone.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralStone.mat");
            return readable;
        }
        private static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,Vector2 position,Vector2 size,int points)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);var rect=(RectTransform)g.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var t=g.GetComponent<TMP_Text>();t.font=font;t.fontSize=points;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.richText=false;t.raycastTarget=false;t.text="";return t;
        }
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var root=scene.GetRootGameObjects().Single().transform;
                var zone=root.Find("Environment/Zone02_PathGuardians");if(zone.Find("Guidance inscription")!=null)throw new Exception("Inscription already exists.");
                var readable=Build(zone,"Guidance inscription",new Vector3(30.5f,1.23f,10.5f),"Debes ordenar el patrón.");
                var canvas=root.GetComponentInChildren<Canvas>(true);var ui=new GameObject("Readable presentation",typeof(RectTransform));ui.transform.SetParent(canvas.transform,false);var r=(RectTransform)ui.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var presenter=ui.AddComponent<InscriptionPresenter>();var panel=new GameObject("Reading panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(ui.transform,false);var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.2f);rect.sizeDelta=new Vector2(570,125);
                var image=panel.GetComponent<Image>();image.color=new Color(.045f,.055f,.05f,.92f);image.raycastTarget=false;
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");var title=Label(panel.transform,"Optional title",font,new Vector2(0,30),new Vector2(530,28),19);var body=Label(panel.transform,"Readable text",font,new Vector2(0,-14),new Vector2(530,58),24);
                Set(presenter,"_inscriptions",new UnityEngine.Object[]{readable});Set(presenter,"_panel",panel);Set(presenter,"_title",title);Set(presenter,"_text",body);panel.SetActive(false);
                EditorSceneManager.SaveScene(scene);Debug.Log("TIKAL_INSCRIPTION_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
