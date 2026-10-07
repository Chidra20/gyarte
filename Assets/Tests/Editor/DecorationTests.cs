using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DecorationTests
{
    static readonly RectInt Room = new RectInt(0, 0, 16, 10);

    [Test]
    public void AgainstTopWall_UsesTheTopFloorRows()
    {
        List<RectInt> spots = DecorationPlacer.Candidates(Placement.AgainstTopWall, Room, new Vector2Int(2, 1));
        Assert.IsNotEmpty(spots);
        foreach (RectInt r in spots) Assert.AreEqual(Room.yMax - 1, r.yMin);
    }

    [Test]
    public void Corner_GivesTheFourCorners()
    {
        List<RectInt> spots = DecorationPlacer.Candidates(Placement.Corner, Room, Vector2Int.one);
        CollectionAssert.AreEquivalent(new[]
        {
            new RectInt(0, 0, 1, 1), new RectInt(15, 0, 1, 1), new RectInt(0, 9, 1, 1), new RectInt(15, 9, 1, 1)
        }, spots);
    }

    [Test]
    public void AlongWall_TouchesAWall()
    {
        foreach (RectInt r in DecorationPlacer.Candidates(Placement.AlongWall, Room, Vector2Int.one))
        {
            bool touches = r.xMin == Room.xMin || r.xMax == Room.xMax || r.yMin == Room.yMin || r.yMax == Room.yMax;
            Assert.IsTrue(touches, r.ToString());
        }
    }

    [Test]
    public void Anywhere_KeepsTwoCellsFromWalls()
    {
        List<RectInt> spots = DecorationPlacer.Candidates(Placement.Anywhere, Room, new Vector2Int(1, 1));
        Assert.IsNotEmpty(spots);
        foreach (RectInt r in spots)
        {
            Assert.GreaterOrEqual(r.xMin, 2);
            Assert.GreaterOrEqual(r.yMin, 2);
            Assert.LessOrEqual(r.xMax, Room.xMax - 2);
            Assert.LessOrEqual(r.yMax, Room.yMax - 2);
        }
    }

    [Test]
    public void OnTopWall_IsOnTheWallRowAwayFromCorners()
    {
        List<RectInt> spots = DecorationPlacer.Candidates(Placement.OnTopWall, Room, Vector2Int.one);
        Assert.IsNotEmpty(spots);
        foreach (RectInt r in spots)
        {
            Assert.AreEqual(Room.yMax, r.yMin);
            Assert.Greater(r.xMin, Room.xMin);
            Assert.Less(r.xMax, Room.xMax);
        }
    }

    [Test]
    public void Pick_StartRoomGetsStartTheme()
    {
        var start = Theme("Start", false);
        var crypt = Theme("Crypt", false);
        Assert.AreSame(start, RoomTheme.Pick(new[] { crypt }, true, false, false, start, new System.Random(1)));
        Object.DestroyImmediate(start);
        Object.DestroyImmediate(crypt);
    }

    [Test]
    public void Pick_NoTrapInExitRoomOrTwiceInARow()
    {
        var trap = Theme("Trap", true);
        var crypt = Theme("Crypt", false);
        var rng = new System.Random(3);
        for (int i = 0; i < 50; i++)
        {
            Assert.AreSame(crypt, RoomTheme.Pick(new[] { trap, crypt }, false, true, false, null, rng));
            Assert.AreSame(crypt, RoomTheme.Pick(new[] { trap, crypt }, false, false, true, null, rng));
        }
        Object.DestroyImmediate(trap);
        Object.DestroyImmediate(crypt);
    }

    static RoomTheme Theme(string name, bool trap)
    {
        var theme = ScriptableObject.CreateInstance<RoomTheme>();
        theme.themeName = name;
        theme.isTrap = trap;
        return theme;
    }
}
