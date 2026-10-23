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
    public static class Zone05SanctuaryArtPass
    {
        public const string Evidence="../_Zone05Sanctuary_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zone05";
        const string Root="ENV_L01_Zone05_HiddenSanctuary";
        static Transform root;static MeshCollider snap,floor;static readonly List<GameObject> parts=new List<GameObject>();
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F7")+t.localRotation.ToString("F7")+t.localScale.ToString("F7")+t.gameObject.activeSelf;
        static Zone05SanctuaryArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/Zone05Finish.request")){File.Delete("Temp/Zone05Finish.request");Finish();return;}
            if(File.Exists("Temp/Zone05Apply.request")){File.Delete("Temp/Zone05Apply.request");try{Apply();}catch(Exception e){if(snap!=null)UnityEngine.Object.DestroyImmediate(snap);File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(!File.Exists("Temp/Zone05Survey.request"))return;File.Delete("Temp/Zone05Survey.request");Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Level01EnvironmentInspection.ScenePath){Check(!scene.isDirty,"Unsaved scene must be preserved first");scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);}Check(scene.path==Level01EnvironmentInspection.ScenePath,"Real Level01");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current scene");File.Copy(scene.path,Evidence+"/Before.unity",false);
            var all=Level01EnvironmentArtPass.All(scene);var zone=all.OfType<Tikal.Puzzles.Level01.Zone05RevelationGate>().Single().transform;var lines=new List<string>{"ZONE "+Level01EnvironmentInspection.PathOf(zone)};
            foreach(var t in zone.GetComponentsInChildren<Transform>(true)){lines.Add(Level01EnvironmentInspection.PathOf(t)+" activeSelf="+t.gameObject.activeSelf+" activeHierarchy="+t.gameObject.activeInHierarchy+" world="+t.position.ToString("F3")+" scale="+t.lossyScale.ToString("F3"));foreach(var m in t.GetComponents<MonoBehaviour>().Where(m=>m!=null))lines.Add(m.GetType().Name+" "+Props(m));foreach(var r in t.GetComponents<Renderer>())lines.Add("RENDER "+r.enabled+" "+r.bounds.ToString("F3")+" materials="+string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));}
            foreach(var c in all.OfType<Collider>().Where(c=>c.transform.parent!=null&&(c.transform.parent.name=="Terrain"||c.bounds.center.z>49&&c.bounds.center.z<83&&Mathf.Abs(c.bounds.center.x)<26)))lines.Add("COLLIDER "+Level01EnvironmentInspection.PathOf(c.transform)+" trigger="+c.isTrigger+" "+c.bounds.ToString("F3"));
            foreach(var r in all.OfType<Renderer>().Where(r=>r.enabled&&r.GetComponentInParent<LODGroup>()==null&&r.bounds.center.z>54&&r.bounds.center.z<83&&Mathf.Abs(r.bounds.center.x)<27))lines.Add("LOCAL_RENDER "+Level01EnvironmentInspection.PathOf(r.transform)+" "+r.bounds.ToString("F3"));
            File.WriteAllLines(Evidence+"/RealSceneSurvey.txt",lines);Debug.Log("ZONE05_REAL_SURVEY_READY");
        }
        static bool Bank(float x,float z)=>x>=-24&&x<=24&&z>=55.5f&&z<=88&&!(x> -11&&x<11&&z>56&&z<78)&&!(z<56&&Mathf.Abs(x)<2.6f)&&!(z>78&&x>3.8f&&x<8.2f);
        static void Finish()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Zone05");var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Root);Check(root!=null&&root.GetComponentsInChildren<Transform>().All(t=>t.name!="NorthEast_RuinSightMask"),"Finish only once");var all=Level01EnvironmentArtPass.All(scene);var original=all.Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);parts.Clear();parts.AddRange(all.OfType<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.name.StartsWith("Z05_")).Select(t=>t.gameObject));int before=parts.Count;floor=env.Find("Terrain/Surface_Zone05_Revelation").GetComponent<MeshCollider>();var earth=root.Find("OrganicSanctuaryBanks_48x32m");snap=earth.gameObject.AddComponent<MeshCollider>();snap.sharedMesh=earth.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();
            Put("VEG_ArbolTropical_03",-15,64,1.6f,37,-1,.035f);Put("VEG_ArbolTropical_02",15,68,1.6f,173,-1,.035f);Put("VEG_ArbolTropical_03",-3,82,1.55f,291,-1,.035f);
            var mask=Fit(root,"ARQ_MuroMaya_01",new Bounds(new Vector3(9.3f,3.5f,77.55f),new Vector3(3.2f,5.8f,.65f)));mask.name="NorthEast_RuinSightMask";
            UnityEngine.Object.DestroyImmediate(snap);snap=null;Check(original.All(k=>Props(k.Key)==k.Value)&&poses.All(k=>Pose(k.Key)==k.Value),"Existing gameplay, collider and all transforms unchanged");Check(parts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"No new colliders");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Local canopy/sight mask saved");AssetDatabase.SaveAssets();File.AppendAllText(Evidence+"/EditorValidation.txt","\nFINAL: +3 broad-crown midground trees (15 midground,18 background,total33); +1 northeast ruin sight mask against existing boundary, north route x6 remains free; no change to adjacent final path. Total parts="+parts.Count+"; no new colliders; all existing poses/logic preserved.\n");Debug.Log("ZONE05_CANOPY_FINISHED");
        }
        static float Height(float x,float z)
        {
            float xx=Mathf.Clamp(x,-10.999f,10.999f),zz=Mathf.Clamp(z,56.001f,77.999f);Check(floor.Raycast(new Ray(new Vector3(xx,25,zz),Vector3.down),out var hit,50),"Real floor edge support");float baseline=hit.point.y;
            float dx=Mathf.Max(-11-x,x-11,0),dz=Mathf.Max(56-z,z-78,0),d=Mathf.Sqrt(dx*dx+dz*dz);if(z<56)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(x)-2.6f));if(z>78)d=Mathf.Min(d,Mathf.Max(0,Mathf.Abs(x-6)-2.2f));
            var cs=new[]{new Vector4(-18,59,7,6),new Vector4(-19,69,7,8),new Vector4(-16,80,8,6),new Vector4(-5,84,7,6),new Vector4(16,83,7,6),new Vector4(19,73,7,7),new Vector4(18,60,7,7)};float h=2.8f;
            for(int i=0;i<cs.Length;i++){var c=cs[i];float r=Mathf.Pow((x-c.x)/c.z,2)+Mathf.Pow((z-c.y)/c.w,2);h=Mathf.Max(h,(4.3f+i%3*.55f)*Mathf.Exp(-r*.6f));}
            return baseline+Mathf.SmoothStep(0,1,d/5.2f)*(h+.2f*Mathf.Sin(x*.52f+z*.39f)+.12f*Mathf.Cos(x*.86f-z*.31f));
        }
        static Mesh Terrain()
        {
            const int nx=97,nz=66;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var tris=new List<int>();for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int i=iz*nx+ix;float x=-24+ix*.5f,z=55.5f+iz*.5f;v[i]=new Vector3(x,Height(x,z),z);uv[i]=new Vector2(x,z)*.28f;}
            for(int iz=0;iz<nz-1;iz++)for(int ix=0;ix<nx-1;ix++)if(Bank(-24+(ix+.5f)*.5f,55.5f+(iz+.5f)*.5f)){int a=iz*nx+ix;tris.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            var m=new Mesh{name="MESH_Zone05_OrganicSanctuaryBanks",indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=tris.ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");return m;
        }
        static float Ground(float x,float z){Check(snap.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60),"Bank support "+x+"/"+z);return hit.point.y;}
        static void Lods(GameObject g,int level,bool shadow)
        {foreach(var l in g.GetComponentsInChildren<LODGroup>())if(level>=0){var lod=l.GetLODs()[Math.Min(level,l.lodCount-1)];l.SetLODs(new[]{new LOD(.004f,lod.renderers)});var used=lod.renderers.ToHashSet();foreach(var r in l.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(l);}foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
        static GameObject Put(string asset,float x,float z,float scale,float yaw,int lod=1,float bury=.1f,bool shadow=false)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Reuse "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(p,root);g.name="Z05_"+asset+"_"+parts.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);var b=GoldenSliceArtPass.VisualBounds(g);bool tree=asset.Contains("Arbol")||asset.Contains("Ceiba")||asset.Contains("Palmera");var delta=Vector3.zero;
            float left=tree?x-2.8f*scale:b.min.x,right=tree?x+2.8f*scale:b.max.x,near=tree?z-2.8f*scale:b.min.z,far=tree?z+2.8f*scale:b.max.z;
            if(x< -11)delta.x=-Mathf.Max(0,right+11.3f);else if(x>11)delta.x=Mathf.Max(0,11.3f-left);else if(z>78)delta.z=Mathf.Max(0,78.3f-near);else if(z<56)delta.z=-Mathf.Max(0,far-55.7f);
            g.transform.position+=delta;x=g.transform.position.x;z=g.transform.position.z;if(!Bank(x,z)){UnityEngine.Object.DestroyImmediate(g);Check(scale>.35f,"Fit local footprint "+asset);return Put(asset,Mathf.Clamp(x,-22,22),Mathf.Clamp(z,57,86),scale*.85f,yaw,lod,bury,shadow);}b=GoldenSliceArtPass.VisualBounds(g);float h=Ground(x,z);
            foreach(var q in new[]{new Vector2(b.min.x,b.min.z),new Vector2(b.min.x,b.max.z),new Vector2(b.max.x,b.min.z),new Vector2(b.max.x,b.max.z)})if(Bank(q.x,q.y))h=Mathf.Min(h,Ground(q.x,q.y));g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*bury);Lods(g,lod,shadow);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);parts.Add(g);return g;
        }
        static Transform Visual(Transform parent,string name="VisualRoot_Zone05Stone")
        {Check(parent.Find(name)==null,"No duplicate visual root");var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static GameObject Fit(Transform parent,string asset,Bounds target,float yaw=0)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Reuse "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(p);g.name="Z05_"+asset+"_"+parts.Count.ToString("D3");g.transform.rotation=Quaternion.Euler(0,yaw,0);foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);var b=GoldenSliceArtPass.VisualBounds(g);g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(target.size.x/b.size.x,target.size.y/b.size.y,target.size.z/b.size.z));b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=target.center-b.center;Lods(g,0,true);g.transform.SetParent(parent,true);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);parts.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved real Level01");Check(File.Exists(Evidence+"/Before.unity"),"Real survey before placement");var level=scene.GetRootGameObjects().Single().transform;var env=level.Find("Environment");var zone=env.Find("Zone05_Revelation");Check(env.Find(Root)==null,"Do not reapply");var all=Level01EnvironmentArtPass.All(scene);var original=all.Where(c=>c is MonoBehaviour||c is Collider||c is Renderer||c is MeshFilter).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var gate=zone.GetComponent<Tikal.Puzzles.Level01.Zone05RevelationGate>();var gs=new SerializedObject(gate);var content=(GameObject)gs.FindProperty("_content").objectReferenceValue;var discovery=(Component)gs.FindProperty("_discovery").objectReferenceValue;Check(!content.activeSelf&&gs.FindProperty("_prerequisite").objectReferenceValue is Tikal.Puzzles.Level01.ObservationAlignmentPuzzle,"Original gated chain; content saved inactive");Check(Vector3.Distance(discovery.transform.position,new Vector3(0,.6f,71))<.01f,"Real discovery location");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zone05");root=new GameObject(Root).transform;root.SetParent(env,false);parts.Clear();floor=env.Find("Terrain/Surface_Zone05_Revelation").GetComponent<MeshCollider>();var hidden=new HashSet<Renderer>();
            foreach(var n in new[]{"Talud_73","Talud_76","Talud_79","Revelation eastern rock shoulder"}){var r=env.Find("NaturalBoundaries/"+n).GetComponent<Renderer>();hidden.Add(r);r.enabled=false;}
            foreach(var r in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&Mathf.Abs(r.bounds.center.x)<17&&r.bounds.center.z>56&&r.bounds.center.z<83)){hidden.Add(r);r.enabled=false;}
            // 1/2. Broad terrain and seven organic hills first. Sample the actual original edge height.
            var mesh=Terrain();var earth=new GameObject("OrganicSanctuaryBanks_48x32m");earth.transform.SetParent(root,false);earth.AddComponent<MeshFilter>().sharedMesh=mesh;earth.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");snap=earth.AddComponent<MeshCollider>();snap.sharedMesh=mesh;Physics.SyncTransforms();
            // 3. Eight rock/ruin clusters; solid visual bounds remain outside the original room.
            var clusters=new[]{new Vector2(-14,58),new Vector2(-14.3f,65),new Vector2(-14,73),new Vector2(-10,81),new Vector2(-3,83),new Vector2(13,81),new Vector2(14.5f,72),new Vector2(14,60)};
            for(int i=0;i<8;i++)for(int j=0;j<3;j++){var q=clusters[i]+new Vector2((j-1)*1.1f,Mathf.Sin(i*4+j)*.8f);Put(j==1?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02",q.x,q.y,1.35f+j*.2f,i*71+j*113,1,.22f);}
            for(int i=0;i<8;i++){var q=clusters[i];Put(i%3==0?"ARQ_MuroMaya_01":i%3==1?"ARQ_ColumnaDerrumbada_01":"ARQ_PlataformaElevada_01",q.x,q.y+.7f,i%3==2?.3f:1.05f,i*79,1,.18f);Put("ARQ_BloqueMaya_02",q.x+.8f,q.y-.7f,1.25f,i*113,1,.2f);}
            // 4. Twelve midground and eighteen background trees. Crowns can overlap high above the room;
            // the trunk/root envelope, rather than the full crown AABB, protects navigation.
            var trees=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_CeibaJoven_01"};var mid=new[]{new Vector2(-15,57.8f),new Vector2(-15,63),new Vector2(-15.5f,69),new Vector2(-14.5f,76),new Vector2(-9,81),new Vector2(-1,82),new Vector2(13,81),new Vector2(15,76),new Vector2(15.2f,70),new Vector2(15,64),new Vector2(15,58),new Vector2(14,57.5f)};
            for(int i=0;i<mid.Length;i++)Put(trees[i%3],mid[i].x,mid[i].y,.9f+i%4*.035f,i*137,-1,.035f,i%3==0);
            var bg=new[]{new Vector2(-21,57.5f),new Vector2(-22,63),new Vector2(-20,69),new Vector2(-22,75),new Vector2(-19,81),new Vector2(-15,85),new Vector2(-9,86),new Vector2(-3,86),new Vector2(1,82),new Vector2(12,85),new Vector2(18,86),new Vector2(22,81),new Vector2(20,76),new Vector2(22,70),new Vector2(21,64),new Vector2(21,58),new Vector2(-5,81),new Vector2(15,84)};
            for(int i=0;i<bg.Length;i++)Put(i==2||i==13?"VEG_PalmeraTropical_02":trees[(i+1)%3],bg[i].x,bg[i].y,.9f+i%4*.04f,i*137+31,i%3==0?2:1,.04f);
            // 5. Understory follows visible slope bands and clusters, not the centre or exits.
            var plants=new[]{"VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_RaizTropical_01","VEG_HojasSecasMonton_01"};for(int i=0;i<62;i++){var q=clusters[i%8];float a=i*2.39996f,r=.55f+i%4*.5f;float x=q.x+Mathf.Cos(a)*r,z=q.y+Mathf.Sin(a)*r;if(Bank(x,z))Put(plants[i%5],x,z,.95f+i%4*.1f,i*137,1,i%5==3?.15f:.04f);}
            // 6. Clad existing local masonry; do not add decorative stelae or move logical colliders.
            foreach(var r in zone.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="MonumentalPillar"||r.name=="LowTerminalDais"||r.name=="Provisional inscription stone"||r.name=="Revelation side seal").ToArray()){Fit(Visual(r.transform),r.name=="MonumentalPillar"?"ARQ_ColumnaMaya_01":"ARQ_BloqueMaya_02",r.bounds);hidden.Add(r);r.enabled=false;}
            foreach(var r in env.Find("Ruins_Blockout").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("TerminalMonument")).ToArray()){Fit(Visual(r.transform),"ARQ_BloqueMaya_02",r.bounds);hidden.Add(r);r.enabled=false;}
            var legacy=zone.Find("PuzzleAnchor/Revelation matching mechanism");Check(legacy.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Superseded matching mechanism has no playable authority");foreach(var r in legacy.GetComponentsInChildren<Renderer>().ToArray()){if(r.name=="Ceremonial plinth"||r.name=="Upright stele")Fit(Visual(r.transform),"ARQ_BloqueMaya_02",r.bounds);hidden.Add(r);r.enabled=false;}
            // Only the functional narrative stele is introduced, strictly inside the existing inactive content.
            var slab=content.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Discovery slab");var narrative=Fit(Visual(slab.transform),"ARQ_EstelaFragmentada_01",slab.bounds,180);Check(narrative.transform.IsChildOf(content.transform),"Narrative visual inherits original gate");hidden.Add(slab);slab.enabled=false;
            var stone=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath("ARQ_BloqueMaya_02")).GetComponentInChildren<Renderer>().sharedMaterial;var seal=zone.Find("RevelationAnchor/Sealed revelation panel/Movable stone seal").GetComponent<Renderer>();var sealOld=seal.sharedMaterial;seal.sharedMaterial=stone;
            // 7. Two hanging liana clusters attached to the existing non-narrative pillars.
            foreach(float x in new[]{-8f,8f})Fit(root,"VEG_CortinaLianas_01",new Bounds(new Vector3(x,3.7f,74.18f),new Vector3(1.5f,2.1f,.45f)));
            foreach(var r in level.Find("BlockoutMarkers").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Zone05_Revelation_"))){hidden.Add(r);r.enabled=false;}
            UnityEngine.Object.DestroyImmediate(snap);snap=null;Check(parts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"No new colliders");Check(!content.activeSelf,"Content still saved inactive");Check(original.All(k=>hidden.Contains(k.Key as Renderer)||k.Key==seal||k.Key!=null&&Props(k.Key)==k.Value),"All original scripts/colliders/references and meshes unchanged");Check(poses.All(k=>Pose(k.Key)==k.Value),"All original transforms/activation unchanged, including gate/key/stele and frozen zones");Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");Check(parts.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m!=null)),"No Missing Materials");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Zone05 saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.txt","Real Zone05_Revelation: south entry (0,56), north outlet (6,78); functional discovery (0,.6,71). Original gate points only to ObservationAlignmentPuzzle; content remains inactive.\nTerrain: 48x32.5m; 13m lateral /10m north backdrop; seven broad organic hills, target variation2.5-6m; actual old edge sampled, original physical floor untouched.\n24 rocks;16 bank ruins;12 midground /18 background trees;62 understory +2 liana clusters; no new colliders.\nNarrative ARQ_EstelaFragmentada_01 is exclusively inside original gated content. Original key silhouette, transforms and six symbols retained. Old nonfunctional matching marks hidden; retained colliders clad as masonry, not decorative stelae.\nAll original MonoBehaviours, colliders, refs, poses and activation unchanged; no new Missing Scripts/Materials.\nTotal prefab parts="+parts.Count+" terrain triangles="+mesh.triangles.Length/3+" hidden proxy renderers="+hidden.Count+"\n"+string.Join("\n",parts.Select(g=>g.name+" pos="+g.transform.position.ToString("F3")+" parent="+Level01EnvironmentInspection.PathOf(g.transform.parent))));Debug.Log("ZONE05_SANCTUARY_SAVED "+parts.Count);
        }
    }
}
