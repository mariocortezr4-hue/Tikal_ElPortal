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
    public static class Puzzle02AccessArtPass
    {
        public const string Evidence="../_Puzzle02Access_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Puzzle02Access";
        const string RootName="ENV_L01_Puzzle02_AccessBase";
        static Transform root;static Vector3[] bank;static readonly List<GameObject> placed=new List<GameObject>();
        const int NX=45,NZ=57;
        [Serializable] public class Report {public bool passed;public int placements,retired,originalColliders,newColliders,floating,bankTriangles,pathTriangles;public float clearWidth;public string[] retiredPaths,changedRenderers,hiddenProxies,localColliderPaths,checks,assets;}
        static Puzzle02AccessArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isUpdating||EditorApplication.isCompiling||(!File.Exists("Temp/Puzzle02AccessApply.request")&&!File.Exists("Temp/Puzzle02AccessPolish.request")))return;
            bool polish=File.Exists("Temp/Puzzle02AccessPolish.request");File.Delete(polish?"Temp/Puzzle02AccessPolish.request":"Temp/Puzzle02AccessApply.request");Directory.CreateDirectory(Evidence);
            try{if(polish)PolishCurrent();else Apply();}catch(Exception e){File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Puzzle02 access: "+message);}
        static string Id(UnityEngine.Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>Id(t.parent)+t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        public static float Floor(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,35,z),Vector3.down,70,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is MeshCollider&&h.collider.transform.parent!=null&&h.collider.transform.parent.name=="Terrain").OrderBy(h=>h.distance).ToArray();
            Check(hits.Length>0,"Original walking floor at "+x+"/"+z);return hits[0].point.y;
        }
        static float Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,35,z),Vector3.down,70,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is MeshCollider&&h.collider.transform.parent!=null&&(h.collider.transform.parent.name=="Terrain"||h.collider.transform.parent.name=="NaturalBoundaries")).OrderBy(h=>h.collider.transform.parent.name=="Terrain"?0:1).ThenBy(h=>h.distance).ToArray();
            return hits.Length>0?hits[0].point.y:Floor(Mathf.Clamp(x,9.05f,29.8f),12);
        }
        static float Height(float x,float z)
        {
            float floor=Ground(x,z);float edge=Mathf.SmoothStep(0,1,Mathf.Max(Mathf.Abs(z-12)-2,0)/3);
            return floor+.012f+edge*(.1f*Mathf.Sin(x*.8f+z*.5f)+.07f*Mathf.Cos(z*.9f));
        }
        static bool Cell(float x,float z)=>x<19?(z<10||z>14):(z<3||z>21);
        static float Surface(float x,float z)
        {
            if(bank==null)return Height(x,z);
            float gx=Mathf.Clamp((x-9)*2,0,NX-1.001f),gz=Mathf.Clamp((z+2)*2,0,NZ-1.001f);int ix=(int)gx,iz=(int)gz;float u=gx-ix,v=gz-iz;
            float a=bank[iz*NX+ix].y,b=bank[iz*NX+ix+1].y,c=bank[(iz+1)*NX+ix].y,d=bank[(iz+1)*NX+ix+1].y;
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static Transform Group(string n){var t=root.Find(n);if(t!=null)return t;var g=new GameObject(n);g.transform.SetParent(root,false);return g.transform;}
        static Mesh Mesh(string name,Vector3[] verts,Vector2[] uv,int[] indices)
        {
            var m=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");if(m==null){m=new Mesh{name=name};AssetDatabase.CreateAsset(m,Folder+"/"+name+".asset");}else m.Clear();m.vertices=verts;m.uv=uv;m.triangles=indices;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);return m;
        }
        static void Visual(string name,Mesh mesh,Material mat)
        {var g=new GameObject(name);g.transform.SetParent(Group("Ground"),false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;}
        static int Bank(Material mat)
        {
            bank=new Vector3[NX*NZ];var uv=new Vector2[bank.Length];var tri=new List<int>();
            for(int z=0;z<NZ;z++)for(int x=0;x<NX;x++){int i=z*NX+x;float wx=9+x*.5f,wz=-2+z*.5f;bank[i]=new Vector3(wx,Height(wx,wz),wz);uv[i]=new Vector2(wx,wz)*.28f;}
            for(int z=0;z<NZ-1;z++)for(int x=0;x<NX-1;x++){if(!Cell(9+(x+.5f)*.5f,-2+(z+.5f)*.5f))continue;int a=z*NX+x,b=a+1,c=a+NX,d=c+1;tri.AddRange(new[]{a,c,b,b,c,d});}
            Visual("OrganicPathSideBanks",Mesh("MESH_P02Access_Banks",bank,uv,tri.ToArray()),mat);return tri.Count/3;
        }
        static int Path()
        {
            var verts=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
            for(int i=0;i<=100;i++)
            {
                float x=4+i*.25f;
                for(int j=0;j<4;j++){float across=new[]{-1f,-.76f,.76f,1f}[j],z=12+across*1.75f;verts.Add(new Vector3(x,Floor(x,z)+.008f,z));uv.Add(new Vector2(x,z)*.32f);colors.Add(new Color(1,1,1,j==0||j==3?0:1));}
                if(i<100)for(int j=0;j<3;j++){int a=i*4+j;indices.AddRange(new[]{a,a+1,a+4,a+1,a+5,a+4});}
            }
            var mesh=Mesh("MESH_P02Access_ClearPath",verts.ToArray(),uv.ToArray(),indices.ToArray());mesh.colors=colors.ToArray();
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/EntranceHero/MAT_Entrance_WornEarth.mat");Visual("ClearWornPath_3_5m",mesh,mat);return indices.Count/3;
        }
        static bool Hard(string path)=>path.Contains("PFB_ROC_")||path.Contains("PFB_ARQ_")||path.Contains("Arbol")||path.Contains("Ceiba")||path.Contains("Palmera")||path.Contains("Raiz");
        static Bounds? BodyBounds(GameObject g)
        {
            float floor=Floor(Mathf.Clamp(g.transform.position.x,4,29),12);bool any=false;Bounds b=default;
            var lod=g.GetComponentInChildren<LODGroup>();var rs=lod!=null?lod.GetLODs()[0].renderers:g.GetComponentsInChildren<Renderer>(true);
            foreach(var r in rs){var f=r.GetComponent<MeshFilter>();if(f==null)continue;foreach(var v in f.sharedMesh.vertices){var w=f.transform.TransformPoint(v);if(w.y<floor+.14f||w.y>floor+2.3f)continue;if(!any){b=new Bounds(w,Vector3.zero);any=true;}else b.Encapsulate(w);}}
            return any?b:(Bounds?)null;
        }
        static Bounds Clear=>new Bounds(new Vector3(16.5f,1.6f,12),new Vector3(25,4,3.5f));
        static GameObject Put(string asset,string name,float x,float z,float scale,float yaw,float burial=0,bool distant=false)
        {
            Check(x>=10&&x<=30&&z>=-1&&z<=24&&Cell(x,z),"Solid and vegetation placement outside walkable connector/room floor");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Existing prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Group(Hard(asset)?"SolidEdges":"Vegetation"));g.name=name;g.transform.SetPositionAndRotation(new Vector3(x,Surface(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float h=Surface(x,z);if(asset.StartsWith("ROC_")||asset.Contains("Raiz"))h=Mathf.Min(h,Surface(b.min.x,b.min.z),Surface(b.max.x,b.min.z),Surface(b.min.x,b.max.z),Surface(b.max.x,b.max.z));g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*burial+.002f);
            var lod=g.GetComponentInChildren<LODGroup>();if(distant&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.008f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            placed.Add(g);return g;
        }
        static void ProjectBank(Mesh mesh)
        {
            var old=mesh.vertices;var normals=mesh.normals;var ix=mesh.triangles;var v=new Vector3[ix.Length];var ns=new Vector3[ix.Length];var uv=new Vector2[ix.Length];
            for(int i=0;i<ix.Length;i+=3)
            {
                var n=Vector3.Cross(old[ix[i+1]]-old[ix[i]],old[ix[i+2]]-old[ix[i]]).normalized;
                for(int j=0;j<3;j++){int k=i+j;var p=old[ix[k]];v[k]=p;ns[k]=normals[ix[k]];uv[k]=(Mathf.Abs(n.y)>.55f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*.28f;}
            }
            mesh.Clear();mesh.vertices=v;mesh.normals=ns;mesh.uv=uv;mesh.triangles=Enumerable.Range(0,v.Length).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        }
        static void PolishCurrent()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved access pass before directed polish");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(RootName);Check(root!=null,"Existing access layer retained");var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));
            var all=Level01EnvironmentArtPass.All(scene);var plants=all.OfType<Transform>().Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&!t.IsChildOf(root)&&t.position.x>=4&&t.position.x<19&&t.position.z>=5&&t.position.z<=19&&new[]{"Arbusto","Helecho","PlantaTropical"}.Any(n=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains(n))).Where(t=>{var b=BodyBounds(t.gameObject);return b.HasValue&&b.Value.Intersects(Clear);}).ToArray();
            Check(plants.All(t=>t.GetComponentsInChildren<Collider>(true).Length==0),"Only collider-free foliage removed from central strip");foreach(var t in plants)t.gameObject.SetActive(false);
            var pebble=env.Find("ENV_GoldenSlice/1_GROUND/Path_SunkenStone_162/LOD0");var unnecessary=pebble==null?null:pebble.GetComponent<MeshCollider>();if(unnecessary!=null){UnityEngine.Object.DestroyImmediate(unnecessary);report.newColliders--;}
            ProjectBank(AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/MESH_P02Access_Banks.asset"));
            var path=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/MESH_P02Access_ClearPath.asset");var colors=path.colors;var pv=path.vertices;
            for(int i=0;i<colors.Length;i++)colors[i].a*=Mathf.SmoothStep(0,1,(pv[i].x-4)/1.5f)*Mathf.SmoothStep(0,1,(29-pv[i].x)/1.5f);path.colors=colors;EditorUtility.SetDirty(path);
            bank=new Vector3[NX*NZ];for(int z=0;z<NZ;z++)for(int x=0;x<NX;x++){float wx=9+x*.5f,wz=-2+z*.5f;bank[z*NX+x]=new Vector3(wx,Height(wx,wz),wz);}placed.Clear();
            foreach(var p in new[]{new Vector2(21,2.55f),new Vector2(24.5f,2.6f),new Vector2(28,2.5f),new Vector2(21.5f,21.6f),new Vector2(25,21.5f),new Vector2(28.5f,21.7f)})
            {int i=placed.Count;Put("VEG_ArbustoTropical_03","BaseLowerShrub_"+i,p.x,p.y,1,i*71,0,true);Put("VEG_HelechoTropical_02","BaseLowerFern_"+i,p.x+.4f,p.y+(p.y<12?-.3f:.3f),.85f,i*47);}
            report.placements+=placed.Count;report.retiredPaths=report.retiredPaths.Concat(plants.Select(t=>Level01EnvironmentInspection.PathOf(t))).ToArray();report.retired=report.retiredPaths.Length;
            report.localColliderPaths=report.localColliderPaths.Where(p=>!p.Contains("Path_SunkenStone_162")).Select(p=>p.Replace("static LOD0","static visual mesh")).ToArray();
            all=Level01EnvironmentArtPass.All(scene);Check(all.OfType<Collider>().Count(c=>c.enabled)==208+report.newColliders,"Original collider count plus eight local contacts");
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))if(!t.name.StartsWith("HangingLiana"))Check(GoldenSliceArtPass.VisualBounds(t.gameObject).min.y-Surface(t.position.x,t.position.z)<.03f,"Grounded access art "+t.name);
            report.checks=report.checks.Concat(new[]{"Central intrusive foliage retired without deletion","Directional UVs remove slope texture stretching without moving geometry","Path ends feathered","Small stepping stone collider removed; eight solid mesh contacts retained","Twelve lower-edge plants complete base composition"}).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Polish saved");AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("PUZZLE02_ACCESS_POLISHED "+report.placements);
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01 Edit Mode");Physics.SyncTransforms();var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(RootName)==null,"Access pass is additive; existing root must be reviewed before reapplying");
            var all=Level01EnvironmentArtPass.All(scene);Check(all.All(c=>c!=null),"No original Missing Script");
            var retire=all.OfType<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.gameObject.activeInHierarchy&&t.position.x>=4&&t.position.x<19&&t.position.z>=5&&t.position.z<=19&&Hard(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject))).Where(t=>{var b=BodyBounds(t.gameObject);return b.HasValue&&b.Value.Intersects(Clear);}).ToArray();
            Check(retire.All(t=>t.GetComponentsInChildren<Collider>(true).Length==0),"Only collider-free decorative instances retired");
            var selected=all.OfType<MeshRenderer>().Where(r=>r.transform.parent!=null&&((r.transform.parent.name=="Terrain"&&(r.name=="Surface_01_to_02"||r.name=="Surface_Zone02_PathGuardians"))||(r.transform.parent.name=="NaturalBoundaries"&&new[]{"Talud_34","Talud_36","Talud_82","Talud_85","Talud_27","Talud_30"}.Contains(r.name)))).ToArray();
            var proxies=all.OfType<MeshRenderer>().Where(r=>r.transform.parent!=null&&r.transform.parent.name=="Vegetation_Blockout"&&r.bounds.center.x>=9&&r.bounds.center.x<=30&&r.bounds.center.z>=-2&&r.bounds.center.z<=25).ToArray();
            var filters=selected.Select(r=>r.GetComponent<MeshFilter>()).ToHashSet();var protectedProps=all.Where(c=>!(c is Transform)&&!(c is MeshRenderer mr&&(selected.Contains(mr)||proxies.Contains(mr)))&&!(c is MeshFilter mf&&filters.Contains(mf))).ToDictionary(Id,Props);var protectedPoses=all.OfType<Transform>().Where(t=>!retire.Contains(t)).ToDictionary(Id,Pose);
            var retiredPoses=retire.ToDictionary(t=>t,t=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6"));string lighting=EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings"));
            foreach(var t in retire)t.gameObject.SetActive(false);foreach(var r in proxies)r.enabled=false;
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Puzzle02Access");
            root=new GameObject(RootName).transform;root.SetParent(env,false);placed.Clear();
            var earth=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");var floorMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_PlazaGround.mat");
            foreach(var r in selected)
            {
                var f=r.GetComponent<MeshFilter>();var source=f.sharedMesh;var uv=AssetDatabase.LoadAssetAtPath<Mesh>(Level01SurfacePresentation.Folder+"/UV_"+GlobalObjectId.GetGlobalObjectIdSlow(f.transform)+".asset");Check(uv!=null,"Existing UV presentation copy "+r.name);
                var a=source.vertices;var ai=source.triangles;var b=uv.vertices;var bi=uv.triangles;Check(ai.Length==bi.Length&&Enumerable.Range(0,ai.Length).All(i=>a[ai[i]].Equals(b[bi[i]])),"UV copy preserves original surface geometry "+r.name);f.sharedMesh=uv;r.sharedMaterial=r.transform.parent.name=="Terrain"?floorMat:earth;
            }
            int bankTris=Bank(earth),pathTris=Path();
            for(int i=0;i<5;i++)foreach(int side in new[]{-1,1})
            {
                float x=10.8f+i*1.7f,z=12+side*(3.6f+i%2*.6f);
                Put(i%2==0?"VEG_ArbustoTropical_03":"VEG_PlantaTropical_02","ConnectorUnderstory_"+side+"_"+i,x,z,.85f+i%3*.08f,i*61,0,true);
                Put("VEG_HelechoTropical_02","ConnectorFern_"+side+"_"+i,x+.45f,z+side*.6f,.8f,i*47);
                if(i%2==0)Put("ROC_RocaMusgo_02","ConnectorRock_"+side+"_"+i,x,z+side*1.3f,.85f,i*83,.2f);
            }
            foreach(var p in new[]{new Vector2(11.5f,5),new Vector2(15.2f,19.3f),new Vector2(18,4.5f),new Vector2(21.8f,.3f),new Vector2(26.8f,23),new Vector2(29.5f,.4f)})
            {
                int i=placed.Count;var tree=Put(i%2==0?"VEG_ArbolTropical_03":"VEG_ArbolTropical_04","AccessCanopy_"+i,p.x,p.y,.8f,i*37,0,true);
                Put("VEG_CeibaJoven_01","AccessMiddleCrown_"+i,p.x+.4f,p.y+.4f,.72f,i*61,0,true);
                Put("VEG_ArbustoTropical_03","AccessShrubLayer_"+i,p.x-.5f,p.y+.6f,1.05f,i*79,0,true);
                if(p.x<16){var l=Put("VEG_CortinaLianas_01","HangingLiana_"+i,p.x+.15f,p.y+.3f,.7f,i*11,0,true);var b=GoldenSliceArtPass.VisualBounds(l);var crown=GoldenSliceArtPass.VisualBounds(tree);l.transform.position+=Vector3.up*(crown.max.y-crown.size.y*.28f-b.max.y);}
            }
            foreach(var p in new[]{new Vector2(13.5f,6.7f),new Vector2(17.2f,18.2f),new Vector2(23.5f,1),new Vector2(25.5f,23.1f)})Put("VEG_RaizTropical_01","EmbeddedRoot_"+placed.Count,p.x,p.y,.55f,placed.Count*31,.1f);
            // Base enclosure only. Interactive guardians, inscriptions, pillars and reward anchors remain exposed and untouched.
            foreach(var p in new[]{new Vector2(21,1.3f),new Vector2(25,1.6f),new Vector2(22.5f,23.1f),new Vector2(29,23.3f)})
            {int i=placed.Count;Put("VEG_HelechoTropical_02","RoomEdgeFern_"+i,p.x,p.y,1,i*47);Put("ROC_RocaMusgo_02","RoomEdgeRock_"+i,p.x+.3f,p.y-.2f,.85f,i*57,.2f);}
            // Exact static mesh contact only for remaining accessible outlet solids. No vegetation or distant-bank colliders.
            var localColliders=new List<MeshCollider>();
            foreach(var t in all.OfType<Transform>().Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x>=4&&t.position.x<9.1f&&t.position.z>=5&&t.position.z<=19&&Hard(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject))))
            {
                var body=BodyBounds(t.gameObject);if(!body.HasValue)continue;
                var origin=new Vector3(t.position.x,Floor(t.position.x,12)+1,12);var target=body.Value.center;var delta=target-origin;
                bool alreadyGuarded=Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider is BoxCollider||h.collider.transform.parent?.name=="NaturalBoundaries");if(alreadyGuarded)continue;
                var lod=t.GetComponentInChildren<LODGroup>();var rs=lod!=null?lod.GetLODs()[0].renderers:t.GetComponentsInChildren<Renderer>();
                foreach(var r in rs){var f=r.GetComponent<MeshFilter>();if(f==null||f.GetComponent<Collider>()!=null)continue;var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.convex=false;localColliders.Add(c);}
            }
            all=Level01EnvironmentArtPass.All(scene);var ids=all.ToDictionary(Id);
            Check(protectedProps.All(k=>ids.TryGetValue(k.Key,out var c)&&Props(c)==k.Value),"Every original gameplay component and collider preserved");Check(protectedPoses.All(k=>ids.TryGetValue(k.Key,out var c)&&Pose((Transform)c)==k.Value),"Every original pose/activation preserved except retired outlet decoration");Check(retiredPoses.All(k=>!k.Key.gameObject.activeSelf&&k.Value==k.Key.localPosition.ToString("F6")+k.Key.localRotation.ToString("F6")+k.Key.localScale.ToString("F6")),"Retired objects retained with unchanged poses");Check(lighting==EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings")),"Lighting preserved");
            Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new collider");Check(all.All(c=>c!=null),"No Missing Script");
            int floating=placed.Count(g=>!g.name.StartsWith("HangingLiana")&&GoldenSliceArtPass.VisualBounds(g).min.y-Surface(g.transform.position.x,g.transform.position.z)>.03f);Check(floating==0,"Grounded new base bounds");
            foreach(var t in all.OfType<Transform>().Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x>=4&&t.position.x<=30&&t.position.z>=3&&t.position.z<=24&&Hard(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)))){var b=BodyBounds(t.gameObject);Check(!b.HasValue||!b.Value.Intersects(Clear),"3.5m body-height art clearance: "+t.name);}
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Scene saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{passed=true,placements=placed.Count,retired=retire.Length,retiredPaths=retire.Select(t=>Level01EnvironmentInspection.PathOf(t)).ToArray(),changedRenderers=selected.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),originalColliders=all.OfType<Collider>().Count(c=>c.enabled)-localColliders.Count,newColliders=localColliders.Count,localColliderPaths=localColliders.Select(c=>Level01EnvironmentInspection.PathOf(c.transform)+" [MeshCollider, non-convex, static visual mesh]").ToArray(),hiddenProxies=proxies.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),floating=floating,clearWidth=3.5f,bankTriangles=bankTris,pathTriangles=pathTris,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"Original colliders/runtime/poses unchanged; only decorative outlet instances retired","Only selected connector/base floor and boundary renderers reskinned with geometry-identical UV copies; local vegetation proxies hidden","3.5m central art clearance at body height; original connector 4m","No colliders on new art layer; only exact mesh contact on remaining accessible outlet solids; no Missing Script","Grounded base bounds; visual review still required","Door/secret/checkpoint references valid","Saved Level01 only"}},true));Debug.Log("PUZZLE02_ACCESS_SAVED "+placed.Count);
        }
    }
}
