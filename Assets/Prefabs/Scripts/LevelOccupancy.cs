using System.Collections.Generic;
using UnityEngine;

// The level-wide record of which cells are taken, so nothing is placed on top of anything else:
//   Blocked  - can't be walked through (pillars, pits, blocking props)
//   Hazard   - can be walked on but hurts (spikes); nothing else goes there either
//   Reserved - must stay clear (doorways and the cell in front, the spawn, the exit)
// Also answers "can the player still get everywhere?" with a flood fill.
public class LevelOccupancy
{
    private readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> hazard = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();

    public void Clear()
    {
        blocked.Clear();
        hazard.Clear();
        reserved.Clear();
    }

    public void Block(RectInt area) { Add(blocked, area); }
    public void Hazard(RectInt area) { Add(hazard, area); }
    public void Reserve(RectInt area) { Add(reserved, area); }

    // Takes a blocking placement back, when it turned out to seal part of the level off
    public void Unblock(RectInt area)
    {
        foreach (Vector2Int cell in area.allPositionsWithin) blocked.Remove(cell);
    }

    public bool IsBlocked(Vector2Int cell) { return blocked.Contains(cell); }
    public bool IsHazard(Vector2Int cell) { return hazard.Contains(cell); }
    public bool IsReserved(Vector2Int cell) { return reserved.Contains(cell); }

    // True when no cell of the area, grown by 'padding' on every side, is taken in any way
    public bool IsFree(RectInt area, int padding)
    {
        for (int x = area.xMin - padding; x < area.xMax + padding; x++)
        {
            for (int y = area.yMin - padding; y < area.yMax + padding; y++)
            {
                var cell = new Vector2Int(x, y);
                if (blocked.Contains(cell) || hazard.Contains(cell) || reserved.Contains(cell)) return false;
            }
        }
        return true;
    }

    // Flood fill over 'walkable' (4 directions) from 'from'; true when every target was reached
    public static bool AllReachable(ISet<Vector2Int> walkable, Vector2Int from, IEnumerable<Vector2Int> targets)
    {
        if (!walkable.Contains(from)) return false;

        var seen = new HashSet<Vector2Int> { from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            foreach (Vector2Int step in Steps)
            {
                Vector2Int next = cell + step;
                if (walkable.Contains(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }

        foreach (Vector2Int target in targets)
        {
            if (!seen.Contains(target)) return false;
        }
        return true;
    }

    private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    static void Add(HashSet<Vector2Int> set, RectInt area)
    {
        foreach (Vector2Int cell in area.allPositionsWithin) set.Add(cell);
    }
}
