using NUnit.Framework;

public class GameLoopTests
{
    [Test]
    public void Transition_HappyPath()
    {
        var state = LoopState.Starting;
        state = GameLoop.Transition(state, "levelReady");
        Assert.AreEqual(LoopState.Fighting, state);
        state = GameLoop.Transition(state, "wavesCleared");
        Assert.AreEqual(LoopState.KeyHunt, state);
        state = GameLoop.Transition(state, "gateOpened");
        Assert.AreEqual(LoopState.GateOpen, state);
        Assert.AreEqual(LoopState.Choosing, GameLoop.Transition(state, "choose"));
        Assert.AreEqual(LoopState.Starting, GameLoop.Transition(state, "gateEntered"));
        Assert.AreEqual(LoopState.Starting, GameLoop.Transition(LoopState.Choosing, "choiceMade"));
    }

    [Test]
    public void Transition_IgnoresEventsOutOfOrder()
    {
        Assert.AreEqual(LoopState.Fighting, GameLoop.Transition(LoopState.Fighting, "gateEntered"));
        Assert.AreEqual(LoopState.KeyHunt, GameLoop.Transition(LoopState.KeyHunt, "choiceMade"));
    }

    [Test]
    public void GameLoop_DeathDuringChoosing()
    {
        Assert.AreEqual(LoopState.Dead, GameLoop.Transition(LoopState.Choosing, "died"));
        Assert.AreEqual(LoopState.Dead, GameLoop.Transition(LoopState.Dead, "choiceMade"));
        Assert.AreEqual(LoopState.Dead, GameLoop.Transition(LoopState.Dead, "levelReady"));
    }

    [Test]
    public void OffersChoice_EverySecondLevel()
    {
        Assert.IsFalse(GameLoop.OffersChoice(1, 2));
        Assert.IsTrue(GameLoop.OffersChoice(2, 2));
        Assert.IsFalse(GameLoop.OffersChoice(3, 2));
        Assert.IsTrue(GameLoop.OffersChoice(4, 2));
        Assert.IsFalse(GameLoop.OffersChoice(2, 0));
    }
}
