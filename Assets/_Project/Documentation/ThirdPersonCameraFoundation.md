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
    InteractionOrigin                configurable logical chest target (1.35 m initially)
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
| CollisionReturnSmoothTime | 0.12 s |
| RotationSmoothTime | 0.06 s |
| GamepadYawRate / GamepadPitchRate | 190 / 145 degrees/s |
| GamepadResponseExponent | 1.5 |
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

Foundation v1 Look never writes Explorer's rotation. Movement Integration v2 now routes Move
through a horizontal camera basis and smoothly turns Explorer; stationary free orbit remains.
See [ThirdPersonMovement.md](ThirdPersonMovement.md) for the separate movement authority and tests.
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

New obstruction distance retracts immediately. Clearance restores arm length via SmoothDamp,
using CollisionReturnSmoothTime. Only yaw/pitch and collision recovery are smoothed; the world
position is constructed directly from the final rotation and current radial distance. No
Cartesian position interpolation or second smoothing pass can cut the orbit arc. OverlapSphere and
ComputePenetration resolve initial overlap, which SphereCast alone cannot detect, including
low ceilings and pivots near geometry. Depenetration is bounded to eight passes; pathological
overlapping solid volumes or a spawn embedded in geometry still require level-authoring fixes
and manual verification. Queries allocate hit arrays in v1; optimize after profiling if needed.

### Camera feel polish diagnosis and input

The former implementation first smoothed orbit angles, then applied Vector3.SmoothDamp from
the previous world position to the rotated boom endpoint. That interpolation follows a chord
inside the orbit circle, temporarily shortening Pivot-to-camera distance. It also applied two
successive smoothing stages to camera response. TPS FOV was already stable; LateUpdate was
already after all player Update calls and before interaction LateUpdate. Neither FOV nor update
ordering was the source of the confirmed geometric radius compression.

Now the configured local boom is `(ShoulderOffset, 0, -Distance)`, rotated by smoothed pitch/yaw.
The configured radial length is sqrt(Distance squared + ShoulderOffset squared), approximately
3.418 m for 3.4/0.35. CollisionDistance is the SphereCast hit distance minus padding, capped at
that configured radius. CurrentDistance retracts immediately and recovers smoothly, then places
the camera exactly on that direction/radius. Orbit alone cannot reduce radius. The shoulder
rotates with the same rig and scales toward the pivot on retraction, retaining collision safety.
PositionSmoothTime migrates to CollisionReturnSmoothTime through FormerlySerializedAs; existing
profile assets keep their tuned 0.12 value without hand-edited YAML. RotationSmoothTime remains
0.06 s; there is no extra input acceleration or translation-follow lag. A short existing angular
settling tail remains when input is released; no long inertia is added. TPS FOV remains the
authored base 75, while the existing FPS sprint transition 75-to-78 remains unchanged.

Look bindings have no added processors; the generic Gamepad rightStick layout already applies
radial stickDeadzone. Installed InputSettings defaults are inner 0.125 and outer 0.925; no custom
settings asset overrides were found. Keep them: no second custom deadzone is applied. The 0.25
InputDeviceManager threshold selects active device and never gates camera Look.

TPS gamepad Look preserves radial direction with output magnitude `magnitude^1.5`. Compared with
the former linear response, processed magnitude 0.2 becomes approximately 0.089, giving finer
near-center control while full stick still reaches the full rate. This moderate exponent is a
blockout tuning choice for exploration, to confirm by another controller playtest. Base yaw is
190 degrees/s; pitch is 145 degrees/s (0.763 of yaw). The existing GamepadSensitivity acts as
`setting / 150`, matching its existing default 150. For example, 75 halves both rates, 300 doubles
them. No preference values or JSON format are changed. PlayStation and Xbox use the same path.

Gamepad angular delta is shaped stick times per-axis degrees/s times that multiplier times
deltaTime. Mouse stays raw frame delta times existing MouseSensitivity: no curve, deadzone,
acceleration or deltaTime multiplier. InvertY and pitch clamps -30/+70 are retained. FPS input
processing, approved movement/turn time 0.14, interaction and all gameplay flows are unchanged.

The Foundation validation menu now runs scene-free tests for full 360-degree radii at three
pitches, the former chord compression, radial curve/rates, settings scaling, mouse delta and
30/60/120 FPS gamepad integration, plus collision retract/return math. Its source contracts
reject Cartesian position smoothing. The existing Play Mode behavior checks add repeated orbit
radius and FOV assertions, and retain real collision recovery and FPS checks. Run them and repeat
mouse/PlayStation/Xbox feel tests interactively; mathematical checks do not establish subjective
feel, wall clipping or visual jitter correctness in a live scene.

Verification for this polish: 1,099 scene-free mathematical assertions passed with the installed
UnityEngine managed assembly. Runtime and Editor sources compile with no errors and no new
warnings against HEAD (217 existing runtime warnings, two Editor warnings). No Unity batchmode
was used. The new Play Mode radius/FOV assertions and another interactive controller playtest
have not been executed by this change.

## FPS, Tutorial and Level01 compatibility

Reticle alignment: authored Dot/Cross/root anchors and pivot are centered, but the later
physical-hit projection introduced a runtime TPS offset when chest-origin reach hit an
occluder away from the camera ray. The native audit reproduced Canvas offset (-33.43,-18.98).
The presenter now follows the screen projection of camera viewport (0.5,0.5), equivalent to
ViewportPointToRay(0.5,0.5), converted into Canvas parent coordinates. It never follows the
shoulder, Player.forward or physical reach impact. FPS retains exact zero offset. No camera
parameter or interaction reach changes are needed; None/Dot/Cross share one position.
See ThirdPersonReticleParallax.md for the formula, Editor gizmos and focused validation.

PlayerMovement is untouched: walk/sprint/jump/crouch, acceleration, gravity and headroom behavior
stay as authored. PlayerLook retains FPS pitch/yaw/sensitivity, eye-height smoothing and its
75-to-78 sprint FOV. The proxy/visual root are hidden in FPS. The original camera's local pose,
tag, render setup and AudioListener are retained when switching back.

TutorialLookStep measures the active TPS orbit angles instead of requiring Explorer rotation.
FPS keeps its original yaw/pitch measurement. A mode switch alone contributes no tutorial Look
progress. The steps, required degrees, prompts and semantic controls remain unchanged, with
no new SecretCommand. Level01 puzzles, secrets and narrative scenes are untouched.
`Intro_AncientMap` and its Timeline cameras are excluded from all authoring.

### Interaction reach correction

`PlayerInteraction` retains its original FPS ray exactly: actual camera position/forward,
existing `_interactionDistance` (3 m), existing interaction layer mask and ignored triggers.
Existing FPS tests and older scenes without a TPS rig continue to use that path.

TPS uses two-stage targeting. The same MainCamera casts from viewport center (also when the
reticle mode is None). Its nearest non-self solid hit identifies the visually selected
IInteractable and AimPoint. The visual search bound is camera-to-origin separation plus the
canonical interaction distance: this includes all potentially reachable points without
creating another gameplay reach setting.

Physical reach starts at `Explorer/CameraTargets/InteractionOrigin`, initially 1.35 m above
the feet. Edit this Transform to tune chest height; repeated authoring retains that tuning.
It belongs to the logical rig, never a Head bone or Animator. Its effective height is capped
below the existing CharacterController top while crouching, without changing movement.

The selected AimPoint must be within the original 3 m Euclidean player reach and must not be
behind Explorer's facing (negative dot product with Explorer.forward). A second ray from the
logical chest toward AimPoint uses only the original interaction distance. Its first non-self
solid hit must resolve to the same IInteractable selected by the camera. Thus walls or other
interactables between player and target reject interaction even when visible from the camera;
a camera-side blocker also prevents visual selection. Both stages ignore triggers and only
filter this Explorer's own hierarchy, preserving legitimate world colliders on shared layers.
An origin overlapping world geometry fails closed instead of casting out through a wall.

CurrentInteractable, FocusChanged, CanInteract and queued InteractPressed handling are unchanged.
The existing prompt consumes this validated focus; None/Dot/Cross are unchanged. Objects beyond
player reach, behind the character or behind blockers cannot gain an interaction prompt simply
because the orbit camera sees them. No distance compensation or puzzle-specific logic is added.

## Validation and manual checks

Static compilation covers both runtime and Editor assemblies using Unity's bundled Roslyn
compiler and installed assembly references, without launching Unity. Existing repository
warnings are separated from new-file warnings. Runtime/physics and authoring checks must run
in the interactive Editor; a static compile does not establish gameplay correctness.

The validation menu checks all critical references, one active camera/listener, persisted modes,
three stable targets, humanoid hierarchy, visibility in both modes, valid profile/mask/clamp,
missing scripts, reticle wiring, Tutorial Look references and settings/input-lock source contracts.
It also checks one PlayerInteraction, stable InteractionOrigin, original input/camera references,
positive canonical reach, separation of visual aim/player range and the occlusion contract.
The Play Mode checks create disposable fixtures away from gameplay with their camera/listener
disabled before activation, restore cursor/volume, and never save preferences. They exercise FPS
look/FOV, live settings, free orbit, mouse/stick math, inversion/clamps, both lock gates, wall
retraction, trigger/self filtering, collision priority over minimum distance, initial overlap,
smoothed return and switching back to the original FPS local camera pose. Added interaction
fixtures check FPS in/out of range; TPS within player reach with cameras at 0.5/3.4/8 m; a
visually selected target outside player reach; chest-side and camera-side occlusion; another
interactable blocking the physical ray; triggers/self filtering; behind-player rejection;
input-lock focus clearing/restoration; and an origin embedded in world geometry. Run these
behavior checks in the interactive Editor; static compilation does not execute physics tests.

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

TPS-2 camera-relative movement and smoothed facing are documented in ThirdPersonMovement.md.
Strafe/animation blending, final Humanoid locomotion and further aiming refinements remain future
work. Body-aware FPS, head hiding, shadows and separate first-person mesh remain future decisions.
The camera foundation and the following movement integration perform no merges to develop.
