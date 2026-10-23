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
    // Deliberate one-shot authoring, confined to an additive, non-colliding Zona02 shell.
    [InitializeOnLoad]
    public static class Zona02DenseShell
    {
        public const string Evidence="../_Zona02Shell_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zona02Shell";
        const string Root="ENV_L01_Zona02_DenseShell";
        static Transform root;
        static MeshCollider snap;
        static readonly List<GameObject> placed=new List<GameObject>();
        static readonly List<GameObject> rocks=new List<GameObject>();
        static readonly List<GameObject> drapes=new List<GameObject>();
        // Centre X/Z, horizontal radii, crest rise over the ceremonial floor.
        static readonly Vector4[] lobes={new Vector4(24,-14,7,6),new Vector4(37,-15,9,7),new Vector4(49,-10,8,9),new Vector4(54,4,7,10),new Vector4(54,19,7,9),new Vector4(23.1f,27.8f,4,4.5f),new Vector4(35.7f,27.8f,4,4.5f)};
        static readonly float[] rises={4.1f,5.4f,4.8f,5.8f,4.6f,5.9f,5.6f};
        [Serializable] class Report {public int banks,rocks,ruins,trees,backgroundTrees,understory,placements,newColliders,triangles;public float eastExpansion,southExpansion;public string[] assets,checks;}
        static Zona02DenseShell(){EditorApplication.update+=Poll;}
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Poll()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/Zona02ShellRefine.request"))
            {File.Delete("Temp/Zona02ShellRefine.request");try{Refine();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);File.WriteAllText(Evidence+"/RefineError.txt",e.ToString());Debug.LogException(e);}return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Zona02ShellApply.request"))return;
            File.Delete("Temp/Zona02ShellApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static bool Bank(float x,float z)=>x>=19&&x<=61&&z>=-22&&z<=29&&!(x<39&&z>3&&z<21)&&!(z>20.9f&&x>26.5f&&x<31.5f);
        static float Old(float x,float z)
        {
            float d=Mathf.Max(3-z,z-21,x-39,0),f=Mathf.SmoothStep(0,1,d/4);
            return 1.2f+2.6f*(1-Mathf.Exp(-d*.3f))+f*(.46f*Mathf.Sin(x*.45f+z*.31f)+.24f*Mathf.Cos(x*.71f-z*.37f))+Mathf.SmoothStep(0,1,(d-.8f)/4)*(.65f+1.2f*(.5f+.5f*Mathf.Sin(x*.29f-z*.38f)));
        }
        static float Height(float x,float z)
        {
            float h=2.9f;
            for(int i=0;i<lobes.Length;i++){var l=lobes[i];float r=Mathf.Pow((x-l.x)/l.z,2)+Mathf.Pow((z-l.y)/l.w,2);h=Mathf.Max(h,1.2f+rises[i]*Mathf.Exp(-r*.72f));}
            h+=.18f*Mathf.Sin(x*.65f+z*.48f)+.12f*Mathf.Cos(x*.9f-z*.33f);
            float blend=Mathf.Max(Mathf.SmoothStep(0,1,(x-46.5f)/4),Mathf.SmoothStep(0,1,(-z-7.5f)/4),Mathf.SmoothStep(0,1,(z-24)/3));
            // An additive cap meets the existing bank, leaving its inner rim unchanged.
            return Mathf.Lerp(Old(x,z)+.025f,Mathf.Max(Old(x,z)+.025f,h),blend);
        }
        static bool Cap(float x,float z)=>Bank(x,z)&&(x>46.5f||z<-7.5f||z>24);
        static Mesh Terrain()
        {
            const int nx=85,nz=103;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=19+ix*.5f,z=-22+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Cap(19+(ix+.5f)*.5f,-22+(iz+.5f)*.5f)){int a=iz*nx+ix;tri.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_Zona02_ExpandedOrganicShell",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tri.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static float Ground(float x,float z)
        {
            if(snap.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60))return hit.point.y;
            return Old(x,z); // Existing measured inner bank, not the playable floor.
        }
        static GameObject Put(string asset,float x,float z,float scale,float yaw,float burial=.04f,int low=-1,bool shadow=false)
        {
            Check(Bank(x,z),"Local shell bounds "+asset+" "+x+"/"+z);
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Reused prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(p,root);g.name="Z02_Shell_"+asset+"_"+placed.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,Ground(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float h=Ground(x,z);
            if(asset.StartsWith("ROC_")||asset.StartsWith("ARQ_")||asset.Contains("Raiz"))h=Mathf.Min(h,Ground(b.min.x,b.min.z),Ground(b.max.x,b.max.z),Ground(b.min.x,b.max.z),Ground(b.max.x,b.min.z));
            g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*burial);
            foreach(var lod in g.GetComponentsInChildren<LODGroup>())if(low>=0){var level=lod.GetLODs()[Math.Min(low,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.004f,level.renderers)});var enabled=level.renderers.ToHashSet();foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=enabled.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Level01 Edit Mode required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Current approved state preserved");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(Root)==null,"One-shot shell already exists; inspect before retry");File.Copy(scene.path,Evidence+"/Before.unity",false);
            var all=Level01EnvironmentArtPass.All(scene);var props=all.Where(c=>c is MonoBehaviour||c is Collider||c is Renderer||c is MeshFilter).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);
            int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zona02Shell");
            root=new GameObject(Root).transform;root.SetParent(env,false);placed.Clear();rocks.Clear();drapes.Clear();
            var mesh=Terrain();var ground=new GameObject("ExpandedTerrain_12mEast_12mSouth_SevenBroadSwells");ground.transform.SetParent(root,false);ground.AddComponent<MeshFilter>().sharedMesh=mesh;ground.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");snap=ground.AddComponent<MeshCollider>();snap.sharedMesh=mesh;Physics.SyncTransforms();
            // Rock clusters first, with Maya fragments integrated into alternate clusters.
            var clusters=new[]{new Vector2(23,-11),new Vector2(35,-12),new Vector2(48,-8),new Vector2(51,3),new Vector2(52,17),new Vector2(23.4f,26.2f),new Vector2(35.5f,26.3f),new Vector2(46,25.7f)};
            for(int i=0;i<clusters.Length;i++)for(int j=0;j<3;j++){var q=clusters[i]+new Vector2((j-1)*1.8f,Mathf.Sin(i*4+j*2)*1.1f);rocks.Add(Put(j==1?"ROC_RocaFragmentada_01":j==0?"ROC_RocaMusgo_02":"ROC_RocaTropical_01",q.x,q.y,1.25f+(i+j)%3*.28f,67*i+113*j,.18f));}
            for(int i=0;i<clusters.Length;i++){var p=clusters[i];Put(i%3==0?"ARQ_MuroMaya_01":i%3==1?"ARQ_ColumnaDerrumbada_01":"ARQ_BloqueMaya_02",p.x+.6f,p.y+.65f,1.12f+i%3*.13f,i*71+19,.22f);if(i<4)Put("ARQ_BloqueMaya_02",p.x-1,p.y-.6f,1.3f,i*83,.2f);}
            // Twelve middle-distance trees preserve their original LOD chain; six cast shadows.
            var near=new[]{new Vector2(21.5f,-4),new Vector2(29,-6),new Vector2(38,-4),new Vector2(44,-1),new Vector2(46.8f,6),new Vector2(47.8f,16),new Vector2(44.5f,23),new Vector2(36.7f,25.5f),new Vector2(33.1f,27.6f),new Vector2(24.5f,27.3f),new Vector2(21.3f,23.4f),new Vector2(22.2f,1)};
            var trees=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_ArbolTropical_04","VEG_CeibaJoven_01"};
            for(int i=0;i<near.Length;i++)Put(trees[i%4],near[i].x,near[i].y,.9f+(i*7%11)*.022f,i*137,.025f,-1,i%2==0);
            // Twenty-two staggered background crowns: no physics, no shadow casting, LOD1/2 only.
            var far=new[]{new Vector2(21,-17),new Vector2(26,-19),new Vector2(30,-14),new Vector2(35,-20),new Vector2(40,-16),new Vector2(44,-20),new Vector2(49,-15),new Vector2(54,-18),new Vector2(58,-12),new Vector2(53,-5),new Vector2(58,0),new Vector2(54,8),new Vector2(58,13),new Vector2(53,21),new Vector2(57,26),new Vector2(49,27),new Vector2(43,28),new Vector2(38.5f,28),new Vector2(34.2f,28.5f),new Vector2(25.1f,28.4f),new Vector2(20.3f,27),new Vector2(21,-9)};
            for(int i=0;i<far.Length;i++)Put(i==10||i==15?"VEG_PalmeraTropical_02":trees[(i+2)%4],far[i].x,far[i].y,.92f+(i*13%12)*.018f,i*137+43,.03f,i%3==0?2:1);
            // Fifty-four understory plants/roots around the same masses, not in the ceremonial centre.
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_ArbustoTropical_02","VEG_RaizTropical_01"};
            for(int i=0;i<54;i++){var p=clusters[i%clusters.Length];float a=i*2.39996f,r=1.1f+(i%5)*.42f;float x=p.x+Mathf.Cos(a)*r,z=p.y+Mathf.Sin(a)*r;if(z>28.5f)z=28.5f;if(z>21&&x>25.8f&&x<32.2f)x=x<29?25.5f:32.5f;Put(plants[i%5],x,z,.86f+(i%6)*.065f,i*137,i%5==4?.16f:.04f,1);}
            // Six curtains physically attached to existing new rock crests, rather than suspended in air.
            for(int i=0;i<6;i++){var support=rocks[i*3];var b=GoldenSliceArtPass.VisualBounds(support);var p=support.transform.position;var g=Put("VEG_CortinaLianas_01",p.x,p.z,.55f,i*71,0,1);var gb=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=Vector3.up*(b.max.y-.08f-gb.max.y);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);drapes.Add(g);}
            UnityEngine.Object.DestroyImmediate(snap);snap=null;
            Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No final art colliders");Check(props.All(k=>k.Key!=null&&Props(k.Key)==k.Value)&&poses.All(k=>Pose(k.Key)==k.Value),"All prior components, materials, meshes and poses unchanged");
            Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");
            Check(placed.Except(drapes).All(g=>GoldenSliceArtPass.VisualBounds(g).min.y<=Mathf.Max(Height(g.transform.position.x,g.transform.position.z),Old(g.transform.position.x,g.transform.position.z))+.05f),"All new bases snapped or deliberately embedded");
            var room=new Bounds(new Vector3(29,2.3f,12),new Vector3(19.8f,2.2f,17.8f));Check(placed.Where(g=>g.name.Contains("ROC_")||g.name.Contains("ARQ_")).All(g=>!GoldenSliceArtPass.VisualBounds(g).Intersects(room)),"Solid art beyond original room boundaries");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Shell scene saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{banks=7,rocks=24,ruins=12,trees=34,backgroundTrees=22,understory=60,placements=placed.Count,newColliders=0,triangles=mesh.triangles.Length/3,eastExpansion=12,southExpansion=12,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"Approved scene saved before authoring","All existing transforms, activation, renderers, meshes, MonoBehaviours and colliders unchanged","Only additive Zona02 shell; no northward extension into Zone03 or westward extension into Puzzle01","No new Missing Scripts or colliders; temporary snap collider removed","Ground raycast on shell; measured legacy bank support in inner band; six supported rock curtains","Native local FPS/TPS and five views pending"}},true));Debug.Log("ZONA02_DENSE_SHELL_SAVED "+placed.Count);
        }
        static float Shoulder(float x,float z)
        {
            if(x<19)
            {
                float near=z<10?6.3f:20.1f;float r=Mathf.Pow((x-14.5f)/4.4f,2)+Mathf.Pow((z-near)/6,2);
                return 1.2f+5.9f*Mathf.Exp(-r*.65f)*Mathf.SmoothStep(0,1,(19-x)/2.8f)+.1f*Mathf.Sin(x+z);
            }
            float h=Height(x,z);
            if(z>21&&z<29&&(x<26.5f||x>31.5f&&x<40))
            {
                float cx=x<29?23:35.8f;float r=Mathf.Pow((x-cx)/4.7f,2)+Mathf.Pow((z-25.2f)/5.3f,2);
                float target=1.2f+6*Mathf.Exp(-r*.5f)+.12f*Mathf.Sin(x*.7f+z);
                h=Mathf.Lerp(h,Mathf.Max(h,target),Mathf.SmoothStep(0,1,(z-21)/2.6f));
            }
            return h;
        }
        static Mesh ShoulderMesh()
        {
            const int nx=99,nz=103;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=12+ix*.5f,z=-22+iz*.5f;v[i]=new Vector3(x,Shoulder(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++){float x=12+(ix+.5f)*.5f,z=-22+(iz+.5f)*.5f;bool west=x<19&&z>3&&z<28&&(z<9.5f||z>14.5f);if(Cap(x,z)||west||Bank(x,z)&&z>21&&(x<26.5f||x<40&&x>31.5f)){int a=iz*nx+ix;tri.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}}
            var m=new Mesh{name="MESH_Zona02_ExpandedOrganicShell",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tri.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        static void Refine()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved local scene");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);Check(root!=null&&root.GetComponentsInChildren<Transform>().Count(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))==130,"Only initial shell may be refined once");
            var original=Level01EnvironmentArtPass.All(scene).Where(c=>c!=null&&!c.transform.IsChildOf(root)).ToArray();var props=original.Where(c=>c is MonoBehaviour||c is Collider||c is Renderer||c is MeshFilter).ToDictionary(c=>c,Props);var poses=original.OfType<Transform>().ToDictionary(t=>t,Pose);
            placed.Clear();placed.AddRange(root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.gameObject));
            var ground=root.GetComponentInChildren<MeshFilter>();var m=ShoulderMesh();EditorUtility.CopySerialized(m,ground.sharedMesh);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(ground.sharedMesh);snap=ground.gameObject.AddComponent<MeshCollider>();snap.sharedMesh=ground.sharedMesh;Physics.SyncTransforms();
            // Compact-root tropical crowns overlap from beyond the room; oversized root skirts are unsuitable here.
            foreach(var p in new[]{new Vector2(25,-1.7f),new Vector2(35.6f,-1.8f),new Vector2(23.5f,26),new Vector2(35,26.5f)})Put("VEG_ArbolTropical_04",p.x,p.y,1.12f,placed.Count*137,.03f,1);
            // Two western crowns lean over the boundary while keeping the connector opening clear.
            foreach(var p in new[]{new Vector2(19.1f,1.3f),new Vector2(19.1f,24.5f)})Put("VEG_ArbolTropical_04",p.x,p.y,1.08f,placed.Count*137,.04f,1);
            foreach(var p in new[]{new Vector2(21.5f,23.5f),new Vector2(24.5f,24),new Vector2(33.4f,24),new Vector2(37,23.5f)})
            {
                Put("ROC_RocaMusgo_02",p.x,p.y,2.4f,placed.Count*137,.18f);
                Put("ROC_RocaFragmentada_01",p.x+.3f,p.y+1.7f,1.8f,placed.Count*137,.2f);
                Put("VEG_ArbustoTropical_03",p.x,p.y-.4f,1.3f,placed.Count*137,.05f,1);
                Put("VEG_PlantaTropical_02",p.x+.5f,p.y+.6f,1.05f,placed.Count*137,.04f,1);
            }
            UnityEngine.Object.DestroyImmediate(snap);snap=null;Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No shell collider");Check(props.All(k=>Props(k.Key)==k.Value)&&poses.All(k=>Pose(k.Key)==k.Value),"All pre-existing approved art, puzzle components and poses preserved");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Refined shell saved");AssetDatabase.SaveAssets();
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));report.banks=9;report.rocks=32;report.trees=40;report.backgroundTrees=28;report.understory=68;report.placements=placed.Count;report.assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray();report.triangles=ground.sharedMesh.triangles.Length/3;report.checks=report.checks.Concat(new[]{"Two broad western shoulder caps stop outside original room and connector; Puzzle01 floor untouched","North shoulder faces raised organically without closing north exit","Six extra background crowns, eight rocks and eight plants added locally","All original approved components and transforms preserved; zero final colliders"}).ToArray();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("ZONA02_DENSE_SHELL_REFINED "+placed.Count);
        }
    }
}
