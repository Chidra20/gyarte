using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    // An ability that only counts how often it was applied
    class CountingAbility : Ability
    {
        public int applied;
        public override void Apply(GameObject player) { applied++; }
    }

    GameObject go;
    Inventory inventory;
    readonly List<Object> created = new List<Object>();

    CountingAbility MakeAbility(string name, int maxStacks = 3)
    {
        var ability = ScriptableObject.CreateInstance<CountingAbility>();
        ability.displayName = name;
        ability.maxStacks = maxStacks;
        created.Add(ability);
        return ability;
    }

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("InventoryTest");
        inventory = go.AddComponent<Inventory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        foreach (var o in created) Object.DestroyImmediate(o);
        created.Clear();
    }

    [Test]
    public void Keys_AddAndUse()
    {
        inventory.AddKey();
        Assert.AreEqual(1, inventory.Keys);

        Assert.IsTrue(inventory.TryUseKey());
        Assert.AreEqual(0, inventory.Keys);
        Assert.IsFalse(inventory.TryUseKey());
    }

    [Test]
    public void AddAbility_StacksAndApplies()
    {
        var ability = MakeAbility("Test");
        int changes = 0;
        inventory.Changed += () => changes++;

        inventory.AddAbility(ability);
        inventory.AddAbility(ability);

        Assert.AreEqual(2, inventory.StackCount(ability));
        Assert.AreEqual(2, ability.applied);
        Assert.AreEqual(2, changes);
    }

    [Test]
    public void Draw_ReturnsDistinct()
    {
        var pool = new List<Ability> { MakeAbility("A"), MakeAbility("B"), MakeAbility("C"), MakeAbility("D") };

        List<Ability> hand = AbilityPool.Draw(pool, a => 0, 3, new System.Random(1));

        Assert.AreEqual(3, hand.Count);
        CollectionAssert.AllItemsAreUnique(hand);
    }

    [Test]
    public void Draw_SkipsMaxedAndShortensHand()
    {
        var a = MakeAbility("A", 1);
        var b = MakeAbility("B", 1);
        var pool = new List<Ability> { a, b, MakeAbility("C"), MakeAbility("D") };

        List<Ability> hand = AbilityPool.Draw(pool, x => x == a || x == b ? 1 : 0, 3, new System.Random(1));
        Assert.AreEqual(2, hand.Count);
        CollectionAssert.DoesNotContain(hand, a);
        CollectionAssert.DoesNotContain(hand, b);

        List<Ability> empty = AbilityPool.Draw(pool, x => 99, 3, new System.Random(1));
        Assert.AreEqual(0, empty.Count);
    }
}
