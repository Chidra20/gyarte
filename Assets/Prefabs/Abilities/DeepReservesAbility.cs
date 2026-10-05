using UnityEngine;

// Placeholder: one more fire spell charge, given right away
[CreateAssetMenu(menuName = "gyarte/Abilities/Deep Reserves")]
public class DeepReservesAbility : Ability
{
    public int extraCharges = 1;

    public override void Apply(GameObject player)
    {
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack == null) return;
        attack.SetMaxSpellCharges(attack.maxSpellCharges + extraCharges, false);
        attack.AddSpellCharges(extraCharges);
    }
}
