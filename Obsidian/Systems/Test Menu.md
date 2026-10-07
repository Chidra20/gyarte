# Test Menu

A developer window for trying out everything the game has, live, without leaving Play mode. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/TestMenu.cs`, on the `Test Menu` object in `testing the new thing` and in `Demo`

Added on 2 Oct 2026.

## The rule

**Every new thing gets a section in this menu.** When a system, enemy, attack or setting is added to the game, its controls are added here in the same piece of work. The menu is how features are tried out, so a feature that is not in it is not finished. The rule is also written in `CLAUDE.md`.

## What it is

Pressing the toggle key (U by default) shows or hides a window on the right side of the screen. The key is a public field on the component, `toggleKey`, so it can be changed in the Inspector.

It is separate from the pause menu ([[Menus and Game Flow]]) in two ways: it is opened by its own key, and **it does not pause the game**. The point is to change something and watch what happens. Both menus can be open at once.

The window can be dragged by its title bar and scrolls when it is taller than the screen. A line at the bottom reports what the last action did, or why it did nothing.

## What is in it

Sections only appear when the scene has what they control. The game loop and waves sections, for example, only appear in `Demo`.

### Game loop

Only in a scene with a `GameLoop` ([[Game Loop]]). It shows the level and state, and has buttons to kill all enemies, skip the wave, put the key next to the player, open the gate, jump to the gate, go to the next level, and open the ability choice right now (the level carries on afterwards). A slider sets how often abilities are offered.

### Enemies

- **Which enemy.** One button per entry in the `spawnables` list on the component. Today that is the big slime and the baby slime ([[Slime Enemy]]). A new enemy type is added by putting its prefab in that list; no code change is needed for it to be spawnable.
- **Health.** The health the spawned enemy starts with. The slider jumps to the prefab's normal health when an enemy is picked.
- **How many**, and **distance in front** of the player.
- **Spawn in front of player.** Uses the direction the player is facing.
- **Left click on the map.** A three-way switch: do nothing, spawn the chosen enemy where the click lands, or move the player there. This is how the spawn position is chosen freely.
- **Freeze enemies.** Switches the slime scripts off so they stand still.
- **Hit all once** deals one hit of the player's attack damage to every enemy. **Remove all** deletes every enemy without big slimes splitting.

An enemy is never spawned inside a wall; the status line says so instead. Clicks on the window itself or on the normal UI do not count as clicks on the map.

**Touching the player:** contact damage, seconds between hits and knockback speed ([[Slime Enemy]]). Changes apply to every living enemy at once and to every enemy spawned from the menu afterwards. They don't change the prefab, so enemies created by a split or a merge use the prefab's values.

The "alive" count in the header is the same list of living enemies the waves use ([[Combat]]).

### Player

Health (current and max, shown and adjustable; setting it can never kill), hurt immunity seconds, god mode, heal to full, take 1 damage. Then move speed, attack damage, swing and fireball knockback (used by both the swing and the fireball), swing range, swing cooldown, a switch to show the swing's hitbox, the fire spell's maximum charges and recharge time, unlimited fire spell, standing still while casting, and a button to refill the spell. See [[Player]] and [[Combat]].

### Inventory

Keys held, with buttons to give one or to place a key or a gate in front of the player, and the abilities held with their stacks. Every ability in the menu's ability pool has a button that adds it straight away; it is greyed out once at its maximum. See [[Inventory and Abilities]].

### Waves

Only shown in a scene with a `WaveSpawner`. It shows the current wave and the time to the next. There are buttons to start the waves for any level number, skip the current wave, or kill every enemy, and sliders for every wave setting: counts, growth per level, the breather, how long the spawn markers glow before enemies appear, and the spawn spacing rules. See [[Game Loop]].

### Level

- **Randomize level** builds a new level through the [[Level Randomizer]]. It removes all enemies first, because old enemies could end up inside the new walls and their pathfinders still remember the old ones ([[Pathfinding]]).
- **Decorations** (only with a level style): which theme the current room has (and whether it is the exit), decorations on or off and a density multiplier for the next build, and spike trap hidden and out times and damage, applied to every trap in the level at once ([[Decorations]]).
- **Show all rooms** turns the darkness off so the whole level and everything in it can be seen ([[Rooms and Darkness]]). It works by switching `RoomManager` off and hiding every overlay; turning it back on restores the normal one-room view.

### Game

Show or hide the FPS counter, switch the frame rate cap (VSync) on or off ([[HUD]]), game speed, back to normal speed, and restart the scene. The speed slider is replaced by "Paused" while the pause menu has the game frozen, and resuming from the pause menu sets the speed back to normal.

## How it works

- The window is drawn with Unity's immediate-mode GUI, in code, rather than built from UI objects in the scene. For a developer tool this keeps everything in one script: adding a control is a line of code, and there is nothing to lay out or wire up in the scene.
- It is drawn as if the screen were a fixed height and scaled to the real one, so it is the same size at any resolution. `referenceHeight` on the component controls how big it appears.
- Almost every control reads and writes the real value on the real component each frame: the move speed slider *is* `PlayerMovement.moveSpeed`. Nothing is copied, so the menu always shows the truth and changes apply at once. Changes last until Play mode stops.
- The three switches that have to keep being applied (frozen enemies, unlimited spell, no darkness) keep working while the window is hidden, so a setup survives closing the menu.
- It finds the player, the level randomizer and the room manager by itself when they are not set, and hides the sections that have nothing to control. It can be dropped into any scene.

Two small methods were added to other scripts for it: `EnemyHealth.SetMaxHealth()`, which gives one enemy different health from its prefab, and `PlayerAttack.RefillSpellCharges()`.

## Adding a new thing

1. If it is a new enemy, add its prefab to `spawnables` on the `Test Menu` object. That is all spawning needs.
2. For anything else, add a `Draw…()` method to `TestMenu.cs` with the controls and call it from `DrawWindow()`. The existing sections and the two slider helpers show the pattern.
3. Update this note.

## Limits

- **Keyboard and mouse only.** The game favours the gamepad, but this is a developer tool; it has no gamepad control.
- **Only in `testing the new thing`.** Other scenes need a `Test Menu` object added.
- **Spawned slimes' children use prefab health.** The health chosen for a big slime does not carry over to its babies or to the slime they merge back into.
- **Freeze only knows slimes.** A new enemy type has to be added to it.
- **It ships with the game.** Nothing strips it from a build yet; before a real release it should be removed or limited to development builds.

## Verified in Play mode (2 Oct 2026)

A simulated U key press opened the menu. Spawning in front of the player and at a chosen point, the wall check, a five-health slime surviving four hits, removing all enemies, the darkness switch both ways, and moving the player were each run and checked. The sliders, the mouse clicks on the map, and the buttons for randomize, speed and restart were not operated by hand from the terminal.

## Links to other systems

- Spawns and controls the [[Slime Enemy]] and changes health through [[Combat]]'s `EnemyHealth`.
- Changes the [[Player]]'s movement and attack settings.
- Calls the [[Level Randomizer]] and switches [[Rooms and Darkness]] off and on.
- Relies on the `wall` tag for its spawn check, like the rest of the game ([[Architecture]]).
