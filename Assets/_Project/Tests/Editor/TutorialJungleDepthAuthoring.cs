using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tikal.Tests.Editor
{
    // Final visual layer only. Reuses imported meshes/textures and one local moss material.
    public static class TutorialJungleDepthAuthoring
    {
        public const string VariantFolder = TutorialEnvironmentArtPass.Prefabs + "Depth";
        public const string Protection = TutorialEnvironmentArtPass.Docs + "TutorialJungleDepthProtection.json";
        static Material BankMaterial()
        {
            string path = TutorialEnvironmentArtPass.Art + "Ground/MAT_Tutorial_JungleBanks.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentArtPass.Prefabs + "PFB_ROC_RocaMusgo_02.prefab").GetComponentInChildren<Renderer>().sharedMaterial;
            var material = new Material(source) { name = "MAT_Tutorial_JungleBanks", enableInstancing = true };
            material.SetColor("_BaseColor", new Color(.65f, .85f, .6f));
            foreach (string slot in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" }) if (material.HasProperty(slot)) material.SetTextureScale(slot, new Vector2(8, 8));
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static Transform Child(Transform parent, string name)
        {
            var found = parent.Find(name); if (found != null) return found;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        static void DistantProfile(GameObject go, bool removeUnused)
        {
            var group = go.GetComponentInChildren<LODGroup>(); var original = group.GetLODs();
            var near = original[original.Length - 2].renderers; var far = original[original.Length - 1].renderers;
            group.SetLODs(new[] { new LOD(.95f, near), new LOD(.002f, far) }); group.fadeMode = LODFadeMode.None;
            var retained = near.Concat(far).ToArray();
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!retained.Contains(renderer))
                {
                    if (removeUnused) { var filter = renderer.GetComponent<MeshFilter>(); UnityEngine.Object.DestroyImmediate(renderer); if (filter != null) UnityEngine.Object.DestroyImmediate(filter); }
                    else { renderer.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
                    continue;
                }
                renderer.enabled = true; if (removeUnused) renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (!removeUnused) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            group.RecalculateBounds(); group.size *= .45f; if (!removeUnused) PrefabUtility.RecordPrefabInstancePropertyModifications(group);
        }
        static GameObject Variant(string asset)
        {
            string path = VariantFolder + "/PFB_BG_" + asset + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            if (!AssetDatabase.IsValidFolder(VariantFolder)) AssetDatabase.CreateFolder(TutorialEnvironmentArtPass.Prefabs.TrimEnd('/'), "Depth");
            if (asset == "EarthMound")
            {
                var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                try
                {
                    UnityEngine.Object.DestroyImmediate(mound.GetComponent<Collider>()); mound.name = "PFB_BG_EarthMound";
                    mound.GetComponent<Renderer>().sharedMaterial = BankMaterial();
                    mound.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    return PrefabUtility.SaveAsPrefabAsset(mound, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(mound); }
            }
            var go = PrefabUtility.LoadPrefabContents(TutorialEnvironmentArtPass.Prefabs + "PFB_" + asset + ".prefab");
            try
            {
                var nested = go.GetComponentsInChildren<Transform>(true).Select(t => PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g => g != null).Distinct().ToArray();
                foreach (var root in nested) if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var collider in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                if (go.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException("Visual background source unexpectedly contains scripts: " + asset);
                DistantProfile(go, true); go.name = "PFB_BG_" + asset;
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        static GameObject Place(Transform parent, string name, string asset, Vector3 position, float yaw, float scale)
        {
            var prefab = Variant(asset);
            var t = parent.Find(name);
            if (t != null && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != prefab) { UnityEngine.Object.DestroyImmediate(t.gameObject); t = null; }
            if (t == null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); go.name = name; t = go.transform; }
            t.localPosition = position; t.localRotation = Quaternion.Euler(0, yaw, 0); t.localScale = Vector3.one * scale;
            foreach (var child in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            return t.gameObject;
        }
        static float GroundAt(Transform relief, float x, float z)
        {
            float top = -.04f;
            foreach (Transform mound in relief)
            {
                var p = mound.InverseTransformPoint(new Vector3(x, mound.position.y, z)); float radial = 4 * (p.x * p.x + p.z * p.z);
                if (radial <= 1) top = Mathf.Max(top, mound.position.y + .5f * mound.lossyScale.y * Mathf.Sqrt(1 - radial));
            }
            return top;
        }
        public static void Apply()
        {
            if (AssetDatabase.IsValidFolder(VariantFolder)) foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VariantFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); var local = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var group = local.GetComponentInChildren<LODGroup>();
                    if (group == null)
                    {
                        var renderer = local.GetComponent<Renderer>();
                        if (renderer != null && path.EndsWith("PFB_BG_EarthMound.prefab") && renderer.sharedMaterial != BankMaterial()) { renderer.sharedMaterial = BankMaterial(); PrefabUtility.SaveAsPrefabAsset(local, path); }
                        continue;
                    }
                    float oldSize = group.size; DistantProfile(local, true); if (Mathf.Abs(oldSize - group.size) > .0001f) PrefabUtility.SaveAsPrefabAsset(local, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(local); }
            }
            var environment = TutorialEnvironmentArtPass.Find("Environment"); var art = TutorialEnvironmentArtPass.Find(TutorialEnvironmentArtPass.RootName);
            var forest = Child(art, "BackgroundForest"); var mid = Child(forest, "MidForest"); var far = Child(forest, "FarSilhouettes"); var caps = Child(forest, "Continuation");
            var relief = Child(forest, "Relief"); var clusters = Child(art, "FinalEdgeClusters");
            foreach (var obsolete in caps.Cast<Transform>().Where(t => t.name.StartsWith("Bush_Silhouette_")).ToArray()) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            var random = new System.Random(210623); Func<float, float, float> range = (a, b) => a + (float)random.NextDouble() * (b - a);
            var ground = BankMaterial();
            // Shared built-in sphere mesh, buried below its equator: no exposed underside or new mesh asset.
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                Vector3 position = i < 8 ? new Vector3(side * 21, -1.8f, -10 + (i / 2) * 19 + (side > 0 ? 5 : 0)) : new Vector3(side * 13, -1.8f, i < 10 ? -22 : 70);
                var go = Place(relief, "Relief_Mound_" + i.ToString("D2"), "EarthMound", position, range(-12, 12), 1);
                go.transform.localScale = new Vector3(range(24, 30), range(8, 9.5f), range(25, 30));
                var b = TutorialEnvironmentArtPass.BoundsOf(go);
                if (i < 8) go.transform.position += Vector3.right * (side < 0 ? Mathf.Min(0, -7.8f - b.max.x) : Mathf.Max(0, 7.8f - b.min.x));
                else if (i < 10) go.transform.position += Vector3.forward * Mathf.Min(0, -8 - b.max.z);
                else go.transform.position += Vector3.forward * Mathf.Max(0, 55 - b.min.z);
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) { renderer.sharedMaterial = ground; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
            }
            string[] trees = { "VEG_ArbolTropical_02", "VEG_ArbolTropical_04", "VEG_PalmeraTropical_02" };
            int index = 0;
            var usedTrees = new System.Collections.Generic.Dictionary<string, Transform>();
            for (int side = -1; side <= 1; side += 2)
            {
                foreach (float z in new[] { -4f, 25, 52 }) { string name = "Canopy_Depth_" + index.ToString("D2"); Place(mid, name, trees[index % 3], new Vector3(side * range(16, 20), -.2f, z + range(-2, 2)), range(0, 360), range(.9f, 1.15f)); usedTrees[name] = mid; index++; }
                foreach (float z in new[] { 11f, 47 }) { string name = "Canopy_Depth_" + index.ToString("D2"); Place(far, name, trees[index % 3], new Vector3(side * range(29, 35), -.2f, z + range(-3, 3)), range(0, 360), range(.9f, 1.15f)); usedTrees[name] = far; index++; }
            }
            foreach (float z in new[] { -25f, 72f })
            { string name = "Canopy_Depth_" + index.ToString("D2"); Place(caps, name, "VEG_ArbolTropical_02", new Vector3(range(-2, 2), -.2f, z + range(-3, 3)), range(0, 360), range(.9f, 1.15f)); usedTrees[name] = caps; index++; }
            foreach (var root in new[] { mid, far, caps }) foreach (var obsolete in root.Cast<Transform>().Where(t => t.name.StartsWith("Canopy_Depth_") && (!usedTrees.TryGetValue(t.name, out var expected) || t.parent != expected)).ToArray()) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            foreach (var tree in forest.GetComponentsInChildren<LODGroup>().Select(g => g.transform).ToArray())
            { var p = tree.position; p.y = tree.parent == mid ? GroundAt(relief, p.x, p.z) - .2f : range(-7, -5); tree.position = p; }
            // Existing exit canopy supplies the close layer; shift only its scene LOD profile, never its shared prefab.
            foreach (Transform canopy in art.Find("Background")) DistantProfile(canopy.gameObject, false);
            art.Find("Vegetation/Trees/Tree_00").localScale = Vector3.one * .7630355f;
            art.Find("Vegetation/Trees/Tree_09").localScale = Vector3.one * .83371395f;
            string[] rocks = { "ROC_RocaMusgo_02", "ROC_RocaPlana_01", "ROC_RocaTropical_01" };
            for (int cluster = 0; cluster < 4; cluster++)
            {
                float side = cluster % 2 == 0 ? -1 : 1, z = new[] { 6f, 19, 34, 46 }[cluster];
                for (int member = 0; member < 3; member++)
                {
                    float x = side * range(8, 10.3f), pz = z + range(-1.8f, 1.8f);
                    Place(clusters, "Rock_Cluster_" + cluster + "_" + member, rocks[(cluster + member) % 3], new Vector3(x, GroundAt(relief, x, pz) - .25f, pz), range(0, 360), range(.9f, 1.15f));
                }
            }
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, x = side * range(13, 17), z = -3 + i / 2 * 16 + range(-2, 2);
                Place(clusters, "Bush_Mid_" + i.ToString("D2"), "VEG_ArbustoTropical_03", new Vector3(x, GroundAt(relief, x, z) - .15f, z), range(0, 360), range(2.6f, 3.4f));
            }
            for (int i = 0; i < 7; i++)
            {
                float x = i < 4 ? -18 + i * 12 : -12 + (i - 4) * 13, z = i < 4 ? range(-20, -15) : range(62, 70);
                Place(caps, "Bush_Mass_" + i.ToString("D2"), "VEG_ArbustoTropical_03", new Vector3(x, GroundAt(relief, x, z) - .15f, z), range(0, 360), range(3.2f, 4));
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -5.3f : 5.4f, z = i == 0 ? 3 : 31;
                Place(clusters, "Plant_Foreground_" + i, "VEG_PlantaTropical_02", new Vector3(x, 0, z), range(0, 360), range(.9f, 1.05f));
            }
            Physics.SyncTransforms();
        }
        public static void Validate(Action<bool, string> check)
        {
            var art = TutorialEnvironmentArtPass.Find(TutorialEnvironmentArtPass.RootName); var forest = art.Find("BackgroundForest"); var clusters = art.Find("FinalEdgeClusters");
            check(forest != null && forest.childCount == 4 && forest.GetComponentsInChildren<LODGroup>().Length == 19 && forest.Find("Relief").childCount == 12, "BackgroundForest: 12 trees / seven understory masses / 12 buried mounds / four layers");
            check(clusters != null && clusters.childCount == 22, "Four three-rock clusters, eight mid plants and two foreground accents");
            foreach (var root in new[] { forest, clusters })
            {
                check(root.GetComponentsInChildren<Collider>(true).Length == 0 && root.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Pure visual layer: no collider or gameplay script: " + root.name);
                check(root.GetComponentsInChildren<Renderer>(true).All(r => r.shadowCastingMode == ShadowCastingMode.Off), "New distant/decorative layer casts no new shadows: " + root.name);
                foreach (var group in root.GetComponentsInChildren<LODGroup>()) check(group.GetLODs().Length == 2 && group.GetLODs()[0].screenRelativeTransitionHeight >= .94f && group.GetLODs().All(l => l.renderers.Length > 0 && l.renderers.All(r => r != null)), "Existing LOD1/LOD2 mesh references only: " + group.name);
            }
            foreach (Transform hill in forest.Find("Relief"))
            {
                var b = TutorialEnvironmentArtPass.BoundsOf(hill.gameObject);
                check(b.max.x < -7.7f || b.min.x > 7.7f || b.max.z < -7.9f || b.min.z > 54.9f, "Relief stays beyond playable corridor: " + hill.name);
                check(hill.position.y < 0 && b.min.y < -1 && hill.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3 < 1000, "Buried shared low-detail mound without exposed underside: " + hill.name);
            }
            check(art.Find("Vegetation/Trees/Tree_00").GetComponentInChildren<LODGroup>() != null && art.Find("Vegetation/Trees/Tree_09").GetComponentInChildren<LODGroup>() != null, "Two existing framing trees reused; no extra hero import");
        }
    }
}
