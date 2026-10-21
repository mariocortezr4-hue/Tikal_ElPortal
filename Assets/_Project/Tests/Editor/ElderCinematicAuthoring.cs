using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Cinematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Tests.Editor
{
    public static class ElderCinematicAuthoring
    {
        public const string Reports="Assets/_Project/Documentation/Characters/Elder";
        public const string ElderTrack="Elder Seated Performance";
        public const string ExplorerTrack="Explorer Cinematic Visual";
        public const float VisualY=-.52f;
        public static readonly string[] Cameras={"CAM_Establishing","CAM_ExplorerWalk","CAM_TwoShot","CAM_ExplorerDialogue","CAM_ElderDialogue","CAM_MapHandoff","CAM_FinalWarning","CAM_ExplorerDeparture"};
        public static AnimationClip Idle=>Clip("CHR_Anciano_Seated_Idle");
        public static AnimationClip Talk=>Clip("Sitting Talking");
        public static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(ElderCinematicPilot.Art+"/Animations/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        public static IEnumerable<TrackAsset> Tracks(TimelineAsset timeline)=>timeline.GetRootTracks().SelectMany(Flatten);
        static IEnumerable<TrackAsset> Flatten(TrackAsset t){yield return t;foreach(var c in t.GetChildTracks())foreach(var d in Flatten(c))yield return d;}
        public static Transform Find(string name)=>UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Single(t=>t.name==name);
        static void Require(bool value,string message)=>ElderCinematicPilot.Require(value,message);
        [MenuItem("Tikal/Authoring/Apply Elder Cinematic Character")]
        public static void Apply()
        {
            Require(!Application.isBatchMode&&!EditorApplication.isPlayingOrWillChangePlaymode,"Interactive Edit Mode required");
            // Preserve the later polish rather than reapplying historical timestamps.
            var currentTimeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(AncientMapIntroAuthoring.TimelinePath);
            if(currentTimeline!=null && Tracks(currentTimeline).OfType<AudioTrack>().Any(t=>t.name=="Music"&&t.GetClips().Any())) { IntroAudioPolishAuthoring.Apply(); return; }
            Require(!EditorSceneManager.GetActiveScene().isDirty,"Save current scene before authoring");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ElderCinematicPilot.Prefab);Require(prefab!=null,"Pilot prefab exists");
            var avatar=prefab.GetComponentInChildren<Animator>(true).avatar;Require(avatar!=null&&avatar.isHuman&&avatar.isValid,"Validated own Elder Avatar");
            var contents=PrefabUtility.LoadPrefabContents(ElderCinematicPilot.Prefab);bool changed=false;
            try
            {
                var visual=contents.transform.Find("VisualRoot");var desired=new Vector3(0,VisualY,.3f);if(visual.localPosition!=desired){visual.localPosition=desired;changed=true;}
                var a=contents.GetComponentInChildren<Animator>(true);if(a.applyRootMotion||a.runtimeAnimatorController!=null){a.applyRootMotion=false;a.runtimeAnimatorController=null;changed=true;}
                if(changed)PrefabUtility.SaveAsPrefabAsset(contents,ElderCinematicPilot.Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(contents);}
            var scene=EditorSceneManager.OpenScene(AncientMapIntroAuthoring.ScenePath,OpenSceneMode.Single);
            var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();var timeline=(TimelineAsset)director.playableAsset;
            var elder=Find("ElderAnchor");var explorer=Find("ExplorerAnchor");
            var real=elder.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.avatar==avatar);
            if(real==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,elder);go.name="CHR_Elder_Cinematic";real=go.GetComponentInChildren<Animator>(true);}
            var placeholder=elder.Find("ElderPlaceholder");if(placeholder!=null)placeholder.gameObject.SetActive(false);
            var visualRoot=explorer.Find("CharacterVisualRoot");var dwight=visualRoot.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.avatar!=null);
            if(dwight==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DwightGameplayAuthoring.Prefab),visualRoot);go.name="DwightCinematicVisual";dwight=go.GetComponentInChildren<Animator>(true);}
            visualRoot.Find("ExplorerPlaceholder").gameObject.SetActive(false);dwight.applyRootMotion=false;dwight.runtimeAnimatorController=null;
            // Scene-only override: the cinematic has no gameplay capsule's 4 cm clearance.
            var dwightWrapper=dwight.transform.parent;dwightWrapper.localPosition=new Vector3(0,DwightGameplayAuthoring.VisualOffset+.04f,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(dwight);PrefabUtility.RecordPrefabInstancePropertyModifications(dwightWrapper);
            var blocking=Tracks(timeline).OfType<AnimationTrack>().Single(t=>t.name=="ExplorerAnchor Movement");
            var blockingAsset=(AnimationPlayableAsset)blocking.GetClips().Single().asset;blockingAsset.removeStartOffset=false;EditorUtility.SetDirty(blockingAsset);
            var blockingClip=blockingAsset.clip;
            foreach(var binding in AnimationUtility.GetCurveBindings(blockingClip))
            {
                var curve=AnimationUtility.GetEditorCurve(blockingClip,binding);var keys=curve.keys;bool edit=false;
                for(int i=0;i<keys.Length;i++)if(Mathf.Abs(keys[i].time-45)<.01f||Mathf.Abs(keys[i].time-80)<.01f)
                {float v=binding.propertyName.EndsWith(".x")?1.3f:binding.propertyName.EndsWith(".z")?10.9f:0;if(keys[i].value!=v){keys[i].value=v;edit=true;}}
                if(edit){curve.keys=keys;AnimationUtility.SetEditorCurve(blockingClip,binding,curve);EditorUtility.SetDirty(blockingClip);}
            }
            var group=timeline.GetRootTracks().OfType<GroupTrack>().Single(t=>t.name=="Blocking");
            if(!Tracks(timeline).Any(t=>t.name==ElderTrack))
            {
                var track=timeline.CreateTrack<AnimationTrack>(group,ElderTrack);track.trackOffset=TrackOffset.ApplyTransformOffsets;track.position=Vector3.zero;track.rotation=Quaternion.identity;
                Add(track,Idle,"Seated Idle / Listen",0,48.25,0,1,new Vector3(0,-.04f,.244f),true);
                Add(track,Talk,"Seated Talk: questions",48,53.25,2.9,1.3/5.25,Vector3.zero,false);
                Add(track,Idle,"Seated Listen",53,57.65,0,1,new Vector3(0,-.04f,.244f),true);
                Add(track,Talk,"Seated Talk: stories",57.4,65.25,2.9,1.3/7.85,Vector3.zero,false);
                Add(track,Talk,"Seated Give Map (existing reach excerpt)",65,71.45,0,.4/6.45,Vector3.zero,false);
                Add(track,Talk,"Seated Talk: hidden entrance",71.2,80.25,2.9,1.3/9.05,Vector3.zero,false);
                Add(track,Idle,"Seated Listen / departure",80,87.25,0,1,new Vector3(0,-.04f,.244f),true);
                Add(track,Talk,"Seated Final Warning",87,94.25,2.9,1.3/7.25,Vector3.zero,false);
                Add(track,Idle,"Seated Idle final",94,97,0,1,new Vector3(0,-.04f,.244f),true);
                director.SetGenericBinding(track,real);
            }
            else director.SetGenericBinding(Tracks(timeline).Single(t=>t.name==ElderTrack),real);
            if(!Tracks(timeline).Any(t=>t.name==ExplorerTrack))
            {
                var track=timeline.CreateTrack<AnimationTrack>(group,ExplorerTrack);track.trackOffset=TrackOffset.ApplyTransformOffsets;
                var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioGameplayAuthoring.Art+"/Animations/CHR_Explorer_Idle.anim");var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioWalkGroundValidation.Walk);
                Add(track,idle,"Dwight Idle",0,35.25,0,1,Vector3.zero,true);Add(track,walk,"Dwight approach",35,45.25,0,1,Vector3.zero,true);Add(track,idle,"Dwight listens",45,80.25,0,1,Vector3.zero,true);Add(track,walk,"Dwight departure",80,87.25,0,1,Vector3.zero,true);Add(track,idle,"Dwight hears warning",87,94.25,0,1,Vector3.zero,true);Add(track,walk,"Dwight leaves",94,97,0,1,Vector3.zero,true);
                director.SetGenericBinding(track,dwight);
            }
            else director.SetGenericBinding(Tracks(timeline).Single(t=>t.name==ExplorerTrack),dwight);
            var map=Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();
            if(constraint==null)
            {
                constraint=map.gameObject.AddComponent<ParentConstraint>();constraint.translationAxis=Axis.X|Axis.Y|Axis.Z;constraint.rotationAxis=Axis.None;
                constraint.AddSource(new ConstraintSource{sourceTransform=real.GetBoneTransform(HumanBodyBones.RightHand),weight=1});constraint.AddSource(new ConstraintSource{sourceTransform=dwight.GetBoneTransform(HumanBodyBones.RightHand),weight=0});
                constraint.SetTranslationOffset(0,Vector3.zero);constraint.SetTranslationOffset(1,Vector3.zero);constraint.locked=true;constraint.constraintActive=true;constraint.weight=1;
                // Preserve the existing map motion clip and its identity; add only attachment weights.
                var mapTrack=Tracks(timeline).OfType<AnimationTrack>().Single(t=>t.name=="AncientMap Handoff");var clip=((AnimationPlayableAsset)mapTrack.GetClips().Single().asset).clip;
                Curve(clip,"m_Sources.Array.data[0].weight",new[]{new Keyframe(0,1),new Keyframe(70.35f,1),new Keyframe(71,0),new Keyframe(97,0)});
                Curve(clip,"m_Sources.Array.data[1].weight",new[]{new Keyframe(0,0),new Keyframe(70.35f,0),new Keyframe(71,1),new Keyframe(97,1)});
                Curve(clip,"m_Weight",new[]{new Keyframe(0,1),new Keyframe(83,1),new Keyframe(85,0),new Keyframe(97,0)});EditorUtility.SetDirty(clip);
            }
            // Retarget independent helpers; preserve their objects and the approved camera roles/FOVs.
            director.time=60;director.Evaluate();
            var elderHead=elder.Find("CinematicTargets/HeadLookTarget");var elderChest=elder.Find("CinematicTargets/ChestLookTarget");
            elderHead.position=real.GetBoneTransform(HumanBodyBones.Head).position;elderChest.position=real.GetBoneTransform(HumanBodyBones.Chest).position;
            var explorerHead=explorer.Find("CinematicTargets/HeadLookTarget");var explorerChest=explorer.Find("CinematicTargets/ChestLookTarget");explorerHead.position=dwight.GetBoneTransform(HumanBodyBones.Head).position;explorerChest.position=dwight.GetBoneTransform(HumanBodyBones.Chest).position;
            Find("CAM_ElderDialogue").LookAt(elderHead);Find("CAM_FinalWarning").LookAt(elderHead);Find("CAM_ExplorerDialogue").LookAt(explorerHead);Find("CAM_TwoShot").LookAt((elderHead.position+explorerHead.position)*.5f);
            director.time=67;director.Evaluate();
            var donor=real.GetBoneTransform(HumanBodyBones.RightHand);var receiver=dwight.GetBoneTransform(HumanBodyBones.RightHand);var direction=(receiver.position-donor.position).normalized;
            constraint.SetTranslationOffset(0,donor.InverseTransformVector(direction*.12f));constraint.SetTranslationOffset(1,receiver.InverseTransformVector(-direction*.12f));
            map.rotation=Quaternion.LookRotation(Vector3.Cross(direction,Vector3.up),Vector3.up);
            Find("CAM_MapHandoff").LookAt((donor.position+receiver.position)*.5f-Vector3.up*.18f);
            var mapMotion=((AnimationPlayableAsset)Tracks(timeline).OfType<AnimationTrack>().Single(t=>t.name=="AncientMap Handoff").GetClips().Single().asset).clip;
            Curve(mapMotion,"m_Weight",new[]{new Keyframe(0,1),new Keyframe(97,1)});EditorUtility.SetDirty(mapMotion);
            var mapAsset=(AnimationPlayableAsset)Tracks(timeline).OfType<AnimationTrack>().Single(t=>t.name=="AncientMap Handoff").GetClips().Single().asset;mapAsset.removeStartOffset=false;EditorUtility.SetDirty(mapAsset);
            director.Stop();
            CleanPose(real);CleanPose(dwight);
            explorer.localPosition=new Vector3(0,0,-5);map.localPosition=new Vector3(1.75f,1.55f,11.55f);
            var ambience=Find("Ambience").GetComponent<AudioSource>();ambience.volume=.55f;
            var presenter=UnityEngine.Object.FindAnyObjectByType<CinematicSubtitlePresenter>();var data=new SerializedObject(presenter);
            foreach(var name in new[]{"_dateCard","_narrativePanel","_subtitlePanel","_titleCard"})((GameObject)data.FindProperty(name).objectReferenceValue).SetActive(false);
            foreach(var name in new[]{"_speakerText","_dialogueText","_narrativeText"})((TMPro.TMP_Text)data.FindProperty(name).objectReferenceValue).text=string.Empty;
            Find("FadeCanvas").GetComponent<CanvasGroup>().alpha=1;

            foreach(var n in Cameras)Find(n).gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();Validate();
        }
        static void CleanPose(Animator animator)
        {
            var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(animator.gameObject);var mods=PrefabUtility.GetPropertyModifications(instance);
            if(mods!=null)PrefabUtility.SetPropertyModifications(instance,mods.Where(m=>!(m.target is Transform t&&(t.name.StartsWith("mixamorig:")||t.name=="Armature"||t.name=="DwightModel"||t.name=="ElderModel"))).ToArray());
        }
        static void Curve(AnimationClip clip,string property,Keyframe[] keys)
        {
            var curve=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(ParentConstraint),property),curve);
        }
        static void Add(AnimationTrack track,AnimationClip source,string name,double start,double end,double clipIn,double speed,Vector3 offset,bool loop)
        {
            var clip=track.CreateClip(source);clip.displayName=name;clip.start=start;clip.duration=end-start;clip.clipIn=clipIn;clip.timeScale=speed;clip.blendInDuration=start>0?.25:0;clip.blendOutDuration=end<97?.25:0;
            var asset=(AnimationPlayableAsset)clip.asset;asset.applyFootIK=false;asset.removeStartOffset=false;asset.position=offset;asset.rotation=Quaternion.identity;asset.loop=loop?AnimationPlayableAsset.LoopMode.On:AnimationPlayableAsset.LoopMode.Off;
        }
        [Serializable]public class Report{public bool passed;public string error,unity;public double duration;public int tracks;public string[] checks;}
        [MenuItem("Tikal/Validation/Validate Elder Intro Integration")]
        public static void Validate()
        {
            if(UnityEngine.Object.FindAnyObjectByType<IntroMusicEnvelope>()!=null) { IntroAudioPolishAuthoring.ValidateLoaded(); return; }
            var report=new Report{unity=Application.unityVersion};var checks=new List<string>();
            Action<bool,string> check=(v,m)=>{Require(v,m);checks.Add(m);};
            try
            {
                var director=UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();check(director!=null&&director.gameObject.scene.path==AncientMapIntroAuthoring.ScenePath,"Canonical Intro scene / existing director");
                var timeline=(TimelineAsset)director.playableAsset;var tracks=Tracks(timeline).ToArray();report.duration=timeline.duration;report.tracks=tracks.Length;check(Math.Abs(timeline.duration-97)<.01,"Global duration remains 97 seconds");
                var elder=Find("ElderAnchor");var a=elder.GetComponentsInChildren<Animator>(true).Single(x=>x.avatar!=null);check(a.avatar.isHuman&&a.avatar.isValid,"Own valid Humanoid Elder Avatar");check(a.runtimeAnimatorController==null&&!a.applyRootMotion,"Timeline actor only / no AC_ExplorerGameplay / Root Motion false");
                check(elder.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==1&&!elder.Find("ElderPlaceholder").gameObject.activeSelf,"One real Elder / placeholder inactive");
                check(elder.Find("CinematicTargets/HeadLookTarget")!=null&&elder.Find("CinematicTargets/ChestLookTarget")!=null,"Elder anchors and independent targets preserved");
                check(a.GetComponentsInParent<Component>(true).All(c=>c!=null),"Actor ancestor references complete");
                var visual=AssetDatabase.LoadAssetAtPath<GameObject>(ElderCinematicPilot.Prefab);check(visual.GetComponentsInChildren<Component>(true).All(c=>c is Transform||c is Animator||c is SkinnedMeshRenderer),"Visual-only prefab / no Missing Scripts or gameplay components");
                check(visual.transform.localScale==Vector3.one&&visual.transform.localRotation==Quaternion.identity,"Prefab root Scale 1 / Rotation 0");
                check(a.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.All(m=>m!=null&&m.shader.name=="Universal Render Pipeline/Lit"),"URP/Lit materials complete");
                var performance=tracks.OfType<AnimationTrack>().Single(t=>t.name==ElderTrack);check(director.GetGenericBinding(performance)==a,"Elder Timeline binding valid");
                check(performance.GetClips().Any(c=>c.displayName.Contains("Idle")&&((AnimationPlayableAsset)c.asset).clip==Idle),"Existing Seated Idle assigned");check(performance.GetClips().Any(c=>c.displayName.Contains("Talk")&&((AnimationPlayableAsset)c.asset).clip==Talk),"Existing Seated Talk assigned");check(performance.GetClips().Single(c=>c.displayName.Contains("Give Map")).start==65,"Give Map existing reach excerpt at 65 seconds");
                check(tracks.Count(t=>t.name==ElderTrack)==1&&tracks.Count(t=>t.name==ExplorerTrack)==1,"No duplicate performance tracks");
                foreach(var n in Cameras){var camera=Find(n).GetComponent<Camera>();check(camera!=null&&tracks.OfType<ActivationTrack>().Count(t=>t.name==n&&director.GetGenericBinding(t)==camera.gameObject)==1,"Approved camera / activation binding preserved: "+n);}
                var map=Find("AncientMap_Proxy");var constraint=map.GetComponent<ParentConstraint>();check(map.GetComponent<Animator>()!=null&&constraint!=null&&constraint.sourceCount==2&&constraint.GetSource(0).sourceTransform==a.GetBoneTransform(HumanBodyBones.RightHand),"Independent map proxy / two hand attachment sources");
                var cues=tracks.OfType<CinematicCueTrack>().SelectMany(t=>t.GetClips()).ToArray();check(cues.Count(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.AncientMapReceived)==1&&cues.Single(c=>((CinematicCueAsset)c.asset).Type==CinematicCueType.AncientMapReceived).start==71,"Original AncientMapReceived cue at 71 seconds");
                check(cues.Any(c=>((CinematicCueAsset)c.asset).Text=="No confíes en el sistema."),"Canonical final warning text preserved");
                check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.gameObject.scene==a.gameObject.scene).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts in Intro");
                check(Tikal.Core.GameFlowController.GetIntroCompletionDestination(false)==Tikal.Core.GameSceneIds.Tutorial,"Existing Intro completion routes to Tutorial");report.passed=true;
            }
            catch(Exception e){report.error=e.ToString();throw;}
            finally{report.checks=checks.ToArray();Directory.CreateDirectory(Reports);File.WriteAllText(Reports+"/ElderEditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("ELDER_EDITOR_VALIDATION passed="+report.passed+" checks="+checks.Count);}
        }
    }
}
