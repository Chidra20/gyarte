# Richer Levels — Design

**Date:** 2026-10-07
**Status:** approved in conversation ("Sounds good")

## Goal

Use the whole Catacombs pack ([[Catacombs Asset Catalog]] in the vault) to make generated levels richer: varied floors, pits and grates, themed decorations, wall torches and candles that glow, spike traps, and a stairs-down exit behind a barred gate.

## Decisions

| Question | Decision |
|---|---|
| Lighting | Glow sprites (soft additive-looking sprite that flickers), no real 2D lights. Real lighting is a later step. |
| Spikes | A real trap: cycles hidden, rising, out, retracting. While out it damages the player **and** slimes standing on it. Never placed in doorways, on the spawn point or on the exit. |
| Structures | Pits (blocking) and grates (walk-over) in rooms. The stairs-down archway in the farthest room becomes the exit, closed by the barred gate until the key is used. Archway/doorway art is not used yet. |
| Where the art choices live | A new `LevelStyle` ScriptableObject (one per art pack). The randomizer references one. Existing wall and pillar fields stay on the randomizer for now. |
| How data is created | By an editor script (`Tools > Catacombs > Build Level Style`) that reads tile numbers from the catalogue, so it can be re-run and is documented. |

## Components

### LevelOccupancy (new, pure C#)

The level-wide record of cells: `Blocked` (walls, pillars, pits, blocking props), `Hazard` (spikes), `Reserved` (doorways plus one cell in front, the spawn area, the exit). Methods:
- `bool IsFree(RectInt area, int padding)`: no cell of area grown by padding is blocked, hazard or reserved.
- `void Block(RectInt)`, `Hazard(RectInt)`, `Reserve(RectInt)`.
- `static bool AllReachable(ISet<Vector2Int> walkable, Vector2Int from, IEnumerable<Vector2Int> targets)`: a flood fill.

The randomizer builds it during `Randomize()`. `TryGetRandomFloorPoint` also refuses any cell whose 3x3 area is blocked or hazard, so waves and the key avoid props and spikes.

**Connectivity rule.** After every blocking placement (pit, blocking prop), a flood fill over walkable floor cells from the spawn cell must reach every doorway cell and the exit front. If it doesn't, that placement is undone.

### LevelStyle (new ScriptableObject)

- `FloorPalette[] palettes`: per colour, the three 2x2 floor blocks (TileA, TileB, Rough), a 2x3 slab stamp, and the ground patch for that colour family.
- `float stampChance`: chance per room of 1–2 slabs or ground patches.
- `TileStamp[] pits` (6x6 square core, 4x4 round well), `TileStamp[] grates` (6x6 large, 2x2 small), with chances per room and minimum room sizes.
- `TileStamp stairs` (4x6: the archway rows 1–6), `TileStamp bars` (4x5), and the cells of the stairs that are walkable.
- `RoomTheme[] themes`, `RoomTheme startTheme`.
- `Decoration torch`, torch count per room range.

`TileStamp`: `int width; TileBase[] tiles` (row-major, top row first; null = leave the cell alone), same convention as today's pillar props.

### Floors

Each room picks one palette, then mixes that palette's three blocks with the existing Perlin noise and accent chance. With `stampChance` it adds 1–2 stamps (a slab, or a ground patch) at even cells, away from doors.

### Structures

- **Pits:** in rooms big enough, at most one pit, placed with at least 3 cells from the room's walls and never on reserved cells. Painted on the **Walls** tilemap (it blocks, slimes path around it, and fireballs stop at it). Marked Blocked; connectivity rule applies.
- **Grates:** at most one per room, painted on the **Floor** tilemap (walkable decoration). Not Blocked.
- **Stairs exit:** in the farthest room (`LevelGraph.FarthestRoom`), against the top wall, at an x where the top wall has no doorway within the stairs' width ±1. Archway frame painted on Walls, the walkable stair cells on Floor. The randomizer exposes `bool HasStairsExit`, `Vector2 ExitPoint` (the stair opening's centre) and `RectInt ExitCells`. If no spot fits, `HasStairsExit` is false and the game loop uses the old gate square.

### Gate (changed)

Optional `closedLook` and `openLook` GameObjects, switched on Open. A new **Stairs Gate** prefab:
- a trigger over the stair opening plus one row below,
- a `Bars` child (the bar tiles, drawn by a child Tilemap under the prefab's own Grid) with a solid collider tagged `wall` over the opening, active only while closed.

Logic unchanged: touch with key → open (bars vanish), then walk onto the stairs → Entered.

### GameLoop (changed)

`PlaceGate` uses the Stairs Gate at `randomizer.ExitPoint` when `HasStairsExit`, else the old gate in the farthest room as now. The key logic is unchanged.

### Decorations (new)

- `Decoration` (ScriptableObject): `GameObject prefab`, `Placement rule` (`AgainstTopWall`, `Corner`, `AlongWall`, `Anywhere`, `Centre`, `OnTopWall` for torches), `Vector2Int footprint` (cells on the floor it occupies), `bool blocks`, `bool hazard`.
- `RoomTheme` (ScriptableObject): `string themeName`, list of `(Decoration, min, max)`.
- **Themes:**
  - **Crypt:** lying and upright sarcophagi, upright coffins, candles, candle stubs.
  - **Storeroom:** brown and green urns, broken urns, coffins.
  - **Shrine:** skull post, brazier post, hanging basins and chains, candle groups.
  - **Trap room:** 1–2 spike fields (2x2 or 3x2), a few urns.
  - **Start:** a few candles and urns only.
- **Placement:** for each entry, `count = Random(min, max)`. Candidates come from the rule:
  - `AgainstTopWall`: top floor row.
  - `Corner`: the four corner footprints.
  - `AlongWall`: any edge row or column.
  - `Anywhere` / `Centre`: interior cells, at least 2 from the walls.
  - `OnTopWall`: the wall row itself, for torches, on beam tiles only (not corners or doorways).

  A candidate must pass `IsFree` (padding 1 for blocking, 0 otherwise), then the connectivity rule if it blocks. Up to 20 tries per item.
- **Every room gets a theme:** the start room gets `startTheme`; other rooms get a random theme, with trap rooms at most every other room and never the exit room. Plus 1–3 torches on the top wall.
- `RoomThemeAt(int room)` exposes the theme name for the Test Menu.

### Prefabs and behaviour scripts (new)

- **Prop prefabs**, one per decorative sprite used: a SpriteRenderer with the pivot at bottom-centre and sorting order 0. Blocking props get a BoxCollider2D over their footprint, tagged `wall`.
- **`FrameAnimator`:** plays a sprite list at a frame rate, starting on a random frame (torches and candles).
- **`GlowFlicker`:** a child SpriteRenderer with the soft glow sprite, warm colour, alpha and scale wobbling with Perlin noise. Used on torches and candles.
- **`SpikeTrap`:**
  - Phases `hiddenTime` → rising (frames 1–3) → `outTime` (frame 4, dangerous) → retracting. It is dangerous only while fully out.
  - While dangerous, every `hitInterval` it damages anything on its cells: the player through `Health` (hurt immunity applies), slimes through `EnemyHealth`.
  - A pure `static SpikePhase PhaseAt(float t, timings)` drives it, so it can be tested.
  - A field of several spikes shares one start time.
- **Import fix:** `decorative.png`, `torch_*`, `candle*`, `spike_*` set to 16 pixels per unit, Point filter, no compression. `spike_4` and `candleB_*` are re-sliced to one sprite per file (Single mode).
- **Y-sorting:** the URP 2D renderer's transparency sort mode is set to Custom Axis (0, 1, 0), so characters and props at order 0 draw by height. Floors (−2), walls (−1), torches on walls (1) and darkness (10) are unchanged.

### Test Menu

The Level section gets:
- decorations on/off,
- a density multiplier (scales every count),
- "This room: <theme>",
- regenerate (already there),
- spike timings and damage (applied to all spikes now in the level).

### Editor setup

`Tools > Catacombs > Build Level Style` creates or updates:
- `Assets/Prefabs/Level/Catacombs Style.asset`,
- the Decoration and RoomTheme assets under `Assets/Prefabs/Level/Decorations/`,
- the prefabs.

It takes tile numbers from the catalogue groups, and is idempotent. The vault records it as the way to rebuild.

## Edge cases

- **Tiny rooms:** pits and grates are skipped below their minimum room size; decorations simply find fewer spots.
- **No stairs spot:** fall back to the old gate square.
- **Sealing a room:** prevented by the connectivity rule (undo the placement).
- **Waves and the key:** `TryGetRandomFloorPoint` avoids blocked and hazard cells; spawn markers never land on spikes or props.
- **Slimes:** they path around props (wall-tagged colliders) and pits (Walls tilemap). Spikes hurt them too.
- **Rebuilds:** all decoration objects live under a `Decorations` child of the randomizer and are destroyed on `Randomize()`, like the room objects.

## Testing

- **EditMode:** `LevelOccupancy` (IsFree with padding, reachability with and without a blocking wall), placement candidates per rule, theme picking (start theme, no trap in the exit room), and `SpikeTrap.PhaseAt`.
- **Play mode in Demo:**
  - several generated levels: every room has a theme, no sealed doorways (flood-fill check over the built level), and pits and grates appear;
  - the stairs exit is closed by bars, opens with the key, and entering it finishes the level;
  - spikes hurt the player and a slime;
  - waves don't spawn on props or spikes;
  - screenshots;
  - FPS compared with the 62 FPS laptop baseline (measured on this machine; the user re-measures on the laptop).

## Vault

- **New `Systems/Decorations.md`**, including how the assets are built and how to add a decoration.
- **Updated:** Level Randomizer (occupancy, floors, structures, exit, how it was made), Game Loop (stairs exit), Catacombs Asset Catalog (which groups are now used), Test Menu, Scenes and Assets, Architecture, Roadmap.

## Out of scope

Real 2D lighting, archway/doorway art as room connections, rooms shaped other than rectangles, more art packs.
