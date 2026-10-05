using System;
using System.IO;
using UnityEngine;

namespace Tikal.Persistence
{
    public sealed class GamePersistenceService
    {
        private readonly string _progressPath;
        private readonly string _settingsPath;

        public string ProgressPath => _progressPath;
        public string SettingsPath => _settingsPath;

        public GamePersistenceService(string rootPath = null)
        {
            var root = string.IsNullOrWhiteSpace(rootPath) ? Application.persistentDataPath : rootPath;
            _progressPath = Path.Combine(root, "tikal_progress.json");
            _settingsPath = Path.Combine(root, "tikal_settings.json");
        }

        public GameProgressData LoadProgress() => Load(_progressPath, GameProgressData.CreateDefault);
        public GameSettingsData LoadSettings() => Load(_settingsPath, GameSettingsData.CreateDefault);
        public void SaveProgress(GameProgressData data) => Save(_progressPath, data ?? GameProgressData.CreateDefault());
        public void SaveSettings(GameSettingsData data) => Save(_settingsPath, data ?? GameSettingsData.CreateDefault());
        public void ResetProgress() => SaveProgress(GameProgressData.CreateDefault());

        private static T Load<T>(string path, Func<T> fallback) where T : class
        {
            try
            {
                if (!File.Exists(path)) return Normalize(fallback());
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return Normalize(fallback());
                return Normalize(JsonUtility.FromJson<T>(json) ?? fallback());
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Failed to load persistence file '{path}'. Defaults will be used. {exception.Message}");
                return Normalize(fallback());
            }
        }

        private static T Normalize<T>(T data) where T : class
        {
            switch (data)
            {
                case GameProgressData progress:
                    progress.Normalize();
                    break;
                case GameSettingsData settings:
                    settings.Normalize();
                    break;
            }
            return data;
        }

        private static void Save<T>(string path, T data) where T : class
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Normalize(data);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
