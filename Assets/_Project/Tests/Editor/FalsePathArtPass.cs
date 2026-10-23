using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Tikal.Core;
using Tikal.UI;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class FalsePathArtPass
    {
        public const string Evidence="../_FalsePath_20261022";
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static void Check(bool ok,string s){if(!ok)throw new InvalidOperationException(s);}
        static FalsePathArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/FalsePathFinish.request")){File.Delete("Temp/FalsePathFinish.request");try{Finish();}catch(Exception e){File.WriteAllText(Evidence+"/FinishError.txt",e.ToString());Debug.LogException(e);}return;}
            if(File.Exists("Temp/FalsePathApply.request")){File.Delete("Temp/FalsePathApply.request");try{Apply();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap.gameObject);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(!File.Exists("Temp/FalsePathSurvey.request"))return;
            File.Delete("Temp/FalsePathSurvey.request");
            try{Survey();}catch(Exception e){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Survey()
        {
            Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Real Level01 required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current scene");if(!File.Exists(Evidence+"/Before.unity"))File.Copy(scene.path,Evidence+"/Before.unity");var all=Level01EnvironmentArtPass.All(scene);var reveal=all.OfType<FalsePathReveal>().Single();var path=(GameObject)new SerializedObject(reveal).FindProperty("_path").objectReferenceValue;var lines=new List<string>{"FALSE PATH "+Level01EnvironmentInspection.PathOf(path.transform),"REVEAL "+Props(reveal)};
            foreach(var t in all.OfType<Transform>().Where(t=>t.IsChildOf(path.transform)||t.name.Contains("FinalWarning")||t.name.Contains("Checkpoint")))
            {lines.Add("TRANSFORM "+Level01EnvironmentInspection.PathOf(t)+" world="+t.position.ToString("F3")+" local="+t.localPosition.ToString("F3")+" scale="+t.lossyScale.ToString("F3")+" active="+t.gameObject.activeSelf);foreach(var m in t.GetComponents<MonoBehaviour>().Where(m=>m!=null))lines.Add(m.GetType().Name+" "+Props(m));foreach(var r in t.GetComponents<Renderer>())lines.Add("RENDER enabled="+r.enabled+" "+r.bounds.ToString("F3"));}
            foreach(var c in all.OfType<Collider>()){var b=c is BoxCollider bc?new Bounds(c.transform.TransformPoint(bc.center),Vector3.Scale(bc.size,c.transform.lossyScale)):c.bounds;if(b.max.z<77||b.min.z>110||b.max.x< -12||b.min.x>28)continue;lines.Add("COLLIDER "+Level01EnvironmentInspection.PathOf(c.transform)+" active="+c.gameObject.activeInHierarchy+" trigger="+c.isTrigger+" "+b.ToString("F3")+" "+Props(c));}
            foreach(var m in all.OfType<MonoBehaviour>().Where(m=>m is LevelOutcomeController||m is LevelResultPresenter||m is CheckpointManager||m is PointOfNoReturnTrigger))lines.Add("FUNCTION "+Level01EnvironmentInspection.PathOf(m.transform)+" "+m.GetType().Name+" "+Props(m));
            foreach(var r in all.OfType<Renderer>().Where(r=>r.enabled&&r.bounds.center.z>78&&r.bounds.center.z<108&&r.bounds.center.x> -12&&r.bounds.center.x<28&&r.GetComponentInParent<LODGroup>()==null))lines.Add("LOCAL_RENDER "+Level01EnvironmentInspection.PathOf(r.transform)+" "+r.bounds.ToString("F3"));
            File.WriteAllLines(Evidence+"/RealSceneSurvey.txt",lines);Debug.Log("FALSE_PATH_REAL_SURVEY_READY");
        }

        const string Folder="Assets/_Project/Art/Environment/Level01/FalsePath";
        const string Root="ENV_L01_FalsePath_DenseWarning";
        static Transform root;static MeshCollider snap;static MeshCollider previousBank;
        static readonly List<GameObject> parts=new List<GameObject>();
        static string Pose(Transform t)=>t.localPosition.ToString("F7")+t.localRotation.ToString("F7")+t.localScale.ToString("F7")+t.gameObject.activeSelf;
        static bool Bank(float x,float z)=>x>=-9&&x<=21&&z>=78&&z<=110&&!(x>4.4f&&x<7.6f&&z<98);
        static float Height(float x,float z)
        {
            float d=Mathf.Max(0,Mathf.Abs(x-6)-1.6f);if(z>=98)d=Mathf.Sqrt(d*d+(z-98)*(z-98));
            float h=3.2f;var hills=new[]{new Vector4(.5f,83,4.8f,5.8f),new Vector4(-1,94,5.5f,7),new Vector4(1,104,6,5),new Vector4(11.8f,84,4.6f,6),new Vector4(13,96,6,7),new Vector4(9,105,6,5)};
            for(int i=0;i<hills.Length;i++){var c=hills[i];float r=Mathf.Pow((x-c.x)/c.z,2)+Mathf.Pow((z-c.y)/c.w,2);h=Mathf.Max(h,(4.5f+i%3*.5f)*Mathf.Exp(-r*.55f));}
            float y=.6f+Mathf.SmoothStep(0,1,d/4.2f)*(h+.18f*Mathf.Sin(x*.7f+z*.35f)+.12f*Mathf.Cos(x*.35f-z*.8f));
            // Match the already approved sanctuary backdrop at the southern seam without changing it.
            if(z<88&&previousBank.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60))y=Mathf.Lerp(hit.point.y,y,Mathf.SmoothStep(0,1,(z-78)/10));
            return y;
        }
        static Mesh Terrain()
        {
            const int nx=61,nz=65;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tris=new List<int>();
            for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=-9+ix*.5f,z=78+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Bank(-9+(ix+.5f)*.5f,78+(iz+.5f)*.5f)){int a=iz*nx+ix;tris.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_FalsePath_OrganicBanks",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tris.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static float Ground(float x,float z)
        {Check(snap.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60),"New visual bank support "+x+"/"+z);return hit.point.y;}
        static void Lods(GameObject g,int level,bool shadow)
        {
            foreach(var l in g.GetComponentsInChildren<LODGroup>(true))if(level>=0){var lod=l.GetLODs()[Math.Min(level,l.lodCount-1)];l.SetLODs(new[]{new LOD(.004f,lod.renderers)});var used=lod.renderers.ToHashSet();foreach(var r in l.GetComponentsInChildren<Renderer>(true)){r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        }
        static GameObject New(string asset)
        {var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Existing optimized asset "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(p);g.name="FP_"+asset+"_"+parts.Count.ToString("D3");foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;}
        static GameObject Put(string asset,float x,float z,float scale,float yaw,int lod=1,float bury=.08f,bool shadow=false)
        {
            var g=New(asset);g.transform.SetPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;var b=GoldenSliceArtPass.VisualBounds(g);bool tree=asset.Contains("Arbol")||asset.Contains("Ceiba")||asset.Contains("Palmera");
            // The original solid corridor remains the physical authority. Keep all low solids outside it.
            float left=tree?x-2.8f*scale:b.min.x,right=tree?x+2.8f*scale:b.max.x;
            if(z<98){if(x<6)g.transform.position+=Vector3.left*Mathf.Max(0,right-4.35f);else g.transform.position+=Vector3.right*Mathf.Max(0,7.65f-left);}
            x=g.transform.position.x;z=g.transform.position.z;Check(Bank(x,z),"Local placement domain "+asset);b=GoldenSliceArtPass.VisualBounds(g);float h=Ground(x,z);
            foreach(var q in new[]{new Vector2(b.min.x,b.min.z),new Vector2(b.min.x,b.max.z),new Vector2(b.max.x,b.min.z),new Vector2(b.max.x,b.max.z)})if(Bank(q.x,q.y))h=Mathf.Min(h,Ground(q.x,q.y));
            g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*bury);Lods(g,lod,shadow);g.transform.SetParent(root,true);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);parts.Add(g);return g;
        }
        static GameObject Fit(Transform parent,string asset,Bounds target,float yaw=0)
        {
            var g=New(asset);g.transform.rotation=Quaternion.Euler(0,yaw,0);var b=GoldenSliceArtPass.VisualBounds(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(target.size.x/b.size.x,target.size.y/b.size.y,target.size.z/b.size.z));b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=target.center-b.center;Lods(g,0,true);g.transform.SetParent(parent,true);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);parts.Add(g);return g;
        }
        static Transform Visual(Transform parent){var t=new GameObject("FalsePath_VisualCladding").transform;t.SetParent(parent,false);return t;}
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Preserved real scene");Check(File.Exists(Evidence+"/RealSceneSurvey.txt"),"Real survey required");var all=Level01EnvironmentArtPass.All(scene);var reveal=all.OfType<FalsePathReveal>().Single();var path=(GameObject)new SerializedObject(reveal).FindProperty("_path").objectReferenceValue;Check(path.transform.Find(Root)==null,"Single application only");var trap=path.GetComponentInChildren<PointOfNoReturnTrigger>(true);var warning=path.transform.Find("FinalWarningSteleAnchor");Check(Vector3.Distance(trap.transform.position,new Vector3(6,1.8f,91.5f))<.01f&&Vector3.Distance(warning.position,new Vector3(4.9f,.6f,83.5f))<.01f,"Surveyed functional coordinates unchanged");
            var original=all.Where(c=>c is MonoBehaviour||c is Collider||c is Renderer||c is MeshFilter).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));var hidden=new HashSet<Renderer>();parts.Clear();root=new GameObject(Root).transform;root.SetParent(path.transform,true);root.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.localScale=Vector3.one;
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","FalsePath");
            var old=all.OfType<MeshFilter>().Single(f=>f.name=="OrganicSanctuaryBanks_48x32m");previousBank=new GameObject("FalsePath_SeamQuery_TEMP").AddComponent<MeshCollider>();previousBank.sharedMesh=old.sharedMesh;previousBank.transform.SetPositionAndRotation(old.transform.position,old.transform.rotation);previousBank.transform.localScale=old.transform.lossyScale;Physics.SyncTransforms();
            // 1/2/3: continuous terrain mass, an unchanged 3.2m corridor, and six rounded hills.
            var mesh=Terrain();UnityEngine.Object.DestroyImmediate(previousBank.gameObject);previousBank=null;var earth=new GameObject("OrganicBanks_30x32m_6Hills");earth.transform.SetParent(root,false);earth.AddComponent<MeshFilter>().sharedMesh=mesh;var earthMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");earth.AddComponent<MeshRenderer>().sharedMaterial=earthMat;
            var query=new GameObject("FalsePath_GroundQuery_TEMP");snap=query.AddComponent<MeshCollider>();snap.sharedMesh=mesh;Physics.SyncTransforms();
            var floor=path.transform.Find("Corridor floor").GetComponent<Renderer>();floor.sharedMaterial=earthMat;
            foreach(var name in new[]{"Western corridor rock","Eastern corridor rock"}){var r=path.transform.Find(name).GetComponent<Renderer>();r.enabled=false;hidden.Add(r);}
            // 4. Broken stone shoulders hide the retained narrow-wall colliders, irregular silhouettes above.
            for(int side=0;side<2;side++)for(int i=0;i<7;i++)
            {float z=79.8f+i*2.7f,h=3.3f+(i%3)*.32f;float x=side==0?3.45f:8.55f;Fit(root,i%3==1?"ARQ_MuroMaya_01":"ROC_RocaMusgo_02",new Bounds(new Vector3(x,.5f+h*.5f,z),new Vector3(1.85f,h,3.15f)),side*180+i*17);}
            for(int i=0;i<12;i++){bool left=i%2==0;float z=81.5f+(i/2)*3.8f;Put(i%3==0?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02",left?1.6f:10.4f,z,1.1f+i%3*.15f,i*137,1,.2f);}
            for(int i=0;i<8;i++)Put(i%2==0?"ARQ_BloqueMaya_02":"ARQ_ColumnaDerrumbada_01",i%2==0?1.8f:10.5f,82+i*2.7f,.8f+i%3*.12f,i*79,1,.2f);
            // 5. Ten midground and sixteen background trees; only four midground trees cast shadows.
            var trees=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_CeibaJoven_01"};
            for(int i=0;i<10;i++)Put(trees[i%3],i%2==0?-.2f:12.1f,80.5f+(i/2)*4.5f,1.12f+i%3*.06f,i*137,-1,.035f,i%3==0);
            for(int i=0;i<16;i++){float x=i<6?-5.5f:i<12?17.8f:-2+(i-12)*5;float z=i<12?80.5f+i%6*5:107;Put(trees[(i+1)%3],x,z,1.03f+i%4*.06f,i*137+33,i%3==0?2:1,.04f);}
            // 6. Preserve warning position, collider and visual-only design; 8m to trigger centre.
            var warningProxy=warning.GetComponentsInChildren<Renderer>(true).Single();Fit(Visual(warningProxy.transform),"ARQ_EstelaFragmentada_01",warningProxy.bounds,180);warningProxy.enabled=false;hidden.Add(warningProxy);
            // 7. Both closure visuals inherit the exact existing activation/reset hierarchy.
            foreach(var name in new[]{"Natural corridor closure","Closing passage"}){var r=path.transform.Find(name).GetComponent<Renderer>();Fit(Visual(r.transform),"ARQ_MuroMaya_01",r.bounds);r.enabled=false;hidden.Add(r);}
            // 8. Understory clusters on the banks; no decorative stelae or centre obstructions.
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_RaizTropical_01","VEG_HojasSecasMonton_01"};
            for(int i=0;i<48;i++){bool left=i%2==0;float z=79.8f+(i/2)*1.1f;float x=left?2.3f-Mathf.Sin(i*1.7f)*.8f:9.7f+Mathf.Cos(i*1.7f)*.8f;Put(plants[i%5],x,z,.8f+i%4*.12f,i*137,1,i%5==3?.15f:.045f);}
            foreach(var q in new[]{new Vector3(4,4.1f,86),new Vector3(8,4.4f,92),new Vector3(6,5.8f,97)})Fit(root,"VEG_CortinaLianas_01",new Bounds(q,new Vector3(1.2f,2,.35f)));
            UnityEngine.Object.DestroyImmediate(snap.gameObject);snap=null;
            Check(parts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"No new colliders");Check(!path.activeSelf,"Original reveal activation preserved");Check(original.All(k=>hidden.Contains(k.Key as Renderer)||k.Key==floor||k.Key!=null&&Props(k.Key)==k.Value),"All original components/references unchanged except scoped visual renderers");Check(poses.All(k=>Pose(k.Key)==k.Value),"Every original transform and activation preserved, including frozen Zone05");Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new missing scripts");Check(parts.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m!=null)),"No missing materials");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"False Path saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.txt","FalsePath real entrance=(6,.6,78); floor x4.4..7.6,z78..98; warning=(4.9,.6,83.5); trigger=(6,1.8,91.5), original size3.2x2.4x1.2; closurez88.5. 8m narrative separation; no trigger/closure/collider/runtime changes.\nTerrain30x32m; six broad hills~3.2-5.5m, southern seam sampled from frozen Zone05 mesh. Original physical floor retained, earth material reused.\n26 trees (10 mid/16 background LOD1/2),51 understory/lianas,12 bank rocks,14 stone shoulders,8 broken ruins,one warning and2 existing closure claddings.\nAll original transforms/activation, MonoBehaviours, collider shapes/references/poses retained; frozen zones untouched. No new colliders, Missing Scripts or missing materials.\nParts="+parts.Count+" terrain triangles="+mesh.triangles.Length/3+"\n"+string.Join("\n",parts.Select(g=>g.name+" pos="+g.transform.position.ToString("F3")+" parent="+Level01EnvironmentInspection.PathOf(g.transform.parent))));Debug.Log("FALSE_PATH_ART_SAVED "+parts.Count);
        }

        static void Finish()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved local art");var all=Level01EnvironmentArtPass.All(scene);var path=(GameObject)new SerializedObject(all.OfType<FalsePathReveal>().Single()).FindProperty("_path").objectReferenceValue;root=path.transform.Find(Root);Check(root!=null&&!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="FinalCanopyCap"),"Finish once");parts.Clear();parts.AddRange(path.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("FP_")&&PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.gameObject));
            var original=all.Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);var frozen=all.OfType<Transform>().Where(t=>!t.IsChildOf(root)).ToDictionary(t=>t,Pose);var query=new GameObject("FalsePath_FinishGround_TEMP");snap=query.AddComponent<MeshCollider>();snap.sharedMesh=root.Find("OrganicBanks_30x32m_6Hills").GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            // Lower only the four newly placed side-wall fragments, retaining their supported bases.
            foreach(var g in parts.Where(g=>g.transform.parent==root&&g.name.Contains("ARQ_MuroMaya"))){float y=GoldenSliceArtPass.VisualBounds(g).min.y;var scale=g.transform.localScale;scale.y*=.68f;g.transform.localScale=scale;g.transform.position+=Vector3.up*(y-GoldenSliceArtPass.VisualBounds(g).min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);}
            var plants=parts.Where(g=>g.transform.parent==root&&(g.name.Contains("Helecho")||g.name.Contains("PlantaTropical")||g.name.Contains("Arbusto"))).Take(12).ToArray();
            var queries=new List<GameObject>();
            foreach(var g in parts.Where(g=>g.transform.parent==root&&g.name.Contains("ROC_")))foreach(var f in g.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.Contains("LOD0")))
            {var q=new GameObject("RockSupport_TEMP");q.transform.SetPositionAndRotation(f.transform.position,f.transform.rotation);q.transform.localScale=f.transform.lossyScale;var c=q.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;queries.Add(q);}Physics.SyncTransforms();
            for(int i=0;i<plants.Length;i++)
            {
                bool left=i%2==0,upper=i<6;float x=upper?(left?3.35f:8.65f):(left?4.35f:7.65f);float z=upper?80.4f+i/2*6.6f:80.4f+(i-6)/2*6.7f;float h=upper?Ground(x,z):.6f;
                if(upper)foreach(var q in queries)if(q.GetComponent<Collider>().Raycast(new Ray(new Vector3(x,20,z),Vector3.down),out var hit,40))h=Mathf.Max(h,hit.point.y);
                var g=plants[i];g.transform.localScale=Vector3.one*(upper?.72f:.35f);g.transform.position=new Vector3(x,0,z);var b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*.045f);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
            }
            foreach(var q in queries)UnityEngine.Object.DestroyImmediate(q);int before=parts.Count;
            foreach(var q in new[]{new Vector2(0,103),new Vector2(4,106),new Vector2(9,104),new Vector2(14,105)})Put("VEG_ArbolTropical_03",q.x,q.y,1.35f,q.x*37,1,.035f);
            new GameObject("FinalCanopyCap").transform.SetParent(root,false);UnityEngine.Object.DestroyImmediate(snap.gameObject);snap=null;
            Check(original.All(k=>Props(k.Key)==k.Value)&&frozen.All(k=>Pose(k.Key)==k.Value),"All original logic, colliders and frozen poses unchanged");Check(parts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"No added colliders");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final local art saved");AssetDatabase.SaveAssets();File.AppendAllText(Evidence+"/EditorValidation.txt","\nFINISH: four newly placed side-wall fragments lowered; twelve existing new understory placements redistributed (six supported on rock shoulders, six small ground ferns/plants at the edges); +4 background LOD1 tropical trees close terminal sky gap. Total trees30 (10mid/20background), total parts="+parts.Count+"; no new colliders, no original poses/logic changes.\n");Debug.Log("FALSE_PATH_FINISH_SAVED "+parts.Count);
        }
    }
}
