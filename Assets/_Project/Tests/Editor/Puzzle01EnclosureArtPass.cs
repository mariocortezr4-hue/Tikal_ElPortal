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
    // Explicit request only. This authoring tool never runs on scene load alone.
    [InitializeOnLoad]
    public static class Puzzle01EnclosureArtPass
    {
        public const string Evidence="../_Puzzle01Art_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Puzzle01";
        const string RootName="ENV_L01_Puzzle01_Enclosure";
        const int NX=35,NZ=29;
        static Transform root;static Vector3[] land;
        static readonly List<GameObject> placed=new List<GameObject>();
        [Serializable] public class Report {public bool passed;public int placements,colliders,newColliders,floating,terrainTriangles;public string[] checks,assets;}
        static Puzzle01EnclosureArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Puzzle01EnclosureApply.request"))return;
            File.Delete("Temp/Puzzle01EnclosureApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Puzzle01 art: "+message);}
        static string Id(UnityEngine.Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{c});
        static string FloorProps(Component c)=>string.Join("\n",Props(c).Split('\n').Where(l=>!l.StartsWith("m_Materials")));
        static string Pose(Transform t)=>Id(t.parent)+t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static float OriginalFloor(float x,float z)
        {
            return Physics.RaycastAll(new Vector3(Mathf.Clamp(x,-8.8f,8.8f),30,Mathf.Clamp(z,2.1f,21.9f)),Vector3.down,60,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider is MeshCollider&&h.collider.transform.parent!=null&&h.collider.transform.parent.name=="Terrain")
                .Select(h=>h.point.y).DefaultIfEmpty(.4f).Max();
        }
        static float Height(float x,float z)
        {
            // Visual skirt, with the existing traversable plaza and eastern connector left open.
            float side=Mathf.SmoothStep(0,1,Mathf.Max(0,Mathf.Abs(x)-8.8f)/3.8f);
            float back=Mathf.SmoothStep(0,1,Mathf.Max(0,z-19.5f)/4.5f);
            float floor=OriginalFloor(x,z);
            float h=floor+Mathf.Max(side,back)*(3.3f+.65f*Mathf.Sin(x*.36f+z*.29f)+.38f*Mathf.Cos(z*.62f-x*.21f));
            float exit=Mathf.SmoothStep(0,1,Mathf.Abs(z-12)/3.8f);
            if(x>8.7f&&x<17)h=Mathf.Lerp(floor,h,exit);
            float edge=Mathf.Max(Mathf.Abs(x)-14,z-27,0);
            return h-Mathf.SmoothStep(0,1,edge/3)*3.7f;
        }
        static float Surface(float x,float z)
        {
            float gx=Mathf.Clamp(x+17,0,NX-1.001f),gz=Mathf.Clamp(z-2,0,NZ-1.001f);int ix=(int)gx,iz=(int)gz;float u=gx-ix,v=gz-iz;
            float a=land[iz*NX+ix].y,b=land[iz*NX+ix+1].y,c=land[(iz+1)*NX+ix].y,d=land[(iz+1)*NX+ix+1].y;
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static Transform Group(string name)
        {var t=root.Find(name);if(t!=null)return t;var g=new GameObject(name);g.transform.SetParent(root,false);return g.transform;}
        static int Relief()
        {
            land=new Vector3[NX*NZ];var uv=new Vector2[land.Length];var tris=new List<int>();
            for(int z=0;z<NZ;z++)for(int x=0;x<NX;x++){int i=z*NX+x;float wx=x-17,wz=z+2;land[i]=new Vector3(wx,Height(wx,wz),wz);uv[i]=new Vector2(wx,wz)*.28f;}
            for(int z=0;z<NZ-1;z++)for(int x=0;x<NX-1;x++)
            {
                float cx=x-16.5f,cz=z+2.5f;
                if(Mathf.Abs(cx)<9&&cz<20)continue; // no second floor over the playable plaza
                if(cx>=9&&cz>8&&cz<16)continue; // retain the eastern exit corridor
                if(cz<5)continue; // never touches the saved entrance composition
                int a=z*NX+x,b=a+1,c=a+NX,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});
            }
            string path=Folder+"/MESH_Puzzle01_OrganicBanks.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name="MESH_Puzzle01_OrganicBanks"};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.vertices=land;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            string mp=Folder+"/MAT_Puzzle01_Earth.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(mat==null){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/EntranceHero/MAT_Entrance_ReliefEarth.mat")){name="MAT_Puzzle01_Earth"};AssetDatabase.CreateAsset(mat,mp);}
            mat.SetColor("_BaseColor",new Color(.76f,.82f,.62f,1));mat.enableInstancing=true;EditorUtility.SetDirty(mat);
            var go=new GameObject("OrganicBanks_VisualOnly");go.transform.SetParent(Group("Relief"),false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
            return tris.Count/3;
        }
        static GameObject Put(string asset,string name,string group,float x,float z,float scale,float yaw,float bury=0,bool distant=false,bool shadow=false,float lift=0,Vector3? dimensions=null)
        {
            Check(x>=-17&&x<=17&&z>=5&&z<=29,"Puzzle01 local scope");
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Existing prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(p,Group(group));g.name=name;g.transform.SetPositionAndRotation(new Vector3(x,Surface(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=dimensions??Vector3.one*scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float support=Surface(x,z);
            // On a slope, center-only grounding can leave the downhill rock edge visibly suspended.
            // Embed against the lowest footprint sample; keep the original traversable collider untouched.
            if(asset.StartsWith("ROC_"))support=Mathf.Min(support,Surface(b.min.x,b.min.z),Surface(b.max.x,b.min.z),Surface(b.min.x,b.max.z),Surface(b.max.x,b.max.z));
            g.transform.position+=Vector3.up*(support-b.min.y-b.size.y*bury+.002f+lift);
            var lod=g.GetComponentInChildren<LODGroup>();
            if(distant&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.008f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadow?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            placed.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Correct saved Level01 scene in Edit Mode");
            Check(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git","branch --show-current"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true}).StandardOutput.ReadToEnd().Trim()=="feature/level1-environment-art-pass","Authorized branch");
            Physics.SyncTransforms();var env=scene.GetRootGameObjects().Single().transform.Find("Environment");var prior=env.Find(RootName);
            var originals=Level01EnvironmentArtPass.All(scene).Where(c=>prior==null||!c.transform.IsChildOf(prior)).ToArray();
            var plazaFloor=env.Find("Terrain/Surface_Zone01_MuralFragments").GetComponent<Renderer>();
            string floorState=FloorProps(plazaFloor);
            var protectedComponents=originals.Where(c=>c!=plazaFloor&&(c is Collider||c is MonoBehaviour||c is Renderer||c is MeshFilter||c is Light)).ToDictionary(Id,Props);
            // One earlier plaza dressing tree intersects the original TPS camera near the mural.
            // Preserve the object and its transform, retiring its visual instance instead of deleting it.
            var retirementPaths=new[]{"3_CANOPY/HeroCanopy_4","2_MID/ForestEdgeHedge_-1_1","2_MID/PerimeterShrub_-1_4","2_MID/EdgeUnderstory_8","1_GROUND/EdgeGroundPlant_8","2_MID/PerimeterMass_-1_4","2_MID/BuriedEdgeRock_8"};
            var retired=retirementPaths.Select(p=>env.Find("ENV_GoldenSlice/"+p)).Where(t=>t!=null).ToArray();
            var protectedTransforms=originals.OfType<Transform>().Where(t=>!retired.Contains(t)).ToDictionary(Id,Pose);
            var retiredPoses=retired.ToDictionary(t=>t,t=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6"));
            string lighting=EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings"));
            if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);root=new GameObject(RootName).transform;root.SetParent(env,false);placed.Clear();
            foreach(var t in retired){Check(t.GetComponentsInChildren<Collider>(true).Length==0,"Retired dressing has no gameplay collider");t.gameObject.SetActive(false);}
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Puzzle01");
            string floorMaterialPath=Folder+"/MAT_Puzzle01_PlazaGround.mat";
            var groundMaterial=AssetDatabase.LoadAssetAtPath<Material>(floorMaterialPath);
            if(groundMaterial==null){groundMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art+"Ground/MAT_Tutorial_ForestFloor.mat")){name="MAT_Puzzle01_PlazaGround"};AssetDatabase.CreateAsset(groundMaterial,floorMaterialPath);}
            foreach(string p in groundMaterial.GetTexturePropertyNames())if(groundMaterial.GetTexture(p)!=null)groundMaterial.SetTextureScale(p,Vector2.one*2);
            groundMaterial.SetColor("_BaseColor",new Color(1.05f,1.06f,.95f,1));groundMaterial.SetFloat("_Smoothness",.12f);groundMaterial.enableInstancing=true;EditorUtility.SetDirty(groundMaterial);plazaFloor.sharedMaterial=groundMaterial;
            int triangles=Relief();
            string[] trees={"VEG_ArbolTropical_03","VEG_ArbolTropical_04","VEG_ArbolTropical_02"};
            // The bank behind the mural is the dominant enclosure, progressively layered into the canopy.
            for(int i=0;i<8;i++)
            {
                float x=-13.5f+i*3.9f,z=24.2f+(i%3)*1.1f;
                Put(trees[i%3],"RearCanopy_"+i,"Canopy",x,z,.78f+i%2*.12f,i*61,0,true);
                Put("VEG_CeibaJoven_01","RearMiddleCrown_"+i,"Background",x+.7f,z-1.2f,.67f,i*47,0,true);
                Put("VEG_ArbustoTropical_03","BankCrownShrub_"+i,"Understory",x,z-2.3f,1.25f+i%2*.18f,i*71,0,true);
            }
            foreach(int side in new[]{-1,1})for(int i=0;i<5;i++)
            {
                float x=side*(12.1f+i%2*1.2f),z=7+i*3.8f;
                if(side>0&&z>8&&z<16)continue;
                Put(trees[(i+1)%3],"SideCanopy_"+side+"_"+i,"Canopy",x,z,.85f,i*83,0,true);
                Put("VEG_ArbustoTropical_03","SideMass_"+side+"_"+i,"Understory",x-side*1.2f,z,1.45f,i*39,0,true);
                Put("ROC_RocaMusgo_02","SideBuriedRock_"+side+"_"+i,"Relief",x-side*1.0f,z+.6f,1.3f,i*53,.18f);
                Put("VEG_HelechoTropical_02","SideFern_"+side+"_"+i,"Understory",x-side*2.4f,z-.5f,1.05f,i*37);
            }
            // Irregular stepped retaining fragments flank the original mural, never overlap the four rotating panels.
            foreach(var p in new[]{new Vector2(-9.5f,16.8f),new Vector2(-2.7f,17.5f),new Vector2(-.3f,20.4f)})
            {
                int n=placed.Count;
                var bottom=Put("ARQ_BloqueMaya_02","RetainingFoot_"+n,"Ruins",p.x,p.y,1,8+n%13,.15f,false,true,0,new Vector3(2.2f,1.8f,1.2f));
                var b=GoldenSliceArtPass.VisualBounds(bottom);
                Put("ARQ_BloqueMaya_02","RetainingCourse_"+n,"Ruins",p.x+.12f,p.y+.12f,1,14+n%11,0,false,true,b.max.y-Surface(p.x+.12f,p.y+.12f)-.05f,new Vector3(1.9f,1.5f,1.15f));
                Put("VEG_HelechoTropical_02","RuinFootFern_"+n,"Understory",p.x+.65f,p.y-.7f,.8f,n*29);
                Put("VEG_RaizTropical_01","RuinRoot_"+n,"Relief",p.x-.35f,p.y+.55f,.65f,n*17,.08f);
            }
            for(int i=0;i<7;i++)
            {
                float x=-10.3f+i*2.4f,z=20.4f+(i%2)*.8f;
                Put("ROC_RocaMusgo_02","MuralRearRock_"+i,"Relief",x,z,1.3f+i%3*.2f,i*57,.2f,false,i<3);
                Put("VEG_ArbustoTropical_03","MuralRearShrub_"+i,"Understory",x+.3f,z+.7f,1.3f,i*41);
                Put("VEG_PlantaTropical_02","MuralRearPlant_"+i,"Understory",x-.5f,z-.65f,.9f,i*73);
            }
            Put("VEG_CeibaTropical_01","MuralLeftHeroCeiba","Canopy",-12.8f,18.8f,.83f,27,0,false,true);
            Put("VEG_RaizTropical_01","MuralLeftExposedRoot","Relief",-9.7f,16.5f,.9f,72,.1f);
            for(int i=0;i<2;i++)
            {
                Put("VEG_CeibaJoven_01","EastLowerCrown_"+i,"Background",11.8f+i*2.2f,17.8f+i*2.8f,.85f,53+i*71,0,true);
                Put("VEG_ArbustoTropical_03","EastLowerMass_"+i,"Understory",10.6f+i*1.7f,17.3f+i*2.6f,1.5f,91+i*57,0,true);
            }
            // A shallow stone foot supports the existing raised mural without touching its fragments or collider.
            var plinth=Put("ARQ_BloqueMaya_02","MuralStoneFoot","Ruins",-6,15.65f,1,0,.05f,false,true);
            var pb=GoldenSliceArtPass.VisualBounds(plinth);plinth.transform.localScale=Vector3.Scale(plinth.transform.localScale,new Vector3(3.15f/pb.size.x,.6f/pb.size.y,.85f/pb.size.z));
            pb=GoldenSliceArtPass.VisualBounds(plinth);plinth.transform.position+=new Vector3(-6-pb.center.x,.405f-pb.min.y,15.65f-pb.center.z);
            // Lianas begin above the field of the puzzle, anchored to the canopy outside its silhouette.
            for(int i=0;i<3;i++)Put("VEG_CortinaLianas_01","RearLianas_"+i,"Canopy",-10+i*6,24.5f,.85f,15+i*23,0,true,false,2.6f);
            foreach(var p in new[]{new Vector2(-8.2f,10),new Vector2(-3,17.8f),new Vector2(5.9f,18.8f),new Vector2(-8.5f,7.1f)})
            {
                int n=placed.Count;Put("VEG_HojasSecasMonton_01","EdgeLitter_"+n,"Ground",p.x,p.y,.65f,n*47);
                Put("ROC_RocaTropical_01","SunkenPebble_"+n,"Ground",p.x+.4f,p.y+.5f,.15f,n*31,.2f);
            }
            var all=Level01EnvironmentArtPass.All(scene);var ids=all.ToDictionary(Id);
            Check(protectedComponents.All(k=>ids.TryGetValue(k.Key,out var c)&&Props(c)==k.Value),"Every existing renderer/mesh/gameplay/collider/light preserved");
            Check(FloorProps(plazaFloor)==floorState&&plazaFloor.sharedMaterials.Length==1&&plazaFloor.sharedMaterial==groundMaterial,"Only local plaza floor material changed; every other floor renderer property preserved");
            Check(protectedTransforms.All(k=>ids.TryGetValue(k.Key,out var c)&&Pose((Transform)c)==k.Value),"All existing transforms and activations preserved, including entrance and puzzle");
            Check(retiredPoses.All(k=>!k.Key.gameObject.activeSelf&&k.Value==k.Key.localPosition.ToString("F6")+k.Key.localRotation.ToString("F6")+k.Key.localScale.ToString("F6")),"Only seven occluding GoldenSlice plaza dressing instances retired; transforms and objects preserved");
            Check(EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings"))==lighting,"Existing lighting preserved");
            Check(all.All(c=>c!=null),"No Missing Scripts");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new collider");
            int floating=placed.Count(g=>!g.name.StartsWith("RetainingCourse_")&&!g.name.StartsWith("RearLianas_")&&GoldenSliceArtPass.VisualBounds(g).min.y-Surface(g.transform.position.x,g.transform.position.z)>.03f);
            Check(floating==0,"Grounded plants and rocks at visible relief; elevated masonry and hanging lianas excluded");
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Scene saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{passed=true,placements=placed.Count,colliders=all.OfType<Collider>().Count(c=>c.enabled),newColliders=0,floating=floating,terrainTriangles=triangles,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"All preexisting gameplay/colliders/meshes/transforms unchanged; local plaza material changed and seven GoldenSlice left-plaza dressing instances retired to clear TPS camera","Entrance root unchanged","Lighting unchanged","No new collider or Missing Script","Base bounds grounded within 3cm; not a per-vertex contact guarantee","Door/secret/checkpoint references valid","Saved Level01 only"}},true));
            Debug.Log("PUZZLE01_ENCLOSURE_SAVED "+placed.Count);
        }
    }
}
