# Combat

The player's two attacks and the health that enemies lose to them. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/PlayerAttack.cs`, `Assets/Prefabs/Scripts/FireProjectile.cs`, `Assets/Prefabs/FireProjectile.prefab`, `Assets/Prefabs/Enemies/Slime/Scripts/EnemyHeatlh.cs` (class `EnemyHealth`; the file name is misspelled), `Assets/Animations/Swing.anim`, `Assets/Animations/FireSpell.anim`, art in `Assets/Art/Attacks/`

Added on 2 Oct 2026 by yumisyumm; the same day the attacks were moved onto the input actions and two bugs in `EnemyHealth` were fixed. Since 5 Oct 2026 damage goes both ways: slimes hurt the player by touching them ([[Slime Enemy]]).

## The two attacks

`PlayerAttack` sits on the [[Player]] root next to `PlayerMovement`. Both attacks aim along the direction the player is facing, and only one can run at a time.

| Action | PlayStation | Xbox | Keyboard | What it does |
|---|---|---|---|---|
| `Attack` | Square | X | J | Scythe swing |
| `Projectile` | Triangle | Y | Space | Fire spell |

**Scythe swing (melee).** Pressing the attack button plays the `Swing` animation, unless the swing is still on **cooldown**. The cooldown starts when a swing starts and is a little longer than the animation, so the swing cannot be spammed. Part-way through, at the moment the scythe is in front of the character, everything on the `Enemy` layer inside a circle in front of the player takes damage. The circle follows an `Attack` object that the script creates and keeps in front of the player every frame. The swing cannot be repeated until the animation has finished.

**Fire spell (ranged).** Pressing the projectile button plays the `FireSpell` animation and, on the frame the fire leaves the hand, spawns a `FireProjectile`. The spell uses charges: a small number can be stored, each cast spends one, and they come back one at a time on a timer. The attack corner of the [[HUD]] shows the charges and the time until the next one.

**Why an attack is blocked.** `PlayerAttack` names the reason an attack can't be used right now: the other attack is still playing ("Busy"), the swing is on "Cooldown", or the spell has "No charges". The [[HUD]] shows these reasons. Abilities can change the cooldown and the number of charges ([[Inventory and Abilities]]).

While either attack is playing, `PlayerMovement` keeps the facing direction fixed and holds the run animations off so the attack animation is not interrupted. The player can still move during an attack unless `lockMovementWhileCasting` is turned on.

**Input.** Both attacks are actions in `PlayerInputActions`, next to `Move`, and `PlayerAttack` holds a reference to each, the same way `PlayerMovement` holds one to `Move`. The gamepad bindings are written by button position (`buttonWest`, `buttonNorth`) rather than by a controller's own names, which is why the same binding is Square on a PlayStation pad and X on an Xbox pad. The first version read raw keyboard keys through the old input system; that is gone.

A debug circle showing the melee hitbox can be switched on with `showHitbox`. It flashes when a swing lands its hit check.

Damage, swing range, the hitbox display and the spell's charges can all be changed live from the [[Test Menu]].

## The fireball

`FireProjectile` animates itself from three lists of sprites rather than an Animator: a short launch burst, a looping flight, and a fade.

- It flies in a straight line and **stops at anything tagged `wall`**, the same contract the slime uses ([[Architecture]]).
- It **pierces enemies**: every enemy it passes through is damaged once and the bolt keeps going until it hits a wall or reaches its maximum range.
- It only damages **enemies that existed when it was cast**. Baby slimes that appear mid-flight are ignored, so one fireball cannot kill a big slime and then its babies in the same pass.
- It carries a small 2D light as a glow. Scenes without any 2D light render fully lit, and adding the first light would turn everything else dark, so the projectile creates a plain white global light the first time one is launched in a scene that has none.

## Health

**`Health`** (`Assets/Prefabs/Scripts/Health.cs`, added 5 Oct 2026) is the one health component for everything that can be hurt. It only counts and reports. It holds max and current health and an optional invulnerability window after each hit, and it raises events when health changes, when a hit lands and when the owner dies. What a hit or a death *means* is left to whoever listens. A god-mode switch for the [[Test Menu]] makes it ignore hits.

Two details protect it from same-frame trouble:

- **It can only die once.** Destroying an object takes effect at the end of the frame, so two lethal hits landing together (a fireball and a swing) used to make a big slime split twice, into four babies. After the fatal hit, every further hit is ignored.
- **Health is set as soon as the object exists**, not on its first frame. A slime created by a merge can be hit in the very frame it appears.

## Enemy health

`EnemyHealth` sits next to `Health` on both slime prefabs (it adds `Health` automatically). The health amount itself now lives on `Health`. `EnemyHealth` is the enemy's reaction to it:

- A non-lethal hit flashes the sprite, and on a big slime it calls `BigSlime.Alert()`, so a slime hit from behind turns and chases ([[Slime Enemy]]).
- At zero health, a **big slime** calls `BigSlime.DieAndSplit()`; anything else is destroyed. This is how baby slimes die.
- The attacks still call `EnemyHealth.TakeDamage`, which passes the hit on to `Health`.

`SetMaxHealth()` gives a single enemy a different amount of health from its prefab. The [[Test Menu]] uses it when spawning.

**The list of living enemies.** (The design said enemies would join and leave the list when switched on and off. They join when created and leave when destroyed instead, so a switched-off enemy would still count. Nothing switches enemies off today.) `EnemyHealth` keeps a shared count of every enemy that is alive, babies included. The wave spawner uses it to tell when a wave is cleared ([[Roadmap]]), and the [[Test Menu]] shows it. The order matters: an enemy joins the list as soon as it exists, and a dying big slime leaves it only *after* its babies have joined. A two-babies-merge likewise creates the new big slime before the babies are removed. So a split or a merge never makes the count touch zero, which would otherwise look like a cleared wave. Enemies destroyed without the usual callbacks are dropped from the list the next time it is read.

## Verified in Play mode (2 Oct 2026, `Test AI enemy`)

- Two swings kill the big slime, it splits, and the babies merge back into a new big slime.
- A fireball damages a slime in its path and pierces on to a second one.
- The automatic global light appears on the first cast.
- A simulated gamepad pressing the west button swings and the north button casts. Mamuka then confirmed the attacks by hand.
- Two lethal hits in one frame give two babies, and a slime hit in the frame it spawns keeps its health.

Not tested: the fireball stopping at a wall.

## Known problems

- **Melee ignores walls.** The swing's circle has no wall check, so an enemy on the other side of a thin wall can be hit.
- **Leftover baby health.** `BabySlime` still has its own `maxHealth` and `TakeDamage`, now unused; `EnemyHealth` is what the attacks call.

## Links to other systems

- Reads the facing direction from the [[Player]] and freezes it during attacks.
- Finds enemies by the `Enemy` layer and damages them through `EnemyHealth` ([[Slime Enemy]]).
- The fireball relies on the `wall` tag, like the slime's sight and [[Pathfinding]].
- Pausing stops both attacks: the script ignores the buttons while the time scale is 0 ([[Menus and Game Flow]]).
- Every setting here is adjustable from the [[Test Menu]].

## How it can progress

- Turn `EnemyHealth` into a general `Health` with a death event, so the player and future enemies can share it.
