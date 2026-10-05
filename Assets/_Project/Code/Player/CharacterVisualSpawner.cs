using Tikal.Core;
using Tikal.Persistence;
using UnityEngine;

namespace Tikal.Player
{
    [DisallowMultipleComponent]
    public sealed class CharacterVisualSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _characterVisualRoot;

        private GameObject _currentVisual;

        private void Awake()
        {
            Apply(new GamePersistenceService().LoadProgress().SelectedCharacterId);
        }

        public void Apply(string characterId)
        {
            var definition = CharacterCatalog.GetOrDefault(characterId);
            if (_currentVisual != null)
            {
                if (Application.isPlaying) Destroy(_currentVisual);
                else DestroyImmediate(_currentVisual);
            }
            if (_characterVisualRoot == null || definition.VisualPrefab == null) return;
            _currentVisual = Instantiate(definition.VisualPrefab, _characterVisualRoot);
            _currentVisual.name = definition.Id;
        }
    }
}
