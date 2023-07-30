using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.FlowCore.Events;
using TeleCore.Generics.Container;
using TeleCore.Network.Data;
using TeleCore.Primitive;

namespace TeleCore.FlowCore;

public class FlowVolume<T> : INotifyFlowEvent where T : FlowValueDef
{
    private readonly FlowVolumeConfig<T> _config;
    private DefValueStack<T, double> mainStack;
    private DefValueStack<T, double> prevStack;
    
    public DefValueStack<T, double> Stack => mainStack;
    
    public DefValueStack<T, double> PrevStack
    {
        get => prevStack;
        set => prevStack = value;
    }
    
    public IList<T> AllowedValues => _config.AllowedValues;
    
    public double FlowRate { get; set; }
    public double TotalValue => mainStack.TotalValue;
    public virtual double MaxCapacity => _config.Volume;
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

    public FlowVolume(FlowVolumeConfig<T> config)
    {
        _config = config;
    }
    
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
        moveAmount = Math.Abs(moveAmount);
        if (moveAmount == 0) return DefValueStack<T, double>.Empty;
        if(mainStack.IsEmpty) return DefValueStack<T, double>.Empty;
        
        var total = mainStack.TotalValue;
        var finalStack = new DefValueStack<T, double>();
        foreach (var value in mainStack.Values)
        {
            var rem = value.Value * moveAmount / total;
            finalStack += (value.Def, rem);
            mainStack -= (value.Def, rem);
        }
        return finalStack;
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
        var result = FlowResult<T, double>.Init(value);

        if (Full) return result.Fail();
        
        var expected = mainStack.TotalValue + value;
        if (expected > MaxCapacity)
        {
            var abundance = expected - MaxCapacity;
            var actual = value - abundance;
            var actualDefVal = (def, actual);
            mainStack += actualDefVal;
            return result.Complete(actualDefVal);
        }

        mainStack += (def, value);
        return FlowResult<T, double>.Init(value).Complete((def, value));
    }

    public FlowResult<T, double> TryRemove(T def, double value)
    {
        var result = FlowResult<T, double>.Init(value);   
        if (value > mainStack.TotalValue)
        {
            if(mainStack.IsEmpty) return result.Fail();
            var leftOver = value - mainStack.TotalValue;
            var final = mainStack;
            mainStack = DefValueStack<T, double>.Empty;
            return result.Complete(final);
        }
        
        mainStack -= (def, value);
        return result.Complete((def, value));
    }

    public FlowResult<T, double>  TryConsume(T def, double value)
    {
        return TryRemove(def, value);
    }
    
    public void Clear()
    {
        mainStack = new DefValueStack<T, double>();
    }

    #endregion
}