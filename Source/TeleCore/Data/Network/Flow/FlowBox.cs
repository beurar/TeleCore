using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.Defs;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using TeleCore.Network.Flow.Values;
using UnityEngine;

namespace TeleCore.Network.Flow;

public interface INotifyFlowBoxEvent
{
    event FlowBoxEventHandler FlowBoxEvent;
    void OnFlowBoxEvent(FlowBoxEventArgs e);
}

public delegate void FlowBoxEventHandler(FlowBox sender, FlowBoxEventArgs e);

public class FlowBoxEventArgs : EventArgs
{
    public FlowValue Value { get; private set; }

    public FlowBoxEventArgs(FlowValue valueChange)
    {
        Value = valueChange;
    }
}

/// <summary>
/// The logical handler for fluid flow.
/// Area and height define the total content, elevation allows for flow control.
/// </summary>
public class FlowBox : INotifyFlowBoxEvent
{
    //
    private FlowBoxConfig _config;
    private double _flowRate;

    private FlowValueStack _mainStack;
    private FlowValueStack _prevStack;
    
    public event FlowBoxEventHandler? FlowBoxEvent;
    
    public FlowValueStack Stack => _mainStack;
    
    public FlowValueStack PrevStack
    {
        get => _prevStack;
        set => _prevStack = value;
    }

    public double FlowRate
    {
        get => _flowRate;
        set => _flowRate = value;
    }

    public double TotalValue => _mainStack.TotalValue;
    public double MaxCapacity => _config.Volume;

    public double FillHeight => (TotalValue / MaxCapacity) * _config.height;
    public double FillPercent => TotalValue /MaxCapacity;
    
    public ContainerFillState FillState
    {
        get
        {
            return TotalValue switch
            {
                0 => ContainerFillState.Empty,
                var n when n >= MaxCapacity => ContainerFillState.Full,
                _ => ContainerFillState.Partial
            };
        }
    }

    //TODO => Move into container config
    public IList<FlowValueDef> AcceptedTypes { get; set; }

    #region Data Getters
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CapacityOf(FlowValueDef? def)
    {
        return MaxCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double StoredValueOf(FlowValueDef? def)
    {
        if (def == null) return 0;
        return Stack[def].Value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double TotalStoredOfMany(IEnumerable<FlowValueDef> defs)
    {
        return defs.Sum(StoredValueOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(FlowValueDef def)
    {
        return (float) (StoredValueOf(def) / Math.Ceiling(CapacityOf(def)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull(FlowValueDef def)
    {
        if (def.sharesCapacity) return StoredValueOf(def) >= CapacityOf(def);
        return FillState == ContainerFillState.Full;
    }
    
    #endregion

    public FlowBox(FlowBoxConfig config)
    {
        _config = config;
    }

    #region Value Changing

    public void AddValue(FlowValue value)
    {
        _mainStack += value;
    }

    #endregion
    
    public FlowValueStack RemoveContent(double moveAmount)
    {
        var rem = _mainStack * moveAmount;
        _mainStack -= rem;
        return rem;
    }

    public void AddContent(FlowValueStack fullDiff)
    {
        _mainStack += fullDiff;
    }

    public void OnFlowBoxEvent(FlowBoxEventArgs e)
    {
        FlowBoxEvent?.Invoke(this, e);
    }

    public FlowValueResult TryAdd(FlowValueDef def, double value)
    {
        _mainStack += new FlowValue(def, value);
        return FlowValueResult.Init(value).Complete(value);
    }

    public FlowValueResult TryRemove(FlowValueDef def, double value)
    {
        _mainStack -= new FlowValue(def, value);
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