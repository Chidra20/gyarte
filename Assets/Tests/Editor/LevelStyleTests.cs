using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelStyleTests
{
    [Test]
    public void TileStamp_HeightAndIndexing()
    {
        var tiles = new TileBase[6];
        for (int i = 0; i < 6; i++) tiles[i] = ScriptableObject.CreateInstance<Tile>();
        try
        {
            var stamp = new TileStamp { width = 2, tiles = tiles };

            Assert.AreEqual(3, stamp.Height);
            Assert.AreEqual(new Vector2Int(2, 3), stamp.Size);
            // Row-major from the top: (x, rowFromTop)
            Assert.AreSame(tiles[0], stamp.At(0, 0));
            Assert.AreSame(tiles[3], stamp.At(1, 1));
            Assert.AreSame(tiles[4], stamp.At(0, 2));
        }
        finally
        {
            foreach (var t in tiles) Object.DestroyImmediate(t);
        }
    }

    [Test]
    public void TileStamp_EmptyIsSafe()
    {
        var stamp = new TileStamp();
        Assert.AreEqual(0, stamp.Height);
        Assert.IsNull(stamp.At(0, 0));
    }
}
