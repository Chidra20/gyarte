# Inventory and Abilities

What the player carries through a run (keys and abilities) and how abilities are made and handed out. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/Inventory.cs`, everything in `Assets/Prefabs/Abilities/` (the `Ability` base class, `AbilityPool`, the four placeholder abilities and their assets, and `Default Ability Pool.asset`)

Added on 5 Oct 2026 for the game loop ([[Roadmap]]).

## Inventory

`Inventory` sits on the [[Player]] and holds two things:

- **Keys.** A level's gate needs a key to open ([[Game Loop]]). Keys are counted rather than being a yes/no, so a later level could ask for more than one.
- **Abilities**, each with how many times it has been picked (its stacks).

Whenever either changes it raises an event. Nothing listens to it yet; the loop's HUD simply reads the inventory every frame ([[Game Loop]]). The inventory lives on the player object, so it lasts across the levels of one run (the Demo scene rebuilds levels around the same player) and is gone when the run ends and the scene is left.

## Abilities

An ability is a **ScriptableObject**: one small script that says what it does to the player, and one asset that gives it a name, a one-line description for its choice card, and how many times it may be picked in a run. Picking an ability adds a stack to the inventory and applies its effect at once. Picking it again applies the effect again.

To add an ability: write a subclass of `Ability` with its `Apply`, create its asset from the Create menu (`gyarte/Abilities/…`), and add the asset to `Default Ability Pool`.

### The four placeholders

There are no real abilities yet. These four exist so the choice screen and the inventory can be played and tested. They are plain stat boosts:

| Ability | Effect per pick |
|---|---|
| Vitality | More maximum health, and heals the same amount |
| Quick Scythe | Swing cooldown 20% shorter, but never shorter than the swing animation itself ([[Combat]]). It can be picked twice: after that the floor is reached and a third pick would do nothing |
| Deep Reserves | One more fire spell charge, given right away |
| Fleet Foot | Moves 15% faster (raises the player's speed multiplier) |

Verified on 5 Oct 2026 in `testing the new thing`: one pick of each changed health from 7/10 to 9/12, the swing cooldown from 0.6 to 0.48 seconds, the spell charges from 2/2 to 3/3, and speed to 1.15×.

## The pool and drawing cards

`AbilityPool` is the list the choice screen draws from. A draw:

1. leaves out every ability the player already has at its maximum stacks,
2. shuffles the rest,
3. takes up to three, all different.

If fewer than three are left, the screen shows fewer cards. If none are left, it is skipped. The choice screen itself is part of the [[Game Loop]].

## In the Test Menu

The **Inventory** section shows the keys and the abilities held. It can give a key and add any ability from the pool directly; abilities at their maximum are greyed out. See [[Test Menu]].

## How it can progress

- Real abilities, including ones with their own button, which would also need a slot in the [[HUD]] attack corner.
- Rarity, so some cards turn up less often.
- An inventory window when there is enough in it to manage.
