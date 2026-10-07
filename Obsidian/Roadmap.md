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
- Visited rooms shown dimmed, or real 2D lighting
- Decoration pass with torches, candles and spikes from the art pack
- Level seeds, room roles (treasure, boss), loops between rooms
- Sound and music; none exist yet
- Menus, pause and save

## Known issues

| Issue | Where | Impact |
|---|---|---|
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
