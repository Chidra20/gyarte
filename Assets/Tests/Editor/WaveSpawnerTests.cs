using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WaveSpawnerTests
{
    static readonly Vector2 FarPlayer = new Vector2(100f, 100f);

    [Test]
    public void WaveCount_Grows()
    {
        Assert.AreEqual(2, WaveSpawner.WaveCount(1, 2, 0.5f, 6));
        Assert.AreEqual(3, WaveSpawner.WaveCount(2, 2, 0.5f, 6));
        Assert.AreEqual(6, WaveSpawner.WaveCount(20, 2, 0.5f, 6));
    }

    [Test]
    public void WaveSize_Grows()
    {
        Assert.AreEqual(3, WaveSpawner.WaveSize(1, 2, 1, 10));
        Assert.AreEqual(10, WaveSpawner.WaveSize(20, 2, 1, 10));
    }

    [Test]
    public void IsValidSpawn_RespectsPlayerDistance()
    {
        var none = new List<Vector2>();
        Assert.IsFalse(WaveSpawner.IsValidSpawn(new Vector2(5f, 0f), Vector2.zero, none, 6f, 2f, 5f, 3));
        Assert.IsTrue(WaveSpawner.IsValidSpawn(new Vector2(7f, 0f), Vector2.zero, none, 6f, 2f, 5f, 3));
    }

    [Test]
    public void IsValidSpawn_RespectsSpacingAndCrowd()
    {
        var close = new List<Vector2> { new Vector2(1.5f, 0f) };
        Assert.IsFalse(WaveSpawner.IsValidSpawn(Vector2.zero, FarPlayer, close, 6f, 2f, 5f, 3));

        var three = new List<Vector2> { new Vector2(3f, 0f), new Vector2(0f, 3f), new Vector2(-3f, 0f) };
        Assert.IsFalse(WaveSpawner.IsValidSpawn(Vector2.zero, FarPlayer, three, 6f, 2f, 5f, 3));

        var two = new List<Vector2> { new Vector2(3f, 0f), new Vector2(0f, 3f) };
        Assert.IsTrue(WaveSpawner.IsValidSpawn(Vector2.zero, FarPlayer, two, 6f, 2f, 5f, 3));
    }

    [Test]
    public void SpawnRules_GivesUpAfterAttempts()
    {
        int samples = 0;
        bool found = WaveSpawner.TryFindSpawn(() => { samples++; return Vector2.zero; }, p => false, 15, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(15, samples);
    }
}
