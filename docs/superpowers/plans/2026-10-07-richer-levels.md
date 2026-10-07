# Richer Levels Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (chosen: inline, as for the game loop). Steps use checkbox (`- [ ]`) syntax.

**Goal:** Generated levels use the whole Catacombs pack: palette floors with stamps, pits and grates, themed decorations, glowing torches and candles, spike traps, and a stairs exit behind bars.

**Architecture:** A pure `LevelOccupancy` tracks blocked, hazard and reserved cells for the whole level and checks reachability. A `LevelStyle` asset holds the art choices (floors, stamps, structures, themes); `Decoration` and `RoomTheme` assets describe props. The randomizer gains build steps after the walls: floors by palette, structures, the stairs exit, then decorations. Editor menus build the assets from the catalogue's tile numbers.

**Spec:** `docs/superpowers/specs/2026-10-07-richer-levels-design.md`

## Global Constraints

- Project conventions from CLAUDE.md: scripts in `Assets/Prefabs/Scripts/` (editor scripts in `Assets/Prefabs/Scripts/Editor/`), no namespaces, `[Header]`/`[Tooltip]`, short why-comments, Unity 6 APIs.
- No commits unless the user asks.
- Tests: EditMode tests in `Assets/Tests/Editor/`, run with `TestRunLogger.Run("<Class>")`, results in `Temp/test-results.txt`. Hooks that tests need are public.
- Every new feature gets Test Menu controls. The vault records *how* each thing is made (feedback memory).
- Spec values: no real lights; spikes hurt player and slimes; pits block, grates don't; stairs exit with bars; never sealed rooms.
- Tile numbers come from `Tools/catacombs-catalog/groups.py` rectangles; the editor setup reads them from a generated `Tools/catacombs-catalog/style-tiles.txt` so nothing is hand-typed in C#.

## Review Focus

1. **A room sealed off by a prop or pit.** No doorway, the spawn or the exit is ever unreachable. Covered by the flood-fill rule (Task 2 tests) and a Play check over several levels (Task 8).
2. **Waves spawning inside props or on spikes.** `TryGetRandomFloorPoint` must respect occupancy (Task 2 test, Task 8 Play check).
3. **No stairs spot.** The loop must fall back to the old gate (Task 5).
4. **Spikes vs hurt immunity.** The player isn't drained every frame; slimes take damage on the interval (Task 7 tests plus Play check).
5. **Y-sorting side effects.** HUD, darkness and fireballs still draw correctly after the sort-axis change (Task 1 capture, Task 8 screenshots).

---

### Task 1: Import fix and Y-sorting

**Files:** the pack's `.meta` files (via `TextureImporter`), the URP 2D Renderer asset.
- [ ] Find the 2D renderer data asset used by the project (`Renderer2DData`), set transparency sort mode to Custom Axis (0,1,0) through `SerializedObject`. Record old values in the ledger.
- [ ] Set `decorative.png`, `torch_*`, `candle*`, `spike_*` to PPU 16, Point, uncompressed. Set `spike_4` and `candleB_*` to Single sprite mode. Check sprite counts afterwards.
- [ ] Check: a screenshot of Demo in Play mode looks unchanged (HUD, darkness, player over floor). Console clean.

### Task 2: LevelOccupancy (pure) and its use in the randomizer

**Files:** create `Assets/Prefabs/Scripts/LevelOccupancy.cs`, `Assets/Tests/Editor/LevelOccupancyTests.cs`; modify `LevelRandomizer.cs`.

**Interfaces:**
- `class LevelOccupancy { void Block(RectInt), Hazard(RectInt), Reserve(RectInt); bool IsBlocked(Vector2Int); bool IsHazard(Vector2Int); bool IsFree(RectInt area, int padding); static bool AllReachable(ISet<Vector2Int> walkable, Vector2Int from, IEnumerable<Vector2Int> targets); }`
- `LevelRandomizer.Occupancy` (public, read-only); `PlaceProps` (pillars) marks Blocked and reserves doors and spawn; `TryGetRandomFloorPoint` rejects 3x3 areas with blocked or hazard cells; `bool StillConnected()` runs the flood fill over walkable floor cells from the spawn to all door cells and the exit front.

- [ ] Tests (RED): `IsFree_RespectsPadding`, `ReservedIsNotFree`, `Reachable_OpenRoom`, `Unreachable_WhenWallSplitsRoom`, `Reachable_TargetsMissingFromWalkable_False`.
- [ ] Implement; tests GREEN; full suite green.
- [ ] Play check in the sandbox: Randomize 3 times, log `StillConnected()` = true each time and pillar count. Floor points never inside pillars.

### Task 3: LevelStyle, floors and stamps

**Files:** create `LevelStyle.cs` (with `TileStamp`, `FloorPalette`), `Editor/CatacombsStyleBuilder.cs`; extend `Tools/catacombs-catalog/groups.py` to write `style-tiles.txt`; modify `LevelRandomizer.cs` (`public LevelStyle style;` BuildFloor uses a palette when a style is set, else the old floorBlocks).

**Interfaces:**
- `TileStamp { int width; TileBase[] tiles; int Height; }` (row-major, top row first, null = skip).
- `FloorPalette { string name; FloorBlockSet blocks (3 x 2x2); TileStamp slab; TileStamp ground; }`. Reuse the `LevelRandomizer.FloorBlock` struct for the blocks.
- `style-tiles.txt`: lines `key|w|i,i,i,...` (e.g. `floor.0.tileA|2|581,582,622,623`, `slab.0|2|...`, `ground.0|4|...`, `pit.square|6|...`, `pit.round|4|...` with empty cells as -1, `grate.large|6|...`, `grate.small|2|...`, `stairs|4|...`, `bars|4|...`).
- `CatacombsStyleBuilder.Build()` (menu `Tools/Catacombs/Build Level Style`): creates or updates `Assets/Prefabs/Level/Catacombs Style.asset` from the file.

- [ ] Python: add the style export to groups.py (positions → indices via `pos`), run it, inspect the file.
- [ ] Test (RED→GREEN): `TileStamp_HeightAndIndexing` (width 2, 6 tiles → Height 3; tile at (x,y) from the top).
- [ ] Implement LevelStyle, the builder, and palette floors and stamps in the randomizer; assign the style in both scenes.
- [ ] Play check: screenshot several rooms; each room one colour family; stamps visible.

### Task 4: Pits and grates

**Files:** `LevelRandomizer.cs` (new `PlaceStructures(room, index)` after props), `LevelStyle` fields (chances, min room sizes).
- [ ] Test (RED→GREEN) in `LevelOccupancyTests`: `StructureSpot_KeepsDistanceFromWalls` (a static `LevelRandomizer.StructureCandidates(RectInt room, Vector2Int size, int margin)` returns only anchors with the margin).
- [ ] Implement: pit painted on Walls (blocked, connectivity-checked, undone if it seals); grate painted on Floor; never on reserved cells; skipped in the start room.
- [ ] Play check: across 3 levels, pits and grates appear, `StillConnected()` true, slimes path around a pit (spawn one on the far side and watch it reach the player).

### Task 5: Stairs exit, Gate looks, GameLoop

**Files:** `LevelRandomizer.cs` (`BuildStairsExit`, `HasStairsExit`, `ExitPoint`, `ExitCells`), `Gate.cs` (`closedLook`, `openLook`), new prefab `Assets/Prefabs/Stairs Gate.prefab` (built by the style builder: Grid root, `Bars` child Tilemap with the bar tiles, solid BoxCollider2D tagged wall on Bars, trigger on the root), `GameLoop.cs` (`stairsGatePrefab`, PlaceGate uses it), Demo scene wiring.
- [ ] Test (RED→GREEN): `StairsSpot_AvoidsTopWallDoors` (a static helper returns the x-range allowed given door x positions in the top wall).
- [ ] Implement; the fallback to the old gate when no spot fits.
- [ ] Play check in Demo: the exit room shows the archway with bars; touching without a key says "Need a key"; with a key, the bars disappear; walking onto the stairs builds the next level.

### Task 6: Decoration and RoomTheme data and placement

**Files:** `Decoration.cs`, `RoomTheme.cs`, `DecorationPlacer.cs` (static candidate generation), `LevelRandomizer.cs` (`Decorate(room, index)`, `RoomThemeAt`, a `Decorations` parent object, `decorationDensity`, `decorate` toggle), tests `DecorationTests.cs`.

**Interfaces:**
- `enum Placement { AgainstTopWall, Corner, AlongWall, Anywhere, Centre, OnTopWall }`
- `static List<RectInt> DecorationPlacer.Candidates(Placement rule, RectInt room, Vector2Int footprint)`
- `static RoomTheme RoomTheme.Pick(IList<RoomTheme> themes, bool isStart, bool isExit, bool previousWasTrap, RoomTheme startTheme, System.Random rng)`

- [ ] Tests (RED→GREEN): candidates per rule (top row, the four corners, edges, interior ≥2 from walls, wall row for OnTopWall); `Pick_StartRoomGetsStartTheme`; `Pick_NoTrapInExitRoom`; `Pick_NoTwoTrapRoomsInARow`.
- [ ] Implement placement in the randomizer (the IsFree check, the connectivity check for blocking, destroy on rebuild).

### Task 7: Prefabs and behaviours

**Files:** `FrameAnimator.cs`, `GlowFlicker.cs`, `SpikeTrap.cs`, tests `SpikeTrapTests.cs`; the builder creates the prefabs and the Decoration and RoomTheme assets (Crypt, Storeroom, Shrine, Trap, Start) and fills the style's themes and torch.
- [ ] Tests (RED→GREEN): `PhaseAt` over a full cycle (hidden → rising frames → out → retracting, wraps around); `DangerousOnlyWhenOut`.
- [ ] Implement scripts; SpikeTrap damages player `Health` and slime `EnemyHealth` on `hitInterval` while out (OverlapBox over its cells, Player and Enemy layers).
- [ ] Builder: one prop prefab per used decorative sprite (pivot bottom-centre via child offset; collider tagged wall if blocking), Torch (4 frames + glow), CandleA, CandleB, Spikes (5 frames); Decoration assets with rules and footprints; themes.
- [ ] Run the builder; check the asset counts; console clean.

### Task 8: Test Menu, verification, vault

**Files:** `TestMenu.cs` (the Level section: decorations toggle, density, the current room's theme, spike timing and damage), the vault notes.
- [ ] Test Menu controls.
- [ ] Play checks in Demo (god mode as needed):
  - 3 levels with `StillConnected()` true and every room themed;
  - screenshots of a crypt, a storeroom, a shrine and a trap room;
  - spikes hurt the player once per interval and hurt a slime;
  - no wave spawn on a blocked or hazard cell;
  - the stairs exit flow;
  - FPS vs before (note the machine).
- [ ] Full suite green; console clean.
- [ ] Vault: new `Systems/Decorations.md`; updates per the spec, each with how it was made. Final review by a fresh reviewer, fix pass, report.
