using System;
using UnityEngine;

// Health for anything that can be hurt: the player and every enemy.
// It only counts and reports; what a hit or a death means is decided by whoever listens to the events.
public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    [Tooltip("Seconds of immunity after taking a hit. 0 for enemies, so every attack lands.")]
    public float invulnerableTime = 0f;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => lastHitTime > float.NegativeInfinity && clock() < lastHitTime + invulnerableTime;
    // Set from the Test Menu: hits are ignored entirely
    public bool GodMode { get; set; }

    // current, max
    public event Action<int, int> Changed;
    public event Action Damaged;
    public event Action Died;

    // Tests replace this so the invulnerability window can be checked without waiting
    [NonSerialized] public Func<float> clock = () => Time.time;

    private float lastHitTime = float.NegativeInfinity;

    // Public so EditMode tests can call it. Awake, not Start: an enemy spawned by a merge
    // can be hit in the frame it appears, before Start has run
    public void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // Returns true when the hit counted
    public bool TakeDamage(int amount)
    {
        // Destroy only happens at the end of the frame, so a second hit in the same frame
        // must not kill the same thing twice
        if (IsDead || GodMode || amount <= 0 || IsInvulnerable) return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        lastHitTime = clock();
        Changed?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            IsDead = true;
            Died?.Invoke();
        }
        else
        {
            Damaged?.Invoke();
        }
        return true;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Changed?.Invoke(CurrentHealth, maxHealth);
    }

    // For the Test Menu: sets health directly, between 1 and max, so it can never kill
    public void SetCurrentHealth(int value)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Clamp(value, 1, maxHealth);
        Changed?.Invoke(CurrentHealth, maxHealth);
    }

    // refill: start at full health; otherwise keep current health, capped at the new max
    public void SetMaxHealth(int amount, bool refill)
    {
        maxHealth = Mathf.Max(1, amount);
        CurrentHealth = refill ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
        Changed?.Invoke(CurrentHealth, maxHealth);
    }
}
