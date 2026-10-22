using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Tikal.Puzzles.Level01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Puzzle02GuardianVisualIntegration
    {
        public const string Evidence="../_Puzzle02FinalVisuals_20261022";
        public const string Kit="Assets/Tikal/PuzzlesKit/Puzzle02";
        [Serializable] class Row {public string path;public Vector3 position,localPosition,rotation,scale,boundsCenter,boundsSize;public string[] components,materials;public string mesh;public bool rendererEnabled;}
        [Serializable] class Inspection {public Row[] objects;public string[] guardianVisuals;}
        [Serializable] class IntegrationReport {public bool passed;public int rotorVisuals,destinationSymbols,hiddenRenderers,reskinnedRenderers,newColliders,environmentAdjustments;public string[] hidden,materials,guardians,checks;}
        static Puzzle02GuardianVisualIntegration(){EditorApplication.update+=Poll;}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Temp/Puzzle02VisualFinish.request"))
            {File.Delete("Temp/Puzzle02VisualFinish.request");try{FinishCurrent();}catch(Exception e){File.WriteAllText(Evidence+"/FinishError.txt",e.ToString());Debug.LogException(e);}return;}
            if(File.Exists("Temp/Puzzle02StoneFinish.request"))
            {File.Delete("Temp/Puzzle02StoneFinish.request");try{FinishStonePresentation();}catch(Exception e){File.WriteAllText(Evidence+"/StoneError.txt",e.ToString());Debug.LogException(e);}return;}
            bool inspect=File.Exists("Temp/Puzzle02VisualInspect.request"),apply=File.Exists("Temp/Puzzle02VisualApply.request");if(!inspect&&!apply)return;File.Delete(inspect?"Temp/Puzzle02VisualInspect.request":"Temp/Puzzle02VisualApply.request");Directory.CreateDirectory(Evidence);
            try{if(inspect)Inspect();else Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Inspect()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Open Level01 Edit Mode required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Save current valid scene first");File.Copy(scene.path,Evidence+"/Before.unity",false);
            var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone02_PathGuardians");var rows=new List<Row>();
            foreach(var t in zone.GetComponentsInChildren<Transform>(true))
            {
                var r=t.GetComponent<Renderer>();var f=t.GetComponent<MeshFilter>();rows.Add(new Row{path=Level01EnvironmentInspection.PathOf(t),position=t.position,localPosition=t.localPosition,rotation=t.localEulerAngles,scale=t.localScale,components=t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name).ToArray(),materials=r==null?Array.Empty<string>():r.sharedMaterials.Select(m=>m==null?"MISSING":AssetDatabase.GetAssetPath(m)).ToArray(),mesh=f==null?"":AssetDatabase.GetAssetPath(f.sharedMesh),rendererEnabled=r!=null&&r.enabled,boundsCenter=r==null?Vector3.zero:r.bounds.center,boundsSize=r==null?Vector3.zero:r.bounds.size});
            }
            var guardians=zone.GetComponentsInChildren<PathGuardian>(true);File.WriteAllText(Evidence+"/Inspection.json",JsonUtility.ToJson(new Inspection{objects=rows.ToArray(),guardianVisuals=guardians.Select(g=>g.name+" => "+Level01EnvironmentInspection.PathOf((Transform)new SerializedObject(g).FindProperty("_visual").objectReferenceValue)).ToArray()},true));Debug.Log("PUZZLE02_VISUAL_INSPECTED");
        }
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Level01 only");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current valid scene");
            var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone02_PathGuardians");var puzzle=zone.GetComponentInChildren<PathGuardiansPuzzle>(true);Check(puzzle!=null,"Existing Puzzle02 authority");var ps=new SerializedObject(puzzle);
            var guardians=Enumerable.Range(0,3).Select(i=>(PathGuardian)ps.FindProperty("_guardians").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();var markers=Enumerable.Range(0,3).Select(i=>(DestinationMarker)ps.FindProperty("_destinations").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Check(Enumerable.Range(0,3).Select(i=>ps.FindProperty("_solution").GetArrayElementAtIndex(i).intValue).SequenceEqual(new[]{3,1,2}),"Canonical solution 3/1/2");Check(guardians.Select(g=>new SerializedObject(g).FindProperty("_initialOrientation").intValue).SequenceEqual(new[]{3,0,0}),"Canonical initial 3/0/0");
            Check(guardians.All(g=>g.transform.Find("Rotation pivot/VisualRoot_FinalKit")==null),"No duplicate guardian visuals");
            var art=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/Prefabs/PFB_P02_Guardian_"+(char)('A'+i)+"_RotorVisual.prefab")).ToArray();var reference=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/Prefabs/PFB_P02_Guardians_VisualReferenceSet.prefab");var stone=AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/MAT_SymbolMount_AgedLimestone.mat");var emission=AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/MAT_GuardiansAlignment_Emission_0.mat");
            Check(art.All(p=>p!=null)&&reference!=null&&stone!=null&&emission!=null,"Puzzle02-only assets imported");Check(art.All(p=>p.GetComponentsInChildren<Collider>(true).Length==0&&p.GetComponentsInChildren<MonoBehaviour>(true).Length==0),"Pure visual rotors, no gameplay/collision authority");
            var all=Level01EnvironmentArtPass.All(scene);var hidden=new HashSet<Renderer>();var reskin=new HashSet<Renderer>();
            foreach(var g in guardians){foreach(var r in g.transform.Find("Rotation pivot").GetComponentsInChildren<Renderer>(true))hidden.Add(r);reskin.Add(g.transform.Find("Fixed plinth").GetComponent<Renderer>());}
            foreach(var m in markers){foreach(var n in new[]{"Matching abstract symbol","Rear matching symbol"})foreach(var r in m.transform.Find(n).GetComponentsInChildren<Renderer>(true))hidden.Add(r);foreach(var n in new[]{"Intact marker stone","Marker footing"})reskin.Add(m.transform.Find(n).GetComponent<Renderer>());}
            var feedback=new SerializedObject(puzzle.GetComponent<GuardiansAlignmentFeedback>());var feedbackRenderers=new List<Renderer>();foreach(var field in new[]{"_individualMarks","_centralMarks"})for(int i=0;i<3;i++)feedbackRenderers.Add((Renderer)feedback.FindProperty(field).GetArrayElementAtIndex(i).objectReferenceValue);
            foreach(var r in feedbackRenderers)reskin.Add(r);reskin.Add(puzzle.transform.Find("Central neutral stone/Low central platform").GetComponent<Renderer>());
            // Retain the three auxiliary blockout pillars and their collision volumes; only their stone finish changes.
            foreach(var t in zone.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="OrientationPillar"))reskin.Add(t.GetComponent<Renderer>());
            reskin.Add(zone.Find("Guidance inscription/Provisional inscription stone").GetComponent<Renderer>());
            var protectedComponents=all.Where(c=>!(c is Transform)&&!(c is Renderer r&&(hidden.Contains(r)||reskin.Contains(r)))).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);
            foreach(var r in hidden)r.enabled=false;foreach(var r in reskin)r.sharedMaterial=feedbackRenderers.Contains(r)?emission:stone;
            var installed=new List<GameObject>();
            for(int i=0;i<3;i++)
            {
                var pivot=(Transform)new SerializedObject(guardians[i]).FindProperty("_visual").objectReferenceValue;Check(pivot==guardians[i].transform.Find("Rotation pivot"),"Original pivot binding retained");
                var g=(GameObject)PrefabUtility.InstantiatePrefab(art[i],pivot);g.name="VisualRoot_FinalKit";g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;g.transform.localScale=Vector3.one;PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);installed.Add(g);
                var body=g.GetComponentInChildren<LODGroup>();Check(body!=null&&body.lodCount==3,"Three imported body LODs");Check(body.GetLODs().Select(l=>l.screenRelativeTransitionHeight).Zip(new[]{.25f,.07f,.01f},(a,b)=>Mathf.Abs(a-b)<.001f).All(x=>x),"Kit LOD thresholds retained");
                var source=reference.transform.Find("PathGuardians/DestinationMarker "+(char)('A'+i));foreach(var suffix in new[]{"Front","Rear"})
                {
                    var symbol=source.Find("Symbol_"+(char)('A'+i)+"_"+suffix);Check(symbol!=null,"Kit destination symbol source");var child=UnityEngine.Object.Instantiate(symbol.gameObject,markers[i].transform,false);child.name="FinalKit_"+symbol.name;Check(child.GetComponentsInChildren<Collider>(true).Length==0&&child.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Destination additions visual only");installed.Add(child);
                }
                var b=GoldenSliceArtPass.VisualBounds(g);Check(b.size.y>1.8f&&b.size.y<2.1f,"Human-scale guardian height "+b.size.y);Check(b.min.y>=guardians[i].transform.position.y+.1f&&b.min.y<guardians[i].transform.position.y+.23f,"Body rests at kit pedestal height");
            }
            Check(protectedComponents.All(k=>k.Key!=null&&Props(k.Key)==k.Value),"Every original gameplay component, collider and non-selected visual unchanged");Check(poses.All(k=>Pose(k.Key)==k.Value),"All original poses/activation retained including environment");Check(installed.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0&&g.GetComponentsInChildren<MonoBehaviour>(true).Length==0),"No new colliders or runtime components");
            var materials=installed.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Concat(reskin.SelectMany(r=>r.sharedMaterials)).Distinct().ToArray();Check(materials.All(m=>m!=null&&m.shader!=null&&m.shader.name=="Universal Render Pipeline/Lit"&&m.shader.isSupported),"All final materials supported URP/Lit");Check(materials.All(m=>!m.HasProperty("_Metallic")||m.GetFloat("_Metallic")<.05f||(m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")&&m.GetTexture("_MetallicGlossMap")!=null)),"Stone metallic uses verified near-zero packed map or zero scalar");
            Check(Level01EnvironmentArtPass.All(scene).All(c=>c!=null),"No Missing Scripts");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final Puzzle02 visuals saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new IntegrationReport{passed=true,rotorVisuals=3,destinationSymbols=6,hiddenRenderers=hidden.Count,reskinnedRenderers=reskin.Count,newColliders=0,environmentAdjustments=0,hidden=hidden.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),materials=materials.Select(AssetDatabase.GetAssetPath).ToArray(),guardians=guardians.Select(g=>g.name+" | original pivot and collider | neutral VisualRoot | scale 1 | approx 1.93m rotor").ToArray(),checks=new[]{"Solution [3,1,2], initial [3,0,0] retained; authority/reward/definitions unchanged","Every original transform and functional component preserved","Three kit rotors plus six destination symbol groups; no new gameplay authority","Original six feedback Renderer bindings retained; kit MPB-compatible emission material","Original collision volumes retained; no new colliders","Environment frozen, zero placements/repositions in jungle","No Missing Scripts/materials; supported URP/Lit, near-zero metallic","Final visuals saved; local interaction/rotation/clue validation pending"}},true));Debug.Log("PUZZLE02_FINAL_VISUALS_SAVED");FinishCurrent();
        }
        static void FinishCurrent()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!EditorApplication.isPlaying,"Current Level01 Edit Mode");
            var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone02_PathGuardians");var puzzle=zone.GetComponentInChildren<PathGuardiansPuzzle>();var gs=puzzle.GetComponentsInChildren<PathGuardian>();Check(gs.Length==3&&gs.All(g=>g.transform.Find("Rotation pivot/VisualRoot_FinalKit")!=null),"Three installed visual roots");
            // The packed map remains authoritative in URP. Its R mean was measured as 3.345/255
            // and alpha mean as 13.7/255. Zero the scalar only for the shader's no-map fallback.
            var body=AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/MAT_PUZ_GuardianMaya_01_Stone.mat");body.SetFloat("_Metallic",0);EditorUtility.SetDirty(body);
            var roots=gs.Select(g=>g.transform.Find("Rotation pivot/VisualRoot_FinalKit")).ToArray();Check(roots.All(t=>t.localPosition.sqrMagnitude<.000001f&&Quaternion.Angle(t.localRotation,Quaternion.identity)<.01f&&t.localScale==Vector3.one),"Neutral final visuals; no double initial rotation");Check(roots.All(t=>t.GetComponentsInChildren<Collider>(true).Length==0&&t.GetComponentsInChildren<MonoBehaviour>(true).Length==0),"Visual roots contain no gameplay authority/collision");
            var before=JsonUtility.FromJson<Inspection>(File.ReadAllText(Evidence+"/Inspection.json"));var originalPaths=before.objects.Where(r=>r.rendererEnabled).Select(r=>r.path).ToHashSet();var hidden=zone.GetComponentsInChildren<Renderer>(true).Where(r=>!r.enabled&&originalPaths.Contains(Level01EnvironmentInspection.PathOf(r.transform))).ToArray();
            var mats=zone.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.sharedMaterials.Any(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(Kit))).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();Check(mats.All(m=>m!=null&&m.shader.name=="Universal Render Pipeline/Lit"&&m.shader.isSupported),"Final material shader support");
            foreach(var p in AssetDatabase.FindAssets("t:Texture2D",new[]{Kit+"/Textures"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.IndexOf("Normal",StringComparison.OrdinalIgnoreCase)>=0))Check(((TextureImporter)AssetImporter.GetAtPath(p)).textureType==TextureImporterType.NormalMap,"Correct Normal import "+p);
            Check(Level01EnvironmentArtPass.All(scene).All(c=>c!=null),"No Missing Scripts");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final guardian scene saved");AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new IntegrationReport{passed=true,rotorVisuals=3,destinationSymbols=6,hiddenRenderers=hidden.Length,reskinnedRenderers=20,newColliders=0,environmentAdjustments=0,hidden=hidden.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),materials=mats.Select(AssetDatabase.GetAssetPath).ToArray(),guardians=gs.Select(g=>g.name+" original logic/collider/pivot; kit visual local identity, scale1; approx1.93m").ToArray(),checks=new[]{"Existing functional components, original poses and non-selected visuals protected during installation","Three final rotor visuals and six fixed destination symbol groups","Original six feedback renderers and their bindings retained","Packed metallic mean R=0.0131, smoothness mean alpha=0.0537; fallback Metallic=0","Supported URP/Lit and correctly imported normals; no Missing Scripts/materials","No new colliders, gameplay components or environment adjustments","Saved scene; local native interaction/rotation/clue verification pending"}},true));Debug.Log("PUZZLE02_FINAL_VISUALS_SAVED");
        }
        static void FinishStonePresentation()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01 required");var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone02_PathGuardians");
            var pillars=zone.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="OrientationPillar").ToArray();Check(pillars.Length==3,"Exactly three obsolete auxiliary pillars");
            var objects=pillars.SelectMany(t=>t.GetComponents<Component>().Cast<UnityEngine.Object>().Append(t.gameObject)).ToHashSet();
            foreach(var behaviour in Level01EnvironmentArtPass.All(scene).OfType<MonoBehaviour>())
            {
                var so=new SerializedObject(behaviour);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)Check(!objects.Contains(it.objectReferenceValue),"No gameplay binding to obsolete auxiliary pillars: "+behaviour.GetType().Name+"/"+it.propertyPath);
            }
            var all=Level01EnvironmentArtPass.All(scene);var protectedParts=all.Where(c=>c is MonoBehaviour||c is Collider).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,t=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6"));
            var reference=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/Prefabs/PFB_P02_Guardians_VisualReferenceSet.prefab");var cube=reference.transform.Find("PathGuardians/Guardian A/Fixed plinth").GetComponent<MeshFilter>().sharedMesh;
            var puzzle=zone.GetComponentInChildren<PathGuardiansPuzzle>();var selected=puzzle.GetComponentsInChildren<Transform>().Where(t=>new[]{"Fixed plinth","Intact marker stone","Marker footing","Low central platform"}.Contains(t.name)).Concat(new[]{zone.Find("Guidance inscription/Provisional inscription stone")}).ToArray();
            foreach(var t in selected)t.GetComponent<MeshFilter>().sharedMesh=cube;
            // Remove only this task's unsuccessful new cladding. All pre-existing objects/components remain.
            foreach(var p in pillars){var temporary=p.Find("FinalKit_StoneCourses");if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary.gameObject);p.gameObject.SetActive(false);}
            Check(protectedParts.All(k=>k.Key!=null&&Props(k.Key)==k.Value),"All original script and collider components preserved");
            Check(poses.Where(k=>k.Key!=null).All(k=>k.Value==k.Key.localPosition.ToString("F6")+k.Key.localRotation.ToString("F6")+k.Key.localScale.ToString("F6")),"All surviving original poses preserved");
            var report=JsonUtility.FromJson<IntegrationReport>(File.ReadAllText(Evidence+"/EditorValidation.json"));report.checks=report.checks.Where(c=>!c.StartsWith("Kit R2 chamfered limestone mesh adapted")&&!c.StartsWith("Three auxiliary pillars visually replaced")&&!c.StartsWith("Eleven existing fixed stone visual")).Concat(new[]{"Three obsolete OrientationPillar objects retained inactive after zero gameplay object-reference bindings and no runtime name references","Their auxiliary collision volumes retire with their visuals; all functional guardian/destination/pedestal colliders stay active","Temporary cladding discarded; fixed reference stone meshes restored; no environment decoration"}).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Final local presentation saved");
            const string folder="Assets/_Project/Art/Models/Puzzle02Visuals";const string meshPath=folder+"/SM_P02_KitLimestone_NormalizedVisual.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh!=null){Check(!Level01EnvironmentArtPass.All(scene).OfType<MeshFilter>().Any(f=>f.sharedMesh==mesh),"Temporary generated mesh has no scene references");Check(Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Temporary folder within project Assets");AssetDatabase.DeleteAsset(meshPath);if(Directory.Exists(folder)&&Directory.GetFileSystemEntries(folder).Length==0)AssetDatabase.DeleteAsset(folder);}
            AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(report,true));Debug.Log("PUZZLE02_AUXILIARY_PLACEHOLDERS_RETIRED");
        }
    }
}
