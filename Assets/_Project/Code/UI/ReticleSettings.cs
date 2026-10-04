using System;
using UnityEngine;

namespace Tikal.UI
{
    [DisallowMultipleComponent]
    public sealed class ReticleSettings : MonoBehaviour
    {
        [SerializeField] private ReticleMode _mode = ReticleMode.Dot;
        public ReticleMode Mode => _mode;
        public event Action<ReticleMode> ModeChanged;

        /// <summary>Future settings or persistence can apply a mode through this state source.</summary>
        public void SetMode(ReticleMode mode)
        {
            if (mode != ReticleMode.None && mode != ReticleMode.Dot && mode != ReticleMode.Cross)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (_mode == mode) return;
            _mode = mode;
            ModeChanged?.Invoke(mode);
        }

        private void OnValidate()
        {
            if (_mode != ReticleMode.None && _mode != ReticleMode.Dot && _mode != ReticleMode.Cross)
                _mode = ReticleMode.Dot;
            ModeChanged?.Invoke(_mode);
        }
    }
}
