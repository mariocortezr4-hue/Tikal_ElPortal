using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class Level01SurfacePresentation
    {
        public const string Folder="Assets/_Project/Art/Environment/Level01/SurfaceMeshes";
        [Serializable] public class Assignment {public string path,material;}
        [Serializable] public class Assignments {public Assignment[] entries;}
        public static HashSet<Renderer> GameplayRenderers(Component[] components)
        {
            var result=new HashSet<Renderer>();
            foreach(var component in components.OfType<MonoBehaviour>().Where(c=>c!=null))
            {
                var p=new SerializedObject(component).GetIterator();
                while(p.Next(true)) if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue is Renderer renderer)result.Add(renderer);
            }
            return result;
        }
        public static void PreserveFunctionalMaterials(Component[] components)
        {
            var original=JsonUtility.FromJson<Assignments>(File.ReadAllText(Level01EnvironmentArtPass.Docs+"Level01EnvironmentOriginalMaterials.json")).entries.GroupBy(x=>x.path).ToDictionary(g=>g.Key,g=>g.First().material);
            foreach(var renderer in GameplayRenderers(components))
            {
                if(!original.TryGetValue(Level01EnvironmentInspection.PathOf(renderer.transform),out string path))continue;
                renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        public static void Apply(Component[] all)
        {
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/_Project/Art/Environment/Level01","SurfaceMeshes");
            var baseline=JsonUtility.FromJson<Level01EnvironmentArtPass.Baseline>(File.ReadAllText(Level01EnvironmentArtPass.Docs+"Level01EnvironmentProtection.json"));var ids=new HashSet<string>(baseline.transforms.Select(x=>x.id));var functional=GameplayRenderers(all);
            foreach(var filter in all.OfType<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();var path=Level01EnvironmentInspection.PathOf(filter.transform);var mesh=filter.sharedMesh;
                if(renderer==null||!renderer.enabled||functional.Contains(renderer)||!path.Contains("/Environment/")||path.Contains("/PuzzleAnchor/")||path.Contains("/RevelationAnchor/")||path.Contains("/FinalWarningSteleAnchor/")||!ids.Contains(GlobalObjectId.GetGlobalObjectIdSlow(filter.transform).ToString())||mesh==null||mesh.subMeshCount!=1)continue;
                if(AssetDatabase.GetAssetPath(mesh).StartsWith(Folder+"/"))continue;
                string output=Folder+"/UV_"+GlobalObjectId.GetGlobalObjectIdSlow(filter.transform)+".asset";var copy=AssetDatabase.LoadAssetAtPath<Mesh>(output);
                if(copy==null)
                {
                    var indices=mesh.triangles;var old=mesh.vertices;var normals=mesh.normals;var vertices=new Vector3[indices.Length];var ns=new Vector3[indices.Length];var uv=new Vector2[indices.Length];
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var a=filter.transform.TransformPoint(old[indices[i]]);var b=filter.transform.TransformPoint(old[indices[i+1]]);var c=filter.transform.TransformPoint(old[indices[i+2]]);var n=Vector3.Cross(b-a,c-a).normalized;
                        for(int j=0;j<3;j++)
                        {
                            int source=indices[i+j];var world=filter.transform.TransformPoint(old[source]);vertices[i+j]=old[source];ns[i+j]=normals.Length==old.Length?normals[source]:Vector3.up;
                            uv[i+j]=(Mathf.Abs(n.y)>=.5f?new Vector2(world.x,world.z):Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(world.z,world.y):new Vector2(world.x,world.y))*.125f;
                        }
                    }
                    copy=new Mesh{name=mesh.name+"_SurfaceUV",indexFormat=vertices.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};copy.vertices=vertices;copy.normals=ns;copy.uv=uv;copy.triangles=Enumerable.Range(0,vertices.Length).ToArray();copy.RecalculateBounds();copy.RecalculateTangents();
                    AssetDatabase.CreateAsset(copy,output);
                }
                filter.sharedMesh=copy;if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            }
        }
        public static void Validate(Component[] all,Action<bool,string> check)
        {
            var original=JsonUtility.FromJson<Assignments>(File.ReadAllText(Level01EnvironmentArtPass.Docs+"Level01EnvironmentOriginalMaterials.json")).entries.GroupBy(x=>x.path).ToDictionary(g=>g.Key,g=>g.First().material);
            foreach(var renderer in GameplayRenderers(all))if(original.TryGetValue(Level01EnvironmentInspection.PathOf(renderer.transform),out string path))check(AssetDatabase.GetAssetPath(renderer.sharedMaterial)==path,"Gameplay feedback material preserved: "+renderer.name);
            foreach(var filter in all.OfType<MeshFilter>().Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Folder+"/")))
            {
                var visual=filter.sharedMesh;check(visual.uv.Length==visual.vertexCount&&visual.uv.Distinct().Count()>2,"Usable UV on original visual surface: "+filter.name);
                var collider=filter.GetComponent<MeshCollider>();if(collider==null)continue;
                var originalMesh=collider.sharedMesh;var sourceIndices=originalMesh.triangles;var sourceVertices=originalMesh.vertices;var targetIndices=visual.triangles;var targetVertices=visual.vertices;
                check(sourceIndices.Length==targetIndices.Length&&Enumerable.Range(0,sourceIndices.Length).All(i=>sourceVertices[sourceIndices[i]].Equals(targetVertices[targetIndices[i]])),"UV-only visual copy matches original collider triangles exactly: "+filter.name);
            }
        }
    }
}
