using System;
using System.Collections.Generic;
using UnityEngine;

// What the player is carrying during a run: keys for the level's gate, and the abilities picked so far.
// Lives on the player, so a run's inventory is gone when the scene is left.
public class Inventory : MonoBehaviour
{
    public int Keys { get; private set; }
    public IReadOnlyDictionary<Ability, int> Abilities => abilities;

    // Fired whenever a key or an ability is added or used, for the HUD
    public event Action Changed;

    private readonly Dictionary<Ability, int> abilities = new Dictionary<Ability, int>();

    public void AddKey()
    {
        Keys++;
        Changed?.Invoke();
    }

    public bool TryUseKey()
    {
        if (Keys <= 0) return false;
        Keys--;
        Changed?.Invoke();
        return true;
    }

    // Takes one more stack of the ability and applies its effect to the player straight away
    public void AddAbility(Ability ability)
    {
        if (ability == null) return;
        abilities[ability] = StackCount(ability) + 1;
        ability.Apply(gameObject);
        Changed?.Invoke();
    }

    public int StackCount(Ability ability)
    {
        return ability != null && abilities.TryGetValue(ability, out int count) ? count : 0;
    }
}
