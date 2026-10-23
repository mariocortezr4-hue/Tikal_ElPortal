using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Editor.Art
{
    // Final cleanup only: survey, individually reviewed visual edits and evidence.
    [InitializeOnLoad]
    public static class Level01FinalVisualCleanup
    {
        public const string ScenePath="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        public const string Evidence="../_Level01FinalPolish_20261023";
        [Serializable] public class Item
        {
            public string id,path,prefab,mesh,materials,components;
            public Vector3 position,min,max;
            public bool active,enabled;
        }
        [Serializable] public class Survey {public int missingScripts,missingMaterials;public Item[] renderers,placements;}
        [Serializable] public class Edit {public string id,reason,material;public bool hide,replaceStele,groundUV;public Vector3 delta;}
        [Serializable] public class Plan {public Edit[] edits;}
        static Level01FinalVisualCleanup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var op in new[]{"Survey","Apply","Capture","Contacts","Probe","Seam"})
            {
                string request="Temp/Level01Final"+op+".request";if(!File.Exists(request))continue;
                File.Delete(request);
                try{if(op=="Survey")Inspect();else if(op=="Apply")Apply();else if(op=="Contacts")Contacts();else if(op=="Probe")Probe();else if(op=="Seam")CloseSeam();else Capture();}
                catch(Exception e){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
                return;
            }
        }
        static Scene Open()
        {
            var s=EditorSceneManager.GetActiveScene();if(s.isDirty&&!EditorSceneManager.SaveScene(s))throw new Exception("Could not preserve current scene.");
            if(s.path!=ScenePath)s=EditorSceneManager.OpenScene(ScenePath);return s;
        }
        public static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        static Component[] All(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
        static Item Describe(Component c)
        {
            var r=c as Renderer;var bounds=r!=null?r.bounds:new Bounds(c.transform.position,Vector3.zero);
            if(r==null){var rs=c.GetComponentsInChildren<Renderer>(true);if(rs.Length>0){bounds=rs[0].bounds;foreach(var x in rs.Skip(1))bounds.Encapsulate(x.bounds);}}
            var f=c.GetComponent<MeshFilter>();return new Item{id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),path=PathOf(c.transform),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject),position=c.transform.position,min=bounds.min,max=bounds.max,active=c.gameObject.activeInHierarchy,enabled=r==null||r.enabled,mesh=f!=null&&f.sharedMesh!=null?f.sharedMesh.name:"",materials=r!=null?string.Join(";",r.sharedMaterials.Select(m=>m==null?"MISSING":m.name)):"",components=string.Join(",",c.GetComponents<Component>().Select(x=>x==null?"MISSING":x.GetType().Name))};
        }
        public static void Inspect()
        {
            var s=Open();Directory.CreateDirectory(Evidence);if(!File.Exists(Evidence+"/Before.unity"))File.Copy(ScenePath,Evidence+"/Before.unity");var all=All(s);
            File.WriteAllText(Evidence+"/Survey.json",JsonUtility.ToJson(new Survey{missingScripts=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),missingMaterials=all.OfType<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Sum(r=>r.sharedMaterials.Count(m=>m==null)),renderers=all.OfType<Renderer>().Select(Describe).ToArray(),placements=all.OfType<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(Describe).ToArray()},true));
            Capture("Before");Debug.Log("LEVEL01_FINAL_SURVEY_READY");
        }
        static void Apply()
        {
            var s=Open();var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Evidence+"/Plan.json"));
            foreach(var e in plan.edits)
            {
                if(!GlobalObjectId.TryParse(e.id,out var id))throw new Exception("Invalid object ID");var obj=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Component;
                if(obj==null||obj.gameObject.scene!=s)throw new Exception("Object is outside Level01");
                if(!string.IsNullOrEmpty(e.material))
                {var r=obj as Renderer;var m=AssetDatabase.LoadAssetAtPath<Material>(e.material);if(r==null||m==null)throw new Exception("Existing renderer/material required");Undo.RecordObject(r,e.reason);r.sharedMaterials=r.sharedMaterials.Select(_=>m).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    if(e.groundUV)
                    {
                        if(!PathOf(r.transform).Contains("/Terrain/"))throw new Exception("Ground UV only for existing floor");const string folder="Assets/_Project/Art/Environment/Level01/FinalCleanup";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","FinalCleanup");
                        var f=r.GetComponent<MeshFilter>();var original=f.sharedMesh;string path=folder+"/UV_"+f.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if(mesh==null){mesh=UnityEngine.Object.Instantiate(original);mesh.name="UV_"+f.name;mesh.uv=mesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new Vector2(w.x,w.z)*.28f;}).ToArray();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);}
                        Undo.RecordObject(f,e.reason);f.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(f);
                        // Collider deliberately keeps its original mesh; vertices/topology are identical.
                    }
                }
                else if(e.replaceStele)
                {
                    var t=obj as Transform;if(t==null||t.name!="Plaza_ErodedStele"||t.GetComponentsInChildren<Collider>(true).Length>0||t.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("Decorative stele only");
                    foreach(var r in t.GetComponentsInChildren<Renderer>(true)){Undo.RecordObject(r,e.reason);r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}foreach(var l in t.GetComponentsInChildren<LODGroup>(true)){Undo.RecordObject(l,e.reason);l.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Level01/PFB_ROC_RocaMusgo_01.prefab");var rock=(GameObject)PrefabUtility.InstantiatePrefab(prefab,t.parent);rock.name="Plaza_MossRock_FinalCleanup";rock.transform.SetPositionAndRotation(new Vector3(5.5f,.32f,20),Quaternion.Euler(0,37,0));rock.transform.localScale=Vector3.one*.85f;
                    foreach(var c in rock.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);PrefabUtility.RecordPrefabInstancePropertyModifications(rock.transform);
                }
                else if(e.hide){var r=obj as Renderer;if(r==null)throw new Exception("Hide requires Renderer");Undo.RecordObject(r,e.reason);r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                else
                {
                    var t=obj as Transform;if(t==null||t.GetComponentsInChildren<Collider>(true).Length>0||t.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("Move only isolated decorative art without colliders or logic");
                    if(e.delta.magnitude>2)throw new Exception("Microadjustment maximum 2m");Undo.RecordObject(t,e.reason);t.position+=e.delta;PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                }
            }
            EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/Applied.json",JsonUtility.ToJson(plan,true));Capture("After");Debug.Log("LEVEL01_FINAL_CLEANUP_SAVED");
        }
        static void Capture()=>Capture("Final");
        static void CloseSeam()
        {
            var scene=Open();var env=scene.GetRootGameObjects().Single().transform.Find("Environment");if(env.Find("FinalCleanup_Zone03_04_BankSeam")!=null)throw new Exception("Seam already closed");
            var f=env.Find("ENV_L01_Zone04_ObservationTerrace/OrganicTerraceBanks_45x30m").GetComponent<MeshFilter>();var mesh=f.sharedMesh;var verts=mesh.vertices;var tris=mesh.triangles;
            var counts=new System.Collections.Generic.Dictionary<(int,int),int>();
            for(int i=0;i<tris.Length;i+=3)for(int j=0;j<3;j++){int a=tris[i+j],b=tris[i+(j+1)%3];var key=a<b?(a,b):(b,a);counts[key]=counts.TryGetValue(key,out int n)?n+1:1;}
            var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var indices=new System.Collections.Generic.List<int>();
            var height=typeof(Tikal.Tests.Editor.Zone03EnvironmentArtPass).GetMethod("Height",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            foreach(var edge in counts.Where(k=>k.Value==1))
            {
                var a=f.transform.TransformPoint(verts[edge.Key.Item1]);var b=f.transform.TransformPoint(verts[edge.Key.Item2]);if(Mathf.Abs(a.x-20)>.01f||Mathf.Abs(b.x-20)>.01f||Mathf.Min(a.z,b.z)<29.5f||Mathf.Max(a.z,b.z)>50)continue;
                if(a.z>b.z){var swap=a;a=b;b=swap;}float lowA=(float)height.Invoke(null,new object[]{20.95f,a.z})-.04f,lowB=(float)height.Invoke(null,new object[]{20.95f,b.z})-.04f;
                if(Mathf.Max(a.y-lowA,b.y-lowB)<.12f)continue;int n=v.Count;v.AddRange(new[]{a+Vector3.left*.02f,b+Vector3.left*.02f,new Vector3(20.95f,lowA,a.z),new Vector3(20.95f,lowB,b.z)});uv.AddRange(new[]{new Vector2(a.z,a.y)*.28f,new Vector2(b.z,b.y)*.28f,new Vector2(a.z,lowA)*.28f,new Vector2(b.z,lowB)*.28f});indices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
            }
            if(indices.Count==0||indices.Count>600)throw new Exception("Unexpected seam scope");var patch=new Mesh{name="MESH_Zone03_04_BankSeam"};patch.vertices=v.ToArray();patch.uv=uv.ToArray();patch.triangles=indices.ToArray();patch.RecalculateNormals();patch.RecalculateTangents();patch.RecalculateBounds();AssetDatabase.CreateAsset(patch,"Assets/_Project/Art/Environment/Level01/FinalCleanup/"+patch.name+".asset");var go=new GameObject("FinalCleanup_Zone03_04_BankSeam");go.transform.SetParent(env,false);go.AddComponent<MeshFilter>().sharedMesh=patch;go.AddComponent<MeshRenderer>().sharedMaterial=f.GetComponent<Renderer>().sharedMaterial;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/Seam.txt","One visual skirt joins existing Zone04 east terrain edge to Zone03 bank at X20..20.95, Z29.5..50, excludes existing connector opening. Original terrain, collider and navigation untouched. New triangles="+indices.Count/3+"; one placement; no collider.");Debug.Log("LEVEL01_FINAL_SEAM_SAVED");
        }
        static void Probe()
        {
            var scene=Open();var intersect=typeof(HandleUtility).GetMethod("IntersectRayMesh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);var lines=new System.Collections.Generic.List<string>();
            var go=new GameObject("ProbeCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.fieldOfView=65;camera.aspect=16f/9;
            try
            {
                foreach(var sample in new[]{("Bank",new Vector3(29,2.1f,35),new Vector2(.04f,.65f)),("Column",new Vector3(0,2.3f,60),new Vector2(.05f,.58f))})
                {
                    camera.transform.SetPositionAndRotation(sample.Item2,Quaternion.identity);var ray=camera.ViewportPointToRay(sample.Item3);var hits=new System.Collections.Generic.List<(float,string)>();
                    foreach(var f in All(scene).OfType<MeshFilter>())
                    {
                        var r=f.GetComponent<Renderer>();if(f.sharedMesh==null||r==null||!r.enabled||!r.gameObject.activeInHierarchy||!r.bounds.IntersectRay(ray))continue;
                        object[] args={ray,f.sharedMesh,f.transform.localToWorldMatrix,null};if(!(bool)intersect.Invoke(null,args))continue;var hit=(RaycastHit)args[3];var root=PrefabUtility.GetOutermostPrefabInstanceRoot(f.gameObject);var t=root!=null?root.transform:f.transform;
                        hits.Add((hit.distance,sample.Item1+" "+PathOf(f.transform)+" point="+hit.point+" root="+GlobalObjectId.GetGlobalObjectIdSlow(t)+" rootPosition="+t.position+" bounds="+r.bounds));
                    }
                    lines.AddRange(hits.OrderBy(h=>h.Item1).Take(4).Select(h=>h.Item2));
                }
                File.WriteAllLines(Evidence+"/VisualContactProbe.txt",lines);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
            Debug.Log("LEVEL01_FINAL_VISUAL_PROBE_READY");
        }
        static void Contacts()
        {
            var scene=Open();var all=All(scene);var temp=new GameObject("ContactSurvey_Transient"){hideFlags=HideFlags.HideAndDontSave};var cols=new System.Collections.Generic.List<MeshCollider>();var lines=new System.Collections.Generic.List<string>();
            try
            {
                foreach(var f in all.OfType<MeshFilter>().Where(f=>f.sharedMesh!=null&&(PathOf(f.transform).Contains("/Terrain/")||f.name.Contains("Organic")||f.name.Contains("Banks")||f.name.Contains("Relief"))))
                {var go=new GameObject("ContactSurface"){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(temp.transform);go.transform.SetPositionAndRotation(f.transform.position,f.transform.rotation);go.transform.localScale=f.transform.lossyScale;var c=go.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;cols.Add(c);}
                Physics.SyncTransforms();
                foreach(var t in all.OfType<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.name.Contains("Columna")&&t.gameObject.activeInHierarchy))
                {
                    var item=Describe(t);var ray=new Ray(new Vector3(t.position.x,30,t.position.z),Vector3.down);float h=float.NegativeInfinity;foreach(var c in cols)if(c.Raycast(ray,out var hit,60))h=Mathf.Max(h,hit.point.y);
                    if(!float.IsNegativeInfinity(h))lines.Add(item.id+" | "+item.path+" | base="+item.min.y.ToString("F4")+" ground="+h.ToString("F4")+" gap="+(item.min.y-h).ToString("F4"));
                }
                File.WriteAllLines(Evidence+"/ColumnContacts.txt",lines);
            }
            finally{UnityEngine.Object.DestroyImmediate(temp);}
            Debug.Log("LEVEL01_FINAL_CONTACTS_READY");
        }
        static void Capture(string folder)
        {
            Open();Directory.CreateDirectory(Evidence+"/"+folder);var go=new GameObject("FinalCleanup_TransientCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.08f;camera.farClipPlane=250;
            var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;var previous=RenderTexture.active;
            var names=new[]{"Final_Entrance","Final_Puzzle01","Final_Puzzle02","Final_Zone03","Final_Zone04","Final_Zone05","Final_FalsePath","Connector01","Connector02","Connector03","Connector04","Connector05","Connector06"};
            var positions=new[]{new Vector3(0,2.3f,-10),new Vector3(0,2.3f,8),new Vector3(29,3,12),new Vector3(29,2.1f,35),new Vector3(0,3.4f,40),new Vector3(0,2.4f,60),new Vector3(6,2.4f,82),new Vector3(0,2.2f,2),new Vector3(12,2.3f,12),new Vector3(29,2.6f,25),new Vector3(17,3,39),new Vector3(0,3,53),new Vector3(6,2.4f,76)};
            var yaw=new[]{0f,0f,90f,0f,235f,0f,0f,0f,90f,0f,270f,0f,0f};
            try
            {
                for(int i=0;i<names.Length;i++){camera.transform.SetPositionAndRotation(positions[i],Quaternion.Euler(2,yaw[i],0));camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Evidence+"/"+folder+"/"+names[i]+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
