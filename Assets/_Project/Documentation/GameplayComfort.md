# Gameplay comfort

Unity 6000.6.3f1 / Input System 1.20.0.

## Sprint

The existing Sprint action and bindings are unchanged: Keyboard Left Shift and
Gamepad leftStickPress. The latter is the click of the LEFT stick, commonly L3
(on Xbox, LS); R3 names the RIGHT stick. This feature preserves leftStickPress.

PlayerInputReader resolves device-specific sprint intent. IPlayerInputReader
exposes SprintRequested for gameplay and retains SprintHeld as the raw action
button hold. PlayerController passes SprintRequested to PlayerMovement.
Movement physics, walk/sprint/crouch speeds, jump, coyote time and jump buffering
remain unchanged. PlayerMovement has no device families or physical bindings.

KeyboardMouse uses the held controls bound to the semantic Sprint action:
holding Shift sprints; release returns to walking. There is no keyboard toggle,
and a held gamepad button does not become a keyboard hold after switching devices.

Xbox, PlayStation and GenericGamepad use the same policy:

1. Move above the neutral threshold.
2. Press Sprint once to latch sprint.
3. Releasing the Sprint button does not clear it while moving.
4. Move at or below the neutral threshold clears the latch.
5. Moving again walks until another Sprint press.

Pressing Sprint while stationary does not arm a future sprint. Holding that
stationary press while beginning movement also does not arm it. Pressing again
while already latched keeps sprint active; this is not an on/off toggle.

The configurable inspector field Gamepad Sprint Neutral Threshold defaults to
0.15, applied to the magnitude of the PROCESSED Move action (after the stick
deadzone). No exact Vector2.zero comparison is required. Move performed/canceled
callbacks clear the latch at or below the threshold; repeated neutral input stays
unlatched.

InputDeviceManager supplies the existing InputDeviceFamily. Family changes clear
the latch, including gamepad to KeyboardMouse and the reverse. DisableGameplay,
component disable and re-enable clear it. Global stays enabled when only gameplay
is disabled, so Journal can close normally. Opening Journal resets sprint;
closing it does not resume gamepad sprint, even with the stick/button held.

Older serialized input test objects without InputDeviceManager receive the
required component at runtime. Existing Explorer already contains it.

## Reticle

ReticleMode has stable serialized values None=0, Dot=1, Cross=2. Default is Dot.

ReticleSettings is a small in-memory state component. SetMode publishes ModeChanged
only when the selected mode changes and rejects unsupported enum values.
A future Settings/Persistence flow can store the enum and apply it through SetMode.
No settings file, PlayerPrefs, save implementation, sliders or settings menu are
introduced here.

ReticlePresenter only presents this state and gameplay availability. It does not
raycast, read keys, detect interactables or know doors/puzzles. GameplayEnabledChanged
updates visibility without frame polling. SetModalVisible allows a future modal
or pause flow to request hiding without changing the selected mode.

Reticle.prefab is independent of InteractionPrompt, Discovery and ClueJournal.
Its anchors, pivot and anchored position put it exactly at the Canvas centre.
Dot uses a 4 reference-pixel light native Unity circle over a 7-pixel dark circle.
Cross uses 10x2-pixel light bars over 12x4-pixel dark outlines, so the light core
remains visible when the Canvas scales down. All images are
non-blocking (raycastTarget=false). Light and dark layers preserve contrast.

Level01 reuses its existing Canvas and CanvasScaler. InteractionPrompt remains
at the lower left (50,50 reference pixels), Discovery at the top (0,-50), and
Journal remains modal. Reticle is stable regardless of focus and does not reveal
puzzle solutions.

Journal disables gameplay, which hides the reticle. Closing restores the selected
mode; None remains hidden. Independently disabled gameplay, presenter/input
lifecycle changes and external modal requests also suppress it appropriately.

## Validation

GameplayComfortValidation uses native Input System events with Keyboard, Mouse,
XInputController, DualShockGamepad and Generic Gamepad. It covers hold/latch,
button release, nonzero processed drift, configurable threshold, stationary press,
device changes, held-button resume, gameplay gating, Journal, lifecycle listeners,
reticle modes, modal requests and existing walk/sprint/crouch/jump behavior.

Centre and separation checks use the existing CanvasScaler layout at:
1920x1080 and 1280x720 (16:9), 1920x1200 and 1280x800 (16:10), and
1280x960 (4:3). Native URP previews are available through
GameplayComfortAuthoring.Preview without saving preview scene modifications.

Level01 navigation now exercises native gamepad Sprint and analog braking at
waypoints, rather than combining a gamepad movement stick with keyboard Shift.
Its physical route, colliders, traversal assertions and Door checks remain.

The full regression set also covers Input, Player, Game Feel, Interaction,
Dynamic Prompts, Puzzle Framework, Puzzle01/02/03, Clue Journal and Level01
navigation. Authoring uses Unity prefab/scene APIs and native .meta import.
No Puzzle04, SecretCommand consumer, Pause Menu or other gameplay is added.
