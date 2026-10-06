using System;
using System.IO;
using System.Linq;
using Tikal.Core;
using Tikal.Input;
using Tikal.Player;
using Tikal.Tutorial;
using Tikal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Tests.Editor
{
    public static class TutorialExplorerBasicsAuthoring
    {
        const string ScenePath = "Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity";
        const string PlayerPath = "Assets/_Project/Prefabs/Player/Explorer.prefab";
        const string PromptPath = "Assets/_Project/Prefabs/UI/InteractionPrompt.prefab";
        const string ReticlePath = "Assets/_Project/Prefabs/UI/Reticle.prefab";

        [MenuItem("Tikal/Authoring/Build Explorer Tutorial")]
        public static void RunFromMenu()
        {
            try { Build(); Debug.Log("TIKAL_EXPLORER_TUTORIAL_AUTHORED"); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/_Project/Scenes/Tutorial");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TUT_ExplorerBasics");
            var flow = root.AddComponent<GameFlowController>();
            var environment = new GameObject("Environment").transform;
            environment.SetParent(root.transform, false);
            var floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/Floor.mat");
            var route = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/Route.mat");
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/Stone.mat");
            var vegetation = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Level01/VegetationMediumProxy.mat");
            Ground(environment, floor, route);
            Obstacles(environment, stone, vegetation);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
            player.name = "Explorer";
            player.transform.position = new Vector3(0, .05f, 0);
            var input = player.GetComponent<PlayerInputReader>();
            var movement = player.GetComponent<PlayerMovement>();
            var look = player.GetComponent<PlayerLook>();
            var devices = player.GetComponent<InputDeviceManager>();
            var canvas = Canvas(root.transform);
            var reticle = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReticlePath), canvas.transform);
            var reticleSettings = reticle.GetComponent<ReticleSettings>();
            EchoSceneAuthoring.Set(reticle.GetComponent<ReticlePresenter>(), "_inputReader", input);
            var interactionUi = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PromptPath), canvas.transform);
            var interaction = player.GetComponent<Tikal.Interaction.PlayerInteraction>();
            var interactionPresenter = interactionUi.GetComponent<InteractionPromptPresenter>();
            EchoSceneAuthoring.Set(interactionPresenter, "_interaction", interaction);
            EchoSceneAuthoring.Set(interactionPresenter, "_deviceManager", devices);
            EchoSceneAuthoring.Set(interactionPresenter, "_inputReader", input);
            var settings = root.AddComponent<Tikal.Persistence.PersistentSettingsApplier>();
            EchoSceneAuthoring.Set(settings, "_reticleSettings", reticleSettings);
            EchoSceneAuthoring.Set(settings, "_playerLook", look);

            var sequenceRoot = new GameObject("TutorialSequence");
            sequenceRoot.transform.SetParent(root.transform, false);
            var sequence = sequenceRoot.AddComponent<TutorialSequenceController>();
            var gate = Gate(environment, stone, player.transform);
            gate.SetInteractionAvailable(false);
            var crouch = CrouchPassage(environment, stone, player.transform, movement);
            var steps = CreateSteps(sequenceRoot.transform, player.transform, look, input, movement, crouch, gate);
            EchoSceneAuthoring.Set(sequence, "_flow", flow);
            EchoSceneAuthoring.Set(sequence, "_steps", steps.Cast<UnityEngine.Object>().ToArray());
            EchoSceneAuthoring.Set(sequence, "_exitGate", gate);
            BuildObjectiveUi(canvas.transform, sequence, input, devices);
            ExitGuidance(environment, route, stone);
            Light(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            AssetDatabase.SaveAssets();
        }

        static void Ground(Transform parent, Material floor, Material route)
        {
            Shape("Jungle floor", parent, PrimitiveType.Cube, new Vector3(0, -.3f, 23), new Vector3(28, .6f, 60), floor);
            Shape("Expedition path", parent, PrimitiveType.Cube, new Vector3(0, .03f, 23), new Vector3(4.6f, .08f, 54), route);
        }

        static void Obstacles(Transform parent, Material stone, Material vegetation)
        {
            Shape("Jump root", parent, PrimitiveType.Cube, new Vector3(0, .32f, 11), new Vector3(3.5f, .6f, .7f), stone);
            Shape("Low ruin lintel", parent, PrimitiveType.Cube, new Vector3(0, 1.55f, 19), new Vector3(4.2f, .45f, 1.2f), stone);
            Shape("Ruin left", parent, PrimitiveType.Cube, new Vector3(-2.15f, .75f, 19), new Vector3(.45f, 1.5f, 1.2f), stone);
            Shape("Ruin right", parent, PrimitiveType.Cube, new Vector3(2.15f, .75f, 19), new Vector3(.45f, 1.5f, 1.2f), stone);
            for (var index = 0; index < 20; index++)
            {
                var side = index % 2 == 0 ? -1 : 1;
                var z = 2 + index * 2.35f;
                Shape("Vegetation proxy " + index, parent, PrimitiveType.Cylinder, new Vector3(side * (3.8f + (index % 3)), .9f, z), new Vector3(.45f, 1.8f, .45f), vegetation);
            }
        }

        static TutorialCrouchPassageStep CrouchPassage(Transform parent, Material stone, Transform player, PlayerMovement movement)
        {
            var zone = new GameObject("CrouchPassageZone", typeof(BoxCollider));
            zone.transform.SetParent(parent, false);
            zone.transform.position = new Vector3(0, .8f, 19);
            var collider = zone.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3.2f, 1.5f, 2.6f);
            var step = zone.AddComponent<TutorialCrouchPassageStep>();
            EchoSceneAuthoring.Set(step, "_player", player);
            EchoSceneAuthoring.Set(step, "_movement", movement);
            return step;
        }

        static TutorialExitGate Gate(Transform parent, Material stone, Transform player)
        {
            var gate = new GameObject("ExpeditionExitGate");
            gate.transform.SetParent(parent, false);
            gate.transform.position = new Vector3(0, 0, 36);
            var stoneBlock = Shape("Lift stone", gate.transform, PrimitiveType.Cube, new Vector3(0, 1.4f, 0), new Vector3(3.6f, 2.8f, .7f), stone);
            var component = gate.AddComponent<TutorialExitGate>();
            var exitTrigger = gate.GetComponent<BoxCollider>();
            exitTrigger.isTrigger = true;
            exitTrigger.center = new Vector3(0, 1.25f, 3.5f);
            exitTrigger.size = new Vector3(4f, 2.5f, 1.5f);
            EchoSceneAuthoring.Set(component, "_movingStone", stoneBlock.transform);
            EchoSceneAuthoring.Set(component, "_player", player);
            return component;
        }

        static void ExitGuidance(Transform parent, Material route, Material stone)
        {
            Shape("Exit path highlight", parent, PrimitiveType.Cube, new Vector3(0, .09f, 42), new Vector3(3.8f, .04f, 11f), route);
            Shape("Exit marker left", parent, PrimitiveType.Cube, new Vector3(-2.1f, 1.1f, 42), new Vector3(.35f, 2.2f, .35f), stone);
            Shape("Exit marker right", parent, PrimitiveType.Cube, new Vector3(2.1f, 1.1f, 42), new Vector3(.35f, 2.2f, .35f), stone);
            var beacon = new GameObject("Warm exit beacon", typeof(Light));
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = new Vector3(0, 2.7f, 43);
            var light = beacon.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 13f;
            light.intensity = 4.5f;
            light.color = new Color(1f, .64f, .3f);
        }

        static TutorialStep[] CreateSteps(Transform parent, Transform player, PlayerLook look, PlayerInputReader input, PlayerMovement movement, TutorialCrouchPassageStep crouch, TutorialExitGate gate)
        {
            var move = Component<TutorialMovementStep>("01_Move", parent, "MUÉVETE", "Avanza por el sendero.", TutorialPromptAction.Move);
            EchoSceneAuthoring.Set(move, "_player", player);
            var observe = Component<TutorialLookStep>("02_Look", parent, "OBSERVA TU ENTORNO", "Mira a tu alrededor antes de continuar.", TutorialPromptAction.Look);
            EchoSceneAuthoring.Set(observe, "_player", player); EchoSceneAuthoring.Set(observe, "_look", look);
            var jump = Component<TutorialJumpStep>("03_Jump", parent, "SALTA", "Supera el obstáculo.", TutorialPromptAction.Jump);
            EchoSceneAuthoring.Set(jump, "_input", input);
            EchoSceneAuthoring.Set(jump, "_player", player);
            jump.transform.position = new Vector3(0, 1.1f, 10.5f);
            var jumpZone = jump.GetComponent<BoxCollider>();
            jumpZone.isTrigger = true;
            jumpZone.size = new Vector3(4.5f, 2.4f, 5f);
            SetBase(crouch, "AGÁCHATE", "Pasa bajo la piedra caída.", TutorialPromptAction.Crouch);
            var sprint = Component<TutorialSprintStep>("05_Sprint", parent, "CORRE", "Cruza el tramo abierto con rapidez.", TutorialPromptAction.Sprint);
            EchoSceneAuthoring.Set(sprint, "_player", player); EchoSceneAuthoring.Set(sprint, "_movement", movement);
            var interact = Component<TutorialInteractStep>("06_Interact", parent, "INTERACTÚA", "Abre el paso hacia la entrada.", TutorialPromptAction.Interact);
            EchoSceneAuthoring.Set(interact, "_gate", gate);
            return new TutorialStep[] { move, observe, jump, crouch, sprint, interact };
        }

        static T Component<T>(string name, Transform parent, string title, string instruction, TutorialPromptAction prompt) where T : TutorialStep
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var step = go.AddComponent<T>(); SetBase(step, title, instruction, prompt); return step;
        }

        static void SetBase(TutorialStep step, string title, string instruction, TutorialPromptAction prompt)
        {
            EchoSceneAuthoring.Set(step, "_title", title); EchoSceneAuthoring.Set(step, "_instruction", instruction); EchoSceneAuthoring.Set(step, "_promptAction", (int)prompt);
        }

        static void BuildObjectiveUi(Transform canvas, TutorialSequenceController sequence, PlayerInputReader input, InputDeviceManager devices)
        {
            var root = Panel("TutorialObjective", canvas, new Vector2(.5f, 0), new Vector2(0, 95), new Vector2(500, 112));
            var title = Text("Title", root.transform, 22, new Vector2(22, 68), new Vector2(440, 28));
            var control = Text("Control", root.transform, 18, new Vector2(22, 39), new Vector2(440, 24));
            var instruction = Text("Instruction", root.transform, 17, new Vector2(22, 14), new Vector2(440, 22));
            var complete = Panel("TutorialComplete", canvas, new Vector2(.5f, 0), new Vector2(0, 95), new Vector2(500, 58));
            var completion = Text("Label", complete.transform, 19, new Vector2(22, 17), new Vector2(440, 26));
            complete.SetActive(false);
            var fade = Panel("TutorialFade", canvas, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920, 1080));
            fade.GetComponent<Image>().color = Color.black;
            var fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0;
            fade.SetActive(false);
            var presenter = canvas.gameObject.AddComponent<TutorialPromptPresenter>();
            EchoSceneAuthoring.Set(presenter, "_sequence", sequence); EchoSceneAuthoring.Set(presenter, "_input", input); EchoSceneAuthoring.Set(presenter, "_devices", devices);
            EchoSceneAuthoring.Set(presenter, "_objectiveRoot", root); EchoSceneAuthoring.Set(presenter, "_title", title); EchoSceneAuthoring.Set(presenter, "_control", control); EchoSceneAuthoring.Set(presenter, "_instruction", instruction);
            EchoSceneAuthoring.Set(presenter, "_completionRoot", complete); EchoSceneAuthoring.Set(presenter, "_completion", completion);
            EchoSceneAuthoring.Set(presenter, "_fadeOverlay", fadeGroup);
        }

        static Canvas Canvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = new Color(.018f, .035f, .055f, .9f); image.raycastTarget = false; return go;
        }

        static TMP_Text Text(string name, Transform parent, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 0); rect.pivot = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<TMP_Text>(); text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/InteractionPromptFont.asset"); text.fontSize = size; text.color = new Color(.96f, .93f, .84f); text.alignment = TextAlignmentOptions.Left; text.raycastTarget = false; return text;
        }

        static Renderer Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; return go.GetComponent<Renderer>();
        }

        static void Light(Transform parent)
        {
            var sun = new GameObject("Jungle Sun", typeof(Light)); sun.transform.SetParent(parent, false); sun.transform.rotation = Quaternion.Euler(42, -28, 0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f; light.color = new Color(1f, .88f, .67f);
            RenderSettings.ambientLight = new Color(.24f, .34f, .31f);
        }

        static void RegisterScene()
        {
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath && item.path != "Assets/_Project/Scenes/Core/TutorialPlaceholder.unity").ToList();
            var levelIndex = scenes.FindIndex(item => item.path == "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity");
            scenes.Insert(levelIndex < 0 ? scenes.Count : levelIndex, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
