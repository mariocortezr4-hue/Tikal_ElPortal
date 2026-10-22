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
    public static class EntranceExpansionArtPass
    {
        const string Assets="Assets/_Project/Art/Environment/Level01/EntranceHero";
        public const string Evidence="../_EntranceHero_20261022/Expansion";
        const string Name="Entrance_LateralExpansion";
        const int NX=49,NZ=29;
        static Transform root;static Vector3[] land;
        static readonly List<GameObject> placements=new List<GameObject>();
        [Serializable] public class Report {public bool passed;public int pass,placements,groundedRepairs,colliders,floating,terrainTriangles,pathTriangles;public string[] checks,assets;}
        static EntranceExpansionArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/EntranceExpansionApply.request"))return;
            File.Delete("Temp/EntranceExpansionApply.request");Directory.CreateDirectory(Evidence);
            try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/ApplyError.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool ok,string msg){if(!ok)throw new InvalidOperationException("Entrance expansion: "+msg);}
        static string Id(UnityEngine.Object obj)=>GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        static string Properties(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6");
        static bool Floor(float x,float z,out float y)
        {
            var hits=Physics.RaycastAll(new Vector3(x,45,z),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is MeshCollider&&h.collider.transform.parent!=null&&h.collider.transform.parent.name=="Terrain"&&h.normal.y>.4f).OrderBy(h=>h.distance).ToArray();
            y=hits.Length>0?hits[0].point.y:0;return hits.Length>0;
        }
        static float LandHeight(float x,float z)
        {
            float edgeX=Mathf.Clamp(x,-7.8f,7.8f),edgeZ=Mathf.Clamp(z,-15.5f,-.25f);Floor(edgeX,edgeZ,out float floor);
            float d=Mathf.Max(Mathf.Abs(x)-7.8f,-z-15.6f,0);
            float rise=Mathf.SmoothStep(0,1,d/3.5f);
            float waves=1.25f+.62f*Mathf.Sin(x*.38f+z*.24f)+.34f*Mathf.Cos(z*.55f-x*.18f);
            float farDrop=Mathf.SmoothStep(0,1,Mathf.Max(Mathf.Abs(x)-21,-z-25,0)/3)*3;
            return floor+rise*waves+Mathf.SmoothStep(0,1,d/11)*.75f-farDrop;
        }
        public static float Surface(float x,float z)
        {
            if(Floor(x,z,out float floor))return floor;
            if(land==null) return LandHeight(x,z);
            float gx=Mathf.Clamp(x+24,0,NX-1.001f),gz=Mathf.Clamp(z+28,0,NZ-1.001f);int ix=(int)gx,iz=(int)gz;float u=gx-ix,v=gz-iz;
            float a=land[iz*NX+ix].y,b=land[iz*NX+ix+1].y,c=land[(iz+1)*NX+ix].y,d=land[(iz+1)*NX+ix+1].y;
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static Transform Group(string name){var t=root.Find(name);if(t!=null)return t;var go=new GameObject(name);go.transform.SetParent(root,false);return go.transform;}
        static Material Material(string name,Material source,Vector2 tile,Color tint)
        {
            string path=Assets+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(source){name=name};AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",tint);foreach(string p in m.GetTexturePropertyNames())if(m.GetTexture(p)!=null)m.SetTextureScale(p,tile);m.enableInstancing=true;return m;
        }
        static Mesh MeshAsset(string name,Vector3[] vertices,int[] indices,Vector2[] uv)
        {
            string path=Assets+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        static void VisualMesh(string name,Mesh mesh,Material mat,string group)
        {
            var go=new GameObject(name);go.transform.SetParent(Group(group),false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static int Land(Material material)
        {
            land=new Vector3[NX*NZ];var uv=new Vector2[land.Length];var tris=new List<int>();
            for(int z=0;z<NZ;z++)for(int x=0;x<NX;x++){int i=z*NX+x;float wx=x-24,wz=z-28;land[i]=new Vector3(wx,LandHeight(wx,wz),wz);uv[i]=new Vector2(wx,wz)*.25f;}
            for(int z=0;z<NZ-1;z++)for(int x=0;x<NX-1;x++)
            {
                float cx=x-23.5f,cz=z-27.5f;
                if(Mathf.Abs(cx)<8&&cz>=-16)continue;
                int a=z*NX+x,b=a+1,c=a+NX,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=MeshAsset("MESH_Entrance_OrganicApron",land,tris.ToArray(),uv);VisualMesh("OrganicLateralAndRearRelief",mesh,material,"Relief");return tris.Count/3;
        }
        static int Path(Material material)
        {
            // The native baffle is retained: the worn strip visibly follows its left bypass, never promises a straight route.
            var points=new[]{new Vector2(0,-15),new Vector2(0,-12),new Vector2(-1,-10),new Vector2(-4.8f,-10),new Vector2(-6,-9),new Vector2(-6,-4),new Vector2(-4.8f,-3),new Vector2(-1.2f,-3),new Vector2(0,-1.4f),new Vector2(0,-.1f)};
            var centers=new List<Vector2>();
            for(int i=0;i<points.Length-1;i++)
            {
                Vector2 a=points[Mathf.Max(0,i-1)],b=points[i],c=points[i+1],d=points[Mathf.Min(points.Length-1,i+2)];
                for(int j=0;j<8;j++){float t=j/8f;centers.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}
            }
            centers.Add(points.Last());var verts=new Vector3[centers.Count*4];var uv=new Vector2[verts.Length];var colors=new Color[verts.Length];var tris=new List<int>();
            for(int i=0;i<centers.Count;i++)
            {
                var p=centers[i];var dir=(centers[Mathf.Min(i+1,centers.Count-1)]-centers[Mathf.Max(0,i-1)]).normalized;var side=new Vector2(-dir.y,dir.x);
                float width=p.x< -4.5f&&p.y> -9.5f&&p.y< -4?1.15f:1.85f;
                float irregular=1+.04f*Mathf.Sin(i*1.7f);float[] across={-1,-.74f,.74f,1};
                for(int s=0;s<4;s++){var v=p+side*width*across[s]*irregular;v.x=Mathf.Clamp(v.x,-7.8f,7.8f);Check(Floor(v.x,v.y,out float floor),"Path stays on original Terrain");verts[i*4+s]=new Vector3(v.x,floor+.009f,v.y);uv[i*4+s]=new Vector2(v.x,v.y)*.32f;colors[i*4+s]=new Color(1,1,1,s==0||s==3?0:1);}
                if(i<centers.Count-1)for(int s=0;s<3;s++){int k=i*4+s;tris.AddRange(new[]{k,k+1,k+4,k+1,k+5,k+4});}
            }
            var mesh=MeshAsset("MESH_Entrance_WornPath",verts,tris.ToArray(),uv);mesh.colors=colors;VisualMesh("WornEarth_Path",mesh,material,"Path");return tris.Count/3;
        }
        static GameObject Put(string asset,string name,string group,float x,float z,float scale,float yaw,float burial=0,bool distant=false,bool shadows=false)
        {
            Check(z<=0,"Entrance vicinity only");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Prefab "+asset);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Group(group));go.name=name;go.transform.SetPositionAndRotation(new Vector3(x,Surface(x,z),z),Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            var bounds=GoldenSliceArtPass.VisualBounds(go);go.transform.position+=Vector3.up*(Surface(x,z)-bounds.min.y-bounds.size.y*burial+.002f);
            var lod=go.GetComponentInChildren<LODGroup>();if(distant&&lod!=null){var low=lod.GetLODs()[Math.Min(1,lod.lodCount-1)];lod.SetLODs(new[]{new LOD(.008f,low.renderers)});var used=low.renderers.ToHashSet();foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            placements.Add(go);return go;
        }
        static void Apply()
        {
            Check(!Application.isBatchMode&&!EditorSceneManager.GetActiveScene().isDirty,"Saved interactive Edit Mode");var scene=EditorSceneManager.OpenScene(Level01EnvironmentInspection.ScenePath);Physics.SyncTransforms();
            var env=scene.GetRootGameObjects().Single().transform.Find("Environment");var entrance=env.Find(EntranceHeroArtPass.RootName);Check(entrance!=null,"Preserve saved frontal base");var prior=entrance.Find(Name);
            var all=Level01EnvironmentArtPass.All(scene);var gameplay=all.Where(c=>c is Collider||c is MonoBehaviour).Where(c=>prior==null||!c.transform.IsChildOf(prior)).ToDictionary(Id,Properties);
            var transforms=all.OfType<Transform>().Where(t=>prior==null||!t.IsChildOf(prior)).Where(t=>!t.name.StartsWith("LeftBank_")&&t.name!="RearHedge_2"&&t.name!="RearHedge_3").ToDictionary(Id,Pose);
            var visual=all.OfType<Renderer>().Where(r=>prior==null||!r.transform.IsChildOf(prior)).ToDictionary(Id,Properties);
            if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);root=new GameObject(Name).transform;root.SetParent(entrance,false);placements.Clear();
            foreach(string group in new[]{"Path","LeftJungle","RightJungle","BackgroundForest","Relief","SetDressing"})Group(group);
            var source=AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art+"Ground/MAT_Tutorial_ForestFloor.mat");
            var soil=Material("MAT_Entrance_ReliefEarth",source,Vector2.one,new Color(.83f,.88f,.68f,1));
            var path=Material("MAT_Entrance_WornEarth",source,new Vector2(.7f,.7f),new Color(1.26f,1.2f,1.03f,1));
            // Built-in URP lit shader with vertex alpha feathers the worn strip into existing earth; no new texture.
            path.shader=Shader.Find("Universal Render Pipeline/Particles/Lit");Check(path.shader!=null&&path.shader.isSupported,"Supported vertex-alpha ground shader");
            path.SetFloat("_Surface",1);path.SetFloat("_Blend",0);path.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);path.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);path.SetFloat("_SrcBlendAlpha",1);path.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);path.SetFloat("_ZWrite",0);path.SetFloat("_AlphaClip",0);path.SetFloat("_ColorMode",0);path.SetOverrideTag("RenderType","Transparent");path.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");path.DisableKeyword("_ALPHAPREMULTIPLY_ON");path.renderQueue=3000;
            int terrainTriangles=Land(soil),pathTriangles=Path(path);
            // Repair only the outer left support points; the gate composition and native path remain untouched.
            int repairs=0;foreach(var t in entrance.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("LeftBank_")&&PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)))
            {
                var b=GoldenSliceArtPass.VisualBounds(t.gameObject);float burial=t.name.Contains("Rock")?b.size.y*.18f:0;
                t.position+=Vector3.up*(Surface(t.position.x,t.position.z)-b.min.y-burial+.002f);repairs++;
            }
            foreach(string name in new[]{"RearHedge_2","RearHedge_3"})
            {
                var t=entrance.Find("BackgroundForest/"+name);if(t==null)continue;
                float x=name.EndsWith("2")?-5:5,z=-18.5f;var b=GoldenSliceArtPass.VisualBounds(t.gameObject);
                t.position=new Vector3(x,Surface(x,z)+(t.position.y-b.min.y)+.002f,z);
            }
            string[] trees={"VEG_ArbolTropical_02","VEG_ArbolTropical_03","VEG_ArbolTropical_04"};
            foreach(int side in new[]{-1,1})
            {
                string group=side<0?"LeftJungle":"RightJungle";
                for(int i=0;i<7;i++)
                {
                    float z=-21.3f+i*3.25f,x=side*(11.3f+(i%3)*1.6f);
                    Put(trees[(i+(side>0?1:0))%3],"LateralCanopy_"+side+"_"+i,group,x,z,.85f+i%3*.08f,i*73,0,true);
                    Put("VEG_ArbustoTropical_03","LateralMiddle_"+side+"_"+i,group,x-side*1.3f,z+.7f,1.4f+i%2*.2f,i*41,0,true);
                    Put(i%2==0?"VEG_HelechoTropical_02":"VEG_PlantaTropical_02","LateralGround_"+side+"_"+i,group,x-side*2.1f,z-.8f,1.05f,i*61);
                    if(i%2==0)Put("ROC_RocaMusgo_02","LateralBuriedRock_"+side+"_"+i,"Relief",x-side*2.3f,z,1.2f+i%3*.2f,i*47,.18f,true);
                }
                for(int i=0;i<5;i++)
                {
                    float z=-20+i*4.3f,x=side*(17.5f+(i%2)*1.4f);
                    Put(trees[(i+2)%3],"DistantSilhouette_"+side+"_"+i,"BackgroundForest",x,z,.95f,i*79,0,true);
                    Put("VEG_CeibaJoven_01","LowerForestCrown_"+side+"_"+i,"BackgroundForest",x-side*1.3f,z+1,.9f,i*37,0,true);
                }
            }
            for(int i=0;i<9;i++)
            {
                float x=-17+i*4.25f,z=-22.7f+(i%3)*.75f;
                Put(trees[i%3],"RearForest_"+i,"BackgroundForest",x,z,.9f+i%2*.12f,i*59,0,true);
                Put("VEG_ArbustoTropical_03","RearUnderstory_"+i,"BackgroundForest",x,z+2,1.7f,i*39,0,true);
            }
            foreach(var p in new[]{new Vector2(-9,-15),new Vector2(-9,-6),new Vector2(10,-13),new Vector2(11,-4),new Vector2(-5,-19),new Vector2(5,-20)})
            {
                int n=placements.Count;Put("ARQ_ColumnaDerrumbada_01","BuriedRemnant_"+n,"SetDressing",p.x,p.y,.85f,n*31,.15f);
                Put("VEG_RaizTropical_01","ExposedRoot_"+n,"SetDressing",p.x+.5f,p.y-.6f,.65f,n*71,.07f);
            }
            // Fine edge details stay below ankle height and clear of the central walking line.
            foreach(var p in new[]{new Vector2(1.8f,-12.4f),new Vector2(-2,-12.1f),new Vector2(-3.7f,-11.7f),new Vector2(-7.5f,-8.5f),new Vector2(-4.4f,-4.7f),new Vector2(2,-2)})
            {
                int n=placements.Count;Put("VEG_HojasSecasMonton_01","PathLitter_"+n,"Path",p.x,p.y,.52f,n*57);
                Put("ROC_RocaTropical_01","PathPebble_"+n,"Path",p.x+.28f,p.y+.35f,.18f,n*31,.18f);
            }
            for(int i=0;i<9;i++)
            {
                float z=-13+i*1.2f,x=z< -10?0:z< -4? -6:0;
                foreach(int side in new[]{-1,1})
                {
                    float offset=x< -4?1:1.5f;float px=x+side*offset;
                    Put("ROC_RocaTropical_01","WornPath_SmallPebble_"+i+"_"+side,"Path",px,z+.17f*(i%3),.065f+(i%3)*.018f,i*47,.2f);
                }
            }
            all=Level01EnvironmentArtPass.All(scene);var ids=all.ToDictionary(Id);
            Check(gameplay.All(e=>ids.TryGetValue(e.Key,out var c)&&Properties(c)==e.Value),"All existing gameplay/colliders preserved");
            Check(transforms.All(e=>ids.TryGetValue(e.Key,out var c)&&Pose((Transform)c)==e.Value),"Frontal/door/plaza and all other retained transforms preserved");
            Check(visual.All(e=>ids.TryGetValue(e.Key,out var c)&&Properties(c)==e.Value),"Every existing renderer preserved");
            Check(root.GetComponentsInChildren<Component>(true).All(c=>c!=null),"No Missing Scripts");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No new permanent collider");
            int floating=0;foreach(var go in placements){var b=GoldenSliceArtPass.VisualBounds(go);if(b.min.y-Surface(go.transform.position.x,go.transform.position.z)>.03f)floating++;}Check(floating==0,"Grounded new placements");
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();int pass=SessionState.GetInt("Tikal.EntranceExpansion.Pass",0)+1;SessionState.SetInt("Tikal.EntranceExpansion.Pass",pass);
            File.WriteAllText(Evidence+"/EditorValidation_Pass"+pass+".json",JsonUtility.ToJson(new Report{passed=true,pass=pass,placements=placements.Count,groundedRepairs=repairs,colliders=all.OfType<Collider>().Count(c=>c.enabled),floating=floating,terrainTriangles=terrainTriangles,pathTriangles=pathTriangles,assets=placements.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"Existing frontal/door/plaza transforms and renderers preserved","Existing colliders/gameplay preserved","No Missing Scripts","No new colliders","No grounded placement gap >3cm","Door/secret/checkpoint references intact","Scene saved"}},true));Debug.Log("ENTRANCE_EXPANSION_SAVED "+pass);
        }
    }
}
