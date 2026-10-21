using UnityEngine;

namespace Tikal.Core
{
    // Serialized visual references for the existing catalog, not another selection system.
    public sealed class CharacterVisualAssets : ScriptableObject
    {
        [SerializeField] private GameObject _dwightVisualPrefab;
        [SerializeField] private GameObject _axelVisualPrefab;
        [SerializeField] private GameObject _marioVisualPrefab;
        public GameObject DwightVisualPrefab => _dwightVisualPrefab;
        public GameObject AxelVisualPrefab => _axelVisualPrefab;
        public GameObject MarioVisualPrefab => _marioVisualPrefab;
    }
}
