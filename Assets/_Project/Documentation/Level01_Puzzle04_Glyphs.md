# Puzzle04 — Observation Alignment

Puzzle04 in `Zone04_GlyphGallery` is now an observation and camera alignment puzzle. The old playable `GlyphStone` sequence is retired as the canonical mechanic: its blockout visual objects remain part of the composition, but their interactive sequence components are removed from the scene.

The player discovers a small altar, stands on it, observes the environment, and holds the correct view long enough to solve the puzzle. This keeps the terrain, vegetation, terrace, anchors, structures, navigation, and backtracking intact while changing the mechanic.

## Guidance

The local inscription uses `ReadableInscription` with interaction text `Leer`:

> Debes aprender a observar.

It is not a clue, does not enter the Journal, and does not change progress. It only teaches the design idea.

## Architecture

`ObservationAlignmentPuzzle` derives from `PuzzleBase` and stays device-agnostic. It depends on `ObservationPoint`, a target transform, and visual fragments. It does not know about Zone05, the key, UI, clue presentation, or physical devices.

`ObservationPoint` owns the spatial references: player, camera, input reader, and `ObservationZone`. The zone is a forgiving volume around the altar, not an exact coordinate check.

## Tolerance

The authored threshold is:

- `dot >= 0.97`
- hold time `0.35` seconds
- `ObservationZone` size `1.8 x 0.35 x 1.8`

The check runs while the player is inside the zone and gameplay input is enabled. Moving out, looking away, or interrupting the alignment resets the held time. The puzzle uses camera orientation and a dot product; it does not use keyboard-specific logic.

Mouse, Xbox right stick, PlayStation right stick, and generic gamepad right stick are validated through the production look path.

## Feedback

The fragments glow subtly as the player approaches the correct orientation. Solving creates a short pulse and leaves the pattern illuminated. There are no arrows, percentages, "correct" labels, or pixel-perfect requirements.

## Clue04

- ID: `level01.clue04`
- Title: `La mirada`
- Text: `No todo se revela desde cualquier lugar.`

The clue does not mention `G`, `Y`, `△`, the door, or `SecretCommand`.

## Special Dependency

This is the only authored progress dependency between the late Level01 zones:

`ObservationAlignmentPuzzle` -> `PuzzleSolved` -> `Zone05RevelationGate` -> reveals Puzzle05 content.

Puzzle01, Puzzle02, and Puzzle03 are not prerequisites for Puzzle04.
