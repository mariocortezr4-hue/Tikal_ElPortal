using System;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class SecretEntranceAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        internal static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("Tikal/Authoring/Apply Level01 Secret Entrance")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Apply Secret Entrance outside Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                Apply(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Secret Entrance integration.");
                Debug.Log("TIKAL_SECRET_ENTRANCE_AUTHORED: closed Door + nearby player; camera independent.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        internal static void Apply(Scene scene)
        {
            var door = Components<Door>(scene).Single();
            var reader = Components<PlayerInputReader>(scene).Single();
            var outcome = Components<LevelOutcomeController>(scene).Single();
            var receivers = Components<SecretEntranceCommandReceiver>(scene);
            if (receivers.Length > 1) throw new InvalidOperationException("Duplicate secret receivers must be resolved before authoring.");
            var receiver = receivers.Length == 1 ? receivers[0] : outcome.gameObject.AddComponent<SecretEntranceCommandReceiver>();
            var serialized = new SerializedObject(receiver);
            var target = (Transform)serialized.FindProperty("_target").objectReferenceValue;
            if (target == null)
            {
                var targetObject = new GameObject("SecretCommandTarget");
                targetObject.transform.SetParent(door.transform.parent, false);
                targetObject.transform.position = door.transform.position + Vector3.up * 1.6f;
                target = targetObject.transform;
            }
            if (target.IsChildOf(door.transform)) throw new InvalidOperationException("Secret target must not move with Door.");
            var zone = (BoxCollider)serialized.FindProperty("_zone").objectReferenceValue;
            if (zone == null)
            {
                var zoneObject = new GameObject("SecretCommandZone");
                zoneObject.transform.SetParent(target, false);
                zoneObject.transform.localPosition = new Vector3(0, -.1f, -.05f);
                zone = zoneObject.AddComponent<BoxCollider>();
                zone.size = new Vector3(3.4f, 3, 5.8f);
            }
            zone.isTrigger = true;
            zone.enabled = true;
            if (zone.transform.IsChildOf(door.transform)) throw new InvalidOperationException("Secret zone must not move with Door.");
            serialized.FindProperty("_door").objectReferenceValue = door;
            serialized.FindProperty("_inputReader").objectReferenceValue = reader;
            serialized.FindProperty("_player").objectReferenceValue = reader.transform;
            serialized.FindProperty("_outcome").objectReferenceValue = outcome;
            serialized.FindProperty("_target").objectReferenceValue = target;
            serialized.FindProperty("_zone").objectReferenceValue = zone;
            serialized.FindProperty("_range").floatValue = 3;
            serialized.FindProperty("_movingLeaf").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            receiver.enabled = true;
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
