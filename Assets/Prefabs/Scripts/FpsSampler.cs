using System.Collections.Generic;

// Averages frame times over a short sliding window: frames per second, milliseconds per frame,
// and the slowest single frame in the window (a hitch shows there even when the average looks fine).
public class FpsSampler
{
    public float Fps { get; private set; }
    public float FrameMs { get; private set; }
    public float WorstFrameMs { get; private set; }

    private readonly float window;
    private readonly Queue<float> frames = new Queue<float>();
    private float total;

    // window: seconds of frames to average over
    public FpsSampler(float window)
    {
        this.window = window;
    }

    // seconds: how long the last frame took (real time, not slowed by pause or game speed)
    public void AddFrame(float seconds)
    {
        if (seconds <= 0f) return;

        frames.Enqueue(seconds);
        total += seconds;
        while (frames.Count > 1 && total - frames.Peek() >= window) total -= frames.Dequeue();

        Fps = frames.Count / total;
        FrameMs = total / frames.Count * 1000f;

        float worst = 0f;
        foreach (float frame in frames) if (frame > worst) worst = frame;
        WorstFrameMs = worst * 1000f;
    }
}
