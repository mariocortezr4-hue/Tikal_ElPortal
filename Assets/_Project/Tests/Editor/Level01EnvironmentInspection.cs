using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Level01EnvironmentInspection
    {
        public const string ScenePath = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        public const string Evidence = "Temp/Level01EnvironmentArtPass";
        [Serializable] public class Item
        {
            public string path, id, name, components, prefab, material, json, mesh;
            public int triangles;
            public Vector3 position, scale, size, center;
            public bool active, rendererEnabled;
        }
        [Serializable] public class Dump { public Item[] objects; }
        static Level01EnvironmentInspection() { EditorApplication.update += Poll; }
        public static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        static void Poll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Temp/Level01Inspect.request")) return;
            File.Delete("Temp/Level01Inspect.request");
            try { Inspect(); } catch (Exception e) { Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "/InspectionError.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Tikal/Validation/Inspect Level01 Environment")]
        public static void Inspect()
        {
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save current scene before inspection");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var report = new Dump { objects = transforms.Select(t =>
            {
                var r = t.GetComponent<Renderer>(); var c = t.GetComponent<Collider>(); var f=t.GetComponent<MeshFilter>();var b = r != null ? r.bounds : c != null ? c.bounds : new Bounds(t.position, Vector3.zero);
                return new Item { path = PathOf(t), id = GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(), name = t.name, position = t.position, scale = t.lossyScale, size = b.size, center = b.center, active = t.gameObject.activeSelf,
                    components = string.Join(",", t.GetComponents<Component>().Select(x => x == null ? "MISSING" : x.GetType().Name)), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject), material = r != null && r.sharedMaterial != null ? AssetDatabase.GetAssetPath(r.sharedMaterial) : "", rendererEnabled = r != null && r.enabled,mesh=f!=null&&f.sharedMesh!=null?AssetDatabase.GetAssetPath(f.sharedMesh):"",triangles=f!=null&&f.sharedMesh!=null?f.sharedMesh.triangles.Length/3:0,
                    json = string.Join("\n", t.GetComponents<MonoBehaviour>().Where(x => x != null).Select(x => x.GetType().FullName + "\n" + EditorJsonUtility.ToJson(x, true))) };
            }).ToArray() };
            Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "/Inspection.json", JsonUtility.ToJson(report, true)); Debug.Log("LEVEL01_ENVIRONMENT_INSPECTED " + transforms.Length);
        }
    }
}
