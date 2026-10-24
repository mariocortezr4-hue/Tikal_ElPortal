using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Tikal.Cinematics;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad] public static class IntroCinematicArtPass
    {
        public const string Evidence="../_IntroArtPass_20261023";
        public static readonly double[] Times={23.25,28,34.5,43.5,48.5,59.3,79.5,83};
        public static readonly string[] Shots={"Establishing","ExplorerWalk","TwoShot","ExplorerDialogue","ElderDialogue","MapHandoff","FinalWarning","Departure"};
        static IntroCinematicArtPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/IntroArtPass.request"))return;
            string mode=File.ReadAllText("Temp/IntroArtPass.request").Trim();File.Delete("Temp/IntroArtPass.request");Directory.CreateDirectory(Evidence);
            try{if(mode=="audit")Audit();else if(mode=="apply")Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        [Serializable]class Shot {public string camera;public double time;public Vector3 cameraPosition;public string[] visibleRenderers;public Vector3 elderHead,explorerHead,map;}
        [Serializable]class Report {public Shot[] shots;public string[] assets;}
        public static void Audit()
        {
            if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save scene before audit");var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);var d=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var shots=new List<Shot>();
            for(int i=0;i<Times.Length;i++)
            {
                d.time=Times[i];d.Evaluate();var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.isActiveAndEnabled);var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                Capture(camera,Evidence+"/Audit_"+Shots[i]+".png");
                var elder=ElderCinematicAuthoring.Find("ElderAnchor").GetComponentsInChildren<Animator>().Single(a=>a.avatar!=null);var explorer=ElderCinematicAuthoring.Find("ExplorerAnchor").GetComponentsInChildren<Animator>().Single(a=>a.avatar!=null);
                shots.Add(new Shot{camera=camera.name,time=Times[i],cameraPosition=camera.transform.position,visibleRenderers=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)).Select(r=>r.name).ToArray(),elderHead=elder.GetBoneTransform(HumanBodyBones.Head).position,explorerHead=explorer.GetBoneTransform(HumanBodyBones.Head).position,map=ElderCinematicAuthoring.Find("AncientMap_Proxy").position});
            }
            var assets=new List<string>();foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs/Environment"}).Select(AssetDatabase.GUIDToAssetPath))
            {var o=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var renderers=o.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);assets.Add(path+" | bounds "+b.size.ToString("F2")+" | min "+b.min.ToString("F2"));}UnityEngine.Object.DestroyImmediate(o);}
            File.WriteAllText(Evidence+"/Audit.json",JsonUtility.ToJson(new Report{shots=shots.ToArray(),assets=assets.ToArray()},true));d.Stop();EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);Debug.Log("INTRO_ART_AUDIT_FINISHED");
        }
        public static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);var old=camera.targetTexture;var active=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
        }
        const string Art="Assets/_Project/Art/Environment/IntroFinal";
        static Transform artRoot;static int count;
        [MenuItem("Tikal/Authoring/Apply Intro Cinematic Art Pass")]
        public static void Apply()
        {
            if(EditorSceneManager.GetActiveScene().isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Saved Edit Mode required");
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath);var d=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();if(Math.Abs(d.duration-87.6)>.001)throw new Exception("Keep approved 87.6 second Timeline");
            if(ElderCinematicAuthoring.Find("Environment").Find("Intro final art")!=null){artRoot=ElderCinematicAuthoring.Find("Environment").Find("Intro final art");Refine();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("INTRO_ART_IDEMPOTENT");return;}
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();artRoot=new GameObject("Intro final art").transform;artRoot.SetParent(ElderCinematicAuthoring.Find("Environment"),false);count=0;
            // Preserve all technical objects and colliders; only hide their blockout renderers.
            foreach(var r in ElderCinematicAuthoring.Find("Environment").GetComponentsInChildren<MeshRenderer>(true))if(!r.transform.IsChildOf(artRoot))r.enabled=false;
            var soil=LocalMaterial("Intro forest soil","Assets/_Project/Art/Environment/Level01/EntranceHero/MAT_L01_Entrance_Soil.mat",new Color(.48f,.43f,.31f));
            var trail=LocalMaterial("Intro worn path","Assets/_Project/Art/Environment/Level01/EntranceHero/MAT_Entrance_WornEarth.mat",new Color(.65f,.55f,.38f));
            Surface("Forest terrain",false,soil);Surface("Explorer path",true,trail);
            Place("Tutorial/PFB_ROC_RocaPlana_01",new Vector3(2.3f,.03f,12),new Vector3(1.03f,1.15f,1.1f),-8,false);
            Place("Tutorial/PFB_ROC_RocaPlana_01",new Vector3(2.3f,.42f,12),new Vector3(1,.95f,1),4,false);
            Place("Level01/PFB_ROC_RocaMusgo_01",new Vector3(3.4f,-.08f,12.6f),new Vector3(.65f,.7f,.8f),37,false);
            Place("Level01/PFB_ARQ_ArcoMaya_01",new Vector3(0,-.04f,32),new Vector3(1.1f,1.2f,1),0,false);
            Place("Tutorial/PFB_ARQ_MuroMaya_01",new Vector3(-5.8f,-.05f,19),new Vector3(1.1f,1.1f,1),12,false);
            Place("Tutorial/PFB_ARQ_MuroMaya_01",new Vector3(6.9f,-.08f,16),new Vector3(.9f,.9f,1),-35,false);
            Place("Level01/PFB_ARQ_ColumnaDerrumbada_01",new Vector3(5.9f,-.08f,13.9f),Vector3.one*.75f,24,false);
            Place("Level01/PFB_ARQ_EscombrosMaya_01",new Vector3(-3.3f,-.04f,25),Vector3.one,83,false);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<8;i++)
                {
                    float z=-11+i*7.2f,x=side*(11.8f+Mathf.Sin(i*2.3f)*2.5f);float scale=.72f+(i%3)*.1f;
                    Place(i%3==0?"Level01/PFB_VEG_ArbolTropical_03":"Tutorial/PFB_VEG_ArbolTropical_0"+(i%2==0?"2":"4"),new Vector3(x,Ground(x,z)-.12f,z),Vector3.one*scale,i*47+side*12,true);
                    if(i%2==0)Place("Level01/PFB_VEG_CeibaJoven_01",new Vector3(side*7.5f,Ground(side*7.5f,z+3)-.08f,z+3),Vector3.one*.9f,i*38,true);
                }
                for(int i=0;i<20;i++)
                {
                    float z=-10+i*2.6f,x=side*(4.4f+(i%4)*.9f);float y=Ground(x,z)-.07f;
                    Place(i%3==0?"Tutorial/PFB_VEG_PlantaTropical_02":i%3==1?"Tutorial/PFB_VEG_ArbustoTropical_03":"Tutorial/PFB_VEG_ArbustoTropical_02",new Vector3(x,y,z),Vector3.one*(.8f+(i%3)*.18f),i*73,false);
                    Place("Tutorial/PFB_VEG_HelechoTropical_02",new Vector3(side*(3.2f+(i%3)*.35f),-.04f,z+.8f),Vector3.one*(.6f+(i%2)*.18f),i*91,false);
                }
                for(int i=0;i<6;i++)
                {
                    float z=-4+i*8;float x=side*(5.4f+(i%2)*.8f);Place("Level01/PFB_ROC_RocaMusgo_01",new Vector3(x,Ground(x,z)-.16f,z),Vector3.one*(1.2f+(i%2)*.35f),i*61,false);
                    Place("Level01/PFB_VEG_RaizTropical_01",new Vector3(side*3.8f,-.10f,z+1),Vector3.one*.8f,i*45,false);

                }
                for(int i=0;i<4;i++)Place("Level01/PFB_VEG_CortinaLianas_01",new Vector3(side*(8.5f+i*.7f),6+i*.6f,12+i*7),Vector3.one*1.2f,i*43,true);
            }
            for(int i=0;i<9;i++)Place(i%2==0?"Level01/PFB_VEG_CeibaTropical_01":"Tutorial/PFB_VEG_ArbolTropical_04",new Vector3(-28+i*7,Ground(-28+i*7,52)-.2f,49+(i%3)*4),Vector3.one*(.8f+(i%2)*.2f),i*53,true);
            // Local layers behind the actor reverse angles, without filling their shared acting space.
            for(int i=0;i<5;i++){float z=8+i*2.5f;Place("Tutorial/PFB_VEG_ArbustoTropical_03",new Vector3(7.8f,Ground(7.8f,z)-.08f,z),Vector3.one*1.15f,i*67,false);}
            var sun=ElderCinematicAuthoring.Find("Soft Jungle Light").GetComponent<Light>();sun.color=new Color(1,.85f,.66f);sun.intensity=1.35f;sun.transform.rotation=Quaternion.Euler(48,-38,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.38f,.34f);RenderSettings.ambientEquatorColor=new Color(.22f,.27f,.22f);RenderSettings.ambientGroundColor=new Color(.11f,.105f,.075f);RenderSettings.reflectionIntensity=.45f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.14f,.21f,.18f);RenderSettings.fogDensity=.018f;
            Light("Soft canopy fill",new Vector3(-1.5f,3.1f,9),new Vector3(1.6f,1.2f,11.6f),new Color(.82f,.90f,.86f),3.1f,12,105);
            Light("Warm edge through canopy",new Vector3(5,4.5f,15),new Vector3(1.7f,1.2f,11.5f),new Color(1,.83f,.62f),3.8f,12,95);
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)){camera.backgroundColor=RenderSettings.fogColor;camera.farClipPlane=130;}
            var walk=ElderCinematicAuthoring.Find("CAM_ExplorerWalk");walk.position=new Vector3(-5,2.2f,-6.5f);walk.LookAt(new Vector3(0,1.1f,5));walk.GetComponent<Camera>().fieldOfView=45;
            var carrier=ElderCinematicAuthoring.Find("AncientMap_Proxy");var visual=carrier.Find("Ancient map final visual");var offset=new GameObject("Map visual contact offset").transform;offset.SetParent(carrier,false);offset.localScale=new Vector3(1/carrier.localScale.x,1/carrier.localScale.y,1/carrier.localScale.z);visual.SetParent(offset,false);visual.localScale=Vector3.one;visual.localPosition=new Vector3(-.025f,-.035f,.015f);visual.localRotation=Quaternion.Euler(9,0,3);
            Refine();EditorUtility.SetDirty(sun);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/Placements.txt",count+" existing-prefab placements; zero new colliders; 2 local shadowless fill/rim lights; no Volume changes");Debug.Log("INTRO_ART_APPLIED");
        }
        static void Refine()
        {
            var establishing=ElderCinematicAuthoring.Find("CAM_Establishing");establishing.position=new Vector3(-3.8f,5,-12);establishing.LookAt(new Vector3(0,1.2f,18));establishing.GetComponent<Camera>().fieldOfView=52;
            var departure=ElderCinematicAuthoring.Find("CAM_ExplorerDeparture");departure.position=new Vector3(2.8f,3.2f,5);departure.LookAt(new Vector3(0,1.3f,23));departure.GetComponent<Camera>().fieldOfView=50;
            var handoff=ElderCinematicAuthoring.Find("CAM_MapHandoff");handoff.position=new Vector3(-2.6f,2.35f,12.8f);handoff.LookAt(new Vector3(1.8f,1.15f,11.4f));
            var map=ElderCinematicAuthoring.Find("AncientMap_Proxy").Find("Map visual contact offset").Find("Ancient map final visual");map.localPosition=new Vector3(-.025f,-.065f,.015f);
            foreach(var child in artRoot.Cast<Transform>().Where(t=>t.name.Contains("PFB_VEG_HojasSecasMonton")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            count=artRoot.Cast<Transform>().Count(t=>!string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
            {
                string name="Intro far canopy "+side+"_"+i;if(artRoot.Find(name)!=null)continue;
                var o=Place("Tutorial/PFB_VEG_ArbolTropical_04",new Vector3(side*23,Ground(side*23,2+i*14)-.15f,2+i*14),Vector3.one*.9f,side*32+i*51,true);o.name=name;
            }
            File.WriteAllText(Evidence+"/Placements.txt",count+" existing-prefab placements; zero new colliders; 2 local shadowless fill/rim lights; no Volume changes");
        }
        static Material LocalMaterial(string name,string source,Color tint)
        {var path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(AssetDatabase.LoadAssetAtPath<Material>(source)){name=name};m.SetColor("_BaseColor",tint);AssetDatabase.CreateAsset(m,path);}return m;}
        static float Ground(float x,float z){float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.8f,12,Mathf.Abs(x)));return -.045f+edge*(.3f+Mathf.PerlinNoise(x*.08f+7,z*.08f+4)*2.2f);}
        static void Surface(string name,bool path,Material material)
        {
            int nx=path?8:80,nz=path?80:96;var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var triangles=new int[nx*nz*6];int k=0;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float wz=path?Mathf.Lerp(-14,52,z/(float)nz):Mathf.Lerp(-35,76,z/(float)nz);float wx=path?Mathf.Lerp(-2.3f,2.3f,x/(float)nx)*(1+.08f*Mathf.Sin(wz*.61f)):Mathf.Lerp(-52,52,x/(float)nx);int i=z*(nx+1)+x;v[i]=new Vector3(wx,path?-.005f:Ground(wx,wz),wz);uv[i]=new Vector2(wx/3,wz/3);if(x<nx&&z<nz){triangles[k++]=i;triangles[k++]=i+nx+1;triangles[k++]=i+1;triangles[k++]=i+1;triangles[k++]=i+nx+1;triangles[k++]=i+nx+2;}}
            var mesh=new Mesh{name=name};mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/"+name+".asset");var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(artRoot,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        static GameObject Place(string asset,Vector3 pos,Vector3 scale,float yaw,bool background)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+asset+".prefab");if(prefab==null)throw new Exception(asset);var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab);o.transform.SetParent(artRoot,false);o.transform.position=pos;o.transform.localScale=scale;o.transform.rotation=Quaternion.Euler(0,yaw,0);o.name=(background?"BG_":"ART_")+count.ToString("D3")+"_"+prefab.name;count++;
            foreach(var c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var group in o.GetComponentsInChildren<LODGroup>(true))
            {var lods=group.GetLODs();if(background&&lods.Length>1){int chosen=Math.Min(1,lods.Length-1);foreach(var r in lods.SelectMany(l=>l.renderers).Distinct())r.enabled=lods[chosen].renderers.Contains(r);group.enabled=false;}}
            foreach(var r in o.GetComponentsInChildren<Renderer>(true)){if(background)r.shadowCastingMode=ShadowCastingMode.Off;else if(asset.Contains("VEG_"))r.shadowCastingMode=ShadowCastingMode.Off;}
            return o;
        }
        static void Light(string name,Vector3 pos,Vector3 target,Color color,float intensity,float range,float angle){var o=new GameObject(name);o.transform.SetParent(artRoot,false);o.transform.position=pos;o.transform.LookAt(target);var l=o.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.7f;l.shadows=LightShadows.None;}

    }
}
