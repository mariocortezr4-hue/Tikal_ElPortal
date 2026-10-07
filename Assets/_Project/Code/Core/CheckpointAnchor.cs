using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    public sealed class CheckpointAnchor : MonoBehaviour
    {
        [SerializeField] private string _checkpointId = "";
        public string CheckpointId => _checkpointId;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public bool IsValid => isActiveAndEnabled && !string.IsNullOrWhiteSpace(_checkpointId) &&
            Finite(Position.x) && Finite(Position.y) && Finite(Position.z) &&
            Finite(Rotation.x) && Finite(Rotation.y) && Finite(Rotation.z) && Finite(Rotation.w);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
