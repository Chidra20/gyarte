# Level Randomizer

Builds a random multi-room level from the Catacombs tiles. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/LevelRandomizer.cs`, scene `Assets/Scenes/testing the new thing.unity`

## Scene setup

```
Grid              LevelRandomizer
├── Floor         Tilemap (sorting order -2)
└── Walls         Tilemap (order -1) + TilemapCollider2D, tag: wall
Rooms             RoomManager — room objects are created under this
Canvas            PauseMenu (see [[Menus and Game Flow]])
├── Randomize Button   onClick → LevelRandomizer.Randomize()
└── Pause Menu         hidden until Escape is pressed
EventSystem
Player, CinemachineCamera, Main Camera
```

The scene is saved with a generated level already in it. On Play the randomizer only builds a new one if the floor tilemap is empty, so the level changes only when the button is pressed.

## Richer levels (7 Oct 2026)

With a **Level Style** assigned (`Catacombs Style`), a build does a lot more than lay out bare rooms. Without one, rooms stay as before.

- **Occupancy.** One level-wide record of taken cells (`LevelOccupancy`): **blocked** (pillars, pits, blocking props), **hazard** (spikes), **reserved** (doorways and the cell in front, the spawn, the stairs and the two rows in front of them). Everything placed asks it first. Spawn points for waves and the key avoid blocked and hazard cells.
- **Never sealed.** After every pit or blocking prop, a flood fill over the walkable floor checks that the spawn can still reach every doorway and the exit (`StillConnected`). If not, that piece is taken back out.
- **Floors by colour.** Each room picks one of 6 colour families and mixes that family's three 2×2 patterns with the noise. Sometimes it stamps a smooth slab (2×3) or a plain ground patch (4×4) on top.
- **Pits and grates.** At most one of each per room (never in the start room):
  - **Pits** (6×6 square or 4×4 round) go on the **Walls** layer, so they block and slimes path around them. They keep 3 cells from the walls.
  - **Grates** go on the new **Details** layer (walkable, no collider, drawn over the floor without replacing it). Their cells are reserved, so nothing else lands on them. A spike field under a grate would be hidden but still hurt.
- **Stairs exit.** In the room farthest from the start, the stairs-down archway is built against the top wall, away from any doorway in that wall. Its frame goes on Walls and its steps on Details. The [[Game Loop]] puts the barred gate over it. If it doesn't fit, the old gate square is used.
- **Decorations.** Each room gets a theme and its props, torches and traps; see [[Decorations]].

The build order is: lay out rooms → reserve doorways and the spawn → floors and walls → doorways → stairs exit → per room: pillars, pits and grates, decorations, darkness object → move the player.

**The Details tilemap.** A third tilemap under `Grid` (between Floor and Walls, order −1, no collider) holds walk-over pieces that are partly see-through: the stair steps and grates. Painting them on the Floor layer replaced the floor tile and left black where the art is transparent.

**How the style data was made.** Tile numbers come from the [[Catacombs Asset Catalog]] via `Tools/catacombs-catalog/groups.py` (`style-tiles.txt`). The asset is built by **Tools > Catacombs > Build Level Style**. Nothing is picked by hand in code.

Verified on 7 Oct 2026, over 15+ generated levels:
- **Connectivity:** every level was fully connected.
- **Stairs:** the stairs fitted every time.
- **Pits and grates:** pits landed in about 1 in 5 rooms, grates in about 2 in 5.

## What one press does

`Randomize()` runs these steps in order:

1. **Clear** both tilemaps and delete the old room objects.
2. **Lay out rooms.** The first room is centred on cell (0, 0). Each further room picks a random existing room and a random side, and is placed against it with a random offset. A candidate is dropped if its walls would overlap another room; walls may touch. This repeats until the target room count is reached or 200 attempts are used up.
3. **Build each room.** Floor first, then a frame of walls one cell outside the floor.
4. **Cut doorways.** Every room was attached to a parent, and that attachment is its door: a 2×2 hole through the two touching walls, filled with floor.
5. **Place pillars** in each room, away from the walls, each other, the doorways and the spawn point.
6. **Create room objects** for [[Rooms and Darkness]].
7. **Move the player** to the centre of the first room.

Because every room attaches to an earlier one, the level is always fully connected.

## How the tile lists were filled in

The randomizer only knows which tiles are floor, wall pieces and pillars because someone filled in its Inspector lists (on the `Grid` object). That was done in Week 1 by an earlier Claude Code session. It looked at the Catacombs sheet, picked the tiles by eye, put them in the lists and checked the result in a scene capture. No record was kept of how. On 7 Oct 2026 the whole pack was catalogued and labelled ([[Catacombs Asset Catalog]]); the tiles in the lists are the groups `WallFrame-A`, `Floor-TileA`, `Floor-TileB`, `Floor-Rough` and the pillar groups there.

To change what a room looks like: find tiles with the Project search (`l:Floor`, `l:WallFrame-B` …) and drag them into the lists on the `Grid` object's Level Randomizer.

## What it tells other systems about the level

After a build the randomizer keeps a description of the level it made, for the [[Game Loop]] to use:

- **The rooms**, as rectangles of floor cells. Room 0 is always the start room.
- **The links**: which two rooms each doorway joins. Every room is attached to exactly one earlier room, so the links form a tree with no loops.
- **A random free spot in a room.** It picks a floor cell whose eight neighbours hold no wall or pillar tile, so a character placed there is never stuck. It checks the tiles rather than the colliders, because the wall collider is only rebuilt after the level has been built, and enemies and the key are placed in that same moment.
- **Which room a point is in.** A doorway counts as part of one of the two rooms it joins.

`LevelGraph` answers distance questions from that data: how many doors apart two rooms are, which room is farthest from a given one, and all rooms ordered from far to near. Ties in door count go to the room that is farther away in a straight line. The game loop uses it to put the gate as far from the start as possible and the key away from the player.

This data only exists after `Randomize()` has run in the current session. A level that was saved into the scene and loaded with it has no room list.

**Building on start** can be switched off (`buildOnStart`). The sandbox keeps it on. The Demo scene turns it off, because the game loop decides when each level is built.

## How each part looks the way it does

**Floor.** The floor art is drawn as 2×2 tile blocks, so the floor is placed block by block and room sizes are given in blocks. Each room picks two floor styles and spreads them in large patches using Perlin noise, with an occasional random block mixed in. That mimics a hand-painted floor.

**Walls.** The frame uses the arch pieces from the sheet: a top beam, a bottom beam, two columns and four corners, plus a cap piece under each top corner. Beams and columns are short patterns that repeat along the wall.

**Doorways.** A sideways door goes through two columns and avoids the top and bottom rows of both rooms, so the wall above and below stays intact. A cap piece is placed under it to finish the columns. A vertical door goes through a top beam and a bottom beam.

**Pillars.** Each pillar is a small grid of tiles on the wall tilemap, so it blocks movement like a wall. A two-cell gap is kept around every pillar, which is what guarantees the player (about 1.1 units tall) can always get past.

**Collision.** Wall and pillar tiles collide using the shape of their sprites. No colliders are set by code.

## What you can change in the Inspector

- Room count range and room size range (in blocks). Since 7 Oct 2026 rooms are 7×5 to 12×8 blocks, i.e. 14×10 to 24×16 tiles (they were 4×3 to 9×6), so waves have room to spawn away from the player
- The list of floor blocks, patch size and how often odd blocks appear
- Every wall piece and repeating pattern
- The list of pillars, how many per room and the spacing around them
- The darkness sprite, colour and sorting order

## Limits

- **Rooms are rectangles.** The tileset has no inner-corner pieces, so L-shapes and corridors are not possible with this wall set.
- **Layouts tend to sprawl** into chains because rooms attach to a random earlier room. There are no loops; every room has exactly one way back.
- **No enemies, loot or goal** are placed yet.
- **Slimes do not survive a rebuild.** A slime's [[Pathfinding]] remembers where walls were, so one that was alive during `Randomize()` would walk into the new walls.

## Links to other systems

- Creates the room objects that [[Rooms and Darkness]] manages.
- Tags the wall tilemap `wall`, which is what makes generated levels readable by the [[Slime Enemy]] and its [[Pathfinding]].
- Moves the [[Player]] on every rebuild.

## How it can progress

- **Spawn enemies per room**, skipping the start room. The room list and door list are already known at the right moment. This is the bridge to Week 2 combat ([[Roadmap]]).
- **Room roles:** treasure, boss, chosen by distance from the start (the exit already is; see [[Game Loop]]).
- **Seeds**, so a level can be reproduced for testing or sharing.
- **Extra doors** between rooms that happen to touch, to create loops.
- **Decoration pass** using `decorative.png`, torches and candles from the art pack.
- **Use it for the real game:** generate on scene load instead of from a button. The scene is already in the build list and reachable from the start menu.
