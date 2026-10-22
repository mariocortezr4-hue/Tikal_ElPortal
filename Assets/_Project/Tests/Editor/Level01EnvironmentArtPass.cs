using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tikal.Core;
using Tikal.Puzzles;
using Tikal.Puzzles.Level01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Level01EnvironmentArtPass
    {
        public const string ScenePath = Level01EnvironmentInspection.ScenePath;
        public const string Evidence = Level01EnvironmentInspection.Evidence;
        public const string Docs = "Assets/_Project/Documentation/";
        public const string Prefabs = "Assets/_Project/Prefabs/Environment/Level01/";
        const string Tutorial = "Assets/_Project/Prefabs/Environment/Tutorial/";
        const string RootName = "ENV_Level01Art";
        const string Protection = Docs + "Level01EnvironmentProtection.json";
        [Serializable] public class SourceAsset { public string name, prefab, category; }
        [Serializable] public class SourceFile { public string source, destination, sha256, metaSHA256, metaContents; }
        [Serializable] public class Sources { public SourceAsset[] assets; public SourceFile[] files; }
        [Serializable] public class Entry { public string id, value; }
        [Serializable] public class Baseline { public Entry[] components, transforms, activeObjects, puzzleRenderers; }
        [Serializable] public class Stats
        {
            public bool passed; public int checks, instances, lodGroups, shadowCasters, sceneColliders, newArtColliders, materials, textures, largestMesh, largestTexture;
            public string largestMeshName, largestTextureName; public string[] assertions;
        }
        static List<Bounds> reserves;
        static Transform art;
        static Sources Manifest => JsonUtility.FromJson<Sources>(File.ReadAllText(Docs + "Level01EnvironmentSources.json"));
        static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException("Level01 art: " + message); }
        static Level01EnvironmentArtPass() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/Level01ArtApply.request")) return;
            File.Delete("Temp/Level01ArtApply.request"); Directory.CreateDirectory(Evidence);
            try
            {
                Apply();
                var paths = new[] { ScenePath }.Concat(AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath)).ToArray();
                var first = paths.Select(File.ReadAllBytes).ToArray(); Apply();
                var changed = paths.Where((p, i) => !first[i].SequenceEqual(File.ReadAllBytes(p))).ToArray();
                Require(changed.Length == 0, "Authoring twice byte-identical: " + string.Join(",", changed));
                File.WriteAllText(Evidence + "/Apply.txt", "PASS: scene and all Level01 prefabs byte-identical after second application");
            }
            catch (Exception e) { File.WriteAllText(Evidence + "/Apply.txt", e.ToString()); Debug.LogException(e); }
        }
        public static Component[] All(Scene scene) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
        static string Id(UnityEngine.Object obj) => obj == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        static string Properties(Component component) => (string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { component });
        static Entry ComponentEntry(Component c) => new Entry { id = Id(c), value = Properties(c) };
        static Entry TransformEntry(Transform t) => new Entry { id = Id(t), value = Id(t.parent) + "|" + t.localPosition.ToString("F6") + "|" + t.localRotation.ToString("F6") + "|" + t.localScale.ToString("F6") };
        static Entry ActiveEntry(Transform t) => new Entry { id = Id(t), value = t.gameObject.activeSelf.ToString() };
        static bool ReservedHierarchy(Transform t) => Level01EnvironmentInspection.PathOf(t).Contains("/PuzzleAnchor/") || Level01EnvironmentInspection.PathOf(t).Contains("/RevelationAnchor/") || Level01EnvironmentInspection.PathOf(t).Contains("/FinalWarningSteleAnchor/");
        static void CaptureProtection(Scene scene)
        {
            if (File.Exists(Protection)) return;
            var all = All(scene);
            File.WriteAllText(Protection, JsonUtility.ToJson(new Baseline
            {
                components = all.Where(c => c is MonoBehaviour || c is Collider).Select(ComponentEntry).ToArray(),
                transforms = all.OfType<Transform>().Where(t => !(t is RectTransform)).Select(TransformEntry).ToArray(),
                activeObjects = all.OfType<Transform>().Select(ActiveEntry).ToArray(),
                puzzleRenderers = all.OfType<Renderer>().Where(r => ReservedHierarchy(r.transform)).Select(ComponentEntry).ToArray()
            }, true));
        }
        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        static Bounds BoundsOf(GameObject go)
        {
            var group = go.GetComponentInChildren<LODGroup>(); var rs = group != null ? group.GetLODs()[0].renderers : go.GetComponentsInChildren<Renderer>(true);
            Require(rs.Length > 0, "Visual has renderers " + go.name); var bounds = rs[0].bounds;
            foreach (var r in rs.Skip(1)) bounds.Encapsulate(r.bounds); return bounds;
        }
        public static string AssetPath(string asset)
        {
            string local = Prefabs + "PFB_" + asset + ".prefab";
            return File.Exists(local) ? local : Tutorial + "PFB_" + asset + ".prefab";
        }
        static void PrepareAssets()
        {
            // Imports only selected manifest payload. Never writes to a source kit or replaces an existing copy.
            foreach (var file in Manifest.files)
            {
                if (File.Exists(file.destination)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(file.destination)); File.Copy(file.source, file.destination);
                if(!string.IsNullOrEmpty(file.metaContents)) File.WriteAllText(file.destination + ".meta", file.metaContents);
                else File.Copy(file.source + ".meta", file.destination + ".meta");
            }
            AssetDatabase.Refresh();
            foreach (var entry in Manifest.assets)
            {
                var go = PrefabUtility.LoadPrefabContents(entry.prefab); bool dirty = false;
                try
                {
                    Require(go.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Selected source is purely visual " + entry.name);
                    foreach (var collider in go.GetComponentsInChildren<Collider>(true)) { UnityEngine.Object.DestroyImmediate(collider); dirty = true; }
                    if (dirty) PrefabUtility.SaveAsPrefabAsset(go, entry.prefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(go); }
            }
        }
        static float GroundAt(float x, float z)
        {
            // Original collision shell is the authority. No new floor or collider is introduced.
            return Physics.RaycastAll(new Vector3(x, 45, z), Vector3.down, 80, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider is MeshCollider && (h.collider.transform.parent.name == "Terrain" || h.collider.transform.parent.name == "NaturalBoundaries"))
                .Select(h => h.point.y).DefaultIfEmpty(0).Max();
        }
        static void Reservations()
        {
            reserves = new List<Bounds> {
                new Bounds(new Vector3(-6, 3, 14.5f),new Vector3(7,7,8)),
                new Bounds(new Vector3(31.75f, 3.5f, 10),new Vector3(14,8,16)),
                new Bounds(new Vector3(33, 3, 40),new Vector3(10,7,14)),
                new Bounds(new Vector3(-5, 4.5f, 35.5f),new Vector3(7,9,13)),
                new Bounds(new Vector3(0, 3, 70),new Vector3(7,7,11)),
                new Bounds(new Vector3(0, 2, -.5f),new Vector3(4.2f,6,7)) };
        }
        static GameObject Place(Transform parent, string name, string asset, Vector3 position, float yaw, float scale, bool distant = false, bool protect = true)
        {
            var t = parent.Find(name);
            var sourcePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(asset));
            if(t!=null && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)!=sourcePrefab){UnityEngine.Object.DestroyImmediate(t.gameObject);t=null;}
            if (t == null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, parent); go.name = name; t = go.transform; }
            t.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); t.localScale = Vector3.one * scale;
            foreach (var collider in t.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            if (protect && reserves.Any(b => b.Intersects(BoundsOf(t.gameObject)))) { UnityEngine.Object.DestroyImmediate(t.gameObject); return null; }
            foreach (var renderer in t.GetComponentsInChildren<Renderer>(true))
            {
                if (distant || asset.StartsWith("VEG_Arbusto") || asset.StartsWith("VEG_Helecho") || asset.StartsWith("VEG_Planta") || asset.Contains("Hojas")) renderer.shadowCastingMode = ShadowCastingMode.Off;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (var child in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            var group = t.GetComponentInChildren<LODGroup>();
            if (distant && group != null)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(asset)).GetComponentInChildren<LODGroup>(); var original = source.GetLODs();
                float cull = asset.StartsWith("ROC_") || asset.StartsWith("VEG_Arbusto") || asset.StartsWith("VEG_Helecho") || asset.StartsWith("VEG_Planta") || asset.Contains("Hojas") ? .03f : .006f;
                var lods = original.Skip(Math.Max(0, original.Length - 2)).Select((l, i) => new LOD(i == 0 ? .6f : cull, l.renderers.Select(r => t.Find(AnimationUtility.CalculateTransformPath(r.transform, source.transform)).GetComponent<Renderer>()).ToArray())).ToArray();
                group.SetLODs(lods); group.fadeMode = LODFadeMode.None; group.RecalculateBounds();
                var active = lods.SelectMany(l => l.renderers).ToArray();
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { r.enabled = active.Contains(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
                PrefabUtility.RecordPrefabInstancePropertyModifications(group);
            }
            return t.gameObject;
        }
        static Material MaterialOf(string asset) => AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(asset)).GetComponentInChildren<Renderer>().sharedMaterial;
        static void BoundaryUnderstory(Transform env)
        {
            var root=Child(art.Find("Vegetation"),"BoundaryUnderstory");var random=new System.Random(210625);int index=0;
            var rockRoot=Child(art,"Rocks");
            foreach(var obsolete in rockRoot.Cast<Transform>().Where(t=>t.name.StartsWith("Rock_")).ToArray()) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            foreach(var obsolete in root.Cast<Transform>().Where(t=>t.name.StartsWith("ToeRock_")).ToArray()) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            string[] clusterAssets={"ROC_RocaMusgo_02","ROC_RocaPlana_01","ROC_RocaTropical_01","ROC_RocaFragmentada_01"};
            var natural=env.Find("NaturalBoundaries");
            foreach(var center in new[]{new Vector2(0,-8),new Vector2(0,12),new Vector2(29,12),new Vector2(29,39),new Vector2(0,39),new Vector2(0,67)})
            {
                float ground=Physics.RaycastAll(new Vector3(center.x,20,center.y),Vector3.down,40,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.parent.name=="Terrain").Select(h=>h.point.y).DefaultIfEmpty(0).Max();
                for(int direction=0;direction<8;direction++)
                {
                    var ray=Quaternion.Euler(0,direction*45,0)*Vector3.forward;
                    var hits=Physics.RaycastAll(new Vector3(center.x,ground+1.25f,center.y),ray,18,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(natural)||h.collider.name.Contains("Boundary")).OrderBy(h=>h.distance).ToArray();
                    if(hits.Length==0||hits[0].distance<6)continue;
                    var hit=hits[0];var point=hit.point-ray*.6f;
                    point.y=Physics.RaycastAll(new Vector3(point.x,20,point.z),Vector3.down,40,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.parent.name=="Terrain").Select(h=>h.point.y+.02f).DefaultIfEmpty(hit.point.y-.8f).Max();
                    Place(root,"ToeBush_"+index,"VEG_ArbustoTropical_03",point,(float)random.NextDouble()*360,.9f,true);
                    if(index%2==0)for(int member=0;member<3;member++)
                    {
                        var tangent=Vector3.Cross(Vector3.up,ray);var rockPoint=point+ray*.55f+tangent*(member-1)*.65f-Vector3.up*.35f;
                        Place(rockRoot,"BoundaryCluster_"+index+"_"+member,clusterAssets[(index+member)%4],rockPoint,25+index*31+member*49,.9f+member*.07f,true);
                    }
                    index++;
                }
            }
            // The approved approach baffle retains its exact BoxCollider; replace its slab presentation only.
            env.Find("SpawnArea/ApproachBaffle").GetComponent<Renderer>().enabled=false;
            env.Find("NaturalBoundaries/ApproachRockCut").GetComponent<Renderer>().enabled=false;
            var framing=Child(art,"EntryRockCut");string[] rocks={"ROC_RocaMusgo_02","ROC_RocaTropical_01","ROC_RocaMusgo_01","ROC_RocaFragmentada_01"};
            for(int i=0;i<4;i++)
            {
                float x=-3.3f+i*3.3f,z=-6+(i%2==0?-.15f:.2f);
                var go=Place(framing,"BaffleRock_"+i,rocks[i],new Vector3(x,0,z),20+i*73,1,false);
                if(go!=null){var b=BoundsOf(go);go.transform.localScale*=((2.15f+i*.12f)/b.size.y);b=BoundsOf(go);go.transform.position+=Vector3.up*(-.15f-b.min.y);}
                Place(framing,"BaffleBush_"+i,"VEG_ArbustoTropical_03",new Vector3(x,-.1f,z-.45f),i*57,1.05f,true);
            }
            Place(framing,"PlazaShoulderRock","ROC_RocaPlana_01",new Vector3(9,.3f,15.5f),20,1.1f,true);
        }
        static void ExistingPresentation(Transform env)
        {
            var functional=Level01SurfacePresentation.GameplayRenderers(All(env.gameObject.scene));
            var soil = AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art + "Ground/MAT_Tutorial_ForestFloor.mat");
            const string bankPath="Assets/_Project/Art/Environment/Level01/MAT_Level01_JungleBanks.mat";
            var banks = AssetDatabase.LoadAssetAtPath<Material>(bankPath);
            if(banks==null){banks=new Material(AssetDatabase.LoadAssetAtPath<Material>(TutorialEnvironmentArtPass.Art + "Ground/MAT_Tutorial_JungleBanks.mat")){name="MAT_Level01_JungleBanks"};banks.SetFloat("_Cull",0);AssetDatabase.CreateAsset(banks,bankPath);}
            foreach (var r in env.Find("Terrain").GetComponentsInChildren<Renderer>()) r.sharedMaterial = soil;
            foreach (var r in env.Find("NaturalBoundaries").GetComponentsInChildren<Renderer>()) r.sharedMaterial = banks;
            env.Find("NaturalBoundaries/EchoRoofRockCover").GetComponent<Renderer>().enabled=false;
            foreach (var r in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>()) r.enabled = false;
            var markers = env.parent.Find("BlockoutMarkers"); foreach (var r in markers.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (functional.Contains(r) || ReservedHierarchy(r.transform) || r.transform.IsChildOf(art) || r.transform.IsChildOf(env.Find("Terrain")) || r.transform.IsChildOf(env.Find("NaturalBoundaries")) || r.transform.IsChildOf(env.Find("Vegetation_Blockout"))) continue;
                if (r.name.Contains("FloorBand") || r.name.Contains("LocalRoute")) { r.enabled = false; continue; }
                if (r.name.Contains("Boundary")) continue;
                if (r.sharedMaterial != null && AssetDatabase.GetAssetPath(r.sharedMaterial).StartsWith("Assets/_Project/Art/Materials/Level01/")) r.sharedMaterial = MaterialOf("ARQ_MuroMaya_01");
            }
        }
        [MenuItem("Tikal/Authoring/Apply Level01 Environment Art Pass")]
        public static void Apply()
        {
            Require(!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode, "Normal interactive Edit Mode required");
            Require(!EditorSceneManager.GetActiveScene().isDirty, "Save active scene before authoring");
            PrepareAssets(); var scene = EditorSceneManager.OpenScene(ScenePath); CaptureProtection(scene);
            var env = scene.GetRootGameObjects().Single().transform.Find("Environment"); art = Child(env, RootName);
            var vegetation = Child(art, "Vegetation"); var trees = Child(vegetation, "Trees"); var bushes = Child(vegetation, "Bushes"); var cover = Child(vegetation, "GroundCover");
            var rocks = Child(art, "Rocks"); var ruins = Child(art, "Ruins"); Child(art, "Props"); var background = Child(art, "BackgroundForest"); Child(art, "GameplayBoundaries");
            Reservations(); Physics.SyncTransforms(); var random = new System.Random(210624); Func<float, float, float> range = (a, b) => a + (float)random.NextDouble() * (b - a);
            string[] treeAssets = { "VEG_ArbolTropical_02", "VEG_ArbolTropical_03", "VEG_CeibaJoven_01", "VEG_ArbolTropical_04", "VEG_PalmeraTropical_02" };
            int n = 0;
            foreach (var proxy in env.Find("Vegetation_Blockout").GetComponentsInChildren<Renderer>())
            {
                var b = proxy.bounds; float x = b.center.x, z = b.center.z, y = GroundAt(x, z);
                if (proxy.name.Contains("Dense"))
                {
                    Place(trees, "Canopy_" + n.ToString("D3"), treeAssets[n % treeAssets.Length], new Vector3(x,y-4f,z), range(0,360), range(.9f,1.12f), true);
                    Place(bushes, "Understory_" + n.ToString("D3"), "VEG_ArbustoTropical_03", new Vector3(x+.6f,y-.15f,z+.8f), range(0,360), range(1.2f,1.6f), true);
                }
                else Place(bushes, "Bush_" + n.ToString("D3"), "VEG_ArbustoTropical_02", new Vector3(x,y-.1f,z), range(0,360), range(.9f,1.3f), true);
                Place(cover, "Plant_" + n.ToString("D3"), n % 2 == 0 ? "VEG_HelechoTropical_02" : "VEG_PlantaTropical_02", new Vector3(x+range(-1,1),y-.05f,z+range(-1,1)), range(0,360), range(.9f,1.15f), true);
                n++;
            }
            string[] stoneAssets = { "ROC_RocaMusgo_02", "ROC_RocaPlana_01", "ROC_RocaTropical_01", "ROC_RocaFragmentada_01" };
            // Perimeter silhouette, using actual extent of the original map; no ring of identical trees.
            n=0; foreach(var position in new[]{new Vector3(-20,0,-18),new Vector3(15,0,-22),new Vector3(-25,0,10),new Vector3(49,0,4),new Vector3(50,0,30),new Vector3(-26,0,40),new Vector3(49,0,51),new Vector3(-25,0,67),new Vector3(23,0,79),new Vector3(-8,0,94)})
                Place(background,"Silhouette_"+n.ToString("D2"),treeAssets[(n++)%treeAssets.Length],position+new Vector3(range(-2,2),range(-2,0),range(-2,2)),range(0,360),range(.95f,1.15f),true);
            // Architectural accents follow existing landmarks; no new puzzle authority or interaction collider.
            var accents = new[] { ("ARQ_ArcoMaya_01",new Vector3(0,0,-.9f),0f,1.27f), ("ARQ_ColumnaDerrumbada_01",new Vector3(5.8f,.4f,8.2f),25f,1f), ("ARQ_EstelaFragmentada_01",new Vector3(5,.4f,17),15f,.8f), ("ARQ_EscombrosMaya_01",new Vector3(7,.4f,20),65f,1f), ("ARQ_BloqueMaya_02",new Vector3(-9.4f,GroundAt(-9.4f,6)-.2f,6),25f,1f), ("ARQ_EscalinataMaya_01",new Vector3(9,.1f,23),90f,.9f), ("ARQ_PlataformaElevada_01",new Vector3(29,4.8f,39),0f,1f), ("ARQ_ColumnaDerrumbada_01",new Vector3(22,.3f,43),80f,1f), ("ARQ_EscombrosMaya_01",new Vector3(37,.3f,45),30f,1f), ("ARQ_PisoPiedraMaya_01",new Vector3(7,1.42f,41),0f,1f), ("ARQ_ColumnaMaya_01",new Vector3(9,1.6f,43),0f,1.1f), ("ARQ_ColumnaDerrumbada_01",new Vector3(-8,.6f,73),25f,1f), ("ARQ_EscombrosMaya_01",new Vector3(9,.6f,75),50f,.9f) };
            n=0; foreach(var item in accents) Place(ruins,"Ruin_"+(n++).ToString("D2"),item.Item1,item.Item2,item.Item3,item.Item4,false,item.Item1!="ARQ_ArcoMaya_01" && item.Item1!="ARQ_PlataformaElevada_01");
            n=0; foreach(var point in new[]{new Vector2(-2.5f,-10),new Vector2(4,8),new Vector2(26,15),new Vector2(26,35),new Vector2(4,40),new Vector2(-4,65)})
                Place(cover,"LeafLitter_"+(n++),"VEG_HojasSecasMonton_01",new Vector3(point.x,GroundAt(point.x,point.y)+.025f,point.y),range(0,360),range(.9f,1.1f),true);
            foreach(var obsolete in art.Find("Props").Cast<Transform>().Where(t=>t.name.StartsWith("FallenLog_")).ToArray())UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            var finalPath=env.Find("Zone05_Revelation/FalsePath"); var finalArt=Child(finalPath,"ENV_FinalPathArt");
            for(int i=0;i<8;i++)
            {
                float x=i%2==0?2.9f:9.1f,z=80+i/2*4;
                Place(finalArt,"FinalBush_"+i,"VEG_ArbustoTropical_03",new Vector3(x,3.3f,z),range(0,360),range(.95f,1.15f),true,false);
                Place(finalArt,"FinalRock_"+i,stoneAssets[i%stoneAssets.Length],new Vector3(x,.4f,z+.6f),range(0,360),range(.9f,1.1f),true,false);
            }
            Place(finalArt,"FinalCanopy_Left","VEG_ArbolTropical_02",new Vector3(-1,0,92),30,.9f,true,false);
            Place(finalArt,"FinalCanopy_Right","VEG_PalmeraTropical_02",new Vector3(13,0,98),160,.95f,true,false);
            ExistingPresentation(env);
            BoundaryUnderstory(env);
            Level01SurfacePresentation.PreserveFunctionalMaterials(All(scene));
            Level01SurfacePresentation.Apply(All(scene));
            foreach(var renderer in All(scene).OfType<Renderer>()) if(PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.40f,.48f,.46f);RenderSettings.ambientEquatorColor=new Color(.24f,.32f,.26f);RenderSettings.ambientGroundColor=new Color(.16f,.19f,.14f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=140;RenderSettings.fogColor=new Color(.28f,.37f,.33f);
            var sun=All(scene).OfType<Light>().Single(l=>l.type==LightType.Directional);sun.color=new Color(.98f,.92f,.8f);sun.intensity=1.15f;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Tikal/Validation/Validate Level01 Environment Art Pass")]
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene(); Require(scene.path==ScenePath,"Open actual Level01");var all=All(scene); var assertions=new List<string>();Action<bool,string> check=(valid,message)=>{Require(valid,message);assertions.Add(message);};
            check(all.All(c=>c!=null),"No Missing Scripts");var current=all.ToDictionary(Id);var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(Protection));
            foreach(var entry in baseline.components)check(current.TryGetValue(entry.id,out var c)&&Properties(c)==entry.value,"Original gameplay/collider unchanged: "+entry.id);
            foreach(var entry in baseline.transforms)check(current.TryGetValue(entry.id,out var c)&&TransformEntry((Transform)c).value==entry.value,"Original transform unchanged: "+entry.id);
            foreach(var entry in baseline.activeObjects)check(current.TryGetValue(entry.id,out var c)&&ActiveEntry((Transform)c).value==entry.value,"Original activation unchanged: "+entry.id);
            foreach(var entry in baseline.puzzleRenderers)check(current.TryGetValue(entry.id,out var c)&&Properties(c)==entry.value,"Reserved puzzle visual unchanged: "+entry.id);
            SecretEntranceValidation.ValidateScene(scene);Level01CheckpointValidation.ValidateScene(scene);
            Level01SurfacePresentation.Validate(all,check);
            foreach(string id in new[]{CharacterIds.Default,CharacterIds.Character02,CharacterIds.Character03}) check(CharacterCatalog.TryGet(id,out var character)&&character.VisualPrefab!=null&&!character.IsLocked,"Canonical selectable character "+id);
            check(all.OfType<PuzzleBase>().Count()==5,"Exactly five original puzzles");check(all.OfType<ObservationAlignmentPuzzle>().Single().MinimumDot==.97f,"Observation dot unchanged");
            var root=all.OfType<Transform>().Single(t=>t.name==RootName);var final=all.OfType<Transform>().Single(t=>t.name=="ENV_FinalPathArt");
            var artistic=root.GetComponentsInChildren<Component>(true).Concat(final.GetComponentsInChildren<Component>(true)).ToArray();
            check(artistic.OfType<Collider>().Count()==0,"No new art colliders, including background");check(artistic.OfType<MonoBehaviour>().Count()==0,"No gameplay in art roots");
            check(root.Find("BackgroundForest").childCount==10,"Ten background silhouettes");check(root.Find("Vegetation/Trees").childCount>15,"Composed tree layer");
            var renderers=all.OfType<Renderer>().ToArray();var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();var textures=materials.SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture)).OfType<Texture2D>().Distinct().ToArray();
            foreach(var r in renderers)check(r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported),"Supported assigned material "+r.name);
            foreach(var group in artistic.OfType<LODGroup>())
            {
                string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(group.gameObject);bool single=source.Contains("ROC_RocaMusgo_01")||source.Contains("ARQ_EscombrosMaya_01");
                check((group.GetLODs().Length>=2||single)&&group.GetLODs().All(l=>l.renderers.Length>0&&l.renderers.All(r=>r!=null)),"Valid LOD or documented single-mesh near prop: "+group.name);
            }
            var meshes=artistic.OfType<MeshFilter>().Select(f=>f.sharedMesh).Where(m=>m!=null).Distinct().ToArray();var largest=meshes.OrderByDescending(m=>m.triangles.Length).First();check(largest.triangles.Length/3<50000,"No high-poly source mesh");
            var observation=all.OfType<ObservationAlignmentPuzzle>().Single();var so=new SerializedObject(observation);var point=(Component)so.FindProperty("_point").objectReferenceValue;var fragments=so.FindProperty("_fragments");
            foreach(var r in artistic.OfType<Renderer>())for(int i=0;i<fragments.arraySize;i++)
            { var target=(Renderer)fragments.GetArrayElementAtIndex(i).objectReferenceValue; var origin=point.transform.position+Vector3.up*1.9f; var delta=target.bounds.center-origin; bool hit=r.bounds.IntersectRay(new Ray(origin,delta.normalized),out float distance)&&distance<delta.magnitude;check(!hit,"Observation sightline clear: "+r.name+"/"+i); }
            var largestTex=textures.OrderByDescending(t=>t.width*t.height).First();
            var stats=new Stats{passed=true,checks=assertions.Count,assertions=assertions.ToArray(),instances=artistic.OfType<Transform>().Count(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)),lodGroups=artistic.OfType<LODGroup>().Count(),shadowCasters=artistic.OfType<Renderer>().Count(r=>r.shadowCastingMode!=ShadowCastingMode.Off),sceneColliders=all.OfType<Collider>().Count(c=>c.enabled),newArtColliders=0,materials=materials.Length,textures=textures.Length,largestMesh=largest.triangles.Length/3,largestMeshName=largest.name,largestTexture=largestTex.width,largestTextureName=largestTex.name};
            Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(stats,true));Debug.Log("LEVEL01_ART_EDITOR_PASS "+assertions.Count);
        }
    }
}
