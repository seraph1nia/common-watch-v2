# Dynamic environment presentation

Common Watch can animate its existing illustrated scenery without changing simulation,
networking, camera controls or input. Environment effects default to **Off**, preserving
existing screenshots, including the arena's original static rain. Enable them in
**Settings → Environment effects → Subtle / Full**, or at launch:

```sh
mise run play-assets -- --environment-effects full
mise run battle-assets -- --environment-effects full --environment-preset rain
mise run play-assets -- --environment-effects subtle --environment-preset night
```

The quality preference is saved separately in `user://environment-settings.json`.
A CLI quality overrides it for that launch. Tests using `--settings-path FILE` use
`FILE.environment.json`, keeping ordinary player preferences untouched. The audio
JSON format and reconnect files are unchanged. Preset, seed and capture time are
launch/development options, not saved match state.

## Ownership and composition

`Scenes/Main.tscn` declares the `EnvironmentController`. The application initializes
it with the existing application-owned `AssetLoader`. Each world owner retains an
`EnvironmentView`; the menu attaches one behind its background's native Controls.
The application only coordinates suspension and ownership. No environment class
reads Core rules, submits commands, changes snapshots or consumes combat events.
`EnvironmentView.SetArena` accepts the existing detached presentation layout.
Settlement supplies its actual building foundation anchors.

| Owner | Responsibility |
| --- | --- |
| `EnvironmentController` | One local visual clock, quality, shader/preset cache, registration and resource lifetime |
| `EnvironmentSettings` | Strict CLI parsing and separate quality preference persistence |
| `EnvironmentPreset` and `Presets/*.tres` | Data-driven composition, color and wind; smooth parameter blending |
| `EnvironmentView` | Retained scene layers, visible bounds, quality budget and local emitters |
| `EnvironmentLayer` | One input-free shader CanvasItem, continuous world sampling, clipped geometry |
| `AmbientParticles` | Bounded batched pollen/fireflies/magic, snow, rain fallback and impact rings |
| `EnvironmentEmitter` | Anchor-relative additive fire/crystal glow or rising smoke |

World effects use ordinary Node2D depth ordering and the existing Camera2D. The
arena's scenery is at -20; distant effects at -5 and local backdrop glow at -4;
actors keep their existing exact ground-depth order. Near mist, rain and particles
are at 2500–2502, below the combat labels/bars at 4095. In settlement, overlays sit
above the painted map at 1–2 and near weather at 2500. Native HUD/modal/menu canvases
stay separate and sharp. These values are internal ordering, not new gameplay depth.
Effects are not Controls and never consume mouse input.

Each layer exposes `Enabled`, `Strength`, `Tint` and `SpeedScale`. The composition
also exposes `ParticlesEnabled`, `EmittersEnabled` and `LightingEnabled`. Presets
control baseline opacity and velocity; `Strength` and `SpeedScale` multiply those
values. `AddLayer` registers another retained shader/texture layer. Layer materials
are unique instances; shader resources and decoded PNGs are shared.

Only visible world rectangles are submitted. Ground mist is a lower-world band, and arena embers stay near the burning backdrop; low-value fullscreen overdraw is avoided. Shader coordinates come from the
world vertices, not a tile's UV origin, so sector boundaries and viewport clipping
do not restart texture motion. The near rain uses a larger repeat size and faster
fall than distant rain. Subtle omits near rain entirely. Enabled modes suppress
legacy static rain, avoiding double rain; Off restores it.

## Presets and active effects

| Preset | Composition |
| --- | --- |
| `clear` | Gentle cloud shadows, restrained distant haze, pollen and low glow |
| `mist` | Cooler tint, more drifting mist/haze, quiet particles |
| `rain` | Two rain depths in Full, clouds, cool darkening, light impact rings in arena |
| `night` | Readable cool darkness, fireflies, fog and stronger local glow |
| `enchanted` | Cyan motes, magical mist and pulsing crystal overlays |
| `ember` | Arena default: animated rain, supplied drifting smoke/embers, haze and fire/crystal glow |
| `snow` | Optional varied falling flakes, sideways drift, cool haze; no accumulation |

Settlement defaults to Clear, and the menu to Enchanted at 30% intensity. Preset
mixing is explicit: `controller.SetPreset(AtmospherePreset.Mist)` approaches the
new colors, densities and wind exponentially over a few seconds. `SetPreset(null)`
restores each scene's own default. `immediate: true` is for deterministic capture.
Edit or add a `.tres` alongside the others to author a preset, then register its enum
name in `AtmospherePreset`; the controller loads these resources once.

Arena glow positions reference the supplied backdrop's fire and rune details in
normalized tile coordinates, repeated at `ArenaLayout.Pitch`, outside active rows.
The six-column topology, caps, playable board and actor anchors are unchanged.
Production smoke attaches to Lumbermill/MetalMine roof offsets; Arcanum and
ResearchTower receive localized crystal glow. Buildings themselves never bob,
change brightness globally or inherit a wind deformation.

The existing far arena backdrop is already blurred offline, without actors or UI.
It remains camera independent. Additional parallax is deliberately not applied to
the sharp repeated backdrop/foreground, whose painted ground contact must remain
aligned with the board. Moving procedural shadows provide depth without sliding
those contacts. The settlement's distant region is explicitly the upper 24% of
the map; this is restrained haze, not inferred painting depth or whole-map blur.

## Timing and lifecycle

Ambient time advances only when an enabled composition is visible, the window has
focus and the application has not suspended presentation. It freezes with match
pause, showcase pause, frozen scenario checkpoints and disconnection with retained
state. Focus loss freezes wind/preset transitions too. Hiding a scene stops redraws
and parameter updates. A single clock continues across scene transitions; retained
hidden scenes neither spawn particles nor accumulate simulation effects.

Combat attacks, deaths, damage flashes, statuses and character idle poses remain
on the existing Core playback clock. Ambient time never advances that clock.
No extra character idle transform is layered over combat poses.

`--environment-seed 417` selects deterministic particle distribution.
`--environment-time 8` fixes the visual sample in seconds (0–86400), including
while paused or unfocused. Captures can call `controller.Sample(seconds)`; calling
`Sample(null)` resumes local time. Shaders use `visual_time`, never Godot `TIME`.
Layers/emitters release their private materials on tree exit, compositions unregister
from the controller, and the controller releases cached shader/preset resources.
External PNG lifetime remains exclusively with `AssetLoader`. A shared 256×128 seamless Godot NoiseTexture2D supplies fog/shadow noise; it is generated once and disposed with the controller.

## Quality and performance

Off hides compositions immediately and performs no ambient redraw/animation.
Subtle scales opacity/particle budgets to 45%, omits near rain and restrains tint.
Full enables both rain depths and the full authored budget. Menu intensity is
further reduced. The Ember preset omits cloud shadows over its burning sky; Clear/Rain retain them. Zero-density layers are hidden instead of submitting transparent
fullscreen passes. There are no SubViewports, screen-reading shaders, generated
textures per frame, per-particle nodes or new dependencies.

Particle drawing examines at most 21 × 16 visible grid cells and coarsens the grid
at overview zoom. Glows retain at most two anchors per arena sector; offscreen
emitters are culled. Settlement has at most nine building effect anchors. Materials
are retained, optional textures decoded once and masks are optional. The existing
client still performs its own per-frame snapshot/layout/drawing allocations; this
system does not claim allocation-free whole-client rendering.

`capture-environment` writes `performance.json` with Off/Full median and p95 frame
intervals and process-wide managed bytes per frame, for local/overview framing,
1/6 players, both projections and 720p/1080p. It checks zero steady-state texture
decodes. These are 30-frame Linux/Xvfb/Mesa llvmpipe observations, including the
whole client and frame scheduling, not isolated GPU timings or hardware certification.
Do not compare measurements taken alongside another graphical workload.

## Shader parameters and asset-dependent support

All shaders are Compatibility `canvas_item` resources in `Environment/Shaders`.

| Shader | Key parameters and intended use |
| --- | --- |
| `ScrollingTexture` | `visual_time`, `velocity`, `texture_scale`, `opacity`, `tint`, `repeat_texture`, `textured`, `band_fade`; rain/smoke/embers with procedural fallback |
| `AtmosphericHaze` | Common world parameters plus `height_fade`, `cloud_shadow`; soft noise haze, drift and shadows |
| `SoftGlow` | `visual_time`, `pulse_frequency`, `pulse_amplitude`; restrained additive overlays |
| `WindSway` | `visual_time`, `strength`, `frequency`, `phase_offset`, `anchor_stiffness`, `wind_direction`, `banner`; anchored isolated foliage/fabric only |
| `WaterRipple` | `visual_time`, `ripple_strength`, `ripple_speed`, `wave_frequency`, `flow`, `highlight_strength`, `water_tint`, `surface_mask`, `use_mask`; isolated water or painted reflection texture |
| `DistanceBlur` | `blur_strength`, `distance_fade`, `haze_tint`; five-tap sampling of a supplied distant texture only, never the screen |

Wind and water resources are functional, graphically compiled swatches in the
capture suite. They are **not applied to the flattened settlement**. It contains
painted rivers, trees, bridges and buildings, but lacks qualified masks/separated
surfaces. Use an isolated water image (optionally including painted reflections)
and a grayscale water mask before attaching water. Keep `ripple_strength` around
0.0015 for still water. Reflections can be a masked, painted/duplicated image using
the same material. Full-scene mirrored rendering is intentionally absent.

The sway vertex shader holds the base (`UV.y=1`) stationary. For banners, `banner`
holds the left attachment (`UV.x=0`) stationary. Only four corners of an ordinary
sprite deform; richer fabric needs an authored subdivided mesh. Animated windows
need separate emissive masks/cutouts. No automatic occluders, whole-building
brightness pulses, new artwork or derived game masks were created.

## Practical extensions

To add a localized glow to a building, supply the existing foundation position and
an offset measured on its cutout, scaled by that building's display size:

```csharp
// Rebuild anchored emitters only when the viewed city's building types change.
var glow = view.AddEmitter(foundation + new Vector2(0, -displaySize * .64f),
    EnvironmentEmitterKind.Crystal, displaySize * .16f);
```

For smoke, measure the chimney relative to the same foundation:

```csharp
view.AddEmitter(foundation + chimneyOffset * displaySize,
    EnvironmentEmitterKind.Smoke, displaySize * .12f);
```

The current default integration is in `SettlementView.UpdateEnvironmentAnchors`.
Do not attach emissions to arbitrary screen coordinates or empty plots. To add a
new weather layer:

```csharp
var fog = view.AddLayer("HighMist", "AtmosphericHaze", -5);
fog.Bounds = distantSceneryRegion;
fog.Opacity = .06f;
fog.Velocity = new Vector2(3, 0);
fog.TextureScale = new Vector2(350, 120);
// EnvironmentView updates registered layers automatically with the shared clock.
```

For an isolated animated water/foliage Sprite2D, retain a ShaderMaterial using
`controller.Shader("WaterRipple")` or `controller.Shader("WindSway")`, assign it to
that sprite, set its mask/strength once, and update `visual_time` from
`controller.VisualTime` only while visible/enabled. Dispose the material with its
owning node. Never use a shared material instance for independently tuned objects.
New particle types should join the bounded batch or use one retained particle
system per region, with explicit seed, visibility and pause handling.

## Verification

```sh
mise run build
mise run test-core
mise run test-shared
mise run test-session
mise run test-animation
mise run capture-battle
mise run capture-arena
mise run capture-settlement
mise run test-environment
# Existing native picking/camera suites with effects enabled:
mise run capture-arena -- --environment-effects full --environment-time 8
mise exec -- bash tools/graphical.sh capture-settlement --environment-effects full --environment-preset mist --environment-time 8
```

The new task captures real legally earned battles at fixed playback tick while
sampling atmosphere at 2/8 seconds, all qualities, 1/6-player local/overview, both
projections and resolutions. It asserts changed pixels across time, exact repeated
samples, exact restoration after Off, unchanged combat tick, all nine settlement
picks, independently animated shader layers, scene transition cleanup and zero
steady decoding. It captures all seven settlement presets at 2/12 seconds, menu
motion, technical shader swatches, missing artwork and debug fallback. Original
camera/purchase/native picking suites remain the stronger interaction checks.
Inspect PNGs in `artifacts/captures/godot/environment/`; passing compilation alone
is not graphical verification. See `docs/VERIFICATION.md` for executed results.

## Remaining limits

Lighting uses additive texture/procedural glow and localized tint, not renderer
bloom, realtime occlusion or gameplay lights. Smoke origins on current cutouts
use explicit foundation-relative offsets; changed artwork requires retuning.
The map has no qualified depth/water/emission/foliage masks. No dynamic river
reflection, moving painted trees, window animation or reconstructed depth is
claimed. Atmosphere stays within the authored world bounds; the existing soft
arena backdrop fills outside those bounds. No new gameplay weather, Steam/native
Windows qualification, renderer migration or physical GPU performance claim is
included. The known intermittent native .NET/JIT crash risk remains.
