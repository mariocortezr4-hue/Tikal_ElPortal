# Level 01 — La Entrada Secreta (blockout)

Scene: `Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity`.

## Organization and route

`Level01_SecretEntrance` groups Systems (contextual UI), Player (Explorer prefab), Environment, Lighting and BlockoutMarkers. Environment contains SpawnArea, Entrance, the five named zones, Connectors and Boundaries. Each zone owns its floor, visible solid boundaries, primitive props and future anchors. Anchors contain only Transform components. Colored floor markers are separate, non-colliding geometry under BlockoutMarkers.

The route is a compact zigzag: entrance → open courtyard → orientation court → roofed chamber → elongated observation gallery → monumental dead end. The same route is reversible. No teleport, mandatory jump, one-way drop, invisible shortcut or functional puzzle is present.

| Space | Center X/Z (m) | Width × depth (m) | Height / character |
| --- | --- | --- | --- |
| SpawnArea | 0 / -8 | 16 × 16 | 3.6 m visible boundaries |
| Zone01_MuralFragments | 0 / 12 | 18 × 20 | 3 m walls, open |
| Zone02_PathGuardians | 29 / 12 | 20 × 18 | 3.5 m walls, local paths and pillars |
| Zone03_EchoChamber | 29 / 39 | 16 × 16 | 4.2 m walls with roof and alcove divider |
| Zone04_GlyphGallery | 0 / 39 | 22 × 14 | 3.8 m walls, blank observation panels |
| Zone05_Revelation | 0 / 67 | 22 × 22 | 6 m walls, pillars and 0.2 m terminal dais |

All four connectors are 10 m long and 4 m wide, with solid visible walls. Overall footprint is approximately 50 × 94 m. Floors meet at Y=0. The final low dais can be reached and left using the existing CharacterController step offset; jumping is unnecessary.

## Initial approach and entrance

Explorer starts at (0, 0.08, -10), facing +Z, 10 m directly before the gate. A 2.4 m baffle creates a left-hand approach bend; the upper gate remains visible and simple floor bands indicate the bend. This reconciles the requested 8–12 m spawn distance with 5–10 seconds of initial movement at the existing 4 m/s walking speed. Prompt acquisition through the approach measured 19.55 m / 5.90 s; the full walking path to the doorway plane is approximately 22 m.

EntranceDoor_Blockout is a prefab variant of StoneDoor_Blockout. Outer dimensions: 4.4 m wide, 5.2 m high. Leaf: 3.6 × 4.8 m. Root scale stays 1; hinge and child geometry are resized individually, avoiding a distorted rotation. Existing Door behavior, open angle and speed are retained. The passage is usable by Explorer in both directions.

The level uses the existing InteractionPrompt prefab wired to the scene Explorer, input reader, interaction authority and device manager. E / X / square select Interact; normal state labels are Abrir / Cerrar. The movement debug component is disabled only in this scene instance. There is no additional HUD.

## Anchors

Every zone contains PuzzleAnchor and ClueAnchor. Zone03 also has AudioAnchor; Zone04 has GlyphWallAnchor; Zone05 has RevelationAnchor. They reserve space without implementing scripts, inscriptions, audio, logic or narrative. No new consumer of SecretCommand exists. G / Y / triangle neither open the gate nor change gameplay or expose secret prompts.

## Measured route

Measured with real CharacterController collisions, Input System simulated devices, 0.02 s steps and brief input releases at waypoints. No player transform teleportation occurs in the route test. These measurements are reproducible automation, not a subjective physical-controller playtest.

| Segment | Distance | Walking time |
| --- | ---: | ---: |
| Door → Zone01 center | 12.04 m | 3.26 s |
| Zone01 → Zone02 | 29.08 m | 7.52 s |
| Zone02 → Zone03 | 27.09 m | 7.02 s |
| Zone03 → Zone04 | 29.09 m | 7.52 s |
| Zone04 → Zone05 center | 28.05 m | 7.26 s |
| Door → terminal anchor | 132.35 m | 34.58 s |
| Terminal → original Door | 132.40 m | 22.72 s with sprint |

Door → Zone05 center is approximately 125.35 m / 32.58 s. Terminal anchor adds 7 m. The return test includes deliberate braking before turns; a human route will vary. Exploration and future puzzles are not included in these times.

## Validation

Use **Tikal → Tests → Level 01 physical round trip** (`Tikal.Tests.Editor.Level01Validation.Run` in batchmode). It reopens the serialized scene, checks hierarchy, pure anchors, missing scripts and critical player/door/UI references, then physically traverses the approach, opens the Door through input, visits all five zones, reaches the dais, returns through the entrance, closes the original Door and returns to spawn. Keyboard, XInput and DualShock prompts and inert secret input are checked in the new scene. It exits batchmode with code 0 only on success.

Run **Tikal → Tests → Dynamic prompts and all regressions** separately for Input, Player, Game Feel, Interaction and Dynamic Prompt regression coverage. Existing Testing scenes, runtime scripts, input asset, Packages and ProjectSettings are preserved.

Lighting is directional plus flat ambient and one basic chamber fill light. Materials are six plain URP colors. No textures, production art, packages or runtime gameplay were added. Puzzle implementation and further integration are intentionally pending.
