# TUT_ExplorerBasics

`TUT_ExplorerBasics` replaces the old tutorial placeholder with a short playable expedition approach. It is authored from **Tikal/Authoring/Build Explorer Tutorial** and validates from **Tikal/Validation/Validate Explorer Tutorial**.

## Goal and duration

The intended first play is two to four minutes. The route is a compact jungle-and-ruin blockout: an expedition path, a root, a low fallen lintel, an open sprint stretch and an exit stone. It teaches controls through the route rather than through an instruction room.

## Sequence

1. **MUÉVETE** — accumulate real planar movement.
2. **OBSERVA TU ENTORNO** — accumulate meaningful player yaw or pitch change.
3. **SALTA** — use the real Jump action at the root.
4. **AGÁCHATE** — enter the low-passage trigger while physically crouched.
5. **CORRE** — cover distance while the existing `PlayerMovement` reports sprinting.
6. **INTERACTÚA** — use the existing `PlayerInteraction` flow on the exit stone.
7. **CONTINÚA POR EL SENDERO** — cross the unlocked exit trigger after the six control steps.

Each control step shows `✓ COMPLETADO` for 0.8 seconds and then presents the next objective automatically. The final interaction opens the stone passage; it does not finish the tutorial. A warm environmental beacon and highlighted path lead the player through the exit trigger, which is enabled only after all six control steps are complete.

`SecretCommand` is deliberately absent from the tutorial and no secret control is displayed.

## Prompts and settings

`TutorialPromptPresenter` listens to `InputDeviceManager.ActiveDeviceChanged` and obtains semantic control text through `InputPromptResolver` and the generated Input System actions. It supports Keyboard/Mouse, Xbox, PlayStation and generic gamepads without tutorial-specific physical-control logic. Device changes refresh the active prompt without resetting step progress, and the presenter remains active while its objective and completion panels alternate.

The scene instantiates the existing Explorer prefab, interaction prompt and reticle prefab. `PersistentSettingsApplier` loads saved reticle, audio, sensitivity and invert-Y settings. Camera Mode remains a persisted preference; gameplay uses the existing functional first-person implementation until third-person runtime is built separately.

## Flow

`GameSceneIds.Tutorial` keeps its stable `tutorial` ID and now points at `Assets/_Project/Scenes/Tutorial/TUT_ExplorerBasics.unity`.

- First run: Intro completion opens the tutorial. Crossing the final exit calls `GameFlowController.CompleteTutorial()`, records `TutorialCompleted`, sets `CurrentLevelId` to Level01, and loads `Level01_SecretEntrance` after a brief completion fade.
- Replay: Main Menu calls `LaunchTutorial()`. Crossing the final exit returns to Main Menu without changing progress, selected character or the current level.
- Interruption: tutorial progress is intentionally not checkpointed. An incomplete tutorial is restarted from its beginning through the existing Continue flow.

## Pending assets

The authored route uses Level01 materials and primitive vegetation/stone proxies. Jungle ambience, final footsteps and polished environmental meshes remain hooks for the Tikal asset library; no external audio or art is required for the tutorial to run.
