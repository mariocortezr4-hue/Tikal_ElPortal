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

        public void PlayOrContinue()
        {
            if (!Progress.HasStartedGame)
            {
                StartNewGame();
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
            if (!Progress.IsLevelUnlocked(levelId)) return;
            Load(levelId);
        }

        public void CompleteIntro()
        {
            if (_introReplayToMenu)
            {
                _introReplayToMenu = false;
                Load(GameSceneIds.MainMenu);
                return;
            }
            Progress.IntroSeen = true;
            Persistence.SaveProgress(Progress);
            Load(GameSceneIds.Tutorial);
        }

        public void CompleteTutorial()
        {
            if (_tutorialReplayToMenu)
            {
                _tutorialReplayToMenu = false;
                Load(GameSceneIds.MainMenu);
                return;
            }
            Progress.TutorialCompleted = true;
            Progress.CurrentLevelId = GameSceneIds.Level01;
            Progress.UnlockLevel(GameSceneIds.Level01);
            Persistence.SaveProgress(Progress);
            Load(GameSceneIds.Level01);
        }

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

        private static void Load(string sceneId)
        {
            var scene = SceneCatalog.Get(sceneId);
            if (string.IsNullOrWhiteSpace(scene.Path))
            {
                Debug.LogWarning($"Scene '{sceneId}' is not implemented yet.");
                return;
            }
            SceneManager.LoadScene(scene.SceneName, LoadSceneMode.Single);
        }
    }
}
