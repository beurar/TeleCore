using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.FlowCore.Events;
using TeleCore.Generics.Container;
using TeleCore.Primitive;

namespace TeleCore.FlowCore;

public abstract class FlowVolume<T> : INotifyFlowEvent where T : FlowValueDef
{
    protected DefValueStack<T, double> mainStack;
    protected DefValueStack<T, double> prevStack;

    public DefValueStack<T, double> Stack => mainStack;

    public DefValueStack<T, double> PrevStack
    {
        get => prevStack;
        set => prevStack = value;
    }

    public double FlowRate { get; set; }

    public double TotalValue => mainStack.TotalValue;
    public abstract double MaxCapacity { get; }

    public double FillPercent => TotalValue / MaxCapacity;

    public bool Full => TotalValue >= MaxCapacity;
    public bool Empty => TotalValue <= 0;
    
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

    public event FlowEventHandler? FlowEvent;

    #region EventHandling

    public void OnFlowEvent(FlowEventArgs e)
    {
        FlowEvent?.Invoke(this, e);
    }

    #endregion

    #region Data Getters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CapacityOf(T? def)
    {
        return MaxCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double StoredValueOf(T? def)
    {
        if (def == null) return Numeric<double>.Zero;
        return Stack[def].Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double TotalStoredOfMany(IEnumerable<T> defs)
    {
        return defs.Sum(StoredValueOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(T def)
    {
        return (float) (StoredValueOf(def) / Math.Ceiling(CapacityOf(def)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFull(T def)
    {
        if (def.sharesCapacity) return StoredValueOf(def) >= CapacityOf(def);
        return FillState == ContainerFillState.Full;
    }

    #endregion

    #region Manipulation Helpers
    
    public DefValueStack<T, double> RemoveContent(double moveAmount)
    {
        var rem = mainStack * moveAmount;
        mainStack -= rem;
        return rem;
    }

    public void AddContent(DefValueStack<T, double> fullDiff)
    {
        mainStack += fullDiff;
    }

    public void LoadFromStack(DefValueStack<T, double> stack)
    {
        mainStack = stack;
    }
    
    public FlowResult<T, double>  TryAdd(T def, double value)
    {
        mainStack += (def, value);
        return FlowResult<T, double>.Init(value).Complete((def, value)).Resolve();
    }

    public FlowResult<T, double> TryRemove(T def, double value)
    {
        mainStack -= (def, value);
        return FlowResult<T, double>.Init(-value).Complete((def, -value));
    }

    public FlowResult<T, double>  TryConsume(T def, double value)
    {
        return TryRemove(def, value);
    }
    
    public void Clear()
    {
        
    }

    #endregion
}