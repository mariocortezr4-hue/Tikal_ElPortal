using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tikal.Core
{
    public readonly struct CharacterDefinition
    {
        public CharacterDefinition(string id, string displayName, GameObject visualPrefab = null, Sprite portrait = null, bool isLocked = false)
        {
            Id = id;
            DisplayName = displayName;
            VisualPrefab = visualPrefab;
            Portrait = portrait;
            IsLocked = isLocked;
        }

        public bool IsLocked { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public GameObject VisualPrefab { get; }
        public Sprite Portrait { get; }
    }

    public static class CharacterCatalog
    {
        private static CharacterDefinition[] Characters => new CharacterDefinition[]
        {
            new CharacterDefinition(CharacterIds.Default, "Dwight", Resources.Load<CharacterVisualAssets>("Characters/GameplayCharacterVisuals")?.DwightVisualPrefab),
            new CharacterDefinition(CharacterIds.Character02, "Axel", Resources.Load<CharacterVisualAssets>("Characters/GameplayCharacterVisuals")?.AxelVisualPrefab),
            new CharacterDefinition(CharacterIds.Character03, "Mario", Resources.Load<CharacterVisualAssets>("Characters/GameplayCharacterVisuals")?.MarioVisualPrefab),
            new CharacterDefinition(CharacterIds.Character04, "DESCONOCIDO", isLocked: true)
        };

        public static IReadOnlyList<CharacterDefinition> All => Characters;

        public static CharacterDefinition Default => Characters[0];

        public static bool IsSelectable(string id) => TryGet(id, out var character) && !character.IsLocked;

        public static bool IsValid(string id) => TryGet(id, out _);

        public static bool TryGet(string id, out CharacterDefinition definition)
        {
            foreach (var character in Characters)
                if (string.Equals(character.Id, id, StringComparison.Ordinal))
                {
                    definition = character;
                    return true;
                }
            definition = default;
            return false;
        }

        public static CharacterDefinition GetOrDefault(string id) => TryGet(id, out var definition) ? definition : Default;
    }
}
