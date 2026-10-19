using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class MarioAnimationFeelValidation
    {
        static MarioAnimationFeelValidation(){EditorApplication.delayCall+=RequestedAudit;}
        static void RequestedAudit(){
            if(!File.Exists("Temp/MarioFeelAudit.request"))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=RequestedAudit;return;}
            File.Delete("Temp/MarioFeelAudit.request");Audit();
        }
        public const float WalkPlaybackBefore=4.292876f;
        public const float WalkPlayback=2.2f;
        public const float RunPlaybackBefore=1.7278966f;
        public const float RunPlayback=1.5896648f;
        [Serializable]public class ProfileReport {
            public float walkPlaybackBefore,walkPlayback,runPlaybackBefore,runPlayback,walkPhysicalSpeed,sprintPhysicalSpeed;
            public float[] thresholdsBefore,thresholds;
            public bool thresholdsChanged,footIK,rootMotion;
            public string slidingStatus,scope;
        }
        static UnityEditor.Animations.BlendTree Tree(UnityEditor.Animations.AnimatorController controller) =>
            controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state.motion as UnityEditor.Animations.BlendTree;
        public static void ApplyProfile(UnityEditor.Animations.AnimatorController controller) {
            var tree=Tree(controller);MarioGameplayAuthoring.Require(tree!=null,"Existing shared locomotion Blend Tree");
            var children=tree.children;MarioGameplayAuthoring.Require(children.Length==3,"Idle/Walk/Run children");
            if(Mathf.Abs(children[1].timeScale-WalkPlayback)<.000001f&&Mathf.Abs(children[2].timeScale-RunPlayback)<.000001f)return;
            children[1].timeScale=WalkPlayback;children[2].timeScale=RunPlayback;tree.children=children;
            EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);
        }
        public static ProfileReport ValidateProfile() {
            var movement=AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath).GetComponent<Tikal.Player.PlayerMovement>();
            MarioGameplayAuthoring.Require(movement.WalkSpeed==4f&&movement.SprintSpeed==6.5f&&movement.CrouchSpeed==2f,"Approved physical movement speeds unchanged");
            var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(MarioGameplayAuthoring.Controller);
            var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();var locomotion=states.Single(s=>s.name=="Locomotion");var tree=Tree(controller);var children=tree.children;
            MarioGameplayAuthoring.Require(tree.blendParameter=="Speed"&&children.Length==3&&children.All(c=>c.motion!=null),"Speed meaning/complete Idle Walk Run tree");
            MarioGameplayAuthoring.Require(Mathf.Abs(children[1].timeScale-WalkPlayback)<.000001f&&Mathf.Abs(children[2].timeScale-RunPlayback)<.000001f,"Independent Walk/Run feel rates");
            MarioGameplayAuthoring.Require(Mathf.Abs(children[1].threshold-movement.WalkSpeed/movement.SprintSpeed)<.000001f&&children[0].threshold==0&&children[2].threshold==1,"Approved normalized thresholds unchanged");
            MarioGameplayAuthoring.Require(locomotion.speed==1&&!locomotion.speedParameterActive&&children[0].timeScale==1,"No global/state multiplier changes Idle");
            MarioGameplayAuthoring.Require(states.Where(s=>s.name=="Air"||s.name=="Land").All(s=>s.speed==1&&s.timeParameterActive&&s.timeParameter=="JumpPhase"),"Jump playback/time parameter unchanged");
            MarioGameplayAuthoring.Require(Mathf.Abs(states.Single(s=>s.name=="CrouchWalk").speed-1.6366963f)<.000001f&&states.Single(s=>s.name=="CrouchIdle").timeParameter=="CrouchPose","Crouch playback unchanged");
            MarioGameplayAuthoring.Require(!locomotion.iKOnFeet&&!controller.layers[0].iKPass,"No unverified Foot IK introduced");
            MarioWalkGroundValidation.ValidateConfiguration();
            return new ProfileReport{walkPlaybackBefore=WalkPlaybackBefore,walkPlayback=children[1].timeScale,runPlaybackBefore=RunPlaybackBefore,runPlayback=children[2].timeScale,walkPhysicalSpeed=movement.WalkSpeed,sprintPhysicalSpeed=movement.SprintSpeed,thresholdsBefore=new[]{0f,4f/6.5f,1f},thresholds=children.Select(c=>c.threshold).ToArray(),thresholdsChanged=false,footIK=false,rootMotion=false,slidingStatus="SEVERE low-foot velocity proxy for 2.2x at 4 m/s; perceptual acceptance pending human playtest",scope="Animation feel initial compromise; does not claim NONE/MINOR sliding"};
        }
        const string TempRoot="Temp/MarioFeelAudit";
        [Serializable]public class Candidate
        {
            public string clip, slippingHeuristic;
            public float playback, physicalSpeed, cycleSeconds, estimatedStepsPerSecond;
            public float meanContactSlipSpeed, contactTravelPerSample, cycleClosure;
            public int contactSamples;
        }
        [Serializable]public class AuditReport
        {
            public string unity, scope;
            public Candidate[] candidates;
        }
        [MenuItem("Tikal/Validation/Audit Mario Animation Feel")]
        public static void Audit()
        {
            MarioGameplayAuthoring.Require(!Application.isBatchMode&&!EditorApplication.isPlaying,"Interactive Editor Edit Mode required");
            Directory.CreateDirectory(TempRoot);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MarioGameplayAuthoring.Prefab);
            var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioWalkGroundValidation.Walk);
            var run=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_Run.anim");
            var rates=new[]{1f,1.15f,1.25f,1.4f,1.6f,1.8f,2.2f,2.6f,2.8f,4.292876f};
            var report=new AuditReport {unity=Application.unityVersion,scope="192 phase Humanoid bone sampling. Slip is a low-height foot-bone velocity proxy at prescribed physical travel; not a measured gameplay contact constraint or human approval.",candidates=rates.Select(r=>Sample(prefab,walk,r,4)).Concat(new[]{1.7278966f,1.7278966f*.88f,1.7278966f*.92f,1.7278966f*.95f}.Select(r=>Sample(prefab,run,r,6.5f))).ToArray()};
            File.WriteAllText(TempRoot+"/Candidates.json",JsonUtility.ToJson(report,true));
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
                var sun=new GameObject("FeelAuditSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(0,-.5f,20);ground.transform.localScale=new Vector3(10,1,100);
                var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.20f,.26f,.23f));ground.GetComponent<Renderer>().sharedMaterial=mat;
                var stripeMat=new Material(mat);stripeMat.SetColor("_BaseColor",new Color(.4f,.45f,.4f));
                for(int i=-10;i<60;i++){var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);stripe.transform.position=new Vector3(0,.001f,i);stripe.transform.localScale=new Vector3(10,.002f,.035f);stripe.GetComponent<Renderer>().sharedMaterial=stripeMat;}
                foreach(var rate in new[]{1f,1.4f,1.6f,1.8f,2.2f,2.6f,2.8f,4.292876f})Capture(prefab,walk,rate,4,"Side");
                foreach(var rate in new[]{1.7278966f,1.7278966f*.88f,1.7278966f*.92f,1.7278966f*.95f})Capture(prefab,run,rate,6.5f,"Side");
                foreach(var view in new[]{"Front","Rear"}){Capture(prefab,walk,WalkPlayback,4,view);Capture(prefab,run,RunPlayback,6.5f,view);}
                UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(stripeMat);
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;}
            File.WriteAllText(TempRoot+"/Done.txt","Completed");Debug.Log("MARIO_FEEL_AUDIT_FINISHED");
        }
        static Candidate Sample(GameObject prefab,AnimationClip clip,float rate,float speed)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var graph=PlayableGraph.Create("FeelSamples");
            try
            {
                var animator=instance.GetComponentInChildren<Animator>(true);animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();
                var left=new Vector3[193];var right=new Vector3[193];
                for(int i=0;i<=192;i++){playable.SetTime(clip.length*i/192d);graph.Evaluate(0);left[i]=instance.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);right[i]=instance.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position);}
                var result=new Candidate{clip=clip.name,playback=rate,physicalSpeed=speed,cycleSeconds=clip.length/rate,estimatedStepsPerSecond=2*rate/clip.length,cycleClosure=Mathf.Max(Vector3.Distance(left[0],left[192]),Vector3.Distance(right[0],right[192]))};
                var dt=clip.length/192f/rate;
                foreach(var foot in new[]{left,right})
                {
                    var minimum=foot.Min(p=>p.y);
                    for(int i=1;i<=192;i++)if(foot[i].y<minimum+.02f&&foot[i-1].y<minimum+.02f)
                    {
                        var velocity=(foot[i].z-foot[i-1].z)/dt+speed;
                        result.meanContactSlipSpeed+=Mathf.Abs(velocity);result.contactSamples++;
                    }
                }
                if(result.contactSamples>0)result.meanContactSlipSpeed/=result.contactSamples;
                // 0.2 seconds is an illustrative stance window, not an inferred contact duration.
                result.contactTravelPerSample=result.meanContactSlipSpeed*.2f;
                result.slippingHeuristic=result.contactTravelPerSample<.02f?"NONE":result.contactTravelPerSample<.1f?"MINOR":result.contactTravelPerSample<.25f?"NOTICEABLE":"SEVERE";
                return result;
            }
            finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(instance);}
        }
        static void Capture(GameObject prefab,AnimationClip clip,float rate,float speed,string view)
        {
            var name=clip.name+"_"+rate.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+view;
            Directory.CreateDirectory(TempRoot+"/"+name);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var graph=PlayableGraph.Create("FeelRender");
            var camera=new GameObject("FeelAuditCamera").AddComponent<Camera>();camera.fieldOfView=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.2f);camera.allowHDR=false;
            var rt=new RenderTexture(360,360,24);rt.Create();var image=new Texture2D(360,360,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                var animator=instance.GetComponentInChildren<Animator>(true);animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();
                // Render CPU-skinned snapshots: repeated submissions in one Editor tick
                // otherwise reuse the GPU skinning matrices of the first sampled pose.
                var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                var proxies=skins.Select(r=>{
                    var go=new GameObject("PoseSnapshot");go.transform.SetParent(instance.transform,false);
                    var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;r.enabled=false;return go;
                }).ToArray();
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                for(int i=0;i<36;i++)
                {
                    var time=i/15f;instance.transform.position=new Vector3(0,.04f,time*speed);playable.SetTime((time*rate)%clip.length);graph.Evaluate(0);
                    var offset=view=="Side"?new Vector3(3.8f,1.3f,0):view=="Front"?new Vector3(0,1.3f,3.8f):new Vector3(0,1.3f,-3.8f);
                    for(int ri=0;ri<skins.Length;ri++){
                        var skin=skins[ri];var source=skin.sharedMesh;var vertices=source.vertices;var normals=source.normals;var weights=source.boneWeights;
                        var matrices=skin.bones.Select((b,j)=>instance.transform.worldToLocalMatrix*b.localToWorldMatrix*source.bindposes[j]).ToArray();
                        for(int vi=0;vi<vertices.Length;vi++){
                            var w=weights[vi];var v=vertices[vi];var n=normals[vi];
                            vertices[vi]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                            normals[vi]=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;
                        }
                        var mesh=proxies[ri].GetComponent<MeshFilter>().sharedMesh;mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
                    }
                    camera.transform.position=instance.transform.position+offset;camera.transform.LookAt(instance.transform.position+Vector3.up*.85f);
                    RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,360,360),0,0);image.Apply();File.WriteAllBytes(TempRoot+"/"+name+"/"+i.ToString("D3")+".png",image.EncodeToPNG());
                }
                File.WriteAllText(TempRoot+"/Progress.txt",name);foreach(var proxy in proxies)UnityEngine.Object.DestroyImmediate(proxy.GetComponent<MeshFilter>().sharedMesh);
            }
            finally {RenderTexture.active=previous;graph.Destroy();UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
        }
    }
}