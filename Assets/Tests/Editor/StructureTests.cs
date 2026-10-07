using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class StructureTests
{
    [Test]
    public void StructureSpot_KeepsDistanceFromWalls()
    {
        var room = new RectInt(0, 0, 20, 14);
        List<Vector2Int> anchors = LevelRandomizer.StructureCandidates(room, new Vector2Int(6, 6), 3);

        Assert.AreEqual(9 * 3, anchors.Count);
        foreach (Vector2Int a in anchors)
        {
            Assert.GreaterOrEqual(a.x, 3);
            Assert.GreaterOrEqual(a.y, 3);
            Assert.LessOrEqual(a.x + 6, 17);
            Assert.LessOrEqual(a.y + 6, 11);
        }
    }

    [Test]
    public void StructureSpot_NoneWhenRoomTooSmall()
    {
        var room = new RectInt(0, 0, 14, 10);
        Assert.AreEqual(0, LevelRandomizer.StructureCandidates(room, new Vector2Int(6, 6), 3).Count);
    }

    [Test]
    public void StairsSpot_AvoidsTopWallDoors()
    {
        // Room x 0..19; a door in the top wall at x 8 (2 cells wide). Stairs are 4 wide, keep 1 cell from doors and corners
        List<int> xs = LevelRandomizer.StairsCandidates(new RectInt(0, 0, 20, 14), 4, new[] { 8 });

        Assert.IsNotEmpty(xs);
        foreach (int x in xs)
        {
            Assert.GreaterOrEqual(x, 1);
            Assert.LessOrEqual(x + 4, 19);
            // stairs cells x..x+3 grown by 1 must not touch the door cells 8..9
            Assert.IsTrue(x + 4 < 8 || x - 1 > 9, "x=" + x);
        }
    }
}
