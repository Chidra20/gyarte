using System.Collections.Generic;
using UnityEngine;

// Questions about how the rooms of a generated level connect: how many doors apart two rooms are,
// and which rooms are far from a given one. Rooms are indices; links are pairs of rooms joined by a door.
public static class LevelGraph
{
    // Doors to walk through from 'from' to every room. Unreachable rooms get int.MaxValue
    public static int[] DoorDistances(int roomCount, IReadOnlyList<Vector2Int> links, int from)
    {
        var distances = new int[roomCount];
        for (int i = 0; i < roomCount; i++) distances[i] = int.MaxValue;
        if (from < 0 || from >= roomCount) return distances;

        distances[from] = 0;
        var queue = new Queue<int>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            int room = queue.Dequeue();
            foreach (Vector2Int link in links)
            {
                int other = link.x == room ? link.y : link.y == room ? link.x : -1;
                if (other < 0 || distances[other] != int.MaxValue) continue;
                distances[other] = distances[room] + 1;
                queue.Enqueue(other);
            }
        }
        return distances;
    }

    // Every room except 'from', farthest first: most doors away, then farthest centre to centre
    public static List<int> RoomsByDistance(int roomCount, IReadOnlyList<Vector2Int> links, IReadOnlyList<RectInt> rooms, int from)
    {
        int[] doors = DoorDistances(roomCount, links, from);
        Vector2 origin = rooms[from].center;

        var order = new List<int>();
        for (int i = 0; i < roomCount; i++)
        {
            if (i != from && doors[i] != int.MaxValue) order.Add(i);
        }

        order.Sort((a, b) =>
        {
            if (doors[a] != doors[b]) return doors[b].CompareTo(doors[a]);
            float da = (rooms[a].center - origin).sqrMagnitude;
            float db = (rooms[b].center - origin).sqrMagnitude;
            return db.CompareTo(da);
        });
        return order;
    }

    // The room farthest from 'from', or 'from' itself when it is the only room
    public static int FarthestRoom(int roomCount, IReadOnlyList<Vector2Int> links, IReadOnlyList<RectInt> rooms, int from)
    {
        List<int> order = RoomsByDistance(roomCount, links, rooms, from);
        return order.Count > 0 ? order[0] : from;
    }
}
