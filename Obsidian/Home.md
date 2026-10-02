# gyarte — Project Blueprint

This vault is the blueprint of the project: what exists, how the parts depend on each other, and where it is going. Start here.

**The game:** a top-down 2D dungeon crawler in Unity 6 (6000.5.2f1, URP 2D). The player moves through dark catacomb rooms that light up as they are entered, and slimes patrol, chase, split and merge.

**Stage:** testing. Everything so far is groundwork built in test scenes; nothing is final.

## Map

| Note | What it covers |
|---|---|
| [[Weekly]] | The running log: what happened each week and the goal for the next one |
| [[Architecture]] | How every system connects to the others |
| [[Player]] | Movement, facing, animation, input |
| [[Rooms and Darkness]] | The rule that only the current room is visible |
| [[Level Randomizer]] | Random multi-room levels built from the tileset |
| [[Slime Enemy]] | Big slime AI, splitting and merging |
| [[Pathfinding]] | The grid A* the slime uses to walk around walls |
| [[Menus and Game Flow]] | Start menu, pause menu, settings, and how scenes follow each other |
| [[Scenes and Assets]] | Every scene, prefab and art source, and what each is for |
| [[Roadmap]] | Week 2 combat plan, later steps, and known issues |

## Current state in one paragraph

The player can move in four directions with matching animations ([[Player]]). Levels can be generated at the press of a button: several walled rooms joined by doorways, with every room but the current one hidden ([[Level Randomizer]], [[Rooms and Darkness]]). A start menu leads into the game and Escape opens a pause menu ([[Menus and Game Flow]]). A slime enemy can patrol, spot and chase the player around walls ([[Slime Enemy]], [[Pathfinding]]), but nothing can deal or take damage yet. That gap is the Week 2 goal: combat ([[Roadmap]]).

## Keeping this vault true

- When a system changes, update its note in the same piece of work.
- At the end of each week, add an entry to [[Weekly]] and move the finished items out of [[Roadmap]].
- Notes describe how things work and why; exact values live in the Inspector and the code.
