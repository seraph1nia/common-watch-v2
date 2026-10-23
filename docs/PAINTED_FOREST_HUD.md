# Painted forest HUD — 21 October 2026

The production Godot game now uses painted bark frames, purple/cyan mushrooms,
woodland characters, honey construction plaques and emerald/violet match controls.
The resource ribbon has been replaced by five floating regions around the existing
settlement. All text and interaction remain native Godot Controls.

The starting HEAD was `45741e3a6d3a29af4b3b6daf64b4b5293ca7bcd3`. Eight pre-existing
working-tree changes in README, Forest HUD/verification/playing documentation,
GameApplication, SessionScreens, SessionExitChecks and SessionExitDialog were
preserved. Fresh baseline build, Core (46), Shared (88), art (19) and three
actual-process session tests passed. Baseline screenshots were freshly rendered
at 1280×720 and 1920×1080 before implementation, using legal production scenarios
and environment effects Off.

## Artwork and composition

The [source delivery](../art/source/painted_forest_hud/README.md) contains the actual
RGBA paintings, original reference, authoring instructions, rejected first plaque,
contact sheet, proof, [manifest](../art/source/painted_forest_hud/manifest.json),
anchors, slices, registration and checksums. There are 44 PNG runtime exports and
four technical SVGs. No layered painting sources were produced by the authoring
tool. The first bottom-right proof was inspected in Godot, revised for title/perch
alignment, and used as the material/character quality gate. The construction
plaque was revised after actual game captures exposed insufficient text space,
then its portrait was separated from the stretchable face after the wide-size
review exposed socket distortion. Portrait frames and thumbnails now keep fixed
dimensions and aspect ratios.

- Upper-left: one wooden Menu plaque (Settings is in the menu), with an independently perched owl.
- Upper-right: rabbit, crescent, lantern, mushrooms and a native four-column ledger
  for six resources during planning. Stock and signed production stay together;
  funding exceptions live in the threat summary, with exact manifests in City details.
  The ledger and its ornaments leave during combat, rewards and terminal observation.
- Lower-left: selected context with the existing building thumbnail, clear-selection
  control, stacked actions and bounded native scrolling. It disappears on clear.
- Bottom-center: native navigation and a separately retained construction palette.
  Warm plaques show name, existing sprite, purpose or shortage, and the actual cost.
  Categories, selection, hover and focus remain distinct. Build confirmation spends
  only after activation; Gold recovery retains its selected mode and exact quote.
- Lower-right: clock, squirrel, mushrooms, wave, the three actual preparations and
  Combat. Compact layouts use a phase row; wider layouts use a vertical list. Ready
  and Pause remain side by side during preparation; combat hides the completed ladder. Allied inspection labels Ready with the local
  player's identity. Food shortage warns without blocking readiness.

`SettlementHud`, `ResourcePanel`, `MatchControls`, `ConstructionPanel/Card` and
`BuildingInspector` own the native composition. `PaintedHudArt` provides cached
textures, runtime StyleBoxTextures and aspect-preserving, input-free decoration.
Recruitment's selector clips within the narrow inspector and retains a full native
tooltip. The existing Army, Worldtree, reward and session flows are retained.

Only fixed settlement framing changed to reserve the right edge. The nine canonical
clearings, painting, anchors, occupancy and hit geometry are unchanged. The existing
map renderer fills exposed outer edges with its existing approved-map cover.
Arena framing/pan/zoom, simulation, protocol, private resume files, Core and Shared
were not modified. Explicit debug/plain and missing-art fallbacks remain available.

## Actual screenshots and checks

The [interactive review](../artifacts/painted-forest-hud/index.html) compares fresh
before/after game frames at both resolutions, with native-pixel inspection. The
proof and contact sheet are separate, labeled views. Game screenshots are actual
Godot renders earned through legal checkpoints, not composited mockups.

Verification receipts and the complete screenshot inventory are under
`artifacts/painted-forest-hud/`. The final results are recorded in
[verification](VERIFICATION.md#painted-forest-hud-2026-10-21).

New native checks click Ready through preparations 1, 2 and 3, click Pause, resume
with controller accept, check panel gaps and target bounds, and retain existing
card/confirmation, scroll/focus, menu and nine-clearing picking assertions. Tests
were not weakened to fit the layout; camera reset now checks the new framing scale
and origin.

## Limitations and separate failures

No world painting, building, unit, Worldtree, arena or environmental asset was
authored or replaced. The existing older building-style mismatches remain. Native
Windows/macOS execution, live Steam, physical GPU/controller/audio hardware and
release exports are not qualified by Linux/Xvfb/Mesa/Dummy-audio checks. The known
intermittent native .NET/JIT crash risk remains.

The existing [CI run at the starting commit](https://github.com/seraph1nia/common-watch-v2/actions/runs/39656535912)
failed `Escape cancels confirmation and restores invoking focus` and logged missing
`rg` on its runner. No new CI run was triggered and no CI fix is claimed. One local
intermediate capture also failed an Esc/details assertion during resize; the failed
receipt is retained separately from subsequent successful checks. Earlier layout
failures caught collapsed scrolling, clipped confirmation and inspector overlap;
those led to implementation fixes.

The local review's JavaScript and served image resources were checked. Browser
automation could not attach because the Chrome bridge reported its CDP target
gone; no browser-rendering check of the review page is claimed. This does not
replace the actual Godot viewport inspections above.
