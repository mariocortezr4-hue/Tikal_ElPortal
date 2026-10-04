using System;
using System.IO;
using System.Linq;
using Tikal.Interaction;
using Tikal.Puzzles;
using Tikal.Puzzles.Clues;
using Tikal.Puzzles.Level01;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class GlyphSceneAuthoring
    {
        static GameObject At(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }
        static Renderer Shape(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.GetComponent<Renderer>();
        }
        static Vector2[] Marks(int value)
        {
            float a = .24f;
            switch (value)
            {
                case 1: return new[] { Vector2.zero };
                case 2: return new[] { new Vector2(-a, 0), new Vector2(a, 0) };
                case 3: return new[] { new Vector2(-a, a), new Vector2(a, a), new Vector2(0, -a) };
                case 4: return new[] { new Vector2(-a, a), new Vector2(a, a), new Vector2(-a, -a), new Vector2(a, -a) };
                case 5: return Marks(4).Concat(new[] { Vector2.zero }).ToArray();
                case 6: return new[] { new Vector2(-a, a), new Vector2(a, a), new Vector2(-a, 0), new Vector2(a, 0), new Vector2(-a, -a), new Vector2(a, -a) };
                default: return new[] { new Vector2(-a, .32f), new Vector2(0, .32f), new Vector2(a, .32f),
                    new Vector2(-a, 0), Vector2.zero, new Vector2(a, 0), new Vector2(0, -.32f) };
            }
        }

        public static void Run()
        {
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var root = scene.GetRootGameObjects().Single().transform;
                var zone = root.Find("Environment/Zone04_GlyphGallery");
                if (zone.GetComponentInChildren<GlyphSequencePuzzle>(true) != null)
                    throw new Exception("Glyph puzzle already exists; refusing duplicate installation.");
                const string stonePath = "Assets/_Project/Art/Materials/Level01/GlyphStone.mat";
                const string marksPath = "Assets/_Project/Art/Materials/Level01/GlyphMarks.mat";
                if (File.Exists(stonePath) || File.Exists(marksPath)) throw new Exception("Glyph materials already exist.");
                var stoneMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/MuralStone.mat"));
                stoneMaterial.name = "GlyphStone";
                stoneMaterial.SetColor("_BaseColor", new Color(.19f, .22f, .2f));
                stoneMaterial.EnableKeyword("_EMISSION");
                stoneMaterial.SetColor("_EmissionColor", Color.black);
                AssetDatabase.CreateAsset(stoneMaterial, stonePath);
                var marksMaterial = new Material(stoneMaterial);
                marksMaterial.name = "GlyphMarks";
                marksMaterial.SetColor("_BaseColor", new Color(.82f, .74f, .52f));
                AssetDatabase.CreateAsset(marksMaterial, marksPath);
                var host = At("Glyph sequence mechanism", zone.Find("PuzzleAnchor"), zone.Find("PuzzleAnchor").position);
                var puzzle = host.AddComponent<GlyphSequencePuzzle>();
                EchoSceneAuthoring.Set(puzzle, "_definition", AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.glyph_gallery.asset"));
                EchoSceneAuthoring.Set(puzzle, "_initialState", 1);
                var floor = root.Find("Environment/Terrain").GetComponentsInChildren<MeshCollider>()
                    .Single(collider => collider.Raycast(new Ray(new Vector3(0, 20, 39), Vector3.down), out _, 30));
                int[] order = { 3, 1, 5, 2, 7, 4, 6 };
                var stones = new GlyphStone[7];
                Physics.SyncTransforms();
                for (int index = 0; index < 7; index++)
                {
                    float x = -7.6f + index * 1.45f;
                    float z = index == 0 ? 35.35f : 34.8f + (index % 2) * .12f;
                    if (index == 0) x = -7.35f;
                    if (!floor.Raycast(new Ray(new Vector3(x, 20, z), Vector3.down), out var ground, 30))
                        throw new Exception("Glyph stone has no terrain support.");
                    var go = At("GlyphStone_" + order[index], host.transform, new Vector3(x, ground.point.y, z));
                    var stone = go.AddComponent<GlyphStone>();
                    stones[index] = stone;
                    Shape("Plinth", go.transform, new Vector3(0, .1f, 0), new Vector3(1.1f, .2f, .65f), stoneMaterial, true);
                    var visual = Shape("Vertical slab", go.transform, new Vector3(0, .98f, 0), new Vector3(.96f, 1.56f, .35f), stoneMaterial, true);
                    var count = At("Count marks", go.transform, go.transform.position);
                    var points = Marks(order[index]);
                    for (int mark = 0; mark < points.Length; mark++)
                    {
                        var glyph = Shape("Neutral mark " + mark, count.transform, new Vector3(points[mark].x, 1.02f + points[mark].y, .195f),
                            new Vector3(.14f, .14f, .045f), marksMaterial);
                        glyph.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    }
                    EchoSceneAuthoring.Set(stone, "_puzzle", puzzle);
                    EchoSceneAuthoring.Set(stone, "_sequenceValue", order[index]);
                    EchoSceneAuthoring.Set(stone, "_visual", visual);
                    Debug.Log("GLYPH_AUTHORED value=" + order[index] + " position=" + go.transform.position);
                }
                EchoSceneAuthoring.Set(puzzle, "_stones", stones.Cast<UnityEngine.Object>().ToArray());
                var board = At("Seven-place progress frieze", zone.Find("GlyphWallAnchor"), zone.Find("GlyphWallAnchor").position + new Vector3(1.75f, 2.45f, .65f));
                var frame = Shape("Frieze stone", board.transform, Vector3.zero, new Vector3(2.9f, .5f, .18f), stoneMaterial);
                var progressMarks = new Renderer[7];
                for (int index = 0; index < 7; index++)
                    progressMarks[index] = Shape("Progress mark " + index, board.transform, new Vector3((index - 3) * .35f, 0, .12f),
                        new Vector3(.18f, .18f, .055f), marksMaterial);
                var feedback = host.AddComponent<GlyphProgressFeedback>();
                EchoSceneAuthoring.Set(feedback, "_puzzle", puzzle);
                EchoSceneAuthoring.Set(feedback, "_marks", progressMarks.Cast<UnityEngine.Object>().ToArray());
                EchoSceneAuthoring.Set(feedback, "_frame", frame);
                var definition = AssetDatabase.LoadAssetAtPath<PuzzleDefinition>("Assets/_Project/Data/Puzzles/level01.glyph_gallery.asset");
                EchoSceneAuthoring.Set(definition, "_displayName", "Galería de glifos");
                var clue = AssetDatabase.LoadAssetAtPath<ClueDefinition>("Assets/_Project/Data/Clues/level01.glyph_gallery.asset");
                EchoSceneAuthoring.Set(clue, "_id", "level01.clue04");
                EchoSceneAuthoring.Set(clue, "_title", "El séptimo lugar");
                EchoSceneAuthoring.Set(clue, "_description", "La respuesta ocupa el séptimo lugar entre las letras.");
                var reward = At("Glyph clue reward", zone.Find("ClueAnchor"), zone.Find("ClueAnchor").position).AddComponent<PuzzleClueReward>();
                EchoSceneAuthoring.Set(reward, "_puzzle", puzzle);
                EchoSceneAuthoring.Set(reward, "_tracker", root.GetComponentInChildren<ClueTracker>());
                EchoSceneAuthoring.Set(reward, "_clue", clue);
                InscriptionSceneAuthoring.Build(zone, "Glyph guidance inscription", new Vector3(2.8f, 1.6f, 36.4f), "Cada marca sigue a la anterior.");
                var presenter = root.GetComponentInChildren<InscriptionPresenter>(true);
                EchoSceneAuthoring.Set(presenter, "_inscriptions", root.GetComponentsInChildren<ReadableInscription>(true).Cast<UnityEngine.Object>().ToArray());
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_GLYPH_PUZZLE_AUTHORED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        public static void RefineLayout()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var first = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GlyphStone>(true))
                    .Single(stone => stone.SequenceValue == 3);
                first.transform.position = new Vector3(-7.35f, 1.6f, 35.35f);
                var zone = scene.GetRootGameObjects().Single().transform.Find("Environment/Zone04_GlyphGallery");
                zone.Find("GlyphWallAnchor/Seven-place progress frieze").position = new Vector3(-3.25f, 4.05f, 33.65f);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("TIKAL_GLYPH_LAYOUT_REFINED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        public static void Preview()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
                var camera = new GameObject("Temporary glyph preview").AddComponent<Camera>();
                camera.transform.position = new Vector3(-3.2f, 4.6f, 42);
                camera.transform.LookAt(new Vector3(-3.2f, 2.95f, 34.8f));
                camera.fieldOfView = 68;
                var target = new RenderTexture(1600, 1000, 24);
                target.Create();
                for (int index = 0; index < 2; index++)
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                        new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "TikalGlyphOverview.png"), image.EncodeToPNG());
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                Debug.Log("TIKAL_GLYPH_PREVIEW_RENDERED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
