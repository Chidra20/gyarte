# Player

Movement, facing, animation and input. Attacks are in [[Combat]]. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Player.prefab`, `Assets/Prefabs/Scripts/PlayerMovement.cs`, `Assets/Prefabs/Scripts/PlayerDash.cs`, `Assets/Prefabs/Scripts/DashGhost.cs`, `Assets/Animations/PlayerAnim.controller`, `Assets/Animations/Dash_Side.anim` / `Dash_Down.anim` / `Dash_Up.anim`, art `Assets/Art/SidewaysDash.aseprite` / `DownwardDash.aseprite` / `UpwardDash.aseprite`, `Assets/PlayerInputActions.inputactions`

## The prefab

```
Player            tag: Player — Rigidbody2D, BoxCollider2D, PlayerMovement, PlayerAttack, PlayerDash, PlayerInput
├── visual        SpriteRenderer + Animator (the character art)
└── facing        small red marker, kept one unit in front of the player
```

At runtime `PlayerAttack` adds a third child, `Attack`, which marks the centre of the melee hitbox.

- The body is dynamic with gravity off and rotation frozen, so walls stop it through normal physics.
- The art sits on the `visual` child so it can be flipped without flipping the collider.
- The root also has its own `SpriteRenderer`, which is disabled in the prefab so only `visual` draws.
- `PlayerMovement` has its `visual` and `facingObject` references set in the prefab, so a fresh instance works in any scene without extra wiring.

## How movement works

1. `Update` reads the `Move` action (WASD or left stick) as a 2D vector and normalizes it.
2. The dominant axis picks one of four facing directions. Facing only changes while there is input, so the player keeps looking the way they last moved. It also does not change while an attack is playing, so a swing or cast finishes in the direction it started.
3. `FixedUpdate` sets the body's velocity to direction × `moveSpeed` × `speedMultiplier`. The multiplier is 1 normally; abilities raise it ([[Inventory and Abilities]]).

**Knockback.** `ApplyKnockback` pushes the player at a set velocity for a short time. Meanwhile `FixedUpdate` ignores the stick, because otherwise the next physics step would overwrite the push. Enemies use it when they hit the player ([[Slime Enemy]]).

`FacingVector` exposes the facing direction to other scripts; [[Combat]] uses it to aim. `IsMoving` and `MoveInput` expose the stick direction; the dash and the cast use them.

Move speed can be changed while playing from the [[Test Menu]].

Because the input is normalized, speed is always full or zero. A slight stick tilt moves at full speed.

While the game is paused (time scale 0) `Update` returns early, so the character keeps its pose instead of turning on the spot. See [[Menus and Game Flow]].

## Dash

Added on 8 Oct 2026. `PlayerDash` is on the prefab next to `PlayerMovement`.

| Action | PlayStation | Xbox | Keyboard |
|---|---|---|---|
| `Dash` | Circle | B | K |

- **Direction:** the way the player is moving, or the way they face when standing still.
- **Movement:** `dashDistance` 3.75 units over `dashTime` 0.2 s. It reuses `ApplyKnockback`, so steering is ignored for the dash and walls still stop it.
- **Dodging:** while dashing, `Health.Dodging` is on and hits are ignored, including the knockback that comes with them. `Dodging` is separate from god mode so the [[Test Menu]] switch survives a dash.
- **Cooldown:** 0.6 s from the start of one dash to the next.
- **Afterimages:** every 0.035 s a fading copy of the current frame (`DashGhost`) is left behind.
- **Blocked:** no dash in the middle of a swing, or during a cast before the fireball is out; no attacks while dashing. The reason is shown in the [[HUD]] corner.

The dash art is three 3-frame `.aseprite` files on the same 93×150 canvas as the other sheets. They import at 30 pixels per unit with the pivot on the body centre, so the character doesn't jump when the dash starts.

## Health

The player has a `Health` component, the same one enemies use ([[Combat]]). It has a short **hurt immunity** after each hit: another hit inside that window does not count. While it lasts, `PlayerHurtFlash` blinks the sprite so this is visible. The amount of health and the length of the window are set on the prefab and can be changed from the [[Test Menu]], which also has god mode.

What happens at zero health is up to the scene. In the test scenes nothing happens; the player just stays at zero. In the game loop it ends the run ([[Roadmap]]).

**The `Player` layer.** The player is on its own physics layer, and that layer does not collide with the `Enemy` layer. Enemies therefore pass through the player instead of pushing it, and damage comes from the enemies' own overlap checks rather than from collisions.

## How animation works

The script sets four animator parameters every frame:

| Parameter | Meaning |
|---|---|
| `Direction` | 0 down, 1 up, 3 right |
| `IsRunning` | moving mostly sideways |
| `RunningUp` | moving mostly up |
| `RunningDown` | moving mostly down |

There is no left-facing art. Facing left sends `Direction` 3 (right) and mirrors `visual` by flipping its X scale.

The controller has three idles and three runs. "Any State" transitions jump into the right run state, and each run state falls back to its matching idle when its flag clears.

More states are entered from "Any State" by triggers and return to the idle for the current direction when they finish:

- `Swing` (trigger `Swing`): one clip for all directions.
- `FireSpell` and `FireSpell_Down` (trigger `Attack`): the down version plays when `Direction` is 0.
- `Dash_Side`, `Dash_Down`, `Dash_Up` (trigger `Dash`): picked by `Direction`.

While an attack or a dash is playing the three run flags are held off so a run state cannot cut it short. A cast only holds them until the fireball is out ([[Combat]]).

## The facing marker

`facing` is moved to `player position + facing direction × facingObjectDistance` every frame. It still does nothing. It was meant as the origin for attacks, but `PlayerAttack` keeps its own `Attack` point instead, so the marker is a leftover that one of the two could replace.

## Links to other systems

- [[Rooms and Darkness]] reads the player's position to decide which room is lit.
- The [[Slime Enemy]] finds the player by tag and uses its position for sight and chasing.
- The [[Level Randomizer]] teleports the player to the start room after building a level.
- The Cinemachine camera in each scene has the player as its tracking target.

## How it can progress

- **Attack animations per direction**: casting down is done; casting up and swinging up/down still use the side clips.
- **Analog speed** by dropping the normalization for stick input, if walking slowly should be possible.
- **More actions** (interact) as extra entries in the input actions asset, the way `Dash` was added.
- A stamina cost for the dash, if it should share the hearts with the fire spell.
- The `PlayerInput` component on the prefab is unused; the script reads the action directly. Remove it or switch to it, but not both.
