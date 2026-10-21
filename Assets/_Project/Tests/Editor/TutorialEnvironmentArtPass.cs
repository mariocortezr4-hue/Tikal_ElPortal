using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Tikal.Core;
using Tikal.Player;
using Tikal.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class TutorialEnvironmentArtPass
    {
        public const string ScenePath = "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity";
        public const string Prefabs = "Assets/_Project/Prefabs/Environment/Tutorial/";
        public const string Art = "Assets/_Project/Art/Environment/Tutorial/";
        public const string Docs = "Assets/_Project/Documentation/";
        public const string Evidence = "Temp/TutorialEnvironmentArtPass";
        public const string RootName = "ENV_TutorialArt";
        public const int Seed = 210621;
        [Serializable] public class AssetEntry { public string name, prefab, category; public float[] dimensions; public int[] triangles; }
        [Serializable] public class Selection { public AssetEntry[] assets; }
        [Serializable] public class Signature { public string id, value; }
        [Serializable] public class Baseline { public Signature[] components, transforms; }
        [Serializable] public class Stats
        {
            public bool passed;
            public int instances, trees, bushes, plants, rocks, ruins, props, lodGroups, materials, textures, shadowCasters, artColliders, sceneColliders, largestMesh;
            public string largestMeshName, largestTexture, scope;
            public string[] resolutions, checks;
            public long allLodTriangles, lod0Triangles;
        }
        public static Selection Catalog => JsonUtility.FromJson<Selection>(File.ReadAllText(Docs + "TutorialEnvironmentSelection.json"));
        public static Transform Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Single(t => t.name == name);
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("Tutorial art: " + message); }
        static TutorialEnvironmentArtPass() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/TutorialArtApply.request")) return;
            File.Delete("Temp/TutorialArtApply.request"); Directory.CreateDirectory(Evidence);
            try
            {
                Apply();
                var files = new[] { ScenePath }.Concat(AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs.TrimEnd('/') }).Select(g => AssetDatabase.GUIDToAssetPath(g))).ToArray();
                var before = files.Select(File.ReadAllBytes).ToArray();
                Apply();
                Require(files.Select((p, i) => before[i].SequenceEqual(File.ReadAllBytes(p))).All(v => v), "Authoring twice byte-identical");
                File.WriteAllText(Evidence + "/Apply.txt", "PASS: deterministic placements, scene and prefabs byte-identical after second application");
            }
            catch (Exception e) { File.WriteAllText(Evidence + "/Apply.txt", e.ToString()); Debug.LogException(e); }
        }
        static string Id(UnityEngine.Object obj) => obj == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        static string Properties(Component obj)
        {
            var result = new List<string>(); var iterator = new SerializedObject(obj).GetIterator();
            while (iterator.Next(true))
            {
                // ObjectReference children are transient native addresses, already represented by stable GlobalObjectId.
                if (iterator.propertyPath == "m_TextStyleHashCode") continue; // TMP initializes this non-narrative cache when the UI renders.
                if (iterator.propertyPath.EndsWith(".m_FileID") || iterator.propertyPath.EndsWith(".m_PathID")) continue;
                string value;
                switch (iterator.propertyType)
                {
                    case SerializedPropertyType.ObjectReference: value = Id(iterator.objectReferenceValue); break;
                    case SerializedPropertyType.Generic: continue;
                    case SerializedPropertyType.String: value = iterator.stringValue; break;
                    case SerializedPropertyType.Boolean: value = iterator.boolValue.ToString(); break;
                    case SerializedPropertyType.Float: value = iterator.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                    case SerializedPropertyType.Integer: case SerializedPropertyType.Enum: case SerializedPropertyType.LayerMask: case SerializedPropertyType.ArraySize: value = iterator.longValue.ToString(); break;
                    default: value = iterator.boxedValue?.ToString() ?? "null"; break;
                }
                result.Add(iterator.propertyPath + "=" + value);
            }
            return string.Join("\n", result);
        }
        static Signature CaptureComponent(Component component) => new Signature { id = Id(component), value = Properties(component) };
        static Signature CaptureTransform(Transform t) => new Signature { id = Id(t), value = Id(t.parent) + "|" + t.localPosition.ToString("F6") + "|" + t.localRotation.ToString("F6") + "|" + t.localScale.ToString("F6") + "|" + t.gameObject.activeSelf };
        static Baseline CaptureBaseline() => new Baseline
        {
            components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c => c != null && (c is MonoBehaviour || c is Collider)).Select(CaptureComponent).ToArray(),
            transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.name != "Jungle Sun").Select(CaptureTransform).ToArray()
        };
        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        static GameObject Place(Transform parent, string key, string asset, Vector3 position, float yaw, Vector3 scale, bool isStatic = true)
        {
            var child = parent.Find(key);
            if (child == null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PFB_" + asset + ".prefab"), parent); go.name = key; child = go.transform; }
            child.localPosition = position; child.localRotation = Quaternion.Euler(0, yaw, 0); child.localScale = scale;
            foreach (var t in child.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, isStatic ? StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic : 0);
            return child.gameObject;
        }
        public static Bounds BoundsOf(GameObject go)
        {
            var lod = go.GetComponentInChildren<LODGroup>();
            var renderers = lod != null ? lod.GetLODs()[0].renderers : go.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.First().bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
        }
        static void Mount(string target, string asset, Vector3 size, bool dynamic = false)
        {
            var proxy = Find(target); var original = proxy.GetComponent<Renderer>(); original.enabled = false;
            var child = Place(proxy, "Art_" + asset, asset, Vector3.zero, 0, new Vector3(1 / proxy.lossyScale.x, 1 / proxy.lossyScale.y, 1 / proxy.lossyScale.z), !dynamic);
            var bounds = BoundsOf(child);
            child.transform.localScale = Vector3.Scale(child.transform.localScale, new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z));
            bounds = BoundsOf(child);
            child.transform.position += proxy.GetComponent<Collider>().bounds.center - bounds.center;
        }
        static Material Ground(string name, Color tint, Vector2 tiling)
        {
            string path = Art + "Ground/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Directory.CreateDirectory(Art + "Ground"); AssetDatabase.Refresh();
            var source = AssetDatabase.FindAssets("VALIDATION_ONLY_Ground_Surfaces_Demo_Plane_3x3_0 t:Material", new[] { Art }).Single();
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(source))) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", tint); material.SetTextureScale("_BaseMap", tiling); material.SetTextureScale("_BumpMap", tiling); material.SetTextureScale("_MetallicGlossMap", tiling);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static void PreparePrefabs()
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var go = PrefabUtility.LoadPrefabContents(path); bool changed = false;
                try
                {
                    // Keep diagnostic colliders disabled. Canonical Tutorial colliders remain untouched.
                    foreach (var collider in go.GetComponentsInChildren<Collider>(true)) if (collider.enabled) { collider.enabled = false; changed = true; }
                    bool small = go.name.Contains("Helecho") || go.name.Contains("Hojas") || go.name.Contains("Arbusto") || go.name.Contains("PlantaTropical");
                    foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                        if (small && renderer.shadowCastingMode != ShadowCastingMode.Off) { renderer.shadowCastingMode = ShadowCastingMode.Off; changed = true; }
                    if (changed) PrefabUtility.SaveAsPrefabAsset(go, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(go); }
            }
        }
        [MenuItem("Tikal/Authoring/Apply Tutorial Environment Art Pass")]
        public static void Apply()
        {
            Require(!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode, "Interactive Edit Mode required");
            Require(!EditorSceneManager.GetActiveScene().isDirty, "Save current scene before applying art");
            PreparePrefabs();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var environment = Find("Environment");
            if (!File.Exists(Docs + "TutorialEnvironmentBaseline.json"))
            {
                Require(environment.Find(RootName) == null, "Capture baseline before art");
                File.WriteAllText(Docs + "TutorialEnvironmentBaseline.json", JsonUtility.ToJson(CaptureBaseline(), true));
            }
            var root = Child(environment, RootName); var terrain = Child(root, "Terrain"); var vegetation = Child(root, "Vegetation");
            var trees = Child(vegetation, "Trees"); var bushes = Child(vegetation, "Bushes"); var cover = Child(vegetation, "GroundCover");
            var rocks = Child(root, "Rocks"); var ruins = Child(root, "Ruins"); Child(root, "Props"); var background = Child(root, "Background");
            foreach (var proxy in environment.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Vegetation proxy "))) proxy.enabled = false;
            var random = new System.Random(Seed); Func<float, float, float> range = (a, b) => a + (float)random.NextDouble() * (b - a);
            string[] treeAssets = { "VEG_ArbolTropical_04", "VEG_PalmeraTropical_02", "VEG_ArbolTropical_02" };
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, s = range(.66f, .88f);
                Place(trees, "Tree_" + i.ToString("D2"), treeAssets[i % 3], new Vector3(side * range(9.5f, 12.5f), 0, -3 + i * 3.8f + range(-1, 1)), range(0, 360), Vector3.one * s);
            }
            for (int i = 0; i < 6; i++)
            {
                float s = range(.85f, 1.05f);
                Place(background, "Canopy_" + i.ToString("D2"), treeAssets[(i + 1) % 3], new Vector3(-15 + i * 6 + range(-1, 1), 0, 55 + range(-2, 3)), range(0, 360), Vector3.one * s);
            }
            for (int i = 0; i < 24; i++)
            {
                float s = range(.65f, .95f);
                Place(bushes, "Bush_" + i.ToString("D2"), i % 2 == 0 ? "VEG_ArbustoTropical_02" : "VEG_ArbustoTropical_03", new Vector3((i % 2 == 0 ? -1 : 1) * range(3.8f, 6.2f), 0, -3 + i * 2.25f + range(-.8f, .8f)), range(0, 360), Vector3.one * s);
            }
            for (int i = 0; i < 30; i++)
            {
                float s = range(.5f, .85f);
                Place(cover, "Plant_" + i.ToString("D2"), i % 3 == 0 ? "VEG_PlantaTropical_02" : "VEG_HelechoTropical_02", new Vector3((i % 2 == 0 ? -1 : 1) * range(3, 5.6f), 0, -4 + i * 1.85f + range(-.8f, .8f)), range(0, 360), Vector3.one * s);
            }
            for (int i = 0; i < 12; i++)
                Place(cover, "LeafLitter_" + i.ToString("D2"), "VEG_HojasSecasMonton_01", new Vector3((i % 2 == 0 ? -1 : 1) * range(1.9f, 2.2f), .071f, -1 + i * 4.2f), range(0, 360), Vector3.one * range(.38f, .6f));
            string[] rockAssets = { "ROC_RocaMusgo_02", "ROC_RocaPlana_01", "ROC_RocaTropical_01" };
            for (int i = 0; i < 18; i++)
                Place(rocks, "Rock_" + i.ToString("D2"), rockAssets[i % 3], new Vector3((i % 2 == 0 ? -1 : 1) * range(3.8f, 6), -.025f, -3 + i * 3.1f), range(0, 360), Vector3.one * range(.55f, .95f));
            Place(ruins, "Wall_Jump_Left", "ARQ_MuroMaya_01", new Vector3(-5.7f, -.2f, 12.5f), 73, Vector3.one * .8f);
            Place(ruins, "Wall_Sprint_Right", "ARQ_MuroMaya_01", new Vector3(6.5f, -.4f, 27), 104, Vector3.one * .9f);
            Place(ruins, "Wall_Exit_Left", "ARQ_MuroMaya_01", new Vector3(-5, -.3f, 39), 62, Vector3.one * .75f);
            Mount("Jump root", "ADP_Tutorial_JumpObstacle", new Vector3(3.5f, .6f, .658768f));
            Mount("Low ruin lintel", "ADP_Tutorial_LowLintel_R2", new Vector3(4.2f, .45f, 1.2f));
            Mount("Lift stone", "ADP_Tutorial_LiftStone", new Vector3(3.6f, 2.8f, .7f), true);
            foreach (var name in new[] { "Ruin left", "Ruin right" }) Mount(name, "ARQ_ColumnaMaya_01", new Vector3(.5f, 1.5f, 1.1f));
            foreach (var name in new[] { "Exit marker left", "Exit marker right" }) Mount(name, "ARQ_ColumnaMaya_01", new Vector3(.4f, 2.2f, .4f));
            Find("Jungle floor").GetComponent<Renderer>().sharedMaterial = Ground("MAT_Tutorial_ForestFloor", Color.white, new Vector2(28f / 3, 20));
            Find("Expedition path").GetComponent<Renderer>().sharedMaterial = Ground("MAT_Tutorial_Path", new Color(1.08f, 1.04f, .96f), new Vector2(4.6f / 3, 18));
            Find("Exit path highlight").GetComponent<Renderer>().sharedMaterial = Ground("MAT_Tutorial_ExitPath", new Color(1.1f, 1.07f, 1), new Vector2(3.8f / 3, 11f / 3));
            var extension = terrain.Find("ForestFloor_Extension");
            if (extension == null) { var go = GameObject.CreatePrimitive(PrimitiveType.Plane); go.name = "ForestFloor_Extension"; go.transform.SetParent(terrain, false); UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); extension = go.transform; }
            extension.localPosition = new Vector3(0, -.04f, 23); extension.localScale = new Vector3(8, 1, 11);
            extension.GetComponent<Renderer>().sharedMaterial = Ground("MAT_Tutorial_Extension", Color.white, new Vector2(80f / 3, 110f / 3));
            extension.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var sun = Find("Jungle Sun").GetComponent<Light>(); sun.transform.rotation = Quaternion.Euler(48, -34, 0); sun.color = new Color(1, .91f, .76f); sun.intensity = 1.25f; sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.46f, .57f, .57f); RenderSettings.ambientEquatorColor = new Color(.29f, .39f, .32f); RenderSettings.ambientGroundColor = new Color(.18f, .22f, .16f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 28; RenderSettings.fogEndDistance = 72; RenderSettings.fogColor = new Color(.29f, .38f, .34f);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Tikal/Validation/Validate Tutorial Environment Art Pass")]
        public static void Validate()
        {
            var scene = EditorSceneManager.GetActiveScene(); Require(scene.path == ScenePath, "Open the actual Tutorial scene");
            var checks = new List<string>(); Action<bool, string> check = (v, m) => { Require(v, m); checks.Add(m); };
            var baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Docs + "TutorialEnvironmentBaseline.json"));
            var current = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c => c != null).ToDictionary(Id);
            foreach (var c in baseline.components)
            {
                bool found = current.TryGetValue(c.id, out var original);
                if (found && Properties(original) != c.value)
                {
                    Directory.CreateDirectory(Evidence);
                    File.WriteAllText(Evidence + "/SignatureExpected.txt", c.value);
                    File.WriteAllText(Evidence + "/SignatureActual.txt", Properties(original));
                    File.WriteAllText(Evidence + "/SignatureObject.txt", original.name + " " + original.GetType());
                }
                check(found && Properties(original) == c.value, "Original gameplay component unchanged: " + c.id);
            }
            foreach (var t in baseline.transforms)
            {
                Require(current.TryGetValue(t.id, out var component), "Original transform still exists: " + t.id);
                if(component is RectTransform) continue; // Canvas layout is recomputed by Unity; UI serialized blocks are audited separately.
                check(CaptureTransform((Transform)component).value == t.value, "Original transform/spawn/anchor unchanged: " + t.id);
            }
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
            check(all.All(c => c != null), "No Missing Scripts");
            var sequence = all.OfType<TutorialSequenceController>().Single();
            check(sequence.Steps.Select(s => s.PromptAction).SequenceEqual(new[] { TutorialPromptAction.Move, TutorialPromptAction.Look, TutorialPromptAction.Jump, TutorialPromptAction.Crouch, TutorialPromptAction.Sprint, TutorialPromptAction.Interact }), "Six original ordered steps plus Exit");
            check(sequence.Steps.All(s => s.HasRequiredReferences) && sequence.ExitGate.HasRequiredReferences, "All Tutorial references intact");
            check(all.OfType<Transform>().Count(t => t.name == RootName) == 1, "One environment art root");
            foreach (var t in Find(RootName).GetComponentsInChildren<Transform>(true)) check(t.Cast<Transform>().Select(c => c.name).Distinct().Count() == t.childCount, "Unique placement names: " + t.name);
            check(all.OfType<AudioListener>().Count() == 1, "One original AudioListener");
            check(all.OfType<PlayerCameraController>().Single().IsConfigured, "Shared FPS/TPS controller configured");
            check(GameFlowController.GetIntroCompletionDestination(false) == GameSceneIds.Tutorial && GameFlowController.GetTutorialCompletionDestination(false) == GameSceneIds.Level01, "Intro / Tutorial / Level01 routing preserved");
            var placements = all.OfType<Transform>().Where(t => PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) == t.gameObject && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)).StartsWith(Prefabs)).Select(t => t.gameObject).ToArray();
            var rs = placements.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            var filters = placements.SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).ToArray();
            check(filters.All(f => f.sharedMesh != null && f.sharedMesh.vertexCount > 0), "Every environment mesh exists");
            check(placements.SelectMany(g => g.GetComponentsInChildren<Collider>(true)).All(c => !c.enabled), "No enabled artistic/diagnostic colliders");
            foreach (var group in placements.SelectMany(g => g.GetComponentsInChildren<LODGroup>(true)))
                check(group.GetLODs().Length >= 2 && group.GetLODs().All(l => l.renderers.Length > 0 && l.renderers.All(r => r != null)), "Valid existing LODs: " + group.name);
            var mats = rs.SelectMany(r => r.sharedMaterials).Concat(new[] { Find("Jungle floor").GetComponent<Renderer>().sharedMaterial, Find("Expedition path").GetComponent<Renderer>().sharedMaterial, Find("Exit path highlight").GetComponent<Renderer>().sharedMaterial, Find(RootName).Find("Terrain/ForestFloor_Extension").GetComponent<Renderer>().sharedMaterial }).Distinct().ToArray();
            check(mats.All(m => m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit" && m.shader.isSupported), "All environment materials supported URP/Lit");
            var textures = mats.SelectMany(m => m.GetTexturePropertyNames().Select(n => m.GetTexture(n))).Where(t => t != null).Distinct().ToArray();
            foreach (var tex in textures)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                check(importer != null && importer.mipmapEnabled && Math.Max(tex.width, tex.height) <= (tex.name == "VEG_HelechoTropical_02_Normal" ? 4096 : 2048), "Optimized texture with mipmaps: " + tex.name);
            }
            foreach (var mat in mats.Where(m => m.GetTexture("_BumpMap") != null)) check(((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")))).textureType == TextureImporterType.NormalMap, "Normal import type: " + mat.name);
            check(filters.Max(f => f.sharedMesh.triangles.Length / 3) < 50000, "No high-poly source mesh");
            var largest = filters.OrderByDescending(f => f.sharedMesh.triangles.Length).First(); var largestTex = textures.OrderByDescending(t => t.width * t.height).First();
            var report = new Stats { passed = true, instances = placements.Length, trees = placements.Count(g => g.name.StartsWith("Tree_") || g.name.StartsWith("Canopy_")), bushes = placements.Count(g => g.name.StartsWith("Bush_")), plants = placements.Count(g => g.name.StartsWith("Plant_") || g.name.StartsWith("LeafLitter_")), rocks = placements.Count(g => g.name.StartsWith("Rock_")), ruins = placements.Count(g => g.name.StartsWith("Wall_") || g.name.Contains("Columna") || g.name.Contains("LowLintel")), props = placements.Count(g => g.name.Contains("LiftStone") || g.name.Contains("JumpObstacle")), lodGroups = placements.Sum(g => g.GetComponentsInChildren<LODGroup>(true).Length), materials = mats.Length, textures = textures.Length, shadowCasters = rs.Count(r => r.shadowCastingMode != ShadowCastingMode.Off), artColliders = placements.Sum(g => g.GetComponentsInChildren<Collider>(true).Count(c => c.enabled)), sceneColliders = all.OfType<Collider>().Count(c => c.enabled), largestMesh = largest.sharedMesh.triangles.Length / 3, largestMeshName = largest.sharedMesh.name, largestTexture = largestTex.name + " " + largestTex.width + "x" + largestTex.height, allLodTriangles = filters.Sum(f => (long)f.sharedMesh.triangles.Length / 3), lod0Triangles = placements.Sum(g => { var group = g.GetComponentInChildren<LODGroup>(); return group != null ? group.GetLODs()[0].renderers.Sum(r => (long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3) : g.GetComponentsInChildren<MeshFilter>().Sum(f => (long)f.sharedMesh.triangles.Length / 3); }), resolutions = textures.Select(t => t.name + ":" + t.width + "x" + t.height).ToArray(), checks = checks.ToArray(), scope = "Editor integrity and scene geometry; visibility and frame timings are measured separately in real Play Mode." };
            Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + "/EditorValidation.json", JsonUtility.ToJson(report, true)); Debug.Log("TUTORIAL_ART_EDITOR_PASS " + report.instances);
        }
    }
}
