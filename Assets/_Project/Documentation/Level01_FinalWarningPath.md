# Level01 — Final Warning Path

The final path opens only after Puzzle05 is solved. Before Clue05 is obtained, the path stays inactive and the no-return trigger cannot produce defeat.

## Warning Anchor

`FinalWarningSteleAnchor` holds `FinalWarningStele_Proxy`, a replaceable visual blockout. The final warning stele has no `ReadableInscription`, no prompt, no clue reward, and no Journal entry. Its future message is intended to be integrated visually into the final asset:

> Si continúas, termina tu recorrido.

No fake Maya writing is authored in this blockout.

## Decision Distance

The warning anchor is roughly 8 metres before the centre of `PointOfNoReturnTrigger`, which leaves time to see it and return. Before crossing the trigger, the player can backtrack through:

`Zone05 -> Zone04 -> Zone03 -> Zone02 -> Zone01 -> Entrance`

## Defeat

`PointOfNoReturnTrigger` requires Puzzle05 to be solved. Once crossed, it closes the passage behind the player and calls `LevelOutcomeController.TriggerDefeat()` exactly once.

Defeat displays:

`HAS QUEDADO ATRAPADO`

The sequence is non-violent. `REINTENTAR` reloads `Level01_SecretEntrance` and restores the level state.

## Validation

`FinalPuzzlesRedesignValidation` covers early Zone05 access, hidden key content, Puzzle04 gating, device-aware Clue05 presentation, backtracking before the trigger, defeat after Puzzle05, and Retry cleanup.
