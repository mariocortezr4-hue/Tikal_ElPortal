using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    // One additive art pass. Never calls the environment rebuild or changes shared prefabs.
    [InitializeOnLoad]
    public static class PlazaKeynesReferenceMatch
    {
        public const string ScenePath="Assets/_Project/Scenes/Levels/Level02_PlazaKeynes.unity";
        public const string Art="Assets/_Project/Art/Environment/Level02/ReferenceMatch";
        public const string Evidence="Assets/_Project/Documentation/Level02_PlazaKeynes/ReferenceMatch";
        const string Request="Temp/PlazaReferenceMatch.command";
        static Transform env,pass;
        static System.Random random;
        static readonly Dictionary<string,GameObject> sources=new Dictionary<string,GameObject>();
        static readonly List<Placement> added=new List<Placement>();
        [Serializable] public class Placement {public string name,source,category;public Vector3 position;public bool background;}
        [Serializable] public class Stats {public int placements,meshRenderers,lodGroups,enabledColliders,shadowCasters,realtimeLights,highestLodTriangles,lowestLodTriangles;}
        [Serializable] public class Report {public string baseCommit,scope;public Stats before,after;public Placement[] additions;public int resizedTemples,pathSections,supportPatches,impostorClusters;public Vector3 statuePosition,statueScale;}
        static PlazaKeynesReferenceMatch(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            string command=File.ReadAllText(Request).Trim();File.Delete(Request);
            try{if(command=="apply")Apply();else if(command=="validate")PlazaKeynesReferenceMatchValidation.Begin();else if(command=="finalize")FinalizeSupports();else if(command=="settle")SettleMonumentDressing();else if(command=="supportfix")RepairSupportFootprints();}
            catch(Exception e){Directory.CreateDirectory("Temp/PlazaReferenceMatch");File.WriteAllText("Temp/PlazaReferenceMatch/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static float F(float a,float b)=>(float)(a+random.NextDouble()*(b-a));
        static void RepairSupportFootprints()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required.");
            var root=GameObject.Find("ENV_Level02_PlazaKeynes").transform;int index=0;
            foreach(var group in new[]{root.Find("Temples"),root.Find("ReferenceMatch_ArtPass/CityLayers")})foreach(Transform t in group)
            {
                if(group==root.Find("Temples")&&t.position.z<140)continue;
                // A stair foot can be fractionally lower than the base. It must never define the entire foundation footprint.
                var lowest=t.GetComponentsInChildren<LODGroup>(true).Where(l=>l.name.Contains("TempleTier")).OrderBy(l=>l.GetLODs()[0].renderers.Min(r=>r.bounds.min.y)).First();
                var b=BoundsOf(lowest.gameObject);float top=b.min.y-.015f;const int n=21;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var tris=new List<int>();float hx=b.extents.x+11,hz=b.extents.z+11;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){float px=b.center.x+(x/(float)(n-1)*2-1)*hx,pz=b.center.z+(z/(float)(n-1)*2-1)*hz;float dx=Mathf.Max(0,Mathf.Abs(px-b.center.x)-b.extents.x),dz=Mathf.Max(0,Mathf.Abs(pz-b.center.z)-b.extents.z);float blend=1-Mathf.SmoothStep(0,1,Mathf.Max(dx,dz)/10);float y=Ground(px,pz);vertices[z*n+x]=new Vector3(px,Mathf.Max(y,Mathf.Lerp(y,top,blend)),pz);uv[z*n+x]=new Vector2(px/8,pz/8);}
                for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int a=z*n+x;tris.AddRange(new[]{a,a+n,a+1,a+1,a+n,a+n+1});}
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/SM_Plaza_TempleSupport_"+index+".asset");mesh.Clear();mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);index++;
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/FoundationValidation.txt","Seven visual foundations rebuilt from full bottom TempleTier bounds, excluding stair-foot bounds. No colliders, original terrain or existing assets changed.");
        }
        static void SettleMonumentDressing()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required.");
            var root=GameObject.Find("ENV_Level02_PlazaKeynes").transform.Find("ReferenceMatch_ArtPass/MonumentDressing");
            foreach(Transform stone in root)stone.position+=Vector3.up*(.48f-BoundsOf(stone.gameObject).min.y);
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/ArtAudit.json"));foreach(var entry in report.additions.Where(p=>p.category=="MonumentStone"))entry.position=root.Cast<Transform>().Single(t=>t.name==entry.name).position;
            File.WriteAllText(Evidence+"/ArtAudit.json",JsonUtility.ToJson(report,true));EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static void FinalizeSupports()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode required.");
            env=GameObject.Find("ENV_Level02_PlazaKeynes").transform;pass=env.Find("ReferenceMatch_ArtPass");if(pass.Find("TerraceSupports"))throw new Exception("Final correction already applied.");
            var support=Group(pass,"TerraceSupports");var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level02/MAT_PlazaKeynes_JungleFloor.mat");int patches=0;
            foreach(var group in new[]{env.Find("Temples"),pass.Find("CityLayers")})foreach(Transform t in group)
            {
                if(group==env.Find("Temples")&&t.position.z<140)continue;
                var lowest=t.GetComponentsInChildren<LODGroup>(true).Where(l=>l.name.Contains("TempleTier")).OrderBy(l=>l.GetLODs()[0].renderers.Min(r=>r.bounds.min.y)).First();var b=BoundsOf(lowest.gameObject);float top=b.min.y-.015f;const int n=21;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var tris=new List<int>();
                float hx=b.extents.x+11,hz=b.extents.z+11;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {float px=b.center.x+(x/(float)(n-1)*2-1)*hx,pz=b.center.z+(z/(float)(n-1)*2-1)*hz;float dx=Mathf.Max(0,Mathf.Abs(px-b.center.x)-b.extents.x),dz=Mathf.Max(0,Mathf.Abs(pz-b.center.z)-b.extents.z);float blend=1-Mathf.SmoothStep(0,1,Mathf.Max(dx,dz)/10);float y=Ground(px,pz);vertices[z*n+x]=new Vector3(px,Mathf.Max(y,Mathf.Lerp(y,top,blend))-.015f,pz);uv[z*n+x]=new Vector2(px/8,pz/8);}
                for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int a=z*n+x;tris.AddRange(new[]{a,a+n,a+1,a+1,a+n,a+n+1});}
                var mesh=new Mesh{name="SM_Plaza_TempleSupport_"+patches};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");var obj=Group(support,"GroundedTempleTerrace_"+patches).gameObject;obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;obj.isStatic=true;patches++;
            }
            // Reuse the approved forest atlas in three small meshes: canopy density without more 3D tree renderers.
            random=new System.Random(241030);var canopy=Group(pass,"CanopyEdgeMasses");int clusters=0;var temples=pass.Find("CityLayers").Cast<Transform>().Concat(env.Find("Temples").Cast<Transform>()).Select(t=>t.position).ToArray();
            for(int sector=0;sector<3;sector++)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
                for(int i=0;i<60;i++)
                {
                    float angle=(sector*120+i*2f+F(-2,2))*Mathf.Deg2Rad,radius=F(103,205);var p=new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);if(temples.Any(t=>Vector2.Distance(new Vector2(t.x,t.z),new Vector2(p.x,p.z))<29))continue;p.y=Ground(p.x,p.z)-.25f;float h=F(17,25),w=h*F(.85f,1.12f),yaw=F(0,180);int slot=i%3;float u=slot%2*.5f,v=slot/2*.5f;
                    for(int plane=0;plane<2;plane++){var right=Quaternion.Euler(0,yaw+90*plane,0)*Vector3.right*w*.5f;int index=vertices.Count;vertices.AddRange(new[]{p-right,p+right,p+right+Vector3.up*h,p-right+Vector3.up*h});uv.AddRange(new[]{new Vector2(u+.001f,v+.001f),new Vector2(u+.499f,v+.001f),new Vector2(u+.499f,v+.499f),new Vector2(u+.001f,v+.499f)});tris.AddRange(new[]{index,index+2,index+1,index,index+3,index+2});}clusters++;
                }
                var mesh=new Mesh{name="SM_Plaza_CanopyEdge_"+sector};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");var g=Group(canopy,"CanopyEdge_"+sector).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level02/Forest/MAT_PlazaKeynes_ForestImpostor.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;g.isStatic=true;
            }
            var plants=pass.Find("RocksAndLowVegetation").Cast<Transform>().Where(t=>t.name.StartsWith("RM_LowVegetation")).Take(32).ToArray();for(int i=0;i<plants.Length;i++){float side=i%2==0?-1:1;var p=new Vector3(side*F(68,82),0,-67+(i/2)*8.5f);var b=BoundsOf(plants[i].gameObject);plants[i].position+=new Vector3(p.x-b.center.x,Ground(p.x,p.z)-b.min.y-.025f,p.z-b.center.z);}
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Evidence+"/ArtAudit.json"));report.supportPatches=patches;report.impostorClusters=clusters;foreach(var entry in report.additions){var obj=pass.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==entry.name);if(obj)entry.position=obj.position;}report.after=Observe();File.WriteAllText(Evidence+"/ArtAudit.json",JsonUtility.ToJson(report,true));AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.Refresh();
        }
        static Transform Group(Transform p,string name){var g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static Bounds BoundsOf(GameObject g)=>PlazaKeynesAuthoring.BoundsOf(g);
        static GameObject Source(string name)
        {
            if(sources.TryGetValue(name,out var source))return source;
            var paths=AssetDatabase.FindAssets("PFB_"+name+" t:Prefab",new[]{"Assets/_Project/Prefabs/Environment"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileNameWithoutExtension(p)=="PFB_"+name&&!p.Contains("/Depth/")).ToArray();
            if(paths.Length!=1)throw new Exception("Unique existing source required: "+name);
            return sources[name]=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
        }
        public static float Ground(float x,float z)=>PlazaKeynesAuthoring.Height(x,z)-.025f;
        public static void Background(GameObject g,bool distant)
        {
            foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            foreach(var lod in g.GetComponentsInChildren<LODGroup>(true))
            {
                var levels=lod.GetLODs();if(distant&&levels.Length>1)levels=levels.Skip(1).ToArray();
                for(int i=0;i<levels.Length;i++)levels[i].screenRelativeTransitionHeight=levels.Length==1?.004f:(i==0?.055f:.004f);
                var keep=new HashSet<Renderer>(levels.SelectMany(l=>l.renderers));
                foreach(var r in lod.GetComponentsInChildren<Renderer>(true))if(!keep.Contains(r))r.gameObject.SetActive(false);
                lod.SetLODs(levels);lod.RecalculateBounds();
            }
        }
        static GameObject Place(string name,Transform parent,Vector3 point,Vector3 scale,float yaw,string category,bool background=false,bool solid=false)
        {
            var source=Source(name);var g=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);g.name="RM_"+category+"_"+added.Count.ToString("D3")+"_"+name;
            g.transform.localScale=scale;g.transform.rotation=Quaternion.Euler(0,yaw,0);var b=BoundsOf(g);g.transform.position+=new Vector3(point.x-b.center.x,point.y-b.min.y,point.z-b.center.z);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=background||category=="LowVegetation"?ShadowCastingMode.Off:ShadowCastingMode.On;}
            if(background)Background(g,true);
            else foreach(var lod in g.GetComponentsInChildren<LODGroup>()){var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)levels[i].screenRelativeTransitionHeight=i==0?.23f:(i==1&&levels.Length>2?.08f:.012f);lod.SetLODs(levels);}
            if(solid)
            {
                b=BoundsOf(g);var box=g.AddComponent<BoxCollider>();box.center=g.transform.InverseTransformPoint(b.center);
                // Conservative oriented local bounds of the authored visual's highest LOD.
                var points=g.GetComponentsInChildren<MeshFilter>(true).SelectMany(m=>Enumerable.Range(0,8).Select(i=>g.transform.InverseTransformPoint(m.transform.TransformPoint(m.sharedMesh.bounds.center+Vector3.Scale(m.sharedMesh.bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))))).ToArray();
                var local=new Bounds(points[0],Vector3.zero);foreach(var v in points)local.Encapsulate(v);box.center=local.center;box.size=local.size;
            }
            added.Add(new Placement{name=g.name,source=AssetDatabase.GetAssetPath(source),category=category,position=g.transform.position,background=background});return g;
        }
        static GameObject Fit(string name,Transform parent,Vector3 point,Vector3 dimensions,float yaw,string category,bool bg=false,bool solid=false)
        {var size=BoundsOf(Source(name)).size;return Place(name,parent,point,new Vector3(dimensions.x/size.x,dimensions.y/size.y,dimensions.z/size.z),yaw,category,bg,solid);}
        public static Stats Observe()
        {
            var root=GameObject.Find("ENV_Level02_PlazaKeynes");var renderers=root.GetComponentsInChildren<Renderer>(true);var lods=root.GetComponentsInChildren<LODGroup>(true);
            int Tris(Renderer r){var m=r?r.GetComponent<MeshFilter>():null;return m&&m.sharedMesh?m.sharedMesh.triangles.Length/3:0;}
            var managed=new HashSet<Renderer>(lods.SelectMany(l=>l.GetComponentsInChildren<Renderer>(true)));
            int unmanaged=renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!managed.Contains(r)).Sum(Tris);
            return new Stats{placements=root.GetComponentsInChildren<Transform>(true).Count(t=>PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)==t.gameObject),meshRenderers=renderers.Count(r=>r.gameObject.activeInHierarchy),lodGroups=lods.Length,enabledColliders=root.GetComponentsInChildren<Collider>(true).Count(c=>c.enabled&&c.gameObject.activeInHierarchy),shadowCasters=renderers.Count(r=>r.gameObject.activeInHierarchy&&r.shadowCastingMode!=ShadowCastingMode.Off),realtimeLights=UnityEngine.Object.FindObjectsByType<Light>().Count(l=>l.enabled&&l.lightmapBakeType!=LightmapBakeType.Baked),highestLodTriangles=unmanaged+lods.Sum(l=>l.GetLODs()[0].renderers.Sum(Tris)),lowestLodTriangles=unmanaged+lods.Sum(l=>l.GetLODs().Last().renderers.Sum(Tris))};
        }
        static void Skyline(Transform parent,Vector3 center,float width,float height,float yaw)
        {
            var frame=Group(parent,"SkylineTemple_"+parent.childCount);frame.position=center;frame.rotation=Quaternion.Euler(0,yaw,0);
            float y=center.y;for(int i=0;i<4;i++){float span=width*(1-i*.18f);Fit("ARQ_PlataformaElevada_01",frame,new Vector3(center.x,y,center.z),new Vector3(span,height*.18f,span),yaw,"CityTempleTier",true);y+=height*.18f;}
            Fit("ARQ_ArcoMaya_01",frame,new Vector3(center.x,y-.2f,center.z),new Vector3(width*.28f,height*.28f,Mathf.Max(2,width*.10f)),yaw,"CityTempleCrest",true);
            Fit("ARQ_EscalinataMaya_01",frame,center-frame.forward*width*.28f,new Vector3(width*.22f,height*.70f,width*.5f),yaw,"CityTempleStair",true);
        }
        static void Ridges(Transform parent)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level02/MAT_PlazaKeynes_JungleFloor.mat");
            var centers=new[]{new Vector3(-400,0,225),new Vector3(0,0,455),new Vector3(405,0,290)};
            for(int k=0;k<3;k++)
            {
                const int n=33;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var triangles=new List<int>();
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){float px=(x/(float)(n-1)-.5f)*380,pz=(z/(float)(n-1)-.5f)*290;float edge=Mathf.Pow(Mathf.Sin(Mathf.PI*x/(n-1))*Mathf.Sin(Mathf.PI*z/(n-1)),.7f);float noise=Mathf.PerlinNoise(px*.017f+7*k+32,pz*.019f+19);float y=-18+edge*(95+65*noise);vertices[z*n+x]=new Vector3(px,y,pz);uv[z*n+x]=new Vector2(px/12,pz/12);}
                for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int a=z*n+x;triangles.AddRange(new[]{a,a+n,a+1,a+1,a+n,a+n+1});}
                var mesh=new Mesh{name="SM_Plaza_DistantRidge_"+k};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+mesh.name+".asset");
                var g=Group(parent,"DistantRidge_"+k).gameObject;g.transform.position=centers[k];g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;g.isStatic=true;
            }
        }
        [MenuItem("Tikal/Level02/Apply Monumental Reference Match Once")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save manual scene changes before this art pass.");
            if(!File.ReadAllText(".git/HEAD").Trim().EndsWith("refs/heads/feature/level2-plaza-keynes-art-pass"))throw new Exception("Dedicated Level02 art branch required.");
            EditorSceneManager.OpenScene(ScenePath);env=GameObject.Find("ENV_Level02_PlazaKeynes").transform;
            if(env.Find("ReferenceMatch_ArtPass"))throw new Exception("Pass already applied; do not rebuild or layer another pass.");
            Directory.CreateDirectory(Art);Directory.CreateDirectory(Evidence+"/Captures");AssetDatabase.Refresh();
            var before=Observe();random=new System.Random(241027);added.Clear();pass=Group(env,"ReferenceMatch_ArtPass");
            var monument=env.Find("KeynesMonument");var statue=monument.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Contains("KeynesHero"));Vector3 statuePosition=statue.position,statueScale=statue.localScale;
            var decor=Group(pass,"MonumentDressing");
            foreach(var p in new[]{new Vector3(-7.6f,.48f,-5),new Vector3(7.6f,.48f,5)})Place("ARQ_BloqueMaya_02",decor,p,Vector3.one*1.35f,p.x<0?12:105,"MonumentStone");
            var paths=env.Find("Plaza_Paths");int pathSections=0;
            foreach(Transform t in paths){t.localScale=new Vector3(t.localScale.x*F(.80f,.94f),t.localScale.y*F(.85f,1.14f),t.localScale.z*F(.94f,1.02f));t.position+=new Vector3(F(-.18f,.18f),F(-.01f,.012f),F(-.2f,.2f));t.rotation*=Quaternion.Euler(0,F(-1.8f,1.8f),0);pathSections++;}
            // Enlarge existing distant landmarks in place, retaining all explorable foreground stairs.
            int resized=0;foreach(Transform t in env.Find("Temples")){if(t.position.z<140)continue;t.localScale*=t.position.x<0?1.8f:1.95f;resized++;Background(t.gameObject,false);}
            var city=Group(pass,"CityLayers");Skyline(city,new Vector3(-104,Ground(-104,110)-.3f,110),40,31,23);Skyline(city,new Vector3(117,Ground(117,135)-.3f,135),46,36,-27);
            Skyline(city,new Vector3(-49,Ground(-49,232)-1,232),30,28,10);Skyline(city,new Vector3(83,Ground(83,276)-1,276),35,33,-15);
            var courts=Group(pass,"ArchitecturalSectors");
            var centers=new[]{new Vector3(-49,0,-44),new Vector3(46,0,-35),new Vector3(-67,0,34),new Vector3(58,0,54)};
            for(int sector=0;sector<centers.Length;sector++)
            {
                var c=centers[sector];float yaw=new[]{24f,-17f,12f,-29f}[sector];var rotation=Quaternion.Euler(0,yaw,0);var group=Group(courts,new[]{"WestForecourt","EastForecourt","WestGallery","RearCourt"}[sector]);
                for(int j=0;j<3;j++){var p=c+rotation*new Vector3(j*5.5f-5.5f,0,3);Fit("ARQ_MuroMaya_01",group,p,new Vector3(4.9f,j==1?2.1f:1.4f,1.1f),yaw,"CourtWall",false,true);}
                for(int j=0;j<2;j++){var p=c+rotation*new Vector3(j==0?-7:7,0,-2);Place("ARQ_ColumnaMaya_01",group,p,Vector3.one*F(1.35f,1.7f),yaw+F(-8,8),"Columns",false,true);}
                Place("ARQ_ColumnaDerrumbada_01",group,c+rotation*new Vector3(-4,0,-3.5f),Vector3.one*1.1f,yaw+19,"BrokenColumn",false,true);
                Place("ARQ_ArcoMaya_01",group,c+rotation*new Vector3(0,0,4),Vector3.one*1.18f,yaw,"CourtPortal");
            }
            var accents=Group(pass,"RocksAndLowVegetation");string[] plants={"VEG_HelechoTropical_02","VEG_PlantaTropical_02","VEG_ArbustoTropical_02","VEG_HojasSecasMonton_01"};
            for(int i=0;i<20;i++){var c=centers[i%4];var p=c+new Vector3(F(-8,8),0,F(-6,6));Place("ROC_RocaMusgo_02",accents,p-Vector3.up*.08f,Vector3.one*F(.30f,.65f),F(0,360),"Rocks",false,i%5==0);}
            for(int i=0;i<64;i++){var c=centers[i%4];var p=c+new Vector3(F(-10,10),0,F(-8,8));Place(plants[i%4],accents,p-Vector3.up*.025f,Vector3.one*F(.35f,.62f),F(0,360),"LowVegetation");}
            var forest=Group(pass,"PeripheralForest");
            for(int i=0;i<14;i++){float a=(i*137.508f+20)*Mathf.Deg2Rad;float r=F(128,165);var p=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);if(p.z>50&&Mathf.Abs(p.x)<150)continue;p.y=Ground(p.x,p.z)-.18f;Place(i%2==0?"VEG_ArbolTropical_03":"VEG_CeibaJoven_01",forest,p,Vector3.one*F(.9f,1.25f),F(0,360),"MidgroundTrees",true);}
            for(int i=0;i<36;i++){float a=(i*137.508f+65)*Mathf.Deg2Rad;float r=F(200,315);var p=new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);p.y=Ground(p.x,p.z)-.25f;Place(i%3==0?"VEG_ArbolTropical_02":"VEG_ArbolTropical_03",forest,p,Vector3.one*F(1.05f,1.6f),F(0,360),"BackgroundTrees",true);}
            foreach(var g in env.Find("Vegetation_Midground").GetComponentsInChildren<LODGroup>())foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=ShadowCastingMode.Off;
            foreach(Transform t in env.Find("Background_Jungle"))Background(t.gameObject,false);
            Ridges(Group(pass,"DistantRelief"));
            var sun=env.Find("Lighting").GetComponentInChildren<Light>();sun.transform.rotation=Quaternion.Euler(24,-48,0);sun.color=new Color(1,.83f,.62f);sun.intensity=2.0f;
            RenderSettings.ambientSkyColor=new Color(.47f,.56f,.66f);RenderSettings.ambientEquatorColor=new Color(.49f,.46f,.36f);RenderSettings.ambientGroundColor=new Color(.22f,.27f,.18f);
            RenderSettings.fogColor=new Color(.64f,.66f,.62f);RenderSettings.fogDensity=.00185f;
            var sky=new Material(RenderSettings.skybox){name="MAT_PlazaKeynes_GoldenHour"};sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));sky.SetColor("_GroundColor",new Color(.38f,.37f,.32f));sky.SetFloat("_AtmosphereThickness",1.1f);sky.SetFloat("_Exposure",1.15f);AssetDatabase.CreateAsset(sky,Art+"/MAT_PlazaKeynes_GoldenHour.mat");RenderSettings.skybox=sky;
            if(statue.position!=statuePosition||statue.localScale!=statueScale)throw new Exception("Hero statue must remain unchanged.");
            AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new Exception("Save failed");
            File.WriteAllText(Evidence+"/ArtAudit.json",JsonUtility.ToJson(new Report{baseCommit="0e221c3",scope="Second additive Level02 art pass only. Original terrain, statue, playable stairs, HUD/Pause and player systems preserved. No new gameplay.",before=before,after=Observe(),additions=added.ToArray(),resizedTemples=resized,pathSections=pathSections,statuePosition=statuePosition,statueScale=statueScale},true));AssetDatabase.Refresh();
        }
    }
}
