# Rooms and Darkness

The rule that only the room the player is standing in is visible. Part of the [[Architecture]]; back to [[Home]].

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
3. When the player is in a different room than last frame, it enables the black sprite on every room except that one.

Consequences of this design:

- Rooms that were visited go dark again when the player leaves.
- If the player is in no box at all (for example mid-doorway between two boxes that do not touch), nothing changes and the last room stays lit.
- The boxes are triggers, so they never block movement. They are only used as rectangles.

## Two ways rooms get made

**By hand.** Room objects are placed and scaled in the editor over a hand-built layout. This was the first version, built in `SampleScene`, which was deleted on 5 Oct 2026; the method still works in any scene.

**By the generator.** The [[Level Randomizer]] creates one room object per generated room, sized to cover the floor and its walls. Neighbouring rooms touch exactly, so there is no lit gap between them. The start room is lit immediately; the rest start dark.

Both use the same `RoomManager` with no special cases. This is the room object contract described in [[Architecture]].

## Week 1 change

`RoomManager` used to remember the current room by name. A regenerated level reuses the names "Room 1", "Room 2", …, so the new first room was mistaken for the old one and stayed dark. It now remembers the room object itself.

## How it can progress

- **Remember visited rooms.** Keep a set of entered rooms and show them dimmed instead of black, for a map-like feel.
- **Fade** the overlay in and out instead of switching it instantly.
- **Room events.** The moment the current room changes is the natural hook for "wake the enemies in this room" or "lock the doors until the room is cleared". Exposing it as an event would let combat and spawning plug in without touching this script. See [[Roadmap]].
- **Real lighting.** URP 2D lights and shadow casters could replace the black sprites later. The room boxes would still be useful for the events above.
