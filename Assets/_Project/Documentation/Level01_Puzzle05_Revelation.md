# Puzzle05 — Secret Key Discovery

Puzzle05 in `Zone05_Revelation` is now the discovery of the SecretCommand key. The old playable `RevelationStele` pair-selection mechanic is retired as the canonical interaction: its visual composition remains in the zone, but the selection components are removed from the scene.

Zone05 remains physically reachable before Puzzle04 is solved. In that early state there is no visible key symbol, no discovery stele, no `Examinar` prompt, no Clue05, and no visible `G/Y/△` representation.

## Gate

`Zone05RevelationGate` listens to Puzzle04's `PuzzleSolved` event. When Puzzle04 is solved it activates the narrative content and makes `SecretKeyDiscoveryPuzzle` available. It is event-driven and catches up safely if enabled after Puzzle04 has already been solved.

This dependency exists only for `Puzzle04 -> Puzzle05`. Puzzle01, Puzzle02, and Puzzle03 remain independent.

## Discovery

`SecretKeyDiscoveryPuzzle` derives from `PuzzleBase` and implements `IInteractable`.

- Interaction text: `Examinar`
- Available only after `Zone05RevelationGate` reveals the content
- One interaction solves the puzzle
- No combination, order, or extra selection is required
- `PuzzleClueReward` grants Clue05 once

The key is represented by `KeySymbol_VisualProxy`, a provisional fictional key silhouette. It is not claimed to have Maya historical meaning and can be replaced by final art.

## Clue05

- ID: `level01.clue05`
- Title: `La llave`
- Text: `Ya conoces la llave.`
- Semantic content: `SecretCommand`

The clue data does not hardcode `G` or instruct the player to return to the initial door. It stores the semantic meaning, and presentation resolves the current binding through `InputPromptResolver`.

## Device-Aware Presentation

`CluePresentation` formats semantic clues for Discovery and Journal using `PlayerInputReader.SecretCommandAction` and `InputDeviceManager.ActiveDevice`.

Expected display:

- Keyboard: `[ G ]`
- Xbox: `[ Y ]`
- PlayStation: `[ △ ]`
- Generic gamepad: `[ Botón norte ]`

The Journal updates while open when the active device changes. A rebinding of the SecretCommand action is reflected by presentation without changing the stored clue.

## Final Path

After Puzzle05 is solved, `FalsePathReveal` opens the narrow final path. The path and defeat trigger are tied to Puzzle05 progress, not to mere physical access to Zone05.
