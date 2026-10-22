using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class GoldenSliceArtPass
    {
        public const string Evidence="../_Level01Environment_20261021/GoldenSlice";
        public const string RootName="ENV_GoldenSlice";
        static GoldenSliceArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            foreach(string action in new[]{"Probe","Apply"})
            {
                string request="Temp/GoldenSlice"+action+".request";
                if(!File.Exists(request))continue;File.Delete(request);Directory.CreateDirectory(Evidence);
                try{if(action=="Probe")Probe();else Apply();}
                catch(Exception e){File.WriteAllText(Evidence+"/"+action+"Error.txt",e.ToString());Debug.LogException(e);}
            }
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Golden Slice: "+message);}
        public static Bounds VisualBounds(GameObject go)
        {
            var group=go.GetComponentInChildren<LODGroup>();
            var rs=group!=null?group.GetLODs()[0].renderers:go.GetComponentsInChildren<Renderer>(true);
            Require(rs.Length>0,"Renderers "+go.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        public static bool Ground(float x,float z,out RaycastHit hit)
        {
            // Explicit authority whitelist: original ground and natural collision shell only.
            var hits=Physics.RaycastAll(new Vector3(x,50,z),Vector3.down,100,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.normal.y>.25f && h.collider is MeshCollider &&
                    h.collider.transform.parent!=null &&
                    (h.collider.transform.parent.name=="Terrain"||h.collider.transform.parent.name=="NaturalBoundaries"))
                .OrderBy(h=>h.collider.transform.parent.name=="Terrain"?0:1).ThenBy(h=>h.distance).ToArray();
            hit=hits.FirstOrDefault();return hits.Length>0;
        }
        static void Probe()
        {
            Require(!EditorSceneManager.GetActiveScene().isDirty,"Save active scene first");
            var scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);Physics.SyncTransforms();
            var names=new[]{"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_ArbolTropical_04","VEG_CeibaJoven_01","VEG_PalmeraTropical_02","VEG_ArbustoTropical_02","VEG_ArbustoTropical_03","VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_HojasSecasMonton_01","ROC_RocaMusgo_02","ROC_RocaPlana_01","ROC_RocaTropical_01","ROC_RocaFragmentada_01","ARQ_ArcoMaya_01","ARQ_ColumnaMaya_01","ARQ_ColumnaDerrumbada_01","ARQ_EstelaFragmentada_01","ARQ_PisoPiedraMaya_01","ARQ_BloqueMaya_02","ARQ_EscombrosMaya_01"};
            var lines=new List<string>();
            foreach(string name in names){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(name));var go=UnityEngine.Object.Instantiate(prefab);var b=VisualBounds(go);lines.Add(name+" size="+b.size.ToString("F3")+" min="+b.min.ToString("F3")+" lods="+go.GetComponentInChildren<LODGroup>().lodCount);UnityEngine.Object.DestroyImmediate(go);}
            foreach(var r in Level01EnvironmentArtPass.All(scene).OfType<Renderer>().Where(r=>r.bounds.center.x<17&&r.bounds.center.x> -17&&r.bounds.center.z<28&&r.bounds.center.z> -24))
                if(!r.transform.name.StartsWith("VegetationMass"))lines.Add(Level01EnvironmentInspection.PathOf(r.transform)+" center="+r.bounds.center.ToString("F2")+" size="+r.bounds.size.ToString("F2"));
            File.WriteAllLines(Evidence+"/Probe.txt",lines);Debug.Log("GOLDEN_SLICE_PROBE_DONE");
        }
        [Serializable] public class Placement
        {
            public string name,asset,layer,surface; public Vector3 position; public float baseY,surfaceY,gap,burial;
        }
        [Serializable] public class Audit
        {
            public bool passed; public int pass,objects,trees,plants,rocks,architecture,background,floating,skipped,colliders;
            public string[] checks,skippedPositions;public Placement[] placements;
        }
        static Transform root;static readonly List<Placement> placed=new List<Placement>();static readonly List<string> skipped=new List<string>();
        static Transform Layer(string name){var t=root.Find(name);if(t!=null)return t;var go=new GameObject(name);go.transform.SetParent(root,false);return go.transform;}
        static GameObject Put(string asset,string name,string layer,float x,float z,float scale,float yaw=0,float burial=0,bool shadow=false)
        {
            if(!Ground(x,z,out var hit)){skipped.Add(name+" no valid surface at "+x+"/"+z);return null;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Require(prefab!=null,"Prefab exists "+asset);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Layer(layer));go.name=name;
            go.transform.SetPositionAndRotation(hit.point,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
            if(name.StartsWith("PerimeterMass_"))go.transform.localScale=new Vector3(scale*.75f,scale*1.9f,scale*1.4f);
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=VisualBounds(go);float depth=b.size.y*burial;
            go.transform.position+=Vector3.up*(hit.point.y-b.min.y-depth+(burial==0?.005f:0));
            var group=go.GetComponentInChildren<LODGroup>();
            if(layer=="4_BACKGROUND" && group!=null)
            {
                var source=group.GetLODs();var low=source.Skip(source.Length>2?source.Length-2:Math.Min(1,source.Length-1)).ToArray();
                group.SetLODs(low.Select((l,i)=>new LOD(i==0?.45f:.008f,l.renderers)).ToArray());group.fadeMode=LODFadeMode.None;group.RecalculateBounds();
                var active=low.SelectMany(l=>l.renderers).ToHashSet();foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.enabled=active.Contains(r);
                PrefabUtility.RecordPrefabInstancePropertyModifications(group);
            }
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            placed.Add(new Placement{name=name,asset=asset,layer=layer,surface=Level01EnvironmentInspection.PathOf(hit.transform),position=go.transform.position,baseY=VisualBounds(go).min.y,surfaceY=hit.point.y,gap=VisualBounds(go).min.y-hit.point.y,burial=burial});
            return go;
        }
        static void Surfaces(Scene scene,Transform env)
        {
            var functional=Level01SurfacePresentation.GameplayRenderers(Level01EnvironmentArtPass.All(scene));
            var soil=AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art+"Ground/MAT_Tutorial_ForestFloor.mat");
            var stone=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath("ARQ_MuroMaya_01")).GetComponentInChildren<Renderer>().sharedMaterial;
            foreach(var r in env.GetComponentsInChildren<Renderer>(true))
            {
                string path=Level01EnvironmentInspection.PathOf(r.transform);var b=r.bounds;
                bool selectedFloor=r.name=="Surface_SpawnArea"||r.name=="Surface_Entrance"||r.name=="Surface_Zone01_MuralFragments";
                bool originalEntry=path.Contains("/Entrance/")||path.Contains("/SpawnArea/")||path.Contains("/Zone01_MuralFragments/SolidBoundaries/");
                bool natural=path.Contains("/NaturalBoundaries/")&&((r.name.StartsWith("Talud_")&&int.TryParse(r.name.Substring(6),out int index)&&index<=25)||r.name=="GateRockShoulder"||r.name=="ApproachRockCut");
                bool proxy=path.Contains("/Vegetation_Blockout/")&&b.center.x<14&&b.center.x> -17&&b.center.z<27&&b.center.z> -24;
                bool ruin=path.Contains("/Ruins_Blockout/")&&r.name.StartsWith("Plaza");
                if(functional.Contains(r)||path.Contains("/SecretCommandTarget/"))continue;
                if(natural||proxy||ruin||path.Contains("/SpawnArea/ApproachFloorBand")||r.name=="ApproachBaffle"||(path.Contains("/Boundaries/")&&b.center.z<0)||path.Contains("/Zone01_MuralFragments/SolidBoundaries/"))r.enabled=false;
                else if(selectedFloor||originalEntry){if(r.name!="DoorMesh")r.sharedMaterial=selectedFloor?soil:stone;else {var entries=JsonUtility.FromJson<Level01SurfacePresentation.Assignments>(File.ReadAllText(Level01EnvironmentArtPass.Docs+"Level01EnvironmentOriginalMaterials.json")).entries;var original=entries.FirstOrDefault(e=>e.path==path);if(original!=null)r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(original.material);}}
                else continue;
                // Reuse only existing UV presentation copies relevant to this slice; collision mesh never changes.
                if(r.enabled){var f=r.GetComponent<MeshFilter>();if(f!=null){string uv=Level01SurfacePresentation.Folder+"/UV_"+GlobalObjectId.GetGlobalObjectIdSlow(f.transform)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(uv);if(mesh!=null)f.sharedMesh=mesh;}}
                if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            foreach(var r in env.parent.Find("BlockoutMarkers").GetComponentsInChildren<Renderer>())if(r.bounds.center.z<23&&Mathf.Abs(r.bounds.center.x)<10)r.enabled=false;
        }
        static void Apply()
        {
            Require(!Application.isBatchMode&&!EditorSceneManager.GetActiveScene().isDirty,"Interactive clean Edit Mode");
            int pass=SessionState.GetInt("Tikal.Golden.Pass",0)+1;Require(pass<=3,"Maximum three artistic passes");
            var scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);var env=scene.GetRootGameObjects().Single().transform.Find("Environment");
            Require(env.Find("ENV_Level01Art")==null,"Global placement removed through backed-up baseline restoration");
            var prior=env.Find(RootName);if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            var go=new GameObject(RootName);root=go.transform;root.SetParent(env,false);placed.Clear();skipped.Clear();
            foreach(string layer in new[]{"1_GROUND","2_MID","3_CANOPY","4_BACKGROUND"})Layer(layer);
            Surfaces(scene,env);Physics.SyncTransforms();
            const string ground="1_GROUND",mid="2_MID",canopy="3_CANOPY",background="4_BACKGROUND";
            // Door remains the hero: flank its intact facade without occupying the 3m command/interaction zone.
            Put("ARQ_ArcoMaya_01","Door_ArchaeologicalCrown",mid,0,.7f,1.45f,0,.03f,true);
            Put("ARQ_ColumnaMaya_01","Door_LeftColumn",mid,-3.1f,-.8f,1.5f,8,.08f,true);
            Put("ARQ_ColumnaMaya_01","Door_RightColumn",mid,3.4f,-.65f,1.35f,352,.08f,true);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<4;i++)
                {
                    float x=side*(4.5f+i*.9f),z=-1.1f+(i%2)*.4f;
                    Put("ARQ_BloqueMaya_02","Door_BrokenMasonry_"+side+"_"+i,mid,x,z,1.3f+i*.13f,i*23,.12f,true);
                    if(i%2==0)Put("VEG_HelechoTropical_02","Door_InvasiveFern_"+side+"_"+i,mid,x+side*.35f,z-.65f,.95f,i*67);
                }
            }
            // Preserve the original approach baffle collider: dress it as an irregular fallen stone ridge.
            for(int i=0;i<5;i++)Put(i%2==0?"ROC_RocaFragmentada_01":"ROC_RocaMusgo_02","Entry_FallenRidge_"+i,mid,-3.8f+i*2.5f,-6,1.50f+i%2*.15f,19+i*51,.17f,true);
            // Trees are vertical; terrain supplies elevation. No approximate fallback Y.
            var trees=new[]{new Vector3(-7.9f, .9f,-13.5f),new Vector3(7.2f,.95f,-9),new Vector3(-7.8f,.95f,1),new Vector3(7.5f,.95f,3),new Vector3(-7.8f,.95f,9),new Vector3(8,.95f,9),new Vector3(-8,.9f,20),new Vector3(8,.95f,20),new Vector3(1,1f,21),new Vector3(-4,.9f,-15)};
            string[] treeAssets={"VEG_ArbolTropical_02","VEG_ArbolTropical_04","VEG_ArbolTropical_03"};
            for(int i=0;i<trees.Length;i++)Put(treeAssets[i%3],"HeroCanopy_"+i,canopy,trees[i].x,trees[i].z,trees[i].y,i*43,0,i<6);
            Put("VEG_CeibaJoven_01","Plaza_Ceiba",canopy,7.5f,20,1.2f,24,0,true);
            Put("VEG_PalmeraTropical_02","Plaza_Palm",canopy,-8.6f,6,.95f,156);
            var distant=new[]{new Vector2(-7.7f,-15),new Vector2(7.7f,-14),new Vector2(-7.8f,-13),new Vector2(7.8f,-2),new Vector2(-8.5f,5),new Vector2(8.5f,6),new Vector2(-8.5f,14),new Vector2(8.5f,18),new Vector2(-7,21.5f),new Vector2(-4,21.5f),new Vector2(5,21.5f),new Vector2(8,21.5f),new Vector2(0,-15.5f),new Vector2(-6,-15),new Vector2(6,-15)};
            for(int i=0;i<distant.Length;i++)Put(treeAssets[(i+1)%3],"BackgroundForest_"+i,background,distant[i].x,distant[i].y,.9f+(i%3)*.07f,i*71);
            // Lower crowns make a continuous depth layer behind the open plaza without editing later zones.
            for(int i=0;i<5;i++)Put("VEG_CeibaJoven_01","BackgroundLowCrown_"+i,background,-7+i*3.5f,21.7f,.85f,i*57);
            Put("VEG_CeibaJoven_01","BackgroundEastLowCrown_0",background,8.7f,6.5f,.95f,95);
            Put("VEG_CeibaJoven_01","BackgroundEastLowCrown_1",background,8.7f,17.5f,.95f,162);
            foreach(int side in new[]{-1,1})for(int i=0;i<5;i++)Put("VEG_ArbustoTropical_03","ForestEdgeHedge_"+side+"_"+i,mid,side*8.65f,3.5f+i*4.2f,1.75f,i*61);
            foreach(int side in new[]{-1,1})
            {
                var zs=new[]{-14f,-9.5f,-3.2f,4f,8.5f,13f,17.5f,21f};
                for(int i=0;i<zs.Length;i++)
                {
                    float x=side*(i<3?7.95f:8.45f);Put("ROC_RocaFragmentada_01","PerimeterMass_"+side+"_"+i,mid,x,zs[i],1.3f,0,.18f);
                    Put("VEG_ArbustoTropical_03","PerimeterShrub_"+side+"_"+i,mid,x-side*.6f,zs[i]+1,1.15f,i*43);
                }
            }
            // Recessed organic rock/plant clusters replace visible geometric boundary wedges.
            var edges=new[]{new Vector2(-7,-13),new Vector2(7,-13),new Vector2(-7,-8),new Vector2(7,-3),new Vector2(-7,1),new Vector2(7,3),new Vector2(-8,6),new Vector2(8,7),new Vector2(-8,11),new Vector2(8,17),new Vector2(-8,20),new Vector2(-2,21),new Vector2(4,21),new Vector2(8,21)};
            string[] rocks={"ROC_RocaMusgo_02","ROC_RocaFragmentada_01","ROC_RocaTropical_01"};
            for(int i=0;i<edges.Length;i++)
            {
                var p=edges[i];Put(rocks[i%3],"BuriedEdgeRock_"+i,mid,p.x,p.y,1.35f+(i%3)*.3f,i*47,.1f+(i%3)*.04f);
                Put(i%3==0?"VEG_ArbustoTropical_02":"VEG_ArbustoTropical_03","EdgeUnderstory_"+i,mid,p.x+Mathf.Sign(p.x)*.35f,p.y+.65f,1.05f+(i%2)*.22f,i*59);
                Put(i%2==0?"VEG_HelechoTropical_02":"VEG_PlantaTropical_02","EdgeGroundPlant_"+i,ground,p.x-Mathf.Sign(p.x)*.7f,p.y-.6f,.75f+(i%3)*.1f,i*83);
            }
            // Plaza opens in the middle; ruin fragments and litter concentrate along peripheral transitions.
            Put("ARQ_ColumnaDerrumbada_01","Plaza_FallenColumn",mid,5.9f,7,1.2f,27,.12f,true);
            Put("ARQ_EstelaFragmentada_01","Plaza_ErodedStele",mid,5.5f,20,1.15f,345,.08f,true);
            for(int i=0;i<7;i++)
            {
                float x=i<4?6.4f: -2.5f+(i-4)*2.2f,z=i<4?9+i*2.4f:20;
                Put("ARQ_BloqueMaya_02","Plaza_RuinFragment_"+i,mid,x,z,1.1f+(i%3)*.2f,i*38,.18f,true);
                Put("VEG_HelechoTropical_02","Plaza_RuinFern_"+i,ground,x-.65f,z+.5f,.7f+(i%2)*.15f,i*67);
            }
            foreach(var p in new[]{new Vector2(-4,-12),new Vector2(-5,-4),new Vector2(3,-2),new Vector2(-2.5f,4),new Vector2(4.5f,8),new Vector2(3,17),new Vector2(-1,20)})
            {
                int i=placed.Count;Put("VEG_HojasSecasMonton_01","Path_LeafDeposit_"+i,ground,p.x,p.y,1.2f,i*31);
                Put("ROC_RocaPlana_01","Path_SunkenStone_"+i,ground,p.x+.8f,p.y+.6f,.6f,i*23,.18f);
            }
            foreach(var p in new[]{new Vector2(4,5),new Vector2(5,12),new Vector2(1,19)})Put("ARQ_PisoPiedraMaya_01","Plaza_BrokenPaving_"+placed.Count,ground,p.x,p.y,.8f,placed.Count*17,.18f);
            foreach(var p in new[]{new Vector2(-2.4f,8),new Vector2(2.6f,14),new Vector2(-2,18),new Vector2(-3.4f,-10)})Put("VEG_HojasSecasMonton_01","Path_ScatteredLitter_"+placed.Count,ground,p.x,p.y,.65f,placed.Count*37);
            Put("VEG_HelechoTropical_02","DoorFront_FernLeft",ground,-3.7f,-2.1f,.9f,47);
            Put("VEG_PlantaTropical_02","DoorFront_PlantRight",ground,3.8f,-2,.85f,283);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.55f,.56f);RenderSettings.ambientEquatorColor=new Color(.3f,.36f,.29f);RenderSettings.ambientGroundColor=new Color(.21f,.20f,.16f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=105;RenderSettings.fogColor=new Color(.45f,.54f,.50f);
            var sun=Level01EnvironmentArtPass.All(scene).OfType<Light>().Single(l=>l.type==LightType.Directional);sun.color=new Color(1,.93f,.8f);sun.intensity=1.3f;
            foreach(var r in Level01EnvironmentArtPass.All(scene).OfType<Renderer>())if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            Validate(scene,pass);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();SessionState.SetInt("Tikal.Golden.Pass",pass);
            File.WriteAllText(Evidence+"/Apply.txt","PASS "+pass+" saved "+placed.Count+" objects");Debug.Log("GOLDEN_SLICE_APPLY_PASS "+pass);
        }
        static void Validate(Scene scene,int pass)
        {
            Physics.SyncTransforms();var all=Level01EnvironmentArtPass.All(scene);var checks=new List<string>();
            void Check(bool ok,string msg){Require(ok,msg);checks.Add(msg);}
            Check(all.All(c=>c!=null),"No Missing Scripts");
            Check(root.childCount==4,"Exactly four jungle layers");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"Zero art colliders");Check(root.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Zero art gameplay scripts");
            var baseline=JsonUtility.FromJson<Level01EnvironmentArtPass.Baseline>(File.ReadAllText(Level01EnvironmentArtPass.Docs+"Level01EnvironmentProtection.json"));
            var ids=all.ToDictionary(c=>GlobalObjectId.GetGlobalObjectIdSlow(c).ToString());
            var method=typeof(TutorialEnvironmentArtPass).GetMethod("Properties",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Check(baseline.components.All(e=>ids.TryGetValue(e.id,out var c)&&(string)method.Invoke(null,new object[]{c})==e.value),"All original gameplay components and colliders unchanged");
            Check(baseline.transforms.All(e=>ids.TryGetValue(e.id,out var c)&&c is Transform t&&e.value==(t.parent==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(t.parent).ToString())+"|"+t.localPosition.ToString("F6")+"|"+t.localRotation.ToString("F6")+"|"+t.localScale.ToString("F6")),"All original transforms unchanged");
            Check(baseline.activeObjects.All(e=>ids.TryGetValue(e.id,out var c)&&((Transform)c).gameObject.activeSelf.ToString()==e.value),"All original activation unchanged");
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);checks.Add("Main Door / secret / checkpoint references valid");
            int floating=0;
            foreach(var p in placed)
            {
                var t=root.Find(p.layer+"/"+p.name);Check(t!=null,"Placement exists "+p.name);
                Check(Ground(t.position.x,t.position.z,out var hit),"Valid surface below "+p.name);p.baseY=VisualBounds(t.gameObject).min.y;p.surfaceY=hit.point.y;p.gap=p.baseY-p.surfaceY;
                if(p.gap>.03f)floating++;
                if(p.asset.StartsWith("VEG_Arbol")||p.asset.Contains("Ceiba")||p.asset.Contains("Palmera"))Check(Vector3.Dot(t.up,Vector3.up)>.9999f,"Tree vertical "+p.name);
            }
            Check(floating==0,"No asset base floating over 3cm");
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))Check(r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported),"Supported materials "+r.name);
            var report=new Audit{passed=true,pass=pass,objects=placed.Count,trees=placed.Count(p=>p.layer=="3_CANOPY"),background=placed.Count(p=>p.layer=="4_BACKGROUND"),plants=placed.Count(p=>p.asset.StartsWith("VEG_")&&p.layer!="3_CANOPY"&&p.layer!="4_BACKGROUND"),rocks=placed.Count(p=>p.asset.StartsWith("ROC_")),architecture=placed.Count(p=>p.asset.StartsWith("ARQ_")),floating=floating,skipped=skipped.Count,colliders=all.OfType<Collider>().Count(c=>c.enabled),placements=placed.ToArray(),checks=checks.ToArray(),skippedPositions=skipped.ToArray()};
            File.WriteAllText(Evidence+"/EditorValidation_Pass"+pass+".json",JsonUtility.ToJson(report,true));
        }
    }
}
