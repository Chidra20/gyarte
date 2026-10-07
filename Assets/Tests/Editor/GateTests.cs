using NUnit.Framework;
using UnityEngine;

public class GateTests
{
    // The stairs gate counts as entered only when the player stands on the steps (a 2x5 area centred on
    // the gate), not merely in front of the bars
    [Test]
    public void EnterArea_OnlyOnTheSteps()
    {
        var size = new Vector2(2f, 5f);
        Assert.IsTrue(Gate.InsideEnterArea(new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, size));
        Assert.IsFalse(Gate.InsideEnterArea(new Vector2(0f, -3f), Vector2.zero, Vector2.zero, size), "in front of the bars");
        Assert.IsFalse(Gate.InsideEnterArea(new Vector2(1.5f, 0f), Vector2.zero, Vector2.zero, size), "beside the steps");
    }

    [Test]
    public void EnterArea_NoAreaMeansAnywhereInTheTrigger()
    {
        Assert.IsTrue(Gate.InsideEnterArea(new Vector2(9f, 9f), Vector2.zero, Vector2.zero, Vector2.zero));
    }
}
