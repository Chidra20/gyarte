using NUnit.Framework;

public class SpikeTrapTests
{
    // hidden 2s, rising 0.3s, out 1s, retracting 0.3s = 3.6s cycle
    static readonly SpikeTrap.Timings T = new SpikeTrap.Timings { hidden = 2f, rising = 0.3f, outTime = 1f, retracting = 0.3f };

    [Test]
    public void PhaseAt_WalksThroughTheCycle()
    {
        Assert.AreEqual(SpikeTrap.Phase.Hidden, SpikeTrap.PhaseAt(0.5f, T));
        Assert.AreEqual(SpikeTrap.Phase.Rising, SpikeTrap.PhaseAt(2.1f, T));
        Assert.AreEqual(SpikeTrap.Phase.Out, SpikeTrap.PhaseAt(2.5f, T));
        Assert.AreEqual(SpikeTrap.Phase.Retracting, SpikeTrap.PhaseAt(3.4f, T));
        // wraps around
        Assert.AreEqual(SpikeTrap.Phase.Hidden, SpikeTrap.PhaseAt(3.7f, T));
        Assert.AreEqual(SpikeTrap.Phase.Out, SpikeTrap.PhaseAt(3.6f * 3 + 2.5f, T));
    }

    [Test]
    public void OnlyDangerousWhenOut()
    {
        Assert.IsFalse(SpikeTrap.IsDangerous(SpikeTrap.Phase.Hidden));
        Assert.IsFalse(SpikeTrap.IsDangerous(SpikeTrap.Phase.Rising));
        Assert.IsTrue(SpikeTrap.IsDangerous(SpikeTrap.Phase.Out));
        Assert.IsFalse(SpikeTrap.IsDangerous(SpikeTrap.Phase.Retracting));
    }

    [Test]
    public void FrameFollowsThePhase()
    {
        // 5 frames: 0 = holes, 4 = fully out; rising goes 1..3, retracting 3..1
        Assert.AreEqual(0, SpikeTrap.FrameAt(0.5f, T, 5));
        Assert.AreEqual(4, SpikeTrap.FrameAt(2.5f, T, 5));
        Assert.AreEqual(1, SpikeTrap.FrameAt(2.01f, T, 5));
        Assert.AreEqual(3, SpikeTrap.FrameAt(2.29f, T, 5));
        Assert.AreEqual(3, SpikeTrap.FrameAt(3.31f, T, 5));
    }
}
