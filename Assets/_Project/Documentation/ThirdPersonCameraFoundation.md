# Third Person Camera Foundation v1

## Status and materialization

The C# implementation and Editor tooling are supplied on `feature/third-person-camera-foundation`.
The prefab, default profile asset and scene wiring must be materialized in the interactive Unity
6000.6.3f1 Editor. No scene/prefab YAML is hand-edited. No batchmode is used.

1. Let Unity import and compile the scripts. Leave Play Mode.
2. Run **Tikal/Authoring/Apply Third Person Camera Foundation**.
3. Run **Tikal/Validation/Validate Third Person Camera Foundation**.
4. Open `Assets/_Project/Scenes/Testing/PlayerMovementTest.unity`, enter Play Mode and run
   **Tikal/Validation/Run Third Person Camera Behavior Checks (Play Mode)**.
5. Perform the manual checks below in Tutorial and Level01 with both persisted camera modes.
6. Review and save the generated prefab, profile and scene changes in a second commit.

Authoring updates the real `Assets/_Project/Prefabs/Player/Explorer.prefab` and wires its instances
in `TUT_ExplorerBasics`, `Level01_SecretEntrance` and the existing `PlayerMovementTest` scene.
It restores the user's scene setup and preserves existing gameplay, puzzles, input bindings,
reticle references, camera render settings and player tuning. It refuses duplicate cameras,
listeners or named targets instead of guessing which objects to remove. Running it repeatedly
reuses objects/components/profile; existing profile edits are retained. Validation runs the
configuration once and five more times on temporary prefab contents and compares the structure
and serialized component state. Validation opens scenes as previews and never saves them.

`PlayerMovementTest` already contains walls, ramps, platforms and a low ceiling. It supplies the
collision lab without creating `TPS_CameraLab` or another scene/build entry.

## Existing player and architecture

Explorer already contains `CharacterController`, `PlayerInputReader`, `InputDeviceManager`,
`PlayerController`, `PlayerMovement`, `PlayerLook`, `PlayerInteraction` and a debug probe.
`MainCamera` (with its existing URP camera data and one AudioListener) was under `CameraRoot`.
Tutorial and Level01 both instantiate Explorer. `CharacterVisualSpawner` and
`PersistentSettingsApplier` exist as reusable systems; authoring connects them rather than
creating another character catalog or persistence system.

Authored logical hierarchy:

```text
Explorer
  PlayerCameraController + existing player components
  PersistentSettingsApplier (default for prefab/Level01/testing)
  CharacterVisualSpawner
  CameraRoot                         existing PlayerLook target; unchanged reference
    FirstPersonTarget                identity transform; preserves camera's local pose
      MainCamera                     the original Camera + URP data + AudioListener
  CameraTargets                      identity logical targets; independent of visual bones
    ThirdPersonPivot
    LookTarget
  CharacterVisualRoot
    ThirdPersonHumanProxy
      Head / Body / LeftArm / RightArm / LeftLeg / RightLeg
```

The FPS target intentionally stays under the legacy CameraRoot instead of moving that root:
`PlayerLook` continues to apply its exact local pitch and crouch eye-height calculation.
All targets belong to Explorer's logical rig, with no Head-bone or Animator dependence.
The same MainCamera receives a world pose in TPS and its original local pose in FPS.
No alternate rendering camera or AudioListener is created. The runtime collision query uses
a disabled SphereCollider, never a physical camera collider.

Tutorial already has a scene-level settings applier that references PlayerLook and reticle.
Authoring retains it, assigns it to the camera controller and removes only the newly inherited
default applier from that instance. This prevents two persistence loads/settings owners.
Level01/testing use the prefab applier, with the existing scene reticle wired where present.

## Profile and framing

Authoring creates `Assets/_Project/Settings/Camera/ThirdPersonCameraProfile_Default.asset`
only when absent. Edit it in the Inspector without recompilation:

| Property | Default |
| --- | ---: |
| Distance | 3.4 m |
| PivotHeight | 1.55 m |
| ShoulderOffset | +0.35 m |
| PitchMin / PitchMax | -30 / +70 degrees |
| CollisionRadius | 0.20 m |
| CollisionPadding | 0.10 m |
| MinimumDistance | 0.45 m |
| PositionSmoothTime | 0.12 s |
| RotationSmoothTime | 0.06 s |
| CollisionMask | All layers |

Distance is the backward arm length; the diagonal shoulder arm is slightly longer.
MinimumDistance is a preferred framing limit, subordinated to solid collision safety.
Pivot height follows the existing crouch eye height when lower than the profile height.
TPS uses the camera's authored base FOV (75 currently), with no additional FOV preference.
These are blockout values. The proxy is approximately 1.75 m tall and uses the existing Stone
material. All primitive colliders are removed. It has no locomotion/Animator.

## Look ownership and settings

`PlayerController` reads the semantic Look once per frame and routes it to exactly one owner:

- FPS: the original `PlayerLook.Tick`, unchanged yaw/pitch/clamps and input math.
- TPS: `PlayerCameraController.TickLook`, independent orbit yaw and clamped pitch.

TPS never writes Explorer's rotation. Movement remains relative to the existing Explorer axes;
this is intentional foundation behavior, with no camera-relative movement/facing implementation.
Mouse delta uses PlayerLook.MouseSensitivity; stick rate uses PlayerLook.GamepadSensitivity
and deltaTime, with the same unit-circle clamp and InvertY sign as FPS. No new input actions,
direct device polling, sensitivity values or preferences are introduced.

`PersistentSettingsApplier` exposes `CurrentSettings` and `SettingsApplied`. The camera controller
subscribes/unsubscribes on enable/disable, takes the current loaded settings at Start and supports
`ApplyCameraMode(CameraMode)` during runtime. Settings apply uses the existing `GameSettingsData`
CameraMode and PlayerLook sensitivity/inversion; no per-frame JSON read occurs. Switching modes
reuses the same camera and changes visual root/proxy visibility. Unrelated settings updates do
not reset TPS orbit. A disabled camera controller restores FPS ownership and hides the proxy.

`ReticleSettings` / `ReticlePresenter` / `ReticleMode` remain unchanged: None, Dot and Cross work
with either camera mode. The shared PlayerController.GameplayActive and PlayerInputReader's
GameplayEnabled gates stop TPS input and pose updates during Journal, Pause or modal locks.
No second lock manager is added. Existing Journal cursor/map restoration remains intact.

## Collision and smoothing

The profile collision mask includes all world layers by default; filtering removes only
colliders under this Explorer, so legitimate obstacles on the same layer remain detectable.
Every query ignores triggers. A SphereCast from the pivot along the diagonal shoulder arm
selects the nearest non-self hit, subtracting padding. It can retract below MinimumDistance
when a closer obstruction requires it. All hits are considered, avoiding fixed-buffer overflow.

New obstruction distance retracts immediately. Clearance restores arm length via SmoothDamp.
Yaw/pitch and position are smoothed separately. A second SphereCast constrains the actual
smoothed position so smoothing cannot interpolate through a corner. OverlapSphere and
ComputePenetration resolve initial overlap, which SphereCast alone cannot detect, including
low ceilings and pivots near geometry. Depenetration is bounded to eight passes; pathological
overlapping solid volumes or a spawn embedded in geometry still require level-authoring fixes
and manual verification. Queries allocate hit arrays in v1; optimize after profiling if needed.

## FPS, Tutorial and Level01 compatibility

PlayerMovement is untouched: walk/sprint/jump/crouch, acceleration, gravity and headroom behavior
stay as authored. PlayerLook retains FPS pitch/yaw/sensitivity, eye-height smoothing and its
75-to-78 sprint FOV. The proxy/visual root are hidden in FPS. The original camera's local pose,
tag, render setup and AudioListener are retained when switching back.

TutorialLookStep measures the active TPS orbit angles instead of requiring Explorer rotation.
FPS keeps its original yaw/pitch measurement. A mode switch alone contributes no tutorial Look
progress. The steps, required degrees, prompts and semantic controls remain unchanged, with
no new SecretCommand. Level01 puzzles, interactions, secrets and narrative scenes are untouched.
`Intro_AncientMap` and its Timeline cameras are excluded from all authoring.

**Known foundation limitation:** PlayerInteraction still casts 3 m from the actual camera.
Moving that camera behind Explorer reduces effective reach in front of the character, and a
retracted orbit may see Explorer's CharacterController. Interaction is deliberately unchanged
per this feature's scope. Do not claim TPS interaction parity until the following integration
defines aim origin, character reach and occlusion. FPS interaction remains unchanged.

## Validation and manual checks

Static compilation covers both runtime and Editor assemblies using Unity's bundled Roslyn
compiler and installed assembly references, without launching Unity. Existing repository
warnings are separated from new-file warnings. Runtime/physics and authoring checks must run
in the interactive Editor; a static compile does not establish gameplay correctness.

The validation menu checks all critical references, one active camera/listener, persisted modes,
three stable targets, humanoid hierarchy, visibility in both modes, valid profile/mask/clamp,
missing scripts, reticle wiring, Tutorial Look references and settings/input-lock source contracts.
The Play Mode checks create disposable fixtures away from gameplay with their camera/listener
disabled before activation, restore cursor/volume, and never save preferences. They exercise FPS
look/FOV, live settings, free orbit, mouse/stick math, inversion/clamps, both lock gates, wall
retraction, trigger/self filtering, collision priority over minimum distance, initial overlap,
smoothed return and switching back to the original FPS local camera pose.

Manual checks, after authoring:

- Persist FPS in the existing Settings screen, then load Tutorial and Level01: compare look,
  sensitivity, inversion, crouch, sprint FOV, reticles, prompts and interaction to the baseline.
- Persist TPS and reload both scenes: verify 1.75 m proxy framing, shoulder offset, free mouse
  orbit while the player stays still, right-stick orbit, pitch limits and Look-step completion.
- Change sensitivity/inversion and None/Dot/Cross using existing settings; reload to verify
  persistence. Apply runtime modes via the controller/API without adding a gameplay toggle.
- Orbit against front/side walls, corners, low ceilings, terrain, nearby rocks and a wall behind
  Explorer. Test both slow movement and quick rotations. Check immediate inward safety and
  smooth outward return, without severe jitter or clipping. Triggers must not move the camera.
- Open/close Journal and each existing pause/modal flow: orbit must stop, cursor restoration
  must match FPS, and no latent look delta should rotate the player in TPS.
- Count active gameplay Camera/AudioListener components. Confirm cinematic behavior separately.

## Replacing the visual and TPS-2

Replace `CharacterVisualRoot/ThirdPersonHumanProxy` with the future `CHR_Explorer_Final`:
2K model, Humanoid rig and separate Idle/Walk/Run/Jump/Crouch animations. Keep CameraRoot,
CameraTargets and MainCamera outside the model's skeleton. Do not parent the camera to Head.
Use the existing CharacterCatalog/CharacterVisualSpawner for selected-character spawning and
remove the proxy (or hide it when the final visual is present). Update authoring and validation
to accept the final visual before rerunning this v1 menu, which intentionally ensures the proxy.
PlayerCameraController only gates CharacterVisualRoot visibility; it never needs Animator.

TPS-2 will define camera-relative movement, facing toward movement, strafe/animation blending,
interaction reach/aim integration and final Humanoid locomotion. Body-aware FPS, head hiding,
shadows and separate first-person mesh remain future decisions. No TPS-2 changes or merges
to develop are part of this foundation.
