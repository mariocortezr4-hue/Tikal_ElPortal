using UnityEngine;
namespace Tikal.Puzzles
{
    [CreateAssetMenu(menuName = "Tikal/Puzzles/Puzzle Definition")]
    public sealed class PuzzleDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        public string Id => _id;
        public string DisplayName => _displayName;
        private void OnValidate() { _id = _id?.Trim(); }
    }
}
