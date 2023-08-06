using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TeleCore;
using TeleCore.FlowCore;
using TeleCore.Network.Data;
using TeleCore.Primitive;
using UnityEngine;

namespace TeleTests;

[TestFixture]
[SetUpFixture]
public class FlowVolumeTests
{
    private static List<FlowVolume<FlowValueDef>> volumes;
    
    public static readonly FlowValueDef[] defs = new FlowValueDef[2]
    {
        new FlowValueDef
        {
            defName = "ValueA",
            label = "value A",
            labelShort = "a",
            valueUnit = "°",
            valueColor = Color.red,
            viscosity = 1,
        },
        new FlowValueDef
        {
            defName = "ValueB",
            label = "value B",
            labelShort = "b",
            valueUnit = "°",
            valueColor = Color.blue,
            viscosity = 1,
        },
    };

    public static FlowVolumeConfig<FlowValueDef> Config => new FlowVolumeConfig<FlowValueDef>
    {
        //allowedValues = new List<FlowValueDef>() { defs[0], defs[1]},
        capacity = 500,
    };
    
    

    [OneTimeSetUp]
    public void Setup()
    {
        volumes = new List<FlowVolume<FlowValueDef>>
        {
            new FlowVolume<FlowValueDef>(Config),
            new FlowVolume<FlowValueDef>(Config)
        };
    }

    [Test]
    public void AdditionTest()
    {
        var res1 = volumes[0].TryAdd(defs[0], 250);
        var res2 = volumes[0].TryAdd(defs[1], 250);
        
        Assert.AreEqual(250d ,res1.Desired.Value);
        Assert.AreEqual(250d ,res2.Desired.Value);
        Assert.IsTrue(res1.State == FlowState.Completed);
        Assert.IsTrue(res2.State == FlowState.Completed);
        
        Assert.AreEqual(250, res1.Actual[defs[0]].Value.Value);
        Assert.AreEqual(250, res2.Actual[defs[1]].Value.Value);
        
        Assert.AreEqual(250, volumes[0].StoredValueOf(defs[0]));
        Assert.AreEqual(250, volumes[0].StoredValueOf(defs[1]));
        Assert.AreEqual(500, volumes[0].TotalValue);
    }

    [Test]
    public void AdditionTest_AdhereToCpacity()
    {
        var volume = new FlowVolume<FlowValueDef>(new FlowVolumeConfig<FlowValueDef>
        {
            //allowedValues = defs.ToList(),
            capacity = 250,
        });
        
        Assert.AreEqual(250, volume.MaxCapacity);
        
        var res1 = volume.TryAdd(defs[0], 125);
        var res2 = volume.TryAdd(defs[1], 100);
        var res3 = volume.TryAdd(defs[1], 50);
        
        Assert.AreEqual(1.0d, volume.FillPercent);
        Assert.IsTrue(volume.Full);
        Assert.AreEqual(FlowState.CompletedWithExcess, res3.State);
        
        var res4 = volume.TryAdd(defs[0], 125);
        
        Assert.IsTrue(volume.Full);
        Assert.AreEqual(250, volume.TotalValue);
        Assert.AreEqual(FlowState.Failed, res4.State);
    }

    [Test]
    public void SubtractionTest_Expected()
    {
        const double initialValue = 250;
        const double expectedValue = 125;

        var volume = volumes[0];

        var addRes1 = volume.TryAdd(defs[0], initialValue);
        var addRes2 = volume.TryAdd(defs[1], initialValue);

        volume.TryRemove(defs[0], addRes1.Actual[defs[0]].Value / 2);
        volume.TryRemove(defs[1], addRes2.Actual[defs[1]].Value / 2);

        Assert.AreEqual(expectedValue, volume.StoredValueOf(defs[0]));
        Assert.AreEqual(expectedValue, volume.StoredValueOf(defs[1]));
        Assert.AreEqual(2 * expectedValue, volume.TotalValue);
    }

    [Test]
    public void SubtractionTest_Empty()
    {
        var subResEmpty = volumes[1].TryRemove(defs[0], 100);
       Assert.IsTrue(subResEmpty.Actual.IsEmpty && subResEmpty.State == FlowState.Failed);
    }
    
    [Test]
    public void ContentTest()
    {
        //Fill and remove
        var addRes1 = volumes[0].TryAdd(defs[0], 250);
        var remTest = volumes[0].RemoveContent(125);
        
        //Remove from empty
        var remTest2 = volumes[1].RemoveContent(100);
        
        Assert.AreEqual(125, volumes[0].TotalValue);
        Assert.IsTrue(remTest2.IsEmpty);
    }
}