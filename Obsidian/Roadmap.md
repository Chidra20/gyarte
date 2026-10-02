# Roadmap

Where the project goes next. The week-by-week record is in [[Weekly]]; back to [[Home]].

## Week 2 — Combat

**Goal:** the player can attack, things have health, and the slime can hurt the player.

### The pieces

1. **Health component.** One script for anything that can be hurt: current and max health, a `TakeDamage` method, a short hit flash, and an event when health reaches zero. This is the `EnemyHealth` script the baby slime prefab already expects; building it as a general `Health` covers the player too.
2. **Player attack.** An attack input action. On press, hit everything on the `Enemy` layer in a small area at the `facing` marker ([[Player]]), with a cooldown and an attack animation per direction.
3. **Slime reactions.** Big slime at zero health calls its existing `DieAndSplit()`. Baby slime at zero health dies. Both already have the code; they only need to be connected to the health event ([[Slime Enemy]]).
4. **Slime damage.** When the chase reaches the player, deal contact damage on a timer, with a brief knockback so the player is not pinned.
5. **Player death.** The simplest version first: reload the scene, or reuse the pause screen's End Game path back to the start menu ([[Menus and Game Flow]]).
6. **Feedback.** A health display on the existing canvas, the hit flash, and a short invulnerability window after being hit.

### Suggested order

Health → player attack → slime reactions → slime damage → player death → feedback. After step 3 the split-and-merge fight is playable for the first time, which is the earliest point to judge whether it is fun.

### Open decisions

- **Attack style:** a quick melee swing in front of the player, or something with range?
- **Baby health:** the prefab says 1. Should the big slime take several hits?
- **Merge pressure:** is 3 seconds before babies can re-merge the right window once the player can actually fight them?
- **Where to test:** `Test AI enemy` is the simplest scene for combat; moving to generated levels needs the bridge work below.

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
| Missing `EnemyHealth` script on `BabySlime.prefab` | [[Slime Enemy]] | Warning in the editor; resolved by the Week 2 health component |
| Nothing can deal damage | everywhere | The Week 2 goal |
| Baby slimes move through walls | [[Slime Enemy]] | Visible once splitting works |
| Pathfinder keeps old walls after a level rebuild | [[Pathfinding]] | Blocks slimes in generated levels |
| Stick input is always full speed | [[Player]] | Design choice to confirm |
| Unused `PlayerInput` component on the player | [[Player]] | None; cleanup |
| Escape was not tested with a real key press | [[Menus and Game Flow]] | The pause logic was verified by calling it directly; the key itself needs one manual check |
| Every run starts in the same saved level | [[Menus and Game Flow]] | Start loads the saved layout until Randomize is pressed |

## Cleanup when convenient

- Move scripts into `Assets/Scripts/`, rename `RoomsManager.cs` to match its class, fix the `Pallates` folder name. Do these inside Unity so references survive.
- Delete the duplicate `New Scene` files.
- Commit the Week 1 randomizer and menu work.

## Tooling note

Claude Code talks to the editor through Unity's MCP server. Unity marks that server as deprecated in favour of its newer command-line interface, so the connection may need to be redone on a future package update.
