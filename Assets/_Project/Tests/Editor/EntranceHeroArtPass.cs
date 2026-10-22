using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class EntranceHeroArtPass
    {
        public const string Evidence="../_EntranceHero_20261022";
        public const string RootName="ENV_L01_Entrance_Hero";
        const string Materials="Assets/_Project/Art/Environment/Level01/EntranceHero";
        static Transform root;
        static readonly List<GameObject> instances=new List<GameObject>();
        static readonly List<GameObject> attached=new List<GameObject>();
        static readonly List<string> omitted=new List<string>();
        [Serializable] public class Result {public bool passed;public int pass,placements,retired,plants,trees,rocks,architecture,floating,colliders;public string[] assets,checks,omitted;}
        static EntranceHeroArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!File.Exists("Temp/EntranceHeroApply.request"))return;File.Delete("Temp/EntranceHeroApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException("Entrance hero: "+message);}
        static string Id(UnityEngine.Object obj)=>obj==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        static string TransformState(Transform t)=>Id(t.parent)+"|"+t.localPosition.ToString("F6")+"|"+t.localRotation.ToString("F6")+"|"+t.localScale.ToString("F6");
        static string Properties(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static Transform Group(string name){var t=root.Find(name);if(t!=null)return t;var go=new GameObject(name);go.transform.SetParent(root,false);return go.transform;}
        static Bounds BoundsOf(GameObject go)=>GoldenSliceArtPass.VisualBounds(go);
        static GameObject Put(string asset,string name,string group,float x,float z,float scale,float yaw=0,float burial=0,bool shadow=false,bool background=false)
        {
            Check(z<=0,"Placement confined to entrance "+name);
            if(!GoldenSliceArtPass.Ground(x,z,out var hit)){omitted.Add(name+" no ground at "+x+"/"+z);return null;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Existing prefab "+asset);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Group(group));go.name=name;go.transform.SetPositionAndRotation(hit.point,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            Check(go.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Purely visual prefab "+asset);
            var b=BoundsOf(go);go.transform.position+=Vector3.up*(hit.point.y-b.min.y-b.size.y*burial+.003f);
            var lod=go.GetComponentInChildren<LODGroup>();
            if(background&&lod!=null)
            {
                var levels=lod.GetLODs();var low=levels[Math.Min(1,levels.Length-1)];
                lod.SetLODs(new[]{new LOD(.008f,low.renderers)});lod.fadeMode=LODFadeMode.None;
                var used=low.renderers.ToHashSet();foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);
                PrefabUtility.RecordPrefabInstancePropertyModifications(lod);
            }
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            instances.Add(go);return go;
        }
        static Material LocalMaterial(string name,Material source,Vector2 tiling,Color color)
        {
            string path=Materials+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source){name=name};AssetDatabase.CreateAsset(material,path);}
            foreach(string property in material.GetTexturePropertyNames())if(material.GetTexture(property)!=null)material.SetTextureScale(property,tiling);
            material.SetColor("_BaseColor",color);material.enableInstancing=true;return material;
        }
        static void Hanging(string name,float x,float widthScale,Transform facade)
        {
            var go=Put("VEG_CortinaLianas_01",name,"DoorFraming",x,-.51f,widthScale,0);
            if(go==null)return;var b=BoundsOf(go);float top=facade.GetComponent<Renderer>().bounds.max.y;
            go.transform.position+=Vector3.up*(top-b.max.y);attached.Add(go);
        }
        static void Apply()
        {
            Check(!Application.isBatchMode,"Interactive editor only");Check(!EditorSceneManager.GetActiveScene().isDirty,"Current scene must be saved");
            var scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);var all=Level01EnvironmentArtPass.All(scene);
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");var old=env.Find(GoldenSliceArtPass.RootName);Check(old!=null,"Keep current Golden Slice and plaza");
            var prior=env.Find(RootName);
            // Snapshot gameplay and every retained visual/placement behind the door before local mutation.
            var protectedComponents=all.Where(c=>c is MonoBehaviour||c is Collider).Where(c=>prior==null||!c.transform.IsChildOf(prior)).ToDictionary(Id,Properties);
            var protectedTransforms=all.OfType<Transform>().Where(t=>prior==null||!t.IsChildOf(prior)).ToDictionary(Id,TransformState);
            // Rear renderers include native geometry with origin zero: use hierarchy plus center for those meshes.
            var rear=all.OfType<Renderer>().Where(r=>!(prior!=null&&r.transform.IsChildOf(prior))&&
                (Level01EnvironmentInspection.PathOf(r.transform).Contains("/Zone0")||Level01EnvironmentInspection.PathOf(r.transform).Contains("/Ruins_Blockout/")||r.bounds.center.z>2f)).ToArray();
            var rearState=rear.ToDictionary(Id,r=>Properties(r)+"|"+r.gameObject.activeSelf);
            string renderSettings=EditorJsonUtility.ToJson(Unsupported.GetRenderSettings());
            if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            int retired=0;
            foreach(var layer in old.Cast<Transform>())foreach(var t in layer.Cast<Transform>())
            {
                if(t.position.z<-.01f&&t.gameObject.activeSelf){t.gameObject.SetActive(false);retired++;}
            }
            root=new GameObject(RootName).transform;root.SetParent(env,false);instances.Clear();attached.Clear();omitted.Clear();
            foreach(string group in new[]{"Path","LeftJungle","RightJungle","DoorFraming","BackgroundForest","Relief","SetDressing"})Group(group);
            Physics.SyncTransforms();
            // Local material variants reuse existing PBR textures. Other ground materials remain unchanged.
            var floor=env.Find("Terrain/Surface_SpawnArea").GetComponent<Renderer>();
            var earth=AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art+"Ground/MAT_Tutorial_ForestFloor.mat");
            floor.sharedMaterial=LocalMaterial("MAT_L01_Entrance_Soil",earth,new Vector2(1.75f,1.75f),new Color(1.04f,1.02f,.94f,1));
            var wall=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath("ARQ_MuroMaya_01")).GetComponentInChildren<Renderer>().sharedMaterial;
            var door=env.Find("Entrance/EntranceDoor_Blockout/Pivot/DoorMesh").GetComponent<Renderer>();door.sharedMaterial=LocalMaterial("MAT_L01_Entrance_DoorStone",wall,Vector2.one,new Color(.44f,.49f,.40f,1));PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            // Existing gate and crown remain; low debris leaves its dark central leaf clearly visible.
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<3;i++)
                {
                    float x=side*(3.1f+i*1.45f);float z=-.65f-i*.18f;
                    Put("ARQ_BloqueMaya_02","Gate_BuriedBlock_"+side+"_"+i,"DoorFraming",x,z,1.1f+i*.2f,side*9+i*23,.16f,true);
                    Put("VEG_HelechoTropical_02","Gate_Fern_"+side+"_"+i,"DoorFraming",x+side*.4f,z-.8f,.9f+i*.12f,30+i*65);
                }
            }
            var facades=env.Find("Entrance").Cast<Transform>().Where(t=>t.name=="Facade").ToArray();
            Hanging("Gate_HangingVines_Left",-5.2f,.95f,facades[0]);Hanging("Gate_HangingVines_Right",4.8f,1.05f,facades[1]);
            Put("ARQ_ColumnaMaya_01","Gate_ErodedPier_Left","DoorFraming",-3.1f,-.45f,1.15f,8,.12f,true);
            Put("ARQ_ColumnaDerrumbada_01","Gate_FallenPier_Right","DoorFraming",5,-2,1,14,.14f);
            // Dress the original baffle as a low archaeological terrace, avoiding a new rock wall in the focal cone.
            for(int i=0;i<6;i++)
            {
                float x=-3.6f+i*1.8f,z=-6.05f+(i%2)*.14f;
                Put("ARQ_BloqueMaya_02","LowTerrace_Block_"+i,"Relief",x,z,1.12f+i%3*.13f,i*23,.18f);
                Put(i%2==0?"VEG_HelechoTropical_02":"VEG_ArbustoTropical_02","LowTerrace_Plant_"+i,"SetDressing",x+.3f,z+.7f,.58f+i%2*.1f,i*54);
            }
            // Monumental foreground and receding trees, all vertical and snapped to original ground.
            Put("VEG_CeibaTropical_01","Hero_Ceiba_Left","LeftJungle",-7.5f,-14.4f,.72f,28,0,true);
            Put("VEG_ArbolTropical_04","Hero_Tropical_Right","RightJungle",7.5f,-12.7f,.8f,146,0,true);
            Put("VEG_ArbolTropical_03","Mid_Tree_Left","LeftJungle",-7.8f,-1.3f,.72f,54);
            Put("VEG_CeibaJoven_01","Mid_Ceiba_Right","RightJungle",6.4f,-4.1f,.9f,262);
            Put("VEG_PalmeraTropical_02","Mid_Palm_Right","RightJungle",7.5f,-1.8f,1.1f,80);
            var backdrop=new[]{new Vector2(-6.5f,-15.5f),new Vector2(-2.8f,-15.5f),new Vector2(2,-15.5f),new Vector2(6.4f,-15.5f),new Vector2(7.7f,-8),new Vector2(7.7f,-.9f),new Vector2(-7.8f,-14.5f)};
            for(int i=0;i<backdrop.Length;i++)Put(i%2==0?"VEG_ArbolTropical_02":"VEG_ArbolTropical_03","DeepForest_"+i,"BackgroundForest",backdrop[i].x,backdrop[i].y,.9f+i%3*.07f,i*67,0,false,true);
            // Dense lateral masses follow the actual L-shaped walk route; its central sweep remains clear.
            var clusters=new[]{new Vector2(-3.2f,-12.7f),new Vector2(-6,-14.5f),new Vector2(3.4f,-12.7f),new Vector2(6,-13.5f),new Vector2(5,-10),new Vector2(7,-8),new Vector2(7,-4.5f),new Vector2(6.5f,-2.4f),new Vector2(-7.65f,-1.8f),new Vector2(-3.2f,-4.4f),new Vector2(5.5f,-8.5f),new Vector2(4,-4.8f)};
            for(int i=0;i<clusters.Length;i++)
            {
                var p=clusters[i];string group=p.x<0?"LeftJungle":"RightJungle";
                Put(i%3==0?"ROC_RocaMusgo_02":"ROC_RocaFragmentada_01","BuriedRock_"+i,"Relief",p.x,p.y,.65f+(i%3)*.14f,i*59,.17f);
                Put("VEG_ArbustoTropical_03","Understory_"+i,group,p.x+.5f,p.y+.55f,.95f+i%2*.2f,i*61);
                Put(i%2==0?"VEG_PlantaTropical_02":"VEG_HelechoTropical_02","BroadLeaf_"+i,group,p.x-.65f,p.y+.3f,.8f+i%3*.1f,i*77);
                Put("VEG_HelechoTropical_02","LowFern_"+i,group,p.x-.25f,p.y-.7f,.68f+i%2*.15f,i*43);
            }
            // Close side/rear horizon with low crowns and no new collider or heavy shadow layer.
            for(int i=0;i<6;i++)
            {
                float x=-6.7f+i*2.65f;
                Put("VEG_ArbustoTropical_03","RearHedge_"+i,"BackgroundForest",x,-15.1f,1.8f,i*51,0,false,true);
            }
            for(int i=0;i<5;i++)
            {
                Put("VEG_ArbustoTropical_03","RightHedge_"+i,"RightJungle",7.5f,-12.8f+i*2.8f,1.6f,i*39,0,false,true);
            }
            // Outside the existing left bypass, bridge the terrain edge with buried stone and a dense bank.
            foreach(var p in new[]{new Vector2(-7.97f,-13),new Vector2(-7.97f,-9.5f),new Vector2(-7.97f,-5)})
            {
                int n=instances.Count;
                Put("ROC_RocaMusgo_02","LeftBank_LowerRock_"+n,"Relief",p.x,p.y,.9f,n*31,.18f);
                Put("VEG_ArbustoTropical_03","LeftBank_DeepBush_"+n,"LeftJungle",p.x,p.y+.3f,1.1f,n*41,0,false,true);
                Put("VEG_PlantaTropical_02","LeftBank_DeepLeaf_"+n,"LeftJungle",p.x,p.y-.7f,.9f,n*67);
            }
            Put("VEG_CeibaJoven_01","LeftBank_LowerCrown","BackgroundForest",-7.97f,-12.5f,.8f,126,0,false,true);
            // Stone/leaf edges suggest a four-metre visual path, with the original narrower collider bypass retained.
            var litter=new[]{new Vector2(-3,-12.1f),new Vector2(2.8f,-11.9f),new Vector2(-4.5f,-11.6f),new Vector2(-7.6f,-8),new Vector2(-4.4f,-4.8f),new Vector2(-2.4f,-1.2f),new Vector2(2.6f,-1.5f)};
            for(int i=0;i<litter.Length;i++)
            {
                var p=litter[i];Put("VEG_HojasSecasMonton_01","Path_LeafEdge_"+i,"Path",p.x,p.y,.65f+i%2*.2f,i*53);
                Put("ROC_RocaPlana_01","Path_SmallStone_"+i,"Path",p.x+.25f,p.y+.3f,.3f+i%3*.08f,i*71,.18f);
            }
            Put("VEG_RaizTropical_01","Ceiba_ExposedRoot","SetDressing",-6.8f,-13.1f,.65f,70,.03f);
            Put("VEG_RaizTropical_01","Right_ExposedRoot","SetDressing",6.9f,-10.8f,.7f,205,.03f);
            // Verify protected state without visiting or authoring any later zone.
            all=Level01EnvironmentArtPass.All(scene);var ids=all.ToDictionary(Id);
            Check(protectedComponents.All(e=>ids.TryGetValue(e.Key,out var c)&&Properties(c)==e.Value),"Gameplay/colliders unchanged");
            Check(protectedTransforms.All(e=>ids.TryGetValue(e.Key,out var c)&&TransformState((Transform)c)==e.Value),"All retained local transforms unchanged");
            Check(rearState.All(e=>ids.TryGetValue(e.Key,out var c)&&Properties(c)+"|"+c.gameObject.activeSelf==e.Value),"Rear plaza and later visuals/activation unchanged");
            Check(EditorJsonUtility.ToJson(Unsupported.GetRenderSettings())==renderSettings,"Scene lighting/fog unchanged");
            Check(root.GetComponentsInChildren<Component>(true).All(c=>c!=null),"No Missing Scripts in touched hierarchy");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new art collider");
            int floating=0;foreach(var item in instances.Where(i=>!attached.Contains(i)))
            {
                Check(GoldenSliceArtPass.Ground(item.transform.position.x,item.transform.position.z,out var hit),"Ground below "+item.name);
                if(BoundsOf(item).min.y-hit.point.y>.03f)floating++;
            }
            Check(floating==0,"No floating grounded assets over 3cm");
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            int pass=SessionState.GetInt("Tikal.EntranceHero.Pass",0)+1;SessionState.SetInt("Tikal.EntranceHero.Pass",pass);
            var names=instances.Select(i=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(i)).Distinct().ToArray();
            var result=new Result{passed=true,pass=pass,placements=instances.Count,retired=retired,plants=instances.Count(i=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(i).Contains("VEG_")&&!i.name.Contains("Tree")&&!i.name.Contains("Ceiba")&&!i.name.Contains("Palm")&&!i.name.Contains("DeepForest")),trees=instances.Count(i=>i.name.Contains("Tree")||i.name.Contains("Ceiba")||i.name.Contains("Palm")||i.name.Contains("DeepForest")),rocks=instances.Count(i=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(i).Contains("ROC_")),architecture=instances.Count(i=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(i).Contains("ARQ_")),floating=floating,colliders=all.OfType<Collider>().Count(c=>c.enabled),assets=names,omitted=omitted.ToArray(),checks=new[]{"Scene saved","Rear visuals/transforms/activation unchanged","Gameplay and all collider serialized properties unchanged","Lighting unchanged","No Missing Scripts","Zero new colliders","No grounded asset gap over 3cm","Door/secret/checkpoint references unchanged"}};
            File.WriteAllText(Evidence+"/EditorValidation_Pass"+pass+".json",JsonUtility.ToJson(result,true));Debug.Log("ENTRANCE_HERO_SAVED_PASS "+pass);
        }
    }
}
