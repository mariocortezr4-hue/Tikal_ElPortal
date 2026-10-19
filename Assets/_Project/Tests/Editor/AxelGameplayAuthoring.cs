using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Player;
using Tikal.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class AxelGameplayAuthoring
    {
        public const string Art="Assets/_Project/Art/Characters/Axel";
        public const string Model=Art+"/Models/CHR_Axel.fbx";
        public const string Prefab="Assets/_Project/Prefabs/Characters/CHR_Axel_PlayerVisual.prefab";
        public const string Controller="Assets/_Project/Animation/Characters/Explorer/AC_ExplorerGameplay.controller";
        public const string Registry="Assets/_Project/Resources/Characters/GameplayCharacterVisuals.asset";
        public const string Reports="Assets/_Project/Documentation/Characters/Axel";
        public const float VisualOffset=.01414936f;
        public static readonly string[] Scenes={"Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity","Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity"};
        [Serializable] public class Result { public bool passed;public string unity,error,characterId,displayName,scope;public float height,importScale,visualOffset;public int triangles,vertices,skinnedBones;public string[] checks,clips; }
        public static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Axel: "+message);}
        static List<string> checks;
        static void Check(bool value,string message){Require(value,message);checks.Add(message);}
        public static Transform Orientation(GameObject root)=>root.transform.Find("AxelVisualOrientation");
        public static Bounds GeometryBounds(GameObject root)
        {
            // Renderer bounds include conservative animation padding; measure actual skinned geometry.
            var bounds=new Bounds();bool any=false;
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
                var matrices=skin.bones.Select((bone,i)=>bone.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];var v=vertices[i];
                    var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                    if(!any){bounds=new Bounds(world,Vector3.zero);any=true;}else bounds.Encapsulate(world);
                }
            }
            Require(any,"Non-empty CPU-skinned geometry");return bounds;
        }
        [MenuItem("Tikal/Authoring/Apply Axel Gameplay Character")]
        public static void Apply()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!Application.isBatchMode,"Interactive Edit Mode required");
            var importer=AssetImporter.GetAtPath(Model) as ModelImporter;
            Require(importer!=null&&importer.animationType==ModelImporterAnimationType.Human&&importer.avatarSetup==ModelImporterAvatarSetup.CreateFromThisModel,"Preserve validated Humanoid import; do not rebuild rig during gameplay authoring");
            Require(Mathf.Abs(importer.globalScale-1.80000043f)<.00001f,"Preserve validated model scale");
            var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();
            Require(avatar.isHuman&&avatar.isValid,"Validated own Humanoid Avatar");
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/MAT_Axel_Body.mat");
            Require(controller!=null&&material!=null&&material.shader.name=="Universal Render Pipeline/Lit","Existing shared controller and validated material");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)!=null,"Promote validated visual prefab in place");
            var root=PrefabUtility.LoadPrefabContents(Prefab);bool changed=false;
            try
            {
                var orientation=Orientation(root);Require(orientation!=null,"Existing visual orientation wrapper");
                var a=root.GetComponentInChildren<Animator>(true);Require(a!=null,"One existing Animator");
                if(orientation.localPosition!=Vector3.up*VisualOffset){orientation.localPosition=Vector3.up*VisualOffset;changed=true;}
                if(orientation.localRotation!=Quaternion.identity){orientation.localRotation=Quaternion.identity;changed=true;}
                if(a.avatar!=avatar){a.avatar=avatar;changed=true;}
                if(a.runtimeAnimatorController!=controller){a.runtimeAnimatorController=controller;changed=true;}
                if(a.applyRootMotion){a.applyRootMotion=false;changed=true;}
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if(skin.sharedMaterials.Any(m=>m!=material)){skin.sharedMaterials=Enumerable.Repeat(material,skin.sharedMaterials.Length).ToArray();changed=true;}
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            var registry=AssetDatabase.LoadAssetAtPath<CharacterVisualAssets>(Registry);
            Require(registry!=null&&registry.MarioVisualPrefab!=null,"Existing registry with Mario preserved");
            var mario=registry.MarioVisualPrefab;var data=new SerializedObject(registry);var field=data.FindProperty("_axelVisualPrefab");
            Require(field!=null,"Existing registry extended with Axel reference");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if(field.objectReferenceValue!=prefab){field.objectReferenceValue=prefab;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(registry);AssetDatabase.SaveAssetIfDirty(registry);}
            Require(registry.MarioVisualPrefab==mario,"Mario registry reference unchanged");
            Validate();
        }
        [MenuItem("Tikal/Validation/Validate Axel Gameplay Character")]
        public static void Validate()
        {
            checks=new List<string>();var result=new Result{unity=Application.unityVersion,scope="Editor asset/catalog/reference validation. FPS/TPS, selection and respawn behavior are covered separately by Play Mode validation."};
            try
            {
                var definition=CharacterCatalog.GetOrDefault(CharacterIds.Character02);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
                Check(CharacterCatalog.All.Count==4&&CharacterCatalog.All[1].Id==CharacterIds.Character02,"Exactly four existing slots; character.02 remains slot 2");
                Check(definition.Id==CharacterIds.Character02&&definition.DisplayName=="Axel"&&definition.VisualPrefab==prefab,"character.02 = Axel / canonical visual prefab");
                var mario=CharacterCatalog.GetOrDefault(CharacterIds.Character03);
                Check(mario.DisplayName=="Mario"&&mario.VisualPrefab==AssetDatabase.LoadAssetAtPath<GameObject>(MarioGameplayAuthoring.Prefab)&&mario.VisualPrefab!=prefab,"character.03 = Mario preserved / distinct visual");
                Check(CharacterCatalog.All[0].Id==CharacterIds.Default&&CharacterCatalog.All[0].VisualPrefab==null&&CharacterCatalog.All[3].Id==CharacterIds.Character04&&CharacterCatalog.All[3].VisualPrefab==null,"Other slots retain existing definitions; no Dwight/slot4 implementation");
                var a=prefab.GetComponentInChildren<Animator>(true);var modelAvatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().Single();
                Check(a!=null&&a.avatar==modelAvatar&&a.avatar.isValid&&a.avatar.isHuman,"Own valid Humanoid Animator/Avatar");
                Check(a.runtimeAnimatorController==AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller)&&!a.applyRootMotion,"Exact shared AC_ExplorerGameplay / Root Motion false");
                Check(mario.VisualPrefab.GetComponentInChildren<Animator>(true).avatar!=a.avatar,"Axel and Mario have distinct Avatars");
                var imp=(ModelImporter)AssetImporter.GetAtPath(Model);result.importScale=imp.globalScale;
                Check(imp.animationType==ModelImporterAnimationType.Human&&imp.avatarSetup==ModelImporterAvatarSetup.CreateFromThisModel&&Mathf.Abs(imp.globalScale-1.80000043f)<.00001f,"Validated Humanoid import and scale preserved");
                Check(prefab.transform.localScale==Vector3.one&&prefab.transform.localRotation==Quaternion.identity&&a.transform.localScale==Vector3.one,"Prefab/model Scale 1 and root Rotation 0");
                Check(Orientation(prefab)!=null&&Mathf.Abs(Orientation(prefab).localPosition.y-VisualOffset)<.0000001f&&Orientation(prefab).localRotation==Quaternion.identity,"Validated +0.01414936 m visual offset / no corrective rotation");
                var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/MAT_Axel_Body.mat");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/CHR_Axel_basecolor.PNG");
                Check(material!=null&&material.shader.name=="Universal Render Pipeline/Lit"&&material.GetTexture("_BaseMap")==texture&&texture!=null,"URP/Lit and existing BaseColor assigned");
                Check(material.GetFloat("_Metallic")==0&&material.GetFloat("_Smoothness")==.25f&&material.GetTexture("_BumpMap")==null,"Metallic 0 / Smoothness .25 / no invented Normal Map");
                Check(prefab.GetComponentsInChildren<Component>(true).All(c=>c!=null&&(c is Transform||c is Animator||c is SkinnedMeshRenderer)),"Only visual components; no Missing Scripts, gameplay, cameras or AudioListeners");
                var renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Check(renderers.Length>0&&renderers.All(r=>r.sharedMesh!=null&&r.bones.All(b=>b!=null)&&r.sharedMaterials.All(m=>m==material)),"Complete skinned mesh/bone/material references");
                var tree=(BlendTree)AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller).layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state.motion;
                Check(Mathf.Abs(tree.children[1].timeScale-2.2f)<.000001f&&Mathf.Abs(tree.children[2].timeScale-1.5896648f)<.000001f,"Shared Walk/Run playback unchanged");
                var clips=a.runtimeAnimatorController.animationClips;
                Check(new[]{"Idle","Walk","Run","Jump","CrouchIdle","CrouchWalk"}.All(n=>clips.Any(c=>c!=null&&c.name=="CHR_Explorer_"+n&&c.humanMotion)),"All six existing shared Humanoid clip references");
                Check(clips.All(c=>AssetDatabase.GetAssetPath(c).StartsWith(MarioGameplayAuthoring.Art+"/Animations/",StringComparison.Ordinal)),"No duplicate character-specific clips");
                foreach(var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    var iterator=new SerializedObject(component).GetIterator();
                    while(iterator.NextVisible(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                        Require(iterator.objectReferenceValue!=null||iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)),"Broken visual reference: "+component.name+"/"+iterator.propertyPath);
                }
                Check(true,"No broken serialized visual references");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try{result.height=GeometryBounds(go).size.y;Check(Mathf.Abs(result.height-1.8f)<.003f,"CPU-skinned visual rest height approximately 1.80 m");}
                finally{UnityEngine.Object.DestroyImmediate(go);}
                foreach(var path in Scenes)
                {
                    var scene=EditorSceneManager.OpenPreviewScene(path);
                    try
                    {
                        var players=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerController>(true)).ToArray();
                        Check(players.Length==1&&players[0].GetComponent<CharacterVisualSpawner>()!=null,"Existing Player/spawner; shared animation bridge attaches at runtime: "+path);
                        var spawner=new SerializedObject(players[0].GetComponent<CharacterVisualSpawner>());
                        Check(spawner.FindProperty("_characterVisualRoot").objectReferenceValue!=null&&players[0].GetComponent<PlayerCameraController>()!=null,"Shared FPS/TPS visual root: "+path);
                        Check(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts in gameplay scene: "+path);
                    }finally{EditorSceneManager.ClosePreviewScene(scene);}
                }
                var menu=EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Menus/MainMenu.unity");
                try{Check(menu.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CharacterSelectionController>(true)).Count()==1,"Existing Main Menu selector consumes CharacterCatalog labels/persistence");}
                finally{EditorSceneManager.ClosePreviewScene(menu);}
                result.characterId=definition.Id;result.displayName=definition.DisplayName;result.visualOffset=VisualOffset;
                result.triangles=renderers.Sum(r=>r.sharedMesh.triangles.Length/3);result.vertices=renderers.Sum(r=>r.sharedMesh.vertexCount);result.skinnedBones=renderers.Sum(r=>r.bones.Length);result.clips=clips.Select(c=>c.name).Distinct().OrderBy(n=>n).ToArray();result.passed=true;
            }
            catch(Exception e){result.error=e.ToString();throw;}
            finally{result.checks=checks.ToArray();Directory.CreateDirectory(Reports);File.WriteAllText(Reports+"/AxelEditorValidation.json",JsonUtility.ToJson(result,true));Debug.Log("AXEL_EDITOR_VALIDATION passed="+result.passed+" checks="+checks.Count);}
        }
    }
}
