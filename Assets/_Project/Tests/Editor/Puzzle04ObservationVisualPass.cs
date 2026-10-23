using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tikal.Tests.Editor
{
    [InitializeOnLoad]
    public static class Puzzle04ObservationVisualPass
    {
        public const string Evidence="../_Puzzle04Visuals_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/Puzzle04";
        static readonly List<GameObject> parts=new List<GameObject>();
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F7")+t.localRotation.ToString("F7")+t.localScale.ToString("F7")+t.gameObject.activeSelf;
        static Puzzle04ObservationVisualPass(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/Puzzle04VisualApply.request")){File.Delete("Temp/Puzzle04VisualApply.request");try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(!File.Exists("Temp/Puzzle04VisualSurvey.request"))return;File.Delete("Temp/Puzzle04VisualSurvey.request");Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Real Level01");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current scene");File.Copy(scene.path,Evidence+"/Before.unity",false);
            var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone04_GlyphGallery");var lines=new List<string>();
            foreach(var t in zone.GetComponentsInChildren<Transform>(true))
            {lines.Add(Level01EnvironmentInspection.PathOf(t)+" active="+t.gameObject.activeInHierarchy+" position="+t.position.ToString("F4")+" local="+t.localPosition.ToString("F4")+" scale="+t.lossyScale.ToString("F4"));foreach(var c in t.GetComponents<MonoBehaviour>().Where(c=>c!=null))lines.Add(c.GetType().Name+" enabled="+c.enabled+" "+Props(c));foreach(var r in t.GetComponents<Renderer>())lines.Add("RENDER "+r.enabled+" bounds="+r.bounds.ToString("F4")+" materials="+string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));foreach(var c in t.GetComponents<Collider>())lines.Add("COLLIDER "+c.GetType().Name+" enabled="+c.enabled+" trigger="+c.isTrigger+" "+c.bounds.ToString("F4"));}
            File.WriteAllLines(Evidence+"/PuzzleVisualAudit.txt",lines);Debug.Log("PUZZLE04_VISUAL_AUDIT_READY");
        }
        static Transform Visual(Transform owner)
        {Check(owner.Find("VisualRoot_ObservationStone")==null,"No duplicate visuals");var t=new GameObject("VisualRoot_ObservationStone").transform;t.SetParent(owner,false);return t;}
        static GameObject Fit(Transform owner,string asset,Bounds target)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(p!=null,"Reuse "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(p,owner);g.name="P04_"+asset+"_"+parts.Count.ToString("D2");g.transform.rotation=Quaternion.identity;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);var b=GoldenSliceArtPass.VisualBounds(g);var scale=g.transform.localScale;g.transform.localScale=Vector3.Scale(scale,new Vector3(target.size.x/b.size.x,target.size.y/b.size.y,target.size.z/b.size.z));b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=target.center-b.center;
            foreach(var l in g.GetComponentsInChildren<LODGroup>()){var lod=l.GetLODs()[0];l.SetLODs(new[]{new LOD(.004f,lod.renderers)});var used=lod.renderers.ToHashSet();foreach(var r in l.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(l);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.On;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);parts.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath&&!scene.isDirty,"Saved Level01");Check(File.Exists(Evidence+"/Before.unity"),"Audited baseline required");var level=scene.GetRootGameObjects().Single().transform;var zone=level.Find("Environment/Zone04_GlyphGallery");Check(zone.Find("VisualRoot_ObservationStone")==null,"No reapply");var all=Level01EnvironmentArtPass.All(scene);var original=all.Where(c=>c is MonoBehaviour||c is Collider||c is Renderer||c is MeshFilter).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var puzzle=zone.GetComponentInChildren<Tikal.Puzzles.Level01.ObservationAlignmentPuzzle>();var ps=new SerializedObject(puzzle);var fragments=Enumerable.Range(0,5).Select(i=>(Renderer)ps.FindProperty("_fragments").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();var bounds=fragments.Select(r=>r.bounds).ToArray();Check(fragments.All(r=>r!=null)&&Mathf.Abs(puzzle.MinimumDot-.97f)<.00001f&&Mathf.Abs(puzzle.HoldSeconds-.35f)<.00001f,"Original five fragments and timing");
            var block=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath("ARQ_BloqueMaya_02"));var stone=block.GetComponentInChildren<Renderer>().sharedMaterial;var pulse=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environment/Level01/EchoPuzzle/MAT_EchoSanctuary_PulseStone_PBR.mat");Check(pulse.shader.name=="Universal Render Pipeline/Lit"&&pulse.IsKeywordEnabled("_EMISSION"),"Existing URP PBR pulse material");var hidden=new HashSet<Renderer>();var materialChanges=new HashSet<Renderer>();parts.Clear();var supportRoot=Visual(zone);
            // Replace masonry bodies only. Existing symbols retain their exact meshes and poses.
            var legacy=zone.Find("PuzzleAnchor/Glyph sequence mechanism");Check(legacy.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Legacy seven-stone authority already removed; no logic touched");
            foreach(var owner in Enumerable.Range(0,legacy.childCount).Select(legacy.GetChild))
            {var v=Visual(owner);foreach(var r in owner.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Plinth"||r.name=="Vertical slab").ToArray()){Fit(v,"ARQ_BloqueMaya_02",r.bounds);hidden.Add(r);r.enabled=false;}}
            foreach(var r in zone.GetComponentsInChildren<Renderer>().Where(r=>r.name=="BlankObservationPanel"||r.name=="Frieze stone"||r.name=="Provisional inscription stone").ToArray())
            {var vr=Visual(r.transform);Fit(vr,"ARQ_BloqueMaya_02",r.bounds);if(r.name=="BlankObservationPanel")Fit(vr,"ARQ_BloqueMaya_02",new Bounds(new Vector3(r.bounds.center.x,1.85f,r.bounds.center.z),new Vector3(2.05f,.5f,.45f)));if(r.name=="Frieze stone")foreach(float x in new[]{r.bounds.min.x+.22f,r.bounds.max.x-.22f})Fit(supportRoot,"ARQ_ColumnaMaya_01",new Bounds(new Vector3(x,2.7f,r.bounds.center.z-.02f),new Vector3(.22f,2.2f,.22f)));hidden.Add(r);r.enabled=false;}
            var point=(Component)ps.FindProperty("_point").objectReferenceValue;foreach(var r in point.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Low step"||r.name=="Observation platform"||r.name=="Eastern approach step")){r.sharedMaterial=stone;materialChanges.Add(r);}
            var supports=new List<string>();
            for(int i=0;i<fragments.Length;i++)
            {
                var r=fragments[i];r.sharedMaterial=pulse;materialChanges.Add(r);var vr=Visual(r.transform);var core=new GameObject("CarvedStoneCore_ExactSilhouette");core.transform.SetParent(vr,false);core.transform.localPosition=new Vector3(0,0,.95f);core.transform.localScale=new Vector3(.98f,.98f,2.8f);core.AddComponent<MeshFilter>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;core.AddComponent<MeshRenderer>().sharedMaterial=stone;
                var a=r.transform.TransformPoint(new Vector3(0,-.5f,0));var b=r.transform.TransformPoint(new Vector3(0,.5f,0));var contact=a.y<b.y?a:b;float ground=1.6f;float top=contact.y-.025f;var target=new Bounds(new Vector3(contact.x,(ground+top)*.5f,contact.z-.12f),new Vector3(.15f,top-ground,.16f));Fit(supportRoot,"ARQ_ColumnaMaya_01",target);supports.Add("Fragment "+i+" support="+target.ToString("F4"));
                Check(r.bounds==bounds[i],"Exact original fragment silhouette bounds "+i);
            }
            foreach(var r in level.Find("BlockoutMarkers").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Zone04_GlyphGallery_"))){hidden.Add(r);r.enabled=false;}
            Check(original.All(k=>hidden.Contains(k.Key as Renderer)||materialChanges.Contains(k.Key as Renderer)||k.Key!=null&&Props(k.Key)==k.Value),"All original logic, references, mesh filters and colliders unchanged");Check(poses.All(k=>Pose(k.Key)==k.Value),"Every original pose/activation unchanged including fragments, target, zone and environment");Check(parts.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"Zero new colliders");Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");Check(parts.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m!=null)),"No Missing Materials");EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Puzzle04 saved");
            File.WriteAllText(Evidence+"/EditorValidation.txt","Logic Root="+Level01EnvironmentInspection.PathOf(puzzle.transform)+"\nFive original renderers, meshes, pivots, poses, bounds, symbols and references retained exactly; only PBR material replaced.\nOriginal dot=.97 hold=.35; all colliders/ObservationZone/target/clue/runtime scripts unchanged.\nHidden placeholder renderers="+hidden.Count+"; reused prefab parts="+parts.Count+"; five inset stone cores; 0 new colliders; 0 environmental microplacements; approved Zone04 environment unchanged.\nMaterials reused only: "+AssetDatabase.GetAssetPath(stone)+"; "+AssetDatabase.GetAssetPath(pulse)+"\n"+string.Join("\n",hidden.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)))+"\n"+string.Join("\n",supports));Debug.Log("PUZZLE04_STONE_VISUALS_SAVED");
        }
    }
}
