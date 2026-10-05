using System;
using System.Collections.Generic;
using UnityEngine;

// The abilities the choice screen draws its cards from. Add an ability asset to the list to make it appear.
[CreateAssetMenu(menuName = "gyarte/Ability Pool")]
public class AbilityPool : ScriptableObject
{
    public List<Ability> abilities = new List<Ability>();

    // Up to 'count' different abilities, leaving out any the player already has at maxStacks.
    // Fewer come back when fewer are left, none when everything is maxed.
    public static List<Ability> Draw(IList<Ability> pool, Func<Ability, int> stackCount, int count, System.Random rng)
    {
        var available = new List<Ability>();
        foreach (Ability ability in pool)
        {
            if (ability != null && !available.Contains(ability) && stackCount(ability) < ability.maxStacks) available.Add(ability);
        }

        // Shuffle, then take from the front
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        if (available.Count > count) available.RemoveRange(count, available.Count - count);
        return available;
    }
}
