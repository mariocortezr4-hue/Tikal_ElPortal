using UnityEngine;

namespace Tikal.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private CheckpointManager _manager = null;
        [SerializeField] private CheckpointAnchor _checkpoint = null;
        private bool _activated;
        private void OnTriggerEnter(Collider other)
        {
            if (!_activated && _manager != null && other == _manager.PlayerCollider && _manager.Activate(_checkpoint)) _activated = true;
        }
    }
}
