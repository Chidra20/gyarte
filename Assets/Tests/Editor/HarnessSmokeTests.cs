using NUnit.Framework;

// Proves the test setup works: NUnit is visible here and gameplay scripts can be reached
public class HarnessSmokeTests
{
    [Test]
    public void CanSeeGameplayScripts()
    {
        Assert.IsNotNull(typeof(PlayerAttack));
    }
}
