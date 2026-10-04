using System;
using System.Linq;
using Tikal.Input;
using Tikal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class GameplayComfortAuthoring
    {
        const string ScenePath = "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity";
        const string PrefabPath = "Assets/_Project/Prefabs/UI/Reticle.prefab";

        static GameObject Centre(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return go;
        }

        static void Shape(string name, Transform parent, Vector2 size, Color colour, Sprite sprite = null)
        {
            var image = Centre(name, parent, size).AddComponent<Image>();
            image.color = colour;
            image.sprite = sprite;
            image.raycastTarget = false;
        }

        public static void Run()
        {
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                    throw new Exception("Reticle already exists; refusing to replace it.");
                var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (circle == null) throw new Exception("Native Unity circle sprite is unavailable.");
                var root = Centre("Reticle", null, new Vector2(16, 16));
                try
                {
                    var settings = root.AddComponent<ReticleSettings>();
                    var presenter = root.AddComponent<ReticlePresenter>();
                    var dot = Centre("Dot", root.transform, new Vector2(8, 8));
                    var cross = Centre("Cross", root.transform, new Vector2(12, 12));
                    var dark = new Color(.025f, .03f, .025f, .95f);
                    var light = new Color(.94f, .95f, .9f, .98f);
                    Shape("Outline", dot.transform, new Vector2(7, 7), dark, circle);
                    Shape("Point", dot.transform, new Vector2(4, 4), light, circle);
                    Shape("Horizontal outline", cross.transform, new Vector2(12, 4), dark);
                    Shape("Vertical outline", cross.transform, new Vector2(4, 12), dark);
                    Shape("Horizontal", cross.transform, new Vector2(10, 2), light);
                    Shape("Vertical", cross.transform, new Vector2(2, 10), light);
                    JournalSceneAuthoring.Set(presenter, "_settings", settings);
                    JournalSceneAuthoring.Set(presenter, "_dot", dot);
                    JournalSceneAuthoring.Set(presenter, "_cross", cross);
                    cross.SetActive(false);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                var scene = EditorSceneManager.OpenScene(ScenePath);
                var all = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                if (all.OfType<ReticlePresenter>().Any()) throw new Exception("Scene already contains a reticle.");
                var canvas = all.OfType<Canvas>().Single();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform);
                var rect = (RectTransform)instance.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                JournalSceneAuthoring.Set(instance.GetComponent<ReticlePresenter>(), "_inputReader", all.OfType<PlayerInputReader>().Single());
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_GAMEPLAY_COMFORT_AUTHORED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        public static void RefineCross()
        {
            try
            {
                var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    var cross = prefab.transform.Find("Cross");
                    ((RectTransform)cross.Find("Horizontal outline")).sizeDelta = new Vector2(12, 4);
                    ((RectTransform)cross.Find("Vertical outline")).sizeDelta = new Vector2(4, 12);
                    ((RectTransform)cross.Find("Horizontal")).sizeDelta = new Vector2(10, 2);
                    ((RectTransform)cross.Find("Vertical")).sizeDelta = new Vector2(2, 10);
                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
                AssetDatabase.SaveAssets();
                Debug.Log("TIKAL_RETICLE_CROSS_REFINED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        public static void Preview()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                var all = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                var presenter = all.OfType<ReticlePresenter>().Single();
                var data = new SerializedObject(presenter);
                var dot = (GameObject)data.FindProperty("_dot").objectReferenceValue;
                var cross = (GameObject)data.FindProperty("_cross").objectReferenceValue;
                var canvas = all.OfType<Canvas>().Single();
                var camera = all.OfType<Camera>().Single();
                var target = new RenderTexture(1600, 900, 24);
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                foreach (var mode in new[] { ReticleMode.Dot, ReticleMode.Cross })
                {
                    dot.SetActive(mode == ReticleMode.Dot);
                    cross.SetActive(mode == ReticleMode.Cross);
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                        new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    image.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TikalReticle" + mode + ".png"), image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                RenderTexture.active = null;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                Debug.Log("TIKAL_RETICLE_PREVIEW_RENDERED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
