using System.Collections.Generic;
using NUnit.Framework;
using TeleCore.Defs;
using TeleCore.Network.Flow.Values;

namespace TeleTests;

[TestFixture]
public class MutabilityTests
{
    public static List<FlowValueDef> Defs;

    [SetUp]
    public void Setup()
    {
        Defs = new List<FlowValueDef>();
        Defs.Add(new FlowValueDef
        {
            defName = "ValueA",
            label = "val a",
            labelShort = "(a)",
            valueUnit = "p",
            viscosity = 1,
            capacityFactor = 1,
        });
        Defs.Add(new FlowValueDef
        {
            defName = "ValueB",
            label = "val b",
            labelShort = "(b)",
            valueUnit = "p",
            viscosity = 1,
            capacityFactor = 1,
        });
        Defs.Add(new FlowValueDef
        {
            defName = "ValueC",
            label = "val c",
            labelShort = "(c)",
            valueUnit = "p",
            viscosity = 1,
            capacityFactor = 1,
        });
    }
    
    [Test]
    public void FlowValueStackMutability()
    {
        FlowValueStack stack = new FlowValueStack();
        stack += new FlowValue(Defs[0], 1);
        stack += new FlowValue(Defs[1], 33);
        stack += new FlowValue(Defs[2], 66);
        var stck2 = stack + stack;
        
        Assert.AreNotEqual(stack, stck2);

    }
}