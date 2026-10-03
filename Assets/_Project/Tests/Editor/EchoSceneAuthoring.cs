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
    public static class EchoSceneAuthoring
    {
        internal static void Set(UnityEngine.Object o,string field,object value)
        {
            var so=new SerializedObject(o);var p=so.FindProperty(field);if(p==null)throw new Exception("Unknown property "+field);
            if(value is string s)p.stringValue=s;
            else if(value is int i)p.intValue=i;
            else if(value is float f)p.floatValue=f;
            else if(value is bool b)p.boolValue=b;
            else if(value is int[] a){p.arraySize=a.Length;for(int n=0;n<a.Length;n++)p.GetArrayElementAtIndex(n).intValue=a[n];}
            else if(value is UnityEngine.Object[] r){p.arraySize=r.Length;for(int n=0;n<r.Length;n++)p.GetArrayElementAtIndex(n).objectReferenceValue=r[n];}
            else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=path.Substring(0,path.LastIndexOf('/'));Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static GameObject At(string name,Transform parent,Vector3 pos){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=pos;return g;}
        static Renderer Shape(string name,Transform parent,PrimitiveType primitive,Vector3 pos,Vector3 scale,Material material,bool collider=true)
        {
            var g=GameObject.CreatePrimitive(primitive);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;var r=g.GetComponent<Renderer>();r.sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return r;
        }
        static AudioSource Audio(GameObject g,float spatial)
        {
            var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=false;a.volume=.75f;a.spatialBlend=spatial;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=2;a.maxDistance=12;a.dopplerLevel=0;return a;
        }
        static AudioClip Tone(string name,double frequency)
        {
            const int rate=44100,count=19845;string path="Assets/_Project/Audio/SFX/Echo/"+name+".wav";
            if(File.Exists(path))throw new Exception("Tone already exists: "+path);
            using(var w=new BinaryWriter(File.Create(path)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
                for(int n=0;n<count;n++){double t=n/(double)rate;double envelope=Math.Min(1,t/.025)*Math.Min(1,(.45-t)/.055);w.Write((short)(Math.Sin(2*Math.PI*frequency*t)*envelope*.24*short.MaxValue));}
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.PCM;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        public static void Preview()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var camera=new GameObject("Temporary echo preview").AddComponent<Camera>();camera.transform.position=new Vector3(30,2.5f,41.5f);camera.transform.LookAt(new Vector3(33.5f,1.3f,43.3f));camera.fieldOfView=78;
                var target=new RenderTexture(1440,1000,24);target.Create();
                for(int i=0;i<2;i++)UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target});
                RenderTexture.active=target;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"TikalEchoOverview.png"),image.EncodeToPNG());
                RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        public static void AdjustLayout()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                all.OfType<EchoSequencePuzzle>().Single().transform.position=new Vector3(33,.35f,43);
                all.OfType<EchoSource>().Single().transform.position=new Vector3(33,.35f,43);
                var stones=all.OfType<EchoStone>().OrderBy(s=>s.Note).ToArray();Vector3[] positions={new Vector3(31.5f,.35f,44.7f),new Vector3(35,.35f,44.7f),new Vector3(35,.35f,42.3f)};for(int i=0;i<3;i++)stones[i].transform.position=positions[i];
                EditorSceneManager.SaveScene(scene);if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");var root=scene.GetRootGameObjects().Single().transform;var zone=root.Find("Environment/Zone03_EchoChamber");
                if(zone.GetComponentInChildren<EchoSequencePuzzle>(true)!=null)throw new Exception("Echo already installed.");
                Folder("Assets/_Project/Audio/SFX/Echo");var tones=new[]{Tone("Echo_A_Low",220),Tone("Echo_B_Middle",440),Tone("Echo_C_High",660)};
                string materialPath="Assets/_Project/Art/Materials/Level01/EchoStone.mat";if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)!=null)throw new Exception("Echo material already exists.");
                var stoneMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralStone.mat"));stoneMaterial.name="EchoStone";stoneMaterial.EnableKeyword("_EMISSION");stoneMaterial.SetColor("_EmissionColor",Color.black);AssetDatabase.CreateAsset(stoneMaterial,materialPath);
                var marksMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/GuardiansAlignment.mat");
                var host=At("Echo sequence mechanism",zone.Find("PuzzleAnchor"),new Vector3(33,.35f,43));var puzzle=host.AddComponent<EchoSequencePuzzle>();
                var sourceObject=At("EchoSource",zone.Find("AudioAnchor"),new Vector3(33,.35f,43));var source=sourceObject.AddComponent<EchoSource>();var audio=sourceObject.GetComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=.2f;audio.volume=.75f;audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=2;audio.maxDistance=12;audio.dopplerLevel=0;
                Set(source,"_puzzle",puzzle);Set(source,"_audio",audio);Set(source,"_tones",tones.Cast<UnityEngine.Object>().ToArray());
                Shape("Central listening stone",source.transform,PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(1.2f,.65f,1.2f),stoneMaterial);
                Shape("Listening crest",source.transform,PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(.6f,.25f,.6f),stoneMaterial);
                var progress=new Renderer[3];for(int i=0;i<3;i++)progress[i]=Shape("Echo progress mark "+i,source.transform,PrimitiveType.Cube,new Vector3((i-1)*.3f,1.31f,-.3f),new Vector3(.12f,.025f,.25f),marksMaterial,false);
                var stones=new EchoStone[3];Vector3[] positions={new Vector3(31.5f,.35f,44.7f),new Vector3(35,.35f,44.7f),new Vector3(35,.35f,42.3f)};
                for(int i=0;i<3;i++)
                {
                    var g=At("SoundStone_"+(char)('A'+i),host.transform,positions[i]);var a=Audio(g,.35f);var stone=g.AddComponent<EchoStone>();stones[i]=stone;
                    Shape("Stone plinth",g.transform,PrimitiveType.Cylinder,new Vector3(0,.2f,0),new Vector3(.9f,.2f,.9f),stoneMaterial);
                    var visual=Shape(i==0?"Low square slab":i==1?"Round resonator":"Tall fork resonator",g.transform,i==0?PrimitiveType.Cube:i==1?PrimitiveType.Sphere:PrimitiveType.Cylinder,new Vector3(0,i==2?1: .8f,0),i==0?new Vector3(.95f,.7f,.65f):i==1?new Vector3(.8f,.8f,.8f):new Vector3(.48f,.6f,.48f),stoneMaterial);
                    if(i==2)for(int n=0;n<2;n++)Shape("Fork prong "+n,g.transform,PrimitiveType.Cube,new Vector3((n==0?-.22f:.22f),1.55f,0),new Vector3(.13f,.65f,.25f),stoneMaterial);
                    Set(stone,"_puzzle",puzzle);Set(stone,"_note",i);Set(stone,"_audio",a);Set(stone,"_tone",tones[i]);Set(stone,"_visual",visual);
                }
                Set(puzzle,"_definition",AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.echo_chamber.asset"));Set(puzzle,"_initialState",1);Set(puzzle,"_source",source);Set(puzzle,"_stones",stones.Cast<UnityEngine.Object>().ToArray());Set(puzzle,"_targetSequence",new[]{1,0,2});
                var feedback=host.AddComponent<EchoProgressFeedback>();Set(feedback,"_puzzle",puzzle);Set(feedback,"_marks",progress.Cast<UnityEngine.Object>().ToArray());
                var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.echo_chamber.asset");Set(clue,"_id","level01.clue03");Set(clue,"_title","El primer contacto");Set(clue,"_description","Lo primero que tocaste aún guarda algo.");
                var rewardObject=At("Echo clue reward",zone.Find("ClueAnchor"),zone.Find("ClueAnchor").position);var reward=rewardObject.AddComponent<PuzzleClueReward>();Set(reward,"_puzzle",puzzle);Set(reward,"_tracker",root.GetComponentInChildren<ClueTracker>());Set(reward,"_clue",clue);
                var readable=InscriptionSceneAuthoring.Build(zone,"Echo guidance inscription",new Vector3(31,.35f,37),"Escucha. Después, devuelve el eco.");
                var presenter=root.GetComponentInChildren<InscriptionPresenter>(true);var current=root.GetComponentsInChildren<ReadableInscription>(true);Set(presenter,"_inscriptions",current.Cast<UnityEngine.Object>().ToArray());
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Debug.Log("TIKAL_ECHO_AUTHORED sequence=B,A,C tones=220,440,660 duration=.45 gap=.3");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
