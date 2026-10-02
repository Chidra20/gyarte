# Roadmap

Where the project goes next. The week-by-week record is in [[Weekly]]; back to [[Home]].

## Week 2 — Combat

**Goal:** the player can attack, things have health, and the slime can hurt the player.

### Done (2 Oct 2026, by yumisyumm — see [[Combat]])

- **Enemy health.** `EnemyHealth` on both slime prefabs, with a hit flash. It is slime-specific rather than the general `Health` first planned, and the player has none yet.
- **Player attack.** Two of them: a scythe swing and a fire spell with charges. Both are input actions that work on a gamepad (Square and Triangle, or X and Y) and on the keyboard. Each has one animation rather than one per direction.
- **Slime reactions.** A big slime at zero health splits, a baby dies, and a hit slime turns on the player. The split-and-merge fight is playable for the first time, so this is the point to judge whether it is fun.
- **Test menu.** A developer window, opened with U in `testing the new thing`, for spawning enemies and changing settings live. Every new feature is added to it from now on. See [[Test Menu]].

### Still to do

1. **Slime damage.** When the chase reaches the player, deal contact damage on a timer, with a brief knockback so the player is not pinned. Today the slime pushes the player around instead ([[Slime Enemy]]).
2. **Player health.** Either generalise `EnemyHealth` into one `Health` with a death event, or give the player its own.
3. **Player death.** The simplest version first: reload the scene, or reuse the pause screen's End Game path back to the start menu ([[Menus and Game Flow]]).
4. **Feedback.** A health display and a spell-charge display on the existing canvas (`PlayerAttack` already exposes the charge numbers), and a short invulnerability window after being hit.
5. **Fix the remaining combat bug** listed under Known issues: the swing hitting through walls.

Each of these gets its controls in the [[Test Menu]] as it is built: enemy damage, player health, and so on.

### Decisions made by the first combat pass

- **Attack style:** both. A melee swing and a ranged, piercing fire spell limited by charges.
- **Health:** the big slime takes two hits, a baby one.
- **Merge pressure:** the babies now move slowly and can merge after about a second, down from three.

### Open decisions

- **Is the merge window right?** About a second is tight against a swing that takes almost half of that. Play it and tune.
- **Should the player be able to move while attacking?** They can today; casting has a switch to lock movement, swinging does not.
- **Where to test:** slimes can now be spawned into a generated level from the [[Test Menu]], which makes `testing the new thing` usable for combat as well as `Test AI enemy`. Enemies placed by the level itself still need the bridge work below.

## After combat — connecting the systems

These turn the separate tests into one game. Each is described in its system note.

- **Enemies in generated levels.** Spawn slimes per room from the [[Level Randomizer]], and give the [[Pathfinding]] a way to forget old walls.
- **Rooms that react.** Enemies wake when their room is entered; optionally doors lock until the room is cleared ([[Rooms and Darkness]]).
- **A goal.** An exit room and a next level, so there is a reason to cross the dungeon.
- **A real game scene.** Generate a new level on load and drop the test button.

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
| Nothing can hurt the player | everywhere | The rest of the Week 2 goal |
| Melee hits through walls | [[Combat]] | The swing's hit circle has no wall check |
| Chasing slime shoves the player | [[Slime Enemy]] | The player is carried along instead of the slime stopping; goes away with contact damage and knockback |
| Baby slimes move through walls | [[Slime Enemy]] | Now visible in play, since splitting works |
| Pathfinder keeps old walls after a level rebuild | [[Pathfinding]] | Blocks slimes that live through a rebuild. The [[Test Menu]] sidesteps it by removing enemies when it randomizes; the Randomize button on the canvas does not |
| Test menu is in the build | [[Test Menu]] | Nothing strips it from a released game yet |
| Stale `castKey` override in `Test AI enemy` | [[Combat]] | Left from before the attacks became input actions; it does nothing and can be removed from the Player's overrides in that scene |
| Stick input is always full speed | [[Player]] | Design choice to confirm |
| Unused `PlayerInput` component on the player | [[Player]] | None; cleanup |
| Escape was not tested with a real key press | [[Menus and Game Flow]] | The pause logic was verified by calling it directly; the key itself needs one manual check |
| Every run starts in the same saved level | [[Menus and Game Flow]] | Start loads the saved layout until Randomize is pressed |

## Cleanup when convenient

- Move scripts into `Assets/Scripts/`, rename `RoomsManager.cs` to match its class, fix the `Pallates` folder name. Do these inside Unity so references survive.
- Rename `EnemyHeatlh.cs` to `EnemyHealth.cs` inside Unity.
- Remove the unused health fields and `TakeDamage` from `BabySlime`, and its trigger callbacks that never fire.
- Remove the `Debug.Log` on every hit in `EnemyHealth` once combat is settled.
- Delete the duplicate `New Scene` files.

## Tooling note

Claude Code talks to the editor through Unity's MCP server. Unity marks that server as deprecated in favour of its newer command-line interface, so the connection may need to be redone on a future package update.
