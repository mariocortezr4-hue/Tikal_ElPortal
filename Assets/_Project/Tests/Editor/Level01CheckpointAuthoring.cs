using System;
using System.Linq;
using Tikal.Core;
using Tikal.Player;
using Tikal.Interaction;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class Level01CheckpointAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        internal static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        internal static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        [MenuItem("Tikal/Authoring/Apply Level01 Checkpoints")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Apply checkpoints outside Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                Apply(scene);
                Level01CheckpointValidation.ValidateScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save checkpoint integration.");
                Debug.Log("TIKAL_LEVEL01_CHECKPOINTS_AUTHORED: session-local level01.start; existing puzzles/door preserved.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        internal static void Apply(Scene scene)
        {
            var player = Components<PlayerController>(scene).Single();
            var outcome = Components<LevelOutcomeController>(scene).Single();
            var pause = Components<PauseMenuController>(scene).Single();
            var existing = Components<CheckpointManager>(scene);
            if (existing.Length > 1) throw new InvalidOperationException("Duplicate checkpoint managers.");
            var manager = existing.Length == 1 ? existing[0] : outcome.gameObject.AddComponent<CheckpointManager>();
            var group = manager.transform.Find("Checkpoints");
            if (group == null) { group = new GameObject("Checkpoints").transform; group.SetParent(manager.transform, false); }
            var starts = Components<CheckpointAnchor>(scene).Where(a => a.CheckpointId == "level01.start").ToArray();
            if (starts.Length > 1) throw new InvalidOperationException("Duplicate level01.start anchors.");
            var start = starts.SingleOrDefault();
            if (start == null)
            {
                var marker = new GameObject("level01.start"); marker.transform.SetParent(group, false);
                marker.transform.SetPositionAndRotation(player.transform.position, player.transform.rotation);
                start = marker.AddComponent<CheckpointAnchor>();
                var data = new SerializedObject(start); data.FindProperty("_checkpointId").stringValue = "level01.start"; data.ApplyModifiedPropertiesWithoutUndo();
            }
            var triggers = start.GetComponentsInChildren<CheckpointTrigger>(true);
            if (triggers.Length > 1) throw new InvalidOperationException("Duplicate start triggers.");
            var trigger = triggers.SingleOrDefault();
            if (trigger == null)
            {
                var activation = new GameObject("Activation trigger"); activation.transform.SetParent(start.transform, false);
                activation.transform.localPosition = Vector3.up;
                var box = activation.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(2, 2, 2);
                trigger = activation.AddComponent<CheckpointTrigger>();
            }
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            Set(trigger, "_manager", manager); Set(trigger, "_checkpoint", start);
            Set(manager, "_player", player); Set(manager, "_capsule", player.GetComponent<CharacterController>());
            Set(manager, "_movement", player.GetComponent<PlayerMovement>()); Set(manager, "_look", player.GetComponent<PlayerLook>());
            Set(manager, "_camera", player.GetComponent<PlayerCameraController>()); Set(manager, "_interaction", player.GetComponent<PlayerInteraction>());
            Set(manager, "_outcome", outcome); Set(manager, "_flow", Components<GameFlowController>(scene).Single());
            Set(manager, "_pause", pause); Set(manager, "_journal", Components<ClueJournalPresenter>(scene).Single());
            Set(manager, "_initialCheckpoint", start);
            var canvas = Components<ReticlePresenter>(scene).Single().GetComponentInParent<Canvas>();
            var fadeTransform = canvas.transform.Find("Checkpoint Respawn Fade");
            if (fadeTransform == null)
            {
                var fadeRoot = new GameObject("Checkpoint Respawn Fade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                fadeRoot.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)fadeRoot.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                fadeRoot.GetComponent<Image>().color = Color.black;
                fadeTransform = fadeRoot.transform;
            }
            fadeTransform.SetAsLastSibling();
            var fade = fadeTransform.GetComponent<CanvasGroup>(); fade.alpha = 0; fade.blocksRaycasts = false; fade.interactable = false;
            Set(manager, "_fade", fade); Set(pause, "_checkpointRestart", manager); Set(outcome, "_checkpoints", manager);
            var managerData = new SerializedObject(manager);
            var anchors = Components<CheckpointAnchor>(scene); var anchorsProperty = managerData.FindProperty("_checkpoints");
            anchorsProperty.arraySize = anchors.Length;
            for (int i = 0; i < anchors.Length; i++) anchorsProperty.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
            var resets = Components<PointOfNoReturnTrigger>(scene); var resetsProperty = managerData.FindProperty("_resetTargets");
            resetsProperty.arraySize = resets.Length;
            for (int i = 0; i < resets.Length; i++) resetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = resets[i];
            managerData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
