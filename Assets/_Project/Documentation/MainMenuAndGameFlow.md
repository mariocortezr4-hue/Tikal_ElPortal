# Main Menu And Game Flow

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

`MainMenu.unity` is authored as a functional placeholder with:

- JUGAR / CONTINUAR
- NIVELES
- PERSONAJE
- CINEMATICAS
- TUTORIAL
- CONFIGURACION
- CREDITOS
- SALIR

Level Select shows unlocked levels only after onboarding is complete. Intro and Tutorial are not shown as levels. Cinematics shows Intro only after it has been seen. Tutorial replay appears after tutorial completion.

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
- Reticle: None, Dot, Cross

`PersistentSettingsApplier` can apply persisted settings in gameplay scenes. `PlayerLook.ApplySettings` updates sensitivity and invert Y. `ReticleSettings.SetMode` updates the existing reticle system.

## UI Input

Menu scenes use the existing `UI` Action Map through `InputSystemUIInputModule`. `Navigate`, `Submit`, `Cancel`, `Point`, `Click` and `ScrollWheel` are wired from `TikalInputActions`.

Cancel returns from submenus to the Main Menu. It does not quit the game.

## Manual Editor Checklist

A. `Tikal/Testing/Reset Local Progress` -> MainMenu shows JUGAR -> JUGAR -> Intro -> Tutorial -> Level01.

B. Reset -> JUGAR -> close during Intro -> return to MainMenu -> CONTINUAR -> Intro.

C. Complete Intro -> close during Tutorial -> return to MainMenu -> CONTINUAR -> Tutorial.

D. Before onboarding is complete -> Levels -> Level01 is not accessible.

E. Complete onboarding -> MainMenu -> CONTINUAR -> Level01.

F. After onboarding -> Levels -> Level01 is available.

G. Replay Intro -> MainMenu.

H. Replay Tutorial -> MainMenu.

## Responsibilities

`GameFlowController` decides first run, continue, intro completion, tutorial completion, replay behavior and level launch.

`GamePersistenceService` owns save, load and reset of progress/settings.

`MainMenuController` presents state and delegates to `GameFlowController`.

`SettingsMenuController` edits settings and saves them through `GamePersistenceService`.

`CharacterSelectionController` edits `SelectedCharacterId` and saves it through `GamePersistenceService`.
