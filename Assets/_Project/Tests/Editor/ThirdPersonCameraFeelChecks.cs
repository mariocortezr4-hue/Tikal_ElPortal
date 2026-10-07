using System;
using Tikal.Input;
using Tikal.Player;
using UnityEngine;

namespace Tikal.Tests.Editor
{
    public static class ThirdPersonCameraFeelChecks
    {
        public static int Run()
        {
            var count = 0;
            var radius = ThirdPersonCameraMath.ConfiguredBoom(3.4f, .35f).magnitude;
            foreach (var pitchDegrees in new[] { -30f, 0f, 70f })
            {
                for (var yawDegrees = 0; yawDegrees <= 360; yawDegrees++)
                {
                    var yaw = yawDegrees * Mathf.Deg2Rad;
                    var pitch = pitchDegrees * Mathf.Deg2Rad;
                    var forward = new Vector3((float)(Math.Sin(yaw) * Math.Cos(pitch)), -(float)Math.Sin(pitch),
                        (float)(Math.Cos(yaw) * Math.Cos(pitch)));
                    var right = new Vector3((float)Math.Cos(yaw), 0, -(float)Math.Sin(yaw));
                    var offset = ThirdPersonCameraMath.BoomOffset(right * .35f - forward * 3.4f, radius);
                    Check(Mathf.Abs(offset.magnitude - radius) < .00001f, "constant orbit radius at yaw/pitch", ref count);
                }
            }
            // Reproduce the former Cartesian chord compression without assuming an FOV cause.
            var north = ThirdPersonCameraMath.ConfiguredBoom(3.4f, .35f);
            var east = new Vector3(-3.4f, 0, -.35f);
            Check(((north + east) * .5f).magnitude < radius * .8f, "old position interpolation cuts the orbit arc", ref count);
            var fine = Delta(new Vector2(.2f, 0), LookInputMode.Rate, 1);
            Check(fine.x > 0 && fine.x < .2f * 190, "small stick more precise than linear", ref count);
            Check(Mathf.Abs(Delta(Vector2.right, LookInputMode.Rate, 1).x - 190) < .001f, "full horizontal rate", ref count);
            Check(Mathf.Abs(Delta(Vector2.up, LookInputMode.Rate, 1).y - 145) < .001f, "full vertical rate", ref count);
            var shaped = Delta(new Vector2(.3f, .4f), LookInputMode.Rate, 1);
            Check(Mathf.Abs((shaped.x / 190) / (shaped.y / 145) - .75f) < .001f, "radial direction retained before axis rates", ref count);
            Check(Delta(Vector2.zero, LookInputMode.Rate, 1) == Vector2.zero, "zero stick", ref count);
            var mouse = Delta(new Vector2(10, -5), LookInputMode.Delta, 1f / 30);
            Check((mouse - new Vector2(1.2f, -.6f)).sqrMagnitude < .000001f, "mouse uses direct sensitivity", ref count);
            Check(Delta(new Vector2(10, -5), LookInputMode.Delta, 1f / 120) == mouse, "mouse delta has no deltaTime multiplier", ref count);
            foreach (var fps in new[] { 30, 60, 120 })
            {
                var total = Vector2.zero;
                for (var frame = 0; frame < fps; frame++) total += Delta(new Vector2(.6f, .8f), LookInputMode.Rate, 1f / fps);
                Check((total - new Vector2(114, 116)).magnitude < .002f, "gamepad rate independent at " + fps + " FPS", ref count);
            }
            var scaled = ThirdPersonCameraMath.LookDelta(Vector2.right, LookInputMode.Rate, .12f, 75, 190, 145, 1.5f, 1);
            Check(Mathf.Abs(scaled.x - 95) < .001f, "user setting scales base rate", ref count);
            var velocity = 5f;
            var distance = ThirdPersonCameraMath.RecoverDistance(radius, 1.1f, ref velocity, .12f, .02f);
            Check(distance == 1.1f && velocity == 0, "collision retracts immediately", ref count);
            var next = ThirdPersonCameraMath.RecoverDistance(distance, radius, ref velocity, .12f, .02f);
            Check(next > distance && next < radius, "collision recovery is gradual", ref count);
            for (var frame = 0; frame < 120; frame++) next = ThirdPersonCameraMath.RecoverDistance(next, radius, ref velocity, .12f, .02f);
            Check(Mathf.Abs(next - radius) < .001f, "recovery reaches configured radius", ref count);
            velocity = 0;
            Check(ThirdPersonCameraMath.RecoverDistance(radius, radius, ref velocity, .12f, .02f) == radius, "clear orbit does not animate distance", ref count);
            return count;
        }

        private static Vector2 Delta(Vector2 look, LookInputMode mode, float deltaTime) =>
            ThirdPersonCameraMath.LookDelta(look, mode, .12f, 150, 190, 145, 1.5f, deltaTime);
        private static void Check(bool valid, string name, ref int count)
        {
            if (!valid) throw new InvalidOperationException("Camera feel: " + name);
            count++;
        }
    }
}
