# HUD

What the player sees on top of the game: health and the state of their attacks. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/HealthBar.cs`, `Assets/Prefabs/Scripts/HeartsDisplay.cs`, `Assets/Prefabs/Scripts/AttackCorner.cs`, prefabs `Assets/Prefabs/UI/Health Label.prefab` and `Assets/Prefabs/UI/Attack Corner.prefab`, art in `Assets/Art/healthstamina/`

Added on 5 Oct 2026 as part of the game loop work ([[Roadmap]]). Plain UGUI with Unity's built-in font, like the menus. The health and stamina art was added on 8 Oct 2026.

## Health bar and stamina hearts (top left)

The `Health Label` prefab kept its name but no longer shows text. It has two parts:

- **Health bar (red).** `HealthBar` listens to the player's `Health` ([[Player]]) and slides a filled image to the new value (`fillSpeed` 4). On damage a lighter **damage trail** stays at the old value for `trailDelay` 0.35 s and then drains down (`trailSpeed` 0.8), so the size of the hit is visible. Healing moves the trail up at once.
- **Stamina hearts (blue).** `HeartsDisplay` with source `SpellCharges` shows the fire spell's charges ([[Combat]]). One heart is two charges: full, half or empty. A heart pops briefly when it changes, and hearts are added or removed if the maximum changes (abilities, [[Inventory and Abilities]]). The same component can show health as hearts with source `Health`, but it isn't used that way.

The art is the user's sheet `Untitled_2-9aa1.png` (heart slices: `_0` full, `_3` half, `_4` empty; `_5` the bar). The bar was split into `HealthBar_Frame.png` (68×10) and `HealthBar_Fill.png` (36×6) so the fill can shrink inside the frame. Everything is drawn at 5× with point filtering so the pixels stay sharp.

`HealthLabel.cs` (the old `HP 7/10` text) is still in the project but no longer used.

## Attack corner (bottom left)

Three slots, **Swing**, **Fire** and **Dash** (the dash slot only appears if the player has `PlayerDash`, [[Player]]). Each answers three questions at a glance:

- **Can I use it?** A dark cover over the slot shrinks as the attack comes back. No cover means ready.
- **How long until I can?** The bottom line counts down in seconds.
- **Why not?** The bottom line names the reason:
  - **Ready**
  - **Busy**: another attack or the dash is still playing (only one can run at a time)
  - **Cooldown**: the swing or the dash was used too recently
  - **No charges**: every fire spell charge is spent; the timer is the time until the next one comes back

The Fire slot also shows its charges as dots, filled for each one available.

Both attacks have a limit, so neither can be spammed: the swing has a cooldown that starts when it is swung, and the fire spell has charges that come back one at a time ([[Combat]]).

The corner reads everything from `PlayerAttack` and `PlayerDash` every frame. The reasons are decided there, not in the HUD, so anything else that needs them (an AI hint, a sound) gets the same answer. The slots build themselves when the game starts, so the prefab is a single object.

## FPS counter (bottom right)

`FpsCounter` (`Assets/Prefabs/Scripts/FpsCounter.cs`, with the maths in `FpsSampler.cs`) shows frames per second, milliseconds per frame, and the slowest frame of the last half second. It is in `Demo` and `testing the new thing`.

**How FPS is measured.** Every frame Unity reports how long the previous frame took (`Time.unscaledDeltaTime`, real time, so pausing doesn't affect it). The counter keeps the last half second of those times. FPS is how many frames fit in that half second, divided by its length. Milliseconds per frame is the average frame time. The worst frame is the single slowest one, which shows a hitch even when the average looks fine. The text refreshes four times a second so it can be read.

**Why it can show more than 60 on a 60 Hz monitor.** With VSync on, Unity waits for the monitor's refresh before showing each frame, so the game can never run faster than the screen (60 FPS on a 60 Hz monitor). The counter's `uncapFrameRate` switch (on by default) turns VSync off and removes any frame cap when the game starts, so the number is what the PC can actually do. The monitor still only shows 60 of those frames per second, and fast camera movement can show tearing. The Test Menu can switch the cap back on.

### Measurements so far

| Date | Machine | Power | FPS | Notes |
|---|---|---|---|---|
| 7 Oct 2026 | Laptop: RTX 2050, Intel Core i5-1335U | Plugged in | about 62 | Barely above a 60 Hz screen |
| 7 Oct 2026 | Laptop: RTX 2050, Intel Core i5-1335U | On battery | under 15 | Unplayable; laptops cut CPU and GPU power on battery |
| 7 Oct 2026 | Desktop: RTX 5060 Ti | — | about 600 | |

Measured with the FPS counter in `Demo`, VSync off, before any optimisation.

The gap between the laptop and the desktop is about ten times. Optimisation is planned for later ([[Roadmap]]); new measurements go in this table so changes can be compared.

Colours: green at 60 or more, yellow from 30, red below 30. In the editor the number is lower than in a built game, because the editor draws its own windows too.

## How it can progress

- Icons and art for the slots in the attack corner.
- Rename the `Health Label` prefab and delete `HealthLabel.cs`, now that it is a bar.
- More slots as abilities that can be used actively are added ([[Inventory and Abilities]]).
- The loop's own HUD (level and wave, objective, inventory, banner, death screen) is described in [[Game Loop]]. It exists only in `Demo`.
