# Decorations

How generated rooms get their props, torches, candles and spike traps, and how that data is made. Part of the [[Architecture]]; back to [[Home]].

**Files:**
- Scripts in `Assets/Prefabs/Scripts/`: `Decoration.cs`, `RoomTheme.cs`, `DecorationPlacer.cs`, `LevelStyle.cs`, `FrameAnimator.cs`, `GlowFlicker.cs`, `SpikeTrap.cs`.
- Editor builders in `Assets/Prefabs/Scripts/Editor/`: `CatacombsStyleBuilder.cs`, `CatacombsDecorBuilder.cs`.
- Generated assets in `Assets/Prefabs/Level/`: `Catacombs Style.asset`, `Decor/` (prefabs), `Decorations/`, `Themes/`.

Added on 7 Oct 2026. The design is in `docs/superpowers/specs/2026-10-07-richer-levels-design.md`.

## The idea: data, not code

Nothing about a coffin or an urn is written in code. There are three kinds of asset:

- **Decoration:** one *kind* of thing. Which prefab (or several look-alike **variants**, such as the five brown urns), the **rule** for where it may go, its **footprint** (the floor cells it stands on; tall things only count their base), and whether it **blocks** movement or is a **hazard**.
- **Room Theme:** what a kind of room contains. A list of decorations, each with a min and max count. Themes can be marked as traps.
- **Level Style:** the art set of one tileset. The floor colours, pits, grates, the stairs and bars, the themes, the start theme and the torch. The randomizer points at one style; a new art pack would get its own ([[Level Randomizer]]).

## Placement rules

| Rule | Where |
|---|---|
| Against top wall | The top floor row, backed against the wall (coffins, upright sarcophagi, hanging chains and basins) |
| Corner | The four corners (large urns) |
| Along wall | Any cell touching a wall (urns, posts, broken sarcophagi, candle pairs) |
| Anywhere | The floor at least 2 cells from the walls (candles, posts, lying sarcophagi) |
| Centre | The middle half of the room (spike fields) |
| On top wall | The wall row itself, on plain wall only, never in a doorway gap (torches, the skull ornament) |

For each decoration the randomizer tries up to 20 random candidate spots. A spot is taken only if the level-wide occupancy says it's free:

- **Blocking things** also keep one cell of space around them.
- **After placing anything that blocks,** a flood fill checks that the spawn can still reach every doorway and the exit. If not, the item is taken back out.

So a decoration can never seal a room off. Doorways, the cells in front of them, the spawn and the stairs are always reserved.

## The themes

| Theme | Contents |
|---|---|
| Start (the first room only) | 1–2 candles, 1–2 urns |
| Crypt | lying sarcophagi (closed and broken), upright sarcophagi, wooden coffins, candle pairs, candle stubs |
| Storeroom | large urns in corners, urns along the walls, broken urns, the odd coffin and post |
| Shrine | skull post, brazier post, hanging basins and chains, skull ornament, candles |
| Trap Room | one or two spike fields (2×2 or 3×2), broken urns, candle stubs |

Every other room gets a random theme. A trap room is never the exit room and never directly follows another trap room. Most rooms also get 1–3 torches on the top wall. The Test Menu shows the theme of the room you are in.

## The moving parts

- **Torches and candles:** `FrameAnimator` plays the 4 flame frames from a random starting frame, so they don't flicker in step. `GlowFlicker` breathes a soft orange glow sprite around the flame using noise. There are no real 2D lights, so this costs almost nothing. The torch glow is kept small and low so it doesn't show through the wall into the room behind.
- **Spike traps:** `SpikeTrap` cycles *hidden → rising → out → retracting* (2 s, 0.25 s, 1.2 s, 0.25 s by default). Each field starts at a random point of the cycle. While fully out it damages whatever stands on it at most every 0.6 s: the player through `Health` (hurt immunity applies, so about one hit per cycle) and slimes through `EnemyHealth` (no immunity, so about two hits per cycle). Luring slimes onto spikes is a real tactic.
- **Drawing order:** every prop prefab has a root at the bottom centre of its footprint with a *Sorting Group*. The sprite sits on a child, lifted so its bottom edge is on the root. With Y-sorting turned on in the 2D renderer, the player walks behind a tall coffin when above it and in front of it when below, decided by the coffin's base rather than its middle. Spike fields draw under characters (order −1), torches over the wall (order 1).
- **Blocking props** carry a box collider over their footprint, tagged `wall`. The player is stopped, fireballs stop, and slimes path around them like any wall ([[Pathfinding]]).

## How the data was made, and how to rebuild it

All of it is generated, so it can be rebuilt and checked rather than hand-edited:

1. **Identify the sprites.** Which sprite numbers are coffins, urns and so on comes from the [[Catacombs Asset Catalog]]. That catalogue was made by looking at labelled pictures of the sheets.
2. **Export the tile numbers.** `Tools/catacombs-catalog/groups.py` writes `style-tiles.txt`, which lists the tiles of every floor colour, slab, ground patch, pit, grate, the stairs and the bars, by rectangle on the sheet.
3. **Run the builder.** In Unity, **Tools > Catacombs > Build Level Style** reads that file and builds the Level Style. It then builds one prefab per prop sprite, the torch, candles and spike fields, the Decoration assets with their rules and footprints, and the five themes. The sprite numbers per decoration are listed at the top of `CatacombsDecorBuilder.Build`. Running it again overwrites everything with the same result.

To **change** what a theme contains, edit the theme lines in `CatacombsDecorBuilder.Build` and rerun the menu. Editing the theme asset in the Inspector also works, but the next rebuild overwrites it. To **add** a decoration, add a `Deco(...)` line with its sprite numbers, rule, footprint and blocking flag, then put it in a theme.

## In the Test Menu

The Level section has these controls:

- decorations on or off,
- a density multiplier,
- the current room's theme (and whether it's the exit),
- the number of spike traps,
- spike hidden and out times and damage, with a button to apply them to all traps.

The decoration toggle and density take effect on the next level build; the spike settings apply at once. See [[Test Menu]].

## Verified in Play mode (7 Oct 2026, Demo)

- Level 1 had a Start room, two Storerooms and a Crypt (the exit room), with 34 decorations, and was fully connected.
- The crypt showed the barred stairs, a torch with glow, a glowing candle pair, lying and upright sarcophagi, a coffin and candle stubs.
- Standing on spikes cost the player 1 health per cycle; a slime left on spikes lost 6 of 10 health in 8 seconds.
- 300 random spawn points: none on a prop, a spike or a wall.

## Known issues

- Where two floor patterns of a room meet in a straight line, the mortar of one pattern shows as a thin seam ([[Roadmap]]).
- Torch glows of a dark room can still peek a little past the wall edge; they were shrunk to keep this small.
