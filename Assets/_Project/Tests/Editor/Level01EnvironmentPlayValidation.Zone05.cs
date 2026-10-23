using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Tikal.Input;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Level01;
using Tikal.UI;

namespace Tikal.Tests.Editor
{
    public static partial class Level01EnvironmentPlayValidation
    {
        static IEnumerator Zone05Bindings(ClueDefinition clue)
        {
            var reader=player.GetComponent<PlayerInputReader>();var devices=player.GetComponent<InputDeviceManager>();var presenter=UnityEngine.Object.FindAnyObjectByType<ClueDiscoveryPresenter>();var description=(TMPro.TMP_Text)new SerializedObject(presenter).FindProperty("_description").objectReferenceValue;
            var keyboard=InputSystem.AddDevice<Keyboard>();var xbox=InputSystem.AddDevice<UnityEngine.InputSystem.XInput.XInputController>();var ps=InputSystem.AddDevice<UnityEngine.InputSystem.DualShock.DualShockGamepad>();localValidationDevices.AddRange(new InputDevice[]{keyboard,xbox,ps});
            foreach(var source in new InputDevice[]{keyboard,xbox,ps,pad})
            {
                if(source is Keyboard k)InputSystem.QueueStateEvent(k,new KeyboardState(UnityEngine.InputSystem.Key.F12));else InputSystem.QueueStateEvent((Gamepad)source,new GamepadState().WithButton(GamepadButton.LeftShoulder));yield return null;yield return Frames(2);
                var family=InputDeviceManager.Classify(source);Check(devices.ActiveDevice==family,"Native device family selected "+family);var secret=InputPromptResolver.Resolve(reader.SecretCommandAction,family);var interact=InputPromptResolver.Resolve(reader.InteractAction,family);Check(!string.IsNullOrEmpty(secret.DisplayText)&&secret.ControlPath!=interact.ControlPath,"SecretCommand remains distinct from Interact "+family);Check(description.text.Contains("[ "+secret.DisplayText+" ]")&&description.text.Contains(clue.Description),"Live Clue05 binding "+family+" = "+secret.DisplayText+" path="+secret.ControlPath);
                if(source is Keyboard kk)InputSystem.QueueStateEvent(kk,new KeyboardState());else InputSystem.QueueStateEvent((Gamepad)source,new GamepadState());yield return null;
            }
        }
        static IEnumerator Zone05Tests()
        {
            foreach(var mode in new[]{Tikal.Persistence.CameraMode.FirstPerson,Tikal.Persistence.CameraMode.ThirdPerson})
            {
                save.SaveProgress(GameProgressData.CreateDefault());EditorSceneManager.LoadSceneInPlayMode(Level01EnvironmentArtPass.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return Frames(18);Bind();CameraMode(mode);var gate=UnityEngine.Object.FindAnyObjectByType<Zone05RevelationGate>();Check(gate!=null,"Real Zone05 scene loads "+mode);var gs=new SerializedObject(gate);var content=(GameObject)gs.FindProperty("_content").objectReferenceValue;var discovery=(SecretKeyDiscoveryPuzzle)gs.FindProperty("_discovery").objectReferenceValue;var prerequisite=(ObservationAlignmentPuzzle)gs.FindProperty("_prerequisite").objectReferenceValue;
                var reward=UnityEngine.Object.FindObjectsByType<PuzzleClueReward>(FindObjectsInactive.Include).Single(r=>new SerializedObject(r).FindProperty("_puzzle").objectReferenceValue==discovery);var rs=new SerializedObject(reward);var tracker=(ClueTracker)rs.FindProperty("_tracker").objectReferenceValue;var clue=(ClueDefinition)rs.FindProperty("_clue").objectReferenceValue;int clues=0,reveals=0;tracker.ClueDiscovered+=c=>{if(c.Id==clue.Id)clues++;};gate.Revealed+=()=>reveals++;Check(clue.Id=="level01.clue05"&&clue.Title=="La llave"&&clue.SemanticContent==ClueSemanticContent.SecretCommand,"Original semantic clue preserved "+mode);
                Place(new Vector3(0,Floor(0,55.5f),55.5f),0);yield return Frames(5);yield return WalkTo(0,58);if(mode==Tikal.Persistence.CameraMode.ThirdPerson){yield return Frames(18);ScreenCapture.CaptureScreenshot(Folder+"/Zone05_Approach.png");yield return Frames(5);}yield return WalkTo(0,65);yield return WalkTo(-5,65);yield return WalkTo(0,65);yield return WalkTo(0,67.8f);yield return Frames(20);
                Check(!gate.IsRevealed&&!content.activeInHierarchy&&content.GetComponentsInChildren<Renderer>(true).All(r=>!r.gameObject.activeInHierarchy),"All narrative/key/stele visuals hidden before Puzzle04 "+mode);Check(!discovery.CanInteract&&!tracker.HasClue(clue.Id)&&clues==0,"No premature Examine or Clue05 "+mode);Check(!ReferenceEquals(player.GetComponent<PlayerInteraction>().CurrentInteractable,discovery),"No premature discovery prompt "+mode);if(mode==Tikal.Persistence.CameraMode.ThirdPerson){Place(new Vector3(1.6f,Floor(1.6f,67.8f),67.8f),0);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/Zone05_BeforeReveal.png");yield return Frames(5);}
                // Exercise only the existing dependency; no other puzzle or outcome test.
                var obs=new SerializedObject(prerequisite);var point=(Component)obs.FindProperty("_point").objectReferenceValue;var target=(Transform)obs.FindProperty("_target").objectReferenceValue;Place(point.transform.position+Vector3.up*.4f,180);yield return Frames(15);yield return ObservationAim(target);yield return Frames(24);Check(prerequisite.IsSolved&&gate.IsRevealed&&reveals==1&&content.activeInHierarchy&&discovery.CanInteract,"Original Puzzle04 event reveals gated content once "+mode);Check(!tracker.HasClue(clue.Id)&&clues==0,"Reveal alone does not grant Clue05 "+mode);
                Place(new Vector3(mode==Tikal.Persistence.CameraMode.ThirdPerson?1.6f:0,Floor(mode==Tikal.Persistence.CameraMode.ThirdPerson?1.6f:0,67.8f),67.8f),0);yield return Frames(205);if(mode==Tikal.Persistence.CameraMode.ThirdPerson){ScreenCapture.CaptureScreenshot(Folder+"/Zone05_AfterReveal.png");yield return Frames(5);}yield return EchoFocus(discovery,new Vector3(0,0,69.2f));var prompt=UnityEngine.Object.FindAnyObjectByType<InteractionPromptPresenter>();var label=(TMPro.TMP_Text)new SerializedObject(prompt).FindProperty("_label").objectReferenceValue;Check(label.text.Contains("Examinar"),"Native Examine prompt after reveal "+mode);yield return EchoPress();Check(discovery.IsSolved&&tracker.HasClue(clue.Id)&&clues==1,"Native Examine grants Clue05 once "+mode);yield return EchoPress();Check(clues==1&&!discovery.CanInteract,"Repeated Interact cannot duplicate Clue05 "+mode);
                yield return Zone05Bindings(clue);Place(new Vector3(0,Floor(0,69.2f),69.2f),0);yield return Frames(5);yield return WalkTo(3,69.2f);yield return WalkTo(6,69.2f);yield return WalkTo(6,74);yield return WalkTo(6,77.1f);Check(player.GameplayActive&&clues==1,"Zone05 north outlet approach accessible; no final-path traversal "+mode);
            }
            CameraMode(Tikal.Persistence.CameraMode.ThirdPerson);
            foreach(var pose in new[]{("Zone05_Left",new Vector2(-5.8f,66),25f),("Zone05_Right",new Vector2(5.8f,66),335f),("Zone05_Wide",new Vector2(0,60),0f)})
            {Place(new Vector3(pose.Item2.x,Floor(pose.Item2.x,pose.Item2.y),pose.Item2.y),pose.Item3);yield return Frames(24);ScreenCapture.CaptureScreenshot(Folder+"/"+pose.Item1+".png");yield return Frames(5);}
        }
    }
}
