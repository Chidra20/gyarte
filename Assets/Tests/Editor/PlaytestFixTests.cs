using NUnit.Framework;
using UnityEngine;

// Tests for the changes made after the first playtest of the Demo loop
public class PlaytestFixTests
{
    [Test]
    public void Knockback_PushesAwayFromTheHitter()
    {
        Vector2 direction = EnemyKnockback.Direction(new Vector2(0f, 0f), new Vector2(3f, 0f), Vector2.up);
        Assert.AreEqual(1f, direction.x, 0.0001f);
        Assert.AreEqual(0f, direction.y, 0.0001f);
    }

    [Test]
    public void Knockback_SamePointUsesFallback()
    {
        Vector2 direction = EnemyKnockback.Direction(Vector2.one, Vector2.one, new Vector2(0f, -2f));
        Assert.AreEqual(Vector2.down, direction);
    }

    [Test]
    public void FpsSampler_AveragesOverTheWindow()
    {
        var sampler = new FpsSampler(0.5f);
        for (int i = 0; i < 50; i++) sampler.AddFrame(0.01f);

        Assert.AreEqual(100f, sampler.Fps, 0.5f);
        Assert.AreEqual(10f, sampler.FrameMs, 0.05f);
    }

    [Test]
    public void FpsSampler_TracksTheSlowestFrame()
    {
        var sampler = new FpsSampler(0.5f);
        for (int i = 0; i < 20; i++) sampler.AddFrame(0.01f);
        sampler.AddFrame(0.05f);
        for (int i = 0; i < 25; i++) sampler.AddFrame(0.01f);

        Assert.AreEqual(50f, sampler.WorstFrameMs, 0.05f);
    }

    [Test]
    public void Waves_SpawnInThePlayersRoom()
    {
        Assert.AreEqual(3, WaveSpawner.SpawnRoom(3, 0));
        // Standing in a doorway or outside every room: fall back to the start room
        Assert.AreEqual(0, WaveSpawner.SpawnRoom(-1, 0));
    }
}
