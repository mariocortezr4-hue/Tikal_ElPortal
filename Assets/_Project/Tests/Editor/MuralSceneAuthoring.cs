using System;
using System.Linq;
using Tikal.Puzzles;
using Tikal.Puzzles.Level01;
using Tikal.Puzzles.Clues;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Tikal.Tests.Editor
{
    // Explicit authoring tool; never runs on import. Refuses duplicate installation.
    public static class MuralSceneAuthoring
    {
        public static void FixDiscoveryLayout()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
            var presenter=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ClueDiscoveryPresenter>(true)).Single();
            var rect=presenter.GetComponent<RectTransform>();
            if(rect==null) rect=presenter.gameObject.AddComponent<RectTransform>();
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var puzzle=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MuralFragmentsPuzzle>(true)).Single();
            puzzle.transform.Find("Intact pattern reference").localPosition=new Vector3(-.95f,-1.45f,-.19f);
            EditorSceneManager.SaveScene(scene);if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        public static void Preview()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var mural=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MuralFragmentsPuzzle>(true)).Single();
                var camera=new GameObject("Temporary preview camera").AddComponent<Camera>();
                camera.transform.position=new Vector3(-6,2.7f,11.5f);camera.transform.LookAt(mural.transform.position);camera.fieldOfView=55;camera.nearClipPlane=.05f;
                var target=new RenderTexture(1280,960,24);target.Create();
                string folder=System.IO.Path.GetTempPath();
                for(int view=0;view<2;view++)
                {
                    if(view==1)
                    {
                        var so=new SerializedObject(mural);var indices=so.FindProperty("_solution");var fragments=so.FindProperty("_fragments");
                        for(int i=0;i<4;i++)
                        {
                            var piece=(MuralFragment)fragments.GetArrayElementAtIndex(i).objectReferenceValue;
                            var visual=(Transform)new SerializedObject(piece).FindProperty("_visual").objectReferenceValue;
                            visual.localRotation=Quaternion.Euler(0,0,indices.GetArrayElementAtIndex(i).intValue*90);
                        }
                    }
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination=target });
                    RenderTexture.active=target;var image=new Texture2D(1280,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,view==0?"TikalMuralInitial.png":"TikalMuralSolved.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                RenderTexture.active=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                Debug.Log("TIKAL_MURAL_PREVIEWS_CREATED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void Set(UnityEngine.Object o,string key,object value)
        {
            var so=new SerializedObject(o);var p=so.FindProperty(key);
            if(p==null) throw new Exception("Unknown serialized property: "+key);
            if(value is int n)p.intValue=n;
            else if(value is string s)p.stringValue=s;
            else if(value is UnityEngine.Object[] a){p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=a[i];}
            else if(value is int[] ns){p.arraySize=ns.Length;for(int i=0;i<ns.Length;i++)p.GetArrayElementAtIndex(i).intValue=ns[i];}
            else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Child(string name,Transform parent,Vector3 position)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
        static Material Material(string name,Color color)
        {
            string path="Assets/_Project/Art/Materials/Level01/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m!=null) return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);return m;
        }
        static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static void Stroke(Transform parent,Vector2 a,Vector2 b,float z,float width,Material material)
        {
            Vector2 delta=b-a;var g=Cube("Carved stroke",parent,new Vector3((a.x+b.x)/2,(a.y+b.y)/2,z),new Vector3(delta.magnitude,width,.035f),material);
            g.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string content,Vector2 pos,Vector2 size,float pointSize,Color color)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(parent,false);
            var rect=(RectTransform)g.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;
            var t=g.GetComponent<TMP_Text>();t.font=font;t.text=content;t.fontSize=pointSize;t.color=color;t.alignment=TextAlignmentOptions.Center;t.richText=false;t.raycastTarget=false;return t;
        }
        [MenuItem("Tikal/Authoring/Install first mural (once)")]
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var root=scene.GetRootGameObjects().Single().transform;
                var anchor=root.Find("Environment/Zone01_MuralFragments/PuzzleAnchor");
                if(anchor.Find("MuralFragments")!=null) throw new Exception("Mural already installed; refusing duplicate authoring.");
                var stone=Material("MuralStone",new Color(.30f,.32f,.29f));
                var markings=Material("MuralCarving",new Color(.87f,.65f,.29f));
                var frame=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/EntranceStone.mat");
                var mural=Child("MuralFragments",anchor,new Vector3(0,1.5f,.4f));
                var puzzle=mural.AddComponent<MuralFragmentsPuzzle>();
                Set(puzzle,"_definition",AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.mural_fragments.asset"));
                Set(puzzle,"_initialState",(int)PuzzleState.Available);
                int[] solution={1,3,0,2},initial={2,0,1,3};
                Cube("Stone backing",mural.transform,new Vector3(0,0,.20f),new Vector3(3.1f,3.1f,.18f),frame,true);
                foreach(float x in new[]{-1.5f,1.5f}) Cube("Outer border",mural.transform,new Vector3(x,0,0),new Vector3(.16f,3.15f,.3f),frame,true);
                foreach(float y in new[]{-1.5f,1.5f}) Cube("Outer border",mural.transform,new Vector3(0,y,0),new Vector3(3.15f,.16f,.3f),frame,true);
                var pieces=new MuralFragment[4];
                Vector2[] offsets={new Vector2(-.67f,.67f),new Vector2(.67f,.67f),new Vector2(-.67f,-.67f),new Vector2(.67f,-.67f)};
                float[] starts={90,0,180,270};
                for(int i=0;i<4;i++)
                {
                    Vector2 offset=offsets[i];var holder=Child("Fragment "+(char)('A'+i),mural.transform,new Vector3(offset.x,offset.y,-.05f));
                    var fragment=holder.AddComponent<MuralFragment>();pieces[i]=fragment;
                    var pivot=Child("Rotation pivot",holder.transform,Vector3.zero).transform;
                    Cube("Stone fragment",pivot,Vector3.zero,new Vector3(1.27f,1.27f,.22f),stone,true);
                    var carving=Child("Quarter of circular path",pivot,Vector3.zero).transform;
                    carving.localRotation=Quaternion.Euler(0,0,-solution[i]*90f);
                    for(int n=0;n<12;n++)
                    {
                        float a=(starts[i]+n*7.5f)*Mathf.Deg2Rad,b=(starts[i]+(n+1)*7.5f)*Mathf.Deg2Rad;
                        Stroke(carving,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*1.08f-offset,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*1.08f-offset,-.14f,.055f,markings);
                    }
                    Vector2 direction=offset.normalized;
                    Stroke(carving,direction*.14f-offset,direction*.77f-offset,-.14f,.055f,markings);
                    Set(fragment,"_puzzle",puzzle);Set(fragment,"_visual",pivot);Set(fragment,"_initialOrientation",initial[i]);
                    pivot.localRotation=Quaternion.Euler(0,0,initial[i]*90f);
                }
                Set(puzzle,"_fragments",pieces.Cast<UnityEngine.Object>().ToArray());Set(puzzle,"_solution",solution);
                // Intact small carving on the border: environmental reference without instructions.
                var reference=Child("Intact pattern reference",mural.transform,new Vector3(-.95f,-1.45f,-.19f)).transform;
                for(int n=0;n<24;n++)
                {
                    float a=n*15*Mathf.Deg2Rad,b=(n+1)*15*Mathf.Deg2Rad;
                    Stroke(reference,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.10f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.10f,0,.018f,markings);
                }
                Stroke(reference,new Vector2(-.07f,-.07f),new Vector2(.07f,.07f),0,.018f,markings);
                Stroke(reference,new Vector2(-.07f,.07f),new Vector2(.07f,-.07f),0,.018f,markings);
                var tracker=Child("SessionClues",root.Find("Systems"),Vector3.zero).AddComponent<ClueTracker>();
                var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.mural_fragments.asset");
                Set(clue,"_id","level01.clue01");Set(clue,"_title","El camino");Set(clue,"_description","El camino verdadero no siempre está delante de ti.");EditorUtility.SetDirty(clue);
                var reward=mural.AddComponent<PuzzleClueReward>();Set(reward,"_puzzle",puzzle);Set(reward,"_tracker",tracker);Set(reward,"_clue",clue);
                var light=Child("Solved warm light",mural.transform,new Vector3(0,.7f,-1.2f)).AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.37f);light.intensity=2;light.range=4;light.shadows=LightShadows.None;light.enabled=false;
                var feedback=mural.AddComponent<MuralSolvedFeedback>();Set(feedback,"_puzzle",puzzle);Set(feedback,"_light",light);
                var canvas=root.GetComponentsInChildren<Canvas>(true).Single();
                var ui=new GameObject("ProvisionalDiscovery",typeof(RectTransform));ui.transform.SetParent(canvas.transform,false);
                var uiRect=(RectTransform)ui.transform;uiRect.anchorMin=Vector2.zero;uiRect.anchorMax=Vector2.one;uiRect.offsetMin=uiRect.offsetMax=Vector2.zero;
                var presenter=ui.AddComponent<ClueDiscoveryPresenter>();
                var panel=new GameObject("Discovery panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(ui.transform,false);
                var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(590,150);
                var image=panel.GetComponent<Image>();image.color=new Color(.045f,.055f,.05f,.92f);image.raycastTarget=false;
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");
                Text("Eyebrow",panel.transform,font,"DESCUBRIMIENTO",new Vector2(0,48),new Vector2(550,24),16,new Color(.87f,.65f,.29f));
                var title=Text("Clue title",panel.transform,font,"",new Vector2(0,12),new Vector2(550,36),26,Color.white);
                var desc=Text("Clue text",panel.transform,font,"",new Vector2(0,-34),new Vector2(550,50),20,new Color(.87f,.88f,.83f));
                Set(presenter,"_tracker",tracker);Set(presenter,"_panel",panel);Set(presenter,"_title",title);Set(presenter,"_description",desc);panel.SetActive(false);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_MURAL_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
