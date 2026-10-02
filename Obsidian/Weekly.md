# Weekly

The running log of the project. One entry per week: what was built, how it works at that point, and the goal for the next week. Deeper explanations live in the linked notes; see [[Home]] for the full map.

---

## Week 1 — Foundations (13 Sep – 1 Oct 2026)

**Theme:** get a character moving through a dungeon that reacts to them, and prove the two riskiest ideas early: an enemy that can find its way around walls, and levels that build themselves.

### What happened

1. **Project setup.** Unity 6 project with the 2D render pipeline, the new Input System, Cinemachine and the Catacombs tileset. The sprite sheet was sliced into 1024 tiles and a tile palette.
2. **Player movement.** WASD and gamepad movement on a physics body, with a camera that follows the player.
3. **Room entering logic.** A first version of "only the room you are in is visible", built in SampleScene with trigger boxes and black overlays.
4. **Movement and animation rework.** Rotation-based turning was removed and redone as four fixed facing directions with idle and run animations for each. This took several passes and was finished on 1 Oct.
5. **Slime enemy.** A big slime that patrols, spots the player in a vision cone, chases them around walls and searches when it loses them. It can split into two baby slimes that merge back together.
6. **Level randomizer.** A new test scene where a button builds a random level from the tileset. It started as a single room and was then upgraded to several rooms joined by doorways, with unvisited rooms kept dark.
7. **Menus.** A start scene with Start, Settings and End, and a pause menu on Escape in the game scene. Both share one settings panel. The two scenes are now in the build list, with the menu first.
8. **Tooling.** The Unity editor was connected to Claude Code through Unity's MCP server, so scenes can be built, inspected and play-tested from the terminal. This vault and `CLAUDE.md` were created.

### How things work so far

- **The player is the centre of everything.** It is the only object tagged `Player`, and the room system and the slime both find it by that tag. See [[Player]].
- **Walls are defined by a tag.** Anything with a collider tagged `wall` blocks the slime's sight and its pathfinding. The generated levels tag their whole wall tilemap this way. See [[Pathfinding]].
- **Rooms are objects, not tiles.** A room is a trigger box plus a black sprite. One manager checks which box contains the player and hides every other room. Hand-built and generated levels use the same manager. See [[Rooms and Darkness]].
- **Levels are data-driven.** The randomizer holds lists of floor blocks, wall pieces and pillars, and lays them out by rule. Changing how a level looks means changing lists in the Inspector, not code. See [[Level Randomizer]].
- **The game has a loop around it.** Start menu → game → pause → back to the menu, with pausing done through the time scale. See [[Menus and Game Flow]].
- **Enemies run on a small state machine.** Patrol, chase and search, with a shared A* pathfinder that needs no baking. See [[Slime Enemy]].

[[Architecture]] shows how these connect.

### Where it stands

Working and tested in Play mode: movement and animation, room darkness, level generation with doorways, slime patrol and chase, and the menu flow.

Not working yet: there is no way to hurt anything. The slime's split and merge code exists but nothing triggers it, and the baby slime prefab points at a health script that was never added to the project. The slime also has not been put into a generated level yet.

The randomizer and menu work is uncommitted at the time of writing.

### Goal for Week 2 — Combat

Give the player an attack, give the player and enemies health, and let the slime hurt the player. This turns the existing pieces into a playable loop: enter a dark room, fight what is inside, move on.

The plan and its open decisions are in [[Roadmap]].
