using System;
using Tikal.Player;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    // These checks also run under a console host with Unity's managed math assembly,
    // without loading a scene, graphics device or Unity Editor process.
    public static class ThirdPersonMovementMathChecks
    {
        public static int Run()
        {
            var count = 0;
            Vector(Calculate(Vector2.up, Vector3.forward, Vector3.right), Vector3.forward, "north + W", ref count);
            Vector(Calculate(Vector2.up, Vector3.right, Vector3.back), Vector3.right, "east + W", ref count);
            Vector(Calculate(Vector2.up, Vector3.left, Vector3.forward), Vector3.left, "west + W", ref count);
            Vector(Calculate(Vector2.right, Vector3.forward, Vector3.right), Vector3.right, "north + D", ref count);
            Vector(Calculate(Vector2.left, Vector3.forward, Vector3.right), Vector3.left, "north + A", ref count);
            Vector(Calculate(Vector2.down, Vector3.forward, Vector3.right), Vector3.back, "north + S", ref count);
            var diagonal = Calculate(Vector2.one, Vector3.forward, Vector3.right);
            Check(Mathf.Abs(diagonal.magnitude - 1f) < .0001f && diagonal.x > 0 && diagonal.z > 0, "diagonal speed capped", ref count);
            Check(Mathf.Abs(Calculate(new Vector2(.3f, .4f), Vector3.forward, Vector3.right).magnitude - .5f) < .0001f,
                "analog magnitude preserved", ref count);
            Vector(Calculate(Vector2.up, new Vector3(0, -.94f, .34f), Vector3.right), Vector3.forward, "pitch stays horizontal", ref count);
            Vector(Calculate(Vector2.up, Vector3.down, Vector3.right), Vector3.forward, "vertical forward uses camera right", ref count);
            Vector(ThirdPersonMovementMath.CalculateMove(Vector2.up, Vector3.zero, Vector3.zero, Vector3.right), Vector3.right,
                "degenerate camera uses player fallback", ref count);
            Vector(ThirdPersonMovementMath.CalculateMove(Vector2.up, Vector3.zero, Vector3.zero, Vector3.zero), Vector3.forward,
                "fully degenerate basis stays finite", ref count);
            Vector(Calculate(Vector2.zero, Vector3.right, Vector3.back), Vector3.zero, "zero Move", ref count);
            Vector(Calculate(Vector2.one * .005f, Vector3.right, Vector3.back), Vector3.zero, "residual stick drift", ref count);
            var velocity = 0f;
            var yaw = ThirdPersonMovementMath.TurnYaw(0, Vector3.right, ref velocity, .14f, .02f);
            Check(yaw > 0 && yaw < 90, "turn is gradual, not a snap", ref count);
            for (var frame = 0; frame < 120; frame++) yaw = ThirdPersonMovementMath.TurnYaw(yaw, Vector3.right, ref velocity, .14f, .02f);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw, 90)) < .01f, "turn converges to movement", ref count);
            var stopped = ThirdPersonMovementMath.TurnYaw(yaw, Vector3.zero, ref velocity, .14f, .02f);
            Check(stopped == yaw && velocity == 0, "rest keeps orientation and clears residual turn", ref count);
            Check(ThirdPersonMovementMath.TurnYaw(30, Vector3.right, ref velocity, .14f, 0) == 30, "zero time does not turn", ref count);
            var next = ThirdPersonMovementMath.TurnYaw(350, new Vector3(.173648f, 0, .984808f), ref velocity, .14f, .02f);
            Check(Mathf.DeltaAngle(350, next) > 0 && Mathf.DeltaAngle(350, next) < 20, "shortest turn across yaw wrap", ref count);
            return count;
        }

        private static Vector3 Calculate(Vector2 input, Vector3 forward, Vector3 right) =>
            ThirdPersonMovementMath.CalculateMove(input, forward, right, Vector3.forward);
        private static void Vector(Vector3 actual, Vector3 expected, string name, ref int count) =>
            Check((actual - expected).sqrMagnitude < .000001f, name, ref count);
        private static void Check(bool valid, string name, ref int count)
        {
            if (!valid) throw new InvalidOperationException("TPS movement math failed: " + name);
            count++;
        }
    }
}
