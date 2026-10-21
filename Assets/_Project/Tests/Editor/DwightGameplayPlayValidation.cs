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
    public static class DwightGameplayPlayValidation
    {
        const string Flag="Temp/DwightGameplayValidation.request";
        const string Root=DwightGameplayAuthoring.Reports;
        const string Evidence="Temp/DwightGameplayValidation";
        const string Key="Tikal.Dwight.PlayValidation";
        [Serializable] public class Result {public float idleWorldSole;public bool enteredPlayMode,passed;public string scope; public string[] checks,errors,editorErrors;}
        static readonly List<string> Checks=new List<string>(),Errors=new List<string>(),EditorErrors=new List<string>();
        static IEnumerator _routine;
        static GameObject _player;
        static PlayerMovement _movement;
        static PlayerCameraController _camera;
        static CharacterVisualSpawner _spawner;
        static Animator _animator;
        static double _deadline;
        static Vector3 _modelPosition;
        static float _idleGroundBaseline;
        static Vector3 _orientationPosition;
        static float _previousPelvisY, _maximumPelvisStep;
        static bool _hasPelvisSample;
        static int _originalCaptureRate;
        static int _lastGameFrame;
        static bool _originalBackground;
        static DwightGameplayPlayValidation()
        {
            EditorApplication.playModeStateChanged+=Mode;
            EditorApplication.delayCall+=Resume;
            EditorApplication.update+=PollRequest;
        }
        static void PollRequest()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists(Flag))Resume();
        }
        static void Resume()
        {
            if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying){StartTests();return;}
            if(!File.Exists(Flag))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Resume;return;}
            File.Delete(Flag);Run();
        }
        [MenuItem("Tikal/Validation/Run Dwight Gameplay Play Tests")]
        public static void Run()
        {
            DwightGameplayAuthoring.Require(!Application.isBatchMode&&!EditorApplication.isPlaying,"Normal Editor Edit Mode required");
            DwightGameplayAuthoring.Apply();
            var files=new[]{DwightGameplayAuthoring.Prefab,DwightGameplayAuthoring.Controller,DwightGameplayAuthoring.Registry,DwightGameplayAuthoring.Model+".meta"};var before=files.Select(File.ReadAllBytes).ToArray();
            DwightGameplayAuthoring.Apply();DwightGameplayAuthoring.Require(files.Select((f,i)=>File.ReadAllBytes(f).SequenceEqual(before[i])).All(x=>x),"Authoring idempotence changed assets");
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
                AssetDatabase.Refresh();Debug.Log("DWIGHT_PLAY_VALIDATION_FINISHED");
            }
        }
        static void StartTests()
        {
            if(_routine!=null)return;
            // Screenshot GPU readbacks can stall a wall-clock frame. Use fixed simulation
            // time so continuity compares equal intervals; restore on every Finish path.
            _originalCaptureRate=Time.captureFramerate;Time.captureFramerate=60;
            _originalBackground=Application.runInBackground;Application.runInBackground=true;_lastGameFrame=-1;
            Checks.Clear();Errors.Clear();EditorErrors.Clear();Application.logMessageReceived+=Log;
            _routine=Tests();_deadline=EditorApplication.timeSinceStartup+300;EditorApplication.update+=Tick;
        }
        static void Log(string text,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase"))EditorErrors.Add(text+"\n"+stack);else Errors.Add(text+"\n"+stack);
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try {
                if(EditorApplication.timeSinceStartup>_deadline)throw new Exception("Play validation timeout");
                // Editor update can run repeatedly between game frames; yield means a real game frame.
                if(Time.frameCount==_lastGameFrame)return;_lastGameFrame=Time.frameCount;
                if(_routine.MoveNext())return;Finish();
            }
            catch(Exception e){Errors.Add(e.ToString());Finish();}
        }
        static void Check(bool value,string text){if(!value)throw new Exception("Dwight play: "+text);Checks.Add(text);Debug.Log("DWIGHT_PLAY_CHECK "+text);}
        static void Finish()
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;_routine=null;
            var result=new Result{idleWorldSole=_idleGroundBaseline,enteredPlayMode=Application.isPlaying,passed=Application.isPlaying&&Errors.Count==0,checks=Checks.ToArray(),errors=Errors.ToArray(),editorErrors=EditorErrors.ToArray(),scope="Dwight integration: automated interactive-Editor tests (fixed 60 Hz simulation, capture timing restored): actual PlayerMovement/CharacterController, animation bridge, camera visibility, scene spawning, menu selection and isolated save roundtrip. Not a human WASD/puzzle acceptance playtest."};
            Directory.CreateDirectory(Root);File.WriteAllText(Root+"/DwightPlayValidation.json",JsonUtility.ToJson(result,true));Time.captureFramerate=_originalCaptureRate;Application.runInBackground=_originalBackground;EditorApplication.ExitPlaymode();
        }
        static IEnumerator Tests()
        {
            Check(!Application.isBatchMode,"No batchmode, real Play Mode");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="DwightValidationGround";ground.transform.position=new Vector3(0,-.5f,0);ground.transform.localScale=new Vector3(100,1,100);
            Physics.SyncTransforms();
            Check(Mathf.Abs(ground.GetComponent<Collider>().bounds.max.y)<.00001f,"Test fixture collider top is exactly Y=0 before spawning Player");
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.2f,.28f,.2f));ground.GetComponent<Renderer>().sharedMaterial=mat;
            var sun=new GameObject("ValidationSun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
            _player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ThirdPersonCameraFoundationAuthoring.ExplorerPath));_player.transform.position=Vector3.up*.05f;
            Physics.SyncTransforms();
            _player.GetComponent<PlayerController>().enabled=false;
            _movement=_player.GetComponent<PlayerMovement>();_camera=_player.GetComponent<PlayerCameraController>();_spawner=_player.GetComponent<CharacterVisualSpawner>();
            _spawner.Apply(CharacterIds.Default);_animator=_spawner.CurrentVisual.GetComponentInChildren<Animator>(true);
            _modelPosition=_animator.transform.localPosition;
            _orientationPosition=DwightGameplayAuthoring.Orientation(_spawner.CurrentVisual).localPosition;
            MarioAnimationFeelValidation.ValidateProfile();
            Check(_movement.WalkSpeed==4f&&_movement.SprintSpeed==6.5f,"WalkSpeed and SprintSpeed remain exactly approved");
            Check(_animator.speed==1f,"Global Animator.speed unchanged");
            Check(Mathf.Abs(_orientationPosition.y-DwightGameplayAuthoring.VisualOffset)<.0000001f,"Approved Dwight visual wrapper offset unchanged");_hasPelvisSample=false;_maximumPelvisStep=0;
            Check(!_animator.applyRootMotion,"Root Motion remains disabled");
            Check(_animator.runtimeAnimatorController.animationClips.All(c=>c!=null),"No missing animation clips");
            Check(currentPrefabScripts(_spawner.CurrentVisual),"No missing visual scripts");
            Check(_modelPosition==Vector3.zero,"Model local position unchanged; offset is on visual wrapper");
            for(int i=0;i<5;i++)yield return null;
            _camera.ApplyCameraMode(CameraMode.ThirdPerson);
            Check(_spawner.CurrentVisual.activeInHierarchy,"Dwight visible in TPS");
            var current=_spawner.CurrentVisual;_spawner.Apply(CharacterIds.Default);Check(_spawner.CurrentVisual==current,"Repeated selection does not recreate Dwight");
            Check(_player.GetComponents<CharacterController>().Length==1&&current.GetComponentsInChildren<CharacterController>(true).Length==0,"One gameplay capsule; visual has no physics/input duplicates");
            var proxy=_player.transform.Find("CharacterVisualRoot/ThirdPersonHumanProxy");Check(proxy!=null&&!proxy.gameObject.activeSelf,"Proxy suppressed for Dwight");
            for(int i=0;i<45;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(_movement.IsGrounded&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Idle grounded/Locomotion");Capture("Dwight_Idle.png");
            _idleGroundBaseline=MarioWalkGroundValidation.Sole(current)+_player.transform.position.y;
            Debug.Log("DWIGHT_REAL_GROUND_METRIC playerY="+_player.transform.position.y+" sole="+_idleGroundBaseline+" rootLocal="+_animator.transform.localPosition.y);Check(Mathf.Abs(_idleGroundBaseline)<.08f,"Idle sole within 8 cm of flat floor; exact gameplay clearance recorded for human review");
            _hasPelvisSample=false;_maximumPelvisStep=0;ProbePelvis(); // Start after initial bind-pose/Rebind warm-up.
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,false,false,Time.deltaTime);yield return null;ProbePelvis();}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.WalkSpeed)<.05f&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Actual unchanged Walk speed/animation");Capture("Dwight_Walk.png");
            Check(_animator.GetCurrentAnimatorClipInfo(0).Any(x=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.clip))==MarioWalkGroundValidation.CanonicalGuid && x.weight>.9f),"Actual canonical Walk is playing");
            Check(_animator.transform.localPosition==_modelPosition,"Walk root curves do not move visual root");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,true,false,Time.deltaTime);yield return null;ProbePelvis();}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.SprintSpeed)<.05f&&_animator.GetCurrentAnimatorClipInfo(0).Any(x=>x.clip.name=="CHR_Explorer_Run"&&x.weight>.9f),"Actual unchanged Sprint speed/Run clip");Capture("Dwight_Run.png");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,false,false,Time.deltaTime);yield return null;ProbePelvis();}
            Check(_animator.GetCurrentAnimatorClipInfo(0).Any(x=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x.clip))==MarioWalkGroundValidation.CanonicalGuid&&x.weight>.9f),"Run to new Walk transition");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(_animator.GetFloat("Speed")<.01f&&Mathf.Abs(MarioWalkGroundValidation.Sole(current)+_player.transform.position.y-_idleGroundBaseline)<.003f,"Walk to Idle returns to same ground baseline");
            Debug.Log("DWIGHT_TRANSITION_PELVIS_MAX_STEP " + _maximumPelvisStep);
            Check(_maximumPelvisStep<.08f,"Idle-Walk-Run-Walk transitions have no pelvis teleport (>8cm/frame)");
            _movement.RequestJump();bool airborne=false,landed=false,airClipObserved=false;
            for(int i=0;i<160;i++)
            {
                _movement.Tick(Vector2.zero,false,false,Time.deltaTime);airborne|=!_movement.IsGrounded;if(airborne&&_movement.IsGrounded)landed=true;yield return null;
                if(!airClipObserved&&i>=12&&!_movement.IsGrounded)
                {
                    Check(_animator.GetCurrentAnimatorStateInfo(0).IsName("Air")&&_animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="CHR_Explorer_Jump"&&c.weight>.9f),"Physical Jump evaluates shared Humanoid Jump clip in Air state");
                    Capture("Dwight_Jump.png");airClipObserved=true;
                }
            }
            Check(airClipObserved,"Jump animated pose observed while airborne");
            Check(airborne&&landed&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Physical Jump/full-clip fallback/landing settles");
            Check(_animator.transform.localPosition==_modelPosition&&Mathf.Abs(MarioWalkGroundValidation.Sole(current)+_player.transform.position.y-_idleGroundBaseline)<.003f,"Jump/Land returns to same visual baseline without runtime grounding");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,true,Time.deltaTime);yield return null;}
            Check(_movement.IsCrouching&&_animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchIdle"),"CrouchIdle held alias at phase .25");Capture("Dwight_CrouchIdle.png");Check(_animator.transform.localPosition==_modelPosition,"CrouchIdle preserves visual offset");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,false,true,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-_movement.CrouchSpeed)<.05f&&_animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchWalk"),"Actual existing crouch mechanics/clip");Capture("Dwight_CrouchWalk.png");Check(_animator.transform.localPosition==_modelPosition,"CrouchWalk preserves visual offset");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(!_movement.IsCrouching&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Crouch release settles");
            foreach(var amount in new[]{.25f,.5f,.75f,1f}) {
                for(int i=0;i<70;i++){_movement.Tick(Vector2.up*amount,false,false,Time.deltaTime);yield return null;}
                var normalized=_animator.GetFloat("Speed");var clips=_animator.GetCurrentAnimatorClipInfo(0);
                Check(Mathf.Abs(_movement.CurrentSpeed-4f*amount)<.001f,"Analog physical movement magnitude "+amount);
                Check(Mathf.Abs(normalized-4f*amount/6.5f)<.0001f,"Analog Speed normalization "+amount);
                Check(clips.Where(c=>c.clip.name=="CHR_Explorer_Run").All(c=>c.weight<.001f),"No unintended Run blend at analog Walk "+amount);
                Check(Mathf.Abs(clips.Where(c=>c.clip.name=="CHR_Explorer_Walk").Sum(c=>c.weight)-amount)<.02f,"Continuous Idle/Walk analog weights "+amount);
            }
            for(int i=0;i<70;i++){_movement.Tick(Vector2.up,true,false,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-6.5f)<.001f&&_animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="CHR_Explorer_Run"&&c.weight>.99f),"Sprint remains 6.5 m/s and pure independent Run");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.one,false,false,Time.deltaTime);yield return null;}
            Check(Mathf.Abs(_movement.CurrentSpeed-4f)<.001f&&_animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="CHR_Explorer_Walk"&&c.weight>.99f),"Diagonal movement preserves Walk playback/physical speed");
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            for(int i=0;i<4;i++){_camera.ApplyCameraMode(CameraMode.FirstPerson);Check(!current.activeInHierarchy,"FPS hides full body immediately");_camera.ApplyCameraMode(CameraMode.ThirdPerson);Check(current.activeInHierarchy&&_spawner.CurrentVisual==current,"TPS restores same visual");Check(_animator.transform.localPosition==_modelPosition,"FPS/TPS preserves visual offset");yield return null;}
            _movement.RequestJump();_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;
            _movement.ResetForRespawn();_player.GetComponent<PlayerAnimationController>().ResetForRespawn();
            for(int i=0;i<70;i++){_movement.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check(_movement.IsGrounded&&_animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")&&_animator.GetFloat("Speed")<.01f,"Respawn reset hook clears residual airborne/run state");Check(_animator.transform.localPosition==_modelPosition,"Respawn preserves calibrated model offset");
            var path=Path.Combine(Path.GetTempPath(),"TikalDwightValidation-"+Guid.NewGuid().ToString("N"));var save=new GamePersistenceService(path);var progress=GameProgressData.CreateDefault();progress.SelectedCharacterId=CharacterIds.Default;save.SaveProgress(progress);Check(save.LoadProgress().SelectedCharacterId==CharacterIds.Default,"Existing save schema preserves character.default roundtrip (isolated file)");
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Menus/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<10;i++)yield return null;
            var selector=UnityEngine.Object.FindAnyObjectByType<CharacterSelectionController>(FindObjectsInactive.Include);
            Check(selector!=null,"Actual Main Menu has its existing selector");selector.gameObject.SetActive(true);yield return null;
            var selectorType=typeof(CharacterSelectionController);var fields=BindingFlags.Instance|BindingFlags.NonPublic;
            selectorType.GetField("_persistence",fields).SetValue(selector,save);
            selectorType.GetField("_progress",fields).SetValue(selector,GameProgressData.CreateDefault());
            selector.Select(0);selector.Confirm();
            var labels=(TMP_Text[])selectorType.GetField("_characterLabels",fields).GetValue(selector);
            Check(labels[0].text=="DWIGHT"&&save.LoadProgress().SelectedCharacterId==CharacterIds.Default,"Actual first card Dwight/Confirm writes existing schema to isolated save");
            selector.gameObject.SetActive(false);yield return null;selector.gameObject.SetActive(true);yield return null;
            var buttons=(Button[])selectorType.GetField("_characterButtons",fields).GetValue(selector);
            var marker=(GameObject)typeof(MenuSelectableVisual).GetField("_persistentMarker",fields).GetValue(buttons[0].GetComponent<MenuSelectableVisual>());
            Check(marker.activeSelf,"Dwight remains selected after leaving and reopening panel");
            foreach(var slot in new[]{0,1,2})
            {
                selector.Select(slot);selector.Confirm();var expected=CharacterCatalog.All[slot];
                Check(save.LoadProgress().SelectedCharacterId==expected.Id,"Actual menu confirms and persists "+expected.DisplayName);
                selector.gameObject.SetActive(false);yield return null;selector.gameObject.SetActive(true);yield return null;
                Check(((GameObject)typeof(MenuSelectableVisual).GetField("_persistentMarker",fields).GetValue(buttons[slot].GetComponent<MenuSelectableVisual>())).activeSelf,"Reopen panel retains "+expected.DisplayName);
                selector.Select(3);selector.Confirm();Check(save.LoadProgress().SelectedCharacterId==expected.Id&&!buttons[3].interactable,"Locked card cannot replace "+expected.DisplayName);
                var direct=save.LoadProgress();direct.SelectedCharacterId=CharacterIds.Character04;Check(direct.SelectedCharacterId==expected.Id,"Save setter rejects locked ID preserving "+expected.DisplayName);
            }
            Check(labels[3].text.Contains("DESCONOCIDO")&&labels[3].text.Contains("BLOQUEADO"),"Visible locked label without unsupported emoji glyph");
            var legacy=JsonUtility.FromJson<GameProgressData>("{\"_selectedCharacterId\":\"character.04\"}");legacy.Normalize();Check(legacy.SelectedCharacterId==CharacterIds.Default,"Legacy locked selection normalizes safely to default");
            selector.Select(0);selector.Confirm();
            var events=UnityEngine.EventSystems.EventSystem.current;buttons[0].Select();
            Check(events.currentSelectedGameObject==buttons[0].gameObject&&buttons[0].interactable,"Dwight card receives existing UI focus");
            Check(CharacterCatalog.All[2].Id==CharacterIds.Character03&&CharacterCatalog.All[2].DisplayName=="Mario","Mario remains character.03 slot 3");
            Directory.CreateDirectory(Evidence);ScreenCapture.CaptureScreenshot(Evidence+"/Dwight_Roster.png");for(int i=0;i<3;i++)yield return null;
            foreach(var scenePath in DwightGameplayAuthoring.Scenes)
            {
                EditorSceneManager.LoadSceneInPlayMode(scenePath,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                var players=UnityEngine.Object.FindObjectsByType<PlayerController>();Check(players.Length==1,"Scene has one existing Player: "+scenePath);
                var spawner=players[0].GetComponent<CharacterVisualSpawner>();spawner.Apply(save.LoadProgress().SelectedCharacterId);var camera=players[0].GetComponent<PlayerCameraController>();camera.ApplyCameraMode(CameraMode.ThirdPerson);yield return null;
                Check(spawner.HasVisual&&spawner.CurrentVisual.activeInHierarchy&&spawner.CurrentVisual.GetComponentInChildren<Animator>().avatar.isValid,"Persisted selection maps to Dwight in scene: "+scenePath);
                camera.ApplyCameraMode(CameraMode.FirstPerson);Check(!spawner.CurrentVisual.activeInHierarchy,"Scene FPS hide: "+scenePath);
                if(scenePath.Contains("Tutorial"))
                {
                    camera.ApplyCameraMode(CameraMode.ThirdPerson);
                    players[0].enabled=false;var move=players[0].GetComponent<PlayerMovement>();
                    for(int i=0;i<60;i++){move.Tick(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                    Check(spawner.CurrentVisual.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Actual Tutorial Dwight grounded locomotion");
                }
                if(scenePath.Contains("Level01")){var checkpoint=UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();Check(checkpoint!=null&&checkpoint.CanRestartFromCheckpoint,"Actual Level01 checkpoint available");checkpoint.RestartFromCheckpoint();camera.ApplyCameraMode(CameraMode.ThirdPerson);for(int i=0;i<25;i++)yield return null;var anim=spawner.CurrentVisual.GetComponentInChildren<Animator>();Check(!checkpoint.IsRespawning&&anim.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Actual Level01 RestartFromCheckpoint resets animation safely");Check(Mathf.Abs(DwightGameplayAuthoring.Orientation(spawner.CurrentVisual).localPosition.y-DwightGameplayAuthoring.VisualOffset)<.0000001f,"Actual checkpoint preserves visual calibration");
                    var extra=LevelSmoke(players[0],spawner,save);while(extra.MoveNext())yield return extra.Current;
                }
                foreach(var id in new[]{CharacterIds.Default,CharacterIds.Character02,CharacterIds.Character03})
                {
                    spawner.Apply(id);yield return null;camera.ApplyCameraMode(CameraMode.ThirdPerson);
                    var visual=spawner.CurrentVisual;var expected=CharacterCatalog.GetOrDefault(id).VisualPrefab;var actor=visual.GetComponentInChildren<Animator>(true);var source=expected.GetComponentInChildren<Animator>(true);
                    Check(actor.avatar==source.avatar&&actor.avatar.isValid&&actor.avatar.isHuman&&!actor.applyRootMotion&&actor.runtimeAnimatorController==source.runtimeAnimatorController,"Roster switch own Avatar/shared Animator "+id+" in "+scenePath);
                    Check(visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(x=>x.sharedMaterials).SequenceEqual(expected.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(x=>x.sharedMaterials)),"Roster switch materials "+id);
                    Check(visual.transform.parent.GetComponentsInChildren<Animator>(true).Length==1,"No residual visual on roster switch "+id);
                    camera.ApplyCameraMode(CameraMode.FirstPerson);Check(!visual.activeInHierarchy,"Roster FPS body hidden "+id);camera.ApplyCameraMode(CameraMode.ThirdPerson);Check(visual.activeInHierarchy,"Roster TPS body visible "+id);
                    spawner.Apply(CharacterIds.Character04);Check(spawner.CurrentVisual==visual,"Locked ID cannot replace active visual "+id);
                }
                spawner.Apply(CharacterIds.Default);yield return null;
                var dwightAvatar=spawner.CurrentVisual.GetComponentInChildren<Animator>(true).avatar;
                spawner.Apply(CharacterIds.Character03);yield return null;camera.ApplyCameraMode(CameraMode.ThirdPerson);
                var marioVisual=spawner.CurrentVisual;var marioAnimator=marioVisual.GetComponentInChildren<Animator>(true);
                Check(marioVisual.name==CharacterIds.Character03&&marioAnimator.avatar!=dwightAvatar&&marioAnimator.runtimeAnimatorController==AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DwightGameplayAuthoring.Controller),"Switch Dwight to Mario retains distinct Avatar and shared controller: "+scenePath);
                spawner.Apply(CharacterIds.Default);yield return null;
                Check(spawner.CurrentVisual.name==CharacterIds.Default&&spawner.CurrentVisual.GetComponentInChildren<Animator>(true).avatar==dwightAvatar&&players[0].GetComponent<PlayerAnimationController>().CurrentAnimator==spawner.CurrentVisual.GetComponentInChildren<Animator>(true),"Switch Mario back to Dwight reattaches correct Animator: "+scenePath);
            }
            // Explicit requested route: a fresh real menu selection precedes every target scene.
            foreach(var slot in new[]{0,1,2})
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Menus/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                var menu=UnityEngine.Object.FindAnyObjectByType<CharacterSelectionController>(FindObjectsInactive.Include);menu.gameObject.SetActive(true);yield return null;
                SetField(menu,"_persistence",save);SetField(menu,"_progress",save.LoadProgress());menu.Select(slot);menu.Confirm();
                var id=CharacterCatalog.All[slot].Id;Check(save.LoadProgress().SelectedCharacterId==id,"Requested route begins with real menu confirmation "+id);
                var destination=DwightGameplayAuthoring.Scenes[slot==2?1:0];
                EditorSceneManager.LoadSceneInPlayMode(destination,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var spawn=player.GetComponent<CharacterVisualSpawner>();spawn.Apply(save.LoadProgress().SelectedCharacterId);player.GetComponent<PlayerCameraController>().ApplyCameraMode(CameraMode.ThirdPerson);yield return null;
                var actual=spawn.CurrentVisual.GetComponentInChildren<Animator>();var expected=CharacterCatalog.All[slot].VisualPrefab.GetComponentInChildren<Animator>();
                Check(spawn.CurrentVisual.name==id&&actual.avatar==expected.avatar&&actual.runtimeAnimatorController==expected.runtimeAnimatorController&&spawn.CurrentVisual.transform.parent.GetComponentsInChildren<Animator>(true).Length==1,"Requested menu-to-scene route own Avatar/no stale visual "+id+" => "+destination);
            }
        }
        static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void SetField(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static void Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        static IEnumerator LevelSmoke(PlayerController player,CharacterVisualSpawner spawner,GamePersistenceService save)
        {
            player.enabled=true;player.SetGameplayActive(true);var camera=player.GetComponent<PlayerCameraController>();
            var journal=UnityEngine.Object.FindAnyObjectByType<ClueJournalPresenter>();journal.Open();yield return null;
            Check(journal.IsOpen&&!player.GameplayActive,"Level01 Journal opens and locks gameplay with Dwight");journal.Close();yield return null;
            Check(!journal.IsOpen&&player.GameplayActive,"Level01 Journal closes and restores Dwight gameplay");
            var pause=UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();pause.Open();yield return null;
            Check(pause.IsOpen&&Time.timeScale==0,"Level01 Pause works with Dwight");pause.ShowSettings();yield return null;
            var settings=(SettingsMenuController)Field(pause,"_settings");SetField(settings,"_persistence",save);
            var dropdown=(TMP_Dropdown)Field(settings,"_cameraMode");var visual=spawner.CurrentVisual;
            dropdown.SetValueWithoutNotify(1);dropdown.value=0;yield return null;
            Check(camera.Mode==CameraMode.FirstPerson&&!visual.activeInHierarchy,"Pause Settings TPS to FPS immediately hides Dwight");
            dropdown.value=1;yield return null;
            Check(camera.Mode==CameraMode.ThirdPerson&&visual.activeInHierarchy&&spawner.CurrentVisual==visual,"Pause Settings FPS to TPS restores same Dwight instance");
            pause.Resume();yield return null;Check(!pause.IsOpen&&Time.timeScale==1,"Resume restores gameplay time");
            // Door smoke test uses the actual interaction resolver and queued interaction path.
            var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;player.transform.SetPositionAndRotation(new Vector3(0,.08f,-2.6f),Quaternion.identity);capsule.enabled=true;
            camera.SnapAfterTeleport();camera.ApplyCameraMode(CameraMode.FirstPerson);yield return null;
            var door=UnityEngine.Object.FindAnyObjectByType<Tikal.Interaction.Door>();var interaction=player.GetComponent<Tikal.Interaction.PlayerInteraction>();
            // The first child collider can be the lintel, outside reach; aim at the moving leaf.
            var leaf=((Transform)Field(door,"_pivot")).GetComponentInChildren<Collider>();
            var target=leaf.bounds.center;interaction.ViewCamera.transform.LookAt(target);Physics.SyncTransforms();Call(interaction,"LateUpdate");
            Check(ReferenceEquals(interaction.CurrentInteractable,door),"Level01 actual Door acquired through existing interaction ray with Dwight");
            Call(interaction,"OnInteractPressed");Call(interaction,"LateUpdate");yield return null;
            Check(door.State==Tikal.Interaction.Door.DoorState.Opening||door.State==Tikal.Interaction.Door.DoorState.Open,"Existing Interact opens Level01 Door with Dwight");
            for(int i=0;i<60;i++)yield return null;
            Check(door.State==Tikal.Interaction.Door.DoorState.Open,"Existing Door reaches open state");
            var secret=UnityEngine.Object.FindAnyObjectByType<SecretEntranceCommandReceiver>();
            Check(secret.IsConfigured&&secret.MainDoor==door&&!secret.CanExecuteCommand,"SecretCommand keeps its existing Door/zone/outcome conditions");
            var original=door.State;Call(secret,"Receive");Check(door.State==original,"Ineligible SecretCommand does not operate ordinary Door");
            var puzzles=UnityEngine.Object.FindObjectsByType<Tikal.Puzzles.PuzzleBase>(FindObjectsInactive.Include);
            Check(puzzles.Length==5&&puzzles.All(p=>p.enabled),"Five canonical Level01 puzzles retained, including intentionally gated inactive content");
            var mural=puzzles.OfType<Tikal.Puzzles.Level01.MuralFragmentsPuzzle>().Single();var fragment=((Tikal.Puzzles.Level01.MuralFragment[])Field(mural,"_fragments"))[0];
            int orientation=fragment.CurrentOrientation;if(mural.State==Tikal.Puzzles.PuzzleState.Inactive)mural.MakeAvailable();fragment.Interact();
            for(int i=0;i<30;i++)yield return null;
            Check(fragment.CurrentOrientation==(orientation+1)%4&&!fragment.IsRotating&&mural.State==Tikal.Puzzles.PuzzleState.Active,"Existing mural fragment interaction/rotation works with Dwight");
            camera.ApplyCameraMode(CameraMode.ThirdPerson);var checkpoint=UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();
            var move=player.GetComponent<PlayerMovement>();for(int i=0;i<40;i++){move.Tick(Vector2.up,true,true,Time.deltaTime);yield return null;}
            pause.Open();yield return null;Check(pause.IsOpen&&pause.CanRestart,"Pause exposes existing Restart From Checkpoint");pause.Restart();
            for(int i=0;i<55;i++)yield return null;
            var a=spawner.CurrentVisual.GetComponentInChildren<Animator>();
            Check(!checkpoint.IsRespawning&&move.IsGrounded&&!move.IsCrouching&&a.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")&&a.GetFloat("Speed")<.01f&&!a.GetBool("IsCrouching"),"Actual Pause checkpoint restart clears Run/Crouch/Falling state");
            Check(Mathf.Abs(DwightGameplayAuthoring.Orientation(spawner.CurrentVisual).localPosition.y-DwightGameplayAuthoring.VisualOffset)<.0000001f,"Actual Pause checkpoint retains Dwight offset");
        }
        static void ProbePelvis()
        {
            var y=_animator.GetBoneTransform(HumanBodyBones.Hips).position.y-_player.transform.position.y;
            if(_hasPelvisSample) {
                var step=Mathf.Abs(y-_previousPelvisY);_maximumPelvisStep=Mathf.Max(_maximumPelvisStep,step);
                if(step>.08f)Debug.Log("DWIGHT_PELVIS_OUTLIER previous="+_previousPelvisY+" next="+y+" speed="+_animator.GetFloat("Speed")+" dt="+Time.deltaTime+" phase="+_animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            }
            _previousPelvisY=y;_hasPelvisSample=true;
        }
        static bool currentPrefabScripts(GameObject visual)=>visual.GetComponentsInChildren<MonoBehaviour>(true).All(x=>x!=null);
        static void Capture(string name)
        {
            name=name.Replace("Dwight_","DwightFeel_");
            var renderers=_spawner.CurrentVisual.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var renderer in renderers)
            {
                var bounds=DwightGameplayAuthoring.GeometryBounds(_spawner.CurrentVisual);
                var headHeight=_animator.GetBoneTransform(HumanBodyBones.Head).position.y-_player.transform.position.y;
                Check(bounds.size.y>.5f&&bounds.size.y<2.3f&&bounds.center.y-_player.transform.position.y<2&&headHeight>.7f&&headHeight<2&&Vector2.Distance(new Vector2(bounds.center.x,bounds.center.z),new Vector2(_player.transform.position.x,_player.transform.position.z))<1,"Animated rig remains human-sized at Player: "+name);
            }
            var camera=new GameObject("DwightEvidenceCamera").AddComponent<Camera>();camera.fieldOfView=40;camera.transform.position=_player.transform.position+new Vector3(2.4f,1.5f,3.5f);camera.transform.LookAt(_player.transform.position+Vector3.up*.9f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.2f);camera.allowHDR=false;
            var rt=new RenderTexture(640,640,24);rt.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};Check(RenderPipeline.SupportsRenderRequest(camera,request),"URP evidence rendering supported");RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();Check(!image.GetPixels32().Any(p=>p.r>220&&p.g<50&&p.b>220),"No magenta: "+name);var body=image.GetPixels32();
            try {
                foreach(var renderer in renderers)renderer.enabled=false;
                RenderPipeline.SubmitRenderRequest(camera,request);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();
                var background=image.GetPixels32();var visiblePixels=body.Where((pixel,i)=>Mathf.Abs(pixel.r-background[i].r)+Mathf.Abs(pixel.g-background[i].g)+Mathf.Abs(pixel.b-background[i].b)>30).Count();
                Check(visiblePixels>1000,"Dwight contributes visible pixels (not empty render): "+name);
            } finally {foreach(var renderer in renderers)renderer.enabled=true;}
            image.SetPixels32(body);image.Apply();Directory.CreateDirectory(Evidence);File.WriteAllBytes(Evidence+"/"+name,image.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(camera.gameObject);
        }
    }
}
