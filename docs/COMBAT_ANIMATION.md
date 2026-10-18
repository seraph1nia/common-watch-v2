# Procedural tabletop combat

Both clients animate the existing single-image Swordsman and enemy Raider cutouts. Idle, walking lift/lean, anticipation, lunge, recovery, hit reaction and falling/fading death use the same image. Unillustrated roles use geometric figures with the same poses. Physical shots, magic, damage fragments, status particles and structure effects are drawn procedurally.

This demonstrates readable tabletop combat. It does not establish that fixed limbs and a fixed weapon pose meet every final-art requirement. The first-wave benchmark keeps the actual four-versus-four roster; “two sprites” means two character illustrations, not a fabricated duel.

## Run and inspect

```sh
mise run battle-godot
mise run battle-monogame
mise run battle-godot-assets
mise run battle-monogame-assets
mise run test-godot-animation
mise run test-monogame-animation
mise run capture-godot-battle
mise run capture-monogame-battle
mise run profile-godot-animation
mise run profile-monogame-animation
mise run review-battle
```

Showcases start playing with debug shapes by default. The assets tasks enable the current catalog. Controls are native Godot Controls or Gum Forms: scenario, play/pause, restart, speed, art, overlay and projection. Keyboard shortcuts: **1–6** select a scene, **Space** pauses/resumes, **Backspace** restarts, **Tab** cycles the three supported speeds, **T** changes art, **O** toggles diagnostics, **V** changes projection. Existing pan/zoom/reset controls remain available. Interactive tasks retain normal audio output.

Raw options include `--battle swordsman|ranged|status|burn|chill|structures`, `--battle-tick <tick>` (seek and pause), `--battle-zoom <factor>`, `--battle-pan-x <pixels>`, `--battle-pan-y <pixels>`, and `--combat-shader off`. Existing `--world-art`, `--render-mode`, `--view`, `--assets-root` and `--debug-overlay` options remain supported. Replacement PNGs use catalog anchors and dimensions; no animation code depends on painted limbs or silhouette.

Actual frames and JSON receipts are written to `artifacts/captures/<client>/battle/{debug,sprites}/`. They include all eight named swordsman phases, ranged projectile, magic impact, poison/burn/chill, watchpost/Arrow Tower shots, structure damage/destruction and battle completion. `sequence/frame-*.png` contains 32 ordered application frames, six simulation ticks apart, from idle through movement, the exchange and a completed death. At the default 1x pace these are 200 ms apart. No frames are interpolated or manufactured.

Additional actual frames check missing-sprite fallback and a panned, zoomed, angled view with diagnostics and the shader disabled. `review-battle` builds `artifacts/battle-review/index.html`, which plays the captured sequence and exposes receipts. Its styling follows the repository's dark surfaces, pale text and gold accents. It is a local review page, not a second game implementation.

Captures/checks use an owned Linux Xvfb display, Mesa software rendering, silent test audio and temporary settings. No OS dependencies are installed by these commands. `--no-sequence` skips sequence capture for the focused animation test commands. Full capture commands regenerate it afterward.

## Authoritative scenes

`BattleShowcase` records detached snapshots and events from legal commands against the unchanged default Core. It then uses the original `CombatPlayback` for presentation. Optional unaffordable purchases can be rejected by authority; the replay never grants their effects. All battle results are computed by Core.

- **Swordsman:** the existing paid opening, seed `20261018`, wave one. Four friendly swordsmen face four enemy swordsmen, using two character illustrations.
- **Ranged/magic:** legal campaign progression to wave four includes real enemy crossbows and a Mage. Missing role artwork deliberately uses shapes.
- **Poison:** normal Ranged Foundation and Venom purchases produce an authoritative poison example in wave seven.
- **Burn:** a separate Arcanist opening earns Magic Foundation and Fire; the captured battle is wave six.
- **Chill:** a separate Arcanist opening earns Magic Foundation and Frost; the captured battle is wave seven.
- **Structures:** a paid Arrow Tower, with no recruited army, produces real watchpost/keep damage and destruction and a real defeat. This is an isolated presentation demonstration of a legal losing strategy, not a campaign victory fixture.

The original event model was inspected in the local upstream checkout and pinned import. In this revision, watchpost/tower attacks use `AttackStarted`/`Impact` carrying `TowerState`; the legacy `DefenderShot` enum is also recognized. Targets distinguish a watchpost from a keep even though both have `TargetCity=true`. Frozen aims preserve their authoritative transit interpolation.

## Timing and lifecycle

Each engine independently samples a small pose from the detached unit and playback tick. Windup first draws back, then lunges into `ImpactTick`; the initial part of recovery settles back before `ReadyTick`. Ranged streaks travel during the final part of windup and arrive at the real impact. They introduce no extra damage/travel delay. Movement uses the existing committed source/destination interval and formation offset, without pathfinding or occupancy writes.

Only drained, positive-damage `Hit` events start flashes, recoil and numbers. Impact arcs can also accompany misses; those never manufacture damage feedback. Every event keeps its sequence, including simultaneous hits. Upstream playback undoes future health damage until the corresponding boundary; repeated/out-of-order snapshots cannot duplicate effects.

A playback object change or generation change clears the engine's visual history. This covers replacement, reconnect, event-history gaps and restarts. Death overrides attack recovery, uses Core's death interval and does not retain targetability. Ordinary live play keeps the battlefield visible until the last presented bodies finish before opening the reward overlay; a direct draft baseline still opens its UI immediately.

Idle and all cosmetics pause with presentation. After the final simulation tick, terminal cosmetics use elapsed **CombatPlayback.VisualSeconds**, which already respects pause and connection state. This lets the last structure burst/fade expire without advancing gameplay or using independent wall-clock effect timers.

Status particles are sampled only between authoritative application and expiry ticks. Events are bounded by Core's 4096-entry history limit; particles are sampled vectors rather than persistent objects. No per-frame textures or per-particle nodes are created.

## Godot implementation and observations

`CombatMotion.cs` owns the engine's pose curves. `CombatFigure.cs` retains one Node2D per visible actor, transforms it around the ground anchor, and draws its PNG or a geometric figure. A Main-owned CanvasItem shader and per-figure ShaderMaterials blend actual damaged figures toward white. Shader materials, nodes and the shared shader have explicit lifecycle cleanup. Tint modulation supplies the optional fallback.

Canvas drawing supplies shots, charge rings, impact arcs, particles and diagnostics. One retained foreground CanvasItem keeps health bars and event text above tall figures. Figures sort by projected ground depth. Angled view compresses the ground only; images keep their aspect ratio. Diagnostic bounds include the real image rectangle transformed by the sampled rotation/scale.

Actual phase and sequence inspection found readable anticipation, impact and recovery at normal zoom. The initial flash was too white, and terrain competed with the figures; both were restrained. Close inspection exposes fixed knees, sword and shield. Multiple attackers still obscure portions of each other during the crowded opening. This remains an animated tabletop-figure style, rather than articulated character motion.

Godot-specific work included retained CanvasItem ordering, explicit shader resource lifetime and delaying the reward overlay through the final body lifecycle. Event-driven combat does not rely on Tweens.

## MonoGame implementation and observations

`CombatMotion.cs` independently owns the pose curves. SpriteBatch uses the PNG's ground anchor as its origin. A small local transform in ShapeCanvas gives geometric figures the same pivot, rotation and scale. Textures are loaded/premultiplied once and retained across presentation toggles; all GPU resources are disposed with the client.

The representative GPU effect uses MonoGame's native **BasicEffect** fog shader to blend hit sprites/geometry toward white. Its orthographic projection follows the viewport. Ordinary tinting is the optional fallback. No content compiler or cross-engine shader language was added. Programmatic strokes/rings distinguish physical and magical attacks.

Actual frames show clear hit and health feedback at normal zoom. Walking lift and lean soften translation, while the fixed leg pose still looks like a tabletop piece being moved. Close-up frames expose the static weapon angle. Group fights remain legible through faction bases and health bars, but IDs and simultaneous damage numbers can crowd together.

MonoGame-specific work included ground-origin SpriteBatch transforms, premultiplied alpha, switching a native effect around damaged figures, and reusing scanline scratch storage to reduce allocations. No per-frame texture allocation or particle framework was introduced.

## Artwork and verification limits

The unchanged catalog supplies `assets/sprites/units/swordsman.png` and `enemy_swordsman.png`, both processed 256×256 RGBA images from the supplied sheets. The enemy is a green raider cosmetic stand-in for Core's enemy swordsman. Existing Pillow validation checks alpha, dimensions, hashes, aspect-preserving fit and ground anchors. No artwork, extra poses, rigs, sprite sheets or projectile PNGs were created.

Painted contact shadows/ground islands, a fixed three-quarter view and fixed limb/weapon positions constrain the result. Ranged and Mage artwork is unavailable; their effects and animations are verified with geometric fallbacks. This is not evidence for the visual quality of hypothetical replacement illustrations.

The benchmark is a real group encounter, not a two-actor isolation fixture. Native Windows, physical GPU/audio qualification and live Steam accounts are outside this Linux verification. The pinned Core, package versions, NuGet locks, networking protocols and Steam implementation remain unchanged.

## Verification record

The implementation records native repeated-battle measurements at `artifacts/profiles/{godot,monogame}-animation.json`: requested and measured FPS, client submission time, managed allocation per frame, peak active event count and final cleanup counts. These are basic presentation measurements under Xvfb/Mesa, not comparable GPU benchmarks. Capture receipts separately record frame observation time, scenario, engine, authoritative/presentation ticks, event cursor and unit/status state.

The full suite includes Core/shared tests, independent engine animation contracts, actual native UI/art/fallback checks, real two/three/four-player battles, and disconnect/reconnect during a real paused death. The multiplayer probes require every host and guest to consume actual hit events. Reconnect verification requires a new visual baseline, remaining dying bodies and no replayed historical hit flashes. No transport/session protocol changes were needed.

The final serial verification passed `mise run test`: 151 .NET tests (40 Core, 44 shared, 25 Godot, 34 MonoGame and eight graphical/network integration tests) and 14 Pillow tests. Its build prerequisite passed with no warnings or errors. Separate `test-godot-animation`, `test-monogame-animation`, `capture-godot-battle` and `capture-monogame-battle` runs also passed. Both captures contain 19 named frames and a 32-frame sequence per art mode, plus missing-art and angled/panned/zoomed shader-fallback frames. Actual frames were inspected for both clients. Command outcomes and logs are retained in `artifacts/animation-verification/`.

Both `profile-*-animation` commands passed nine repeated battles. Every run ended with zero active event effects and three surviving actors, rather than retained dying figures. Godot measured mean client submission times of 1.28–1.45 ms, p95 times of 1.52–2.15 ms and 60,914–68,921 managed bytes per frame. Measured rendering tracked 30/60/120 FPS except one requested-120 run at 110.4 FPS. Live managed heap observations fluctuated between 19.1 and 33.0 MB.

MonoGame measured mean client submission times of 3.02–4.08 ms, p95 times of 3.43–9.51 ms and 400,133–477,577 managed bytes per frame; measured rendering tracked each requested rate. Live managed heap observations fluctuated between 78.9 and 92.5 MB. These whole-client probes include existing UI, playback and renderer allocations, not just animation. They demonstrate bounded effect cleanup; they do not establish allocation-free drawing, isolate leaks or qualify hardware performance. Reducing the remaining managed allocations is future optimization work.

One intermediate Godot host crashed before combat with a native .NET tiered-JIT stack; a focused death/reconnect rerun and the final full suite passed. That intermittent runtime failure is retained as a stability limitation, without changing the pinned runtime or game rules.

The local HTML review was generated and opened in Lavish. Automated browser inspection of that review page was unexecuted because the Chrome CDP target exited on both attempts. Native game captures were inspected directly; this browser-tool failure does not replace or invalidate the executed application checks.
