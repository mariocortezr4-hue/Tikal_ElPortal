using UnityEngine;

namespace Tikal.Core
{
    // Serialized visual references for the existing catalog, not another selection system.
    public sealed class CharacterVisualAssets : ScriptableObject
    {
        [SerializeField] private GameObject _marioVisualPrefab;
        public GameObject MarioVisualPrefab => _marioVisualPrefab;
    }
}
