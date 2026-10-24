using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Tikal.Cinematics;
using Tikal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tikal.Tests.Editor
{
    public static class PlazaTransitionDeliveryAudit
    {
        [Serializable] public class FileHash { public string path, sha256; }
        [Serializable] public class Result
        {
            public bool passed;
            public string scene;
            public int timelineTracks, dependencies, missingScripts, missingMaterials, cameras, listeners, gameplayPlayers;
            public double duration;
            public FileHash[] testedFiles;
        }

        public static void Run()
        {
            try
            {
                if (!Application.dataPath.Replace('\\','/').Contains("/_PlazaKeynes_20261024/")) throw new Exception("Isolated audit only.");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var scene = EditorSceneManager.OpenScene(PlazaTransitionAuthoring.ScenePath);
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
                var d = UnityEngine.Object.FindAnyObjectByType<PlayableDirector>();
                var tracks = ((TimelineAsset)d.playableAsset).GetOutputTracks().ToArray();
                var dependencies = AssetDatabase.GetDependencies(scene.path, true);
                var missingScripts = all.Count(c => c == null);
                var missingMaterials = all.OfType<Renderer>().Count(r => r.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader"));
                bool valid = missingScripts == 0 && missingMaterials == 0 && tracks.Length == 4 &&
                    Math.Abs(d.duration - 10.5) < .001 && tracks.All(t => d.GetGenericBinding(t) != null) &&
                    all.OfType<Camera>().Count(c => c.enabled && c.CompareTag("MainCamera")) == 1 &&
                    all.OfType<AudioListener>().Count(c => c.enabled) == 1 &&
                    !all.OfType<PlayerController>().Any() &&
                    dependencies.All(p => p.StartsWith("Packages/") || File.Exists(p));
                var paths = Directory.GetFiles(PlazaTransitionAuthoring.Art, "*", SearchOption.AllDirectories)
                    .Concat(new[]{scene.path,"Assets/_Project/Code/Cinematics/Level01PlazaTransition.cs","Assets/_Project/Code/Cinematics/PlazaPassageSequence.cs","Assets/_Project/Code/Core/SecretEntranceCommandReceiver.cs","Assets/_Project/Code/UI/LevelResultPresenter.cs","Assets/_Project/Code/Core/SceneCatalog.cs","ProjectSettings/EditorBuildSettings.asset"});
                var result = new Result { passed = valid, scene = scene.path, timelineTracks = tracks.Length, dependencies = dependencies.Length,
                    missingScripts = missingScripts, missingMaterials = missingMaterials,
                    cameras = all.OfType<Camera>().Count(c => c.enabled), listeners = all.OfType<AudioListener>().Count(c => c.enabled),
                    gameplayPlayers = all.OfType<PlayerController>().Count(), duration = d.duration,
                    testedFiles = paths.Select(p => new FileHash { path = p.Replace('\\','/'), sha256 = Hash(p) }).ToArray() };
                File.WriteAllText(PlazaTransitionAuthoring.Evidence+"/Editor_Delivery_Audit.json", JsonUtility.ToJson(result,true));
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(valid ? 0 : 9);
            }
            catch(Exception e) { File.WriteAllText(PlazaTransitionAuthoring.Evidence+"/Delivery_Error.txt",e.ToString()); Debug.LogException(e); EditorApplication.Exit(10); }
        }

        static string Hash(string path) { using(var algorithm = SHA256.Create()) using(var stream = File.OpenRead(path)) return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","").ToLowerInvariant(); }
    }
}
