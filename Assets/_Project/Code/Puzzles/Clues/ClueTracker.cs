using System;
using System.Collections.Generic;
using UnityEngine;
namespace Tikal.Puzzles.Clues
{
    [DisallowMultipleComponent]
    public sealed class ClueTracker : MonoBehaviour
    {
        private readonly Dictionary<string, ClueDefinition> _discovered = new Dictionary<string, ClueDefinition>(StringComparer.Ordinal);
        public int Count => _discovered.Count;
        public event Action<ClueDefinition> ClueDiscovered;
        public bool HasClue(string id) => !string.IsNullOrWhiteSpace(id) && _discovered.ContainsKey(id);
        public bool Discover(ClueDefinition clue)
        {
            if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || _discovered.ContainsKey(clue.Id)) return false;
            _discovered.Add(clue.Id, clue);
            ClueDiscovered?.Invoke(clue);
            return true;
        }
    }
}
