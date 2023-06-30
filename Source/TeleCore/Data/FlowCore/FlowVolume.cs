using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.Defs;
using TeleCore.Generics.Container;
using TeleCore.Network.Flow;
using TeleCore.Network.Flow.Values;

namespace TeleCore.FlowCore;

public interface INotifyFlowEvent
{
    event FlowEventHandler FlowEvent;
    void OnFlowEvent(FlowEventArgs e);
}

public delegate void FlowEventHandler(object sender, FlowEventArgs e);

public class FlowEventArgs : EventArgs
{
    public FlowValue Value { get; private set; }

    public FlowEventArgs(FlowValue valueChange)
    {
        Value = valueChange;
    }
}

public abstract class FlowVolume<T> : INotifyFlowEvent where T: FlowValueDef
{
    private double _flowRate;

    protected FlowValueStack mainStack;
    protected FlowValueStack prevStack;
    
    public event FlowEventHandler? FlowEvent;
    
    public FlowValueStack Stack => mainStack;
    
    public FlowValueStack PrevStack
    {
        get => prevStack;
        set => prevStack = value;
    }

    public double FlowRate
    {
        get => _flowRate;
        set => _flowRate = value;
    }
    
    public double TotalValue => mainStack.TotalValue;
    public abstract double MaxCapacity { get; }
    
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
    
    #region Manipulation Helpers

    public FlowValueStack RemoveContent(double moveAmount)
    {
        var rem = mainStack * moveAmount;
        mainStack -= rem;
        return rem;
    }

    public void AddContent(FlowValueStack fullDiff)
    {
        mainStack += fullDiff;
    }


    #endregion

    #region EventHandling

    public void OnFlowEvent(FlowEventArgs e)
    {
        FlowEvent?.Invoke(this, e);
    }

    #endregion
}