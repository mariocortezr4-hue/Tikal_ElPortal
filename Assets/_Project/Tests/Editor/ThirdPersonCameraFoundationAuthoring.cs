using System;
using System.Linq;
using Tikal.Interaction;
using Tikal.Persistence;
using Tikal.Player;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    public static class ThirdPersonCameraFoundationAuthoring
    {
        public const string ExplorerPath = "Assets/_Project/Prefabs/Player/Explorer.prefab";
        public const string ProfilePath = "Assets/_Project/Settings/Camera/ThirdPersonCameraProfile_Default.asset";
        public static readonly string[] GameplayScenes =
        {
            "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity",
            "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity",
            "Assets/_Project/Scenes/Testing/PlayerMovementTest.unity"
        };

        [MenuItem("Tikal/Authoring/Apply Third Person Camera Foundation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Third person authoring requires Edit Mode.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureFolder("Assets/_Project/Settings/Camera");
                var profile = AssetDatabase.LoadAssetAtPath<ThirdPersonCameraProfile>(ProfilePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<ThirdPersonCameraProfile>();
                    AssetDatabase.CreateAsset(profile, ProfilePath);
                }
                if (!profile.IsValid) throw new InvalidOperationException("Correct the camera profile before authoring.");
                var prefab = PrefabUtility.LoadPrefabContents(ExplorerPath);
                try
                {
                    ConfigurePlayer(prefab.GetComponent<PlayerController>(), profile);
                    PrefabUtility.SaveAsPrefabAsset(prefab, ExplorerPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                foreach (var path in GameplayScenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var players = Components<PlayerController>(scene);
                    if (players.Length != 1) throw new InvalidOperationException(path + " must contain exactly one Explorer.");
                    var player = players[0];
                    // Reuse the scene's existing settings applier when it already targets this Explorer.
                    var own = player.GetComponent<PersistentSettingsApplier>();
                    var existing = Components<PersistentSettingsApplier>(scene).FirstOrDefault(applier => applier != own &&
                        new SerializedObject(applier).FindProperty("_playerLook").objectReferenceValue == player.GetComponent<PlayerLook>());
                    if (existing != null)
                    {
                        Set(player.GetComponent<PlayerCameraController>(), "_settingsApplier", existing);
                        // Remove only the newly inherited default applier. The pre-existing scene
                        // applier and its reticle references remain the single settings source.
                        if (own != null) UnityEngine.Object.DestroyImmediate(own);
                    }
                    else
                    {
                        var reticles = Components<ReticleSettings>(scene);
                        if (own == null) throw new InvalidOperationException("No settings applier for " + path);
                        if (reticles.Length == 1) Set(own, "_reticleSettings", reticles[0]);
                    }
                    ThirdPersonCameraFoundationValidation.ValidateScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_THIRD_PERSON_CAMERA_AUTHORED: Explorer, profile, Tutorial, Level01 and PlayerMovementTest. No cinematic edits.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        internal static void ConfigurePlayer(PlayerController player, ThirdPersonCameraProfile profile)
        {
            if (player == null) throw new InvalidOperationException("Explorer is missing PlayerController.");
            var look = player.GetComponent<PlayerLook>();
            if (look == null || !look.IsConfigured) throw new InvalidOperationException("Preserve/configure the existing PlayerLook first.");
            var cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1) throw new InvalidOperationException("Explorer must already own exactly one camera; authoring will not create or delete cameras.");
            var camera = cameras[0];
            if (!camera.transform.IsChildOf(look.CameraRoot)) throw new InvalidOperationException("Existing camera must be under PlayerLook.CameraRoot.");
            camera.tag = "MainCamera";
            camera.enabled = true;
            if (player.GetComponentsInChildren<AudioListener>(true).Length != 1 || camera.GetComponent<AudioListener>() == null)
                throw new InvalidOperationException("Keep exactly one existing AudioListener on MainCamera.");
            camera.GetComponent<AudioListener>().enabled = true;
            var targets = Child(player.transform, "CameraTargets");
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction == null || player.GetComponents<PlayerInteraction>().Length != 1)
                throw new InvalidOperationException("Preserve exactly one existing PlayerInteraction on Explorer.");
            var hadOrigin = targets.Find("InteractionOrigin") != null;
            var origin = Child(targets, "InteractionOrigin");
            if (!hadOrigin)
                origin.localPosition = Vector3.up * 1.35f;
            // Retain any subsequent Inspector tuning of this logical target and of reach.
            Set(interaction, "_interactionOrigin", origin);
            var pivot = Child(targets, "ThirdPersonPivot");
            pivot.localPosition = Vector3.up * profile.PivotHeight;
            var lookTarget = Child(targets, "LookTarget");
            lookTarget.localPosition = new Vector3(0, profile.PivotHeight, profile.Distance);
            // Preserve CameraRoot's serialized reference, crouch coordinates and pitch behavior.
            var fps = Child(look.CameraRoot, "FirstPersonTarget");
            if (camera.transform.parent != fps) camera.transform.SetParent(fps, false);
            var visual = Child(player.transform, "CharacterVisualRoot");
            var proxy = Child(visual, "ThirdPersonHumanProxy");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/Stone.mat");
            Part(proxy, "Head", PrimitiveType.Sphere, new Vector3(0, 1.60f, 0), new Vector3(.30f, .30f, .30f), material);
            Part(proxy, "Body", PrimitiveType.Capsule, new Vector3(0, 1.10f, 0), new Vector3(.42f, .30f, .26f), material);
            Part(proxy, "LeftArm", PrimitiveType.Capsule, new Vector3(-.29f, 1.03f, 0), new Vector3(.14f, .28f, .14f), material);
            Part(proxy, "RightArm", PrimitiveType.Capsule, new Vector3(.29f, 1.03f, 0), new Vector3(.14f, .28f, .14f), material);
            Part(proxy, "LeftLeg", PrimitiveType.Capsule, new Vector3(-.12f, .40f, 0), new Vector3(.18f, .40f, .18f), material);
            Part(proxy, "RightLeg", PrimitiveType.Capsule, new Vector3(.12f, .40f, 0), new Vector3(.18f, .40f, .18f), material);
            var spawner = player.GetComponent<CharacterVisualSpawner>() ?? player.gameObject.AddComponent<CharacterVisualSpawner>();
            Set(spawner, "_characterVisualRoot", visual);
            var applier = player.GetComponent<PersistentSettingsApplier>() ?? player.gameObject.AddComponent<PersistentSettingsApplier>();
            Set(applier, "_playerLook", look);
            var controller = player.GetComponent<PlayerCameraController>() ?? player.gameObject.AddComponent<PlayerCameraController>();
            Set(controller, "_mainCamera", camera);
            Set(controller, "_firstPersonTarget", fps);
            Set(controller, "_thirdPersonPivot", pivot);
            Set(controller, "_lookTarget", lookTarget);
            Set(controller, "_characterVisualRoot", visual.gameObject);
            Set(controller, "_thirdPersonHumanProxy", proxy.gameObject);
            Set(controller, "_profile", profile);
            Set(controller, "_settingsApplier", applier);
            visual.gameObject.SetActive(false);
            proxy.gameObject.SetActive(false);
        }

        private static Transform Child(Transform parent, string name)
        {
            var matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("Duplicate authored child: " + name);
            if (matches.Length == 1) return matches[0];
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = parent.Find(name);
            if (part == null)
            {
                part = GameObject.CreatePrimitive(type).transform;
                part.name = name;
                part.SetParent(parent, false);
            }
            part.localPosition = position;
            part.localRotation = Quaternion.identity;
            part.localScale = scale;
            foreach (var collider in part.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        internal static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        internal static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.name + " has no field " + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
