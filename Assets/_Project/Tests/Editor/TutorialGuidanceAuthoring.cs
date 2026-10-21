using System;
using System.IO;
using System.Linq;
using Tikal.Player;
using Tikal.Tutorial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tikal.Tests.Editor
{
    public static class TutorialGuidanceAuthoring
    {
        public const string Measures = TutorialEnvironmentArtPass.Docs + "TutorialCrouchMeasurements.json";
        public static float Clearance
        {
            get
            {
                var data = JsonUtility.FromJson<TutorialEnvironmentPlayValidation.MeasurementReport>(File.ReadAllText(Measures));
                if (!data.passed || data.characters.Length != 3) throw new InvalidOperationException("Measure all three real characters before guidance authoring");
                float top = data.characters.Max(c => Mathf.Max(c.crouchIdleTop, c.crouchWalkTop));
                float entry = data.characters.Max(c => c.crouchEntryTop);
                float clearance = Mathf.Ceil(Mathf.Max(top + .075f, entry + .06f) * 100) / 100;
                if (clearance - .04f >= data.characters.Min(c => c.standingTop) - .08f) throw new InvalidOperationException("No safe crouch opening fits unchanged standing capsule");
                return clearance;
            }
        }
        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        static void Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var t = Child(parent, name); t.localPosition = position; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            var box = t.GetComponent<BoxCollider>(); if (box == null) box = t.gameObject.AddComponent<BoxCollider>(); box.center = Vector3.zero; box.size = size; box.isTrigger = false; box.enabled = true;
        }
        static GameObject Visual(Transform parent, string name, string asset, Vector3 position, Vector3 size, float yaw = 0)
        {
            var t = parent.Find(name);
            if (t == null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentArtPass.Prefabs + "PFB_" + asset + ".prefab"), parent); go.name = name; t = go.transform; }
            t.localScale = Vector3.one; t.rotation = Quaternion.identity; t.position = Vector3.zero;
            var bounds = TutorialEnvironmentArtPass.BoundsOf(t.gameObject);
            t.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            bounds = TutorialEnvironmentArtPass.BoundsOf(t.gameObject); t.position = new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z);
            t.RotateAround(position, Vector3.up, yaw);
            foreach (var renderer in t.GetComponentsInChildren<Renderer>(true))
                if (asset.StartsWith("VEG_")) { renderer.shadowCastingMode = ShadowCastingMode.Off; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
            foreach (var tr in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            return t.gameObject;
        }
        public static void Apply()
        {
            var environment = TutorialEnvironmentArtPass.Find("Environment"); var art = TutorialEnvironmentArtPass.Find(TutorialEnvironmentArtPass.RootName);
            // Hidden legacy cylinders no longer match the denser edge composition. The continuous simple boundaries replace their physical role.
            foreach (var proxy in environment.GetComponentsInChildren<Collider>(true).Where(c => c.name.StartsWith("Vegetation proxy "))) proxy.enabled = false;
            var edge = Child(art, "PathContainment"); var boundaries = Child(environment, "GameplayBoundaries");
            // Segments overlap; back is closed, forward central route remains open beyond the canonical Exit trigger.
            for (int side = -1; side <= 1; side += 2)
            {
                string label = side < 0 ? "Left" : "Right";
                for (int i = 0; i < 2; i++) Box(boundaries, "Boundary_" + label + "_0" + (i + 1), new Vector3(side * 7, 2.1f, 8.5f + i * 29), new Vector3(1, 4.2f, 29.5f));
                Box(boundaries, "Boundary_ForwardWing_" + label, new Vector3(side * 5.25f, 2.1f, 51.5f), new Vector3(4.5f, 4.2f, 1));
                Box(boundaries, "Boundary_CrouchWing_" + label, new Vector3(side * 4.4f, 1.4f, 19), new Vector3(4.4f, 2.8f, 1.2f));
                Box(boundaries, "Boundary_InteractPillar_" + label, new Vector3(side * 2.25f, 2.45f, 36), new Vector3(.7f, 4.9f, .7f));
                Visual(edge, "InteractPillar_" + label, "ARQ_ColumnaMaya_01", new Vector3(side * 2.25f, 0, 36), new Vector3(.75f, 4.9f, .8f));
            }
            Box(boundaries, "Boundary_Rear", new Vector3(0, 2.1f, -5.8f), new Vector3(15, 4.2f, 1));
            // Reposition existing masses, preserving their meshes, LODs and shared prefabs.
            var random = new System.Random(210622);
            foreach (var bush in art.Find("Vegetation/Bushes").Cast<Transform>())
            {
                float side = bush.localPosition.x < 0 ? -1 : 1;
                var p = bush.localPosition; p.x = side * (5.6f + (float)random.NextDouble() * .85f); bush.localPosition = p;
                bush.localScale *= side < 0 ? 2.1f : 1.65f;
            }
            foreach (var rock in art.Find("Rocks").Cast<Transform>())
            {
                var p = rock.localPosition; p.x = Mathf.Sign(p.x) * (6 + (float)random.NextDouble() * .6f); rock.localPosition = p;
            }
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, z = new[] { 1f, 12, 25, 35, 46 }[i / 2] + (float)random.NextDouble() * 1.6f;
                Visual(edge, "EdgeMass_" + i.ToString("D2"), i % 3 == 0 ? "VEG_ArbustoTropical_02" : "VEG_ArbustoTropical_03", new Vector3(side * (6.1f + (float)random.NextDouble() * .3f), 0, z), new Vector3(3.2f, 2.1f, 3.2f), (float)random.NextDouble() * 360);
            }
            var walls = art.Find("Ruins");
            foreach (var item in new[] { ("Wall_Jump_Left", -1), ("Wall_Sprint_Right", 1) })
            {
                var t = walls.Find(item.Item1); t.rotation = Quaternion.identity; t.localScale = Vector3.one;
                var b = TutorialEnvironmentArtPass.BoundsOf(t.gameObject); t.localScale = new Vector3(4.4f / b.size.x, 2.8f / b.size.y, 1.2f / b.size.z);
                b = TutorialEnvironmentArtPass.BoundsOf(t.gameObject); t.position += new Vector3(item.Item2 * 4.4f - b.center.x, -b.min.y, 19 - b.center.z);
            }
            float clearance = Clearance;
            var lintel = TutorialEnvironmentArtPass.Find("Low ruin lintel"); lintel.localPosition = new Vector3(0, clearance + .225f, 19);
            // Minimal local collision allowance protects the standing-to-crouch animation transition.
            // Visual underside stays 4 cm above collision; front/back pads are 12 cm each.
            var lintelCollider = lintel.GetComponent<BoxCollider>();
            lintelCollider.center = new Vector3(0, -.02f / .45f, 0);
            lintelCollider.size = new Vector3(1, .49f / .45f, 1.44f / 1.2f);
            foreach (string name in new[] { "Ruin left", "Ruin right" })
            {
                var proxy = TutorialEnvironmentArtPass.Find(name); var visual = proxy.Find("Art_ARQ_ColumnaMaya_01");
                var b = TutorialEnvironmentArtPass.BoundsOf(visual.gameObject); visual.localScale = Vector3.Scale(visual.localScale, new Vector3(1, clearance / b.size.y, 1));
                b = TutorialEnvironmentArtPass.BoundsOf(visual.gameObject); visual.position += Vector3.up * -b.min.y;
            }
            var safety = Child(environment, "TutorialOutOfBoundsVolume"); safety.position = new Vector3(0, -6, 23); safety.localScale = Vector3.one;
            var collider = safety.GetComponent<BoxCollider>(); if (collider == null) collider = safety.gameObject.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(100, 10, 120);
            var recovery = safety.GetComponent<TutorialOutOfBoundsVolume>(); if (recovery == null) recovery = safety.gameObject.AddComponent<TutorialOutOfBoundsVolume>();
            var data = new SerializedObject(recovery); data.FindProperty("_player").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); data.FindProperty("_spawnPosition").vector3Value = new Vector3(0, .05f, 0); data.FindProperty("_spawnEuler").vector3Value = Vector3.zero; data.ApplyModifiedPropertiesWithoutUndo(); Physics.SyncTransforms();
        }
        public static void Validate(Action<bool, string> check)
        {
            var environment = TutorialEnvironmentArtPass.Find("Environment"); var root = environment.Find("GameplayBoundaries");
            check(root != null && root.childCount == 11 && root.GetComponentsInChildren<Renderer>(true).Length == 0, "Eleven simple boundary colliders, no renderers");
            check(environment.GetComponentsInChildren<Collider>(true).Where(c => c.name.StartsWith("Vegetation proxy ")).Count(c => !c.enabled) == 20, "Twenty legacy invisible vegetation cylinders disabled; continuous boundaries replace them");
            check(root.GetComponentsInChildren<Collider>().All(c => c is BoxCollider && !c.isTrigger), "Boundary primitives only");
            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "Left" : "Right"; var first = root.Find("Boundary_" + name + "_01").GetComponent<BoxCollider>(); var second = root.Find("Boundary_" + name + "_02").GetComponent<BoxCollider>();
                check(first.bounds.max.z >= second.bounds.min.z && first.bounds.min.z <= -6 && second.bounds.max.z >= 52, "Continuous dangerous edge: " + name);
                check(first.bounds.max.y >= 4.2f && Mathf.Abs(first.bounds.center.x) - first.bounds.extents.x >= 6.49f, "Boundary height exceeds standing plus real 1.2 m jump, central TPS space: " + name);
            }
            check(environment.Find("TutorialOutOfBoundsVolume").GetComponent<TutorialOutOfBoundsVolume>() != null && environment.Find("TutorialOutOfBoundsVolume").GetComponent<BoxCollider>().isTrigger, "One local safety trigger / no checkpoints");
            check(environment.GetComponentsInChildren<TutorialOutOfBoundsVolume>(true).Length == 1, "No duplicate safety components");
            var floor = TutorialEnvironmentArtPass.Find("Jungle floor").GetComponent<Collider>().bounds;
            var rear = root.Find("Boundary_Rear").GetComponent<BoxCollider>().bounds;
            check(floor.min.x < -7.5f && floor.max.x > 7.5f && floor.min.z < -6.8f && floor.max.z > 51 && rear.min.x < -6.5f && rear.max.x > 6.5f && rear.min.z < -6.25f, "Side/back barriers overlap and OOB range ends before physical floor edges");
            var leftWing = root.Find("Boundary_ForwardWing_Left").GetComponent<BoxCollider>().bounds;
            var rightWing = root.Find("Boundary_ForwardWing_Right").GetComponent<BoxCollider>().bounds;
            check(leftWing.max.x <= -3 && rightWing.min.x >= 3 && leftWing.min.z > 40, "Canonical forward exit remains open beyond original Exit trigger");
            var art = TutorialEnvironmentArtPass.Find(TutorialEnvironmentArtPass.RootName).Find("PathContainment");
            check(art != null && art.childCount == 12 && art.Cast<Transform>().Select(t => t.name).Distinct().Count() == 12, "Ten edge masses and two unique fixed Interact pillars");
            foreach (string label in new[] { "Left", "Right" }) check(art.Find("InteractPillar_" + label).parent != TutorialEnvironmentArtPass.Find("Lift stone"), "Fixed pillar outside moving stone: " + label);
            var beam = TutorialEnvironmentArtPass.Find("Low ruin lintel").GetComponent<BoxCollider>();
            var roof = TutorialEnvironmentArtPass.BoundsOf(beam.transform.Find("Art_ADP_Tutorial_LowLintel_R2").gameObject);
            check(Mathf.Abs(roof.min.y - Clearance) < .002f && Mathf.Abs(beam.bounds.min.y - (Clearance - .04f)) < .002f && beam.bounds.min.y < 1.82f && Mathf.Abs(beam.bounds.size.z - 1.44f) < .002f, "Measured visual clearance / local collision allowance below standing capsule");
        }
    }
}
