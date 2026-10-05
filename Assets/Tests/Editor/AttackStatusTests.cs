using NUnit.Framework;

public class AttackStatusTests
{
    [Test]
    public void StatusText_Ready()
    {
        Assert.AreEqual("Ready", AttackCorner.StatusText("", 0f));
    }

    [Test]
    public void StatusText_BusyHasNoTimer()
    {
        Assert.AreEqual("Busy", AttackCorner.StatusText("Busy", 0.4f));
    }

    [Test]
    public void StatusText_CooldownShowsSeconds()
    {
        Assert.AreEqual("Cooldown 0.4s", AttackCorner.StatusText("Cooldown", 0.35f));
    }

    [Test]
    public void StatusText_NoChargesShowsSeconds()
    {
        Assert.AreEqual("No charges 2.3s", AttackCorner.StatusText("No charges", 2.3f));
    }

    [Test]
    public void MeleeReason_ReadyWhenIdle()
    {
        Assert.AreEqual("", PlayerAttack.MeleeReason(false, 0f));
    }

    [Test]
    public void MeleeReason_CastingIsBusyEvenOffCooldown()
    {
        Assert.AreEqual("Busy", PlayerAttack.MeleeReason(true, 0f));
    }

    [Test]
    public void MeleeReason_Cooldown()
    {
        Assert.AreEqual("Cooldown", PlayerAttack.MeleeReason(false, 0.2f));
    }

    [Test]
    public void SpellReason_BusyBeatsNoCharges()
    {
        Assert.AreEqual("Busy", PlayerAttack.SpellReason(true, false, 0));
        Assert.AreEqual("Busy", PlayerAttack.SpellReason(false, true, 1));
    }

    [Test]
    public void SpellReason_NoChargesAndReady()
    {
        Assert.AreEqual("No charges", PlayerAttack.SpellReason(false, false, 0));
        Assert.AreEqual("", PlayerAttack.SpellReason(false, false, 2));
    }
}
