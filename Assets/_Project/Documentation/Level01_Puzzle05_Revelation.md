# Level 01 — Revelation chamber

Unity 6000.6.3f1 / Input System 1.20.0.
Puzzle ID: level01.revelation. Zone: Zone05_Revelation.

## Concept and layout

Four upright stone steles ask the player to recognize the same fundamental
shape on two distinct objects. This is visual matching, rather than alignment,
orientation, memory or sequential counting. No runtime randomization occurs.

Along increasing world X, A/B/C/D have stable PatternIds Cycle/Path/Cycle/Star.
A+C (in either selection order) is the only correct pair. X coordinates are
-4.5, -1.5, 1.5 and 4.5 metres, with Z=70.8/71.0 alternating.
Native terrain raycasts place their bases on the existing ground.

Cycle is a segmented circular shape with a top opening; Path is angular;
Star has radial spokes. C's cycle is 86% of A's scale, with a secondary wear
detail. Recognition relies on count/composition/shape, not colour alone.
These are neutral provisional geometric patterns, not claims of authentic
Maya glyphs. Final historically/culturally reviewed art can replace them.

Existing monumental structures, terrain, vegetation, natural closure and
return route are preserved. The centre route between B and C remains open.
The central movable stone seal uses RevelationAnchor and adds no collider.

## Architecture and selection

RevelationPuzzle derives from PuzzleBase and validates exactly four owned
steles with one logical duplicate pattern and two distinct singleton patterns.
Comparison uses ordinal stable PatternId strings, never meshes or materials.
RevelationStele implements IInteractable, offers Activar, owns selected feedback
and delegates selection. It has no physical input, UI, clue, journal or final
outcome responsibility.

The first selection remains softly illuminated and cannot select itself twice.
The second evaluates the pair with a reentrancy guard. A mismatch briefly gives
both symbols a muted amber/red response for 0.18 real-time seconds, then clears
both selections and immediately enables retry. Additional input during that
brief evaluation is rejected. There is no loss, penalty, scene reload or clue
removal. Disabling the unsolved puzzle safely clears pending selection/coroutine.
Framework reset clears selection; solved is terminal.

A matching pair calls the protected base TrySolve once. Both correct symbols
remain warmly illuminated. All steles become unavailable after completion.
RevelationMonument listens to PuzzleSolved: its stone seal rises 1.15 metres
and exposes a warm interior. Re-enable cannot raise it again. This deliberately
simple blockout reaction can later be replaced with final art/animation.

## Help, reward and journal

The optional ReadableInscription says:
“Lo que aparece al principio vuelve al final.”
Its action is Leer. It uses the existing inscription presenter and never
registers in ClueTracker.

PuzzleClueReward grants the existing fifth clue asset, preserving its GUID:
- ID: level01.clue05
- Order: 5
- Title: El ciclo
- Description: El inicio es el final.

Discovery remains at the existing location. ClueTracker deduplicates reward.
Journal displays exactly the discovered clues in authored order:
El camino; El origen; El primer contacto; El séptimo lugar; El ciclo.
No extra solution, return instruction, key instruction, waypoint or Door hint
is inserted. Five clues remain consultable after the notification expires.

Dynamic Prompts remains E / Xbox X / PlayStation square with Activar.
Reticle Dot/Cross/None keeps its existing logic. Journal gates interaction and
restores gameplay/cursor state. No physical binding or generated wrapper changes.

## After completion and metrics

The player remains free in Zone05 and can return along the original route.
The entrance Door remains conventional and unchanged. SecretCommand still has
no gameplay consumer: G / Xbox Y / PlayStation triangle do nothing to this
puzzle, the Door or a level outcome, including after discovering all five clues.

There is no victory, defeat, teleport, false path, special final warning stele,
Level Complete or sixth puzzle in this milestone.

In Editor/development builds, SelectionCount, IncorrectAttempts and
SecondsToSolve record accepted selections, mismatches and elapsed real-time from
the first accepted interaction to Solve. REVELATION_METRIC logs them on Solve.
They are diagnostics, not score; counters exclude rejected input and persist
through retries/reset within this play session.

Design targets: 15–30 seconds for an observant player and 20–60 seconds for a
new player. These are design targets, not measured human playtest results.

## Validation

RevelationValidation checks native scene references, unique pair, shape
composition, first selection, duplicate rejection, guarded wrong-pair reset,
retry, keyboard/XInput/DualShock input, matching in reverse order, one solve,
persistent visuals, central reaction, unique reward, all five journal entries
and authored order, optional reading, Discovery expiry, gating, reticle modes,
development metrics and inert secret inputs before/after completion.

Level01Validation physically approaches all four steles and the help inscription
with the real CharacterController, then reaches the terminal dais and returns
to the original Door/spawn. No player teleportation or jump is used.
The complete Input, Player, Feel, Interaction, Dynamic Prompt, Framework,
Puzzle01–04, Journal and Gameplay Comfort regression suites remain required.

Authoring uses Unity Editor APIs. Native .meta files are versioned.
Packages and ProjectSettings are unchanged.
Automated checkpoint: 366 Revelation checks passed, alongside all twelve
native batchmode suites and the GPU blockout preview. The synthetic solve
recorded five accepted selections and one incorrect attempt; elapsed time in
this synthetic test is not a human difficulty measurement.
