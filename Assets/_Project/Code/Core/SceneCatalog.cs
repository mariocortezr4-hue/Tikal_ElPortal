using System;
using System.Collections.Generic;

namespace Tikal.Core
{
    public readonly struct SceneInfo
    {
        public SceneInfo(string id, string path, string title, bool level)
        {
            Id = id;
            Path = path;
            Title = title;
            IsLevel = level;
        }

        public string Id { get; }
        public string Path { get; }
        public string Title { get; }
        public bool IsLevel { get; }
        public string SceneName => System.IO.Path.GetFileNameWithoutExtension(Path);
    }

    public static class SceneCatalog
    {
        private static readonly SceneInfo[] Scenes =
        {
            new SceneInfo(GameSceneIds.MainMenu, "Assets/_Project/Scenes/Menus/MainMenu.unity", "Menu principal", false),
            new SceneInfo(GameSceneIds.Intro, "Assets/_Project/Scenes/Core/IntroPlaceholder.unity", "Introduccion", false),
            new SceneInfo(GameSceneIds.Tutorial, "Assets/_Project/Scenes/Core/TutorialPlaceholder.unity", "Tutorial", false),
            new SceneInfo(GameSceneIds.Level01, "Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity", "Nivel 1 - La Entrada Secreta", true),
            new SceneInfo(GameSceneIds.Level02, string.Empty, "Nivel 2", true)
        };

        public static IReadOnlyList<SceneInfo> All => Scenes;

        public static bool TryGet(string id, out SceneInfo info)
        {
            foreach (var scene in Scenes)
                if (string.Equals(scene.Id, id, StringComparison.Ordinal))
                {
                    info = scene;
                    return true;
                }
            info = default;
            return false;
        }

        public static SceneInfo Get(string id)
        {
            if (TryGet(id, out var info)) return info;
            throw new ArgumentException($"Unknown scene id '{id}'.", nameof(id));
        }
    }
}
