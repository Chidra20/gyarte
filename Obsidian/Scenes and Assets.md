# Scenes and Assets

What every scene, prefab and art source is for. Part of the [[Architecture]]; back to [[Home]].

## Scenes

All in `Assets/Scenes/`.

| Scene | Purpose | Notes |
|---|---|---|
| `Start Menu` | The scene the game opens with: Start, Settings, End | First in Build Settings. See [[Menus and Game Flow]]. |
| `SampleScene` | The original demo: three hand-made rooms with the room darkness logic | Third in Build Settings. Built from placeholder squares. Has the only Global Light 2D. |
| `Test AI enemy` | Slime and [[Combat]] test room | Tilemap floor, box colliders tagged `wall`, one BigSlime |
| `testing the new thing` | [[Level Randomizer]] test, and the scene Start loads | Second in Build Settings. Generated level, darkness, Randomize button, pause menu, and the [[Test Menu]] (U) |
| `New Scene`, `New Scene 1` | Early tilemap room | Were identical copies when checked; no `wall` tags, so not usable with the slime |

Every gameplay scene carries its own copy of the same trio: Main Camera (with a Cinemachine brain), a `CinemachineCamera` prefab instance tracking the player, and a `Player` prefab instance.

## Prefabs

| Prefab | Used by |
|---|---|
| `Assets/Prefabs/Player.prefab` | Every scene. See [[Player]]. |
| `Assets/Prefabs/CinemachineCamera.prefab` | Every scene. Follows its tracking target from 10 units back; the target is set per scene. |
| `Assets/Prefabs/Enemies/Slime/BigSlime.prefab` | `Test AI enemy`; also spawned by merging baby slimes. See [[Slime Enemy]]. |
| `Assets/Prefabs/Enemies/Slime/BabySlime.prefab` | Spawned by `BigSlime.DieAndSplit()`. |
| `Assets/Prefabs/FireProjectile.prefab` | Spawned by the player's fire spell. A sprite with a small 2D light child for the glow. See [[Combat]]. |
| `Assets/Prefabs/UI/Settings Panel.prefab` | `Start Menu` and `testing the new thing`. See [[Menus and Game Flow]]. |

## Art

**Catacombs tileset** — `Assets/Art/RF_Catacombs_v1.0/`

- `mainlevbuild.png` is the main sheet: 16-pixel tiles at 16 pixels per unit, so one tile is one world unit. It is sliced into 1024 sprites.
- `decorative.png`, torches, candles and spikes are imported but not used anywhere yet.
- The pack's licence is in `public-license.txt`.

**Tile assets** — `Assets/Art/Pallates/`

One tile asset per sprite (`mainlevbuild_0` … `mainlevbuild_1023`) plus the `Catacombs` palette for painting by hand. The numbers do not follow the sheet row by row, so pick tiles by looking at them in the palette, not by counting.

The [[Level Randomizer]] uses these groups:

| Group | Tiles |
|---|---|
| Floor blocks (18 blocks of 2×2) | three rows of six starting at 581, 670 and 786 |
| Top beam and corners | 15–19 |
| Bottom beam and corners | 210–214 |
| Columns and caps | 38, 39, 73, 74, 116, 117, 163, 164 |
| Pillars | five props built from tiles between 256 and 375 |

**Characters**

- Player sheets: `Assets/Art/mc_spritesheet.png` and the `mc sprites` files; clips and controller in `Assets/Animations/`.
- Slime sprites, clips and controllers sit next to their prefabs in `Assets/Prefabs/Enemies/Slime/`.
- Attack sheets: `Assets/Art/Attacks/` holds the scythe swing and the fire spell. Their clips, `Swing` and `FireSpell`, are in `Assets/Animations/`; the fireball's own frames are set as sprite lists on its prefab.

## Input

`Assets/PlayerInputActions.inputactions` holds one action map, `Player`, with three actions:

| Action | Gamepad | Keyboard |
|---|---|---|
| `Move` | left stick | WASD |
| `Attack` | west button (Square / X) | J |
| `Projectile` | north button (Triangle / Y) | Space |

The gamepad bindings use Unity's generic gamepad layout, which names buttons by position, so one binding covers PlayStation and Xbox controllers. New actions should be bound the same way. See [[Combat]] for what the two attack actions do.

The [[Test Menu]] is the exception: its toggle key is a plain keyboard key set on the component, not an action. `Assets/Settings/InputSystem_Actions.inputactions` is Unity's default asset, set as the project-wide actions; the game code does not use it.

## Project settings worth knowing

- **Tags:** `wall` (custom) and the built-in `Player`.
- **Layers:** `Enemy` (layer 7), used by the slime prefabs and searched by the player's attacks.
- **Rendering:** URP with the 2D renderer. Scenes without any 2D light render fully lit. The fireball adds a global light at runtime in such scenes so its glow does not darken them.
- **Input handling:** both the old and new input systems are enabled. Gameplay only uses the new one.

## Housekeeping candidates

- Scripts live under `Assets/Prefabs/…/Scripts/` while `Assets/Scripts/` is empty.
- `RoomsManager.cs` contains a class named `RoomManager`.
- `EnemyHeatlh.cs` contains a class named `EnemyHealth`.
- The folder `Pallates` is a misspelling of "Palettes".
- The two `New Scene` files can likely be deleted.

None of these break anything today. They are listed in [[Roadmap]] as cleanup.
