using NUnit.Framework;
using UnityEngine;

public class HealthTests
{
    GameObject go;
    Health health;
    float now;

    [SetUp]
    public void SetUp()
    {
        now = 0f;
        go = new GameObject("HealthTest");
        health = go.AddComponent<Health>();
        health.maxHealth = 3;
        health.clock = () => now;
        health.Awake(); // EditMode does not call Awake
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    [Test]
    public void StartsFull()
    {
        Assert.AreEqual(3, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);
    }

    [Test]
    public void DamageLowersAndRaisesChanged()
    {
        int seenCurrent = -1, seenMax = -1;
        health.Changed += (current, max) => { seenCurrent = current; seenMax = max; };

        Assert.IsTrue(health.TakeDamage(1));

        Assert.AreEqual(2, health.CurrentHealth);
        Assert.AreEqual(2, seenCurrent);
        Assert.AreEqual(3, seenMax);
    }

    [Test]
    public void DiesOnceAtZero()
    {
        int deaths = 0;
        health.Died += () => deaths++;

        Assert.IsTrue(health.TakeDamage(5));
        Assert.IsFalse(health.TakeDamage(5));

        Assert.AreEqual(1, deaths);
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual(0, health.CurrentHealth);
    }

    [Test]
    public void InvulnerableWindowBlocksHits()
    {
        health.invulnerableTime = 1f;

        now = 0f;
        Assert.IsTrue(health.TakeDamage(1));
        now = 0.5f;
        Assert.IsTrue(health.IsInvulnerable);
        Assert.IsFalse(health.TakeDamage(1));
        Assert.AreEqual(2, health.CurrentHealth);
        now = 1.01f;
        Assert.IsTrue(health.TakeDamage(1));
        Assert.AreEqual(1, health.CurrentHealth);
    }

    [Test]
    public void GodModeBlocksDamage()
    {
        health.GodMode = true;

        Assert.IsFalse(health.TakeDamage(1));
        Assert.AreEqual(3, health.CurrentHealth);
    }

    [Test]
    public void HealCapsAtMax()
    {
        health.TakeDamage(2);
        health.Heal(10);

        Assert.AreEqual(3, health.CurrentHealth);
    }

    [Test]
    public void SetMaxHealthRefills()
    {
        health.SetMaxHealth(5, true);
        Assert.AreEqual(5, health.maxHealth);
        Assert.AreEqual(5, health.CurrentHealth);

        health.SetMaxHealth(0, false);
        Assert.AreEqual(1, health.maxHealth);
        Assert.AreEqual(1, health.CurrentHealth);
    }

    [Test]
    public void SetCurrentHealth_ClampsAndNeverKills()
    {
        health.SetCurrentHealth(0);
        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);

        health.SetCurrentHealth(99);
        Assert.AreEqual(3, health.CurrentHealth);
    }
}
