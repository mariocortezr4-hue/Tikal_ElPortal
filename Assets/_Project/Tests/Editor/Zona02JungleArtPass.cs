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
    // Explicit one-shot local authoring. No runtime components or gameplay operations.
    [InitializeOnLoad]
    public static class Zona02JungleArtPass
    {
        public const string Evidence="../_Zona02Art_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Zona02";
        const string Name="ENV_L01_Zona02_JungleBanks";
        static Transform root;
        static readonly List<GameObject> placed=new List<GameObject>();
        [Serializable] class Report {public int placements,resnapped,hiddenProxies,replacedBanks,newColliders;public string[] assets,hidden,checks;}
        static Zona02JungleArtPass(){EditorApplication.update+=Poll;}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Temp/Zona02Open.request"))
            {
                File.Delete("Temp/Zona02Open.request");
                if(!EditorSceneManager.GetActiveScene().isDirty)EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);
                return;
            }
            if(File.Exists("Temp/Zona02Finish.request"))
            {
                File.Delete("Temp/Zona02Finish.request");
                try{FinishVisuals();}catch(Exception e){File.WriteAllText(Evidence+"/FinishError.txt",e.ToString());Debug.LogException(e);}return;
            }
            if(!File.Exists("Temp/Zona02Art.request"))return;
            File.Delete("Temp/Zona02Art.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static bool Inside(float x,float z)=>x>=19&&x<=46&&z>=-7&&z<=29;
        static bool Exit(float x,float z)=>z>20.9f&&x>26.5f&&x<31.5f;
        static bool Bank(float x,float z)=>Inside(x,z)&&!(x<39&&z>3&&z<21)&&!Exit(x,z);
        static float Height(float x,float z)
        {
            float d=Mathf.Max(3-z,z-21,x-39,0);
            float fade=Mathf.SmoothStep(0,1,d/4);
            return 1.2f+2.6f*(1-Mathf.Exp(-d*.3f))+fade*(.46f*Mathf.Sin(x*.45f+z*.31f)+.24f*Mathf.Cos(x*.71f-z*.37f));
        }
        static Mesh SaveMesh(string n,Vector3[] v,int[] t,Vector2[] uv,Color[] colors=null)
        {
            var m=new Mesh{name=n,indexFormat=IndexFormat.UInt32};m.vertices=v;m.triangles=t;m.uv=uv;if(colors!=null)m.colors=colors;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+n+".asset");return m;
        }
        static void Visual(string n,Mesh m,Material mat)
        {
            var g=new GameObject(n);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void Terrain()
        {
            const int nx=55,nz=73;var v=new Vector3[nx*nz];var uv=new Vector2[v.Length];var t=new List<int>();
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*nx+x;float wx=19+x*.5f,wz=-7+z*.5f;v[i]=new Vector3(wx,Height(wx,wz),wz);uv[i]=new Vector2(wx,wz)*.28f;}
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)if(Bank(19+(x+.5f)*.5f,-7+(z+.5f)*.5f)){int a=z*nx+x;t.AddRange(new[]{a,a+nx,a+1,a+1,a+nx,a+nx+1});}
            Visual("BroadOrganicBanks_27x36m",SaveMesh("MESH_Zona02_BroadBanks",v,t.ToArray(),uv),AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/Puzzle01/MAT_Puzzle01_Earth.mat"));
        }
        static GameObject Put(string asset,float x,float z,float s,float yaw,float burial=0,bool background=false)
        {
            Check(Bank(x,z),"All new roots on non-playable Zone02 bank: "+x+"/"+z);
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Imported prefab "+asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(p,root);g.name="Z02_"+asset+"_"+placed.Count.ToString("D3");g.transform.SetPositionAndRotation(new Vector3(x,Height(x,z),z),Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one*s;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var b=GoldenSliceArtPass.VisualBounds(g);float h=Height(x,z);if(asset.StartsWith("ROC_")||asset.Contains("Raiz"))h=Mathf.Min(h,Height(b.min.x,b.min.z),Height(b.max.x,b.max.z));g.transform.position+=Vector3.up*(h-b.min.y-b.size.y*burial);
            var lod=g.GetComponentInChildren<LODGroup>();if(background&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.006f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){if(background)r.shadowCastingMode=ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static void Path()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int i=0;i<=76;i++)
            {
                float x=19+i*.25f,center=12+.17f*Mathf.Sin(x*.43f),w=1.7f+.12f*Mathf.Sin(x*.82f);
                for(int j=0;j<4;j++){float z=center+w*new[]{-1f,-.75f,.75f,1f}[j];v.Add(new Vector3(x,Puzzle02AccessArtPass.Floor(x,z)+.014f,z));uv.Add(new Vector2(x,z)*.32f);float fade=Mathf.SmoothStep(0,1,(x-19)/1.5f)*Mathf.SmoothStep(0,1,(38-x)/2);c.Add(new Color(1,1,1,j==0||j==3?0:fade*.65f));}
                if(i<76)for(int j=0;j<3;j++){int a=i*4+j;t.AddRange(new[]{a,a+1,a+4,a+1,a+5,a+4});}
            }
            Visual("SoftWornPath_OrganicEdges",SaveMesh("MESH_Zona02_WornPath",v.ToArray(),t.ToArray(),uv.ToArray(),c.ToArray()),AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/EntranceHero/MAT_Entrance_WornEarth.mat"));
        }
        static void FinishVisuals()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Zone02 Edit Mode");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");root=env.Find(Name);Check(root!=null,"Existing local art");
            Check(AssetDatabase.LoadAssetAtPath<Material>(Folder+"/MAT_Zona02_DryTrail.mat")==null,"Final visual adjustment already applied");
            // Remove only the overlapping Zone02 portion of the older transparent strip.
            var old=env.Find("ENV_L01_Puzzle02_AccessBase/Ground/ClearWornPath_3_5m").GetComponent<MeshFilter>();var m=UnityEngine.Object.Instantiate(old.sharedMesh);var v=m.vertices;var ix=m.triangles;var keep=new List<int>();
            for(int i=0;i<ix.Length;i+=3)if((v[ix[i]].x+v[ix[i+1]].x+v[ix[i+2]].x)/3<19)keep.AddRange(new[]{ix[i],ix[i+1],ix[i+2]});m.name="MESH_P02Access_ConnectorTrailPreserved";m.triangles=keep.ToArray();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/"+m.name+".asset");old.sharedMesh=m;
            var trail=root.Find("SoftWornPath_OrganicEdges").GetComponent<MeshRenderer>();var mat=new Material(trail.sharedMaterial){name="MAT_Zona02_DryTrail"};mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",0);mat.SetFloat("_Metallic",0);mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_EnvironmentReflections",0);mat.SetFloat("_BlendModePreserveSpecular",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");mat.SetColor("_BaseColor",new Color(1.08f,1.05f,.91f,1));mat.SetColor("_Color",new Color(1.08f,1.05f,.91f,1));AssetDatabase.CreateAsset(mat,Folder+"/"+mat.name+".mat");trail.sharedMaterial=mat;
            placed.Clear();
            foreach(var p in new[]{new Vector2(22,-1.6f),new Vector2(27,-1),new Vector2(33,-1.6f),new Vector2(37,-1),new Vector2(42,3),new Vector2(42,10),new Vector2(42,18),new Vector2(37,24),new Vector2(33,23.5f),new Vector2(25.6f,24),new Vector2(21,24.5f),new Vector2(40,24.5f)})
            {var g=Put("VEG_CeibaJoven_01",p.x,p.y,.53f+(placed.Count%3)*.07f,placed.Count*67,0,true);g.name="Z02_LowerCrown_"+placed.Count;}
            foreach(var p in new[]{new Vector2(24,-3),new Vector2(35,-2.8f),new Vector2(43,13),new Vector2(35,26)})
            {var g=Put("VEG_PalmeraTropical_02",p.x,p.y,.72f,placed.Count*31,0,true);g.name="Z02_PalmAccent_"+placed.Count;}
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/EditorValidation.json"));report.placements+=placed.Count;report.assets=report.assets.Concat(placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot)).Distinct().ToArray();report.checks=report.checks.Concat(new[]{"Sixteen lower canopy/palm accents close middle layer","Overlapping Zone02 trail strip replaced with one matte feathered layer; connector unchanged"}).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final visual adjustment saved");AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("ZONA02_VISUAL_FINISHED "+report.placements);
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01 required");
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");Check(env.Find(Name)==null,"One-shot pass already exists; inspect before changing");
            var all=Level01EnvironmentArtPass.All(scene);var protectedComponents=all.Where(c=>c is Collider||c is MonoBehaviour).ToDictionary(c=>c,Props);var transforms=all.OfType<Transform>().ToDictionary(t=>t,t=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6"));
            var access=env.Find("ENV_L01_Puzzle02_AccessBase");Check(access!=null,"Preserved paused access pass");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","Zona02");
            root=new GameObject(Name).transform;root.SetParent(env,false);placed.Clear();
            // Keep the previous mesh asset intact. Its connector triangles remain visible unchanged.
            var old=access.Find("Ground/OrganicPathSideBanks").GetComponent<MeshFilter>();var src=old.sharedMesh;var sv=src.vertices;var si=src.triangles;var keep=new List<int>();
            for(int i=0;i<si.Length;i+=3)if((sv[si[i]].x+sv[si[i+1]].x+sv[si[i+2]].x)/3<19)keep.AddRange(new[]{si[i],si[i+1],si[i+2]});
            var connector=UnityEngine.Object.Instantiate(src);connector.name="MESH_P02Access_ConnectorPreserved";connector.triangles=keep.ToArray();connector.RecalculateBounds();AssetDatabase.CreateAsset(connector,Folder+"/MESH_P02Access_ConnectorPreserved.asset");old.sharedMesh=connector;
            var hide=all.OfType<MeshRenderer>().Where(r=>r.transform.parent!=null&&((r.transform.parent.name=="NaturalBoundaries"&&r.bounds.center.x>=19&&r.bounds.center.x<=43&&r.bounds.center.z>=-2&&r.bounds.center.z<=25.5f)||(r.transform.parent.name=="Vegetation_Blockout"&&r.bounds.center.x>=19&&r.bounds.center.x<=43&&r.bounds.center.z>=-3&&r.bounds.center.z<=27))).ToArray();
            foreach(var r in hide)r.enabled=false;
            Terrain();Path();
            var moved=new HashSet<Transform>();
            foreach(var t in access.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x>=19&&Bank(t.position.x,t.position.z)))
            {var b=GoldenSliceArtPass.VisualBounds(t.gameObject);float sink=t.name.Contains("Rock")?b.size.y*.2f:0;t.position+=Vector3.up*(Height(t.position.x,t.position.z)-b.min.y-sink);PrefabUtility.RecordPrefabInstancePropertyModifications(t);moved.Add(t);}
            // Dense but staggered perimeter: low foliage near the floor, larger crowns behind it.
            for(int side=0;side<2;side++)for(int i=0;i<9;i++)
            {
                float x=20.4f+i*2.5f,z=side==0?1.35f:22.65f;if(Exit(x,z))continue;
                Put("VEG_ArbustoTropical_03",x,z,.95f+(i%3)*.12f,i*61+side*19,0,true);
                Put("VEG_HelechoTropical_02",x+.55f,z+(side==0?-.8f:.8f),.9f,i*43);
                Put("VEG_PlantaTropical_02",x-.25f,z+(side==0?-1.5f:1.5f),.85f,i*79,0,true);
                if(i%2==0)Put("ROC_RocaMusgo_02",x+.4f,z+(side==0?-2.1f:2.1f),1.15f,i*51,.25f);
            }
            foreach(var p in new[]{new Vector2(21,-3),new Vector2(26,-4.3f),new Vector2(32,-3.5f),new Vector2(38,-3),new Vector2(43,0),new Vector2(44,7),new Vector2(44,15),new Vector2(43,23),new Vector2(37,26),new Vector2(33,27),new Vector2(24,27),new Vector2(20,25.5f)})
            {
                int i=placed.Count;Put(i%2==0?"VEG_ArbolTropical_03":"VEG_ArbolTropical_04",p.x,p.y,.76f,i*37,0,true);
                Put("VEG_CeibaJoven_01",p.x+.3f,p.y-.4f,.8f,i*67,0,true);
                Put("VEG_ArbustoTropical_03",p.x-.4f,p.y+.5f,1.1f,i*91,0,true);
            }
            for(int i=0;i<6;i++)
            {
                float z=4.4f+i*3.1f;Put("VEG_ArbustoTropical_03",40.5f,z,1.1f,i*53,0,true);Put("VEG_HelechoTropical_02",40,z+.8f,1,i*73);
                if(i%2==0)Put("ROC_RocaMusgo_02",41.7f,z,1.3f,i*83,.27f);
            }
            foreach(var p in new[]{new Vector2(23,-.5f),new Vector2(35,-1),new Vector2(40.6f,5),new Vector2(40.7f,18),new Vector2(24,24.5f),new Vector2(35,24.5f)})
            {int i=placed.Count;Put("VEG_RaizTropical_01",p.x,p.y,.65f,i*31,.15f);Put("VEG_CortinaLianas_01",p.x+.2f,p.y+.3f,.7f,i*47,0,true);}
            foreach(var p in new[]{new Vector2(33,-.1f),new Vector2(35,.1f),new Vector2(40.8f,15),new Vector2(36,23.5f)})Put("ARQ_BloqueMaya_02",p.x,p.y,1.2f,placed.Count*29,.2f);
            Physics.SyncTransforms();Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new art colliders");
            Check(protectedComponents.All(k=>k.Key!=null&&Props(k.Key)==k.Value),"Original gameplay components and all colliders unchanged");
            Check(transforms.All(k=>moved.Contains(k.Key)||k.Value==k.Key.localPosition.ToString("F6")+k.Key.localRotation.ToString("F6")+k.Key.localScale.ToString("F6")),"All original poses preserved except resnapped paused Zone02 art");
            Check(placed.All(g=>GoldenSliceArtPass.VisualBounds(g).min.y<=Height(g.transform.position.x,g.transform.position.z)+.03f),"All new asset base bounds grounded");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Scene saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{placements=placed.Count,resnapped=moved.Count,hiddenProxies=hide.Count(r=>r.transform.parent.name=="Vegetation_Blockout"),replacedBanks=hide.Count(r=>r.transform.parent.name=="NaturalBoundaries"),newColliders=0,assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),hidden=hide.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),checks=new[]{"Original colliders and MonoBehaviours unchanged","Original poses unchanged except paused Zone02 art re-grounded","Connector bank geometry preserved; original asset retained","No decorative stelae; no new colliders","Only Zone02 art authoring; saved scene; visual review required"}},true));Debug.Log("ZONA02_ART_SAVED "+placed.Count);
        }
    }
}
