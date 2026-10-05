using NUnit.Framework;

public class ContactDamageTests
{
    [Test]
    public void ReadyToHit_FirstContactHitsAtOnce()
    {
        Assert.IsTrue(ContactDamage.ReadyToHit(float.NegativeInfinity, 0f, 1f));
    }

    [Test]
    public void ReadyToHit_WaitsForInterval()
    {
        Assert.IsFalse(ContactDamage.ReadyToHit(0f, 0.5f, 1f));
    }

    [Test]
    public void ReadyToHit_HitsAgainAfterInterval()
    {
        Assert.IsTrue(ContactDamage.ReadyToHit(0f, 1f, 1f));
    }
}
