using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LevelGraphTests
{
    static RectInt Room(int x, int y) => new RectInt(x, y, 4, 4);

    [Test]
    public void FarthestRoom_ChainEnd()
    {
        var rooms = new List<RectInt> { Room(0, 0), Room(10, 0), Room(20, 0), Room(30, 0) };
        var links = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3) };

        Assert.AreEqual(3, LevelGraph.FarthestRoom(rooms.Count, links, rooms, 0));
    }

    [Test]
    public void FarthestRoom_TieGoesToLargerDistance()
    {
        // All three hang off room 0, one door away each; room 3 is physically farthest
        var rooms = new List<RectInt> { Room(0, 0), Room(10, 0), Room(0, 10), Room(-40, 0) };
        var links = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) };

        Assert.AreEqual(3, LevelGraph.FarthestRoom(rooms.Count, links, rooms, 0));
    }

    [Test]
    public void SingleRoom_FarthestIsItselfAndNoOthers()
    {
        var rooms = new List<RectInt> { Room(0, 0) };
        var links = new List<Vector2Int>();

        Assert.AreEqual(0, LevelGraph.FarthestRoom(1, links, rooms, 0));
        Assert.AreEqual(0, LevelGraph.RoomsByDistance(1, links, rooms, 0).Count);
    }

    [Test]
    public void DoorDistances_Chain()
    {
        var links = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3) };

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, LevelGraph.DoorDistances(4, links, 0));
    }

    [Test]
    public void RoomsByDistance_FarthestFirstWithoutStart()
    {
        var rooms = new List<RectInt> { Room(0, 0), Room(10, 0), Room(20, 0) };
        var links = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 2) };

        CollectionAssert.AreEqual(new[] { 0, 1 }, LevelGraph.RoomsByDistance(3, links, rooms, 2));
    }
}
