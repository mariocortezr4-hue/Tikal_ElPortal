using Tikal.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        private static bool _introReplayToMenu;
        private static bool _tutorialReplayToMenu;
        private GamePersistenceService _persistence;
        private GameProgressData _progress;

        public GameProgressData Progress => _progress ??= Persistence.LoadProgress();
        public GamePersistenceService Persistence => _persistence ??= new GamePersistenceService();

        private void Awake()
        {
            _persistence = new GamePersistenceService();
            _progress = _persistence.LoadProgress();
        }

        public string PlayButtonLabel => Progress.HasStartedGame ? "CONTINUAR" : "JUGAR";
        public string CurrentLevelTitle => SceneCatalog.TryGet(Progress.CurrentLevelId, out var info) ? info.Title : SceneCatalog.Get(GameSceneIds.Level01).Title;
        public bool IsIntroReplay => _introReplayToMenu;
        public bool IsTransitioning { get; private set; }

        public void ReturnToMainMenu()
        {
            _introReplayToMenu = _tutorialReplayToMenu = false;
            Time.timeScale = 1;
            Load(GameSceneIds.MainMenu);
        }

        public void RestartCurrentGameplayScene()
        {
            IsTransitioning = true;
            Time.timeScale = 1;
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }

        public void PlayOrContinue()
        {
            if (!Progress.HasStartedGame)
            {
                Progress.HasStartedGame = true;
                if (string.IsNullOrWhiteSpace(Progress.CurrentLevelId)) Progress.CurrentLevelId = GameSceneIds.Level01;
                Progress.UnlockLevel(GameSceneIds.Level01);
                Persistence.SaveProgress(Progress);
            }
            if (!Progress.IntroSeen)
            {
                Load(GameSceneIds.Intro);
                return;
            }
            if (!Progress.TutorialCompleted)
            {
                Load(GameSceneIds.Tutorial);
                return;
            }
            LaunchLevel(string.IsNullOrWhiteSpace(Progress.CurrentLevelId) ? GameSceneIds.Level01 : Progress.CurrentLevelId);
        }

        public void StartNewGame()
        {
            Progress.HasStartedGame = true;
            Progress.IntroSeen = false;
            Progress.TutorialCompleted = false;
            Progress.CurrentLevelId = GameSceneIds.Level01;
            Progress.UnlockLevel(GameSceneIds.Level01);
            Persistence.SaveProgress(Progress);
            Load(GameSceneIds.Intro);
        }

        public void ReplayIntro()
        {
            _introReplayToMenu = true;
            Load(GameSceneIds.Intro);
        }

        public void LaunchTutorial()
        {
            _tutorialReplayToMenu = true;
            Load(GameSceneIds.Tutorial);
        }

        public void LaunchLevel(string levelId)
        {
            if (!CanLaunchLevel(levelId)) return;
            Load(levelId);
        }

        public bool CanLaunchLevel(string levelId) => Progress.HasCompletedOnboarding && Progress.IsLevelUnlocked(levelId);

        public void CompleteIntro()
        {
            if (_introReplayToMenu)
            {
                _introReplayToMenu = false;
                Load(GetIntroCompletionDestination(true));
                return;
            }
            Progress.IntroSeen = true;
            Persistence.SaveProgress(Progress);
            Load(GetIntroCompletionDestination(false));
        }

        public static string GetIntroCompletionDestination(bool replay) =>
            replay ? GameSceneIds.MainMenu : GameSceneIds.Tutorial;

        public void CompleteTutorial()
        {
            if (_tutorialReplayToMenu)
            {
                _tutorialReplayToMenu = false;
                Load(GetTutorialCompletionDestination(true));
                return;
            }
            Progress.TutorialCompleted = true;
            Progress.CurrentLevelId = GameSceneIds.Level01;
            Progress.UnlockLevel(GameSceneIds.Level01);
            Persistence.SaveProgress(Progress);
            Load(GetTutorialCompletionDestination(false));
        }

        public static string GetTutorialCompletionDestination(bool replay) =>
            replay ? GameSceneIds.MainMenu : GameSceneIds.Level01;

        public void CompleteLevel(string levelId)
        {
            Progress.CompleteLevel(levelId);
            if (string.Equals(levelId, GameSceneIds.Level01, System.StringComparison.Ordinal))
                Progress.UnlockLevel(GameSceneIds.Level02);
            Persistence.SaveProgress(Progress);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested from Main Menu.");
#else
            Application.Quit();
#endif
        }

        private void Load(string sceneId)
        {
            var scene = SceneCatalog.Get(sceneId);
            if (string.IsNullOrWhiteSpace(scene.Path))
            {
                Debug.LogWarning($"Scene '{sceneId}' is not implemented yet.");
                return;
            }
            IsTransitioning = true;
            SceneManager.LoadScene(scene.SceneName, LoadSceneMode.Single);
        }
    }
}
