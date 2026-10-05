using System;
using UnityEngine;

// The level's key. Touching it puts it in the player's inventory.
[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    public event Action PickedUp;

    private bool taken;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag("Player")) return;

        Inventory inventory = other.GetComponent<Inventory>();
        if (inventory == null) return;

        taken = true;
        inventory.AddKey();
        PickedUp?.Invoke();
        Destroy(gameObject);
    }
}
