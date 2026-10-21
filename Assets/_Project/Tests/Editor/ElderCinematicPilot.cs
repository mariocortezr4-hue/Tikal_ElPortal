using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tikal.Tests.Editor
{
    public static class ElderCinematicPilot
    {
        public const string Art="Assets/_Project/Art/Characters/Elder";
        public const string Model=Art+"/Models/CHR_Elder.fbx";
        public const string Prefab="Assets/_Project/Prefabs/Characters/CHR_Elder_Cinematic.prefab";
        public const string Output="Temp/ElderCinematicPilot";
        [Serializable] public class Sample {public float phase;public Vector3 hips,head,leftHand,rightHand,leftFoot,rightFoot,min,max;}
        [Serializable] public class Motion {public string file,name;public float duration;public bool human,loop;public Sample[] samples;}
        [Serializable] public class Report {public bool passed;public string error,unity;public float initialHeight,initialScale,finalHeight,scale,humanScale;public int triangles,vertices,bones;public string[] mapping,rigErrors;public Motion[] motions;}
        static List<string> errors;
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||text.Contains("Rig Error"))errors.Add(text);}
        public static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Elder: "+message);}
        public static void Import()
        {
            var result=new Report{unity=Application.unityVersion};errors=new List<string>();Application.logMessageReceived+=Log;GameObject root=null;
            try
            {
                Require(!Application.isBatchMode&&!EditorApplication.isPlaying,"Normal Editor required");Directory.CreateDirectory(Output);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var imp=(ModelImporter)AssetImporter.GetAtPath(Model);imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.NoAvatar;imp.globalScale=1;imp.importAnimation=false;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.SaveAndReimport();
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));result.initialHeight=AxelGameplayAuthoring.GeometryBounds(root).size.y;result.initialScale=imp.globalScale;UnityEngine.Object.DestroyImmediate(root);root=null;
                imp.globalScale=1.72f/result.initialHeight;imp.SaveAndReimport();imp.humanDescription=new HumanDescription{human=Array.Empty<HumanBone>(),skeleton=Array.Empty<SkeletonBone>(),armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f};imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.SaveAndReimport();
                var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();Require(avatar.isValid&&avatar.isHuman&&errors.Count==0,"Own valid Humanoid / zero Rig Errors");
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/CHR_Elder_basecolor.jpeg");var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/MAT_Elder_Body.mat");if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="MAT_Elder_Body";material.SetTexture("_BaseMap",texture);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(material,Art+"/Materials/MAT_Elder_Body.mat");}
                root=new GameObject("CHR_Elder_Cinematic");var visual=new GameObject("VisualRoot");visual.transform.SetParent(root.transform,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));model.name="ElderModel";model.transform.SetParent(visual.transform,false);var a=model.GetComponent<Animator>();a.avatar=avatar;a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                result.mapping=Enumerable.Range(0,(int)HumanBodyBones.LastBone).Select(i=>(HumanBodyBones)i).Where(b=>a.GetBoneTransform(b)!=null).Select(b=>b+"="+a.GetBoneTransform(b).name).ToArray();result.scale=imp.globalScale;result.finalHeight=AxelGameplayAuthoring.GeometryBounds(root).size.y;result.humanScale=a.humanScale;var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();result.triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3);result.vertices=skins.Sum(s=>s.sharedMesh.vertexCount);result.bones=skins.Sum(s=>s.bones.Length);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);UnityEngine.Object.DestroyImmediate(root);root=null;
                var motions=new List<Motion>();
                foreach(var path in Directory.GetFiles(Art+"/Animations","*.fbx").OrderBy(x=>x))
                {
                    var ci=(ModelImporter)AssetImporter.GetAtPath(path);
                    // Source animation skeletons differ in hierarchy/proportions. Retarget through their own Avatars.
                    ci.animationType=ModelImporterAnimationType.Generic;ci.avatarSetup=ModelImporterAvatarSetup.NoAvatar;ci.sourceAvatar=null;ci.globalScale=imp.globalScale;ci.materialImportMode=ModelImporterMaterialImportMode.None;ci.importAnimation=true;ci.SaveAndReimport();
                    ci.humanDescription=new HumanDescription{human=Array.Empty<HumanBone>(),skeleton=Array.Empty<SkeletonBone>(),armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f};ci.animationType=ModelImporterAnimationType.Human;ci.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;ci.SaveAndReimport();
                    var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();Require(source.isHuman&&source.isValid,"Valid own source clip Avatar "+path);
                    var settings=ci.defaultClipAnimations;foreach(var s in settings){s.name=Path.GetFileNameWithoutExtension(path);s.loopTime=path.Contains("Seated_Idle")||path.Contains("Sitting Talking");s.loopPose=s.loopTime;s.lockRootRotation=true;s.lockRootPositionXZ=true;s.lockRootHeightY=true;s.keepOriginalOrientation=true;s.keepOriginalPositionXZ=true;s.keepOriginalPositionY=true;}ci.clipAnimations=settings;ci.SaveAndReimport();
                    var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));Require(clip.humanMotion,"Humanoid clip "+path);motions.Add(Audition(path,clip));
                }
                result.motions=motions.ToArray();Require(errors.Count==0,"Zero rig/import errors throughout pilot");result.passed=true;
                Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            }
            catch(Exception e){result.error=e.ToString();Debug.LogException(e);}
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);Application.logMessageReceived-=Log;result.rigErrors=errors.ToArray();File.WriteAllText(Output+"/Pilot.json",JsonUtility.ToJson(result,true));AssetDatabase.SaveAssets();Debug.Log("ELDER_PILOT passed="+result.passed);}
        }
        public static void Reaudition()
        {
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Output+"/Pilot.json"));
            report.motions=Directory.GetFiles(Art+"/Animations","*.fbx").OrderBy(p=>p).Select(p=>Audition(p,AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")))).ToArray();
            File.WriteAllText(Output+"/Pilot.json",JsonUtility.ToJson(report,true));Debug.Log("ELDER_CPU_AUDITION_FINISHED");
        }
        static Motion Audition(string path,AnimationClip clip)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var a=root.GetComponentInChildren<Animator>();var graph=PlayableGraph.Create("ElderAudition");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(p);graph.Play();var samples=new List<Sample>();
                for(int i=0;i<=12;i++)
                {
                    float phase=i/12f;p.SetTime(clip.length*phase);graph.Evaluate(0);var bounds=AxelGameplayAuthoring.GeometryBounds(root);
                    samples.Add(new Sample{phase=phase,hips=a.GetBoneTransform(HumanBodyBones.Hips).position,head=a.GetBoneTransform(HumanBodyBones.Head).position,leftHand=a.GetBoneTransform(HumanBodyBones.LeftHand).position,rightHand=a.GetBoneTransform(HumanBodyBones.RightHand).position,leftFoot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position,rightFoot=a.GetBoneTransform(HumanBodyBones.RightFoot).position,min=bounds.min,max=bounds.max});
                    if(i==0||i==3||i==6||i==9)Capture(root,Path.GetFileNameWithoutExtension(path)+"_"+i);
                }
                return new Motion{file=path,name=clip.name,duration=clip.length,human=clip.humanMotion,loop=AnimationUtility.GetAnimationClipSettings(clip).loopTime,samples=samples.ToArray()};
            }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);}
        }
        static void Capture(GameObject root,string name)
        {
            var baked=new List<GameObject>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrices=skin.bones.Select((bone,i)=>bone.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();var go=new GameObject("CpuPose",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;baked.Add(go);skin.enabled=false;
            }
            var camera=new GameObject("ElderAuditCamera").AddComponent<Camera>();var sun=new GameObject("ElderAuditSun").AddComponent<Light>();var rt=new RenderTexture(640,640,24);
            try
            {
                camera.transform.position=new Vector3(2.3f,1.5f,3);camera.transform.LookAt(Vector3.up*.8f);camera.fieldOfView=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.18f);sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(45,-30,0);rt.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(640,640,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,640,640),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);
            }finally{foreach(var go in baked){UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);}foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.enabled=true;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(sun.gameObject);}
        }
    }
}
