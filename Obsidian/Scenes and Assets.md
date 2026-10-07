# Scenes and Assets

What every scene, prefab and art source is for. Part of the [[Architecture]]; back to [[Home]].

## Scenes

All in `Assets/Scenes/`. Each scene has one job:

| Scene | Purpose | Notes |
|---|---|---|
| `Start Menu` | The scene the game opens with: Start, Settings, End | First in Build Settings. Start loads `Demo`. See [[Menus and Game Flow]]. |
| `Demo` | **The game.** The full [[Game Loop]] as a player plays it: random levels, waves, key, gate, abilities, death back to the menu | Second in Build Settings. No level is saved in it; the loop builds one on load. Has the [[HUD]] and the [[Test Menu]] |
| `testing the new thing` | **The sandbox.** Every new feature is first tried out here on its own, through the [[Test Menu]] (U) | Third in Build Settings. Generated level, darkness, Randomize button, pause menu, health label and attack corner |
| `Test AI enemy` | **Enemy AI.** Slime and [[Combat]] test room | Tilemap floor, box colliders tagged `wall`, one BigSlime |

Cleaned up on 5 Oct 2026: `SampleScene` (the Week 1 hand-made rooms demo, which the randomizer replaced) and the duplicate early tilemap rooms `New Scene` and `New Scene 1` were deleted. They are still in git history.

`Assets/Settings/Scenes/URP2DSceneTemplate.unity` is not a game scene. It is the template Unity uses when a new scene is created.

Every gameplay scene carries its own copy of the same trio: Main Camera (with a Cinemachine brain), a `CinemachineCamera` prefab instance tracking the player, and a `Player` prefab instance.

## Prefabs

| Prefab | Used by |
|---|---|
| `Assets/Prefabs/Player.prefab` | Every scene. See [[Player]]. |
| `Assets/Prefabs/CinemachineCamera.prefab` | Every scene. Follows its tracking target from 10 units back; the target is set per scene. |
| `Assets/Prefabs/Enemies/Slime/BigSlime.prefab` | `Test AI enemy`; also spawned by merging baby slimes. See [[Slime Enemy]]. |
| `Assets/Prefabs/Enemies/Slime/BabySlime.prefab` | Spawned by `BigSlime.DieAndSplit()`. |
| `Assets/Prefabs/FireProjectile.prefab` | Spawned by the player's fire spell. A sprite with a small 2D light child for the glow. See [[Combat]]. |
| `Assets/Prefabs/UI/Settings Panel.prefab` | `Start Menu`, `testing the new thing` and `Demo`. See [[Menus and Game Flow]]. |
| `Assets/Prefabs/UI/Health Label.prefab`, `Attack Corner.prefab` | `testing the new thing` and `Demo`. See [[HUD]]. |
| `Assets/Prefabs/UI/Loop HUD.prefab`, `Ability Choice.prefab` | `Demo`. See [[Game Loop]]. |
| `Assets/Prefabs/Spawn Marker.prefab` | The glowing warning on a spawn spot, placed by the wave spawner ([[Game Loop]]). Uses `Assets/Art/Placeholders/SoftGlow.png` |
| `Assets/Prefabs/Key.prefab`, `Gate.prefab` | Placed by the [[Game Loop]]; also from the [[Test Menu]]. Placeholder art from `Assets/Art/Placeholders/`. |
| `Assets/Prefabs/Abilities/` | The ability scripts, their assets and `Default Ability Pool`. See [[Inventory and Abilities]]. |

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
- **Layers:** `Player` (layer 6) on the player, and `Enemy` (layer 7), used by the slime prefabs and searched by the player's attacks. The two layers do not collide with each other (Physics 2D collision matrix), so slimes pass through the player instead of pushing it.
- **Rendering:** URP with the 2D renderer. Scenes without any 2D light render fully lit. The fireball adds a global light at runtime in such scenes so its glow does not darken them.
- **Input handling:** both the old and new input systems are enabled. Gameplay only uses the new one.

## Housekeeping candidates

- Scripts live under `Assets/Prefabs/…/Scripts/` while `Assets/Scripts/` is empty.
- `RoomsManager.cs` contains a class named `RoomManager`.
- `EnemyHeatlh.cs` contains a class named `EnemyHealth`.
- The folder `Pallates` is a misspelling of "Palettes".

None of these break anything today. They are listed in [[Roadmap]] as cleanup.
