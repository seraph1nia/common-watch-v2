# Procedural tabletop combat

The application animates the existing single-image Swordsman and enemy Raider cutouts. Idle, walking lift/lean, anticipation, lunge, recovery, hit reaction and falling/fading death use the same image. Unillustrated roles use geometric figures with the same poses. Physical shots, magic, damage fragments, status particles and structure effects are drawn procedurally.

This demonstrates readable tabletop combat. It does not establish that fixed limbs and a fixed weapon pose meet every final-art requirement. The first-wave benchmark keeps the actual four-versus-four roster; “two sprites” means two character illustrations, not a fabricated duel.

## Run and inspect

```sh
mise run battle
mise run battle-assets
mise run test-animation
mise run capture-battle
mise run profile-animation
mise run review-battle
```

Showcases start playing with debug shapes by default. The assets tasks enable the current catalog. Controls are native Godot Controls: scenario, play/pause, restart, speed, art, overlay and projection. Keyboard shortcuts: **1–6** select a scene, **Space** pauses/resumes, **Backspace** restarts, **Tab** cycles the three supported speeds, **T** changes art, **O** toggles diagnostics, **V** changes projection. The complete arena frames automatically as resolution/projection changes. Interactive tasks retain normal audio output.

Raw options include `--battle swordsman|ranged|status|burn|chill|structures`, `--battle-tick <tick>` (seek and pause), `--battle-zoom <factor>`, `--battle-pan-x <pixels>`, `--battle-pan-y <pixels>`, and `--combat-shader off`. Existing `--world-art`, `--render-mode`, `--view`, `--assets-root` and `--debug-overlay` options remain supported. Replacement PNGs use catalog anchors and dimensions; no animation code depends on painted limbs or silhouette.

Actual frames and JSON receipts are written to `artifacts/captures/godot/battle/{debug,sprites}/`. They include all eight named swordsman phases, ranged projectile, magic impact, poison/burn/chill, watchpost/Arrow Tower shots, structure damage/destruction and battle completion. `sequence/frame-*.png` contains 32 ordered application frames, six simulation ticks apart, from idle through movement, the exchange and a completed death. At the default 1x pace these are 200 ms apart. No frames are interpolated or manufactured.

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

The presentation samples a small pose from the detached unit and playback tick. Windup first draws back, then lunges into `ImpactTick`; the initial part of recovery settles back before `ReadyTick`. Ranged streaks travel during the final part of windup and arrive at the real impact. They introduce no extra damage/travel delay. Movement uses the existing committed source/destination interval and formation offset, without pathfinding or occupancy writes.

Only drained, positive-damage `Hit` events start flashes, recoil and numbers. Impact arcs can also accompany misses; those never manufacture damage feedback. Every event keeps its sequence, including simultaneous hits. Upstream playback undoes future health damage until the corresponding boundary; repeated/out-of-order snapshots cannot duplicate effects.

A playback object change or generation change clears the presentation's visual history. This covers replacement, reconnect, event-history gaps and restarts. Death overrides attack recovery, uses Core's death interval and does not retain targetability. Ordinary live play keeps the battlefield visible until the last presented bodies finish before opening the reward overlay; a direct draft baseline still opens its UI immediately.

Combat idle poses and event cosmetics pause with presentation. After the final simulation tick, terminal combat cosmetics use elapsed **CombatPlayback.VisualSeconds**, which already respects pause and connection state. This lets the last structure burst/fade expire without advancing gameplay or using independent wall-clock effect timers. Ambient scenery uses the separate [dynamic environment clock](DYNAMIC_ENVIRONMENT.md#timing-and-lifecycle).

Status particles are sampled only between authoritative application and expiry ticks. Events are bounded by Core's 4096-entry history limit; particles are sampled vectors rather than persistent objects. No per-frame textures or per-particle nodes are created.

## Godot implementation and observations

`Combat/CombatMotion.cs` owns pose curves. `Combat/CombatFigure.cs` retains one Node2D with a native Sprite2D per visible actor, transforms it around the ground anchor, and uses custom drawing for geometric fallback. `Combat/BattlefieldView.cs` owns actor/effect lifetimes and the reusable `Combat/HitFlash.gdshader` resource; per-figure ShaderMaterials blend damaged figures toward white. Shader materials, nodes and the shared shader have explicit lifecycle cleanup. Tint modulation supplies the optional fallback.

Canvas drawing supplies shots, charge rings, impact arcs, particles and diagnostics. One retained foreground CanvasItem keeps health bars and event text above tall figures. Figures sort by projected logical ground depth. Native Camera2D transforms the world; ground projection remains independent. Angled view compresses the ground only; images keep their aspect ratio. Diagnostic bounds include the real image rectangle transformed by the sampled rotation/scale.

Actual phase and sequence inspection found readable anticipation, impact and recovery at normal zoom. The initial flash was too white, and terrain competed with the figures; both were restrained. Close inspection exposes fixed knees, sword and shield. Multiple attackers still obscure portions of each other during the crowded opening. This remains an animated tabletop-figure style, rather than articulated character motion.

Native work includes Sprite2D figures, Camera2D, CanvasItem ordering, explicit shader/cache lifetimes and delaying the reward overlay through the final body lifecycle. Sampled status/impact particles remain batched drawing because they are bounded, pause correctly and need no independent timers. AnimationPlayer/Tweens add no useful timing simplification here. Event-driven combat does not rely on Tweens.

## Artwork and verification limits

The unchanged catalog supplies `assets/sprites/units/swordsman.png` and `enemy_swordsman.png`, both processed 256×256 RGBA images from the supplied sheets. The enemy is a green raider cosmetic stand-in for Core's enemy swordsman. Existing Pillow validation checks alpha, dimensions, hashes, aspect-preserving fit and ground anchors. No artwork, extra poses, rigs, sprite sheets or projectile PNGs were created.

Painted contact shadows/ground islands, a fixed three-quarter view and fixed limb/weapon positions constrain the result. Ranged and Mage artwork is unavailable; their effects and animations are verified with geometric fallbacks. This is not evidence for the visual quality of hypothetical replacement illustrations.

The benchmark is a real group encounter, not a two-actor isolation fixture. Native Windows, physical GPU/audio qualification and live Steam accounts are outside this Linux verification. The pinned Core, package versions, NuGet locks, networking protocols and Steam implementation remain unchanged.

## Verification and profiling

`mise run profile-animation` writes `artifacts/profiles/godot-animation.json`: requested and measured FPS, submission time, managed allocations, peak effects, heap observations and final cleanup counts for nine repeated battles at 30/60/120 FPS and all three speeds. These are Linux/Mesa whole-client observations, not physical GPU benchmarks or proof of allocation-free rendering.

`mise run test-animation` runs pose/lifecycle contracts and actual frame capture. `mise run capture-battle` includes 19 named frames and 32 ordered frames per art mode, plus missing-art and angled/panned/zoomed shader-fallback checks. Multiplayer verification requires each host/guest to consume real hits; reconnect during a paused death requires a new visual baseline and no historical hit flashes.

See [verification](VERIFICATION.md) for current executed results. An intermittent native .NET tiered-JIT crash occurred in a previous multiplayer run. Passing retries do not resolve that known stability risk. Keep logs if it recurs; do not weaken the checks or change gameplay to hide it.
