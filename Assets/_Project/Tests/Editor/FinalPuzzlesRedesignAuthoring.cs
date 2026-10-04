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
namespace Tikal.Tests.Editor
{
    public static class FinalPuzzlesRedesignAuthoring
    {
        const string ScenePath="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        static GameObject At(string name,Transform parent,Vector3 position)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=position;return g;}
        static Renderer Shape(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            return g.GetComponent<Renderer>();
        }
        public static void RefineAltarAccess()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var point=scene.GetRootGameObjects().Single().GetComponentInChildren<ObservationPoint>();
                if(point.transform.Find("Eastern approach step")==null)
                    Shape("Eastern approach step",point.transform,point.transform.position+new Vector3(1.6f,-.16f,0),new Vector3(1,.28f,1.2f),
                        AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/GlyphStone.mat"),true);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_ALTAR_ACCESS_REFINED without terrain or player changes");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Inspect()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath);var root=scene.GetRootGameObjects().Single();
            Physics.SyncTransforms();
            var point=root.GetComponentInChildren<ObservationPoint>(true);
            Debug.Log("TIKAL_ALTAR_POSITION "+point.transform.position);
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))
                if(collider.enabled&&!collider.isTrigger&&collider.bounds.Contains(new Vector3(-3.75f,1.9f,39)))
                    Debug.Log("TIKAL_ALTAR_OBSTACLE "+collider.name+" "+collider.bounds);
            foreach(var collider in point.GetComponentsInChildren<Collider>(true))Debug.Log("TIKAL_ALTAR_COLLIDER "+collider.name+" "+collider.bounds);
            EditorApplication.Exit(0);
        }
        public static void Run()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);var root=scene.GetRootGameObjects().Single().transform;
                if(root.GetComponentInChildren<ObservationAlignmentPuzzle>(true)!=null)throw new Exception("Redesign already installed.");
                var zone4=root.Find("Environment/Zone04_GlyphGallery");var zone5=root.Find("Environment/Zone05_Revelation");
                var reader=root.GetComponentInChildren<PlayerInputReader>();var camera=reader.GetComponentInChildren<Camera>();
                var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/GlyphStone.mat");
                var marks=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/GlyphMarks.mat");
                var old4=zone4.GetComponentInChildren<GlyphSequencePuzzle>();var old5=zone5.GetComponentInChildren<RevelationPuzzle>();
                var host=At("Observation alignment mechanism",zone4.Find("PuzzleAnchor"),zone4.Find("PuzzleAnchor").position);
                var observation=host.AddComponent<ObservationAlignmentPuzzle>();
                var def4=AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.glyph_gallery.asset");
                EchoSceneAuthoring.Set(def4,"_displayName","Alineación visual");
                EchoSceneAuthoring.Set(observation,"_definition",def4);EchoSceneAuthoring.Set(observation,"_initialState",1);
                var terrain=root.Find("Environment/Terrain").GetComponentsInChildren<MeshCollider>();
                if(!terrain.Any(c=>c.Raycast(new Ray(new Vector3(-5,20,39),Vector3.down),out _,30)))throw new Exception("Altar has no terrain support");
                RaycastHit ground=default;foreach(var c in terrain)if(c.Raycast(new Ray(new Vector3(-5,20,39),Vector3.down),out ground,30))break;
                var altar=At("Observation altar",host.transform,new Vector3(-5,ground.point.y,39));
                Shape("Low step",altar.transform,altar.transform.position+Vector3.up*.07f,new Vector3(2.4f,.14f,2.4f),stone,true);
                Shape("Observation platform",altar.transform,altar.transform.position+Vector3.up*.21f,new Vector3(2,.14f,2),stone,true);
                Shape("Eastern approach step",altar.transform,altar.transform.position+new Vector3(1.6f,-.16f,0),new Vector3(1,.28f,1.2f),stone,true);
                var point=altar.AddComponent<ObservationPoint>();
                var volume=At("ObservationZone",altar.transform,altar.transform.position+Vector3.up*1.28f).AddComponent<BoxCollider>();
                volume.size=new Vector3(1.8f,.35f,1.8f);volume.isTrigger=true;
                EchoSceneAuthoring.Set(point,"_player",reader.transform);EchoSceneAuthoring.Set(point,"_camera",camera);
                EchoSceneAuthoring.Set(point,"_zone",volume);EchoSceneAuthoring.Set(point,"_inputReader",reader);
                Vector3 eye=altar.transform.position+Vector3.up*(.28f+1.62f);
                Vector3 direction=new Vector3(0,.23f,-1).normalized;
                var target=At("ObservationAlignmentTarget",host.transform,eye+direction*7);
                var art=At("Perspective pattern VisualProxy",host.transform,eye);
                Vector3 right=Vector3.right,up=Vector3.Cross(direction,right).normalized;
                Vector2[] a={new Vector2(-.7f,0),new Vector2(0,.45f),new Vector2(.7f,0),new Vector2(0,-.45f),new Vector2(0,-.17f)};
                Vector2[] b={new Vector2(0,.45f),new Vector2(.7f,0),new Vector2(0,-.45f),new Vector2(-.7f,0),new Vector2(0,.17f)};
                float[] depths={4.4f,5.5f,6.8f,8,6.1f};var fragments=new Renderer[5];
                for(int i=0;i<5;i++)
                {
                    Vector3 start=eye+direction*depths[i]+(right*a[i].x+up*a[i].y)*(depths[i]/6);
                    Vector3 end=eye+direction*depths[i]+(right*b[i].x+up*b[i].y)*(depths[i]/6);
                    fragments[i]=Shape("Perspective fragment "+i,art.transform,(start+end)*.5f,new Vector3(.07f*depths[i]/6,(end-start).magnitude,.045f),marks);
                    fragments[i].transform.rotation=Quaternion.LookRotation(direction,(end-start).normalized);
                }
                EchoSceneAuthoring.Set(observation,"_point",point);EchoSceneAuthoring.Set(observation,"_target",target.transform);
                EchoSceneAuthoring.Set(observation,"_fragments",fragments.Cast<UnityEngine.Object>().ToArray());
                var clue4=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.glyph_gallery.asset");
                EchoSceneAuthoring.Set(clue4,"_title","La mirada");EchoSceneAuthoring.Set(clue4,"_description","No todo se revela desde cualquier lugar.");
                EchoSceneAuthoring.Set(clue4,"_semanticContent",0);
                var reward4=zone4.GetComponentInChildren<PuzzleClueReward>();EchoSceneAuthoring.Set(reward4,"_puzzle",observation);
                var guidance4=zone4.GetComponentInChildren<ReadableInscription>();EchoSceneAuthoring.Set(guidance4,"_text","Debes aprender a observar.");

                var content=At("Secret key revelation content",zone5.Find("RevelationAnchor"),new Vector3(0,.6f,69));
                var discoveryRoot=At("KeyDiscoveryStele_Root",content.transform,new Vector3(0,.6f,71));
                var discovery=discoveryRoot.AddComponent<SecretKeyDiscoveryPuzzle>();
                var def5=AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.revelation.asset");
                EchoSceneAuthoring.Set(def5,"_displayName","Descubrimiento de la llave");EchoSceneAuthoring.Set(discovery,"_definition",def5);EchoSceneAuthoring.Set(discovery,"_initialState",0);
                var visual=At("DiscoveryStele_VisualProxy",discoveryRoot.transform,discoveryRoot.transform.position);
                Shape("Discovery slab",visual.transform,new Vector3(0,1.5f,71),new Vector3(.9f,1.8f,.35f),stone);
                var collider=At("Interaction collider",discoveryRoot.transform,new Vector3(0,1.5f,71)).AddComponent<BoxCollider>();collider.size=new Vector3(.9f,1.8f,.35f);
                var key=At("KeySymbol_VisualProxy",content.transform,new Vector3(0,2.15f,70.78f));
                // Neutral fictional key silhouette; no historical attribution.
                Shape("Key bow left",key.transform,key.transform.position+new Vector3(-.12f,.1f,0),new Vector3(.065f,.3f,.04f),marks);
                Shape("Key bow right",key.transform,key.transform.position+new Vector3(.12f,.1f,0),new Vector3(.065f,.3f,.04f),marks);
                Shape("Key bow top",key.transform,key.transform.position+new Vector3(0,.25f,0),new Vector3(.3f,.065f,.04f),marks);
                Shape("Key bow bottom",key.transform,key.transform.position+new Vector3(0,-.05f,0),new Vector3(.3f,.065f,.04f),marks);
                Shape("Key shaft",key.transform,key.transform.position+new Vector3(0,-.32f,0),new Vector3(.065f,.5f,.04f),marks);
                Shape("Key tooth",key.transform,key.transform.position+new Vector3(.09f,-.52f,0),new Vector3(.18f,.065f,.04f),marks);
                var gate=zone5.gameObject.AddComponent<Zone05RevelationGate>();
                EchoSceneAuthoring.Set(gate,"_prerequisite",observation);EchoSceneAuthoring.Set(gate,"_discovery",discovery);EchoSceneAuthoring.Set(gate,"_content",content);
                content.SetActive(false);
                var clue5=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.revelation.asset");
                EchoSceneAuthoring.Set(clue5,"_title","La llave");EchoSceneAuthoring.Set(clue5,"_description","Ya conoces la llave.");EchoSceneAuthoring.Set(clue5,"_semanticContent",1);
                EchoSceneAuthoring.Set(zone5.GetComponentInChildren<PuzzleClueReward>(true),"_puzzle",discovery);
                EchoSceneAuthoring.Set(zone5.GetComponentInChildren<RevelationMonument>(true),"_puzzle",discovery);
                EchoSceneAuthoring.Set(root.GetComponentInChildren<FalsePathReveal>(true),"_puzzle",discovery);
                var noReturn=root.GetComponentInChildren<PointOfNoReturnTrigger>(true);
                EchoSceneAuthoring.Set(noReturn,"_requiredPuzzle",discovery);noReturn.transform.position=new Vector3(6,1.8f,91.5f);
                // Keep legacy visual objects and their colliders. Remove only superseded playable components.
                foreach(var component in zone4.GetComponentsInChildren<GlyphStone>(true))UnityEngine.Object.DestroyImmediate(component);
                foreach(var component in zone4.GetComponentsInChildren<GlyphProgressFeedback>(true))UnityEngine.Object.DestroyImmediate(component);
                UnityEngine.Object.DestroyImmediate(old4);
                foreach(var component in zone5.GetComponentsInChildren<RevelationStele>(true))UnityEngine.Object.DestroyImmediate(component);
                UnityEngine.Object.DestroyImmediate(old5);
                foreach(var help in zone5.GetComponentsInChildren<ReadableInscription>(true))help.enabled=false;
                var path=(GameObject)new SerializedObject(root.GetComponentInChildren<FalsePathReveal>(true)).FindProperty("_path").objectReferenceValue;
                var warning=path.GetComponentsInChildren<ReadableInscription>(true).Single();
                var anchor=At("FinalWarningSteleAnchor",warning.transform.parent,warning.transform.position);
                warning.transform.SetParent(anchor.transform,true);
                warning.transform.Find("VisualProxy").name="FinalWarningStele_Proxy";
                UnityEngine.Object.DestroyImmediate(warning);
                var inscriptions=root.GetComponentsInChildren<ReadableInscription>(true).Where(r=>r.enabled).Cast<UnityEngine.Object>().ToArray();
                EchoSceneAuthoring.Set(root.GetComponentInChildren<InscriptionPresenter>(true),"_inscriptions",inscriptions);
                var presentation=root.GetComponentInChildren<ClueDiscoveryPresenter>(true);
                EchoSceneAuthoring.Set(presentation,"_inputReader",reader);EchoSceneAuthoring.Set(presentation,"_devices",reader.GetComponent<InputDeviceManager>());
                var serialized=new SerializedObject(presentation);
                var panel=((GameObject)serialized.FindProperty("_panel").objectReferenceValue).GetComponent<RectTransform>();panel.sizeDelta=new Vector2(660,180);
                var title=((TMP_Text)serialized.FindProperty("_title").objectReferenceValue);title.rectTransform.sizeDelta=new Vector2(620,66);title.rectTransform.anchoredPosition=new Vector2(0,35);title.fontSize=23;
                var description=(TMP_Text)serialized.FindProperty("_description").objectReferenceValue;description.rectTransform.sizeDelta=new Vector2(620,65);description.rectTransform.anchoredPosition=new Vector2(0,-35);description.fontSize=24;
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_FINAL_PUZZLES_REDESIGN_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}