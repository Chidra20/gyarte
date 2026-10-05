# Demo Game Loop — Design

**Date:** 2026-10-05
**Status:** approved in conversation, awaiting spec review

## Goal

A `Demo` scene that plays the whole game loop end to end, so the separate systems built in Weeks 1–2 become one game:

> Start → random layout → survive the waves → find the key → open the gate → next random layout → repeat. Die → back to the Start Menu for a new run.

There is no end to the run yet; the loop repeats forever. Each level is meant to use different art later, but only the Catacombs pack exists, so every level uses it for now.

## Decisions made

| Question | Decision |
|---|---|
| Scene structure | One `Demo` scene that rebuilds itself in place for every level (approach A). The player object, its health and inventory persist across levels; nothing crosses a scene load except death → Start Menu. |
| Waves vs key | Survive all waves first; when the last wave is cleared the key appears somewhere in the level. |
| Where waves spawn | Random floor cells anywhere in the level, away from the player, spaced from each other, and capped per radius. |
| Ability choices | Every Nth level (default 2) the player picks 1 of 3. Four placeholder stat-boost abilities ship so the system is testable. |
| "Timer for the attacks" | Neither attack can be spammed. A HUD corner shows both attacks, a countdown until each is ready, and why it is blocked. |
| Player health | A number on the HUD. |
| Key art | A hollow red square. |

## Components

All scripts in `Assets/Prefabs/Scripts/` (enemy ones in `Assets/Prefabs/Enemies/Slime/Scripts/`), no namespaces, `[Header]`/`[Tooltip]` on public fields, Unity 6 API.

### Health (new, general)

- Fields: `maxHealth`, `invulnerableTime` (seconds of immunity after a hit; 0 for enemies).
- State: `CurrentHealth`, `IsDead`, `IsInvulnerable`, `GodMode` (for the Test Menu).
- Methods: `TakeDamage(int)`, `Heal(int)`, `SetMaxHealth(int, bool refill)`.
- Events: `Damaged`, `Healed` (or a single `Changed`), `Died`.
- Keeps the existing "can only die once" guard and sets health in `Awake`, not `Start` (a merged slime can be hit in its first frame).

### EnemyHealth (reworked)

- Requires and wraps `Health`. Keeps: the hit flash, `Alert()` on a non-lethal hit on a big slime, `DieAndSplit()` on a big slime's death, `Destroy` on anything else's death, and `SetMaxHealth()` for the Test Menu.
- Keeps its public `TakeDamage` signature so `PlayerAttack` and `FireProjectile` don't change.
- **Enemy registry:** a static set of living enemies. Joins in `OnEnable`, leaves in `OnDisable`/death. `WaveSpawner` reads `EnemyHealth.AliveCount`. Splits and merges are counted correctly because babies and merged slimes register themselves.
- The file `EnemyHeatlh.cs` keeps its name for now (renaming goes through Unity; it is already a cleanup item).

### Player damage

- `Health` on the Player prefab, with a short `invulnerableTime` and a sprite flash while invulnerable.
- `Died` → `GameLoop.OnPlayerDied()`.

### ContactDamage (new, on both slime prefabs)

- Fields: `damage`, `damageInterval`, `knockbackForce`, `knockbackTime`, `contactRadius`.
- Every frame it checks overlap with the player (the same way the babies check their merge, because kinematic bodies get no trigger callbacks). It deals `damage` at most once per `damageInterval` and only when the player's `Health` accepts it.
- Knockback: pushes the player away from the slime for `knockbackTime`; `PlayerMovement` gets `ApplyKnockback(Vector2 velocity, float time)` and does not steer while it lasts.
- The slime no longer shoves the player: the Player is put on its own `Player` layer, and the `Player`×`Enemy` pair is switched off in the 2D layer collision matrix. Overlap queries ignore the matrix, so the attacks' `Enemy`-layer checks and the contact check still work, and the slimes' wall pathing doesn't use physics contact at all. This fixes the known "chasing slime shoves the player" issue.

### PlayerAttack (changes)

- New `meleeCooldown` (seconds, must be at least `swingDuration`); the timer starts when the swing starts.
- New read-only properties for the HUD:
  - `MeleeCooldownRemaining`, `MeleeCooldownFraction`
  - `SecondsToNextCharge`
  - `MeleeBlockedReason` / `SpellBlockedReason`: `""` when ready, otherwise `"Cooldown"`, `"No charges"`, or `"Busy"` (the other attack is playing).
- Methods used by abilities: change `meleeCooldown`, `maxSpellCharges`.

### PlayerMovement (changes)

- `ApplyKnockback(Vector2, float)` as above.
- A `speedMultiplier` (default 1) used by the Fleet Foot ability.
- Freezes input while the game is over or the choice screen is up (both use time scale 0, which the movement script already respects).

### LevelRandomizer (changes)

- Exposes the last build: `Rooms` (list of room rects in cells), `StartRoomIndex`, door adjacency (to measure how many doors apart two rooms are).
- `TryGetRandomFloorPoint(int roomIndex, out Vector2 point)`: a walkable world point inside the room, away from walls and pillars.
- New `buildOnStart` toggle (default true, so `testing the new thing` is unchanged). In `Demo` it is false; `GameLoop` calls `Randomize()`.

### GameLoop (new, one per Demo scene)

States: `Starting → Fighting → KeyHunt → GateOpen → Choosing → (next level)`, plus `Dead`.

- `BeginLevel()`:
  1. Destroy leftover enemies, key and gate.
  2. `randomizer.Randomize()` (also moves the player to the start room).
  3. Place the gate in the room farthest from the start room, by door count, with distance as the tie-break.
  4. Show the "Level N" banner, then tell `WaveSpawner` to start.
- When the waves are done: spawn the key in a random room other than the player's, preferring far rooms. State → `KeyHunt`.
- Key picked up → `Inventory`. Touching the gate with a key → the key is used and the gate opens. State → `GateOpen`.
- Walking into the open gate: if `level % abilityEveryNLevels == 0`, open the choice screen (state `Choosing`), then `BeginLevel()` with level + 1.
- `OnPlayerDied()`: time scale 0, "You died" overlay for about 1.5 s of unscaled time, then reset the time scale and load `Start Menu`.
- Fields: `abilityEveryNLevels`, `levelBannerTime`, `deathScreenTime`, references to the randomizer, spawner, HUD, choice screen, key and gate prefabs, and the ability pool.

### WaveSpawner (new)

- Fields: `enemyPrefab` (BigSlime), `baseWaves`, `wavesPerLevel` (fractional, e.g. 0.5), `maxWaves`, `baseEnemiesPerWave`, `enemiesPerLevel`, `maxEnemiesPerWave`, `timeBetweenWaves`, `minPlayerDistance`, `minEnemySpacing`, `crowdRadius`, `maxPerRadius`, `spawnAttemptsPerEnemy`.
- Wave count = `min(maxWaves, baseWaves + floor(level * wavesPerLevel))`. Wave size = `min(maxEnemiesPerWave, baseEnemiesPerWave + level * enemiesPerLevel)`.
- Spawn point: a random room, then `TryGetRandomFloorPoint`, rejected if it is closer than `minPlayerDistance` to the player, closer than `minEnemySpacing` to a living enemy, or if `crowdRadius` already holds `maxPerRadius` enemies. After `spawnAttemptsPerEnemy` failures that enemy is skipped; a wave may come up short.
- Spawned slimes call `Alert()` so they chase immediately.
- A wave is cleared when `EnemyHealth.AliveCount == 0` (babies included). After `timeBetweenWaves` the next wave starts; after the last wave, `AllWavesCleared` fires.
- Exposes `CurrentWave`, `TotalWaves`, and `SkipWave()` / `KillAll()` for the Test Menu.

### KeyPickup and Gate (new prefabs)

- **Key:** a trigger with a hollow red square sprite (a generated outline texture or a 1-px-border sprite asset). On player touch: `Inventory.AddKey()`, then destroy itself.
- **Gate:** a trigger with a closed and an open look (placeholder: a tinted rectangle, or a Catacombs door tile if one is suitable). Touching it while closed with a key: use the key and open. Touching it while open: `GameLoop.OnGateEntered()`. When closed without a key, the HUD hints "Need a key".
- Both sort above the floor and below the darkness (order 0), so a dark room hides them until it is entered.

### Inventory (new, on the player)

- `Keys` (int), `AddKey()`, `TryUseKey()`.
- `Abilities` (list of `Ability` with stack counts), `AddAbility(Ability)`: adds it and calls `ability.Apply(player)`. `StackCount(Ability)`.
- `Changed` event for the HUD.

### Ability (ScriptableObject, abstract)

- `displayName`, `description`, `maxStacks`, `abstract void Apply(GameObject player)`.
- Placeholders, one small script and one asset each, in `Assets/Prefabs/Abilities/`:
  - **Vitality:** +2 max health and heal 2.
  - **Quick Scythe:** `meleeCooldown` ×0.8.
  - **Deep Reserves:** `maxSpellCharges` +1 (and +1 current charge).
  - **Fleet Foot:** `speedMultiplier` +0.15.

### AbilityChoiceScreen (new UI)

- On open: time scale 0; draws 3 different abilities from the pool, skipping any at `maxStacks` (fewer cards if the pool runs short; if none are left, the screen is skipped). The first card is selected for gamepad.
- Card: name and description, as a button. Picking one → `Inventory.AddAbility`, close, time scale 1, and a callback to `GameLoop`.
- `PauseMenu` ignores Escape while the choice screen or the death screen is open.

### HUD (new UI on the Demo canvas)

- **Top-left:** `HP 7/10`.
- **Top-centre:** `Level 3 · Wave 2/4` and an objective line: "Survive", "Next wave in 2s", "Find the key", "Go to the gate", "Need a key".
- **Top-right:** inventory strip with a key icon and count when held, and the collected ability names (×N for stacks).
- **Bottom-left, attack corner:** two slots, **Swing** and **Fire**. Each has a dark overlay that fills down with the remaining cooldown, the seconds left, and a reason line when blocked. Fire shows charge dots.
- **Overlays:** the level banner, "You died".
- Plain UGUI with the default font, matching the existing menus.

### Test Menu (new "Demo" section, shown only when a `GameLoop` exists)

- Kill all enemies, skip the wave, spawn the key at the player, open the gate, next level.
- God mode, set player health, set player max health.
- Offer the ability choice now; add a specific ability.
- Fields for the wave counts and size, spawn spacing, crowd limits, and the slime's contact damage, interval and knockback.

### Scenes and menu

- `Assets/Scenes/Demo.unity`: a copy of `testing the new thing` (via Unity, so references survive) with the Randomize button removed, `buildOnStart` off, and `GameLoop`, `WaveSpawner`, the HUD and the choice screen added.
- Added to Build Settings. `MainMenu` Start loads `Demo`.
- **Scene roles:** `Demo` is where the actual game is tested: the full loop as a player would play it. `testing the new thing` is the sandbox where every new feature is first tried out on its own through the Test Menu, to make sure it works. So the per-feature pieces (player health, contact damage, attack cooldowns, the attack corner) must also work in `testing the new thing`, without a `GameLoop`.
- `testing the new thing` and `Test AI enemy` stay as system test scenes. They have no `GameLoop`, so player death there does nothing beyond health reaching 0 (no scene change).

## Error handling and edge cases

- **Spawning fails** (tiny level, crowd limits): the wave is spawned short; a wave with 0 spawned counts as cleared at once.
- **The key can't be placed in another room** (one-room level): it goes in the current room, away from the player.
- **Player dies on the frame the gate opens** or while choosing: death wins, so `Dead` overrides every other state.
- **Enemies alive during a rebuild:** always destroyed first, which avoids the pathfinder's stale-wall issue.
- **Merging babies** never let the alive count reach 0 by mistake: the merged slime registers in `Awake` before the babies are destroyed.
- **Time scale** is always reset to 1 when leaving the scene (the existing `PauseMenu` pattern).

## Vault updates (same piece of work)

- New system notes: `Systems/Game Loop.md` (GameLoop, waves, key, gate), `Systems/Inventory and Abilities.md`, `Systems/HUD.md`.
- Update: `Combat.md` (Health, cooldowns, contact damage), `Slime Enemy.md` (contact damage, no more shoving), `Player.md` (health, knockback, speed multiplier), `Level Randomizer.md` (room data, `buildOnStart`), `Menus and Game Flow.md` (Start → Demo, death flow), `Test Menu.md`, `Scenes and Assets.md`, `Architecture.md` (diagram and gaps), `Roadmap.md`, `Home.md`.

## Verification

In Unity via MCP: compile with a clean console, check for missing scripts, a capture of the Demo scene, then a Play-mode run driven through the Test Menu methods:

1. The level builds, the gate is placed, wave 1 spawns with the spacing respected.
2. Kill all → the next wave → after the last, the key spawns in another room.
3. Spawn the key at the player → in the inventory → open the gate → enter it → level 2 has a different layout.
4. The choice screen appears after level 2, a pick applies its effect.
5. HUD: the swing cooldown and spell recharge count down, and the reasons show.
6. Slime contact drains health on the interval with knockback → at 0 → "You died" → Start Menu.

Manual checks for the user: gamepad selection on the choice screen and the feel of the cooldowns and contact damage.

## Out of scope

Different art per level, a run end or win, saving, sound, new enemy types, slime room-awareness, melee wall check, baby slime wall pathing.
