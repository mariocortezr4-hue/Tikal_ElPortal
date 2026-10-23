using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tikal.Core;
using Tikal.Persistence;
using Tikal.Puzzles.Level01;
using Tikal.UI;
using Tikal.Interaction;

namespace Tikal.Tests.Editor
{
    public static partial class Level01EnvironmentPlayValidation
    {
        static IEnumerator FalsePathCapture(string name)
        {yield return Frames(20);ScreenCapture.CaptureScreenshot(Folder+"/"+name+".png");yield return Frames(4);}
        static IEnumerator FalsePathTests()
        {
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                save.SaveProgress(GameProgressData.CreateDefault());EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(18);Bind();CameraMode(mode);
                var reveal=UnityEngine.Object.FindAnyObjectByType<FalsePathReveal>();var rs=new SerializedObject(reveal);var path=(GameObject)rs.FindProperty("_path").objectReferenceValue;var discovery=(SecretKeyDiscoveryPuzzle)rs.FindProperty("_puzzle").objectReferenceValue;
                // Local precondition fixture only: expose the gated discovery in memory, without retesting Zone05.
                // The original public puzzle API then fires the production false-path reveal event.
                var gate=UnityEngine.Object.FindAnyObjectByType<Zone05RevelationGate>();var content=(GameObject)new SerializedObject(gate).FindProperty("_content").objectReferenceValue;content.SetActive(true);
                Check(discovery.MakeAvailable(),"Local solved-prerequisite fixture made available "+mode);discovery.Interact();yield return Frames(5);Check(discovery.IsSolved&&reveal.IsRevealed&&path.activeInHierarchy,"Existing discovery event opens original false path "+mode);
                var trap=path.GetComponentInChildren<PointOfNoReturnTrigger>();var ts=new SerializedObject(trap);var closure=(GameObject)ts.FindProperty("_closingPassage").objectReferenceValue;var outcome=UnityEngine.Object.FindAnyObjectByType<LevelOutcomeController>();var manager=UnityEngine.Object.FindAnyObjectByType<CheckpointManager>();var result=UnityEngine.Object.FindAnyObjectByType<LevelResultPresenter>();var title=(TMPro.TMP_Text)new SerializedObject(result).FindProperty("_title").objectReferenceValue;var warning=path.transform.Find("FinalWarningSteleAnchor");
                Check(manager.IsConfigured&&manager.HasCheckpoint&&outcome.UsesCheckpointRespawn,"Original valid checkpoint and outcome references "+mode);var checkpoint=manager.CurrentCheckpoint;int respawns=0;manager.RespawnCompleted+=()=>respawns++;
                Check(Vector3.Distance(trap.transform.position,new Vector3(6,1.8f,91.5f))<.01f&&!closure.activeSelf,"Trigger pose and initial closure unchanged "+mode);
                Check(path.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No Missing Scripts in local path "+mode);
                Place(new Vector3(6,Floor(6,77.1f),77.1f),0);yield return Frames(6);yield return WalkTo(6,80.5f);Check(player.GameplayActive&&outcome.State==LevelOutcome.Playing,"Physical Zone05 outlet to False Path accessible "+mode);
                var wp=warning.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();Check(wp.Length>0&&wp.Any(r=>GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(ViewCamera),r.bounds)),"Warning visible from approach "+mode);Check(warning.GetComponentsInChildren<ReadableInscription>(true).Length==0,"Warning preserves visual-only design; no new read prompt "+mode);
                if(mode==Tikal.Persistence.CameraMode.ThirdPerson){yield return FalsePathCapture("FalsePath_Approach");yield return WalkTo(6,82);CameraMode(Tikal.Persistence.CameraMode.FirstPerson);Place(new Vector3(6,.71f,81.2f),335);yield return ObservationAim(warning.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="FinalWarningStele_Blockout").transform);yield return FalsePathCapture("FinalWarning");CameraMode(mode);Place(new Vector3(6,.71f,82),0);yield return Frames(6);}
                yield return WalkTo(6,85.5f);yield return WalkTo(6,80.5f);yield return WalkTo(6,77.1f);Check(outcome.State==LevelOutcome.Playing&&!closure.activeSelf&&player.GameplayActive,"Native pretrigger backtracking to Zone05 remains possible "+mode);
                yield return WalkTo(6,85.5f);yield return WalkTo(6,89.7f);Check(outcome.State==LevelOutcome.Playing&&!closure.activeSelf,"Approach/decision space before PONR safe "+mode);
                if(mode==Tikal.Persistence.CameraMode.ThirdPerson)
                {
                    yield return FalsePathCapture("BeforePointOfNoReturn");
                    foreach(var view in new[]{("FalsePath_Left",new Vector3(6,.71f,86),315f),("FalsePath_Right",new Vector3(6,.71f,86),45f),("FalsePath_Wide",new Vector3(6,.71f,79.3f),0f)}){Place(view.Item2,view.Item3);yield return FalsePathCapture(view.Item1);}
                }
                // Cross while looking back, so the original activated closure is visible in the production camera.
                Place(new Vector3(6,.71f,90.15f),180);yield return Frames(6);
                for(int i=0;i<100&&outcome.State==LevelOutcome.Playing;i++){Input(Vector2.down);yield return null;}
                Check(outcome.State==LevelOutcome.Defeat&&closure.activeSelf&&!player.GameplayActive,"Physical crossing activates original closure and Defeat "+mode);
                Check(result.IsVisible&&title.text=="HAS QUEDADO ATRAPADO","Original exact defeat presentation "+mode);
                var collider=closure.GetComponent<Collider>();Check(collider.enabled&&collider.Raycast(new Ray(new Vector3(6,1.5f,90.7f),Vector3.back),out var hit,4),"Original closure collider prevents return across corridor "+mode);
                Check(closure.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"Art closure inherits real activation "+mode);
                if(mode==Tikal.Persistence.CameraMode.ThirdPerson){yield return Frames(2);ScreenCapture.CaptureScreenshot(Folder+"/AfterClosure.png");yield return Frames(3);}
                for(int i=0;i<300&&respawns==0;i++)yield return Frames(1);
                Check(respawns==1&&manager.CurrentCheckpoint==checkpoint&&!manager.IsRespawning&&Vector3.Distance(player.transform.position,checkpoint.Position)<.8f,"Automatic defeat/fade returns to last valid checkpoint "+mode+" "+checkpoint.CheckpointId+" at "+checkpoint.Position);
                Check(outcome.State==LevelOutcome.Playing&&player.GameplayActive&&!result.IsVisible&&!closure.activeSelf,"Respawn restores Playing, controls, UI and closure reset "+mode);
            }
        }
    }
}
