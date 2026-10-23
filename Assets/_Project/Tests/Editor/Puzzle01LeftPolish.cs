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
    public static class Puzzle01LeftPolish
    {
        public const string Evidence="../_Puzzle01LeftPolish_20261022";
        const string RootName="ENV_L01_Puzzle01_LeftPolish";
        const string MeshPath="Assets/_Project/Art/Environment/Level01/Puzzle01/MESH_Puzzle01_LeftBackgroundBank.asset";
        const int NX=31,NZ=55;
        static Transform root;static Vector3[] bank,originalBank;
        static readonly List<GameObject> placements=new List<GameObject>();
        [Serializable] public class Report {public bool passed;public int placements,triangles,colliders,newColliders,floating;public string[] checks,assets;}
        static Puzzle01LeftPolish(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isUpdating||EditorApplication.isCompiling||!File.Exists("Temp/Puzzle01LeftApply.request"))return;
            File.Delete("Temp/Puzzle01LeftApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool ok,string m){if(!ok)throw new InvalidOperationException("Puzzle01 left polish: "+m);}
        static string Id(UnityEngine.Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>Id(t.parent)+t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static float Interpolate(Vector3[] grid,int nx,int nz,float x,float z)
        {
            x=Mathf.Clamp(x,0,nx-1.001f);z=Mathf.Clamp(z,0,nz-1.001f);int ix=(int)x,iz=(int)z;float u=x-ix,v=z-iz;
            float a=grid[iz*nx+ix].y,b=grid[iz*nx+ix+1].y,c=grid[(iz+1)*nx+ix].y,d=grid[(iz+1)*nx+ix+1].y;
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static float Original(float x,float z)=>Interpolate(originalBank,35,29,x+17,z-2);
        static float Height(float x,float z)
        {
            if(x>-16)return Original(x,z)+.004f+1.1f*Mathf.SmoothStep(0,1,(x+16)/2)*Mathf.SmoothStep(0,1,(-x-9)/2.5f)*(1-Mathf.SmoothStep(0,1,(z-2)/3));
            float t=(-16-x)/8,along=Mathf.Clamp01((z-6)/23);
            float envelope=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t))*Mathf.Sin(Mathf.PI*along);
            float edge=Original(-16,z);
            return edge+envelope*(3.6f+.6f*Mathf.Sin(z*.57f)+.35f*Mathf.Cos(x*.8f+z*.31f))+.004f;
        }
        static float Surface(float x,float z)=>(x<=-16||z<5)&&x<=-9&&z>=2&&z<=29?Interpolate(bank,NX,NZ,(x+24)*2,(z-2)*2):Original(x,z);
        static Transform Group(string name){var t=root.Find(name);if(t!=null)return t;var g=new GameObject(name);g.transform.SetParent(root,false);return g.transform;}
        static int Bank()
        {
            bank=new Vector3[NX*NZ];var uv=new Vector2[bank.Length];var tris=new List<int>();
            for(int iz=0;iz<NZ;iz++)for(int ix=0;ix<NX;ix++)
            {int k=iz*NX+ix;float x=-24+ix*.5f,z=2+iz*.5f;bank[k]=new Vector3(x,Height(x,z),z);uv[k]=new Vector2(x,z)*.28f;}
            for(int z=0;z<NZ-1;z++)for(int x=0;x<NX-1;x++){if(-24+(x+.5f)*.5f>-16&&2+(z+.5f)*.5f>=5)continue;int a=z*NX+x,b=a+1,c=a+NX,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});}
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);if(mesh==null){mesh=new Mesh{name="MESH_Puzzle01_LeftBackgroundBank"};AssetDatabase.CreateAsset(mesh,MeshPath);}else mesh.Clear();
            mesh.vertices=bank;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var g=new GameObject("LeftBackground_OrganicLoma_VisualOnly");g.transform.SetParent(Group("Relief"),false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat");r.shadowCastingMode=ShadowCastingMode.Off;return tris.Count/3;
        }
        static GameObject Put(string asset,string name,string group,float x,float z,float scale,float yaw,float bury=0,bool distant=false)
        {
            Check((x<=-9.8f&&x>=-23&&z>=3&&z<=28)||(x<=-4&&x>=-9.8f&&z>=24&&z<=28),"Left/left rear Puzzle01 only");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Reused prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Group(group));g.name=name;g.transform.SetPositionAndRotation(new Vector3(x,Surface(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float h=Surface(x,z);
            if(asset.StartsWith("ROC_")||asset.Contains("Raiz"))h=Mathf.Min(h,Surface(b.min.x,b.min.z),Surface(b.max.x,b.min.z),Surface(b.min.x,b.max.z),Surface(b.max.x,b.max.z));
            g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*bury+.002f);
            var lod=g.GetComponentInChildren<LODGroup>();
            if(distant&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.008f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            placements.Add(g);return g;
        }
        static void Liana(GameObject tree,float x,float z,string name)
        {
            var l=Put("VEG_CortinaLianas_01",name,"Background",x,z,.7f,19+placements.Count*7);
            var crown=GoldenSliceArtPass.VisualBounds(tree);var b=GoldenSliceArtPass.VisualBounds(l);
            l.transform.position+=Vector3.up*(crown.max.y-crown.size.y*.28f-b.max.y);
            Check(GoldenSliceArtPass.VisualBounds(l).min.y>=Surface(x,z)-.1f,"Hanging liana above ground, aligned with a named tree crown");
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01 in Edit Mode");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find("ENV_L01_Puzzle01_Enclosure")!=null,"Approved enclosure preserved as base");
            var prior=env.Find(RootName);var all=Level01EnvironmentArtPass.All(scene).Where(c=>prior==null||!c.transform.IsChildOf(prior)).ToArray();
            Check(all.All(c=>c!=null),"Original scene has no missing component");
            var components=all.Where(c=>!(c is Transform)).ToDictionary(Id,Props);var poses=all.OfType<Transform>().ToDictionary(Id,Pose);
            string light=EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings"));
            originalBank=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Art/Environment/Level01/Puzzle01/MESH_Puzzle01_OrganicBanks.asset").vertices;
            Check(originalBank.Length==35*29,"Use exact approved relief surface");
            if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
            root=new GameObject(RootName).transform;root.SetParent(env,false);placements.Clear();int triangles=Bank();
            Put("VEG_ArbolTropical_03","LeftRearTree_A","Background",-19.4f,10.7f,.65f,32,0,true);
            var tree=Put("VEG_ArbolTropical_04","LeftRearTree_B","Background",-20.2f,17.6f,.69f,117,0,true);
            Put("VEG_ArbolTropical_03","LeftRearTree_C","Background",-19.1f,24.1f,.7f,247,0,true);
            Put("VEG_CeibaJoven_01","LeftMiddleCrown_A","Background",-17.8f,12.8f,.76f,54,0,true);
            var young=Put("VEG_CeibaJoven_01","LeftMiddleCrown_B","Background",-18.2f,22.2f,.74f,188,0,true);
            Put("VEG_CeibaJoven_01","LeftLowerCrown_C","Background",-14.7f,9.8f,.9f,83,0,true);
            Put("VEG_CeibaJoven_01","LeftLowerCrown_D","Background",-15.3f,16.7f,.82f,214,0,true);
            Put("VEG_ArbustoTropical_03","LeftBankMiddleShrub_A","Understory",-13.5f,9.9f,1.12f,127,0,true);
            Put("VEG_ArbustoTropical_03","LeftBankMiddleShrub_B","Understory",-14.4f,14.2f,1.06f,286,0,true);
            Put("VEG_ArbustoTropical_03","PlazaLeftCornerShrub","Understory",-9.9f,3.9f,1.4f,331);
            Put("VEG_HelechoTropical_02","PlazaLeftCornerFern","Understory",-10.8f,4.5f,.9f,74);
            Put("ROC_RocaMusgo_02","PlazaLeftCornerRock","Relief",-10.4f,3.1f,.8f,49,.18f);
            var clusters=new[]{new Vector2(-11.2f,8.1f),new Vector2(-12.7f,11.3f),new Vector2(-10.9f,13.6f),new Vector2(-14.3f,16.1f),new Vector2(-11.4f,20.5f),new Vector2(-14.8f,23.4f),new Vector2(-18.4f,9.2f),new Vector2(-20.7f,14.2f),new Vector2(-19.1f,20.2f),new Vector2(-16.9f,26.2f)};
            for(int i=0;i<clusters.Length;i++)
            {
                var p=clusters[i];bool far=p.x<-16;
                Put(i%3==0?"VEG_ArbustoTropical_02":"VEG_ArbustoTropical_03","LeftDispersedShrub_"+i,"Understory",p.x,p.y,.87f+i%3*.13f,i*71+13,0,far);
                Put(i%2==0?"VEG_HelechoTropical_02":"VEG_PlantaTropical_02","LeftLayeredPlant_"+i,"Understory",p.x+.55f,p.y-.7f,.76f+i%3*.09f,i*47+61,0,far);
                if(i%3==0)Put("VEG_HelechoTropical_02","LeftFernAccent_"+i,"Understory",p.x-.65f,p.y+.5f,.73f,i*83);
            }
            foreach(var p in new[]{new Vector2(-10.3f,10.1f),new Vector2(-11.1f,15.3f),new Vector2(-12.5f,22.1f),new Vector2(-7.8f,25.4f),new Vector2(-4.9f,26.4f)})
                Put("VEG_HelechoTropical_02","BordoFern_"+placements.Count,"Understory",p.x,p.y,.85f,placements.Count*43);
            foreach(var p in new[]{new Vector2(-12,12.1f),new Vector2(-17.8f,18.8f),new Vector2(-14,24.2f)})
            {
                int i=placements.Count;Put("ROC_RocaMusgo_02","EmbeddedMossAccent_"+i,"Relief",p.x,p.y,.75f,i*37,.18f);
                Put("VEG_RaizTropical_01","LeftExposedRoot_"+i,"Relief",p.x+.7f,p.y+.5f,.55f,i*23,.06f);
            }
            Liana(tree,-20,17.8f,"LeftTree_HangingLiana");Liana(young,-18,22.4f,"LeftCrown_HangingLiana");
            all=Level01EnvironmentArtPass.All(scene);var ids=all.ToDictionary(Id);
            Check(components.All(k=>ids.TryGetValue(k.Key,out var c)&&Props(c)==k.Value),"Every approved preexisting component, renderer, mesh, collider and runtime field unchanged");
            Check(poses.All(k=>ids.TryGetValue(k.Key,out var c)&&Pose((Transform)c)==k.Value),"Every preexisting transform and activation unchanged");
            Check(light==EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings")),"Lighting unchanged");
            Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new collider");Check(all.All(c=>c!=null),"No Missing Scripts");
            int floating=placements.Count(g=>!g.name.Contains("HangingLiana")&&GoldenSliceArtPass.VisualBounds(g).min.y-Surface(g.transform.position.x,g.transform.position.z)>.03f);Check(floating==0,"No base bounds gap above 3cm at visible support");
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Saved current scene");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{passed=true,placements=placements.Count,triangles=triangles,colliders=all.OfType<Collider>().Count(c=>c.enabled),newColliders=0,floating=floating,assets=placements.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"All approved preexisting transforms/activations/components byte-equivalent via serialized properties","Entrance, other puzzles, colliders and gameplay unchanged","Lighting and materials unchanged","Only separate left/left-rear art layer added","Base bounds grounded within 3cm; hanging lianas aligned to named crowns","No Missing Scripts or new collider","Original door/secret/checkpoint references valid","Scene saved"}},true));
            Debug.Log("PUZZLE01_LEFT_POLISH_SAVED "+placements.Count);
        }
    }
}
