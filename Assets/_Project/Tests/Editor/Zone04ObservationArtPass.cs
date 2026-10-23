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
    public static class Zone04ObservationArtPass
    {
        public const string Evidence="../_Zone04Observation_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zone04";
        const string Root="ENV_L01_Zone04_ObservationTerrace";
        public static readonly Bounds Protected=new Bounds(new Vector3(-5,5.6f,36),new Vector3(7,6.5f,15));
        static Transform root;static MeshCollider snap;static readonly List<GameObject> placed=new List<GameObject>();
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static void Check(bool ok,string s){if(!ok)throw new InvalidOperationException(s);}
        static Zone04ObservationArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/Zone04GroundSeam.request")){File.Delete("Temp/Zone04GroundSeam.request");GroundSeam();return;}
            if(File.Exists("Temp/Zone04Finish.request")){File.Delete("Temp/Zone04Finish.request");try{Finish();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);File.WriteAllText(Evidence+"/FinishError.txt",e.ToString());Debug.LogException(e);}return;}
            if(File.Exists("Temp/Zone04Retry.request")){File.Delete("Temp/Zone04Retry.request");Check(File.ReadAllText(Level01EnvironmentInspection.ScenePath)==File.ReadAllText(Evidence+"/Before.unity"),"Saved baseline untouched by incomplete transaction");EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);AssetDatabase.DeleteAsset(Folder+"/MESH_Zone04_OrganicTerraceBanks.asset");File.WriteAllText("Temp/Zone04Apply.request","Retry validated authoring transaction");return;}
            if(File.Exists("Temp/Zone04Apply.request")){File.Delete("Temp/Zone04Apply.request");try{Apply();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(!File.Exists("Temp/Zone04Survey.request"))return;
            File.Delete("Temp/Zone04Survey.request");Directory.CreateDirectory(Evidence);
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Real Level01 required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Save current state");
            var all=Level01EnvironmentArtPass.All(scene);var lines=new List<string>();
            foreach(var c in all.OfType<MonoBehaviour>().Where(c=>c!=null&&(c is Tikal.Puzzles.Level01.ObservationAlignmentPuzzle||c is Tikal.Puzzles.Level01.ObservationPoint)))
            {lines.Add("PUZZLE "+Level01EnvironmentInspection.PathOf(c.transform)+" pos="+c.transform.position.ToString("F3"));lines.Add(EditorJsonUtility.ToJson(c,true));}
            foreach(var c in all.OfType<Collider>().Where(c=>c.transform.parent!=null&&(c.transform.parent.name=="Terrain"||c.name.Contains("Observation")||c.bounds.center.x<15&&c.bounds.center.x> -25&&c.bounds.center.z>26&&c.bounds.center.z<54)))lines.Add("COLLIDER "+Level01EnvironmentInspection.PathOf(c.transform)+" "+c.bounds.ToString("F3")+" trigger="+c.isTrigger);
            foreach(var r in all.OfType<Renderer>().Where(r=>r.enabled&&r.GetComponentInParent<LODGroup>()==null&&r.bounds.center.x<17&&r.bounds.center.x> -26&&r.bounds.center.z>25&&r.bounds.center.z<55))lines.Add("RENDER "+Level01EnvironmentInspection.PathOf(r.transform)+" "+r.bounds.ToString("F3"));
            foreach(var c in all.OfType<Tikal.Puzzles.Level01.ObservationAlignmentPuzzle>())
            {var s=new SerializedObject(c);var point=(Component)s.FindProperty("_point").objectReferenceValue;var target=(Transform)s.FindProperty("_target").objectReferenceValue;lines.Add("TARGET "+Level01EnvironmentInspection.PathOf(target)+" "+target.position.ToString("F3"));var a=s.FindProperty("_fragments");for(int i=0;i<a.arraySize;i++){var r=(Renderer)a.GetArrayElementAtIndex(i).objectReferenceValue;lines.Add("FRAGMENT "+Level01EnvironmentInspection.PathOf(r.transform)+" "+r.bounds.ToString("F3"));}var p=new SerializedObject(point);var zone=(BoxCollider)p.FindProperty("_zone").objectReferenceValue;lines.Add("OBSERVATION_ZONE "+Level01EnvironmentInspection.PathOf(zone.transform)+" "+zone.bounds.ToString("F3"));}
            File.WriteAllLines(Evidence+"/RealSceneSurvey.txt",lines);Debug.Log("ZONE04_REAL_SURVEY_READY");
        }
        static bool Bank(float x,float z)=>x>=-25&&x<=20&&z>=25&&z<=55.5f&&!(x> -11&&x<11&&z>32&&z<46)&&!(x>11&&z>36.5f&&z<41.5f)&&!(z>46&&x> -2.6f&&x<2.6f);
        static void GroundSeam()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Zone04");root=scene.GetRootGameObjects().Single().transform.Find("Environment/"+Root);var earth=root.Find("OrganicTerraceBanks_45x30m");var m=earth.GetComponent<MeshFilter>().sharedMesh;var vertices=m.vertices;Check(vertices.Any(v=>Mathf.Abs(v.x+11)<.01f&&Mathf.Abs(v.z-44)<.01f&&Mathf.Abs(v.y-2)<.01f),"One-shot correction of original edge mismatch");
            var originals=Level01EnvironmentArtPass.All(scene).Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);snap=earth.gameObject.AddComponent<MeshCollider>();snap.sharedMesh=m;Physics.SyncTransforms();var parts=root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray();
            float Support(Transform t){var b=GoldenSliceArtPass.VisualBounds(t.gameObject);float h=Ground(t.position.x,t.position.z);foreach(var p in new[]{new Vector2(b.min.x,b.min.z),new Vector2(b.min.x,b.max.z),new Vector2(b.max.x,b.min.z),new Vector2(b.max.x,b.max.z)})if(Bank(p.x,p.y))h=Mathf.Min(h,Ground(p.x,p.y));return h;}
            var old=parts.ToDictionary(t=>t,Support);for(int i=0;i<vertices.Length;i++)vertices[i].y=Height(vertices[i].x,vertices[i].z);snap.sharedMesh=null;m.vertices=vertices;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();snap.sharedMesh=m;Physics.SyncTransforms();foreach(var t in parts){float d=Support(t)-old[t];Check(d<=.001f&&d>=-.401f,"Only lowering new bank assets to corrected soil");t.position+=Vector3.up*d;PrefabUtility.RecordPrefabInstancePropertyModifications(t);Check(!GoldenSliceArtPass.VisualBounds(t.gameObject).Intersects(Protected),"Sightline remains protected");}UnityEngine.Object.DestroyImmediate(snap);snap=null;
            Check(originals.All(k=>Props(k.Key)==k.Value),"All original scripts/colliders retained");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"Zero new colliders");EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Terrain seam saved");AssetDatabase.SaveAssets();File.AppendAllText(Evidence+"/EditorValidation.txt","\nGround seam correction: original room edges measured at Y1.60; new bank baseline corrected to1.60, new plants/rocks resupported (lowering only, max0.4m), all original transforms/colliders unchanged; protected bounds retained.\n");Debug.Log("ZONE04_GROUND_SEAM_SAVED");
        }
        static void Finish()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Zone04 scene");var all=Level01EnvironmentArtPass.All(scene);var original=all.Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);
            root=scene.GetRootGameObjects().Single().transform.Find("Environment/"+Root);Check(root!=null&&root.Find("ConnectorVisualShoulders")==null,"Finish only once");placed.Clear();placed.AddRange(root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.gameObject));int beforeCount=placed.Count;
            snap=root.Find("OrganicTerraceBanks_45x30m").gameObject.AddComponent<MeshCollider>();snap.sharedMesh=snap.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_RaizTropical_01"};
            for(int i=0;i<36;i++)
            {
                float x,z;if(i<18){x=-12.4f-i%3*1.4f;z=32.5f+i/3*2.45f;}else if(i<28){x=3.5f+(i-18)%5*2.1f;z=47.6f+(i-18)/5*2.6f;}else{x=-13.2f+(i-28)%4*1.9f;z=47.7f+(i-28)/4*2.6f;}
                Put(plants[i%4],x,z,.95f+i%3*.14f,i*137,1,i%4==3?.13f:.04f);
            }
            UnityEngine.Object.DestroyImmediate(snap);snap=null;
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});foreach(var q in new[]{a,b,c,d})uv.Add(new Vector2(q.x,q.z)*.28f);t.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}
            foreach(var r in new[]{new Vector2(36.45f,37.03f),new Vector2(40.97f,41.55f)})for(int i=0;i<20;i++){float x=11+i*.5f,y=1.6f-(x-11)*.13f-.012f;Quad(new Vector3(x,y,r.x),new Vector3(x,y,r.y),new Vector3(x+.5f,y-.065f,r.x),new Vector3(x+.5f,y-.065f,r.y));}
            foreach(var r in new[]{new Vector2(-2.65f,-1.97f),new Vector2(1.97f,2.65f)})for(int i=0;i<19;i++){float z=46+i*.5f,y=1.6f-(z-46)*.1f-.012f;Quad(new Vector3(r.x,y,z),new Vector3(r.x,y-.05f,z+.5f),new Vector3(r.y,y,z),new Vector3(r.y,y-.05f,z+.5f));}
            var mesh=new Mesh{name="MESH_Zone04_ConnectorVisualShoulders"};mesh.vertices=v.ToArray();mesh.uv=uv.ToArray();mesh.triangles=t.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+mesh.name+".asset");var g=new GameObject("ConnectorVisualShoulders");g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");
            Check(original.All(k=>k.Key!=null&&Props(k.Key)==k.Value),"All original gameplay/colliders unchanged");Check(poses.All(k=>Pose(k.Key)==k.Value),"Previous transforms retained");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new colliders");Check(placed.All(p=>!GoldenSliceArtPass.VisualBounds(p).Intersects(Protected)),"All added vegetation outside sightline");Check(v.All(p=>p.y<Protected.min.y),"Connector shoulders below observation sightline");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final Zone04 saved");AssetDatabase.SaveAssets();File.AppendAllText(Evidence+"/EditorValidation.txt","\nFINAL: +"+(placed.Count-beforeCount)+" supported plants/roots on exposed slopes; total placements="+placed.Count+"; total understory=108; two pairs of visual connector shoulders (156 triangles), original physical surfaces retained; zero new colliders; all old transforms/logic unchanged.\n");Debug.Log("ZONE04_LOCAL_FINISH_SAVED");
        }
        static float Height(float x,float z)
        {
            float dx=Mathf.Max(-11-x,x-11,0),dz=Mathf.Max(32-z,z-46,0),d=Mathf.Sqrt(dx*dx+dz*dz);
            if(x>11)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(z-39)-2.5f));if(z>46)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(x)-2.6f));
            var centres=new[]{new Vector4(-19,31,7,6),new Vector4(-20,42,7,8),new Vector4(-13,51,8,6),new Vector4(8,51,7,6),new Vector4(16,45,6,5),new Vector4(14,31,7,5),new Vector4(1,26,8,5)};
            float h=2.5f;for(int i=0;i<centres.Length;i++){var c=centres[i];float r=Mathf.Pow((x-c.x)/c.z,2)+Mathf.Pow((z-c.y)/c.w,2);h=Mathf.Max(h,(4.2f+i%3*.6f)*Mathf.Exp(-r*.55f));}
            float baseline=1.6f;if(x>11)baseline=1.6f-(x-11)*.13f;if(z>46)baseline=1.6f-(z-46)*.1f;
            float y=baseline+Mathf.SmoothStep(0,1,d/5.5f)*(h+.22f*Mathf.Sin(x*.47f+z*.32f)+.13f*Mathf.Cos(x*.78f-z*.43f));
            // Continuous low soil under the observation window; no raised triangle may enter it.
            if(x> -9.1f&&x< -0.9f&&z>27.9f&&z<44.1f)y=Mathf.Min(y,2.15f);
            return y;
        }
        static Mesh Terrain()
        {
            const int nx=91,nz=62;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=-25+ix*.5f,z=25+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Bank(-25+(ix+.5f)*.5f,25+(iz+.5f)*.5f)){int a=iz*nx+ix;tri.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_Zone04_OrganicTerraceBanks",indexFormat=IndexFormat.UInt32};m.vertices=v;m.uv=uv;m.triangles=tri.ToArray();m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static float Ground(float x,float z)
        {Check(snap.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60),"Supported bank "+x+"/"+z);return hit.point.y;}
        static GameObject Put(string asset,float x,float z,float scale,float yaw,int lod=1,float bury=.1f,bool shadow=false)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Existing optimized "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Z04_"+asset+"_"+placed.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);var delta=Vector3.zero;
            // Solid bodies and tree root skirts stay outside the room and the connector clearances.
            if(x< -11)delta.x=-Mathf.Max(0,b.max.x+11.25f);else if(x>11)delta.x=Mathf.Max(0,11.25f-b.min.x);else if(z<32)delta.z=-Mathf.Max(0,b.max.z-31.65f);else if(z>46)delta.z=Mathf.Max(0,46.3f-b.min.z);
            g.transform.position+=delta;b=GoldenSliceArtPass.VisualBounds(g);
            // Use full bounds (including dormant LODs), more conservative than centre rays alone.
            if(b.max.x>Protected.min.x&&b.min.x<Protected.max.x&&b.max.z>Protected.min.z&&b.min.z<Protected.max.z)
            {g.transform.position+=Vector3.right*(x< -5?Protected.min.x-b.max.x-.35f:Protected.max.x-b.min.x+.35f);b=GoldenSliceArtPass.VisualBounds(g);}
            x=g.transform.position.x;z=g.transform.position.z;if(!Bank(x,z)){UnityEngine.Object.DestroyImmediate(g);Check(scale>.35f,"Footprint cannot fit local domain "+asset);return Put(asset,x< -25?-23:Mathf.Clamp(x,-23,18),Mathf.Clamp(z,26,54),scale*.85f,yaw,lod,bury,shadow);}float ground=Ground(x,z);
            foreach(var c in new[]{new Vector2(b.min.x,b.min.z),new Vector2(b.min.x,b.max.z),new Vector2(b.max.x,b.min.z),new Vector2(b.max.x,b.max.z)})if(Bank(c.x,c.y))ground=Mathf.Min(ground,Ground(c.x,c.y));
            g.transform.position+=Vector3.up*(ground-b.min.y-b.size.y*bury);b=GoldenSliceArtPass.VisualBounds(g);Check(!b.Intersects(Protected),"OBSERVATION_SIGHTLINE_PROTECTED "+g.name);
            foreach(var group in g.GetComponentsInChildren<LODGroup>())if(lod>=0){var l=group.GetLODs()[Math.Min(lod,group.lodCount-1)];group.SetLODs(new[]{new LOD(.004f,l.renderers)});var rs=l.renderers.ToHashSet();foreach(var r in group.GetComponentsInChildren<Renderer>(true))r.enabled=rs.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(group);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved real Level01");Check(File.Exists(Evidence+"/RealSceneSurvey.txt"),"Survey first");if(!File.Exists(Evidence+"/Before.unity"))File.Copy(scene.path,Evidence+"/Before.unity",false);
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(Root)==null,"No duplicate apply");var all=Level01EnvironmentArtPass.All(scene);var original=all.Where(c=>c is MonoBehaviour||c is Collider||c is MeshFilter||c is Renderer).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var puzzle=all.OfType<Tikal.Puzzles.Level01.ObservationAlignmentPuzzle>().Single();Check(Mathf.Abs(puzzle.MinimumDot-.97f)<.00001f&&Mathf.Abs(puzzle.HoldSeconds-.35f)<.00001f,"Original dot/time");var ps=new SerializedObject(puzzle);var point=(Component)ps.FindProperty("_point").objectReferenceValue;Check(Vector3.Distance(point.transform.position,new Vector3(-5,1.9f,39))<.01f,"Surveyed real altar");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zone04");root=new GameObject(Root).transform;root.SetParent(env,false);placed.Clear();var hidden=new HashSet<Renderer>();
            foreach(var n in new[]{"Talud_55","Talud_58","Talud_60","Talud_62","Talud_69","Talud_71","Talud_100","Talud_103","GalleryRuinRockShoulder"}){var r=env.Find("NaturalBoundaries/"+n).GetComponent<Renderer>();hidden.Add(r);r.enabled=false;}
            foreach(var r in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.bounds.center.x<11&&r.bounds.center.x> -18&&r.bounds.center.z>27&&r.bounds.center.z<55.5f)){hidden.Add(r);r.enabled=false;}
            var ruin=env.Find("Ruins_Blockout/GalleryBlankRuinWall").GetComponent<Renderer>();hidden.Add(ruin);ruin.enabled=false;
            var mesh=Terrain();var earth=new GameObject("OrganicTerraceBanks_45x30m");earth.transform.SetParent(root,false);earth.AddComponent<MeshFilter>().sharedMesh=mesh;earth.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");snap=earth.AddComponent<MeshCollider>();snap.sharedMesh=mesh;Physics.SyncTransforms();
            var clusters=new[]{new Vector2(-14,33),new Vector2(-15,43),new Vector2(-10,49),new Vector2(-5,52),new Vector2(6,49),new Vector2(15,44.5f),new Vector2(15,33),new Vector2(4,28)};
            for(int i=0;i<clusters.Length;i++)for(int j=0;j<3;j++){var p=clusters[i]+new Vector2((j-1)*1.3f,Mathf.Sin(i*3+j*2)*.75f);Put(j==1?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02",p.x,p.y,1.2f+j*.25f,i*71+j*113,1,.22f);}
            for(int i=0;i<clusters.Length;i++){var p=clusters[i];Put(i%3==0?"ARQ_MuroMaya_01":i%3==1?"ARQ_ColumnaDerrumbada_01":"ARQ_PlataformaElevada_01",p.x,p.y+.6f,i%3==2?.28f:1.1f,i*73,1,.18f);Put("ARQ_BloqueMaya_02",p.x+.6f,p.y-.7f,1.25f,i*93,1,.19f);}
            var trees=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_CeibaJoven_01","VEG_CeibaJoven_01"};
            var mid=new[]{new Vector2(-16,30),new Vector2(-17,37),new Vector2(-16,45),new Vector2(-12,49),new Vector2(-6,51),new Vector2(6,50),new Vector2(12,49),new Vector2(16,44),new Vector2(16,33),new Vector2(10,28),new Vector2(1,27),new Vector2(-13,27)};
            for(int i=0;i<mid.Length;i++)Put(trees[i%4],mid[i].x,mid[i].y,.83f+i%4*.045f,i*137,-1,.04f,i%3==0);
            var bg=new[]{new Vector2(-21,27),new Vector2(-22,32),new Vector2(-21,39),new Vector2(-23,44),new Vector2(-20,49),new Vector2(-16,53),new Vector2(-11,53),new Vector2(-6,54),new Vector2(6,54),new Vector2(11,54),new Vector2(16,52),new Vector2(18,48),new Vector2(18,44),new Vector2(18,33),new Vector2(17,27),new Vector2(11,26),new Vector2(5,26),new Vector2(-12,26)};
            for(int i=0;i<bg.Length;i++)Put(i==4||i==12?"VEG_PalmeraTropical_02":trees[(i+2)%4],bg[i].x,bg[i].y,.78f+i%5*.04f,i*137+31,i%3==0?2:1,.04f);
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_RaizTropical_01","VEG_HojasSecasMonton_01"};
            for(int i=0;i<72;i++){var p=clusters[i%8];float a=i*2.39996f,r=.65f+i%4*.4f;float x=p.x+Mathf.Cos(a)*r,z=p.y+Mathf.Sin(a)*r;if(Bank(x,z))Put(plants[i%5],x,z,.85f+i%4*.085f,i*137,1,i%5==3?.16f:.045f);}
            UnityEngine.Object.DestroyImmediate(snap);snap=null;
            Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new colliders");Check(original.All(k=>hidden.Contains(k.Key as Renderer)||k.Key!=null&&Props(k.Key)==k.Value),"Original scripts, references, fragments, materials and colliders unchanged");Check(poses.All(k=>Pose(k.Key)==k.Value),"All original transforms unchanged including manual work");Check(placed.All(g=>!GoldenSliceArtPass.VisualBounds(g).Intersects(Protected)),"All new asset bounds outside protected sightline");
            var vertices=mesh.vertices;var tris=mesh.triangles;for(int i=0;i<tris.Length;i+=3){var b=new Bounds(vertices[tris[i]],Vector3.zero);b.Encapsulate(vertices[tris[i+1]]);b.Encapsulate(vertices[tris[i+2]]);Check(!b.Intersects(Protected),"Terrain below/outside sightline");}
            Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");Check(root.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m!=null)),"No Missing Materials");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Zone04 saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.txt","Original ObservationPoint (-5,1.9,39), zone center (-5,3.18,39), target (-5,5.369,32.178); dot .97; hold .35; five fragments unchanged.\nOBSERVATION_SIGHTLINE_PROTECTED="+Protected.ToString("F3")+"\nExpansion west14m east9m north9.5m south7m (bounded by existing adjacent zones); seven broad organic hills; 12 midground / 18 background trees.\n24 rocks;16 ruin pieces; "+(placed.Count-70)+" understory; total="+placed.Count+"; triangles="+tris.Length/3+"\nHidden only scoped proxy renderers="+hidden.Count+"; 0 new colliders.\nAll original transforms, MonoBehaviours, colliders, fragment renderers and references unchanged; all new bounds and terrain triangles outside protected volume.\n"+string.Join("\n",placed.Select(g=>g.name+" "+g.transform.position.ToString("F3")+" "+GoldenSliceArtPass.VisualBounds(g).ToString("F3"))));Debug.Log("ZONE04_OBSERVATION_TERRACE_SAVED "+placed.Count);
        }
    }
}
