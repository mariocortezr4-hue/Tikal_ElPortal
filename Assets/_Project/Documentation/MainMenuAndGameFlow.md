# TIKAL: El Explorador — Main Menu And Game Flow

This milestone adds the base architecture for menu navigation, game flow and persistence. It does not build the final elder cinematic or the real tutorial.

## First Run

`GameProgressData.IntroSeen == false` routes to Intro. `GameProgressData.TutorialCompleted == false` routes to Tutorial. `HasStartedGame` is useful for menu labeling, but it is not the authority for entering Level01.

```text
MainMenu
-> IntroPlaceholder
-> TutorialPlaceholder
-> Level01_SecretEntrance
```

The Play button displays `JUGAR`. Starting a new game marks the game as started, sets the current level to `level01.secret_entrance`, keeps Level01 unlocked, and loads Intro.

Completing the intro in the initial flow sets `IntroSeen = true` and loads Tutorial. Completing the tutorial sets `TutorialCompleted = true`, keeps `CurrentLevelId = level01.secret_entrance`, and loads Level01.

## First Run Onboarding Gate

Fresh install:

```text
MainMenu
-> IntroPlaceholder
-> TutorialPlaceholder
-> Level01_SecretEntrance
```

Interrupted Intro:

```text
MainMenu
-> Continue
-> IntroPlaceholder
```

Interrupted Tutorial:

```text
MainMenu
-> Continue
-> TutorialPlaceholder
```

Completed onboarding:

```text
MainMenu
-> Continue
-> CurrentLevel
```

Level Select cannot skip onboarding. Level01 may be visible as the known first level, but it is not interactable until both `IntroSeen` and `TutorialCompleted` are true.

## Returning Player

`GameProgressData.HasStartedGame == true`

```text
MainMenu
-> Continue
-> CurrentLevel
```

The Play button displays `CONTINUAR` and shows the current level title. Intro and Tutorial do not replay automatically.

## Replay

```text
MainMenu
-> Cinematics or Tutorial
-> MainMenu
```

Replay Intro returns to Main Menu and does not send the player to Tutorial. Replay Tutorial returns to Main Menu and does not change `CurrentLevelId`.

## Progress Data

`GameProgressData` is serializable and stores:

- `HasStartedGame`
- `IntroSeen`
- `TutorialCompleted`
- `CurrentLevelId`
- `SelectedCharacterId`
- unlocked level IDs
- completed level IDs

Stable IDs live in `GameSceneIds`. Level01 is `level01.secret_entrance`; Level02 is prepared as `level02` but has no scene path yet.

Character IDs live in `CharacterIds`. The default is `character.default`; `character.02`, `character.03` and `character.04` are registered for future playable characters.

## Settings Data

`GameSettingsData` is separate from progress and stores:

- master, music and SFX volume
- mouse sensitivity
- gamepad sensitivity
- invert Y
- camera mode (FirstPerson by default, ThirdPerson selectable)
- reticle mode
- prepared graphics quality, display mode and resolution fields

Defaults preserve the current feel: mouse sensitivity `0.12`, gamepad sensitivity `150`, invert Y off, reticle Dot.

## Persistence

`GamePersistenceService` writes JSON in `Application.persistentDataPath`:

- `tikal_progress.json`
- `tikal_settings.json`

Missing files return defaults. Invalid or unreadable files fall back safely to defaults. Settings and progress are saved independently, so resetting progress does not reset user settings.

## Scene Flow

`SceneCatalog` maps stable scene IDs to scene paths and titles. UI code calls semantic operations on `GameFlowController`; button handlers do not decide scene routes directly.

Implemented scene IDs:

- `main_menu`
- `intro`
- `tutorial`
- `level01.secret_entrance`
- `level02` prepared without a scene

## Menu

`MainMenu.unity` uses a cinematic Tikal jungle and ruins backdrop, a vertical navigation column on the left, a journey card on the right, and dark stone panels with amber details for secondary menus. It contains:

- JUGAR / CONTINUAR
- NIVELES
- PERSONAJE
- CINEMATICAS
- TUTORIAL
- CONFIGURACION
- CREDITOS
- SALIR

Level Select keeps Level01 visible but disabled until onboarding is complete. Intro and Tutorial are not shown as levels. Their replay buttons remain visible but disabled until `IntroSeen` and `TutorialCompleted` respectively.

## Character Selection

```text
MainMenu
-> CharacterSelection
-> SelectedCharacterId
-> future CharacterVisualSpawner
-> CharacterVisualRoot
```

The Character panel stores the selected character in `GameProgressData.SelectedCharacterId`, not in settings. New installs use `character.default`. If a saved ID is no longer registered, progress falls back safely to `character.default`.

All characters share the same gameplay architecture. Future models, rigs, animators, materials and animations should be attached as visual prefabs under `CharacterVisualRoot`; `CharacterVisualSpawner` is prepared to resolve `SelectedCharacterId` through `CharacterCatalog` without requiring prefabs in this feature.

## Settings

The initial settings panel applies and saves:

- Master
- Music
- SFX
- Mouse Sensitivity
- Gamepad Sensitivity
- Invert Y
- Camera Mode: Primera Persona, Tercera Persona
- Reticle: Ninguno, Punto, Cruz

All eight values are stored in `tikal_settings.json` and loaded when Main Menu opens. Camera Mode currently stores a preference for future gameplay camera implementation; selecting ThirdPerson does not change the active player camera yet. The same settings service can be used later by an in-game settings panel. `PersistentSettingsApplier` applies existing gameplay settings. `PlayerLook.ApplySettings` updates sensitivity and invert Y. `ReticleSettings.SetMode` updates the existing reticle system.

The visual pass is maintained by `Tikal/Authoring/Polish Existing Main Menu`. It edits the existing `MainMenu.unity` and can be run again without adding duplicate UI elements. `Generate Main Menu Flow` also applies this pass after generating the scene.

## UI Input

Menu scenes use the existing `UI` Action Map through `InputSystemUIInputModule`. `Navigate`, `Submit`, `Cancel`, `Point`, `Click` and `ScrollWheel` are wired from `TikalInputActions`.

Cancel returns from submenus to the Main Menu. It does not quit the game.

### Dynamic Menu Navigation

The current `EventSystem` selection is the single source of truth for keyboard and gamepad focus. Buttons and settings controls render a clear amber focus treatment, mouse hover transfers that same selection, and every panel falls back to its first active, interactable control when its preferred action is locked. Level01 stays visible but disabled until onboarding is complete, so Levels initially focuses VOLVER.

The bottom control bar listens to `InputDeviceManager.ActiveDeviceChanged` and resolves semantic Navigate, Submit and Cancel prompts through `InputPromptResolver`. It updates without polling for Keyboard/Mouse, Xbox, PlayStation and generic gamepads; VOLVER is hidden on the root menu and shown in secondary panels. Character focus is temporary, while the persisted character keeps a separate SELECCIONADO marker until confirmation.

## Manual Editor Checklist

A. `Tikal/Testing/Reset Local Progress` -> MainMenu shows JUGAR -> JUGAR -> Intro -> Tutorial -> Level01.

B. Reset -> JUGAR -> close during Intro -> return to MainMenu -> CONTINUAR -> Intro.

C. Complete Intro -> close during Tutorial -> return to MainMenu -> CONTINUAR -> Tutorial.

D. Before onboarding is complete -> Levels -> Level01 is not accessible.

E. Complete onboarding -> MainMenu -> CONTINUAR -> Level01.

F. After onboarding -> Levels -> Level01 is available.

G. Replay Intro -> MainMenu.

H. Replay Tutorial -> MainMenu.

I. Settings -> change Camera Mode and Reticle -> leave and reopen the menu -> both selections match the saved values. Audio, sensitivity and Invert Y should also retain their chosen values.

## Responsibilities

`GameFlowController` decides first run, continue, intro completion, tutorial completion, replay behavior and level launch.

`GamePersistenceService` owns save, load and reset of progress/settings.

`MainMenuController` presents state and delegates to `GameFlowController`.

`SettingsMenuController` edits settings and saves them through `GamePersistenceService`.

`CharacterSelectionController` edits `SelectedCharacterId` and saves it through `GamePersistenceService`.

## Main Menu visual identity — TIKAL: El Explorador

The canonical game title is **TIKAL: El Explorador**. Main Menu composes TIKAL as the dominant
heading and EL EXPLORADOR as a separate smaller subtitle, retaining UNA AVENTURA EN LAS SOMBRAS.
The repository, folders, namespaces, scene IDs and persistence IDs retain their technical names.
Intro/Timeline is outside this feature; its older title is left for a dedicated cinematic update.

The palette uses obsidian/charcoal, dark green stone, aged amber, restrained jade, warm ivory and
stone beige. The existing jungle/ruins backdrop and fonts are reused. Procedural stepped corners,
thin carved borders and translucent stone surfaces create an architectural frame without fake
Maya glyphs or invented writing. No downloaded or generated texture assets are added. StoneSurface
Images are explicit future texture slots; character PreviewSurface areas remain placeholders.

Normal controls have dark fills, ivory labels and a subtle border. Focus adds an outer amber border,
a lateral accent and stronger text. Saved character cards keep a jade fill, a solid jade border and
a geometric check with SELECCIONADO. Focus on a saved card shows both borders and retains the
check. Disabled controls retain readable muted labels. The check uses two UI strokes, so it does
not require a Unicode glyph in the font atlas. Focus never writes character progress; existing
ELEGIR proposes a pending character and CONFIRMAR saves it through the unchanged controller.

Camera and reticle keep their existing dropdowns, indices, values and navigation. Below each,
a noninteractive option strip highlights only the current value with a jade fill, check and
ACTIVA/ACTIVO label. These strips are presentation, not additional controls. MenuSettingStatePresenter
reads existing dropdown/toggle/slider values, including silent loading, and never owns or writes
settings. Reticle display labels become Ninguna/Punto/Cruceta while the same three indices remain.
Volume sliders display percentages, sensitivities display numeric values, and InvertY explicitly
shows ACTIVADO/DESACTIVADO. No input actions, GameFlow, CharacterSelectionController,
SettingsMenuController, save format, camera or gameplay code changes.

Main actions and the existing level card retain their flow. Level availability/locked hints remain
owned by MainMenuController. The existing device hint resolver remains connected: Enter/Escape,
Xbox A/B, PlayStation cross/circle and generic south/east semantics are unchanged. Pause buttons
that share MenuSelectableVisual adopt the palette/focus text treatment; Pause functionality and
its scene authoring are unchanged. Only Main Menu receives the new decorative borders/readouts.

### Authoring and validation

Outside Play Mode, run **Tikal/Authoring/Polish Existing Main Menu**. The canonical tool reuses named
children/components and validates before saving MainMenu. Repeated runs should produce the same
UI structure. Generate Main Menu Flow also applies this same visual pass. Do not hand-edit scene
YAML. This source commit leaves the existing scene materialization for a second commit after QA.

Run **Tikal/Validation/Validate Main Menu Visual Identity** on the authored scene. This preview-scene
validation checks title/subtitle and absence of old Main Menu branding, sole controllers/EventSystem,
unique children, marker/border/preview support, combined focus/saved behavior, active-option markers,
readouts, navigation and responsive canvas. It temporarily changes only preview control/visual
state using SetValueWithoutNotify, restores it and never saves progress/settings. Existing
Validate Main Menu Flow also calls these structural checks.

For 1920x1080, 1280x720, 1920x1200 and 1280x800, the validator computes CanvasScaler layout bounds,
checks marker/control visibility and control overlaps, and checks the main action/card gap. This is
conceptual geometry validation, not rendered text or screenshot verification. Native Unity QA must
confirm text fits, glyphs, pointer/gamepad focus and dropdown expansion at all four resolutions.

Manual checklist: run Polish twice; verify no duplicates; open Character, move focus without
confirming, choose another pending character and confirm, then reopen to inspect the saved marker.
Check focused+saved together. Switch First/Third and all three reticle values, then reopen Settings;
markers must match the existing saved controls. Adjust five sliders and InvertY. Test locked and
available Level01, all eight main actions, keyboard/Xbox/PlayStation/generic hints, and Cancel.
No Unity batchmode is used. Scene authoring, structural menu execution and rendered/playtest QA
remain pending until these native Editor steps are performed; static C# compilation is separate.