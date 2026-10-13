using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Tikal.Core;
using Tikal.Player;
using Tikal.Persistence;
using Tikal.UI;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class MarioGameplayPlayValidation
    {
        const string Flag="Temp/MarioGameplayValidation.request";
        const string Root="Assets/_Project/Documentation/Characters/Mario";
        const string Key="Tikal.Mario.PlayValidation";
        [Serializable] public class Result {public bool enteredPlayMode,passed;public string scope; public string[] checks,errors,editorErrors;}
        static readonly List<string> Checks=new List<string>(),Errors=new List<string>(),EditorErrors=new List<string>();
        static IEnumerator _routine;
        static SceneSetup[] _setup;
        static GameObject _player;
        static PlayerMovement _movement;
        static PlayerCameraController _camera;
        static CharacterVisualSpawner _spawner;
        static Animator _animator;
        static double _deadline;
        static MarioGameplayPlayValidation()
        {
            EditorApplication.playModeStateChanged+=Mode;
            EditorApplication.delayCall+=Resume;
        }
        static void Resume()
        {
            if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying){StartTests();return;}
            if(!File.Exists(Flag))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Resume;return;}
            File.Delete(Flag);Run();
        }
        [MenuItem("Tikal/Validation/Run Mario Gameplay Play Tests")]
        public static void Run()
        {
            MarioGameplayAuthoring.Require(!Application.isBatchMode&&!EditorApplication.isPlaying,"Normal Editor Edit Mode required");
            MarioGameplayAuthoring.Apply();
            var files=new[]{MarioGameplayAuthoring.Prefab,MarioGameplayAuthoring.Controller,MarioGameplayAuthoring.Registry,MarioGameplayAuthoring.Model+".meta"};var before=files.Select(File.ReadAllBytes).ToArray();
            MarioGameplayAuthoring.Apply();MarioGameplayAuthoring.Require(files.Select((f,i)=>File.ReadAllBytes(f).SequenceEqual(before[i])).All(x=>x),"Authoring idempotence changed assets");
            SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup()}));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        [Serializable]class Setup{public SceneSetup[] scenes;}
        static void Mode(PlayModeStateChange mode)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode)StartTests();
            if(mode==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key,false);
                var saved=SessionState.GetString(Key+"Setup","");if(!string.IsNullOrEmpty(saved))EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<Setup>(saved).scenes);
                AssetDatabase.Refresh();Debug.Log("MARIO_PLAY_VALIDATION_FINISHED");
            }
        }
        static void StartTests()
        {
            if(_routine!=null)return;Checks.Clear();Errors.Clear();EditorErrors.Clear();Application.logMessageReceived+=Log;
            _routine=Tests();_deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;
        }
        static void Log(string text,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase"))EditorErrors.Add(text+"\n"+stack);else Errors.Add(text+"\n"+stack);
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try {if(EditorApplication.timeSinceStartup>_deadline)throw new Exception("Play validation timeout");if(_routine.MoveNext())return;Finish();}
            catch(Exception e){Errors.Add(e.ToString());Finish();}
        }
        static void Check(bool value,string text){if(!value)throw new Exception("Mario play: "+text);Checks.Add(text);Debug.Log("MARIO_PLAY_CHECK "+text);}
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;_routine=null;
            var result=new Result{enteredPlayMode=Application.isPlaying,passed=Application.isPlaying&&Errors.Count==0,checks=Checks.ToArray(),errors=Errors.ToArray(),editorErrors=EditorErrors.ToArray(),scope="Automated interactive-Editor tests: actual PlayerMovement/CharacterController, animation bridge, camera visibility, scene spawning, menu selection and isolated save roundtrip. Not a human WASD/puzzle acceptance playtest."};
            Directory.CreateDirectory(Root);File.WriteAllText(Root+"/MarioPlayValidation.json",JsonUtility.ToJson(result,true));EditorApplication.ExitPlaymode();
        }
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode,"No batchmode, real Play Mode");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="MarioValidationGround";ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(100,1,100);
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.2f,.28f,.2f));ground.GetComponent<Renderer>().sharedMaterial=mat;
            var sun=new GameObject("ValidationSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
            _player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath));_player.transform.position=Vector3.up*.05f;
            _player.GetComponent<PlayerController>().enabled=false;
            _movement=_player.GetComponent<PlayerMovement>();_camera=_player.GetComponent<PlayerCameraController>();_spawner=_player.GetComponent<CharacterVisualSpawner>();
            _spawner.Apply(CharacterIds.Character03);_animator=_spawner.CurrentVisual.GetComponentInChildren<Animator>(true);
            for(int i=0;i<5;i++)yield return null;
            _camera.ApplyCameraMode(CameraMode.ThirdPerson);
            Check(_spawner.CurrentVisual.activeInHierarchy,"Mario visible in TPS");
            var current=_spawner.CurrentVisual;_spawner.Apply(CharacterIds.Character03);Check(_spawner.CurrentVisual==current,"Repeated selection does not recreate Mario");
            Check(_player.GetComponents<CharacterController>().Length==1&&current.GetComponentsInChildren<CharacterController>(true).Length==0,"One gameplay capsule; visual has no physics/input duplicates");
            var proxy=_player.transform.Find("CharacterVisualRoot/ThirdPersonHumanProxy");Check(proxy!=null&&!proxy.gameObject.activeSelf,"Proxy suppressed for Mario");
            for(int i=0;i<45;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(_movement.IsGrounded&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Idle grounded/Locomotion");Capture("Mario_Idle.png");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,false,false,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.WalkSpeed)<.05f&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Actual unchanged Walk speed/animation");Capture("Mario_Walk.png");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,true,false,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.SprintSpeed)<.05f&&_animator.GetCurrentAnimatorClipInfo(0).Any(x=>x.clip.name=="CHR_Explorer_Run"&&x.weight>.9f),"Actual unchanged Sprint speed/Run clip");Capture("Mario_Run.png");
            _movement.RequestJump();bool airborne=false,landed=false;
            for(int i=0;i<160;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);airborne|=!_movement.IsGrounded;if(airborne&&_movement.IsGrounded)landed=true;yield return null;}
            Check(airborne&&landed&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Physical Jump/full-clip fallback/landing settles");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,true,Time.deltaTime);yield return null;}
            Check(_movement.IsCrouching&&_animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchIdle"),"CrouchIdle held alias at phase .25");Capture("Mario_CrouchIdle.png");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,false,true,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.CrouchSpeed)<.05f&&_animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchWalk"),"Actual existing crouch mechanics/clip");Capture("Mario_CrouchWalk.png");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(!_movement.IsCrouching&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Crouch release settles");
            for(int i=0;i<4;i++){_camera.ApplyCameraMode(CameraMode.FirstPerson);Check(!current.activeInHierarchy,"FPS hides full body immediately");_camera.ApplyCameraMode(CameraMode.ThirdPerson);Check(current.activeInHierarchy&&_spawner.CurrentVisual==current,"TPS restores same visual");yield return null;}
            _movement.RequestJump();_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;
            _movement.ResetForRespawn();_player.GetComponent<PlayerAnimationController>().ResetForRespawn();
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(_movement.IsGrounded&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")&&_animator.GetFloat("Speed")<.01f,"Respawn reset hook clears residual airborne/run state");
            var path=Path.Combine(Path.GetTempPath(),"TikalMarioValidation-"+Guid.NewGuid().ToString("N"));var save=new GamePersistenceService(path);var progress=GameProgressData.CreateDefault();progress.SelectedCharacterId=CharacterIds.Character03;save.SaveProgress(progress);Check(save.LoadProgress().SelectedCharacterId==CharacterIds.Character03,"Existing save schema preserves character.03 roundtrip (isolated file)");
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Menus/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<10;i++)yield return null;
            var selector=UnityEngine.Object.FindAnyObjectByType<CharacterSelectionController>(FindObjectsInactive.Include);
            Check(selector!=null,"Actual Main Menu has its existing selector");selector.gameObject.SetActive(true);yield return null;
            var selectorType=typeof(CharacterSelectionController);var fields=BindingFlags.Instance|BindingFlags.NonPublic;
            selectorType.GetField("_persistence",fields).SetValue(selector,save);
            selectorType.GetField("_progress",fields).SetValue(selector,GameProgressData.CreateDefault());
            selector.Select(2);selector.Confirm();
            var labels=(TMP_Text[])selectorType.GetField("_characterLabels",fields).GetValue(selector);
            Check(labels[2].text=="Mario"&&save.LoadProgress().SelectedCharacterId==CharacterIds.Character03,"Actual third card Mario/Confirm writes existing schema to isolated save");
            selector.gameObject.SetActive(false);yield return null;selector.gameObject.SetActive(true);yield return null;
            var buttons=(Button[])selectorType.GetField("_characterButtons",fields).GetValue(selector);
            var marker=(GameObject)typeof(MenuSelectableVisual).GetField("_persistentMarker",fields).GetValue(buttons[2].GetComponent<MenuSelectableVisual>());
            Check(marker.activeSelf,"Mario remains selected after leaving and reopening panel");
            foreach(var scenePath in MarioGameplayAuthoring.Scenes)
            {
                EditorSceneManager.LoadSceneInPlayMode(scenePath,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                var players=UnityEngine.Object.FindObjectsByType<PlayerController>();Check(players.Length==1,"Scene has one existing Player: "+scenePath);
                var spawner=players[0].GetComponent<CharacterVisualSpawner>();spawner.Apply(save.LoadProgress().SelectedCharacterId);var camera=players[0].GetComponent<PlayerCameraController>();camera.ApplyCameraMode(CameraMode.ThirdPerson);yield return null;
                Check(spawner.HasVisual&&spawner.CurrentVisual.activeInHierarchy&&spawner.CurrentVisual.GetComponentInChildren<Animator>().avatar.isValid,"Persisted selection maps to Mario in scene: "+scenePath);
                camera.ApplyCameraMode(CameraMode.FirstPerson);Check(!spawner.CurrentVisual.activeInHierarchy,"Scene FPS hide: "+scenePath);
                if(scenePath.Contains("Level01")){var checkpoint=UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();Check(checkpoint!=null&&checkpoint.CanRestartFromCheckpoint,"Actual Level01 checkpoint available");checkpoint.RestartFromCheckpoint();camera.ApplyCameraMode(CameraMode.ThirdPerson);for(int i=0;i<25;i++)yield return null;var anim=spawner.CurrentVisual.GetComponentInChildren<Animator>();Check(!checkpoint.IsRespawning&&anim.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Actual Level01 RestartFromCheckpoint resets animation safely");}
            }
        }
        static void Capture(string name)
        {
            var renderers=_spawner.CurrentVisual.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var renderer in renderers)
            {
                var bounds=renderer.bounds;
                var headHeight=_animator.GetBoneTransform(HumanBodyBones.Head).position.y-_player.transform.position.y;
                Check(bounds.size.y>.5f&&bounds.size.y<2.3f&&bounds.center.y-_player.transform.position.y<2&&headHeight>.7f&&headHeight<2&&Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(_player.transform.position.x,_player.transform.position.z))<1,"Animated rig remains human-sized at Player: "+name);
            }
            var camera=new GameObject("MarioEvidenceCamera").AddComponent<Camera>();camera.fieldOfView=40;camera.transform.position=_player.transform.position+new Vector3(2.4f,1.5f,3.5f);camera.transform.LookAt(_player.transform.position+Vector3.up*.9f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.2f);camera.allowHDR=false;
            var rt=new RenderTexture(640,640,24);rt.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};Check(RenderPipeline.SupportsRenderRequest(camera,request),"URP evidence rendering supported");RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();Check(!image.GetPixels32().Any(p=>p.r>220&&p.g<50&&p.b>220),"No magenta: "+name);var body=image.GetPixels32();
            try {
                foreach(var renderer in renderers)renderer.enabled=false;
                RenderPipeline.SubmitRenderRequest(camera,request);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();
                var background=image.GetPixels32();var visiblePixels=body.Where((pixel,i)=>Mathf.Abs(pixel.r-background[i].r)+Mathf.Abs(pixel.g-background[i].g)+Mathf.Abs(pixel.b-background[i].b)>30).Count();
                Check(visiblePixels>1000,"Mario contributes visible pixels (not empty render): "+name);
            } finally {foreach(var renderer in renderers)renderer.enabled=true;}
            image.SetPixels32(body);image.Apply();Directory.CreateDirectory(Root);File.WriteAllBytes(Root+"/"+name,image.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(camera.gameObject);
        }
    }
}
