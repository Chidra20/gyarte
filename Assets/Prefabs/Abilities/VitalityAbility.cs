using UnityEngine;

// Placeholder: more maximum health, and heals by the same amount
[CreateAssetMenu(menuName = "gyarte/Abilities/Vitality")]
public class VitalityAbility : Ability
{
    public int extraMaxHealth = 2;
    public int heal = 2;

    public override void Apply(GameObject player)
    {
        Health health = player.GetComponent<Health>();
        if (health == null) return;
        health.SetMaxHealth(health.maxHealth + extraMaxHealth, false);
        health.Heal(heal);
    }
}
