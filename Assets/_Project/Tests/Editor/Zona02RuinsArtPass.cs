using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Zona02RuinsArtPass
    {
        public const string Evidence="../_Zona02SecondPass_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zona02SecondPass";
        const string Root="ENV_L01_Zona02_RuinsSecondPass";
        static Transform root;
        static readonly List<GameObject> placed=new List<GameObject>();
        static readonly List<GameObject> hanging=new List<GameObject>();
        [Serializable] class Report {public int placements,readjusted,newColliders,terrainTriangles;public string[] assets,checks;}
        static Zona02RuinsArtPass(){EditorApplication.update+=Poll;}
        static void Check(bool ok,string s){if(!ok)throw new InvalidOperationException(s);}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Temp/Zona02RuinsSurvey.request"))
            {
                File.Delete("Temp/Zona02RuinsSurvey.request");Directory.CreateDirectory(Evidence);Survey();return;
            }
            if(File.Exists("Temp/Zona02RuinsFinish.request"))
            {
                File.Delete("Temp/Zona02RuinsFinish.request");try{FinishCurrent();}catch(Exception e){File.WriteAllText(Evidence+"/FinishError.txt",e.ToString());Debug.LogException(e);}return;
            }
            if(File.Exists("Temp/Zona02RuinsFront.request"))
            {
                File.Delete("Temp/Zona02RuinsFront.request");try{ForegroundRuins();}catch(Exception e){File.WriteAllText(Evidence+"/ForegroundError.txt",e.ToString());Debug.LogException(e);}return;
            }
            if(!File.Exists("Temp/Zona02RuinsApply.request"))return;
            File.Delete("Temp/Zona02RuinsApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Survey()
        {
            var lines=new List<string>();
            foreach(var asset in new[]{"ARQ_MuroMaya_01","ARQ_BloqueMaya_02","ARQ_ColumnaDerrumbada_01","ROC_RocaMusgo_01","ROC_RocaMusgo_02","ROC_RocaFragmentada_01","ROC_RocaPlana_01","VEG_HojasSecasMonton_01"})
            {
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));if(p==null){lines.Add(asset+" MISSING");continue;}
                var g=UnityEngine.Object.Instantiate(p);var b=GoldenSliceArtPass.VisualBounds(g);lines.Add(asset+" size="+b.size.ToString("F3")+" min="+b.min.ToString("F3"));UnityEngine.Object.DestroyImmediate(g);
            }
            File.WriteAllLines(Evidence+"/LocalAssetBounds.txt",lines);
        }
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static bool Exit(float x,float z)=>z>20.9f&&x>26.5f&&x<31.5f;
        static bool Bank(float x,float z)=>x>=19&&x<=49&&z>=-10&&z<=29&&!(x<39&&z>3&&z<21)&&!Exit(x,z);
        static float Height(float x,float z)
        {
            float d=Mathf.Max(3-z,z-21,x-39,0),f=Mathf.SmoothStep(0,1,d/4);
            float h=1.2f+2.6f*(1-Mathf.Exp(-d*.3f))+f*(.46f*Mathf.Sin(x*.45f+z*.31f)+.24f*Mathf.Cos(x*.71f-z*.37f));
            // Keep the playable rim intact, adding irregular broad masses farther out.
            float crest=.65f+1.2f*(.5f+.5f*Mathf.Sin(x*.29f-z*.38f));
            return h+Mathf.SmoothStep(0,1,(d-.8f)/4)*crest;
        }
        static Mesh Terrain()
        {
            const int nx=61,nz=79;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=19+ix*.5f,z=-10+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Bank(19+(ix+.5f)*.5f,-10+(iz+.5f)*.5f)){int a=iz*nx+ix;tri.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_Zona02_WiderIrregularBanks",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tri.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static GameObject Put(string asset,float x,float z,Vector3 scale,float yaw,float burial=.08f,bool background=false,float pitch=0,float roll=0)
        {
            Check(Bank(x,z),"Placement outside local non-playable banks "+asset+" "+x+"/"+z);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Existing asset "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Z02_Ruins_"+asset+"_"+placed.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,Height(x,z),z),Quaternion.Euler(pitch,yaw,roll));g.transform.localScale=scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float h=Height(x,z);
            if(asset.StartsWith("ROC_")||asset.StartsWith("ARQ_")||asset.Contains("Raiz"))h=Mathf.Min(h,Height(b.min.x,b.min.z),Height(b.max.x,b.max.z),Height(b.min.x,b.max.z),Height(b.max.x,b.min.z));
            g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*burial);
            var lod=g.GetComponentInChildren<LODGroup>();
            if(background&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.006f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){if(background)r.shadowCastingMode=ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static GameObject Put(string a,float x,float z,float s,float yaw,float burial=.08f,bool bg=false)=>Put(a,x,z,Vector3.one*s,yaw,burial,bg);
        static void Drape(GameObject support,float yaw)
        {
            var b=GoldenSliceArtPass.VisualBounds(support);float x=support.transform.position.x,z=support.transform.position.z;
            var l=Put("VEG_CortinaLianas_01",x,z,.57f,yaw,0,true);var lb=GoldenSliceArtPass.VisualBounds(l);l.transform.position+=Vector3.up*(b.max.y-.08f-lb.max.y);hanging.Add(l);PrefabUtility.RecordPrefabInstancePropertyModifications(l.transform);
            var f=Put("VEG_HelechoTropical_02",x+.22f,z+.1f,.66f,yaw+50,0);var fb=GoldenSliceArtPass.VisualBounds(f);f.transform.position+=Vector3.up*(b.max.y-.12f-fb.min.y);hanging.Add(f);PrefabUtility.RecordPrefabInstancePropertyModifications(f.transform);
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01 Edit Mode");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(Root)==null,"Second pass already exists; no blind reapply");
            var all=Level01EnvironmentArtPass.All(scene);var originals=all.Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);
            Check(env.Find("ENV_L01_Zona02_JungleBanks")!=null,"Published first pass retained");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zona02SecondPass");
            root=new GameObject(Root).transform;root.SetParent(env,false);placed.Clear();hanging.Clear();
            var ground=env.Find("ENV_L01_Zona02_JungleBanks/BroadOrganicBanks_27x36m").GetComponent<MeshFilter>();ground.sharedMesh=Terrain();
            var adjusted=new HashSet<Transform>();
            foreach(var branch in new[]{env.Find("ENV_L01_Zona02_JungleBanks"),env.Find("ENV_L01_Puzzle02_AccessBase")})
            foreach(var t in branch.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&Bank(t.position.x,t.position.z)))
            {
                var b=GoldenSliceArtPass.VisualBounds(t.gameObject);float sink=t.name.Contains("ROC_")||t.name.Contains("Rock")?b.size.y*.2f:t.name.Contains("Raiz")?b.size.y*.12f:0;
                t.position+=Vector3.up*(Height(t.position.x,t.position.z)-b.min.y-sink);PrefabUtility.RecordPrefabInstancePropertyModifications(t);adjusted.Add(t);
            }
            // Seven broken retaining-wall clusters, following the perimeter and leaving the north exit open.
            var sectors=new[]{new Vector3(23,-.7f,0),new Vector3(30,-1.1f,0),new Vector3(36,-.4f,0),new Vector3(42,7,270),new Vector3(42,16,270),new Vector3(35,24.2f,180),new Vector3(23,24,180)};
            for(int i=0;i<sectors.Length;i++)
            {
                float x=sectors[i].x,z=sectors[i].y,yaw=sectors[i].z;var along=Quaternion.Euler(0,yaw,0)*Vector3.right;var back=Quaternion.Euler(0,yaw,0)*Vector3.back;
                var wall=Put("ARQ_MuroMaya_01",x,z,new Vector3(1.25f,.85f+(i%3)*.16f,1.15f),yaw+(i%2==0?5:-7),.12f,false,0,i%2==0?3:-4);Drape(wall,yaw);
                for(int j=0;j<3;j++)
                {
                    var p=new Vector3(x,0,z)+along*((j-1)*1.35f)+back*(j%2==0?1.2f:1.8f);
                    Put(j==1?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02",p.x,p.z,1.1f+j*.15f,43*i+71*j,.3f);
                    var q=p+back*1.1f;Put("VEG_PlantaTropical_02",q.x,q.z,1f,i*61+j*47,.05f,true);
                }
                var end=new Vector3(x,0,z)+along*2.2f;Put("ARQ_BloqueMaya_02",end.x,end.z,1.5f,yaw+23,.2f);
                var fallen=new Vector3(x,0,z)-along*2.2f+back*.7f;Put("ARQ_ColumnaDerrumbada_01",fallen.x,fallen.z,.7f,yaw+65,.22f);
                var fern=new Vector3(x,0,z)+along*.8f-back*.3f;Put("VEG_ArbustoTropical_03",fern.x,fern.z,1.1f,i*79,.06f,true);
            }
            // Staggered lower rocks and plants obscure regular bank edges without entering the room.
            for(int side=0;side<2;side++)for(int i=0;i<8;i++)
            {
                float x=20.4f+i*2.45f,z=side==0?1.5f:22.4f;if(Exit(x,z))continue;
                Put("ROC_RocaPlana_01",x,z,.9f+i%3*.12f,i*83+side*19,.35f);
                Put("VEG_HelechoTropical_02",x+.3f,z+(side==0?-.75f:.75f),.8f+i%2*.15f,i*61,.05f);
                Put("VEG_HojasSecasMonton_01",x-.3f,z+(side==0?.35f:-.35f),.8f,i*47,.08f);
            }
            foreach(var p in new[]{new Vector2(22,-6.8f),new Vector2(29,-7.2f),new Vector2(36,-6.6f),new Vector2(46,2),new Vector2(46.3f,11),new Vector2(45.5f,22),new Vector2(36,27.7f),new Vector2(22,27.7f)})
            {
                int i=placed.Count;Put("VEG_CeibaJoven_01",p.x,p.y,.85f,i*47,0,true);Put("VEG_ArbustoTropical_03",p.x+.5f,p.y-.5f,1.3f,i*83,.05f,true);
                Put("VEG_RaizTropical_01",p.x-.3f,p.y+.4f,.75f,i*37,.15f,true);
            }
            Physics.SyncTransforms();Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No art colliders added");
            Check(originals.All(k=>k.Key!=null&&Props(k.Key)==k.Value),"All existing gameplay components and colliders unchanged");Check(poses.All(k=>adjusted.Contains(k.Key)||Pose(k.Key)==k.Value),"Original poses unchanged except local bank art regrounded");
            Check(placed.Where(g=>!hanging.Contains(g)).All(g=>GoldenSliceArtPass.VisualBounds(g).min.y<=Height(g.transform.position.x,g.transform.position.z)+.04f),"New asset bases grounded; drapes supported by wall tops");
            // Original room and entrance are free of newly added solid geometry.
            var playable=new Bounds(new Vector3(29,2.1f,12),new Vector3(19.6f,1.8f,17.6f));
            foreach(var g in placed.Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ARQ_")||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ROC_")))Check(!GoldenSliceArtPass.VisualBounds(g).Intersects(playable),"New solid remains beyond playable room: "+g.name);
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Second pass saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{placements=placed.Count,readjusted=adjusted.Count,newColliders=0,terrainTriangles=ground.sharedMesh.triangles.Length/3,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"All original colliders and MonoBehaviours unchanged","Only existing Zone02 bank decoration regrounded","No new solids inside original room; north exit kept open","No decorative stelae; all imported assets reused","Earlier terrain asset retained; wider irregular visual copy assigned","Saved Level01; no work in other zones"}},true));Debug.Log("ZONA02_SECOND_PASS_SAVED "+placed.Count);
        }
        static void FinishCurrent()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Level01 only");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);Check(root!=null,"Current second pass retained");
            var protectedComponents=Level01EnvironmentArtPass.All(scene).Where(c=>c is Collider||c is MonoBehaviour).ToDictionary(c=>c,Props);
            placed.Clear();placed.AddRange(root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.gameObject));
            var playable=new Bounds(new Vector3(29,2.1f,12),new Vector3(19.6f,1.8f,17.6f));int corrected=0;
            foreach(var g in placed.Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ARQ_")||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ROC_")))
            {
                var b=GoldenSliceArtPass.VisualBounds(g);if(!b.Intersects(playable))continue;
                var p=g.transform.position;if(p.x>39)p.x+=Mathf.Max(0,39.25f-b.min.x);else if(p.z<3)p.z-=Mathf.Max(0,b.max.z-2.75f);else if(p.z>21)p.z+=Mathf.Max(0,21.25f-b.min.z);else throw new InvalidOperationException("Unexpected solid in room "+g.name);
                g.transform.position=p;b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=Vector3.up*(Height(p.x,p.z)-b.min.y-b.size.y*.24f);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);corrected++;
            }
            Check(placed.Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ARQ_")||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ROC_")).All(g=>!GoldenSliceArtPass.VisualBounds(g).Intersects(playable)),"All new solids outside playable room after local placement correction");
            Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new art collider");Check(protectedComponents.All(k=>Props(k.Key)==k.Value),"Existing gameplay/colliders unchanged by correction");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Current pass saved");AssetDatabase.SaveAssets();
            int prior=env.Find("ENV_L01_Zona02_JungleBanks").GetComponentsInChildren<Transform>().Count(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&Bank(t.position.x,t.position.z))+env.Find("ENV_L01_Puzzle02_AccessBase").GetComponentsInChildren<Transform>().Count(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&Bank(t.position.x,t.position.z));
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{placements=placed.Count,readjusted=prior,newColliders=0,terrainTriangles=env.Find("ENV_L01_Zona02_JungleBanks/BroadOrganicBanks_27x36m").GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"Original gameplay components, colliders and poses checked unchanged before initial clearance guard","Existing Zone02 bank art regrounded only","Local correction moves "+corrected+" new rocks beyond room boundary","All new solids outside playable room; north exit preserved","No decorative stelae or new colliders","First pass retained; old terrain asset preserved","Saved Level01; minimal local Play review pending"}},true));Debug.Log("ZONA02_SECOND_PASS_SAVED "+placed.Count);
        }
        static void ForegroundRuins()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved local scene required");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);Check(root!=null&&!root.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Z02_InnerWall")),"Foreground pass not already applied");
            var all=Level01EnvironmentArtPass.All(scene);var props=all.Where(c=>c is Collider||c is MonoBehaviour).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);placed.Clear();hanging.Clear();
            foreach(var p in new[]{new Vector3(24.5f,1.45f,0),new Vector3(34.2f,1.4f,0),new Vector3(40.2f,7,270),new Vector3(40.3f,16.4f,270),new Vector3(23,22.5f,180),new Vector3(35.5f,22.5f,180)})
            {
                int i=placed.Count;var wall=Put("ARQ_MuroMaya_01",p.x,p.y,new Vector3(.92f,1.45f+(i%3)*.1f,1.3f),p.z,.12f,false,0,i%2==0?2:-2);wall.name="Z02_InnerWall_"+i;Drape(wall,p.z);
                // Low broken block in front; narrow footprint stays behind original room boundary.
                var side=Quaternion.Euler(0,p.z,0)*Vector3.right;var outward=Quaternion.Euler(0,p.z,0)*Vector3.back;var q=new Vector3(p.x,0,p.y)+side*1.3f+outward*.25f;
                Put("ARQ_BloqueMaya_02",q.x,q.z,new Vector3(1.3f,1.1f,1.1f),p.z+18,.18f,false,8,12);
                var f=new Vector3(p.x,0,p.y)-side*1.3f+outward*.65f;Put("VEG_PlantaTropical_02",f.x,f.z,.9f,i*41,.04f,true);
            }
            var playable=new Bounds(new Vector3(29,2.1f,12),new Vector3(19.6f,1.8f,17.6f));
            Check(placed.Where(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g).Contains("PFB_ARQ_")).All(g=>!GoldenSliceArtPass.VisualBounds(g).Intersects(playable)),"Foreground ruins outside playable room");
            Check(props.All(k=>Props(k.Key)==k.Value)&&poses.All(k=>Pose(k.Key)==k.Value),"All existing gameplay/colliders/poses preserved");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new colliders");
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));report.placements+=placed.Count;report.assets=report.assets.Concat(placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot)).Distinct().ToArray();report.checks=report.checks.Concat(new[]{"Six closer broken wall fragments draped with lianas and ferns; all outside room","All original poses, gameplay components and colliders preserved during foreground addition"}).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final second pass saved");AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("ZONA02_SECOND_PASS_FINAL "+report.placements);
        }
    }
}
