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
    // A separate pilot gate: never registers Dwight or changes shared gameplay assets.
    public static class DwightPilotValidation
    {
        public const string Art="Assets/_Project/Art/Characters/Dwight";
        public const string Model=Art+"/Models/CHR_Dwight.fbx";
        public const string Prefab="Assets/_Project/Prefabs/Characters/CHR_Dwight_PlayerVisual.prefab";
        public const string Output="Temp/DwightPilot";
        [Serializable] public class Report
        {
            public bool passed;public string unity,error; public float initialHeight,initialScale,finalHeight,finalScale,humanScale,offset;
            public int triangles,vertices,bones;public string[] mapping,rigMessages;
            public MarioWalkGroundValidation.PoseMeasure[] poses;
        }
        static List<string> rigMessages;
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||text.Contains("Rig Error"))rigMessages.Add(text);}
        public static void ImportPilot()
        {
            var report=new Report{unity=Application.unityVersion};rigMessages=new List<string>();Application.logMessageReceived+=Log;
            GameObject instance=null;
            try
            {
                AxelGameplayAuthoring.Require(!Application.isBatchMode&&!EditorApplication.isPlaying,"Interactive Edit Mode pilot");
                Directory.CreateDirectory(Output);
                AxelGameplayAuthoring.Require(!File.Exists(Prefab),"Pilot import only runs before a visual prefab exists; official authoring preserves the validated import");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var imp=(ModelImporter)AssetImporter.GetAtPath(Model);
                imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.NoAvatar;imp.importAnimation=false;imp.isReadable=true;
                imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.globalScale=1;imp.SaveAndReimport();
                instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
                report.initialScale=imp.globalScale;report.initialHeight=AxelGameplayAuthoring.GeometryBounds(instance).size.y;
                UnityEngine.Object.DestroyImmediate(instance);instance=null;
                // Keep a natural 1.82 m stature; apply scale while Generic BEFORE fresh Humanoid creation.
                imp.globalScale=1.82f/report.initialHeight;imp.SaveAndReimport();
                imp.humanDescription=new HumanDescription{human=Array.Empty<HumanBone>(),skeleton=Array.Empty<SkeletonBone>(),armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f};
                imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.SaveAndReimport();
                AssetDatabase.ImportAsset(Model,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();
                AxelGameplayAuthoring.Require(avatar.isHuman&&avatar.isValid&&rigMessages.Count==0,"Valid own Humanoid AND zero Rig Errors");
                var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/CHR_Dwight_basecolor.jpg");ti.sRGBTexture=true;ti.maxTextureSize=4096;ti.SaveAndReimport();
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="MAT_Dwight_Body";
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/CHR_Dwight_basecolor.jpg"));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.25f);
                AssetDatabase.CreateAsset(material,Art+"/Materials/MAT_Dwight_Body.mat");
                instance=new GameObject("CHR_Dwight_PlayerVisual");var wrapper=new GameObject("DwightVisualOrientation");wrapper.transform.SetParent(instance.transform,false);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));model.name="DwightModel";model.transform.SetParent(wrapper.transform,false);
                var a=model.GetComponent<Animator>();a.avatar=avatar;a.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AxelGameplayAuthoring.Controller);a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterials=Enumerable.Repeat(material,skin.sharedMaterials.Length).ToArray();
                report.mapping=Enumerable.Range(0,(int)HumanBodyBones.LastBone).Select(i=>(HumanBodyBones)i).Where(b=>a.GetBoneTransform(b)!=null).Select(b=>b+" = "+a.GetBoneTransform(b).name).ToArray();
                foreach(var b in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot})AxelGameplayAuthoring.Require(a.GetBoneTransform(b)!=null,"Critical mapping "+b);
                report.finalScale=imp.globalScale;report.finalHeight=AxelGameplayAuthoring.GeometryBounds(instance).size.y;report.humanScale=a.humanScale;
                var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();report.triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3);report.vertices=skins.Sum(s=>s.sharedMesh.vertexCount);report.bones=skins.Sum(s=>s.bones.Length);
                PrefabUtility.SaveAsPrefabAsset(instance,Prefab);UnityEngine.Object.DestroyImmediate(instance);instance=null;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
                var idle=MarioWalkGroundValidation.Measure(prefab,AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_Idle.anim"),false);
                // Shared capsule rests 4 cm above Y=0. Calibrate only a constant visual offset.
                report.offset=-idle.meanSole-MarioWalkGroundValidation.PhysicalGroundClearance;
                AxelGameplayAuthoring.Require(Mathf.Abs(report.offset)<.15f,"Small constant visual offset");
                instance=PrefabUtility.LoadPrefabContents(Prefab);instance.transform.Find("DwightVisualOrientation").localPosition=Vector3.up*report.offset;PrefabUtility.SaveAsPrefabAsset(instance,Prefab);PrefabUtility.UnloadPrefabContents(instance);instance=null;
                report.poses=new[]{"Idle","Walk","Run","Jump","CrouchIdle","CrouchWalk"}.Select(n=>MarioWalkGroundValidation.Measure(prefab,AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_"+n+".anim"),n=="CrouchIdle")).ToArray();
                foreach(var n in new[]{"Idle","Walk","Run","Jump","CrouchIdle","CrouchWalk"})Capture(prefab,n);
                Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(Model);EditorGUIUtility.PingObject(Selection.activeObject);
                report.passed=rigMessages.Count==0;
            }
            catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);Application.logMessageReceived-=Log;report.rigMessages=rigMessages.ToArray();Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Pilot.json",JsonUtility.ToJson(report,true));AssetDatabase.SaveAssets();Debug.Log("DWIGHT_PILOT passed="+report.passed);}
        }
        static void Capture(GameObject prefab,string name)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var a=go.GetComponentInChildren<Animator>();var graph=PlayableGraph.Create("DwightPilot");
            var camera=new GameObject("DwightPilotCamera").AddComponent<Camera>();var light=new GameObject("DwightPilotLight").AddComponent<Light>();var rt=new RenderTexture(768,768,24);
            try
            {
                a.runtimeAnimatorController=null;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_"+name+".anim");var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(playable);graph.Play();playable.SetTime(clip.length*.25);graph.Evaluate(0);
                camera.transform.position=new Vector3(2.2f,1.5f,3.2f);camera.transform.LookAt(Vector3.up*.85f);camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.18f,.2f);light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-30,0);
                rt.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(768,768,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,768,768),0,0);texture.Apply();File.WriteAllBytes(Output+"/Pilot_"+name+".png",texture.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);
            }finally{graph.Destroy();rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
