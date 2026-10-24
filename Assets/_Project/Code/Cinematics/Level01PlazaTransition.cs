using System.Collections;
using Tikal.Core;
using Tikal.Input;
using Tikal.Persistence;
using Tikal.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tikal.Cinematics
{
    // Owns only the two fades across scene loads. No gameplay or selection rules live here.
    public sealed class Level01PlazaTransition : MonoBehaviour
    {
        public const string SourceScene = "Level01_SecretEntrance";
        public const string CinematicScene = "Transition_Level01_PlazaKeynes";
        public const string DestinationScene = "Level02_PlazaKeynes";
        public static Level01PlazaTransition Active { get; private set; }
        public string CharacterId { get; private set; }
        public CanvasGroup Fade { get; private set; }
        bool _arriving;
        static LevelOutcomeController _secretVictory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; _secretVictory = null; }

        public static bool TriggerSecretVictory(LevelOutcomeController outcome)
        {
            _secretVictory = outcome;
            try { return outcome.TriggerVictory(); }
            finally { _secretVictory = null; }
        }

        public static bool TryBegin(LevelOutcomeController outcome)
        {
            if (outcome == null || outcome != _secretVictory || outcome.gameObject.scene.name != SourceScene ||
                outcome.State != LevelOutcome.Victory) return false;
            if (!Application.CanStreamedLevelBeLoaded(CinematicScene) ||
                !Application.CanStreamedLevelBeLoaded(DestinationScene))
            {
                Debug.LogError("Level01 transition scenes are missing from Build Settings; retaining the result screen.");
                return false;
            }
            if (Active != null) return true;
            var service = Create(0);
            service.StartCoroutine(service.Depart(outcome));
            return true;
        }

        public static Level01PlazaTransition ForCinematicPreview() => Active != null ? Active : Create(1);

        static Level01PlazaTransition Create(float opacity)
        {
            var go = new GameObject("Level01_Plaza_Transition");
            DontDestroyOnLoad(go);
            var service = go.AddComponent<Level01PlazaTransition>();
            Active = service;
            service.CharacterId = new GamePersistenceService().LoadProgress().SelectedCharacterId;
            var canvasObject = new GameObject("TransitionFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(go.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            service.Fade = canvasObject.GetComponent<CanvasGroup>();
            service.Fade.alpha = opacity;
            service.Fade.blocksRaycasts = true;
            var panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Color.black;
            return service;
        }

        IEnumerator Depart(LevelOutcomeController outcome)
        {
            Time.timeScale = 1;
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) player.SetGameplayActive(false);
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            // Existing completion API, preserving the selected character and all puzzle rules.
            var flow = FindAnyObjectByType<GameFlowController>();
            if (flow != null) flow.CompleteLevel(GameSceneIds.Level01);
            var audio = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            var volumes = new float[audio.Length];
            for (int i = 0; i < audio.Length; i++) volumes[i] = audio[i].volume;
            float elapsed = 0;
            while (elapsed < .75f)
            {
                elapsed += Time.unscaledDeltaTime;
                Fade.alpha = Mathf.SmoothStep(0, 1, elapsed / .75f);
                for (int i = 0; i < audio.Length; i++) if (audio[i] != null) audio[i].volume = volumes[i] * (1 - Fade.alpha);
                yield return null;
            }
            Fade.alpha = 1;
            yield return SceneManager.LoadSceneAsync(CinematicScene, LoadSceneMode.Single);
        }

        public void Arrive()
        {
            if (_arriving) return;
            _arriving = true;
            StartCoroutine(LoadPlaza());
        }

        IEnumerator LoadPlaza()
        {
            Fade.alpha = 1;
            // sceneLoaded runs after Awake/OnEnable and before the first controllable Update.
            SceneManager.sceneLoaded += LockArrival;
            yield return SceneManager.LoadSceneAsync(DestinationScene, LoadSceneMode.Single);
            SceneManager.sceneLoaded -= LockArrival;
            yield return null;
            var player = FindAnyObjectByType<PlayerController>();
            var reader = player != null ? player.GetComponent<PlayerInputReader>() : null;
            float elapsed = 0;
            while (elapsed < .8f)
            {
                elapsed += Time.unscaledDeltaTime;
                Fade.alpha = 1 - Mathf.SmoothStep(0, 1, elapsed / .8f);
                yield return null;
            }
            Fade.alpha = 0;
            if (reader != null) reader.enabled = true;
            if (player != null) player.SetGameplayActive(true);
            Active = null;
            Destroy(gameObject);
        }

        void LockArrival(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != DestinationScene) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) { Debug.LogError("Plaza Keynes has no existing Player."); return; }
            player.SetGameplayActive(false);
            player.GetComponent<PlayerInputReader>().enabled = false;
            player.GetComponent<CharacterVisualSpawner>()?.Apply(CharacterId);
            // Keep the validated Plaza Keynes spawn and orientation; do not move its environment.
        }
    }
}
