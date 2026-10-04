# Level 01 — Glyph gallery

Unity 6000.6.3f1 / Input System 1.20.0.
Puzzle ID: level01.glyph_gallery. Zone: Zone04_GlyphGallery.

## Concept and narrative

This is an observable ordering task, distinct from mural alignment, guardian
orientation and auditory memory. Seven upright plates carry one to seven
countable geometric marks. All seven remain visible; no memorization is required.

The current diamond/square geometry is provisional blockout art. It does not
claim to reproduce authentic Maya numerals or writing. Final culturally and
historically reviewed assets can replace these shapes without changing values
or puzzle logic.

Activate values 1 → 2 → 3 → 4 → 5 → 6 → 7.
The number seven matters for the subsequent narrative deduction.

## Spatial layout

The fixed order along increasing world X is 3, 1, 5, 2, 7, 4, 6.
Viewed from the northern approach, screen left-to-right is 6, 4, 7, 2, 5, 1, 3.
The plate row sits in front of the existing southern ruin wall. X coordinates
are -7.35, -6.15, -4.70, -3.25, -1.80, -0.35 and 1.10 metres.
Z alternates between 34.80 and 34.92 metres; the westernmost value 3 sits at
Z=35.35 to expose all its marks ahead of the existing rock shoulder.
Terrain support is at Y=1.60.
Each slab is 0.96 metres wide and 1.56 metres high, over a low plinth.

Marks use varied neutral compositions: single, pair, triangle, square, square
with centre, two columns of three, and two rows of three plus one. Their count
and arrangement distinguish plates without relying on colour.

The existing terrace, terrain, vegetation, wall, anchors, route toward Zone05
and return route are preserved. The puzzle adds no gate or mandatory obstruction.
Seven progress marks form a frieze under GlyphWallAnchor, above the plate row.
Its support has no collision that could block traversal.

The entrance-side ReadableInscription says:
“Cada marca sigue a la anterior.”
Its action is Leer. It is mechanical guidance, does not become a clue and uses
the existing InscriptionPresenter.

## Rules and architecture

GlyphSequencePuzzle derives from PuzzleBase. ExpectedNextValue starts at 1.
A correct selection increments it; Progress reports accepted values.
Only the seventh correct selection calls the base protected TrySolve.
Solved is terminal: plates become unavailable and progress remains seven.

GlyphStone implements IInteractable. SequenceValue is an integer in 1–7.
It provides Activar, delegates selection to its puzzle and presents accepted
state through a material property block. It contains no physical bindings,
UI, tracker or command handling.

Configuration validates exactly seven owned plates with unique values 1–7
and valid visual references. Layout and sequence never randomize at runtime.
Reentrant selection is rejected while a selection is being processed.

An incorrect selection clears every accepted plate and resets ExpectedNextValue
to 1 without restarting the scene, losing or blocking input. Retry is immediate.
The regular PuzzleBase reset remains available while active; solved cannot reset.

GlyphProgressFeedback listens to semantic progress/error events. Each correct
selection illuminates one environmental mark. Errors clear those marks and
briefly pulse only the backing frame for 0.18 seconds; this never delays retry.
Completion leaves all seven marks and plates warm, with subtle backing glow.
No 1/7 counters, modern HUD, cinematic or memory sequence is added.

## Reward, journal and input

The existing PuzzleClueReward grants the existing clue asset, whose GUID is
preserved while replacing placeholder content:

- ID: level01.clue04
- Order: 4
- Title: El séptimo lugar
- Description: La respuesta ocupa el séptimo lugar entre las letras.

Discovery UI stays in its current position and presents this title/description.
Tracker deduplication prevents repeat rewards, including reward re-enable.

Journal retains El camino, El origen and El primer contacto, and appends
El séptimo lugar in authored order. It reveals no fifth clue. The existing
auto-sizing journal remains the presentation authority; no separate clue UI exists.

The intended conceptual link is seven → seventh letter. The game does not
spell out the alphabet count, name a secret key, instruct a key press or activate
a final ability. SecretCommand remains without a gameplay consumer after Clue04.

Dynamic Prompts continues to show E / Xbox X / PlayStation square with Activar.
Journal continues J / View / Share, gates gameplay and restores cursor state.
Dot/Cross/None remains stable; the reticle gains no puzzle-specific state.

Gamepad sprint now uses rightStickPress: Xbox RS, PlayStation R3 and Generic
Gamepad right-stick click. Left-stick click does not activate it. Keyboard Shift
remains hold; the 0.15 processed Move threshold and latch resets are unchanged.

## Difficulty and validation

Design targets: 20–40 seconds for a player who understands the mechanic quickly,
30–90 seconds for a new player. These are design targets, not measured human
playtest results. The visible counts and short sequence are intentionally simple;
the larger level challenge is connecting the narrative clues.

GlyphValidation checks scene references, seven unique values/counts, fixed layout,
initial state, native keyboard/XInput/DualShock activation, prompts, wrong/reset,
immediate retry, progress geometry, completion once, reward once, all four journal
entries, discovery expiry, cursor/gating, reticle modes and inert secret inputs.

Level01Validation additionally physically approaches all seven plates and the
guidance inscription with the real CharacterController, then continues to Zone05
and back to the original Door/spawn. No player teleportation or jump is used.
The existing Input, Player, Game Feel, Interaction, Dynamic Prompts, Framework,
Puzzle01/02/03, Journal and Gameplay Comfort suites remain regression coverage.

Authoring and asset updates use Unity Editor APIs; imported .meta files belong
in Git. No Packages, ProjectSettings, Puzzle05 or victory flow is changed.
