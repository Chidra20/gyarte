using System.Collections.Generic;
using UnityEngine;

// A* pathfinding over a grid that is built on the fly from colliders tagged as walls.
// No baking needed: each cell is checked the first time it is visited and then cached.
public class GridPathfinder
{
    private readonly float cellSize;
    private readonly float agentRadius;
    private readonly string wallTag;
    private readonly int maxNodes;

    private readonly Dictionary<Vector2Int, bool> walkableCache = new Dictionary<Vector2Int, bool>();
    private readonly Collider2D[] overlapHits = new Collider2D[8];
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private ContactFilter2D wallFilter = new ContactFilter2D { useTriggers = false };

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    public GridPathfinder(float cellSize, float agentRadius, string wallTag = "wall", int maxNodes = 4000)
    {
        this.cellSize = cellSize;
        this.agentRadius = agentRadius;
        this.wallTag = wallTag;
        this.maxNodes = maxNodes;
    }

    // Fills 'path' with world-space waypoints from start to goal. Returns false if no path was found.
    public bool FindPath(Vector2 start, Vector2 goal, List<Vector2> path)
    {
        path.Clear();

        Vector2Int startCell = ToCell(start);
        Vector2Int goalCell = ToCell(goal);
        bool goalIsExact = IsWalkable(goalCell);
        if (!goalIsExact && !TryFindNearbyWalkable(goalCell, out goalCell)) return false;

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [startCell] = 0f };
        var closed = new HashSet<Vector2Int>();
        var open = new MinHeap();
        open.Push(startCell, Heuristic(startCell, goalCell));

        while (open.Count > 0 && closed.Count < maxNodes)
        {
            Vector2Int current = open.Pop();

            if (current == goalCell)
            {
                BuildPath(cameFrom, startCell, goalCell, goalIsExact ? goal : ToWorld(goalCell), path);
                Smooth(start, path);
                return true;
            }

            if (!closed.Add(current)) continue;

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (closed.Contains(next) || !IsWalkable(next)) continue;

                bool diagonal = dir.x != 0 && dir.y != 0;

                // Don't cut corners: both sides of a diagonal step must be open
                if (diagonal && (!IsWalkable(new Vector2Int(current.x + dir.x, current.y)) ||
                                 !IsWalkable(new Vector2Int(current.x, current.y + dir.y))))
                    continue;

                float cost = gScore[current] + (diagonal ? 1.41421f : 1f);
                if (gScore.TryGetValue(next, out float oldCost) && cost >= oldCost) continue;

                gScore[next] = cost;
                cameFrom[next] = current;
                open.Push(next, cost + Heuristic(next, goalCell));
            }
        }

        return false;
    }

    // True when the agent could move in a straight line from 'from' to 'to' without touching a wall
    public bool IsClear(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f) return true;

        int count = Physics2D.CircleCast(from, agentRadius, delta / distance, wallFilter, castHits, distance);
        for (int i = 0; i < count; i++)
        {
            if (castHits[i].collider.CompareTag(wallTag)) return false;
        }
        return true;
    }

    public bool IsWalkable(Vector2 worldPosition)
    {
        return IsWalkable(ToCell(worldPosition));
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (walkableCache.TryGetValue(cell, out bool walkable)) return walkable;

        walkable = true;
        int count = Physics2D.OverlapCircle(ToWorld(cell), agentRadius, wallFilter, overlapHits);
        for (int i = 0; i < count; i++)
        {
            if (overlapHits[i].CompareTag(wallTag))
            {
                walkable = false;
                break;
            }
        }

        walkableCache[cell] = walkable;
        return walkable;
    }

    // If the goal is inside a wall (e.g. the player is hugging one), aim for the closest open cell instead
    private bool TryFindNearbyWalkable(Vector2Int cell, out Vector2Int result)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;

                    Vector2Int candidate = new Vector2Int(cell.x + x, cell.y + y);
                    if (IsWalkable(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        result = cell;
        return false;
    }

    private void BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int startCell, Vector2Int goalCell, Vector2 finalPoint, List<Vector2> path)
    {
        path.Add(finalPoint);

        Vector2Int current = goalCell;
        while (cameFrom.TryGetValue(current, out Vector2Int previous) && previous != startCell)
        {
            path.Add(ToWorld(previous));
            current = previous;
        }

        path.Reverse();
    }

    // Skip waypoints that can be reached in a straight line so movement doesn't zig-zag along the grid
    private void Smooth(Vector2 start, List<Vector2> path)
    {
        Vector2 anchor = start;
        int i = 0;

        while (i < path.Count - 1)
        {
            if (IsClear(anchor, path[i + 1]))
            {
                path.RemoveAt(i);
            }
            else
            {
                anchor = path[i];
                i++;
            }
        }
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.41421f * Mathf.Min(dx, dy);
    }

    private Vector2Int ToCell(Vector2 world)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));
    }

    private Vector2 ToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * cellSize, cell.y * cellSize);
    }

    // Small binary heap used as the A* open list
    private class MinHeap
    {
        private readonly List<(Vector2Int cell, float priority)> items = new List<(Vector2Int, float)>();

        public int Count => items.Count;

        public void Push(Vector2Int cell, float priority)
        {
            items.Add((cell, priority));
            int i = items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (items[parent].priority <= items[i].priority) break;
                (items[parent], items[i]) = (items[i], items[parent]);
                i = parent;
            }
        }

        public Vector2Int Pop()
        {
            Vector2Int top = items[0].cell;
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                int right = left + 1;
                int smallest = i;

                if (left < items.Count && items[left].priority < items[smallest].priority) smallest = left;
                if (right < items.Count && items[right].priority < items[smallest].priority) smallest = right;
                if (smallest == i) break;

                (items[smallest], items[i]) = (items[i], items[smallest]);
                i = smallest;
            }

            return top;
        }
    }
}
