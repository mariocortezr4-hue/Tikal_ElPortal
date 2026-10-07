# Third Person Movement Integration v2

## Architecture and scope

Explorer's existing PlayerController owns semantic input and routes one movement tick each frame.
PlayerMovement remains the single CharacterController/physics authority. The approved camera
controller owns TPS Look/orbit and exposes its existing MainCamera Transform as MovementOrientation.
The two-stage PlayerInteraction implementation is unchanged.

FirstPerson calls the existing PlayerMovement.Tick path. ThirdPerson calls TickThirdPerson only
when the existing PlayerCameraController.OwnsLook gate is true (active, configured TPS mode).
Both paths share the original acceleration, deceleration, sprint selection, vertical integration,
jump requests, coyote time, jump buffer, air control, capsule height/headroom and collisions.
No new input reader, action, sprint system, capsule, profile asset or Animator is introduced.

## Camera-relative directions

ThirdPersonMovementMath projects camera forward onto the world horizontal plane and normalizes
it. A perpendicular horizontal right axis makes the basis orthonormal even if a camera has roll.
For the existing unrolled orbit camera, this matches its normalized projected right vector.

```text
horizontalMove = cameraForwardHorizontal * Move.y + cameraRightHorizontal * Move.x
```

W/stick forward follows horizontal camera forward; S follows its negative. D follows camera
right; A follows its negative. ClampMagnitude limits the input to one without discarding analog
magnitude, so W+D cannot exceed W speed. Pitch never adds vertical movement. The basis uses
camera orientation, never Player-to-camera position: collision retraction does not change it.

Near a vertical camera forward, the projected camera right produces a stable forward fallback.
If both camera axes are degenerate, horizontal Explorer forward is used, then world north.
No zero vector is normalized. The existing Input System deadzone remains intact; residual TPS
Move magnitude below 0.01 is treated as zero. FPS receives exactly its original Move input.

The basis is taken from the currently rendered camera orientation in Update, before the next
LateUpdate camera smoothing pass. This follows the visible view with ordinary one-frame camera
pose latency; it does not bypass approved orbit smoothing or add recentering.

## Facing and smoothing

Explorer's root yaw is the one facing authority. CharacterController, CharacterVisualRoot,
ThirdPersonHumanProxy and InteractionOrigin inherit it. Visual orientation is not independently
offset from the collision/interaction forward. CharacterVisualRoot's existing local pose is
preserved; the provisional proxy keeps its current shape while crouching.

With significant Move input, root yaw follows the resulting accelerated horizontal velocity via
SmoothDampAngle. This preserves natural acceleration/air-control behavior: immediately after
reversing input, facing can briefly follow the remaining velocity until it reverses. There is
no instant snap and no dedicated backward/strafe locomotion. Walk and Sprint use the same turn.

TurnSmoothTime lives on PlayerMovement, initially 0.14 s, editable in the Inspector. No one-float
ScriptableObject is needed. Releasing Move immediately stops turn smoothing and clears its
velocity; ordinary horizontal deceleration still occurs. Orbit while stationary leaves root
orientation unchanged. Camera world orbit yaw is independent of the player's root yaw, so
walking does not recenter the camera.

## Preserved behavior

- Walk remains 4 m/s, sprint 6.5 m/s and crouch 2 m/s, using existing serialized tuning.
- SprintRequested and its gamepad latch/bindings are reused without changes.
- Jump, gravity, coyote time, jump buffer and terminal speed are reused. In-air direction uses
  the camera basis with the original 0.7 air-control response.
- Crouch uses the existing 1.8/1.0 m capsule heights, transition/headroom checks and eye heights.
- FPS still uses Explorer.right/forward, original Look pitch/yaw and sprint FOV 75-to-78.
  After TPS has turned Explorer, returning to FPS synchronizes PlayerLook's internal yaw with
  the current root before the first FPS Look tick; it does not return to a stale yaw.
- PlayerController's existing gameplay gate and input map gate block movement/turning for
  Pause, Journal and modal UI. Camera orbit retains its existing gates. StopMotion clears turn
  velocity together with the existing movement state.
- Interaction still uses camera aim and the stable chest target with canonical 3 m reach,
  first-solid occlusion, self/trigger filtering and rejection behind the player's facing.
  That forward follows the smoothed root facing; while turning, eligibility can change until
  the target is in front. Prompts, Door, puzzles and SecretCommand are unchanged.

TutorialMovementStep already measures horizontal displacement, so camera-relative movement
counts without a step change. TutorialLookStep already measures TPS orbit. Sprint measures
displacement while IsSprinting; Jump/Crouch/Interact steps retain their references and conditions.
Level01 doors, checkpoints, Journal, Point of No Return, puzzles and secret bindings are untouched.
They continue to consume the same player state, position, locks and interaction contracts.

## Validation and tests

Run **Tikal/Validation/Validate Third Person Movement** in Edit Mode for:

- Nineteen scene-free math checks: W/A/S/D, east/west camera headings, diagonal/analog magnitude,
  pitched/vertical/degenerate camera axes, drift/no input, gradual/convergent turn and yaw wrap.
- Explorer prefab and Tutorial/Level01/PlayerMovementTest references using the existing camera
  foundation validator, plus unique capsule/movement/input/camera components and valid turn time.
- Source contracts for TPS versus FPS routing and the shared input-map lock.

Run the same menu in Play Mode (prefer PlayerMovementTest) to add disposable physics fixtures.
They test camera-relative displacement and smoothed facing, stationary orbit, diagonal speed,
existing sprint/jump/crouch, root-relative FPS movement, FPS Look/resumed yaw and disabled-map
freezing. Their rendering camera and AudioListener are disabled before activation; cursor and
volume are restored and no preferences are saved. The existing camera-foundation behavior menu
continues to supply interaction, orbit and collision regression checks.

The math-check source can also run under a console host with Unity's managed math assembly;
this requires no Unity process, graphics or batchmode. Static compile covers runtime and Editor
assemblies and compares warnings with the baseline. Physics fixtures and gameplay feel still
require the interactive Editor; a compile or math-only pass does not establish those results.

No movement authoring is necessary: Explorer already has all components and camera references.
The new serialized turn field receives its code default on existing objects. Keep Inspector
tuning as normal prefab/scene overrides and save it through Unity if adjusted.

Manual playtest before approving v2:

1. TPS root north: orbit 90 degrees west while stationary; body stays north.
2. Press W: move west and smoothly turn west; camera stays in free orbit.
3. Test W+D and analog input: no diagonal speed boost or unwanted drift turn.
4. Sprint, jump and steer in air; crouch through the existing low passage.
5. Try camera collision near walls; retracting the arm must not rotate movement axes.
6. Verify player-range interaction, prompts, doors, puzzles, Journal/locks and checkpoints.
7. Switch to FPS: approved movement/Look/FOV/interaction remain; no stale-yaw snap.
8. Replay Tutorial Move, Look, Jump, Crouch, Sprint and Interact in both modes.

## Future Humanoid integration

Replace the visual under CharacterVisualRoot without changing this movement/camera rig.
Existing CurrentSpeed, IsGrounded, IsSprinting, IsCrouching and VerticalVelocity provide useful
Animator inputs; normalized speed can be derived when the final locomotion design is known.
There is no mesh, bone or Animator dependency here. Humanoid/2K Idle/Walk/Run/Jump/Crouch/
CrouchWalk/Turn animations, blend trees, root motion and IK remain future work. Camera recenter,
shoulder swap, lock-on, strafing animation and combat are not implemented in v2.
