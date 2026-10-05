using UnityEngine;

// Placeholder: the player moves faster
[CreateAssetMenu(menuName = "gyarte/Abilities/Fleet Foot")]
public class FleetFootAbility : Ability
{
    [Tooltip("Added to the player's speed multiplier (0.15 = 15% faster than normal).")]
    public float extraSpeed = 0.15f;

    public override void Apply(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null) return;
        movement.speedMultiplier += extraSpeed;
    }
}
