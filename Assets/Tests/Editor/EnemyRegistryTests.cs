using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemyRegistryTests
{
    readonly List<GameObject> created = new List<GameObject>();

    EnemyHealth MakeEnemy(int maxHealth = 2)
    {
        var go = new GameObject("EnemyTest");
        created.Add(go);
        var health = go.AddComponent<Health>();
        health.maxHealth = maxHealth;
        health.Awake();
        var enemy = go.AddComponent<EnemyHealth>();
        enemy.Awake(); // EditMode does not call Awake
        return enemy;
    }

    [SetUp]
    public void SetUp()
    {
        EnemyHealth.ResetRegistry();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
        EnemyHealth.ResetRegistry();
    }

    [Test]
    public void Registry_CountsAddAndRemove()
    {
        MakeEnemy();
        var second = MakeEnemy();
        Assert.AreEqual(2, EnemyHealth.AliveCount);

        Object.DestroyImmediate(second.gameObject);
        Assert.AreEqual(1, EnemyHealth.AliveCount);
    }

    [Test]
    public void Registry_SplitKeepsCountAboveZero()
    {
        var big = MakeEnemy(1);
        // Stands in for BigSlime.DieAndSplit: two babies appear while the big one dies
        big.onDeathOverride = () => { MakeEnemy(1); MakeEnemy(1); };

        int lowest = int.MaxValue;
        EnemyHealth.AliveCountChanged += Track;
        try
        {
            big.TakeDamage(1);
        }
        finally
        {
            EnemyHealth.AliveCountChanged -= Track;
        }

        Assert.AreEqual(2, EnemyHealth.AliveCount);
        Assert.Greater(lowest, 0);

        void Track() { lowest = Mathf.Min(lowest, EnemyHealth.AliveCount); }
    }

    [Test]
    public void TakeDamage_DelegatesToHealth()
    {
        var enemy = MakeEnemy(2);

        enemy.TakeDamage(1);

        Assert.AreEqual(1, enemy.GetComponent<Health>().CurrentHealth);
        Assert.AreEqual(1, EnemyHealth.AliveCount);
    }
}
