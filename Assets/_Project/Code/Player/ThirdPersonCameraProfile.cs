using UnityEngine;

namespace Tikal.Player
{
    [CreateAssetMenu(menuName = "Tikal/Camera/Third Person Camera Profile")]
    public sealed class ThirdPersonCameraProfile : ScriptableObject
    {
        [Min(.01f)] public float Distance = 3.4f;
        [Min(0)] public float PivotHeight = 1.55f;
        public float ShoulderOffset = .35f;
        [Range(-89, 0)] public float PitchMin = -30f;
        [Range(0, 89)] public float PitchMax = 70f;
        [Min(.01f)] public float CollisionRadius = .20f;
        [Min(0)] public float CollisionPadding = .10f;
        [Min(0)] public float MinimumDistance = .45f;
        [Min(.001f)] public float PositionSmoothTime = .12f;
        [Min(.001f)] public float RotationSmoothTime = .06f;
        public LayerMask CollisionMask = ~0;

        public bool IsValid => Distance > MinimumDistance && MinimumDistance >= 0 &&
            PivotHeight >= 0 && CollisionRadius > 0 && CollisionPadding >= 0 &&
            CollisionMask.value != 0 && PitchMin >= -89 && PitchMax <= 89 && PitchMin < PitchMax &&
            PositionSmoothTime > 0 && RotationSmoothTime > 0;
    }
}
