using System;
using System.IO;
using System.Linq;
using Tikal.Interaction;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Level01;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class RevelationSceneAuthoring
    {
        static GameObject At(string name, Transform parent, Vector3 position)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position; return go; }
        static Renderer Shape(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = false, float angle = 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = size; go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.GetComponent<Renderer>();
        }
        static Renderer[] Pattern(Transform parent, string id, float scale, Material material)
        {
            var root = new GameObject("Pattern elements"); root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0, 1.35f, -.245f);
            root.transform.localScale = Vector3.one * scale;
            if (id == "Cycle")
            {
                for (int i = 1; i <= 11; i++)
                {
                    float angle = i * 30f, rad = angle * Mathf.Deg2Rad;
                    Shape("Broken cycle segment " + i, root.transform, new Vector3(Mathf.Sin(rad)*.45f, Mathf.Cos(rad)*.45f, 0),
                        new Vector3(.22f,.095f,.055f), material, false, -angle);
                }
            }
            else if (id == "Path")
            {
                Shape("Angular left",root.transform,new Vector3(-.22f,.15f,0),new Vector3(.12f,.65f,.055f),material,false,-35);
                Shape("Angular right",root.transform,new Vector3(.15f,.15f,0),new Vector3(.12f,.65f,.055f),material,false,35);
                Shape("Angular lower",root.transform,new Vector3(.15f,-.32f,0),new Vector3(.12f,.42f,.055f),material);
            }
            else
                for(int i=0;i<8;i++)
                {
                    float angle=i*45f,rad=angle*Mathf.Deg2Rad;
                    Shape("Radial spoke "+i,root.transform,new Vector3(Mathf.Sin(rad)*.22f,Mathf.Cos(rad)*.22f,0),
                        new Vector3(.065f,.4f,.055f),material,false,-angle);
                }
            return root.GetComponentsInChildren<Renderer>();
        }
        public static void Run()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var root=scene.GetRootGameObjects().Single().transform;
                var zone=root.Find("Environment/Zone05_Revelation");
                if(zone.GetComponentInChildren<RevelationPuzzle>(true)!=null)throw new Exception("Revelation already installed.");
                const string stonePath="Assets/_Project/Art/Materials/Level01/RevelationStone.mat",markPath="Assets/_Project/Art/Materials/Level01/RevelationPattern.mat";
                if(File.Exists(stonePath)||File.Exists(markPath))throw new Exception("Revelation materials already exist.");
                var stoneMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/GlyphStone.mat"));
                stoneMaterial.name="RevelationStone";stoneMaterial.SetColor("_BaseColor",new Color(.18f,.2f,.21f));AssetDatabase.CreateAsset(stoneMaterial,stonePath);
                var patternMaterial=new Material(stoneMaterial);patternMaterial.name="RevelationPattern";patternMaterial.SetColor("_BaseColor",new Color(.76f,.71f,.54f));AssetDatabase.CreateAsset(patternMaterial,markPath);
                var host=At("Revelation matching mechanism",zone.Find("PuzzleAnchor"),zone.Find("PuzzleAnchor").position);
                var puzzle=host.AddComponent<RevelationPuzzle>();
                EchoSceneAuthoring.Set(puzzle,"_definition",AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.revelation.asset"));
                EchoSceneAuthoring.Set(puzzle,"_initialState",1);
                string[] ids={"Cycle","Path","Cycle","Star"};
                var steles=new RevelationStele[4];
                Physics.SyncTransforms();
                for(int i=0;i<4;i++)
                {
                    float x=-4.5f+i*3f,z=70.8f+(i%2)*.2f;
                    var terrain=root.Find("Environment/Terrain").GetComponentsInChildren<MeshCollider>();
                    RaycastHit ground=default;
                    if(!terrain.Any(c=>c.Raycast(new Ray(new Vector3(x,20,z),Vector3.down),out ground,30)))throw new Exception("Stele lacks terrain support.");
                    var go=At("RevelationStele_"+(char)('A'+i),host.transform,new Vector3(x,ground.point.y,z));
                    var stele=go.AddComponent<RevelationStele>();steles[i]=stele;
                    Shape("Ceremonial plinth",go.transform,new Vector3(0,.12f,0),new Vector3(1.4f,.24f,.85f),stoneMaterial,true);
                    Shape("Upright stele",go.transform,new Vector3(0,1.29f,0),new Vector3(1.25f,2.1f,.42f),stoneMaterial,true);
                    var symbols=Pattern(go.transform,ids[i],i==2?.86f:1,patternMaterial);
                    if(i==2)Shape("Secondary wear notch",go.transform,new Vector3(.48f,.65f,-.22f),new Vector3(.08f,.16f,.035f),patternMaterial,false,12);
                    EchoSceneAuthoring.Set(stele,"_puzzle",puzzle);EchoSceneAuthoring.Set(stele,"_patternId",ids[i]);
                    EchoSceneAuthoring.Set(stele,"_symbols",symbols.Cast<UnityEngine.Object>().ToArray());
                    Debug.Log("REVELATION_AUTHORED "+go.name+" pattern="+ids[i]+" position="+go.transform.position);
                }
                EchoSceneAuthoring.Set(puzzle,"_steles",steles.Cast<UnityEngine.Object>().ToArray());
                var central=At("Sealed revelation panel",zone.Find("RevelationAnchor"),new Vector3(0,2.25f,75.35f));
                var interior=Shape("Warm interior",central.transform,new Vector3(0,0,.02f),new Vector3(1.15f,1,.1f),patternMaterial);
                var panel=Shape("Movable stone seal",central.transform,new Vector3(0,0,-.1f),new Vector3(1.45f,1.2f,.18f),stoneMaterial);
                var monument=central.AddComponent<RevelationMonument>();
                EchoSceneAuthoring.Set(monument,"_puzzle",puzzle);EchoSceneAuthoring.Set(monument,"_panel",panel.transform);EchoSceneAuthoring.Set(monument,"_interior",interior);
                var definition=AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.revelation.asset");
                EchoSceneAuthoring.Set(definition,"_displayName","Cámara de la revelación");
                var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.revelation.asset");
                EchoSceneAuthoring.Set(clue,"_id","level01.clue05");EchoSceneAuthoring.Set(clue,"_title","El ciclo");EchoSceneAuthoring.Set(clue,"_description","El inicio es el final.");
                var reward=At("Revelation clue reward",zone.Find("ClueAnchor"),zone.Find("ClueAnchor").position).AddComponent<PuzzleClueReward>();
                EchoSceneAuthoring.Set(reward,"_puzzle",puzzle);EchoSceneAuthoring.Set(reward,"_tracker",root.GetComponentInChildren<ClueTracker>());EchoSceneAuthoring.Set(reward,"_clue",clue);
                InscriptionSceneAuthoring.Build(zone,"Revelation guidance inscription",new Vector3(6,1,67.4f),"Lo que aparece al principio vuelve al final.");
                EchoSceneAuthoring.Set(root.GetComponentInChildren<InscriptionPresenter>(true),"_inscriptions",root.GetComponentsInChildren<ReadableInscription>(true).Cast<UnityEngine.Object>().ToArray());
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_REVELATION_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        public static void Preview()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var camera=new GameObject("Temporary revelation preview").AddComponent<Camera>();
                camera.transform.position=new Vector3(0,4.8f,64.8f);camera.transform.LookAt(new Vector3(0,2.5f,73));camera.fieldOfView=70;
                var target=new RenderTexture(1600,1000,24);target.Create();
                for(int i=0;i<2;i++)UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"TikalRevelationOverview.png"),image.EncodeToPNG());
                RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                Debug.Log("TIKAL_REVELATION_PREVIEW_RENDERED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}