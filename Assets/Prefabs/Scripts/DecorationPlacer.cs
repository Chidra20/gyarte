using System.Collections.Generic;
using UnityEngine;

// Where in a room a decoration with a given footprint may go, for each placement rule.
// Returns every candidate area (in cells); the level randomizer then picks randomly among
// the ones that are still free.
public static class DecorationPlacer
{
    public static List<RectInt> Candidates(Placement rule, RectInt room, Vector2Int footprint)
    {
        var spots = new List<RectInt>();
        int w = footprint.x, h = footprint.y;
        switch (rule)
        {
            case Placement.AgainstTopWall:
                for (int x = room.xMin; x + w <= room.xMax; x++) spots.Add(new RectInt(x, room.yMax - h, w, h));
                break;

            case Placement.Corner:
                spots.Add(new RectInt(room.xMin, room.yMin, w, h));
                spots.Add(new RectInt(room.xMax - w, room.yMin, w, h));
                spots.Add(new RectInt(room.xMin, room.yMax - h, w, h));
                spots.Add(new RectInt(room.xMax - w, room.yMax - h, w, h));
                break;

            case Placement.AlongWall:
                for (int x = room.xMin; x + w <= room.xMax; x++)
                {
                    spots.Add(new RectInt(x, room.yMin, w, h));
                    spots.Add(new RectInt(x, room.yMax - h, w, h));
                }
                for (int y = room.yMin + 1; y + h <= room.yMax - 1; y++)
                {
                    spots.Add(new RectInt(room.xMin, y, w, h));
                    spots.Add(new RectInt(room.xMax - w, y, w, h));
                }
                break;

            case Placement.Anywhere:
                AddInterior(spots, room, w, h, 2, 2);
                break;

            case Placement.Centre:
                AddInterior(spots, room, w, h, Mathf.Max(2, room.width / 4), Mathf.Max(2, room.height / 4));
                break;

            case Placement.OnTopWall:
                // The wall row itself, one cell in from each corner piece
                for (int x = room.xMin + 1; x + w <= room.xMax - 1; x++) spots.Add(new RectInt(x, room.yMax, w, h));
                break;
        }
        return spots;
    }

    static void AddInterior(List<RectInt> spots, RectInt room, int w, int h, int marginX, int marginY)
    {
        for (int x = room.xMin + marginX; x + w <= room.xMax - marginX; x++)
            for (int y = room.yMin + marginY; y + h <= room.yMax - marginY; y++)
                spots.Add(new RectInt(x, y, w, h));
    }
}
