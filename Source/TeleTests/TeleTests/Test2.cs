using System;
using NUnit.Framework;
using TeleCore;
using Verse;

namespace TeleTests;

[TestFixture]
public class Test2
{
    [Test]
    public void OperationTest()
    {
        Assert.IsTrue(1 + 1 == 2);
    }

    [Test]
    public void FloatControllerTest1()
    {
        var controller = new FloatController(10, 1);
        Console.WriteLine(controller.ToString());
        Assert.IsTrue(controller.CurState == FloatController.FCState.Idle);
        
        controller.Start();
        Console.WriteLine(controller.ToString());
        Assert.IsTrue(controller.CurState == FloatController.FCState.Accelerating);
        for (int i = 0; i < 60; i++)
        {
            controller.Tick();
        }
        Console.WriteLine(controller.ToString());
        Assert.IsTrue(controller.ReachedPeak);
        
        controller.Stop();
        Console.WriteLine(controller.ToString());
        Assert.IsTrue(controller.CurState == FloatController.FCState.Decelerating);
    }
}