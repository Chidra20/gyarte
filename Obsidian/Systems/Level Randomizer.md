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

## How each part looks the way it does

**Floor.** The floor art is drawn as 2×2 tile blocks, so the floor is placed block by block and room sizes are given in blocks. Each room picks two floor styles and spreads them in large patches using Perlin noise, with an occasional random block mixed in. That mimics a hand-painted floor.

**Walls.** The frame uses the arch pieces from the sheet: a top beam, a bottom beam, two columns and four corners, plus a cap piece under each top corner. Beams and columns are short patterns that repeat along the wall.

**Doorways.** A sideways door goes through two columns and avoids the top and bottom rows of both rooms, so the wall above and below stays intact. A cap piece is placed under it to finish the columns. A vertical door goes through a top beam and a bottom beam.

**Pillars.** Each pillar is a small grid of tiles on the wall tilemap, so it blocks movement like a wall. A two-cell gap is kept around every pillar, which is what guarantees the player (about 1.1 units tall) can always get past.

**Collision.** Wall and pillar tiles collide using the shape of their sprites. No colliders are set by code.

## What you can change in the Inspector

- Room count range and room size range (in blocks)
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
- **Room roles:** start, exit, treasure, boss, chosen by distance from the start.
- **Seeds**, so a level can be reproduced for testing or sharing.
- **Extra doors** between rooms that happen to touch, to create loops.
- **Decoration pass** using `decorative.png`, torches and candles from the art pack.
- **Use it for the real game:** generate on scene load instead of from a button. The scene is already in the build list and reachable from the start menu.
