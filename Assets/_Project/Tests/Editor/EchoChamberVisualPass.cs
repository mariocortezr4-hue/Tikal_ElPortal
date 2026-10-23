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
    public static class EchoChamberVisualPass
    {
        public const string Evidence="../_EchoChamberVisuals_20261022";
        const string Folder="Assets/_Project/Art/Environment/Level01/EchoPuzzle";
        static readonly List<GameObject> placed=new List<GameObject>();
        [Serializable] class Report {public int hiddenRenderers,visualRoots,placements,newColliders,environmentAdjustments;public string[] hidden,assets,checks;}
        static EchoChamberVisualPass(){EditorApplication.update+=Poll;}
        static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);}
        static string Props(Component c)=>(string)typeof(TutorialEnvironmentArtPass).GetMethod("Properties",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{c});
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6")+t.gameObject.activeSelf;
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(File.Exists("Temp/EchoVisualApply.request"))
            {File.Delete("Temp/EchoVisualApply.request");try{Apply();}catch(Exception e){File.WriteAllText(Evidence+"/Error.txt",e.ToString());Debug.LogException(e);}return;}
            if(File.Exists("Temp/EchoMaterialFinish.request"))
            {File.Delete("Temp/EchoMaterialFinish.request");var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/MAT_EchoSanctuary_PulseStone_PBR.mat");m.SetColor("_EmissionColor",new Color(.001f,.001f,.001f));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();Debug.Log("ECHO_PBR_PULSE_VARIANT_READY");return;}
            if(File.Exists("Temp/EchoMarksFinish.request"))
            {File.Delete("Temp/EchoMarksFinish.request");var markScene=EditorSceneManager.GetActiveScene();Check(markScene.path==Level01EnvironmentInspection.ScenePath&&!markScene.isDirty,"Saved Level01");var markZone=markScene.GetRootGameObjects().Single().transform.Find("Environment/Zone03_EchoChamber");var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/MAT_EchoSanctuary_PulseStone_PBR.mat");foreach(var r in markZone.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Echo progress mark")))r.sharedMaterial=m;EditorSceneManager.MarkSceneDirty(markScene);EditorSceneManager.SaveScene(markScene);Debug.Log("ECHO_PROGRESS_MARK_PBR_EMISSION_READY");return;}
            if(!File.Exists("Temp/EchoVisualInspect.request"))return;File.Delete("Temp/EchoVisualInspect.request");Directory.CreateDirectory(Evidence);
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Level01 Edit Mode required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current user scene");File.Copy(scene.path,Evidence+"/Before.unity",false);
            var zone=scene.GetRootGameObjects().Single().transform.Find("Environment/Zone03_EchoChamber");var lines=new List<string>();
            foreach(var t in zone.GetComponentsInChildren<Transform>(true))
            {
                lines.Add(Level01EnvironmentInspection.PathOf(t)+" world="+t.position.ToString("F3")+" local="+t.localPosition.ToString("F3")+" scale="+t.lossyScale.ToString("F3")+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name)));
                foreach(var c in t.GetComponents<MonoBehaviour>().Where(c=>c!=null))lines.Add(EditorJsonUtility.ToJson(c,true));
                foreach(var r in t.GetComponents<Renderer>())lines.Add("RENDER enabled="+r.enabled+" bounds="+r.bounds.ToString("F3")+" mat="+AssetDatabase.GetAssetPath(r.sharedMaterial));
                foreach(var c in t.GetComponents<Collider>())lines.Add("COLLIDER "+c.bounds.ToString("F3"));
            }
            File.WriteAllLines(Evidence+"/LocalVisualAudit.txt",lines);
            var assets=new List<string>();foreach(var name in new[]{"ARQ_AltarMaya_01","ARQ_BloqueMaya_02","ARQ_ColumnaMaya_01","ARQ_PlataformaElevada_01","ROC_RocaMusgo_01"})
            {var p=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(name));if(p==null){assets.Add(name+" absent");continue;}var g=UnityEngine.Object.Instantiate(p);assets.Add(name+" "+GoldenSliceArtPass.VisualBounds(g).ToString("F3"));UnityEngine.Object.DestroyImmediate(g);}
            File.WriteAllLines(Evidence+"/CandidateBounds.txt",assets);Debug.Log("ECHO_VISUAL_AUDIT_READY");
        }
        static Transform Visual(Transform owner)
        {Check(owner.Find("VisualRoot_EchoSanctuary")==null,"No duplicate art on "+owner.name);var t=new GameObject("VisualRoot_EchoSanctuary").transform;t.SetParent(owner,false);return t;}
        static GameObject Part(Transform root,string asset,Vector3 point,Vector3 scale,float yaw=0,Material pulse=null)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath(asset));Check(prefab!=null,"Reuse "+asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name="Echo_"+asset+"_"+placed.Count.ToString("D2");g.transform.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));g.transform.localScale=scale;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);var b=GoldenSliceArtPass.VisualBounds(g);g.transform.position+=Vector3.up*(point.y-b.min.y);
            foreach(var lod in g.GetComponentsInChildren<LODGroup>())
            {var first=lod.GetLODs()[0];lod.SetLODs(new[]{new LOD(.004f,first.renderers)});var used=first.renderers.ToHashSet();foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=used.Contains(r);PrefabUtility.RecordPrefabInstancePropertyModifications(lod);}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true)){if(pulse!=null)r.sharedMaterials=r.sharedMaterials.Select(_=>pulse).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);placed.Add(g);return g;
        }
        static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();Check(scene.path==Level01EnvironmentInspection.ScenePath,"Current Level01 scene required");if(scene.isDirty)Check(EditorSceneManager.SaveScene(scene),"Preserve current user changes before visual work");Check(File.Exists(Evidence+"/Before.unity"),"Audit/current-user backup required");File.Copy(scene.path,Evidence+"/BeforeApply.unity",false);
            var level=scene.GetRootGameObjects().Single().transform;var zone=level.Find("Environment/Zone03_EchoChamber");var puzzle=zone.GetComponentInChildren<Tikal.Puzzles.Level01.EchoSequencePuzzle>();var ps=new SerializedObject(puzzle);var stones=Enumerable.Range(0,3).Select(i=>(Tikal.Puzzles.Level01.EchoStone)ps.FindProperty("_stones").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();var source=(Tikal.Puzzles.Level01.EchoSource)ps.FindProperty("_source").objectReferenceValue;
            Check(source!=null&&stones.All(g=>g!=null),"Existing source and three interactables referenced");Check(Enumerable.Range(0,3).Select(puzzle.TargetNoteAt).SequenceEqual(new[]{Tikal.Puzzles.Level01.EchoNote.B,Tikal.Puzzles.Level01.EchoNote.A,Tikal.Puzzles.Level01.EchoNote.C}),"Canonical B/A/C sequence retained");
            var all=Level01EnvironmentArtPass.All(scene);var originals=all.Where(c=>c is MonoBehaviour||c is Collider||c is AudioSource||c is MeshFilter||c is Renderer).ToDictionary(c=>c,Props);var poses=all.OfType<Transform>().ToDictionary(t=>t,Pose);int missing=all.OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var hidden=new HashSet<Renderer>();foreach(var stone in stones)foreach(var r in stone.GetComponentsInChildren<Renderer>())hidden.Add(r);foreach(var r in source.GetComponentsInChildren<Renderer>().Where(r=>!r.name.StartsWith("Echo progress mark")))hidden.Add(r);var inscription=zone.Find("Echo guidance inscription");foreach(var r in inscription.GetComponentsInChildren<Renderer>())hidden.Add(r);
            foreach(var r in level.Find("BlockoutMarkers").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Zone03_EchoChamber_")))hidden.Add(r);Check(hidden.Count==14,"Exactly 14 visible puzzle/debug placeholders");foreach(var r in hidden)r.enabled=false;
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","EchoPuzzle");var basePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Level01EnvironmentArtPass.AssetPath("ARQ_BloqueMaya_02"));var originalMaterial=basePrefab.GetComponentInChildren<Renderer>().sharedMaterial;var pulse=new Material(originalMaterial){name="MAT_EchoSanctuary_PulseStone_PBR"};pulse.SetColor("_EmissionColor",new Color(.001f,.001f,.001f));pulse.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;pulse.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(pulse,Folder+"/"+pulse.name+".mat");placed.Clear();
            for(int i=0;i<3;i++)
            {
                var g=stones[i];var root=Visual(g.transform);var p=g.transform.position;Part(root,"ARQ_BloqueMaya_02",new Vector3(p.x,.3f,p.z),new Vector3(.73f,.55f,1.12f));GameObject main;
                if(g.Note==Tikal.Puzzles.Level01.EchoNote.A)main=Part(root,"ARQ_BloqueMaya_02",new Vector3(p.x,.60f,p.z),new Vector3(.72f,1.45f,.90f),0,pulse);
                else if(g.Note==Tikal.Puzzles.Level01.EchoNote.B)main=Part(root,"ROC_RocaMusgo_01",new Vector3(p.x,.60f,p.z),new Vector3(.57f,.75f,.53f),37,pulse);
                else
                {main=Part(root,"ARQ_ColumnaMaya_01",new Vector3(p.x,.60f,p.z),new Vector3(.64f,.40f,.64f),0,pulse);foreach(float dx in new[]{-.22f,.22f})Part(root,"ARQ_BloqueMaya_02",new Vector3(p.x+dx,1.65f,p.z),new Vector3(.12f,.9f,.28f));}
                var renderer=main.GetComponentsInChildren<Renderer>().First(r=>r.enabled);var serialized=new SerializedObject(g);serialized.FindProperty("_visual").objectReferenceValue=renderer;serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var altar=Visual(source.transform);var sp=source.transform.position;Part(altar,"ARQ_PlataformaElevada_01",new Vector3(sp.x,.3f,sp.z),new Vector3(.15f,.15f,.145f));Part(altar,"ARQ_BloqueMaya_02",new Vector3(sp.x,.715f,sp.z),new Vector3(.95f,1.6f,1.35f));Part(altar,"ARQ_BloqueMaya_02",new Vector3(sp.x,1.6035f,sp.z),new Vector3(.92f,.08f,1.4f));Part(altar,"ROC_RocaMusgo_01",new Vector3(sp.x,1.6475f,sp.z+.12f),new Vector3(.32f,.22f,.32f),29);
            Part(Visual(inscription),"ARQ_BloqueMaya_02",new Vector3(inscription.position.x,.3f,inscription.position.z),new Vector3(.62f,2.35f,.40f),90);
            Check(placed.All(g=>g.GetComponentsInChildren<Collider>(true).Length==0),"No new colliders");Check(poses.All(k=>Pose(k.Key)==k.Value),"All original poses/activation retained, including user edits");
            foreach(var entry in originals)
            {if(hidden.Contains(entry.Key as Renderer))continue;if(entry.Key is Tikal.Puzzles.Level01.EchoStone stone){var a=entry.Value;var b=Props(stone);Check(RemoveVisual(a)==RemoveVisual(b),"Only visual renderer binding changed on "+stone.name);}else Check(Props(entry.Key)==entry.Value,"Original component retained "+entry.Key.name+"/"+entry.Key.GetType().Name);}
            Check(Level01EnvironmentArtPass.All(scene).OfType<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==missing,"No new Missing Scripts");Check(placed.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m!=null)),"No missing materials");
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Echo sanctuary saved");AssetDatabase.SaveAssets();File.WriteAllText(Evidence+"/EditorValidation.json",JsonUtility.ToJson(new Report{hiddenRenderers=hidden.Count,visualRoots=5,placements=placed.Count,newColliders=0,environmentAdjustments=0,hidden=hidden.Select(r=>Level01EnvironmentInspection.PathOf(r.transform)).ToArray(),assets=placed.Select(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).Distinct().ToArray(),checks=new[]{"Existing EchoSequencePuzzle B/A/C authority, source, audio, feedback marks and clue references retained","Three EchoStone visual renderer references rebound only; no runtime scripts or logic changed","Five identity VisualRoots; all existing transforms and colliders preserved","14 placeholders hidden without deleting their objects/components","Existing square/round/fork silhouettes retained; no invented glyphs or decorative stelae","Three original progress marks stay on new altar cap with original emission bindings","One shared PBR pulse material; LOD0 pinned on small interactive visuals for stable feedback","No new Missing Scripts/Materials; no environmental placements or new colliders","User pre-existing scene edits preserved separately from this task"}},true));Debug.Log("ECHO_SANCTUARY_VISUALS_SAVED");
        }
        static string RemoveVisual(string text)
        {return System.Text.RegularExpressions.Regex.Replace(text,@"(?m)^.*_visual.*$","");}
    }
}
