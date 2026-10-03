using UnityEngine;

namespace Tikal.Player
{
    public sealed class PlayerMovementDebugProbe : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _movement;

#if UNITY_EDITOR || DEBUG
        private void OnGUI()
        {
            if (_movement == null)
                return;
            GUILayout.BeginArea(new Rect(12, 12, 280, 180), "Movement Debug", GUI.skin.window);
            GUILayout.Label($"Grounded: {_movement.IsGrounded}");
            GUILayout.Label($"Speed: {_movement.CurrentSpeed:F2} m/s");
            GUILayout.Label($"Vertical: {_movement.VerticalVelocity:F2} m/s");
            GUILayout.Label($"Crouching: {_movement.IsCrouching}");
            GUILayout.Label($"Sprinting: {_movement.IsSprinting}");
            GUILayout.EndArea();
        }
#endif
    }
}
