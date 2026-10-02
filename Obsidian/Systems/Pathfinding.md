# Pathfinding

The grid A* that lets enemies walk around walls. Part of the [[Architecture]]; back to [[Home]].

**File:** `Assets/Prefabs/Enemies/Slime/Scripts/GridPathfinder.cs`

## What it is

A plain C# class, not a component. Each [[Slime Enemy]] creates its own instance, sized to its own body. It needs no baking and no setup in the scene: it discovers the level by asking the physics engine.

## How it works

**The grid is imaginary.** The world is treated as square cells of `cellSize` (0.5 units for the slime). A cell is walkable when a circle of the agent's radius at its centre touches no collider tagged `wall`. Each cell is checked the first time it is needed and the answer is cached.

**Finding a path.** Standard A* over eight directions:

- Diagonal steps are only allowed when both neighbouring straight cells are open, so paths never cut through wall corners.
- If the goal is inside a wall (the player hugging one), the nearest open cell within three cells is used instead.
- The search gives up after 4000 cells and reports no path.

**Smoothing.** The raw path zig-zags along the grid, so waypoints that can be reached in a straight line are removed. "Straight line" is tested by sweeping the agent's circle through the world, which keeps the result safe for the agent's real size.

**Direct line check.** `IsClear(from, to)` is the same circle sweep, exposed so the caller can skip pathfinding entirely when nothing is in the way.

## The one rule: the `wall` tag

Only non-trigger colliders tagged `wall` count. This is one of the three contracts in [[Architecture]]. Room trigger boxes from [[Rooms and Darkness]] are ignored because they are triggers.

## Limits

- **The cache never expires.** Walkable answers are kept for the life of the pathfinder. If walls move or the level is rebuilt by the [[Level Randomizer]], the enemy keeps using the old map.
- **One map per enemy.** Every slime rediscovers the same cells on its own.
- **Other enemies are not obstacles.** Slimes can path through each other.

## How it can progress

- **A reset method** that clears the cache, called when a level is rebuilt. Small change, and required before slimes can live in generated levels.
- **A shared map** owned by the level rather than each enemy, filled once from the wall tilemap. The randomizer already knows exactly which cells are walls.
- **Reuse for other movers:** baby slimes, future enemies, anything that should not walk through walls.
