using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Player;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Level01;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace Tikal.Tests.Editor
{
    public static class EndingSceneAuthoring
    {
        const string ScenePath="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        static string Hierarchy(Transform t){string name=t.name;while(t.parent!=null){t=t.parent;name=t.name+"/"+name;}return name;}
        public static void Preview()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                all.First(t=>t.name=="FalsePath").gameObject.SetActive(true);
                all.First(t=>t.name=="Revelation side seal").gameObject.SetActive(false);
                var camera=all.Select(t=>t.GetComponent<Camera>()).First(c=>c!=null);
                camera.transform.position=new Vector3(6,2.2f,75);
                camera.transform.LookAt(new Vector3(6,1.8f,85));
                Capture(camera,"TikalEnding-Path.png");
                var canvas=all.Select(t=>t.GetComponent<Canvas>()).First(c=>c!=null);
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
                all.First(t=>t.name=="Result panel").gameObject.SetActive(true);
                all.First(t=>t.name=="Result panel").GetComponent<CanvasGroup>().alpha=1;
                var title=all.First(t=>t.name=="Result title").GetComponent<TMP_Text>();
                var subtitle=all.First(t=>t.name=="Result subtitle").GetComponent<TMP_Text>();
                title.text="NIVEL COMPLETADO";subtitle.text="La Entrada Secreta";Canvas.ForceUpdateCanvases();
                Capture(camera,"TikalEnding-Victory.png");
                title.text="HAS QUEDADO ATRAPADO";subtitle.text="";Canvas.ForceUpdateCanvases();
                Capture(camera,"TikalEnding-Defeat.png");
                Debug.Log("TIKAL_ENDING_PREVIEW_PASSED (no scene saved)");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,string file)
        {
            var target=new RenderTexture(1280,720,24);target.Create();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),file),image.EncodeToPNG());
            RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
        public static void Inspect()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var root=scene.GetRootGameObjects().Single();
                foreach(var c in root.GetComponentsInChildren<Collider>())
                    if(c.bounds.Intersects(new Bounds(new Vector3(6,2,80),new Vector3(5,4,16))))
                        Debug.Log("ENDING_COLLIDER "+Hierarchy(c.transform)+" bounds="+c.bounds);
                foreach(var r in root.GetComponentsInChildren<Renderer>())
                    if(r.bounds.Intersects(new Bounds(new Vector3(6,2,80),new Vector3(5,4,16))))
                        Debug.Log("ENDING_VISUAL "+Hierarchy(r.transform)+" bounds="+r.bounds);
                var door=root.GetComponentInChildren<Door>();
                Debug.Log("ENDING_DOOR root="+door.transform.position+" pivot="+((Transform)new SerializedObject(door).FindProperty("_pivot").objectReferenceValue).position);
                foreach(var c in door.GetComponentsInChildren<Collider>())Debug.Log("ENDING_DOOR_LEAF "+Hierarchy(c.transform)+" bounds="+c.bounds);
                Debug.Log("ENDING_EVENTS "+root.GetComponentsInChildren<EventSystem>(true).Length);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static GameObject At(string name,Transform parent,Vector3 position)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go;}
        static Renderer Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.GetComponent<Renderer>();
        }
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        static TMP_Text Text(string name,Transform parent,Vector2 min,Vector2 max,string value,int size)
        {
            var r=Rect(name,parent,min,max);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset");
            t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.91f,.89f,.8f);t.raycastTarget=false;t.richText=false;return t;
        }
        static InputActionReference ActionReference(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Input/TikalInputActions.inputactions")
            .OfType<InputActionReference>().First(r=>r.action!=null&&r.action.actionMap.name=="UI"&&r.action.name==name);
        public static void Run()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);var root=scene.GetRootGameObjects().Single().transform;
                if(root.GetComponentInChildren<LevelOutcomeController>(true)!=null)throw new Exception("Ending already installed.");
                var zone=root.Find("Environment/Zone05_Revelation");
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/RevelationStone.mat");
                var mark=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/RevelationPattern.mat");
                var host=At("Level ending",root,Vector3.zero);
                var outcome=host.AddComponent<LevelOutcomeController>();
                var reader=root.GetComponentInChildren<PlayerInputReader>();
                var player=reader.GetComponent<PlayerController>();var camera=reader.GetComponentInChildren<Camera>();
                var door=root.GetComponentInChildren<Door>();
                var target=At("SecretCommandTarget",root.Find("Environment/Entrance"),new Vector3(0,1.6f,.05f));
                var volume=At("SecretCommandZone",target.transform,new Vector3(0,1.5f,0)).AddComponent<BoxCollider>();
                volume.size=new Vector3(3.4f,3,5.8f);volume.isTrigger=true;
                var seal=Shape("Provisional entrance seal",target.transform,new Vector3(0,4.85f,-.36f),new Vector3(.35f,.3f,.055f),mark,false);
                var receiver=host.AddComponent<SecretEntranceCommandReceiver>();
                EchoSceneAuthoring.Set(receiver,"_inputReader",reader);EchoSceneAuthoring.Set(receiver,"_player",reader.transform);
                EchoSceneAuthoring.Set(receiver,"_camera",camera);EchoSceneAuthoring.Set(receiver,"_target",target.transform);
                EchoSceneAuthoring.Set(receiver,"_zone",volume);EchoSceneAuthoring.Set(receiver,"_outcome",outcome);
                var pivot=(Transform)new SerializedObject(door).FindProperty("_pivot").objectReferenceValue;
                EchoSceneAuthoring.Set(receiver,"_movingLeaf",pivot.GetComponentsInChildren<Collider>().Cast<UnityEngine.Object>().ToArray());

                // Split only the rear closure around a 3.2m portal; preserve all original objects.
                var boundary=zone.Find("SolidBoundaries").GetComponentsInChildren<BoxCollider>()
                    .Single(c=>Mathf.Abs(c.bounds.center.z-78)<.1f&&c.bounds.size.x>20);
                boundary.transform.position=new Vector3(-3.3f,3.6f,78);boundary.transform.localScale=new Vector3(15.4f,6,.35f);
                var east=Shape("SolidBoundary",boundary.transform.parent,new Vector3(9.3f,3.6f,78),new Vector3(3.4f,6,.35f),material);
                east.enabled=false;
                var talud=root.Find("Environment/NaturalBoundaries/Talud_73");
                var eastTalud=UnityEngine.Object.Instantiate(talud.gameObject,talud.parent).transform;eastTalud.name="Revelation eastern rock shoulder";
                Vector3 originalScale=talud.localScale;
                talud.position+=Vector3.left*3.3f;talud.localScale=new Vector3(originalScale.x*(15.4f/22),originalScale.y,originalScale.z);
                eastTalud.position+=Vector3.right*9.3f;eastTalud.localScale=new Vector3(originalScale.x*(3.4f/22),originalScale.y,originalScale.z);
                var greenery=root.Find("Environment/Vegetation_Blockout").GetComponentsInChildren<Renderer>()
                    .Single(r=>r.bounds.center.z>77 && r.bounds.center.z<79 && r.bounds.size.x>15);
                var scale=greenery.transform.localScale;
                greenery.transform.position+=Vector3.left*1.85f;greenery.transform.localScale=new Vector3(scale.x*(11.96f/15.66f),scale.y,scale.z);

                var gate=Shape("Revelation side seal",zone,new Vector3(6,2.3f,78),new Vector3(3.2f,3.4f,.4f),material).gameObject;
                var path=At("FalsePath",zone,new Vector3(6,.6f,78));
                Shape("Corridor floor",path.transform,new Vector3(6,.4f,88),new Vector3(3.2f,.4f,20),root.Find("Environment/Terrain").GetComponentInChildren<Renderer>().sharedMaterial);
                Shape("Western corridor rock",path.transform,new Vector3(4.25f,2.3f,88),new Vector3(.35f,3.4f,20),material);
                Shape("Eastern corridor rock",path.transform,new Vector3(7.75f,2.3f,88),new Vector3(.35f,3.4f,20),material);
                Shape("Natural corridor closure",path.transform,new Vector3(6,2.3f,98),new Vector3(3.2f,3.4f,.4f),material);
                var warning=At("FinalWarningStele_Root",path.transform,new Vector3(4.9f,.6f,83.5f));
                var visual=At("VisualProxy",warning.transform,warning.transform.position);
                Shape("FinalWarningStele_Blockout",visual.transform,new Vector3(4.9f,1.5f,83.5f),new Vector3(.7f,1.8f,.3f),material,false);
                var collision=At("Collider",warning.transform,warning.transform.position).AddComponent<BoxCollider>();
                collision.center=new Vector3(0,.9f,0);collision.size=new Vector3(.7f,1.8f,.3f);
                var readable=warning.AddComponent<ReadableInscription>();InscriptionSceneAuthoring.Set(readable,"_title","Inscripción");
                InscriptionSceneAuthoring.Set(readable,"_text","Si continúas, termina tu recorrido.");
                var closing=Shape("Closing passage",path.transform,new Vector3(6,2.3f,88.5f),new Vector3(3.2f,3.4f,.4f),material).gameObject;
                closing.SetActive(false);
                var noReturn=At("PointOfNoReturn",path.transform,new Vector3(6,1.8f,92));
                var trigger=noReturn.AddComponent<BoxCollider>();trigger.size=new Vector3(3.2f,2.4f,1.2f);trigger.isTrigger=true;
                var trap=noReturn.AddComponent<PointOfNoReturnTrigger>();
                EchoSceneAuthoring.Set(trap,"_player",reader.GetComponent<CharacterController>());
                EchoSceneAuthoring.Set(trap,"_outcome",outcome);EchoSceneAuthoring.Set(trap,"_closingPassage",closing);
                var reveal=host.AddComponent<FalsePathReveal>();
                EchoSceneAuthoring.Set(reveal,"_puzzle",root.GetComponentInChildren<RevelationPuzzle>());
                EchoSceneAuthoring.Set(reveal,"_gate",gate);EchoSceneAuthoring.Set(reveal,"_path",path);path.SetActive(false);

                var canvas=root.GetComponentInChildren<Canvas>(true);
                if(canvas.GetComponent<GraphicRaycaster>()==null)canvas.gameObject.AddComponent<GraphicRaycaster>();
                var events=At("Result EventSystem",host.transform,Vector3.zero).AddComponent<EventSystem>();
                var module=events.gameObject.AddComponent<InputSystemUIInputModule>();
                module.actionsAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/TikalInputActions.inputactions");
                module.move=ActionReference("Navigate");module.submit=ActionReference("Submit");module.cancel=ActionReference("Cancel");
                module.point=ActionReference("Point");module.leftClick=ActionReference("Click");module.scrollWheel=ActionReference("ScrollWheel");
                module.rightClick=null;module.middleClick=null;module.trackedDevicePosition=null;module.trackedDeviceOrientation=null;
                var ui=Rect("Level result",canvas.transform,Vector2.zero,Vector2.one);
                var presenter=ui.gameObject.AddComponent<LevelResultPresenter>();
                var panel=Rect("Result panel",ui,Vector2.zero,Vector2.one);
                var background=panel.gameObject.AddComponent<Image>();background.color=new Color(.012f,.018f,.014f,.97f);
                var fade=panel.gameObject.AddComponent<CanvasGroup>();
                var title=Text("Result title",panel,new Vector2(.1f,.58f),new Vector2(.9f,.72f),"",40);
                var subtitle=Text("Result subtitle",panel,new Vector2(.2f,.49f),new Vector2(.8f,.56f),"",26);
                var buttonRect=Rect("Retry",panel,new Vector2(.4f,.32f),new Vector2(.6f,.41f));
                var image=buttonRect.gameObject.AddComponent<Image>();image.color=new Color(.18f,.2f,.15f);
                var retry=buttonRect.gameObject.AddComponent<Button>();retry.targetGraphic=image;
                Text("Retry label",buttonRect,Vector2.zero,Vector2.one,"REINTENTAR",23);
                EchoSceneAuthoring.Set(presenter,"_outcome",outcome);EchoSceneAuthoring.Set(presenter,"_panel",panel.gameObject);
                EchoSceneAuthoring.Set(presenter,"_fade",fade);EchoSceneAuthoring.Set(presenter,"_title",title);
                EchoSceneAuthoring.Set(presenter,"_subtitle",subtitle);EchoSceneAuthoring.Set(presenter,"_retry",retry);EchoSceneAuthoring.Set(presenter,"_events",events);
                panel.gameObject.SetActive(false);
                EchoSceneAuthoring.Set(outcome,"_player",player);EchoSceneAuthoring.Set(outcome,"_journal",root.GetComponentInChildren<ClueJournalPresenter>(true));
                EchoSceneAuthoring.Set(outcome,"_prompt",root.GetComponentInChildren<InteractionPromptPresenter>(true));
                EchoSceneAuthoring.Set(outcome,"_inscription",root.GetComponentInChildren<InscriptionPresenter>(true));
                EchoSceneAuthoring.Set(outcome,"_discovery",root.GetComponentInChildren<ClueDiscoveryPresenter>(true));
                EchoSceneAuthoring.Set(outcome,"_victorySeal",seal);
                EchoSceneAuthoring.Set(root.GetComponentInChildren<InscriptionPresenter>(true),"_inscriptions",root.GetComponentsInChildren<ReadableInscription>(true).Cast<UnityEngine.Object>().ToArray());
                if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
                    EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_ENDING_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}