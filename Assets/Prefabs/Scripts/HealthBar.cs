using UnityEngine;
using UnityEngine.UI;

// Shows the player's health as a bar. The fill shrinks quickly when hurt; a lighter "trail" behind it
// holds the old amount for a moment and then drains, so you can see how much a hit took.
// Healing grows the fill directly.
public class HealthBar : MonoBehaviour
{
    [Tooltip("Whose health to show. The player (by tag) when empty.")]
    public Health health;
    [Tooltip("Image set to Filled / Horizontal: the red line.")]
    public Image fill;
    [Tooltip("Optional. Same shape as the fill, drawn behind it in a lighter color.")]
    public Image trail;

    [Header("Animation")]
    [Tooltip("How fast the fill moves to the new amount (fraction of the bar per second).")]
    public float fillSpeed = 4f;
    [Tooltip("How long the trail waits before draining.")]
    public float trailDelay = 0.35f;
    [Tooltip("How fast the trail drains (fraction of the bar per second).")]
    public float trailSpeed = 0.8f;

    private float target = 1f;
    private float trailWait;

    void Start()
    {
        TryConnect();
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= Show;
    }

    void Update()
    {
        if (health == null) TryConnect();
        if (fill == null) return;

        // Unscaled, so the bar finishes moving even if the game is paused right after a hit
        float dt = Time.unscaledDeltaTime;
        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, fillSpeed * dt);

        if (trail != null)
        {
            if (trail.fillAmount < fill.fillAmount)
            {
                trail.fillAmount = fill.fillAmount; // healing: the trail never sits below the fill
            }
            else if (trailWait > 0f)
            {
                trailWait -= dt;
            }
            else
            {
                trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * dt);
            }
        }
    }

    void TryConnect()
    {
        if (health == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) health = player.GetComponent<Health>();
        }
        if (health == null) return;

        health.Changed -= Show;
        health.Changed += Show;

        // Start full and in place, no animation
        target = health.maxHealth > 0 ? (float)health.CurrentHealth / health.maxHealth : 0f;
        if (fill != null) fill.fillAmount = target;
        if (trail != null) trail.fillAmount = target;
    }

    void Show(int current, int max)
    {
        float newTarget = max > 0 ? (float)current / max : 0f;
        if (newTarget < target) trailWait = trailDelay; // took damage: hold the trail for a moment
        target = newTarget;
    }
}
