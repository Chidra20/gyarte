# Player

Movement, facing, animation and input. Attacks are in [[Combat]]. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Player.prefab`, `Assets/Prefabs/Scripts/PlayerMovement.cs`, `Assets/Animations/PlayerAnim.controller`, `Assets/PlayerInputActions.inputactions`

## The prefab

```
Player            tag: Player — Rigidbody2D, BoxCollider2D, PlayerMovement, PlayerAttack, PlayerInput
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
3. `FixedUpdate` sets the body's velocity to direction × `moveSpeed`.

`FacingVector` exposes the facing direction to other scripts; [[Combat]] uses it to aim.

Move speed can be changed while playing from the [[Test Menu]].

Because the input is normalized, speed is always full or zero. A slight stick tilt moves at full speed.

While the game is paused (time scale 0) `Update` returns early, so the character keeps its pose instead of turning on the spot. See [[Menus and Game Flow]].

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

Two more states, `Swing` and `FireSpell`, are entered from "Any State" by the `Swing` and `Attack` triggers that `PlayerAttack` sets, and return to the idle for the current direction when they finish. While an attack is playing the three run flags are held off so a run state cannot cut the attack short. Each attack has a single clip, not one per direction.

## The facing marker

`facing` is moved to `player position + facing direction × facingObjectDistance` every frame. It still does nothing. It was meant as the origin for attacks, but `PlayerAttack` keeps its own `Attack` point instead, so the marker is a leftover that one of the two could replace.

## Links to other systems

- [[Rooms and Darkness]] reads the player's position to decide which room is lit.
- The [[Slime Enemy]] finds the player by tag and uses its position for sight and chasing.
- The [[Level Randomizer]] teleports the player to the start room after building a level.
- The Cinemachine camera in each scene has the player as its tracking target.

## How it can progress

- **Attack animations per direction**; the swing and the cast each use one clip for all four.
- **Health and hurt feedback** once enemies can deal damage.
- **Analog speed** by dropping the normalization for stick input, if walking slowly should be possible.
- **More actions** (dash, interact) as extra entries in the input actions asset.
- The `PlayerInput` component on the prefab is unused; the script reads the action directly. Remove it or switch to it, but not both.
