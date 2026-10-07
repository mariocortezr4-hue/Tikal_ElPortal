# Level01 Secret Entrance camera independence

SecretCommand routes from Player/SecretCommand through PlayerInputReader.SecretCommandPressed
directly to SecretEntranceCommandReceiver and LevelOutcomeController.TriggerVictory.
PlayerController and PlayerInteraction do not choose its receiver. Interact retains its separate
PlayerInteraction camera aim and Door.Interact path.

The former receiver computed distance and facing from MainCamera to SecretCommandTarget, then
raycast from that camera. Its maximum camera distance was 3m while the TPS profile's default
boom is 3.4m. Camera collision, orbit and shoulder position therefore changed eligibility at a
fixed player position. The ray explicitly ignored the moving door leaf, and no Door.State check
existed. Open-door victory was therefore permitted. FPS worked when its close camera met the
filters. The user's physical failing frame is unavailable, so which distance/dot/occlusion filter
rejected that particular TPS attempt cannot be identified from source alone.

The receiver now requires active gameplay, a Playing outcome, Door.State exactly Closed,
the player's position + 1m up inside the existing oriented BoxCollider zone, and at most 3m
from that player point to the stable entrance target. No camera distance/facing/ray or
CurrentInteractable condition remains. Open, Opening and Closing all reject; SecretCommand
never toggles the door. No clue/puzzle discovery is required.

The existing zone is 3.4m wide, 3m high and 5.8m deep, centered around the entrance. The 3m
player reach also clips its corners. It supports approaching and returning to the door but
does not allow using the command elsewhere in Level01. No larger trigger was introduced.

Existing serialized scenes resolve their logical Door through the old moving-leaf reference
until authoring stores the explicit _door reference. That compatibility reference is only used
to identify the Door component; it no longer participates in physics or visibility decisions.
Camera and minimum-dot serialized fields are removed from the runtime component.

Run `Tikal/Authoring/Apply Level01 Secret Entrance` outside Play Mode to store canonical references.
It preserves the existing target/zone, uses one receiver and does not rebuild the scene/puzzles
or touch Pause. FinalPuzzlesRedesignAuthoring also invokes this integration for regeneration.
The user's existing Level01/Tutorial scene edits and generated Pause prefab are preserved and
excluded from this code commit. No manual scene YAML patch or Unity batchmode is used.

Run `Tikal/Validation/Validate Level01 Secret Entrance` in Edit Mode for references, bounded
stable geometry, canonical bindings, duplicate receivers and Missing Scripts. Run it in a fresh
Level01 Play session with no clues, closed door and active gameplay for the runtime matrix.
The runtime test uses actual camera modes and semantic input with virtual Keyboard/Xbox/
PlayStation/Generic devices. It checks closed eligibility even with the camera distant and
facing away, native open/outside rejection, opening/closing rejection, gameplay lock, normal
Door.Interact toggles, zero clues, and finishes by triggering real TPS speedrun victory through
generic buttonNorth. It restores transient settings/devices/poses without saving settings or
progress; the scene remains in the resulting Victory state.

Manual playtest: for FPS and TPS, G/Y/triangle/north must win near the CLOSED entrance without
discovering clues. E/X/square opens normally; secret while OPEN does nothing. Close normally
and use secret again to win. Test orbit facing away, near zone edges and outside the zone.
The native Editor/runtime menu tests and physical controller playtests require Unity execution;
source compilation alone does not establish those results.
