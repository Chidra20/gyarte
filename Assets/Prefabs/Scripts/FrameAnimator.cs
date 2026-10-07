using UnityEngine;

// Plays a list of sprites in a loop (torch and candle flames). Starts on a random frame so a row of
// torches doesn't flicker in step.
public class FrameAnimator : MonoBehaviour
{
    public SpriteRenderer target;
    public Sprite[] frames;
    public float framesPerSecond = 8f;
    public bool randomStart = true;

    private float clock;

    void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (randomStart && frames != null && frames.Length > 0) clock = Random.Range(0f, frames.Length / Mathf.Max(0.01f, framesPerSecond));
    }

    void Update()
    {
        if (target == null || frames == null || frames.Length == 0) return;
        clock += Time.deltaTime;
        int frame = Mathf.FloorToInt(clock * framesPerSecond) % frames.Length;
        target.sprite = frames[frame];
    }
}
