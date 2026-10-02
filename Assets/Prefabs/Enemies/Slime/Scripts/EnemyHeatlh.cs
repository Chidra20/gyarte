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

    void Start()
    {
        currentHealth = maxHealth;
        bigSlime = GetComponent<BigSlime>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
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
