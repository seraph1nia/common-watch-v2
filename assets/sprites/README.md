# Optional sprites

These directories intentionally contain no game artwork. Both clients default to shapes and text and ignore every sprite unless `--render-mode sprites` is selected.

Logical identifiers resolve below this folder:

- `building/lumbermill` → `buildings/lumbermill.png`
- `building/farm` → `buildings/farm.png`
- `building/barracks` → `buildings/barracks.png`
- `unit/swordsman` → `units/swordsman.png`
- `unit/enemy_swordsman` → `units/enemy_swordsman.png`
- `terrain/hex` → `terrain/hex.png`

Buildings and unit types use lowercase enum names without spaces. Missing or corrupt images use the labeled geometric fallback. Images never set logical positions, hit areas or rules. Use only artwork you have permission to use; no artwork pipeline is part of this project.
