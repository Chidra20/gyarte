using NUnit.Framework;
using UnityEngine;

// Regression tests for the issues found in the final review of the game loop
public class ReviewFixTests
{
    [Test]
    public void LoopHud_BannerWorksBeforeStart()
    {
        // GameLoop.Start may run before LoopHud.Start and show the first banner straight away
        var go = new GameObject("HudTest");
        try
        {
            var hud = go.AddComponent<LoopHud>();
            hud.Awake();
            hud.ShowBanner("Level 1", 2f);
            Assert.AreEqual("Level 1", hud.BannerText);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ForceOpenGate_OnlyWhileTheLevelIsUnderway()
    {
        Assert.IsFalse(GameLoop.CanForceOpenGate(LoopState.Starting));
        Assert.IsTrue(GameLoop.CanForceOpenGate(LoopState.Fighting));
        Assert.IsTrue(GameLoop.CanForceOpenGate(LoopState.KeyHunt));
        Assert.IsFalse(GameLoop.CanForceOpenGate(LoopState.GateOpen));
        Assert.IsFalse(GameLoop.CanForceOpenGate(LoopState.Choosing));
        Assert.IsFalse(GameLoop.CanForceOpenGate(LoopState.Dead));
    }

    [Test]
    public void WavesThatEndedDuringATestChoiceAreNotLost()
    {
        Assert.IsTrue(GameLoop.WavesFinishedWhileAway(LoopState.Fighting, false, 2));
        Assert.IsFalse(GameLoop.WavesFinishedWhileAway(LoopState.Fighting, true, 2));
        Assert.IsFalse(GameLoop.WavesFinishedWhileAway(LoopState.KeyHunt, false, 2));
        Assert.IsFalse(GameLoop.WavesFinishedWhileAway(LoopState.Fighting, false, 0));
    }

    [Test]
    public void QuickScythe_NeverBelowTheSwingAnimation()
    {
        var go = new GameObject("ScytheTest");
        var ability = ScriptableObject.CreateInstance<QuickScytheAbility>();
        try
        {
            var attack = go.AddComponent<PlayerAttack>();
            attack.swingDuration = 0.44f;
            attack.meleeCooldown = 0.5f;

            ability.Apply(go);

            Assert.AreEqual(0.44f, attack.meleeCooldown, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ability);
        }
    }
}
