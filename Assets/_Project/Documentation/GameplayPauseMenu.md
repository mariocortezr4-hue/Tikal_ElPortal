# Gameplay Pause Menu

## Authoring and integration

Run **Tikal/Authoring/Apply Gameplay Pause Menu** outside Play Mode, then
**Tikal/Validation/Validate Gameplay Pause Menu**. The authoring creates the reusable
GameplayPauseMenu prefab, attaches it to the existing HUD Canvas in TUT_ExplorerBasics and
Level01_SecretEntrance, and reuses the existing EventSystem (creates one only when absent).
It replaces only previously authored Pause instances, and reuses the materialized Main Menu
Settings panel, dropdown templates, typography and MenuSelectableVisual selection treatment.
Run the validation again in Play Mode and complete the manual playtest before committing
the generated prefab/scene changes separately. This source commit does not regenerate scenes.

## Lifecycle and input

PlayerInputReader.PausePressed reuses Global/Pause: Keyboard Escape and Gamepad start.
There is no second Pause binding or persisted pause flag. PauseMenuController queues Pause
and UI Cancel events into LateUpdate to avoid two operations when Escape is shared by both.
Pause toggles the entire overlay; Cancel returns from Settings/Controls to the Pause menu,
and from the main Pause panel resumes. Dropdown cancellation first dismisses the dropdown.
Pause LateUpdate runs before the existing camera/interaction LateUpdate, so a queued interaction
cannot execute after the overlay opens in that frame.
Continue is selected on opening; Settings selects its first slider; Back restores the originating
menu selection. Native InputSystemUIInputModule uses unscaled UI time. There are no menu fades
or scaled menu coroutines. Intro receives no Pause component.

Opening closes the Journal first, snapshots the resulting gameplay/map/cursor state, then
disables the Journal presenter and calls PlayerController.SetGameplayActive(false). This is
the existing gameplay authority: it stops motion and disables Player input including movement,
look, interaction and secret action. Global remains enabled. The reticle and interaction prompt
already follow GameplayEnabled. Journal cannot subscribe/receive navigation above Pause.
Time.timeScale becomes zero; Resume restores the prior scale, gameplay map, cursor and Journal
availability. It does not reset camera state or preferences. Disabling Pause also restores this
state; defeat/victory close the overlay without reactivating gameplay. Opening is rejected during
terminal LevelOutcome, GameFlow scene loads, Tutorial Exiting/Completed, or another gameplay lock.
Scaled Tutorial feedback and physics stop while paused; Tutorial exit fades cannot be paused.

## One settings system

The same SettingsMenuController and GameSettingsData used by Main Menu are reused. An optional
serialized PersistentSettingsApplier receives every runtime edit and broadcasts SettingsApplied.
The existing GamePersistenceService saves the same tikal_settings.json. No PlayerPrefs or new
settings schema is introduced. Reopening Settings reads the current gameplay settings.

CameraMode applies immediately through PlayerCameraController, including its existing deliberate
mode-change pose while UI input is locked. ReticleMode updates ReticleSettings immediately; the
reticle stays hidden while paused and shows the chosen mode on Resume. Mouse/Gamepad sensitivity
and InvertY apply through PlayerLook and its existing TPS consumption. Movement/camera algorithms
are unchanged. MasterVolume applies AudioListener.volume. MusicVolume/SfxVolume persist through
the existing settings service; there is no mixer/channel implementation in this repository, so
this feature does not promise separate audible channel changes.

## Controls and navigation

Controls resolves Move, Look, Jump, Sprint, Crouch, Interact, Journal and Pause through the live
PlayerInputReader actions and InputPromptResolver. SecretCommand never appears in this panel.
InputDeviceManager changes and binding changes refresh the visible labels/hints without polling
input each frame. UI Submit/Cancel supply Enter/Escape, Xbox A/B, PlayStation ×/○ and generic
south/east labels. Gamepad start is named Menu/Options/Start by the existing prompt resolver.
All families share the same bindings. Mouse, keyboard and gamepad use native UI navigation.
Runtime sliders reserve Left/Right for adjustment and use Up/Down to visit every setting.

## Restart and GameFlow

Level01 now connects ICheckpointRestart to CheckpointManager through Apply Level01 Checkpoints.
The existing button becomes available with a valid session-local checkpoint; Resume restores time
and input before local respawn. Puzzles, clues and Door state remain intact. Scenes that have not
been authored keep the button disabled. See CheckpointRespawnSystem.md for integration and tests.
Tutorial may restart from scene start, as requested, through GameFlowController. It does not mark
TutorialCompleted or change progress.

Return to Main Menu closes temporary Pause state, restores timeScale to one and calls
GameFlowController.ReturnToMainMenu. It does not delete/reset progress or change CurrentLevel.
The existing PlayOrContinue logic routes incomplete onboarding back to Tutorial and completed
onboarding to CurrentLevel. Quit uses the existing safe GameFlow implementation: logs in Editor
and Application.Quit in a build. Pause starts closed after every load.

## Validation and manual playtest

Edit validation checks bindings, all six actions/buttons, settings/applier references, scene
integration, one EventSystem, missing scripts and an initially closed overlay. Play Mode validation
uses virtual Keyboard/Xbox/PlayStation/Generic devices to open Pause through real Pause input,
check gameplay/Global locks, cursor, selection, Journal conflict, controls, Resume and lifecycle.
Actual Settings UI callbacks are exercised with isolated temporary persistence; the user's
settings/progress files are not overwritten. Camera/reticle/sensitivity/InvertY/master and saving
are asserted. Test cleanup restores settings, cursor, audio, time and virtual devices.

Manually test Escape/Options toggle, mouse/keyboard/gamepad navigation and dropdowns, FPS/TPS
mode changes, reticle changes after Resume, sensitivity feel, audio persistence, Tutorial restart,
Level01 local checkpoint restart, Main Menu→Continue with incomplete Tutorial and with Level01,
and Pause rejection during defeat/victory/Tutorial exit. No batchmode is used.

## Runtime integration repair

The first authoring implementation used Single() on the imported UI InputActionReference
sub-assets. The installed Input System importer creates both the current reference and a hidden
legacy compatibility reference for each action. Tutorial has no EventSystem yet, so creating its
UI input module failed on the first action reference before saving Tutorial or processing Level01.
The generated prefab alone therefore did not provide a Pause listener in either saved scene.

ActionReference now matches the canonical UI action ID and prefers its visible imported reference,
accepting the legacy duplicate without adding bindings or creating new actions. Scene saving is
checked, and authoring asserts that the configured enabled listener remains outside the hidden
overlay. Runtime rejects an overlay that contains its listener rather than deactivating itself.

Validation now checks all six imported UI action references, active controller/input/Canvas/UI
module, independent overlay, runtime subscription and Global/Pause enabled before input. Its
Play Mode checks use Escape and Start on virtual Xbox, PlayStation and generic devices both to
open and to close, asserting panel visibility, listener retention and initial Continue selection.
The Continue button is also exercised independently.

Run Tikal/Authoring/Apply Gameplay Pause Menu again outside Play Mode, then
Tikal/Validation/Validate Gameplay Pause Menu in Edit Mode and during playable gameplay in
each scene. Retest the physical controllers with Game view focused. This repair compiles runtime
and Editor sources without Unity batchmode; native authoring/Play Mode have not been run by the
agent. Actual delivery of Escape/Options from the user's physical devices was not recorded.
The generated prefab is preserved; protected gameplay code and existing scene files are unchanged.
