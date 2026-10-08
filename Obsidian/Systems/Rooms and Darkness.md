# Rooms and Darkness

The rule that only rooms the player has entered are visible. Part of the [[Architecture]]; back to [[Home]].

**Files:** `Assets/Prefabs/Scripts/RoomsManager.cs` (the class inside is named `RoomManager`)

## The idea

A room is not tiles. It is an invisible box that says "this area is one room" plus a black sprite stretched over that area. Hiding a room means showing its black sprite.

```
Rooms             RoomManager
├── Room 1        BoxCollider2D (trigger) + SpriteRenderer (black)
├── Room 2        …
└── Room 3        …
```

## How it works

1. On start, `RoomManager` finds the player by the `Player` tag.
2. Every frame it walks through its children and asks each box whether the player's position is inside it.
3. When the player is in a different room than last frame, it adds that room to `visitedRooms` and darkens every room that hasn't been visited.
4. The black sprite fades in or out over `fadeTime` 0.35 s instead of switching at once.

Consequences of this design:

- Rooms stay lit once visited (since 8 Oct 2026), so explored parts of the level stay visible.
- `IsRevealed(point)` tells whether a point is in a visited room (or in no room at all). The [[Slime Enemy]] uses it: a slime in a dark room can't see the player.
- `RefreshDarkness(true)` redraws without the fade; the [[Test Menu]] uses it to bring the darkness back after switching it off. Rooms of an old level drop out of the visited set once they are destroyed, so a new level starts dark.
- If the player is in no box at all (for example mid-doorway between two boxes that do not touch), nothing changes and the last room stays lit.
- The boxes are triggers, so they never block movement. They are only used as rectangles.

## Two ways rooms get made

**By hand.** Room objects are placed and scaled in the editor over a hand-built layout. This was the first version, built in `SampleScene`, which was deleted on 5 Oct 2026; the method still works in any scene.

**By the generator.** The [[Level Randomizer]] creates one room object per generated room, sized to cover the floor and its walls. Neighbouring rooms touch exactly, so there is no lit gap between them. The start room is lit immediately; the rest start dark.

Both use the same `RoomManager` with no special cases. This is the room object contract described in [[Architecture]].

## Week 1 change

`RoomManager` used to remember the current room by name. A regenerated level reuses the names "Room 1", "Room 2", …, so the new first room was mistaken for the old one and stayed dark. It now remembers the room object itself.

## How it can progress

- **Dim visited rooms** a little when the player isn't in them, to make the current room stand out.
- **Room events.** The moment the current room changes is the natural hook for "wake the enemies in this room" or "lock the doors until the room is cleared". Exposing it as an event would let combat and spawning plug in without touching this script. See [[Roadmap]].
- **Real lighting.** URP 2D lights and shadow casters could replace the black sprites later. The room boxes would still be useful for the events above.
