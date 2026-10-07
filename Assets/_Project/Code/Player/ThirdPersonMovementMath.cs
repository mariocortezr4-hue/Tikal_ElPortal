using UnityEngine;

namespace Tikal.Player
{
    /// <summary>Horizontal camera basis and heading math, independent of physics and graphics.</summary>
    public static class ThirdPersonMovementMath
    {
        public const float InputThresholdSquared = .0001f;
        private const float BasisThresholdSquared = .000001f;

        public static Vector3 CalculateMove(Vector2 input, Vector3 cameraForward, Vector3 cameraRight, Vector3 fallbackForward)
        {
            if (input.sqrMagnitude < InputThresholdSquared) return Vector3.zero;
            var forward = Horizontal(cameraForward);
            if (forward.sqrMagnitude < BasisThresholdSquared)
            {
                var projectedRight = Horizontal(cameraRight);
                forward = projectedRight.sqrMagnitude >= BasisThresholdSquared
                    ? Vector3.Cross(projectedRight.normalized, Vector3.up)
                    : Horizontal(fallbackForward);
            }
            if (forward.sqrMagnitude < BasisThresholdSquared) forward = Vector3.forward;
            forward.Normalize();
            // Orthonormal horizontal axes prevent pitch/roll or diagonals adding speed.
            var right = Vector3.Cross(Vector3.up, forward);
            var move = Vector2.ClampMagnitude(input, 1f);
            return forward * move.y + right * move.x;
        }

        public static float TurnYaw(float yaw, Vector3 velocity, ref float turnVelocity, float smoothTime, float deltaTime)
        {
            velocity = Horizontal(velocity);
            if (deltaTime <= 0 || velocity.sqrMagnitude < InputThresholdSquared)
            {
                turnVelocity = 0;
                return yaw;
            }
            var target = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            return Mathf.SmoothDampAngle(yaw, target, ref turnVelocity, Mathf.Max(.001f, smoothTime), Mathf.Infinity, deltaTime);
        }

        private static Vector3 Horizontal(Vector3 value) => new Vector3(value.x, 0, value.z);
    }
}
