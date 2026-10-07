# Third-person reticle parallax

The old ReticlePresenter always drew at viewport center. PlayerInteraction already resolved
TPS in two stages: the MainCamera center ray selected a visual aim surface, then a ray from
the stable InteractionOrigin checked physical reach and occlusion. A shoulder-offset camera
and the player's chest are different origins. The final physical ray can hit a protrusion or
blocking wall at a point that projects away from the original center-ray surface. Center anchors
alone cannot represent that result; valid unoccluded surfaces can still project at center.

PlayerInteraction now exposes read-only HasResolvedAimPoint, ResolvedAimPoint, ViewCamera and
UsesThirdPersonAim. It publishes the existing visual hit, then replaces that point with the
existing first physical reach hit if the algorithm reaches that stage. This includes a wall
that rejects the visually selected interactable. No extra raycast, enlarged search, changed
InteractionOrigin or interaction eligibility rule was introduced. FPS queries remain unchanged.

If the existing chain rejects the target before the reach stage (out of range, no interactable,
or character facing), the visual hit remains available as normal aim. CurrentInteractable still
remains null whenever the original algorithm rejects interaction. No valid-target color or
extra feedback was added. If no useful hit exists or gameplay is locked, the resolved flag clears.

ReticlePresenter resolves its PlayerInteraction from the existing PlayerInputReader and uses
that source's camera. Serialized references are optional for compatibility with existing scenes;
canonical ThirdPersonCameraFoundationAuthoring now writes both explicitly. No scene rebuild is
required for existing gameplay scenes and no new preference/reticle mode was created.

FPS stays at anchoredPosition zero. TPS projects the resolved world point with WorldToScreenPoint,
then ScreenPointToLocalPointInRectangle into the reticle parent's plane. Subtracting the parent
rect center yields the correct centered-anchor offset even if its pivot differs. Overlay Canvas
uses no UI camera; ScreenSpaceCamera and WorldSpace use Canvas.worldCamera. This respects the
actual Canvas transform and CanvasScaler rather than treating pixels as anchored coordinates.

No hit, points behind the view camera, missing projection references, NaN/Infinity or failed
plane conversion safely fall back to center. Dot and Cross share the same container offset;
None stays hidden. Existing Journal/Pause visibility is preserved. FPS/TPS switches are consumed
before rendering, and FPS immediately clears the prior offset on the next presentation update.
Position runs at LateUpdate order 100, after camera (-50) and interaction (0), so the rendered
reticle uses the current frame's camera pose and resolved hit. Settings/gameplay refresh also
updates positioning. There is no smoothing, extra lag or arbitrary maximum-offset clamp.

Camera collision/orbit simply change the real camera projection; no CameraDistance approximation
is used. SecretCommand and the closed-Door secret solution remain independent of the reticle.
Camera profiles, movement, sensitivity, reach, Door, puzzles, Pause, Journal and Tutorial are
unchanged by this feature.

Run `Tikal/Validation/Validate Third Person Reticle Alignment` in Edit Mode for Tutorial/Level01
references, duplicate reticles, Missing Scripts and projection checks. The projection fixtures
compare Unity conversion to independent perspective/CanvasScaler math at 1920x1080, 1280x720,
1920x1200 and 1280x800, including screen round trips and invalid-point fallback.
Run the same menu in Play Mode for isolated physics fixtures using the real Explorer/reticle:
FPS center, TPS no-hit center, Dot/Cross/None, real two-stage hit, shoulder-offset occlusion,
out-of-reach aim, shortened camera poses, FPS reset and UI lock. Temporary settings are not saved.

Source compilation and an independent numerical four-resolution check are performed without
Unity batchmode. Native Editor/Play Mode validation and physical playtest must still be run in
Unity; they are not inferred from compilation. Manually test door/stele corners, lateral motion,
camera collision, orbit, and switching TPS to FPS through Pause Settings with Dot/Cross/None.
