using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 2;
    private int currentHealth;

    [Header("Hit Flash")]
    public Color hitFlashColor = new Color(1f, 0.45f, 0.3f, 1f);
    public float hitFlashTime = 0.12f;

    private BigSlime bigSlime;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead;

    // Awake, not Start: a slime spawned by a merge can be hit in the same frame it appears,
    // before Start has run, and would otherwise have 0 health
    void Awake()
    {
        currentHealth = maxHealth;
        bigSlime = GetComponent<BigSlime>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // Gives this enemy a different amount of health than its prefab has, at full health
    public void SetMaxHealth(int amount)
    {
        maxHealth = Mathf.Max(1, amount);
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // Destroy only happens at the end of the frame, so a second hit in the same frame
        // (a fireball and a swing landing together) would make a big slime split twice
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage! HP remaining: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Getting hit makes the slime notice the player, even from behind
        if (bigSlime != null) bigSlime.Alert();

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlash());
        }
    }

    IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        isDead = true;

        if (bigSlime != null)
        {
            // Triggers the split into 2 baby slimes and destroys this big slime
            bigSlime.DieAndSplit();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
