using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LevelOccupancyTests
{
    static HashSet<Vector2Int> Room(int width, int height)
    {
        var cells = new HashSet<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells.Add(new Vector2Int(x, y));
        return cells;
    }

    [Test]
    public void IsFree_RespectsPadding()
    {
        var occupancy = new LevelOccupancy();
        occupancy.Block(new RectInt(5, 5, 1, 1));

        Assert.IsTrue(occupancy.IsFree(new RectInt(7, 5, 1, 1), 0));
        Assert.IsTrue(occupancy.IsFree(new RectInt(7, 5, 1, 1), 1));
        Assert.IsFalse(occupancy.IsFree(new RectInt(6, 5, 1, 1), 1));
        Assert.IsFalse(occupancy.IsFree(new RectInt(5, 5, 1, 1), 0));
    }

    [Test]
    public void ReservedAndHazardAreNotFree()
    {
        var occupancy = new LevelOccupancy();
        occupancy.Reserve(new RectInt(0, 0, 2, 2));
        occupancy.Hazard(new RectInt(4, 0, 1, 1));

        Assert.IsFalse(occupancy.IsFree(new RectInt(1, 1, 1, 1), 0));
        Assert.IsFalse(occupancy.IsFree(new RectInt(4, 0, 1, 1), 0));
        Assert.IsTrue(occupancy.IsHazard(new Vector2Int(4, 0)));
        Assert.IsFalse(occupancy.IsBlocked(new Vector2Int(4, 0)));
    }

    [Test]
    public void Reachable_OpenRoom()
    {
        var walkable = Room(6, 4);
        Assert.IsTrue(LevelOccupancy.AllReachable(walkable, new Vector2Int(0, 0), new[] { new Vector2Int(5, 3), new Vector2Int(5, 0) }));
    }

    [Test]
    public void Unreachable_WhenWallSplitsRoom()
    {
        var walkable = Room(6, 4);
        for (int y = 0; y < 4; y++) walkable.Remove(new Vector2Int(3, y));

        Assert.IsFalse(LevelOccupancy.AllReachable(walkable, new Vector2Int(0, 0), new[] { new Vector2Int(5, 3) }));
    }

    [Test]
    public void Reachable_TargetMissingFromWalkable_False()
    {
        var walkable = Room(3, 3);
        Assert.IsFalse(LevelOccupancy.AllReachable(walkable, new Vector2Int(0, 0), new[] { new Vector2Int(9, 9) }));
    }
}
