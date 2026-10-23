using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.Tests.Support;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class ThirdPersonReticleAimPlayValidation
    {
        const string Key="Tikal.ReticleCameraAim", Folder="../_ReticleCameraAim_20261023";
        const string ScenePath="Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        [Serializable] class Sample
        {
            public string label,mode,focus;public Vector2 anchorsMin,anchorsMax,pivot,anchoredPosition,reticleViewport;
            public Vector3 camera,forward,rayOrigin,rayDirection,interactionOrigin,visualAim,physicalAim,shoulder;
            public float cameraPitch,pivotHeight,screenErrorPixels;
        }
        [Serializable] class Report {public bool passed;public string scope;public string[] checks,errors;public Sample[] samples;}
        static readonly Stack<IEnumerator> work=new Stack<IEnumerator>();static readonly List<string> checks=new List<string>(),errors=new List<string>();static readonly List<Sample> samples=new List<Sample>();
        static PlayerController player;static PlayerCameraController rig;static PlayerInteraction interaction;static ReticlePresenter reticle;static ReticleSettings settings;static Camera camera;static GamePersistenceService save;
        static int lastFrame;static double deadline;static bool oldRun;static float oldVolume;static GameObject fixtures;static bool Audit=>SessionState.GetBool(Key+"Audit",false);
        static ThirdPersonReticleAimPlayValidation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Mode;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/ReticleCameraAim.request"))return;
            bool audit=File.ReadAllText("Temp/ReticleCameraAim.request").Contains("audit");File.Delete("Temp/ReticleCameraAim.request");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved scene before reticle validation; no environment authoring permitted.");
            Directory.CreateDirectory(Folder);SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);SessionState.SetBool(Key+"Audit",audit);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        static void Invoke(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
        static void Inject(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
            {foreach(var flow in root.GetComponentsInChildren<GameFlowController>(true)){Set(flow,"_persistence",save);Set(flow,"_progress",save.LoadProgress());}foreach(var applier in root.GetComponentsInChildren<PersistentSettingsApplier>(true)){Set(applier,"_service",save);applier.Apply(GameSettingsData.CreateDefault());}}
        }
        static void Mode(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {checks.Clear();errors.Clear();samples.Clear();work.Clear();save=new GamePersistenceService(Path.GetFullPath(Folder+"/IsolatedSave"));save.SaveProgress(GameProgressData.CreateDefault());SceneManager.sceneLoaded+=Inject;oldRun=Application.runInBackground;oldVolume=AudioListener.volume;Application.runInBackground=true;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+120;work.Push(Tests());EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool(Key,false);var path=SessionState.GetString(Key+"Scene",ScenePath);if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);Debug.Log("TIKAL_RETICLE_CAMERA_AIM_FINISHED");}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Reticle local validation");while(work.Count>0){var e=work.Peek();if(!e.MoveNext()){work.Pop();continue;}if(e.Current is IEnumerator child){work.Push(child);continue;}return;}Finish();}catch(Exception e){errors.Add(e.ToString());Finish();}
        }
        static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Inject;Application.runInBackground=oldRun;AudioListener.volume=oldVolume;
            File.WriteAllText(Folder+(Audit?"/BeforeAudit.json":"/Validation.json"),JsonUtility.ToJson(new Report{passed=errors.Count==0,scope=Audit?"Before-change authored/runtime audit and reproducible physical parallax only":"Local native Level01 reticle/camera fixtures only; no environment edits or puzzle regression; isolated persistence",checks=checks.ToArray(),errors=errors.ToArray(),samples=samples.ToArray()},true));EditorApplication.ExitPlaymode();
        }
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static GameObject Box(string name,Vector3 center,Vector3 size,bool probe=false)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(fixtures.transform);go.transform.position=center;go.transform.localScale=size;if(probe)go.AddComponent<InteractionTestProbe>();return go;}
        static void Place(Vector3 position)
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.identity);cc.enabled=true;var move=player.GetComponent<PlayerMovement>();move.ResetForRespawn();var look=player.GetComponent<PlayerLook>();look.ResetAfterRespawn(move.EyeHeight,move.MaximumEyeHeight);look.SynchronizePlayerYaw();Physics.SyncTransforms();rig.SnapAfterTeleport();
        }
        static void Refresh(){Physics.SyncTransforms();Canvas.ForceUpdateCanvases();Invoke(interaction,"LateUpdate");Invoke(reticle,"LateUpdate");}
        static Sample Record(string label)
        {
            Refresh();var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));var hit=Physics.RaycastAll(ray,30,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(player.transform)).OrderBy(h=>h.distance).FirstOrDefault();var rect=(RectTransform)reticle.transform;var canvas=reticle.GetComponentInParent<Canvas>();var screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.position);var center=camera.ViewportToScreenPoint(new Vector3(.5f,.5f,0));var vp=camera.ScreenToViewportPoint(screen);
            var sample=new Sample{label=label,mode=rig.Mode.ToString(),focus=interaction.CurrentInteractable==null?"none":interaction.CurrentInteractable.GetType().Name,anchorsMin=rect.anchorMin,anchorsMax=rect.anchorMax,pivot=rect.pivot,anchoredPosition=rect.anchoredPosition,reticleViewport=new Vector2(vp.x,vp.y),camera=camera.transform.position,forward=camera.transform.forward,rayOrigin=ray.origin,rayDirection=ray.direction,interactionOrigin=interaction.InteractionOrigin.position,visualAim=hit.collider!=null?hit.point:ray.GetPoint(30),physicalAim=interaction.ResolvedAimPoint,shoulder=new Vector3(rig.Profile.ShoulderOffset,0,-rig.Profile.Distance),cameraPitch=camera.transform.eulerAngles.x,pivotHeight=rig.Profile.PivotHeight,screenErrorPixels=Vector2.Distance(screen,center)};samples.Add(sample);return sample;
        }
        static void Center(string label)=>Check(Record(label).screenErrorPixels<.25f,label+": reticle matches camera viewport center (<0.25px)");
        static IEnumerator Shot(string name){yield return Frames(2);ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");yield return Frames(3);}
        static IEnumerator Tests()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(15);player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();rig=player.GetComponent<PlayerCameraController>();interaction=player.GetComponent<PlayerInteraction>();camera=interaction.ViewCamera;reticle=UnityEngine.Object.FindAnyObjectByType<ReticlePresenter>();settings=reticle.GetComponent<ReticleSettings>();
            Check(player!=null&&reticle!=null&&rig.IsConfigured,"Saved Level01 loads with original configured Player/camera/reticle");
            var reader=player.GetComponent<PlayerInputReader>();var actions=(TikalInputActions)typeof(PlayerInputReader).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reader);actions.devices=Array.Empty<InputDevice>();
            var data=new SerializedObject(reticle);var dot=(GameObject)data.FindProperty("_dot").objectReferenceValue;var cross=(GameObject)data.FindProperty("_cross").objectReferenceValue;
            Record("Authored Level01 initial state");fixtures=new GameObject("Temporary_ReticleAimFixtures");Box("Temporary floor",new Vector3(1000,9.9f,1000),new Vector3(40,.2f,40));Place(new Vector3(1000,10.05f,1000));rig.ApplyCameraMode(CameraMode.ThirdPerson);yield return Frames(8);rig.SnapAfterTeleport();settings.SetMode(ReticleMode.Dot);
            var target=Box("Temporary aim target",camera.ViewportPointToRay(new Vector3(.5f,.5f,0)).GetPoint(5),Vector3.one*.4f,true);Refresh();var aim=interaction.ResolvedAimPoint;var origin=interaction.InteractionOrigin.position;
            var wall=Box("Temporary reach occluder",Vector3.Lerp(origin,aim,.5f),new Vector3(.08f,1,.12f));var old=Record(Audit?"BEFORE: physical occlusion displaces reticle":"AFTER: physical occlusion does not redirect camera aim");
            Check(interaction.CurrentInteractable==null&&interaction.HasResolvedAimPoint,"Physical obstruction rejects interaction without changing camera intent");
            if(Audit){Check(old.screenErrorPixels>.5f,"Reproduced noncentral reticle from physical occlusion");yield break;}
            Check(old.screenErrorPixels<.25f,"Occlusion preserves canonical center");wall.SetActive(false);
            foreach(float distance in new[]{2f,5f,10f})
            {
                var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f,0));target.transform.position=ray.GetPoint(distance);Refresh();Center("TPS target "+distance+"m");
                if(distance==5){Check(ReferenceEquals(interaction.CurrentInteractable,target.GetComponent<InteractionTestProbe>()),"5m camera distance remains within original 3m physical reach");Invoke(interaction,"OnInteractPressed");Invoke(interaction,"LateUpdate");Check(target.GetComponent<InteractionTestProbe>().InteractionCount==1,"Native queued interaction reaches visible simple target once");}
                if(distance==10)Check(interaction.CurrentInteractable==null,"Far visual target does not extend physical reach");
                foreach(var mode in new[]{ReticleMode.Dot,ReticleMode.Cross,ReticleMode.None}){settings.SetMode(mode);Center("TPS "+distance+"m "+mode);Check(dot.activeSelf==(mode==ReticleMode.Dot)&&cross.activeSelf==(mode==ReticleMode.Cross),"Mode visibility "+mode);}
            }
            target.SetActive(false);settings.SetMode(ReticleMode.Dot);rig.ApplyCameraMode(CameraMode.FirstPerson);yield return Frames(3);foreach(var mode in new[]{ReticleMode.Dot,ReticleMode.Cross,ReticleMode.None}){settings.SetMode(mode);Center("FPS "+mode);Check(reticle.ScreenOffset==Vector2.zero,"FPS retains exact authored zero offset");}
            rig.ApplyCameraMode(CameraMode.ThirdPerson);settings.SetMode(ReticleMode.Cross);rig.SnapAfterTeleport();var full=camera.transform.position;var blocker=Box("Temporary camera wall",player.transform.position+new Vector3(0,1.5f,-1.9f),new Vector3(8,4,.25f));yield return Frames(8);Check(Vector3.Distance(camera.transform.position,full)>.5f,"Actual boom retracts against wall using existing collision controller");
            for(int i=0;i<8;i++){Center("Retracted camera stable frame "+i);yield return null;}yield return Shot("TPS_Retracted_Cross");blocker.SetActive(false);
            var door=UnityEngine.Object.FindAnyObjectByType<Door>();var leaf=door.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger);var point=leaf.bounds.center;Place(new Vector3(point.x,leaf.bounds.min.y+.11f,point.z-1.8f));yield return Frames(5);camera.transform.rotation=Quaternion.LookRotation(point-camera.transform.position);Refresh();Center("Real Door aim");Check(ReferenceEquals(interaction.CurrentInteractable,door),"Real Door under camera ray and within physical reach");Invoke(interaction,"OnInteractPressed");Invoke(interaction,"LateUpdate");Check(door.State==Door.DoorState.Opening||door.State==Door.DoorState.Open,"Original Door receives native queued interaction");yield return Shot("TPS_Door_Cross");
            Check(SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No Missing Scripts in loaded scene");
        }
    }
}
