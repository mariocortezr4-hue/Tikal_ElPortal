using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Player;
using Tikal.Persistence;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class MarioGameplayAuthoring
    {
        public const string Art = "Assets/_Project/Art/Characters/Mario";
        public const string Model = Art + "/Models/CHR_Explorer.fbx";
        public const string Prefab = "Assets/_Project/Prefabs/Characters/CHR_Mario_PlayerVisual.prefab";
        public const string Controller = "Assets/_Project/Animation/Characters/Explorer/AC_ExplorerGameplay.controller";
        public const string Registry = "Assets/_Project/Resources/Characters/GameplayCharacterVisuals.asset";
        public const string Report = "Assets/_Project/Documentation/Characters/Mario/MarioEditorValidation.json";
        public static readonly string[] Scenes = {
            "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity",
            "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity" };
        [Serializable] public class ValidationResult { public bool passed; public string unity, avatar; public float height, importScale; public int triangles, vertices, bones; public string[] clips, checks; public string error; }
        public static void Folder(string path) { Directory.CreateDirectory(path); AssetDatabase.Refresh(); }
        static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "/Animations/CHR_Explorer_" + name + ".anim");
        public static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [MenuItem("Tikal/Authoring/Apply Mario Gameplay Character")]
        public static void Apply()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Mario authoring requires Edit Mode");
            var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
            Require(importer != null, "Copy and import the audited model before authoring");
            foreach (var n in new[] {"Idle","Walk","Run","Jump","CrouchIdle","CrouchWalk"})
                Require(Clip(n) != null && Clip(n).humanMotion, "Missing/invalid Humanoid clip: " + n);
            if (importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel || importer.importCameras || importer.importLights)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
                importer.SaveAndReimport();
            }
            // The external meta contains a manually serialized skeleton rooted at
            // CHR_Explorer(Clone). Rebuild mapping from the actual imported FBX hierarchy.
            if(importer.userData!="Tikal.Mario.AvatarFromImportedModel.v1")
            {
                importer.humanDescription=new HumanDescription {
                    human=Array.Empty<HumanBone>(),skeleton=Array.Empty<SkeletonBone>(),
                    armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,
                    upperLegTwist=.5f,lowerLegTwist=.5f,feetSpacing=0,hasTranslationDoF=false};
                importer.userData="Tikal.Mario.AvatarFromImportedModel.v1";
                importer.SaveAndReimport();
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var preview = (GameObject)PrefabUtility.InstantiatePrefab(model);
            float height;
            try { height = Bounds(preview).size.y; }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
            Require(height > .1f, "Model geometry is empty or invalid");
            if (Mathf.Abs(height-1.8f) > .002f) { importer.globalScale *= 1.8f/height; importer.SaveAndReimport(); }
            model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().SingleOrDefault();
            Require(avatar != null && avatar.isValid && avatar.isHuman, "Mario Avatar must be valid Humanoid");
            Folder(Art + "/Materials"); var mp = Art + "/Materials/MAT_Mario.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit"); Require(shader != null, "URP/Lit unavailable");
                material = new Material(shader); material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/CHR_Explorer_BaseColor.JPEG"));
                material.SetFloat("_Metallic",0); material.SetFloat("_Smoothness",.25f); AssetDatabase.CreateAsset(material,mp);
            }
            var textureImporter = AssetImporter.GetAtPath(Art+"/Textures/CHR_Explorer_BaseColor.JPEG") as TextureImporter;
            if (!textureImporter.sRGBTexture || !textureImporter.mipmapEnabled) { textureImporter.sRGBTexture=true;textureImporter.mipmapEnabled=true;textureImporter.SaveAndReimport(); }
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller)==null) BuildController();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            Folder("Assets/_Project/Prefabs/Characters");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)==null)
            {
                var root=new GameObject("MarioRoot");
                try
                {
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(root.transform,false);visual.name="MarioModel";
                    var bounds=Bounds(visual); visual.transform.localPosition=new Vector3(-bounds.center.x,MarioWalkGroundValidation.ModelLocalY,-bounds.center.z);
                    var animator=visual.GetComponent<Animator>()??visual.AddComponent<Animator>();animator.avatar=avatar;animator.runtimeAnimatorController=controller;
                    animator.applyRootMotion=false;animator.updateMode=AnimatorUpdateMode.Normal;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                    foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                    PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                }
                finally {UnityEngine.Object.DestroyImmediate(root);}
            }
            Folder("Assets/_Project/Resources/Characters");var registry=AssetDatabase.LoadAssetAtPath<CharacterVisualAssets>(Registry);
            if(registry==null){registry=ScriptableObject.CreateInstance<CharacterVisualAssets>();AssetDatabase.CreateAsset(registry,Registry);}
            var data=new SerializedObject(registry);var reference=data.FindProperty("_marioVisualPrefab");var pf=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if(reference.objectReferenceValue!=pf){reference.objectReferenceValue=pf;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(registry);}
            AssetDatabase.SaveAssets();Validate();
        }
        static void BuildController()
        {
            Folder("Assets/_Project/Animation/Characters/Explorer");var c=AnimatorController.CreateAnimatorControllerAtPath(Controller);
            foreach(var n in new[]{"Speed","VerticalVelocity","JumpPhase","CrouchPose"})c.AddParameter(n,AnimatorControllerParameterType.Float);
            foreach(var n in new[]{"IsGrounded","IsCrouching","IsLanding"})c.AddParameter(n,AnimatorControllerParameterType.Bool);
            var parameters=c.parameters;foreach(var p in parameters){if(p.name=="IsGrounded")p.defaultBool=true;if(p.name=="CrouchPose")p.defaultFloat=.25f;if(p.name=="JumpPhase")p.defaultFloat=1;}c.parameters=parameters;
            var machine=c.layers[0].stateMachine;
            var movement=AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath).GetComponent<PlayerMovement>();
            var locomotion=machine.AddState("Locomotion",new Vector3(200,100));
            var tree=new BlendTree{name="ExplorerLocomotion",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,c);
            tree.children=new[]{new ChildMotion{motion=Clip("Idle"),threshold=0,timeScale=1},new ChildMotion{motion=Clip("Walk"),threshold=movement.WalkSpeed/movement.SprintSpeed,timeScale=1.8723404f*(movement.WalkSpeed/1.6f)*(1.6508058f/1.8f)},new ChildMotion{motion=Clip("Run"),threshold=1,timeScale=(movement.SprintSpeed/3.45f)*(1.6508058f/1.8f)}};
            locomotion.motion=tree;machine.defaultState=locomotion;
            var idle=machine.AddState("CrouchIdle",new Vector3(200,270));idle.motion=Clip("CrouchIdle");idle.timeParameterActive=true;idle.timeParameter="CrouchPose";
            var crouch=machine.AddState("CrouchWalk",new Vector3(440,270));crouch.motion=Clip("CrouchWalk");crouch.speed=.87f*(movement.CrouchSpeed/.975f)*(1.6508058f/1.8f);
            var air=machine.AddState("Air",new Vector3(440,100));air.motion=Clip("Jump");air.timeParameterActive=true;air.timeParameter="JumpPhase";
            var land=machine.AddState("Land",new Vector3(680,100));land.motion=Clip("Jump");land.timeParameterActive=true;land.timeParameter="JumpPhase";
            foreach(var state in new[]{locomotion,idle,crouch}) {var t=Transition(state,air);t.AddCondition(AnimatorConditionMode.IfNot,0,"IsGrounded");}
            var landing=Transition(air,land);landing.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");landing.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");
            var lowLanding=Transition(air,idle);lowLanding.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");lowLanding.AddCondition(AnimatorConditionMode.If,0,"IsCrouching");
            foreach(var state in new[]{locomotion,land}) {var t=Transition(state,idle);t.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");t.AddCondition(AnimatorConditionMode.If,0,"IsCrouching");}
            var walk=Transition(idle,crouch);walk.AddCondition(AnimatorConditionMode.Greater,.015f,"Speed");walk.AddCondition(AnimatorConditionMode.If,0,"IsCrouching");
            var stop=Transition(crouch,idle);stop.AddCondition(AnimatorConditionMode.Less,.015f,"Speed");
            foreach(var state in new[]{idle,crouch}){var t=Transition(state,locomotion);t.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");t.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");}
            var done=Transition(land,locomotion);done.AddCondition(AnimatorConditionMode.IfNot,0,"IsLanding");done.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");
            var takeoff=Transition(land,air);takeoff.AddCondition(AnimatorConditionMode.IfNot,0,"IsGrounded");
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
        }
        static AnimatorStateTransition Transition(AnimatorState a,AnimatorState b){var t=a.AddTransition(b);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=.08f;t.interruptionSource=TransitionInterruptionSource.SourceThenDestination;return t;}
        public static Bounds Bounds(GameObject go){var renderers=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);Require(renderers.Length>0,"Missing skinned mesh");var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
        [MenuItem("Tikal/Validation/Validate Mario Gameplay Character")]
        public static void Validate()
        {
            var result=new ValidationResult{unity=Application.unityVersion};
            try
            {
                MarioWalkGroundValidation.ValidateGround();
                var definition=CharacterCatalog.GetOrDefault(CharacterIds.Character03);Require(definition.Id==CharacterIds.Character03&&definition.DisplayName=="Mario","character.03 catalog/name mapping");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);Require(prefab!=null&&definition.VisualPrefab==prefab,"Catalog references Mario prefab");
                var animator=prefab.GetComponentInChildren<Animator>(true);Require(animator!=null&&animator.avatar!=null&&animator.avatar.isHuman&&animator.avatar.isValid,"Valid Humanoid Animator");
                Require(!animator.applyRootMotion&&animator.runtimeAnimatorController==AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller),"Shared controller, root motion disabled");
                Require(prefab.GetComponentsInChildren<CharacterController>(true).Length==0&&prefab.GetComponentsInChildren<PlayerMovement>(true).Length==0&&prefab.GetComponentsInChildren<PlayerLook>(true).Length==0&&prefab.GetComponentsInChildren<Camera>(true).Length==0&&prefab.GetComponentsInChildren<AudioListener>(true).Length==0,"Visual-only prefab");
                Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).All(x=>x!=null),"No missing scripts");Require(prefab.transform.localScale==Vector3.one,"Unit prefab root scale");
                var ob=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var a=ob.GetComponentInChildren<Animator>();var required=new[]{HumanBodyBones.Head,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
                    Require(required.All(b=>a.GetBoneTransform(b)!=null),"Critical Humanoid bones mapped");var b=Bounds(ob);result.height=b.size.y;Require(b.size.y>=1.7f&&b.size.y<=1.85f&&Mathf.Abs(MarioWalkGroundValidation.Sole(ob)+MarioWalkGroundValidation.PhysicalGroundClearance)<.025f,"Human height/feet alignment");
                    var r=ob.GetComponentsInChildren<SkinnedMeshRenderer>();result.vertices=r.Sum(x=>x.sharedMesh.vertexCount);result.triangles=r.Sum(x=>x.sharedMesh.triangles.Length/3);result.bones=r.Sum(x=>x.bones.Length);
                    foreach(var mat in r.SelectMany(x=>x.sharedMaterials)){Require(mat!=null&&mat.shader.name=="Universal Render Pipeline/Lit"&&mat.GetTexture("_BaseMap")!=null,"URP/Lit with BaseColor");Require(mat.GetFloat("_Metallic")<.1f&&mat.GetFloat("_Smoothness")<=.35f,"Natural material");}
                }
                finally{UnityEngine.Object.DestroyImmediate(ob);}
                var setup=EditorSceneManager.GetSceneManagerSetup();
                try
                {
                    foreach(var path in Scenes){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var players=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PlayerController>(true)).ToArray();Require(players.Length==1&&players[0].GetComponent<CharacterVisualSpawner>()!=null,"Scene uses existing one Player and visual spawner: "+path);ThirdPersonCameraFoundationValidation.ValidatePlayer(players[0]);}
                }
                finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
                Require(CharacterCatalog.All.Count==4&&CharacterCatalog.All[2].Id==CharacterIds.Character03,"Existing four-slot selection order");
                var menuSetup=EditorSceneManager.GetSceneManagerSetup();
                try {
                    var menu=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Menus/MainMenu.unity",OpenSceneMode.Single);
                    Require(menu.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Tikal.UI.CharacterSelectionController>(true)).Count()==1,"Main Menu retains its existing selector");
                } finally {EditorSceneManager.RestoreSceneManagerSetup(menuSetup);}
                result.avatar=animator.avatar.name;result.importScale=((ModelImporter)AssetImporter.GetAtPath(Model)).globalScale;result.clips=new[]{"Idle","Walk","Run","Jump (full clip)","CrouchIdle (held alias)","CrouchWalk"};
                result.checks=new[]{"catalog/name/prefab","Humanoid required bone mapping","root motion false/shared controller","visual-only/no missing scripts","height/CPU-skinned-feet/unit root","canonical Walk/GUID/old clip retired/loop","96-phase Idle-Walk baseline/calibrated model offset","URP/BaseColor/no fake Normal","Tutorial+Level01 existing Player/spawner/camera foundation","existing menu selector/four-slot catalog order"};result.passed=true;
                Debug.Log("MARIO_EDITOR_VALIDATION_PASS height="+result.height+" triangles="+result.triangles);
            }
            catch(Exception e){result.error=e.ToString();Debug.LogException(e);throw;}
            finally{Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,JsonUtility.ToJson(result,true));AssetDatabase.Refresh();}
        }
        // Command-line entry is interactive Editor only; batchmode is explicitly prohibited.
        public static void ApplyInteractive()
        {
            Require(!Application.isBatchMode,"Do not run Mario integration in batchmode");
            Apply();
        }
    }
}
