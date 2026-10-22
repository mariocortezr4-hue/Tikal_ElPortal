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
    public static class Zone03EnvironmentArtPass
    {
        public const string Evidence="../_Zone03Environment_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zone03";
        const string Root="ENV_L01_Zone03_JungleBase";
        static Transform root;static MeshCollider snap;static readonly List<GameObject> placed=new List<GameObject>();
        [Serializable] class Report {public int banks,rocks,ruins,midgroundTrees,backgroundTrees,vegetation,placements,newColliders,terrainTriangles;public string[] checks,assets,hiddenLocalProxies;}
        static void Check(bool ok,string s){if(!ok)throw new InvalidOperationException(s);}
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static Zone03EnvironmentArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/Zone03SeamFinish.request"))
            {
                File.Delete("Temp/Zone03SeamFinish.request");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/MESH_Zone03_ConnectorVisualShoulders.asset");var v=mesh.vertices;for(int i=0;i<v.Length;i++){var p=v[i];if(p.x<26.5f)p.x=24.5f;else if(p.x>31.5f)p.x=33.5f;v[i]=p;}Check(v.Length==160,"One-shot final seam underlay");var points=v.ToList();var uv=mesh.uv.ToList();var indices=mesh.triangles.ToList();foreach(var range in new[]{new Vector2(36.45f,37.03f),new Vector2(40.97f,41.55f)})for(int j=0;j<18;j++)
                {float x=12+j*.5f;int n=points.Count;foreach(var p in new[]{new Vector2(x,range.x),new Vector2(x,range.y),new Vector2(x+.5f,range.x),new Vector2(x+.5f,range.y)}){points.Add(new Vector3(p.x,.3f+(21-p.x)*.13f-.012f,p.y));uv.Add(p*.28f);}indices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}
                mesh.vertices=points.ToArray();mesh.uv=uv.ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();Debug.Log("ZONE03_CONNECTOR_VOID_UNDERLAY_SAVED");return;
            }
            if(File.Exists("Temp/Zone03ProxyFinish.request"))
            {
                File.Delete("Temp/Zone03ProxyFinish.request");var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");
                foreach(var n in new[]{"Talud_65","Talud_67","Talud_94","Talud_97"})env.Find("NaturalBoundaries/"+n).GetComponent<Renderer>().enabled=false;
                foreach(var r in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>().Where(r=>r.bounds.center.x>=12&&r.bounds.center.x<=51&&r.bounds.center.z>=29.5f&&r.bounds.center.z<=61))r.enabled=false;
                EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Zone03 western proxy replacement saved");Debug.Log("ZONE03_WESTERN_LOCAL_PROXIES_REPLACED");return;
            }
            if(File.Exists("Temp/Zone03Clearance.request")){File.Delete("Temp/Zone03Clearance.request");Clearance();return;}
            if(File.Exists("Temp/Zone03Finish.request"))
            {
                File.Delete("Temp/Zone03Finish.request");Finish();return;
            }
            if(File.Exists("Temp/Zone03VisualInspect.request"))
            {
                File.Delete("Temp/Zone03VisualInspect.request");var scene=EditorSceneManager.GetActiveScene();var env=scene.GetRootGameObjects().Single().transform.Find("Environment");var area=new Bounds(new Vector3(29,5,37),new Vector3(28,18,25));var lines=new List<string>();
                foreach(var group in new[]{env.Find("NaturalBoundaries"),env.Find("Vegetation_Blockout")})foreach(var r in group.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.Intersects(area)))lines.Add(Level01EnvironmentInspection.PathOf(r.transform)+" bounds="+r.bounds.ToString("F2"));
                foreach(var t in env.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.name.Contains("VEG_")&&t.position.z>=22&&t.position.z<=36))lines.Add("TREE "+Level01EnvironmentInspection.PathOf(t)+" pos="+t.position.ToString("F2")+" bounds="+GoldenSliceArtPass.VisualBounds(t.gameObject).ToString("F2"));
                File.WriteAllLines(Evidence+"/VisualSupports.txt",lines);return;
            }
            if(File.Exists("Temp/Zone03Apply.request"))
            {File.Delete("Temp/Zone03Apply.request");try{Apply();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(File.Exists("Temp/Zone03Survey.request"))
            {
                File.Delete("Temp/Zone03Survey.request");Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Unsaved current scene; preserve before switching");if(scene.path!=Level01EnvironmentInspection.ScenePath)scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);var all=Level01EnvironmentArtPass.All(scene);var lines=new List<string>{"Scene="+scene.path+" dirty="+scene.isDirty};
                foreach(var c in all.OfType<MeshCollider>().Where(c=>c.transform.parent!=null&&c.transform.parent.name=="Terrain"))lines.Add("WALKABLE "+Level01EnvironmentInspection.PathOf(c.transform)+" bounds="+c.bounds.ToString("F3"));
                foreach(var c in all.OfType<MonoBehaviour>().Where(c=>c!=null&&c.transform.position.z>28&&c.transform.position.z<50&&c.transform.position.x>15))lines.Add("LOCAL_COMPONENT "+c.GetType().FullName+" "+Level01EnvironmentInspection.PathOf(c.transform)+" pos="+c.transform.position.ToString("F3"));
                foreach(var r in all.OfType<Renderer>().Where(r=>r.enabled&&r.bounds.center.x>15&&r.bounds.center.x<48&&r.bounds.center.z>28&&r.bounds.center.z<52&&r.GetComponentInParent<LODGroup>()==null))lines.Add("LOCAL_RENDERER "+Level01EnvironmentInspection.PathOf(r.transform)+" bounds="+r.bounds.ToString("F3"));
                foreach(var c in all.OfType<BoxCollider>().Where(c=>c.bounds.center.x>15&&c.bounds.center.x<40&&c.bounds.center.z>28&&c.bounds.center.z<50))lines.Add("LOCAL_BOUNDARY "+Level01EnvironmentInspection.PathOf(c.transform)+" bounds="+c.bounds.ToString("F3"));
                File.WriteAllLines(Evidence+"/LocalSurvey.txt",lines);Debug.Log("ZONE03_LOCAL_SURVEY_READY");
            }
        }
        static bool Bank(float x,float z)=>x>=12&&x<=51&&z>=29.5f&&z<=61&&!(x>21&&x<37&&z>31&&z<47)&&!(x<21&&z>36.5f&&z<41.5f)&&!(z<31&&x>26.5f&&x<31.5f);
        static void Clearance()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Zone03 scene");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);Check(root.Find("ConnectorVisualShoulders")==null,"Do not reapply clearance");
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&(t.name.Contains("Arbol")||t.name.Contains("Ceiba")||t.name.Contains("Palmera"))))
            {var p=t.position;if(p.x<21)p.x=Mathf.Min(p.x,15.5f);else if(p.x>37)p.x=Mathf.Max(p.x,42.5f);else if(p.z>47)p.z=Mathf.Max(p.z,52.5f);else if(p.z<31){p.x=p.x<29?15.5f:42.5f;p.z=32;}t.position=p;var b=GoldenSliceArtPass.VisualBounds(t.gameObject);t.position+=Vector3.up*(Mathf.Min(.3f,b.min.y)-b.min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            var giant=env.Find("ENV_L01_Zona02_MassComposition/Z02_Ruins_VEG_CeibaTropical_01_003");Check(giant!=null&&Mathf.Abs(giant.position.x-34.8f)<.02f,"One identified giant root crossing connector");var gp=giant.position;gp.x=45.5f;giant.position=gp;PrefabUtility.RecordPrefabInstancePropertyModifications(giant);
            // Cover two half-metre void strips beside the existing 4m physical connector. No physics change.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();foreach(var range in new[]{new Vector2(26.45f,27.03f),new Vector2(30.97f,31.55f)})for(int j=0;j<20;j++)
            {float a=21+j*.5f,b=a+.5f;int n=vertices.Count;foreach(var p in new[]{new Vector2(range.x,a),new Vector2(range.x,b),new Vector2(range.y,a),new Vector2(range.y,b)}){vertices.Add(new Vector3(p.x,1.2f-(p.y-21)*.09f-.012f,p.y));uv.Add(p*.28f);}triangles.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}
            var m=new Mesh{name="MESH_Zone03_ConnectorVisualShoulders"};m.vertices=vertices.ToArray();m.uv=uv.ToArray();m.triangles=triangles.ToArray();m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");var g=new GameObject("ConnectorVisualShoulders");g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Clearance saved");AssetDatabase.SaveAssets();var r=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));r.checks=r.checks.Concat(new[]{"New tree trunks moved at least 5.5m beyond room edges so buttress roots cannot occupy the central floor","One pre-existing giant ceiba shifted east from X34.8 to45.5 at the same Y/Z, scale and rotation: obvious connector camera/root obstruction fix only","Two narrow visual connector shoulders cover exposed void; original 4m physical surface and colliders unchanged"}).ToArray();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(r,true));Debug.Log("ZONE03_ROOT_CLEARANCE_SAVED");
        }
        static void Finish()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved local scene");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);var old=Level01EnvironmentArtPass.All(scene).Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);
            int newTrees=0,connectorBugTrees=0;var moved=new HashSet<Transform>();
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&(t.name.Contains("Arbol")||t.name.Contains("Ceiba")||t.name.Contains("Palmera"))))
            {var b=GoldenSliceArtPass.VisualBounds(t.gameObject);if(b.min.y>.3f){t.position+=Vector3.up*(.3f-b.min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(t);newTrees++;}}
            // Two existing elevated root skirts visibly cross the shared connector. This is a bug fix,
            // not another Zona02 art pass: keep X/Z, yaw, scale, meshes and materials unchanged.
            var previous=env.Find("ENV_L01_Zona02_DenseShell");foreach(var t in previous.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.name.Contains("ArbolTropical_04")&&((Mathf.Abs(t.position.x-23.5f)<.01f&&Mathf.Abs(t.position.z-26)<.01f)||(Mathf.Abs(t.position.x-35)<.01f&&Mathf.Abs(t.position.z-26.5f)<.01f))))
            {var b=GoldenSliceArtPass.VisualBounds(t.gameObject);t.position+=Vector3.up*(1.2f-b.min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(t);connectorBugTrees++;moved.Add(t);}
            foreach(var n in new[]{"Talud_41","Talud_43","Talud_88","Talud_91"})env.Find("NaturalBoundaries/"+n).GetComponent<Renderer>().enabled=false;
            Check(connectorBugTrees==2,"Only two identified connector root bugs");Check(old.All(k=>Props(k.Key)==k.Value),"All gameplay scripts and colliders unchanged");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"Zero new colliders");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Local support corrections saved");
            var r=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));r.checks=r.checks.Concat(new[]{"Lowered "+newTrees+" new tree root skirts into bank soil to remove exposed undersides","Bug-only exception to frozen Zona02: lowered exactly two existing elevated root skirts across connector; X/Z, yaw, scale and visuals retained","Four south approach bank proxy renderers hidden; physical connector and original colliders unchanged"}).ToArray();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(r,true));Debug.Log("ZONE03_SUPPORT_CORRECTIONS_SAVED");
        }
        static float Height(float x,float z)
        {
            float dx=Mathf.Max(21-x,x-37,0),dz=Mathf.Max(31-z,z-47,0),d=Mathf.Sqrt(dx*dx+dz*dz);
            if(x<21)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(z-39)-2.5f));if(z<31)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(x-29)-2.5f));
            var centres=new[]{new Vector4(15,32,5,4),new Vector4(15,46,5,6),new Vector4(22,54,7,7),new Vector4(33,55,8,6),new Vector4(44,49,7,8),new Vector4(44,37,7,7),new Vector4(39,30,5,4)};
            float h=2.1f;for(int i=0;i<centres.Length;i++){var c=centres[i];float r=Mathf.Pow((x-c.x)/c.z,2)+Mathf.Pow((z-c.y)/c.w,2);h=Mathf.Max(h,(4.2f+i%3*.8f)*Mathf.Exp(-r*.65f));}
            return .3f+Mathf.SmoothStep(0,1,d/4.8f)*(h+.2f*Mathf.Sin(x*.53f+z*.41f)+.12f*Mathf.Cos(x*.91f-z*.36f));
        }
        static Mesh Terrain()
        {
            const int nx=79,nz=64;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=12+ix*.5f,z=29.5f+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Bank(12+(ix+.5f)*.5f,29.5f+(iz+.5f)*.5f)){int a=iz*nx+ix;tri.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_Zone03_OrganicBanks",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tri.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static float Ground(float x,float z)
        {Check(snap.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60),"Ground support "+x+"/"+z);return hit.point.y;}
        static GameObject Put(string asset,float x,float z,Vector3 scale,float yaw,float burial=.12f,int low=-1,bool shadow=false,float structuralY=float.NaN)
        {
            bool structural=!float.IsNaN(structuralY);Check(structural||Bank(x,z),"Within local non-playable bank "+asset);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Existing optimized prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Z03_"+asset+"_"+placed.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,structural?structuralY:Ground(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);
            if(!structural&&(asset.StartsWith("ROC_")||asset.StartsWith("ARQ_")))
            {
                var delta=Vector3.zero;if(x<21)delta.x=-Mathf.Max(0,b.max.x-20.8f);else if(x>37)delta.x=Mathf.Max(0,37.2f-b.min.x);else if(z>47)delta.z=Mathf.Max(0,47.2f-b.min.z);else if(z<31)delta.z=-Mathf.Max(0,b.max.z-30.8f);
                g.transform.position+=delta;x=g.transform.position.x;z=g.transform.position.z;b=GoldenSliceArtPass.VisualBounds(g);
            }
            float h=structural?structuralY:Ground(x,z);
            if(!structural&&(asset.StartsWith("ROC_")||asset.StartsWith("ARQ_")||asset.Contains("Raiz")))
            {foreach(var p in new[]{new Vector2(b.min.x,b.min.z),new Vector2(b.min.x,b.max.z),new Vector2(b.max.x,b.min.z),new Vector2(b.max.x,b.max.z)})if(Bank(p.x,p.y))h=Mathf.Min(h,Ground(p.x,p.y));}
            g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*burial);
            foreach(var lod in g.GetComponentsInChildren<LODGroup>())if(low>=0){var level=lod.GetLODs()[Math.Min(low,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.004f,level.renderers)});var used=level.renderers.ToHashSet();foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static GameObject Put(string a,float x,float z,float s,float yaw,float burial=.06f,int low=-1,bool shadows=false)=>Put(a,x,z,Vector3.one*s,yaw,burial,low,shadows);
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Real Level01 Edit Mode");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current scene");Directory.CreateDirectory(Evidence);File.Copy(scene.path,Evidence+"/Before.unity",false);
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(Root)==null,"No blind reapply");
            var floor=env.Find("Terrain/Surface_Zone03_EchoChamber").GetComponent<MeshCollider>();Check(Vector3.Distance(floor.bounds.center,new Vector3(29,.3f,39))<.01f&&floor.bounds.size==new Vector3(16,0,16),"Surveyed real 16x16m Echo Chamber surface");
            var all=Level01EnvironmentArtPass.All(scene);var originals=all.Where(c=>c is MonoBehaviour||c is Collider||c is MeshFilter||c is Renderer).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var hidden=new HashSet<Renderer>();
            // Replace only this chamber's visual blockout; functional roof/boundary colliders remain intact.
            foreach(var path in new[]{"NaturalBoundaries/Talud_45","NaturalBoundaries/Talud_48","NaturalBoundaries/Talud_50","NaturalBoundaries/Talud_52","NaturalBoundaries/EchoRoofRockCover","Zone03_EchoChamber/Ceiling","Zone03_EchoChamber/AlcoveDivider","Ruins_Blockout/EchoEntranceLeft","Ruins_Blockout/EchoEntranceRight","Ruins_Blockout/EchoEntranceLintel"})
            {var r=env.Find(path).GetComponent<Renderer>();hidden.Add(r);r.enabled=false;}
            foreach(var r in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.center.x>=18&&r.bounds.center.x<=41&&r.bounds.center.z>=31&&r.bounds.center.z<=52)){hidden.Add(r);r.enabled=false;}
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zone03");root=new GameObject(Root).transform;root.SetParent(env,false);placed.Clear();
            // A/B: terrain first, smoothly descending to both route openings rather than cutting a raised bank.
            var mesh=Terrain();var earth=new GameObject("TerrainMass_39x31m_SevenBroadBanks");earth.transform.SetParent(root,false);earth.AddComponent<MeshFilter>().sharedMesh=mesh;earth.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");snap=earth.AddComponent<MeshCollider>();snap.sharedMesh=mesh;Physics.SyncTransforms();
            // C: seven clusters of three rocks and nine archaeological fragments, partly embedded.
            var clusters=new[]{new Vector2(18.4f,33.2f),new Vector2(18.1f,44.5f),new Vector2(23.5f,50),new Vector2(32.7f,50.5f),new Vector2(40.2f,44.8f),new Vector2(40.5f,36),new Vector2(41.5f,33)};
            for(int i=0;i<clusters.Length;i++)for(int j=0;j<3;j++){var p=clusters[i]+new Vector2((j-1)*1.2f,Mathf.Sin(i*3+j*2)*.65f);Put(j==1?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02",p.x,p.y,1.25f+j*.22f,i*71+j*113,.2f,1);}
            for(int i=0;i<clusters.Length;i++){var p=clusters[i];Put(i%3==0?"ARQ_MuroMaya_01":i%3==1?"ARQ_ColumnaDerrumbada_01":"ARQ_BloqueMaya_02",p.x,p.y+.85f,1.05f+i%3*.12f,i*73,.2f,1);if(i<2)Put("ARQ_BloqueMaya_02",p.x+.7f,p.y-.5f,1.3f,i*113,.2f,1);}
            // C2: local architectural cladding sits against the retained blockout colliders.
            foreach(float x in new[]{26.7f,31.3f})Put("ARQ_ColumnaMaya_01",x,30.6f,new Vector3(.9f,1.38f,.9f),x*53,0,-1,true,.3f);
            for(int i=0;i<5;i++)Put("ARQ_BloqueMaya_02",26.6f+i*1.2f,30.6f,new Vector3(1,.95f,1.1f),0,0,-1,true,4.1f);
            foreach(float y in new[]{.3f,2f})Put("ARQ_MuroMaya_01",34,40,new Vector3(1,1,.45f),0,0,-1,false,y);
            // Eight partial roof/ruin fragments frame canopy openings; no new ceiling physics.
            foreach(var p in new[]{new Vector3(23,31.35f,0),new Vector3(35,31.35f,0),new Vector3(23,46.65f,0),new Vector3(27,46.65f,0),new Vector3(31,46.65f,0),new Vector3(35,46.65f,0),new Vector3(36.65f,35,90),new Vector3(36.65f,43,90)})Put("ARQ_MuroMaya_01",p.x,p.y,new Vector3(.9f,1.5f,.9f),p.z,.08f,1,false,.3f);
            // D: twelve midground trees and eighteen low-detail background trees, no giant root skirts.
            var trees=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_ArbolTropical_04","VEG_CeibaJoven_01"};
            var mid=new[]{new Vector2(19,31.5f),new Vector2(18.6f,35),new Vector2(18.5f,43),new Vector2(20,47),new Vector2(24,49.2f),new Vector2(30,49.8f),new Vector2(36,49.5f),new Vector2(39,46),new Vector2(39.5f,40),new Vector2(39,33.5f),new Vector2(35,30),new Vector2(23,30)};
            for(int i=0;i<mid.Length;i++)Put(trees[i%4],mid[i].x,mid[i].y,.92f+(i*7%10)*.023f,i*137,.025f,-1,i%2==0);
            var bg=new[]{new Vector2(13.5f,31.7f),new Vector2(14.5f,34.1f),new Vector2(13.8f,44.2f),new Vector2(15.6f,49),new Vector2(19,54),new Vector2(23,58),new Vector2(27,54.5f),new Vector2(32,58),new Vector2(36,54),new Vector2(40,58),new Vector2(46,54),new Vector2(44,49),new Vector2(48,44),new Vector2(44,39),new Vector2(48,34),new Vector2(43,31),new Vector2(39,30),new Vector2(23.5f,30)};
            for(int i=0;i<bg.Length;i++)Put(i==1||i==12?"VEG_PalmeraTropical_02":trees[(i+2)%4],bg[i].x,bg[i].y,.92f+(i*13%10)*.022f,i*137+31,.03f,i%3==0?2:1);
            // E: understory follows the large masses; the playable floor remains clean.
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_RaizTropical_01","VEG_HojasSecasMonton_01"};
            for(int i=0;i<56;i++){var c=clusters[i%clusters.Length];float a=i*2.39996f,r=.6f+i%4*.35f;float x=c.x+Mathf.Cos(a)*r,z=c.y+Mathf.Sin(a)*r;if(!Bank(x,z))continue;Put(plants[i%5],x,z,.8f+(i%5)*.09f,i*137,i%5==3?.15f:.04f,1);}
            int plantsCount=placed.Count(g=>g.name.Contains("VEG_")&&!g.name.Contains("Arbol")&&!g.name.Contains("Ceiba")&&!g.name.Contains("Palmera"));
            UnityEngine.Object.DestroyImmediate(snap);snap=null;Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new colliders");Check(originals.All(k=>hidden.Contains(k.Key as Renderer)||k.Key!=null&&Props(k.Key)==k.Value),"Original components/colliders/materials retained; only scoped visual proxies hidden");Check(poses.All(k=>Pose(k.Key)==k.Value),"All original transforms and activation unchanged");Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Zone03 saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{banks=7,rocks=21,ruins=26,midgroundTrees=12,backgroundTrees=18,vegetation=plantsCount,placements=placed.Count,newColliders=0,terrainTriangles=mesh.triangles.Length/3,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),hiddenLocalProxies=hidden.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),checks=new[]{"Live survey confirms EchoSequencePuzzle chamber, 16x16m floor, south approach and west exit","Visual terrain extends 9m west and 14m east/north; connector channels taper to original ground","All original collider/MonoBehaviour/mesh/material assignments and all poses retained","Only chamber-local terrain, roof and vegetation proxy renderers hidden; previous art layers frozen","Terrain first, relief, rock/ruin mass, trees, understory; optimized existing prefabs/materials reused","New assets raycast to terrain or attached as architectural cladding to existing solids; temporary snap collider removed","No new Missing Scripts; local native FPS/TPS review pending"}},true));Debug.Log("ZONE03_ENVIRONMENT_SAVED "+placed.Count);
        }
    }
}
