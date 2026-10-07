# Checkpoint and respawn system

## Scope and authority

One CheckpointManager per gameplay scene owns a registry and CurrentCheckpoint. CheckpointAnchor
provides a stable ID and world position/rotation. CheckpointTrigger accepts only the manager's
actual Player CharacterController, independent of MainCamera and camera mode. Activation is
silent and each trigger accepts its first successful activation. Start registers authored anchors,
rejects duplicate IDs, and automatically activates the initial anchor before player traversal.
Register/Unregister support later scene integration without owning puzzle or save state.

Checkpoints are session-local. Loading the level again begins at the canonical initial spawn.
The manager writes no PlayerPrefs, progress JSON, autosave or world snapshot.

## Level01 integration

Apply Level01 Checkpoints creates/reuses level01.start at the authored Player spawn. It adds a
single manager on the existing outcome object, a trigger under the anchor, and an overlay Image
on the existing HUD Canvas. It wires the existing Pause adapter, outcome and temporary trap reset
hooks. It refuses duplicate managers/start anchors, preserves authored anchor positions on repeat
runs, and creates no additional Canvas/EventSystem or puzzle objects.

Only level01.start is authored in v1. The final section is revealed dynamically by solved puzzles;
there is no canonical, validated safe respawn marker before its irreversible boundary. Choosing
level01.finalApproach now would guess spatial design. Later, place an anchor on stable walkable
geometry strictly BEFORE PointOfNoReturn, with standing clearance and a safe camera boom, add its
trigger, rerun authoring to register it, and validate both camera modes. Never place it inside or
beyond the false-path trap.

## Player, camera and UI

Respawn closes Pause and Journal, blocks Player input, restores timeScale=1, disables the capsule,
sets spawn position/rotation, and calls PlayerMovement.ResetForRespawn. The reset removes
horizontal/vertical velocity, jump request/buffer, coyote time, consumed jump, sprint and turn
momentum; it chooses standing height only when headroom permits it. Gameplay input disable clears
the gamepad sprint latch. Capsule enablement is restored in finally, followed by SyncTransforms.

PlayerLook resets yaw/pitch, eye position and temporary sprint FOV. PlayerCameraController snaps
its FPS or TPS pose and resets smoothing velocities; TPS collision checking still shortens the
boom when needed. Current camera preference, sensitivity, InvertY and reticle preference are
preserved. PlayerInteraction clears focus, cached colliders, queued requests and resolved aim data.
ReticlePresenter and its None/Dot/Cross logic are unchanged.

Pause uses the existing ICheckpointRestart contract and enables Restart when the manager has a
valid destination and is idle. Pause.Resume runs before local Respawn; no confirmation modal was
added. Tutorial retains its existing restart-from-start behavior and has no checkpoint manager.

## Defeat and temporal state

Previously LevelOutcomeController's terminal Retry reloaded the scene. Authored Level01 now uses
CheckpointManager: HAS QUEDADO ATRAPADO, a short realtime delay, fade to black, teleport/reset,
restore outcome to Playing, fade in, then restore Journal availability and player control. Busy
respawns reject parallel requests; Retry during this flow does not start another reload. Victory
keeps the existing terminal retry behavior.

PointOfNoReturnTrigger implements IRespawnResettable: it clears only its activation flag and hides
the temporary closing passage. FalsePathReveal stays revealed. Solved Puzzle01–05, discovered
clues, ClueTracker/Journal data and the main Door logical state are untouched. No PuzzleBase is
configured as resettable. SecretCommand entrance eligibility remains unchanged.

Missing manager or invalid checkpoint retains the existing terminal Retry scene reload. A
checkpoint that becomes invalid during respawn or a reset exception uses GameFlow's scene restart
fallback, restoring timeScale. Authoring validation guarantees the normal initial destination.

## Unity workflow (second commit)

This source commit does not modify scene YAML. In native Unity Editor:

1. Outside Play Mode run Tikal/Authoring/Apply Level01 Checkpoints.
2. Run Tikal/Validation/Validate Level01 Checkpoints in Edit Mode.
3. Start Level01, close Pause/Journal, and run the same validation in Play Mode.
4. Manually test initial Pause restart, solved puzzles and Journal preservation, FPS/TPS snap,
   False Path defeat/fade, repeated trap attempts, Interact, Journal, Pause and SecretCommand.
5. Save the materialized scene and review its diff for a separate scene/playtest commit.

The authoring command saves Level01 only after reference/spawn validation succeeds and restores
the previous editor scene setup. Existing authoring tools that regenerate Level01 should be
followed by Apply Level01 Checkpoints again.

## Validation and limits

Edit validation checks one manager, critical references, stable unique IDs, initial anchor,
registered destinations, ground/standing clearance, position before trap bounds, Pause/outcome
wiring, reset scope, Canvas/EventSystem count and Missing Scripts. Run it only on authored Level01.

Play validation temporarily isolates physical input and checks A respawn, A→B respawn, rotation,
invalid activation, velocity/jump/coyote/sprint cleanup, FPS/TPS destinations and camera proximity,
Pause close/time/control restoration, defeat fade and busy rejection, scene identity, preserved
puzzle/clue/Door state and seeded temporary trap closure reset. It restores settings/input devices
and removes its temporary B anchor. Invalid-anchor availability and fallback source contract are
checked without deliberately reloading the test scene. Test after solving puzzles as well as from
a fresh level to check non-empty progression. The prior final-puzzle journey validator now accepts
local defeat respawn and explicitly reloads only its fixture for the next fresh-level journey.

Static compilation uses Unity's installed Roslyn compiler and existing project references, without
launching Unity or batchmode. Native authoring and Play Mode/manual behavior remain pending until
the workflow above is performed; static compilation does not validate rendered UI or physics.

## Future Level02

Reuse the same manager, anchors, actual-Player triggers, UI adapter and explicit temporal reset
hooks. Potential stable IDs: level02.cp01, level02.cp02, level02.cp03, level02.cp04. Select a safe
initial anchor and register all anchors through scene authoring. Add reset hooks only for temporary
traps that need them; leave puzzle/clue progression outside this authority. No Level02 checkpoints
or scene objects are implemented in this feature.
