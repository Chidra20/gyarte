using UnityEngine;

// Placeholder: the swing comes back sooner. Never shorter than the swing animation itself
[CreateAssetMenu(menuName = "gyarte/Abilities/Quick Scythe")]
public class QuickScytheAbility : Ability
{
    [Tooltip("The swing cooldown is multiplied by this.")]
    public float cooldownMultiplier = 0.8f;

    public override void Apply(GameObject player)
    {
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack == null) return;
        // The swing can't come back before its animation ends, so going lower would do nothing
        attack.meleeCooldown = Mathf.Max(attack.swingDuration, attack.meleeCooldown * cooldownMultiplier);
    }
}
