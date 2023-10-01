using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TeleCore.Data.Events;
using TeleCore.Events;
using TeleCore.FlowCore.Events;
using TeleCore.Generics.Container;
using TeleCore.Primitive;
using UnityEngine;
using Verse;

namespace TeleCore.FlowCore;

public class FlowVolume<T> : IExposable, INotifyFlowEvent where T : FlowValueDef
{
    private FlowVolumeConfig<T> _config;
    private DefValueStack<T, double> _mainStack;
    private DefValueStack<T, double> _prevStack;
    private Color _totalColor;

    public Color Color => _totalColor;
    public DefValueStack<T, double> Stack => _mainStack;

    public DefValueStack<T, double> PrevStack
    {
        get => _prevStack;
        set => _prevStack = value;
    }

    public T MainValueDef => _mainStack.Values.MaxBy(c => (double)c.Value).Def;
    public IReadOnlyCollection<T> AllowedValues => _config.AllowedValues;

    public double FlowRate { get; set; }
    public double TotalValue => _mainStack.TotalValue;
    public virtual double MaxCapacity => _config.Volume;
    public float FillPercent => (float) (TotalValue / MaxCapacity);

    public bool Full => TotalValue >= MaxCapacity;
    public bool Empty => TotalValue <= Mathf.Epsilon;
    
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

    public FlowVolume()
    {
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref _prevStack, "previousStack");
        Scribe_Deep.Look(ref _mainStack, "mainStack");
    }
    
    public void PostLoadInit(FlowVolumeConfig<T> config)
    {
        _config = config;
        RegenColorState();
    }
    
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
        //TODO: add def specific capacity
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

    #region State Change

    public virtual void Notify_AddedValue(T valueType, double amount, double actual)
    {
        //Update stack state
        var delta = amount - actual;
        OnContainerStateChanged(delta);
    }

    public virtual void Notify_RemovedValue(T valueType, double amount, double actual)
    {
        //Update stack state
        var delta = amount - actual;
        OnContainerStateChanged(delta);
    }

    public void RegenColorState()
    {
        var newColor = Color.clear;
        foreach (var value in _mainStack)
        {
            newColor += (float) (value.Value.Value / MaxCapacity) * value.Def.valueColor;
        }

        //Note: Instead of dividing by the count of values here, could accumulate percentages
        newColor /= _mainStack.Length;
        _totalColor += newColor;
        _totalColor *= 0.5f;
        _totalColor = SaturateColor(_totalColor);
    }

    /// <summary>
    ///     Internal container state logic notifier.
    /// </summary>
    private void OnContainerStateChanged(double delta, bool updateMetaData = false)
    {
        //
        RegenColorState();

        //Resolve Action
        VolumeChangedEventArgs<T>.ChangedAction action = VolumeChangedEventArgs<T>.ChangedAction.Invalid;
        if(delta > 0)
            action = VolumeChangedEventArgs<T>.ChangedAction.AddedValue;
        else if(delta < 0)
            action = VolumeChangedEventArgs<T>.ChangedAction.RemovedValue;
        
        if (Empty && delta < 0)
            action = VolumeChangedEventArgs<T>.ChangedAction.Emptied;
        if (Full && delta > 0)
            action = VolumeChangedEventArgs<T>.ChangedAction.Filled;
        
        GlobalEventHandler.NetworkEvents<T>.OnVolumeStateChange(this, action);
    }

    private Color SaturateColor(Color color)
    {
        var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        var avg = (color.r + color.g + color.b) / 3;

        if (avg > max)
        {
            return new Color(color.r / max, color.g / max, color.b / max);
        }
        
        var k = min != 0 ? avg / min : 0;
        return new Color(color.r + ((color.r - avg) * k),
            color.g + ((color.g - avg) * k),
            color.b + ((color.b - avg) * k));
    }
    
    #endregion

    #region Manipulation Helpers

    #region FlowSystem

    //This stack stuff is tricky and mainly just for the FlowSystem
    
    public DefValueStack<T, double> RemoveContent(double moveAmount)
    {
        moveAmount = Math.Abs(moveAmount);
        if (moveAmount == 0) return DefValueStack<T, double>.Empty;
        if (_mainStack.IsEmpty) return DefValueStack<T, double>.Empty;

        var total = _mainStack.TotalValue;
        var finalStack = new DefValueStack<T, double>();
        foreach (var value in _mainStack.Values)
        {
            var rem = value.Value * moveAmount / total;
            finalStack += (value.Def, rem);
            _mainStack -= (value.Def, rem);
        }

        return finalStack;
    }
    
    public void AddContent(DefValueStack<T, double> fullDiff)
    {
        foreach (var value in fullDiff)
        {
            var step = TryAdd(value.Def, value.Value);
        }
    }

    #endregion
    
    public void LoadFromStack(DefValueStack<T, double> stack)
    {
        Clear();
        foreach (var defVal in stack)
            _ = TryAdd(defVal.Def, defVal.Value);
    }

    /// <summary>
    ///     Clears all values inside the container.
    /// </summary>
    public void Clear()
    {
        _mainStack = DefValueStack<T, double>.Empty;
        _totalColor = Color.white;
    }

    /// <summary>
    ///     Clears all values inside the container.
    /// </summary>
    public void Fill(int toCapacity)
    {
        var totalValue = toCapacity - TotalValue;
        if (toCapacity <= 0) return;

        var valuePerType = totalValue / AllowedValues.Count;
        foreach (var def in AllowedValues)
            _ = TryAdd(def, valuePerType);
    }

    #region Processor Methods

    public bool AllowedByFilter(T def)
    {
        //TODO: re-add filter
        return AllowedValues.Contains(def);
        //return filter.CanReceive(valueType);
    }
    
    private bool ValueOperationCheck(FlowOperation operation, DefValue<T, double> value, out FlowFailureReason reason)
    {
        reason = FlowFailureReason.None;
        
        if (!AllowedByFilter(value))
        {
            reason = FlowFailureReason.UsedForbiddenValueDef;
            return false;
        }
        
        if (operation == FlowOperation.Add)
        {
            if (IsFull(value))
            {
                reason = FlowFailureReason.TriedToAddToFull;
                return false;
            }
        }
        else if (operation == FlowOperation.Remove)
        {
            var stored = StoredValueOf(value);
            if (stored <= 0)
            {
                reason = FlowFailureReason.TriedToRemoveEmptyValue;
                return false;
            }
        }
        return true;
    }

    private static bool CanTransferTo(FlowVolume<T> other, T def, double value, out FlowFailureReason flowFailureReason)
    {
        flowFailureReason = FlowFailureReason.None;
        var total = other.TotalValue;
        var max = other.MaxCapacity;
        var expected = total + value;
        if (expected > max)
        {
            flowFailureReason = FlowFailureReason.TransferOverflow;
            return false;
        }
        if (expected < 0)
        {
            flowFailureReason = FlowFailureReason.TransferUnderflow;
            return false;
        }

        if (other.AllowedValues.Contains(def))
        {
            return true;
        }
        flowFailureReason = FlowFailureReason.UsedForbiddenValueDef;
        return false;
    }
    
    public FlowResult<T, double> TryAdd(FlowResult<T, double> prevResult)
    {
        return TryAdd(prevResult.Def, prevResult.Actual);
    }
    
    public bool TryAdd(T def, double value, out FlowResult<T, double> result)
    {
        result = TryAdd(def, value);
        return result;
    }

    // public FlowResult<T, double> TryAddOrFail(T def, double amount)
    // {
    //     
    // }

    /// <summary>
    /// Tries to add as much as possible from a value.
    /// </summary>
    public FlowResult<T, double> TryAdd(T def, double amount)
    {
        //Lazy sanity checks for failure
        if (!ValueOperationCheck(FlowOperation.Add, (def, amount), out var reason))
            return FlowResult<T, double>.InitFailed(def, amount, reason);

        var excessValue = Math.Max(TotalValue + amount - MaxCapacity, 0);
        var actual = amount - excessValue;

        //Note: Technically never possible as this implies a full container
        if (actual <= 0) 
            return FlowResult<T, double>.InitFailed(def, amount, FlowFailureReason.IllegalState);

        //Otherwise continue to add the value
        _mainStack += new DefValue<T, double>(def, actual);

        Notify_AddedValue(def, amount, actual); //Notify internal logic updates

        //On the result, set actual added value and resolve completion status
        return new FlowResult<T, double>(def, amount, actual);
    }

    //##################################################################################################################
    
    public bool TryRemove(T def, double value, out FlowResult<T, double> result)
    {
        result = TryRemove(def, value);
        return result;
    }
    
    public FlowResult<T, double> TryRemove(T def, double amount) //TValue valueDef, int value
    {
        //Lazy sanity checks for failure
        if (!ValueOperationCheck(FlowOperation.Remove, (def, amount), out var reason))
            return FlowResult<T, double>.InitFailed(def, amount, reason);


        var available = _mainStack[def];
        //Calculate the actual removeable value
        var actual = Math.Min(available.Value, amount);
        
        //Remove the value from the dictionary or update the value if there is still some left
        _mainStack -= (def, actual);

        //Notify internal cached data
        Notify_RemovedValue(def, amount, actual);

        //On the result, set actual removed value and resolve completion status
        return new FlowResult<T, double>(def, amount, actual);
    }
    
    /*public FlowResult<T, double> TryRemove(ICollection<DefValue<T, double>> values)
    {
    var result = new FlowResult<T, double>();
    foreach (var value in values)
    {
        var tmp = TryRemove(value);
        var val = tmp.FullDiff[0];
        result.AddDiff(val.Def, val.ValueInt);
    }

    return result.Resolve().Complete();
    }*/

    /// <summary>
    /// Tries to transfer a fixed DefValue, fails when the full amount cannot be transfered.
    /// </summary>
    public FlowResult<T, double> TryTransferOrFail(FlowVolume<T> other, DefValue<T, double> value) //ALL OR NOTHING
    {
        if (CanTransferTo(other, value.Def, value.Value, out FlowFailureReason reason))
        {
            //return TryTransfer(other, value);
            var removeResult = this.TryRemove(value.Def, value.Value);
            if (removeResult)
            {
                return other.TryAdd(removeResult.Def, removeResult.Actual);
            }
        }
        return FlowResult<T, double>.InitFailed(value.Def, value.Value, reason);
    }

    /// <summary>
    /// Tries to transfer as much as possible.
    /// </summary>
    public FlowResult<T, double> TryTransfer(FlowVolume<T> other, DefValue<T, double> value) //AS MUCH AS POSSIBLE
    {
        var remResult = TryRemove(value.Def, value.Value);
        if (remResult)
        {
            return other.TryAdd(remResult.Def, remResult.Actual);
        }
        return remResult;
    }

    #endregion

    /// <summary>
    /// Tries to consume a fixed amount, fails if there is not enough to consume.
    /// </summary>
    public FlowResult<T, double> TryConsumeOrFail(T def, double amount) //ALL OR NOTHING
    {
        if (StoredValueOf(def) >= amount) 
            return TryRemove(def, amount);
        return FlowResult<T, double>.InitFailed(def, amount, FlowFailureReason.TriedToConsumeMoreThanExists); //value.Value
    }
    
    /// <summary>
    /// Tries to consume as much as possible of the required amount.
    /// </summary>
    public FlowResult<T, double> TryConsume(T def, double amount) //AS MUCH AS POSSIBLE
    {
        return TryRemove(def, amount);
    }
    
    #endregion

    public override string ToString()
    {
        return $"[{TotalValue}/{MaxCapacity}][{Stack.Values.Count}/{AllowedValues.Count}]";
    }
}