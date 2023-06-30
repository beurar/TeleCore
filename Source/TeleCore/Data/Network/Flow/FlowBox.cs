using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.Defs;
using TeleCore.FlowCore;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using TeleCore.Network.Flow.Values;
using UnityEngine;

namespace TeleCore.Network.Flow;

/// <summary>
/// The logical handler for fluid flow.
/// Area and height define the total content, elevation allows for flow control.
/// </summary>
public class FlowBox : FlowVolume<NetworkValueDef>
{
    private FlowBoxConfig _config;
    
    public override double MaxCapacity => _config.Volume;

    public double FillHeight => (TotalValue / MaxCapacity) * _config.height;

    //TODO => Move into container config
    public IList<FlowValueDef> AcceptedTypes { get; set; }

    public FlowBox(FlowBoxConfig config)
    {
        _config = config;
    }
    
    public FlowValueResult TryAdd(FlowValueDef def, double value)
    {
        mainStack += new FlowValue(def, value);
        return FlowValueResult.Init(value).Complete(value);
    }

    public FlowValueResult TryRemove(FlowValueDef def, double value)
    {
        mainStack -= new FlowValue(def, value);
        return FlowValueResult.Init(-value).Complete(-value);
    }

    public FlowValueResult TryConsume(NetworkValueDef def, double value)
    {
        return TryRemove(def, value);
    }
    
    public void Clear()
    {
        
    }
}