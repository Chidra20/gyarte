# HUD

What the player sees on top of the game: health and the state of their attacks. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/HealthLabel.cs`, `Assets/Prefabs/Scripts/AttackCorner.cs`, prefabs `Assets/Prefabs/UI/Health Label.prefab` and `Assets/Prefabs/UI/Attack Corner.prefab`

Added on 5 Oct 2026 as part of the game loop work ([[Roadmap]]). Plain UGUI with Unity's built-in font, like the menus; there is no HUD art yet.

## Health label (top left)

Shows the player's health as `HP 7/10`. It listens to the player's `Health` and redraws only when health changes, turning red when health is low ([[Player]], [[Combat]]).

In `testing the new thing` it sits under the Randomize button. In the prefab it is in the top-left corner.

## Attack corner (bottom left)

Two slots, **Swing** and **Fire**. Each answers three questions at a glance:

- **Can I use it?** A dark cover over the slot shrinks as the attack comes back. No cover means ready.
- **How long until I can?** The bottom line counts down in seconds.
- **Why not?** The bottom line names the reason:
  - **Ready**
  - **Busy**: the other attack is still playing (only one can run at a time)
  - **Cooldown**: the swing was used too recently
  - **No charges**: every fire spell charge is spent; the timer is the time until the next one comes back

The Fire slot also shows its charges as dots, filled for each one available.

Both attacks have a limit, so neither can be spammed: the swing has a cooldown that starts when it is swung, and the fire spell has charges that come back one at a time ([[Combat]]).

The corner reads everything from `PlayerAttack` every frame. The reasons are decided there, not in the HUD, so anything else that needs them (an AI hint, a sound) gets the same answer. The slots build themselves when the game starts, so the prefab is a single object.

## How it can progress

- Icons and art for the slots; a hit flash on the health label.
- More slots as abilities that can be used actively are added ([[Inventory and Abilities]]).
- The loop's own HUD (level and wave, objective, inventory, banner, death screen) is described in [[Game Loop]]. It exists only in `Demo`.
