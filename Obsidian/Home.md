# gyarte — Project Blueprint

This vault is the blueprint of the project: what exists, how the parts depend on each other, and where it is going. Start here.

**The game:** a top-down 2D dungeon crawler in Unity 6 (6000.5.2f1, URP 2D). The player moves through dark catacomb rooms that light up as they are entered, and slimes patrol, chase, split and merge.

**Stage:** testing. The game loop is playable end to end in the `Demo` scene; everything is still placeholder and nothing is final.

## Map

| Note | What it covers |
|---|---|
| [[Weekly]] | The running log: what happened each week and the goal for the next one |
| [[Architecture]] | How every system connects to the others |
| [[Player]] | Movement, facing, animation, input |
| [[Rooms and Darkness]] | The rule that only the current room is visible |
| [[Level Randomizer]] | Random multi-room levels built from the tileset |
| [[Combat]] | The scythe swing, the fire spell, cooldowns, and health for everything |
| [[Slime Enemy]] | Big slime AI, splitting and merging |
| [[Pathfinding]] | The grid A* the slime uses to walk around walls |
| [[Game Loop]] | The run: random levels, waves, key and gate, ability choices, death |
| [[Inventory and Abilities]] | Keys and abilities the player carries, and how abilities are made |
| [[HUD]] | Health, the attack corner, and the loop's on-screen text |
| [[Menus and Game Flow]] | Start menu, pause menu, settings, and how scenes follow each other |
| [[Test Menu]] | The developer window for spawning enemies and changing settings live |
| [[Scenes and Assets]] | Every scene, prefab and art source, and what each is for |
| [[Roadmap]] | What is done, what comes next, and known issues |

## Current state in one paragraph

The player can move in four directions with matching animations ([[Player]]). Levels can be generated at the press of a button: several walled rooms joined by doorways, with every room but the current one hidden ([[Level Randomizer]], [[Rooms and Darkness]]). A start menu leads into the game and Escape opens a pause menu ([[Menus and Game Flow]]). A slime enemy can patrol, spot and chase the player around walls ([[Slime Enemy]], [[Pathfinding]]). The player fights with a scythe swing and a fire spell, both limited by a cooldown or charges, on a gamepad or the keyboard. A killed big slime splits into two babies that merge back unless one is killed. Slimes hurt the player by touching them ([[Combat]]). Since 5 Oct 2026 this is a game: Start opens the `Demo` scene. There each level is a fresh random layout, the player survives waves of slimes, finds the key, opens the gate and moves on, picking an ability every second level, until health runs out and the run ends back at the menu ([[Game Loop]], [[Inventory and Abilities]], [[HUD]]). New features are tried in `testing the new thing` first, and everything can be adjusted from the [[Test Menu]].

## Keeping this vault true

- When a system changes, update its note in the same piece of work.
- When something new is added to the game, add it to the [[Test Menu]] too.
- At the end of each week, add an entry to [[Weekly]] and move the finished items out of [[Roadmap]].
- Notes describe how things work and why; exact values live in the Inspector and the code.
