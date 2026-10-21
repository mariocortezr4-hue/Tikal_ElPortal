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
        private GameObject _currentPrefab;
        public bool HasVisual => _currentVisual != null;
        public GameObject CurrentVisual => _currentVisual;

        private void Awake()
        {
            Apply(new GamePersistenceService().LoadProgress().SelectedCharacterId);
        }

        public void Apply(string characterId)
        {
            var definition = CharacterCatalog.GetOrDefault(characterId);
            if (definition.IsLocked) return;
            if (_currentVisual != null && _currentPrefab == definition.VisualPrefab) return;
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
                if (Application.isPlaying) Destroy(_currentVisual);
                else DestroyImmediate(_currentVisual);
            }
            _currentVisual = null;
            _currentPrefab = definition.VisualPrefab;
            var animation = GetComponent<PlayerAnimationController>();
            if (_characterVisualRoot == null || definition.VisualPrefab == null)
            {
                if (animation != null) animation.Attach(null);
                GetComponent<PlayerCameraController>()?.RefreshCharacterVisualVisibility();
                return;
            }
            _currentVisual = Instantiate(definition.VisualPrefab, _characterVisualRoot);
            _currentVisual.name = definition.Id;
            var animator = _currentVisual.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                if (animation == null) animation = gameObject.AddComponent<PlayerAnimationController>();
                animation.Attach(animator);
            }
            else if (animation != null) animation.Attach(null);
            GetComponent<PlayerCameraController>()?.RefreshCharacterVisualVisibility();
        }
    }
}
