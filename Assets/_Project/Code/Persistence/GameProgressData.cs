using System;
using System.Collections.Generic;
using Tikal.Core;
using UnityEngine;

namespace Tikal.Persistence
{
    [Serializable]
    public sealed class GameProgressData
    {
        [SerializeField] private bool _hasStartedGame;
        [SerializeField] private bool _introSeen;
        [SerializeField] private bool _tutorialCompleted;
        [SerializeField] private string _currentLevelId;
        [SerializeField] private string _selectedCharacterId;
        [SerializeField] private List<string> _unlockedLevels = new List<string>();
        [SerializeField] private List<string> _completedLevels = new List<string>();

        public bool HasStartedGame { get => _hasStartedGame; set => _hasStartedGame = value; }
        public bool IntroSeen { get => _introSeen; set => _introSeen = value; }
        public bool TutorialCompleted { get => _tutorialCompleted; set => _tutorialCompleted = value; }
        public string CurrentLevelId { get => _currentLevelId; set => _currentLevelId = value ?? string.Empty; }
        public string SelectedCharacterId { get => _selectedCharacterId; set => _selectedCharacterId = CharacterCatalog.IsValid(value) ? value : CharacterIds.Default; }
        public bool HasCompletedOnboarding => _introSeen && _tutorialCompleted;
        public IReadOnlyList<string> UnlockedLevels => _unlockedLevels;
        public IReadOnlyList<string> CompletedLevels => _completedLevels;

        public static GameProgressData CreateDefault()
        {
            var data = new GameProgressData
            {
                _hasStartedGame = false,
                _introSeen = false,
                _tutorialCompleted = false,
                _currentLevelId = string.Empty,
                _selectedCharacterId = CharacterIds.Default
            };
            data.UnlockLevel(GameSceneIds.Level01);
            return data;
        }

        public bool IsLevelUnlocked(string levelId) => Contains(_unlockedLevels, levelId);
        public bool IsLevelCompleted(string levelId) => Contains(_completedLevels, levelId);
        public bool UnlockLevel(string levelId) => AddUnique(_unlockedLevels, levelId);
        public bool CompleteLevel(string levelId)
        {
            UnlockLevel(levelId);
            return AddUnique(_completedLevels, levelId);
        }

        public void Normalize()
        {
            _currentLevelId ??= string.Empty;
            SelectedCharacterId = _selectedCharacterId;
            NormalizeList(_unlockedLevels);
            NormalizeList(_completedLevels);
            UnlockLevel(GameSceneIds.Level01);
        }

        private static bool Contains(List<string> list, string value) =>
            !string.IsNullOrWhiteSpace(value) && list.Exists(item => string.Equals(item, value, StringComparison.Ordinal));

        private static bool AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Contains(list, value)) return false;
            list.Add(value);
            return true;
        }

        private static void NormalizeList(List<string> list)
        {
            for (int index = list.Count - 1; index >= 0; index--)
            {
                list[index] = list[index]?.Trim();
                if (string.IsNullOrEmpty(list[index]) || list.FindIndex(item => string.Equals(item, list[index], StringComparison.Ordinal)) != index)
                    list.RemoveAt(index);
            }
        }
    }
}
