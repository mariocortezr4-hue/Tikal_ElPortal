using UnityEngine;
namespace Tikal.Puzzles.Clues
{
    public enum ClueSemanticContent { Narrative, SecretCommand }
    [CreateAssetMenu(menuName = "Tikal/Puzzles/Clue Definition")]
    public sealed class ClueDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private int _order;
        [SerializeField] private ClueSemanticContent _semanticContent;
        public ClueSemanticContent SemanticContent => _semanticContent;
        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public int Order => _order;
        private void OnValidate() { _id = _id?.Trim(); }
    }
}
