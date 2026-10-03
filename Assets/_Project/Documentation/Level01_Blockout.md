# Level 01 — La Entrada Secreta (blockout)

Scene: `Assets/_Project/Scenes/Levels/Level01_SecretEntrance.unity`.

## Organization and route

`Level01_SecretEntrance` groups Systems (contextual UI), Player (Explorer prefab), Environment, Lighting and BlockoutMarkers. Environment contains SpawnArea, Entrance, the five named zones, Connectors and Boundaries. Each zone retains its primitive props, future anchors and hidden safety boundaries. Current terrain surfaces, natural boundaries, vegetation masses and new ruins are grouped under Terrain, NaturalBoundaries, Vegetation_Blockout and Ruins_Blockout. Anchors contain only Transform components. Colored floor markers are separate, non-colliding geometry under BlockoutMarkers.

The route is a compact zigzag: entrance → open courtyard → orientation court → roofed chamber → elongated observation gallery → monumental dead end. The same route is reversible. No teleport, mandatory jump, one-way drop, invisible shortcut or functional puzzle is present.

| Space | Center X/Z (m) | Width × depth (m) | Height / character |
| --- | --- | --- | --- |
| SpawnArea | 0 / -8 | 16 × 16 | 3.6 m visible boundaries |
| Zone01_MuralFragments | 0 / 12 | 18 × 20 | 3 m walls, open |
| Zone02_PathGuardians | 29 / 12 | 20 × 18 | 3.5 m walls, local paths and pillars |
| Zone03_EchoChamber | 29 / 39 | 16 × 16 | 4.2 m walls with roof and alcove divider |
| Zone04_GlyphGallery | 0 / 39 | 22 × 14 | 3.8 m walls, blank observation panels |
| Zone05_Revelation | 0 / 67 | 22 × 22 | 6 m walls, pillars and 0.2 m terminal dais |

All four connectors are 10 m long and 4 m wide, with solid visible walls. Overall footprint is approximately 50 × 94 m. Floors originally met at Y=0; the current composition uses continuous varied elevations described below. The final low dais can be reached and left using the existing CharacterController step offset; jumping is unnecessary.

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
| Zone01 → Zone02 | 29.00 m | 7.50 s |
| Zone02 → Zone03 | 27.09 m | 7.02 s |
| Zone03 → Zone04 | 29.09 m | 7.52 s |
| Zone04 → Zone05 center | 28.05 m | 7.26 s |
| Door → terminal anchor | 132.27 m | 34.56 s |
| Terminal → original Door | 132.40 m | 22.72 s with sprint |

Door → Zone05 center is approximately 125.27 m / 32.56 s. Terminal anchor adds 7 m. The return test includes deliberate braking before turns; a human route will vary. Exploration and future puzzles are not included in these times.

## Validation

Use **Tikal → Tests → Level 01 physical round trip** (`Tikal.Tests.Editor.Level01Validation.Run` in batchmode). It reopens the serialized scene, checks hierarchy, pure anchors, missing scripts and critical player/door/UI references, then physically traverses the approach, opens the Door through input, visits all five zones, reaches the dais, returns through the entrance, closes the original Door and returns to spawn. Keyboard, XInput and DualShock prompts and inert secret input are checked in the new scene. It exits batchmode with code 0 only on success.

Run **Tikal → Tests → Dynamic prompts and all regressions** separately for Input, Player, Game Feel, Interaction and Dynamic Prompt regression coverage. Existing Testing scenes, runtime scripts, input asset, Packages and ProjectSettings are preserved.

Lighting is directional plus flat ambient and one basic chamber fill light. Materials are twelve plain URP colors: six initial blockout colors and six environmental proxy colors. No textures, production art, packages or runtime gameplay were added. Puzzle implementation and further integration are intentionally pending.

## Environmental Composition Pass

This second pass remains environmental blockout. No production vegetation, textures, puzzle logic or new runtime consumers are introduced. The scene keeps its five zone names and anchor identities; only anchor heights adapt to ground/support geometry. The entrance variant, hinge and Door behavior remain unchanged.

### Terrain and navigation

Eleven connected grid meshes replace the flat primitive floors: spawn, threshold, five zones and four connectors. Their MeshColliders share the rendered mesh. Approximately 1 m grid spacing avoids microrelief; connector elevation transitions use smooth interpolation. Surface heights range from -0.30 to 2.50 m, measured from the original doorway datum. The greatest triangle slope anywhere on the playable surfaces is 40.17 degrees, on peripheral mounds; the required connector ramps are approximately 7–11 degrees. No mandatory jump is introduced.

| Area | Typical elevation | Composition |
| --- | ---: | --- |
| Spawn / entrance | 0 m, with a -0.30 m shallow depression | Rock approach cut, gate shoulders and lateral vegetation |
| Zone01 | 0.40 m courtyard, 0.75 m peripheral mound | Small ceremonial clearing, nested 0.20 m steps and two provisional stelae |
| Zone02 | 1.20 m, local mound up to 2.00 m | Split local paths, vegetation between orientations, stone structures |
| Zone03 | 0.30 m | Roofed, partly buried stone chamber with a rock roof cover and entrance lintel |
| Zone04 | 1.60 m, local terrace up to 2.00 m | Blank ruin wall integrated into a rock slope; GlyphWallAnchor preserved |
| Zone05 | 0.60 m central clearing, peripheral mound up to 2.50 m | Stepped terminal monument, framing pillars and low approach dais |

The plaza remains approximately 18 × 20 m and its center stays open. Its off-route platform adds 0.60 m in three 0.20 m increments; PuzzleAnchor and its separate marker now sit on the upper platform. Stelae, the entrance gate, path stone structures, chamber lintel, blank gallery wall and final monument serve as provisional landmarks for the return trip.

The main route still follows the original zigzag. Rounded contours, off-axis volumes, elevation transitions and landmarks provide discovery without arrows, mandatory detours or a large maze. Optional plaza, mound and terrace excursions are validated but excluded from the direct-route pacing measurements.

### Natural shell and vegetation

Forty-two solid mesh taludes cover the artificial boundary reading. Their irregular contours and crests rise approximately 4.5–10 m relative to adjacent floors. Typical exterior widths are 7 m, connector shoulders 4 m; the two plaza taludes adjacent to the initial approach are limited to 4 m so they do not invade the recodo. Existing BoxColliders remain enabled behind the visible rock/vegetation shell; their rectangular boundary renderers are disabled. No collision security was removed without a matching surface replacement.

The composition uses 68 grouped low-poly vegetation volumes: 23 Low, 3 Medium and 42 Dense. They share one closed organic proxy mesh and three flat green materials. Vegetation has no colliders, preventing arbitrary bush collisions. The shell layers progress from low vegetation to denser crest masses and elevated rock/ground. This evaluates silhouettes and occlusion; no LOD, instancing, occlusion system or production forest asset is introduced.

The project adds 54 simple mesh assets (11 floors, 42 taludes and one shared organic volume) and six proxy materials. New environmental groups contain no runtime behaviors. Scene inspection and seven rendered viewpoints cover Spawn, Entrance and all five zones. Visible geometry blocks sampled sightlines between non-adjacent zones; the whole map cannot be read from a single tested zone. Composition remains a set of representative masses rather than individual plants.

### Traversal, backtracking and validation

The permanent Level01Validation editor test now additionally checks terrain MeshCollider references, triangle slopes, hidden safety colliders, proxy counts/collider policy, visible occluders, grounded stability, plaza steps and local uphill/downhill paths. It physically completes Spawn → Door → all five zones → Door → Spawn using Input System simulated devices and the existing CharacterController, without teleportation or jumping. Two successive airborne frames were the maximum observed; the largest vertical change in one 0.02 s step was 0.100 m, including platform traversal. No stuck waypoint or large height discontinuity remains in the tested routes. These are automated measurements, not a subjective physical-controller playtest.

After composition, Door → Zone05 center measures approximately 125.27 m / 32.56 s walking; Door → terminal anchor 132.27 m / 34.56 s. The terminal-to-original-Door sprint return remains 132.40 m / 22.72 s, within the 20–40 s target. Initial approach remains 19.55 m / 5.90 s to interaction. The same gate can be opened, crossed, reacquired and closed normally; dynamic E/X/square prompts remain valid and secret input stays inert.

The initial wider plaza shell blocked the approach during testing; its two offending shoulders were reduced before validation passed. This is why the perimeter is composed around the actual walking route, rather than filling every border uniformly. Temporary authoring/capture tools are removed after verification. Runtime input/player/interaction/door/prompt code, InputActionAsset, Testing scenes, Packages and ProjectSettings remain unchanged.

Validation completed with Unity 6000.6.3f1 batchmode exit code 0 for both the final environmental round trip and the full Input / Player / Game Feel / Interaction / Dynamic Prompts regression suite. No Missing Scripts, critical broken references or C# compilation errors were found. Non-blocking environment messages: Unity license-token renewal unavailable (entitlement resolved successfully) and Git LF/CRLF conversion notices.
