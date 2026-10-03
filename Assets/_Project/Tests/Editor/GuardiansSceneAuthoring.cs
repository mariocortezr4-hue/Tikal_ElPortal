using System;
using System.Linq;
using Tikal.Puzzles;
using Tikal.Puzzles.Level01;
using Tikal.Puzzles.Clues;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Tikal.Tests.Editor
{
    public static class GuardiansSceneAuthoring
    {
        public static void RefreshComposition()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");Physics.SyncTransforms();
            var puzzle=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PathGuardiansPuzzle>(true)).Single();
            puzzle.transform.Find("Guardian C").position=new Vector3(31,Ground(31,16)+.03f,16);
            puzzle.transform.Find("DestinationMarker C").position=new Vector3(31,Ground(31,4)+.03f,4);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralCarving.mat");
            foreach(var guardian in puzzle.GetComponentsInChildren<PathGuardian>())
            {
                var visual=guardian.transform.Find("Rotation pivot");
                if(visual.Find("Rear identity carving")!=null)continue;
                var rear=Child("Rear identity carving",visual,new Vector3(0,.81f,-.34f));rear.transform.localRotation=Quaternion.Euler(0,180,0);Symbol(rear.transform,guardian.Symbol,material);
            }
            EditorSceneManager.SaveScene(scene);if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        public static void Preview()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var puzzle=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PathGuardiansPuzzle>(true)).Single();
                var ps=new SerializedObject(puzzle);var refs=ps.FindProperty("_guardians");var answers=ps.FindProperty("_solution");var fs=new SerializedObject(puzzle.GetComponent<GuardiansAlignmentFeedback>());
                for(int i=0;i<3;i++)
                {
                    var guardian=(PathGuardian)refs.GetArrayElementAtIndex(i).objectReferenceValue;
                    bool aligned=new SerializedObject(guardian).FindProperty("_initialOrientation").intValue==answers.GetArrayElementAtIndex(i).intValue;
                    var block=new MaterialPropertyBlock();block.SetColor("_EmissionColor",aligned?new Color(.8f,.48f,.12f):Color.black);
                    foreach(string field in new[]{"_individualMarks","_centralMarks"})((Renderer)fs.FindProperty(field).GetArrayElementAtIndex(i).objectReferenceValue).SetPropertyBlock(block);
                }
                var camera=new GameObject("Temporary guardian preview").AddComponent<Camera>();camera.transform.position=new Vector3(25,8,2);camera.transform.LookAt(new Vector3(32,1.5f,10));camera.fieldOfView=65;
                var target=new RenderTexture(1440,1000,24);target.Create();
                for(int i=0;i<2;i++)UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target});
                RenderTexture.active=target;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TikalGuardiansOverview.png"),image.EncodeToPNG());
                RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        [MenuItem("Tikal/Authoring/Polish guardians UX")]
        public static void Polish()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var puzzle=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PathGuardiansPuzzle>(true)).Single();
                var pieces=puzzle.GetComponentsInChildren<PathGuardian>().OrderBy(g=>g.name).ToArray();int[] initial={3,0,0};
                string path="Assets/_Project/Art/Materials/Level01/GuardiansAlignment.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.38f,.27f,.12f));material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);AssetDatabase.CreateAsset(material,path);
                }
                var individual=new Renderer[3];var central=new Renderer[3];
                var focal=puzzle.transform.Find("Central neutral stone");
                for(int i=0;i<3;i++)
                {
                    Set(pieces[i],"_initialOrientation",initial[i]);pieces[i].transform.Find("Rotation pivot").localRotation=Quaternion.Euler(0,initial[i]*90,0);
                    var engraving=pieces[i].transform.Find("Pedestal alignment engraving");
                    if(engraving==null) engraving=Cube("Pedestal alignment engraving",pieces[i].transform,new Vector3(0,.175f,.48f),new Vector3(.55f,.025f,.07f),material).transform;
                    individual[i]=engraving.GetComponent<Renderer>();
                    var centralName="Progress engraving "+(char)('A'+i);var mark=focal.Find(centralName);
                    if(mark==null)mark=Cube(centralName,focal,new Vector3((i-1)*.4f,.272f,0),new Vector3(.22f,.025f,.42f),material).transform;
                    central[i]=mark.GetComponent<Renderer>();
                }
                var feedback=puzzle.GetComponent<GuardiansAlignmentFeedback>();if(feedback==null)feedback=puzzle.gameObject.AddComponent<GuardiansAlignmentFeedback>();
                Set(feedback,"_puzzle",puzzle);Set(feedback,"_individualMarks",individual.Cast<UnityEngine.Object>().ToArray());Set(feedback,"_centralMarks",central.Cast<UnityEngine.Object>().ToArray());
                var carving=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralCarving.mat");
                foreach(var marker in puzzle.GetComponentsInChildren<DestinationMarker>())
                {
                    var glyph=marker.transform.Find("Matching abstract symbol");glyph.localPosition=new Vector3(0,1.8f,.18f);glyph.localScale=Vector3.one*1.25f;
                    var rear=marker.transform.Find("Rear matching symbol");
                    if(rear==null){rear=Child("Rear matching symbol",marker.transform,new Vector3(0,1.8f,-.18f)).transform;rear.localRotation=Quaternion.Euler(0,180,0);Symbol(rear,marker.Symbol,carving);}
                    rear.localScale=Vector3.one*1.25f;
                }
                // Authored preview mirrors runtime initial feedback without changing shared material.
                var block=new MaterialPropertyBlock();for(int i=0;i<3;i++){block.SetColor("_EmissionColor",i==0?new Color(.8f,.48f,.12f):Color.black);individual[i].SetPropertyBlock(block);central[i].SetPropertyBlock(block);}
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TIKAL_GUARDIANS_POLISHED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void Set(UnityEngine.Object o,string name,object value)
        {
            var so=new SerializedObject(o);var p=so.FindProperty(name);
            if(p==null)throw new Exception("Unknown serialized property "+name);
            if(value is int n)p.intValue=n;
            else if(value is string s)p.stringValue=s;
            else if(value is int[] a){p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).intValue=a[i];}
            else if(value is UnityEngine.Object[] refs){p.arraySize=refs.Length;for(int i=0;i<refs.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=refs[i];}
            else p.objectReferenceValue=(UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Child(string name,Transform parent,Vector3 local)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=local;return g;}
        static GameObject Cube(string name,Transform parent,Vector3 local,Vector3 scale,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static void Line(Transform t,Vector2 a,Vector2 b,Material material)
        {
            Vector2 d=b-a;var line=Cube("Neutral symbol stroke",t,new Vector3((a.x+b.x)*.5f,(a.y+b.y)*.5f,0),new Vector3(d.magnitude,.045f,.035f),material);
            line.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        }
        static void Symbol(Transform t,GuardianSymbol symbol,Material m)
        {
            if(symbol==GuardianSymbol.SplitCircle)
            {
                for(int i=0;i<20;i++){float a=i*18*Mathf.Deg2Rad,b=(i+1)*18*Mathf.Deg2Rad;Line(t,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.25f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.25f,m);}
                Line(t,new Vector2(0,-.25f),new Vector2(0,.25f),m);
            }
            else if(symbol==GuardianSymbol.Angular)
            {
                var a=new Vector2(-.25f,-.21f);var b=new Vector2(0,.27f);var c=new Vector2(.25f,-.21f);Line(t,a,b,m);Line(t,b,c,m);Line(t,c,a,m);
            }
            else for(int i=0;i<6;i++){float a=i*60*Mathf.Deg2Rad;Line(t,Vector2.zero,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.27f,m);}
        }
        static float Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,50,z),Vector3.down,100).Where(h=>h.collider is MeshCollider).ToArray();
            if(hits.Length==0)throw new Exception("Missing terrain at "+x+","+z);
            return hits.OrderBy(h=>h.distance).First().point.y;
        }
        static GameObject Place(string name,Transform parent,float x,float z)
        {var g=Child(name,parent,Vector3.zero);g.transform.position=new Vector3(x,Ground(x,z)+.03f,z);return g;}
        [MenuItem("Tikal/Authoring/Install path guardians (once)")]
        public static void Run()
        {
            try
            {
                if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");Physics.SyncTransforms();
                var root=scene.GetRootGameObjects().Single().transform;var anchor=root.Find("Environment/Zone02_PathGuardians/PuzzleAnchor");
                if(anchor.Find("PathGuardians")!=null)throw new Exception("Guardians already installed; refusing duplicate authoring.");
                var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralStone.mat");
                var mark=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralCarving.mat");
                var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/EntranceStone.mat");
                var assembly=Child("PathGuardians",anchor,Vector3.zero);var puzzle=assembly.AddComponent<PathGuardiansPuzzle>();
                Set(puzzle,"_definition",AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.path_guardians.asset"));Set(puzzle,"_initialState",(int)PuzzleState.Available);
                int[] initial={1,0,3},solution={3,1,2};
                Vector2[] positions={new Vector2(32.5f,8),new Vector2(34.5f,11),new Vector2(31,16)};
                Vector2[] targets={new Vector2(26.5f,8),new Vector2(37,11),new Vector2(31,4)};
                var guardians=new PathGuardian[3];var markers=new DestinationMarker[3];
                for(int i=0;i<3;i++)
                {
                    var guardian=Place("Guardian "+(char)('A'+i),assembly.transform,positions[i].x,positions[i].y);guardians[i]=guardian.AddComponent<PathGuardian>();
                    var hit=guardian.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.05f,0);hit.size=new Vector3(.9f,2.1f,.9f);
                    var visual=Child("Rotation pivot",guardian.transform,Vector3.zero).transform;
                    Cube("Stone monolith",visual,new Vector3(0,1.05f,0),new Vector3(.88f,2.1f,.6f),stone);
                    Cube("Brow",visual,new Vector3(0,1.86f,.33f),new Vector3(.65f,.14f,.16f),dark);
                    foreach(float x in new[]{-.20f,.20f})Cube("Eye recess",visual,new Vector3(x,1.63f,.315f),new Vector3(.13f,.11f,.06f),dark);
                    Cube("Facing nose",visual,new Vector3(0,1.43f,.40f),new Vector3(.12f,.26f,.23f),stone);
                    var symbol=Child("Abstract matching symbol",visual,new Vector3(0,.81f,.34f));Symbol(symbol.transform,(GuardianSymbol)i,mark);
                    var rear=Child("Rear identity carving",visual,new Vector3(0,.81f,-.34f));rear.transform.localRotation=Quaternion.Euler(0,180,0);Symbol(rear.transform,(GuardianSymbol)i,mark);
                    Cube("Fixed plinth",guardian.transform,new Vector3(0,.08f,0),new Vector3(1.05f,.16f,1.05f),dark,true);
                    Set(guardians[i],"_puzzle",puzzle);Set(guardians[i],"_visual",visual);Set(guardians[i],"_symbol",i);Set(guardians[i],"_initialOrientation",initial[i]);visual.localRotation=Quaternion.Euler(0,initial[i]*90,0);
                    var marker=Place("DestinationMarker "+(char)('A'+i),assembly.transform,targets[i].x,targets[i].y);markers[i]=marker.AddComponent<DestinationMarker>();Set(markers[i],"_symbol",i);
                    Vector3 facing=guardian.transform.position-marker.transform.position;facing.y=0;marker.transform.rotation=Quaternion.LookRotation(facing);
                    Cube("Intact marker stone",marker.transform,new Vector3(0,1.30f,0),new Vector3(.95f,1.85f,.3f),stone,true);
                    Cube("Marker footing",marker.transform,new Vector3(0,.16f,0),new Vector3(1.15f,.32f,.70f),dark,true);
                    var glyph=Child("Matching abstract symbol",marker.transform,new Vector3(0,1.55f,.18f));Symbol(glyph.transform,(GuardianSymbol)i,mark);
                }
                Set(puzzle,"_guardians",guardians.Cast<UnityEngine.Object>().ToArray());Set(puzzle,"_destinations",markers.Cast<UnityEngine.Object>().ToArray());Set(puzzle,"_solution",solution);
                var focal=Place("Central neutral stone",assembly.transform,33.5f,10.2f);Cube("Low central platform",focal.transform,new Vector3(0,.13f,0),new Vector3(1.5f,.26f,1.5f),dark,true);
                var light=Child("Solved warm light",focal.transform,new Vector3(0,1.4f,0)).AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.37f);light.intensity=2;light.range=5;light.shadows=LightShadows.None;light.enabled=false;
                var feedback=assembly.AddComponent<GuardiansSolvedFeedback>();Set(feedback,"_puzzle",puzzle);Set(feedback,"_light",light);
                var clue=AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.path_guardians.asset");Set(clue,"_id","level01.clue02");Set(clue,"_title","El origen");Set(clue,"_description","Todo camino recuerda dónde comenzó.");EditorUtility.SetDirty(clue);
                var reward=assembly.AddComponent<PuzzleClueReward>();Set(reward,"_puzzle",puzzle);Set(reward,"_tracker",root.GetComponentInChildren<ClueTracker>(true));Set(reward,"_clue",clue);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TIKAL_GUARDIANS_AUTHORED");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
