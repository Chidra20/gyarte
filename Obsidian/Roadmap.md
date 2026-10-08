# Roadmap

Where the project goes next. The week-by-week record is in [[Weekly]]; back to [[Home]].

## Week 2 — Combat

**Goal:** the player can attack, things have health, and the slime can hurt the player.

### Done (2 Oct 2026, by yumisyumm — see [[Combat]])

- **Enemy health.** `EnemyHealth` on both slime prefabs, with a hit flash. It is slime-specific rather than the general `Health` first planned, and the player has none yet.
- **Player attack.** Two of them: a scythe swing and a fire spell with charges. Both are input actions that work on a gamepad (Square and Triangle, or X and Y) and on the keyboard. Each has one animation rather than one per direction.
- **Slime reactions.** A big slime at zero health splits, a baby dies, and a hit slime turns on the player. The split-and-merge fight is playable for the first time, so this is the point to judge whether it is fun.
- **Test menu.** A developer window, opened with U in `testing the new thing`, for spawning enemies and changing settings live. Every new feature is added to it from now on. See [[Test Menu]].

### Done (5 Oct 2026): the rest of combat, and the Demo game loop

The rest of the Week 2 plan was built as part of a bigger piece: a `Demo` scene that plays the whole loop ([[Game Loop]]).

- **Player health and death.** One general `Health` for the player and the enemies, with a short hurt immunity for the player. Death ends the run and goes back to the Start Menu ([[Combat]], [[Player]]).
- **Slime damage.** Contact damage on a timer, with knockback. Slimes no longer shove the player ([[Slime Enemy]]).
- **Attack limits and feedback.** The swing has a cooldown. A HUD corner shows both attacks, how long until each is ready and why not. The health number is on screen too ([[HUD]]).
- **The loop.** A random level, then waves, the key, the gate, and the next random level, forever. A choice of 1 of 3 abilities every second level. Spaced-out random enemy spawning ([[Game Loop]], [[Inventory and Abilities]]).
- **Scenes.** `Demo` for the game, `testing the new thing` for trying new things, `Test AI enemy` for AI, and `Start Menu`. Start loads `Demo`. Unused scenes were deleted ([[Scenes and Assets]]).
- **Tests.** EditMode unit tests in `Assets/Tests/Editor/`, run from the Test Runner window or through `TestRunLogger`.

### Done (7 Oct 2026): first playtest fixes

Slimes no longer lock on from anywhere; they're slower and get knocked back by hits, with the push strength adjustable. Rooms are bigger. Waves spawn only in the player's room, behind glowing warning markers. The key drops in the player's room. An FPS counter shows real frame rate with VSync off. See [[Game Loop]], [[Slime Enemy]], [[Combat]], [[HUD]].

### Done (7 Oct 2026): richer levels

The level randomizer uses the whole Catacombs pack:
- Floors by colour, with stamps.
- Pits and grates.
- A barred stairs-down exit.
- Themed rooms (crypt, storeroom, shrine, trap room) full of props, glowing torches and candles.
- Spike traps that hurt both the player and slimes.

Nothing can seal a room off; a flood fill checks every placement. See [[Decorations]] and [[Level Randomizer]].

### Done (8 Oct 2026, by yumisyumm): dash and polish

- **Dash** with dodging, a cooldown and afterimages ([[Player]]).
- **Health bar and stamina hearts** in the HUD, replacing the `HP` text ([[HUD]]).
- **Fire spell:** burns what it hits, has a cast-down animation, and moving cancels the cast once the fireball is out ([[Combat]]).
- **Faster swing.**
- **Visited rooms stay lit** and the darkness fades. Slimes in unexplored rooms can't see the player, and about half of each wave spawns in other rooms ([[Rooms and Darkness]], [[Game Loop]]).

### Still to do from Week 2

- **Fix the remaining combat bug** listed under Known issues: the swing hitting through walls.
- **Play it and tune.** Wave sizes, contact damage, hurt immunity, the swing cooldown and the merge window have only been checked to work, not to feel right.

### Decisions made by the first combat pass

- **Attack style:** both. A melee swing and a ranged, piercing fire spell limited by charges.
- **Health:** the big slime takes two hits, a baby one.
- **Merge pressure:** the babies now move slowly and can merge after about a second, down from three.

### Open decisions

- **Is the merge window right?** About a second is tight against a swing that takes almost half of that. Play it and tune.
- **Should the player be able to move while attacking?** They can today; casting has a switch to lock movement, swinging does not.
- **Where to test:** settled on 5 Oct 2026. `Demo` tests the game as a whole, `testing the new thing` tries out each new feature on its own, and `Test AI enemy` is for enemy behaviour ([[Scenes and Assets]]).
- **Is the loop the right size?** Two waves of three slimes on level 1, growing by a slime per wave each level; 10 health; a hit per second while touching a slime; abilities every second level. All starting guesses, adjustable from the [[Test Menu]].

## Next — growing the loop

- **More asset packs**, with each level built from a different one ([[Level Randomizer]]).
- **Real abilities** to replace the four stat-boost placeholders ([[Inventory and Abilities]]).
- **Rooms that react.** Enemies placed per room that wake when their room is entered; optionally doors that lock until the room is cleared ([[Rooms and Darkness]]).
- **An end to a run**, or a record of how far it went.
- **Art** for the key, the gate and the HUD.

## Later ideas

- More enemy types on a shared enemy base
- Visited rooms dimmed when the player isn't in them (they stay fully lit today), or real 2D lighting
- Level seeds, room roles (treasure, boss), loops between rooms
- Sound and music; none exist yet
- Menus, pause and save

## Known issues

| Issue | Where | Impact |
|---|---|---|
| Catacombs props import at 100 pixels per unit | [[Catacombs Asset Catalog]] | `decorative.png`, torches, candles and spikes would appear about six times too small; set them to 16 PPU, Point filter, no compression before using them |
| Exit room can be a trap room if the stairs don't fit | [[Decorations]] | Only when the stairs-down archway can't be placed (never seen in 75+ test levels); the trap-room rule then doesn't know which room the fallback gate is in |
| One-cell corridor beside the stairs | [[Level Randomizer]] | The stairs may stand one cell from a side wall; if a side doorway opens there, the way in is a narrow strip. Starting the stairs two cells from the walls would avoid it |
| Torches ignore the density slider | [[Decorations]] | Density scales theme decorations only; torches are 1–3 per room unless density is 0 |
| Bars draw over the player's head | [[Game Loop]] | Cosmetic: the stairs' bars are drawn above characters standing right in front of them |
| Thin dark lines on tile edges | [[Level Randomizer]] | Faint 1-pixel lines appear on some tile edges at regular screen intervals, more visible on the one-colour floors. Pixel art with point filtering at a camera size that doesn't map tiles onto whole pixels samples the empty pixel next to a tile on the sheet. Fix: a Pixel Perfect Camera (URP 2D) with the Cinemachine pixel-perfect extension, which changes the camera's framing slightly |
| Straight floor seam | [[Level Randomizer]] | Where a room's two floor patterns meet in a straight line, one pattern's mortar shows as a thin line across the room. Comes from the art; mixing patterns that share an edge would avoid it |
| Torch glow near walls | [[Decorations]] | A torch's glow can still peek slightly past the wall into the room behind; it was shrunk to keep this small |
| Low frame rate on weaker hardware | [[HUD]] | About 62 FPS on an RTX 2050 laptop plugged in, under 15 on battery (600 on an RTX 5060 Ti). Not optimised yet; to be looked at later. Measurements are kept in the [[HUD]] note |
| Melee hits through walls | [[Combat]] | The swing's hit circle has no wall check |
| Baby slimes move through walls | [[Slime Enemy]] | Now visible in play, since splitting works |
| Pathfinder keeps old walls after a level rebuild | [[Pathfinding]] | Blocks slimes that live through a rebuild. The [[Test Menu]] sidesteps it by removing enemies when it randomizes; the Randomize button on the canvas does not |
| Test menu is in the build | [[Test Menu]] | Nothing strips it from a released game yet |
| Stale `castKey` override in `Test AI enemy` | [[Combat]] | Left from before the attacks became input actions; it does nothing and can be removed from the Player's overrides in that scene |
| Stick input is always full speed | [[Player]] | Design choice to confirm |
| Unused `PlayerInput` component on the player | [[Player]] | None; cleanup |
| Escape was not tested with a real key press | [[Menus and Game Flow]] | The pause logic, and its blocking during the choice and death screens, were checked by calling them directly; the key itself needs one manual check |
| Choice screen not tried with a gamepad | [[Game Loop]] | The first card is selected on open, so stick and confirm should work; needs one manual check |
| A slime merge on the same frame as a level rebuild can survive into the new level | [[Game Loop]] | Rare (a one-frame window). The merged slime keeps its old position, possibly inside a new wall; if it can't be reached, wave 1 never clears. "Next level" in the [[Test Menu]] recovers |
| "Skip wave" in the Test Menu still waits the breather | [[Game Loop]] | The next wave comes after the usual pause instead of at once |
| Some Test Menu buttons bypass the loop in `Demo` | [[Test Menu]] | "Randomize level" rebuilds behind the loop's back (the key and gate stay where they were), "Gate in front" places a gate the loop doesn't listen to, "Start waves" restarts the waves, and a spare key from "Give a key" can open the next gate early. Fine as sandbox tools; they could be hidden in `Demo` |
| Test Menu actions under an open pause menu | [[Menus and Game Flow]] | Opening the ability choice or dying from the Test Menu while the pause menu is open leaves the pause panel up, and its Resume button unfreezes the death screen. Only reachable through the Test Menu |
| A single wall-free spot check for spawns | [[Game Loop]] | Enemies, the key and the gate are placed on floor cells with no wall in the 3x3 around them; fine for slimes and the player, but a bigger enemy would need a wider check |

## Cleanup when convenient

- Move scripts into `Assets/Scripts/`, rename `RoomsManager.cs` to match its class, fix the `Pallates` folder name. Do these inside Unity so references survive.
- Rename `EnemyHeatlh.cs` to `EnemyHealth.cs` inside Unity.
- Remove the unused health fields and `TakeDamage` from `BabySlime`, and its trigger callbacks that never fire.

## Tooling note

Claude Code talks to the editor through Unity's MCP server. Unity marks that server as deprecated in favour of its newer command-line interface, so the connection may need to be redone on a future package update.
