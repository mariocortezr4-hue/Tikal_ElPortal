using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class MarioWalkGroundValidation
    {
        static MarioWalkGroundValidation() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
            if(!File.Exists("Temp/MarioFeetAudit.request"))return;
            File.Delete("Temp/MarioFeetAudit.request");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);EditorApplication.delayCall+=Diagnose;
        }
        public const string CanonicalGuid = "31ec003e1e834ebba8fa3954574ab9ae";
        public const string OldGuid = "b09c1f85f39f9c548895867b7b174c1f";
        public const string Walk = MarioGameplayAuthoring.Art + "/Animations/CHR_Explorer_Walk.anim";
        public const float ModelLocalY = -.03436009f;
        // Measured rest separation on flat ground with the unchanged gameplay capsule.
        public const float PhysicalGroundClearance = .04f;
        const string ReportRoot = "Assets/_Project/Documentation/Characters/Mario";
        [Serializable] public class PoseMeasure
        {
            public string clip, guid;
            public float length, frameRate, minSole, maxSole, meanSole, rootY, leftFootMin, rightFootMin;
            public int phases, curveCount;
            public bool humanMotion, loopTime, loopPose;
        }
        [Serializable] public class GroundReport
        {
            public string unity, scope;
            public float modelLocalY, visualRootY, bindMin, avatarScale;
            public PoseMeasure[] poses;
        }
        [MenuItem("Tikal/Validation/Audit Mario Feet Baseline")]
        public static void Diagnose()
        {
            MarioGameplayAuthoring.Require(!Application.isBatchMode && !EditorApplication.isPlaying,"Normal Editor Edit Mode required");
            var report = MeasureAll();
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/MarioFeetDiagnostic.json",JsonUtility.ToJson(report,true));
            Debug.Log("MARIO_FEET_DIAGNOSTIC " + JsonUtility.ToJson(report));
        }
        public static GroundReport MeasureAll()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MarioGameplayAuthoring.Prefab);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var animator=instance.GetComponentInChildren<Animator>(true);
                var report=new GroundReport {unity=Application.unityVersion,scope="Manual PlayableGraph sampling of actual Humanoid Avatar and CPU-skinned sole geometry from current bone matrices; not a gameplay playtest",modelLocalY=animator.transform.localPosition.y,visualRootY=instance.transform.localPosition.y,bindMin=Sole(instance),avatarScale=animator.humanScale};
                report.poses=new[]{"Idle","Walk","Run","CrouchIdle","CrouchWalk","Jump"}.Select(n=>Measure(prefab,AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_"+n+".anim"),n=="CrouchIdle")).ToArray();
                return report;
            }
            finally {UnityEngine.Object.DestroyImmediate(instance);}
        }
        public static PoseMeasure Measure(GameObject prefab, AnimationClip clip, bool held)
        {
            MarioGameplayAuthoring.Require(clip!=null,"Missing clip");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var graph=PlayableGraph.Create("MarioFeetAudit");
            try
            {
                var animator=instance.GetComponentInChildren<Animator>(true);
                animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
                var output=AnimationPlayableOutput.Create(graph,"Pose",animator);output.SetSourcePlayable(playable);graph.Play();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                var result=new PoseMeasure {clip=clip.name,guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip)),length=clip.length,frameRate=clip.frameRate,humanMotion=clip.humanMotion,loopTime=settings.loopTime,loopPose=settings.loopBlend,curveCount=AnimationUtility.GetCurveBindings(clip).Length,minSole=float.PositiveInfinity,maxSole=float.NegativeInfinity,leftFootMin=float.PositiveInfinity,rightFootMin=float.PositiveInfinity,phases=held?1:96};
                for(int i=0;i<result.phases;i++)
                {
                    playable.SetTime(clip.length*(held?.25f:i/96f));graph.Evaluate(0);
                    var sole=Sole(instance);result.minSole=Mathf.Min(result.minSole,sole);result.maxSole=Mathf.Max(result.maxSole,sole);result.meanSole+=sole/result.phases;
                    result.leftFootMin=Mathf.Min(result.leftFootMin,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y-instance.transform.position.y);
                    result.rightFootMin=Mathf.Min(result.rightFootMin,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y-instance.transform.position.y);
                }
                result.rootY=animator.transform.localPosition.y;
                MarioGameplayAuthoring.Require(result.leftFootMin<.6f && result.leftFootMin>-.5f,"Invalid sampled foot pose");
                return result;
            }
            finally {graph.Destroy();UnityEngine.Object.DestroyImmediate(instance);}
        }
        public static float Sole(GameObject instance)
        {
            var minimum=float.PositiveInfinity;
            foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // Explicit skinning reads the actual bone matrices immediately after
                // graph evaluation; BakeMesh can use stale render-update matrices in Edit Mode.
                var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
                var matrices=renderer.bones.Select((bone,i)=>bone.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];var v=vertices[i];
                    var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0
                        +matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1
                        +matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2
                        +matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                    minimum=Mathf.Min(minimum,world.y-instance.transform.position.y);
                }
            }
            return minimum;
        }
        public static void ValidateGround()
        {
            ValidateConfiguration();
            var report=MeasureAll();
            var idle=report.poses.Single(p=>p.clip=="CHR_Explorer_Idle");
            var walk=report.poses.Single(p=>p.clip=="CHR_Explorer_Walk");
            MarioGameplayAuthoring.Require(Mathf.Abs(report.modelLocalY-ModelLocalY)<.00001f,"Calibrated visual/model root offset");
            MarioGameplayAuthoring.Require(Mathf.Abs(idle.meanSole+PhysicalGroundClearance)<.002f && idle.minSole+PhysicalGroundClearance>-.003f,"Idle sole baseline with measured physical ground clearance");
            MarioGameplayAuthoring.Require(Mathf.Abs(walk.meanSole-idle.meanSole)<.002f && walk.minSole+PhysicalGroundClearance>-.01f,"New Walk shares Idle baseline");
            Directory.CreateDirectory(ReportRoot);File.WriteAllText(ReportRoot+"/MarioGroundValidation.json",JsonUtility.ToJson(report,true));
        }
        public static void ValidateConfiguration()
        {
            var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(Walk);
            MarioGameplayAuthoring.Require(walk!=null&&walk.humanMotion&&AssetDatabase.AssetPathToGUID(Walk)==CanonicalGuid,"Canonical approved Walk asset/GUID");
            var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(MarioGameplayAuthoring.Controller);
            MarioGameplayAuthoring.Require(controller.animationClips.Contains(walk),"Controller references canonical Walk");
            MarioGameplayAuthoring.Require(controller.animationClips.All(c=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c))!=OldGuid),"Old Walk no longer active");
            var settings=AnimationUtility.GetAnimationClipSettings(walk);
            MarioGameplayAuthoring.Require(settings.loopTime&&settings.loopBlend,"Walk Loop Time and Loop Pose");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MarioGameplayAuthoring.Prefab);
            MarioGameplayAuthoring.Require(!prefab.GetComponentInChildren<Animator>(true).applyRootMotion,"Root motion remains false");
        }
    }
}