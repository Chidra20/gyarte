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

---

## Week 2 — Combat, and a game around it (2 – 8 Oct 2026)

**Theme:** the Week 1 goal was combat. It got done early in the week, and the rest of the week turned the pieces into a game you can actually play: a run with levels, waves, a key and an exit, then two passes of fixes and polish after playing it.

### What happened

1. **Combat (2 Oct).** The player got two attacks, a scythe swing and a fire spell, both on the input actions so they work on a gamepad and on the keyboard. Slimes got health with a hit flash. A big slime at zero health splits into two babies that merge back unless one is killed, so the split-and-merge fight could be played for the first time.
2. **Test menu (2 Oct).** A developer window opened with U for spawning enemies and changing settings live. Since then every new feature is added to it ([[Test Menu]]).
3. **The Demo game loop (5 Oct).** A new `Demo` scene, which the Start button now loads, plays a whole run: random level → waves of slimes → key → gate → next level, with a choice of 1 of 3 abilities every second level, until the player dies and goes back to the menu ([[Game Loop]], [[Inventory and Abilities]]).
4. **Health for everyone (5 Oct).** One `Health` component is shared by the player and the enemies. Slimes hurt the player by touching them; the player has a short hurt immunity after each hit ([[Combat]]).
5. **First playtest fixes (7 Oct).** Knockback on hits, slower slimes that no longer lock on from anywhere, bigger rooms, warning markers before a wave appears, the key dropping near the player, and an FPS counter ([[Slime Enemy]], [[HUD]]).
6. **Richer levels (7 Oct).** Rooms are built from the whole Catacombs pack: themed rooms full of props, torches, candles, pits, spike traps and a barred staircase as the exit ([[Decorations]], [[Level Randomizer]]).
7. **Dash, health bar and spell polish (8 Oct, yumisyumm).** A dash, a health bar with stamina hearts, a burning fireball with its own cast-down animation, a faster swing, rooms that stay lit once visited, and slimes that can wait in unexplored rooms ([[Player]], [[HUD]], [[Combat]], [[Rooms and Darkness]]).

### How things work now

**Attacks.** The **swing** plays an animation and, at the moment the scythe is in front of the player, damages every enemy inside a circle in front of them. A cooldown stops it from being spammed, and since 8 Oct it plays about a third faster. The **fire spell** shoots a fireball that flies straight, passes through enemies and stops at walls. It runs on **charges** that come back one at a time on a timer. Since 8 Oct the fireball also **sets enemies on fire**: a few extra ticks of damage while the slime flickers orange. Once the fireball has left the hand, moving cancels the rest of the cast so the player isn't stuck in place. See [[Combat]].

**Knockback.** A hit pushes the slime away for a moment and pauses its movement. The push checks for walls along its path, so a hit can never shove a slime into a wall. Each attack sets its own strength. Slimes push the player back the same way when they touch them ([[Slime Enemy]], [[Combat]]).

**Dash.** Circle / B / K. The player dashes a short distance in the direction they're moving (or facing, when standing still). It reuses the player's knockback movement, so walls still stop it. While dashing the player **can't be hit**: a separate dodge flag on `Health` ignores damage, and it's kept apart from god mode so the test switch isn't lost. It has a short cooldown, can't start in the middle of an attack, and leaves fading afterimages behind. See [[Player]].

**The run.** `GameLoop` is a small state machine (starting, fighting, key hunt, gate open, choosing, dead). Every state change goes through one table of allowed moves, so odd timings like dying on the frame the gate opens are safe: death always wins. Each level is a fresh random layout. Waves grow with the level number. Since 8 Oct about half of each wave spawns in other rooms, hidden in the dark, so the player runs into them while exploring. When the last wave is cleared, the key drops near the player. The key opens the bars in front of the stairs, and walking onto the steps builds the next level. See [[Game Loop]].

**Abilities.** Each ability is a ScriptableObject: a small script that says what it does and an asset with its name and description. The four that exist are placeholder stat boosts (more health, faster swing, an extra spell charge, more speed). The player's `Inventory` holds abilities and keys for the whole run ([[Inventory and Abilities]]).

**Rooms and darkness.** Rooms you have entered now **stay lit**, and the black cover fades in and out instead of snapping. A slime in a room you haven't explored can't see you, so nothing chases you out of the dark ([[Rooms and Darkness]]).

**Level decoration.** Data, not code: a *Decoration* asset says what a prop is and where it may stand (corner, against a wall, centre…). A *Room Theme* lists which decorations a room gets (crypt, storeroom, shrine, trap room). A *Level Style* holds one art pack. After anything that blocks movement is placed, a flood fill checks that every doorway and the exit can still be reached; if not, the prop is taken back out. So a room can never be sealed off ([[Decorations]]).

**HUD.**
- A red **health bar** slides to the new value. When the player is hit, a lighter trail stays behind for a moment and then drains, so you can see how big the hit was.
- Blue **stamina hearts** show the fire spell's charges, one heart for two casts.
- The **attack corner** shows the swing, the fire spell and the dash, with how long until each is ready and why not (busy, cooldown, no charges).

See [[HUD]].

**FPS counter.** Bottom right. It keeps the frame times of the last half second and shows frames per second, milliseconds per frame, and the slowest single frame (which catches a hitch even when the average looks fine). It uses real time, so pausing doesn't skew it. It also turns VSync off when the game starts, so the number shows what the PC can really do rather than the monitor's 60. See [[HUD]].

**Scenes.** `Demo` tests the game as a whole, `testing the new thing` tries each new feature on its own, `Test AI enemy` is for enemy behaviour, and `Start Menu` leads into `Demo` ([[Scenes and Assets]]).

### First frame rate measurements (7 Oct 2026)

Taken with the FPS counter in `Demo`, VSync off, before any optimisation:

| Machine | Power | FPS |
|---|---|---|
| Laptop: RTX 2050, Intel Core i5-1335U | Plugged in | about 62 |
| Laptop: RTX 2050, Intel Core i5-1335U | On battery | under 15 |
| Desktop: RTX 5060 Ti | — | about 600 |

Optimisation is deferred; these numbers are the baseline to compare against ([[HUD]], [[Roadmap]]).

### Where it stands

The game is playable from the menu to death, and every system has been checked in Play mode. Still open:
- **Bugs.** The swing hits through walls, baby slimes walk through walls, and the pathfinder keeps old walls after a level rebuild.
- **Tuning.** Wave sizes, damage, cooldowns and the merge window have only been checked to work, not to feel right. The new burn may make one fireball per slime too strong.
- **Frame rate.** It is low on weak laptops.

The full list is in [[Roadmap]].

### Goal for Week 3 — make it feel good

Play the loop and tune it: fix the melee-through-walls and baby-slime-through-walls bugs, balance the burn and wave sizes, and start replacing the placeholder abilities with real ones.
