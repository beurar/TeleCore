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

public abstract class FlowVolumeBase<TValue> : IExposable, INotifyFlowEvent where TValue : FlowValueDef
{
    protected FlowVolumeConfig<TValue> _config;
    protected DefValueStack<TValue, double> _mainStack;
    protected DefValueStack<TValue, double> _prevStack;
    protected Color _totalColor;

    public Color Color => _totalColor;
    public DefValueStack<TValue, double> Stack => _mainStack;

    public DefValueStack<TValue, double> PrevStack
    {
        get => _prevStack;
        set => _prevStack = value;
    }

    public FlowVolumeConfig<TValue> Config => _config;
    
    public TValue MainValueDef => _mainStack.Values.MaxBy(c => (double)c.Value).Def;
    public IReadOnlyCollection<TValue> AllowedValues => _config.AllowedValues;

    public double FlowRate { get; set; }
    public double TotalValue => _mainStack.TotalValue;
    public float FillPercent => (float)(TotalValue / MaxCapacity);

    public bool Full => TotalValue >= MaxCapacity;
    public bool Empty => TotalValue <= Mathf.Epsilon;

    #region Extendable

    public virtual double MaxCapacity => _config.Volume;
    public virtual double CapacityPerType => _config.Volume;

    #endregion
    
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
    
    public FlowVolumeBase()
    {
    }
    
    public FlowVolumeBase(FlowVolumeConfig<TValue> config)
    {
        _config = config;
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref _prevStack, "previousStack");
        Scribe_Deep.Look(ref _mainStack, "mainStack");
    }
    
    public void PostLoadInit(FlowVolumeConfig<TValue> config)
    {
        _config = config;
        RegenColorState();
    }

    #region EventHandling

    public void OnFlowEvent(FlowEventArgs e)
    {
        FlowEvent?.Invoke(this, e);
    }

    #endregion

    #region Data Getters

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual double CapacityOf(TValue? def)
    {
        //TODO: add def specific capacity
        return CapacityPerType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double StoredValueOf(TValue? def)
    {
        if (def == null) return Numeric<double>.Zero;
        return Stack[def].Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double TotalStoredOfMany(IEnumerable<TValue> defs)
    {
        return defs.Sum(StoredValueOf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float StoredPercentOf(TValue def)
    {
        return (float) (StoredValueOf(def) / Math.Ceiling(CapacityOf(def)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool IsFull(TValue def)
    {
        return FillState == ContainerFillState.Full;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual double ExcessFor(TValue def, double amount)
    {
        return Math.Max(TotalValue + amount - CapacityPerType, 0);
    }

    #endregion

    #region State Change

    public virtual void Notify_AddedValue(TValue valueType, double amount, double actual)
    {
        //Update stack state
        var delta = amount - actual;
        OnContainerStateChanged(delta);
    }

    public virtual void Notify_RemovedValue(TValue valueType, double amount, double actual)
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
    /// Internal container state logic notifier.
    /// </summary>
    private void OnContainerStateChanged(double delta, bool updateMetaData = false)
    {
        //
        RegenColorState();

        //Resolve Action
        VolumeChangedEventArgs<TValue>.ChangedAction action = VolumeChangedEventArgs<TValue>.ChangedAction.Invalid;
        if(delta > 0)
            action = VolumeChangedEventArgs<TValue>.ChangedAction.AddedValue;
        else if(delta < 0)
            action = VolumeChangedEventArgs<TValue>.ChangedAction.RemovedValue;
        
        if (Empty && delta < 0)
            action = VolumeChangedEventArgs<TValue>.ChangedAction.Emptied;
        if (Full && delta > 0)
            action = VolumeChangedEventArgs<TValue>.ChangedAction.Filled;
        
        GlobalEventHandler.NetworkEvents<TValue>.OnVolumeStateChange(this, action);
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

    public FlowResultStack<TValue> TryTake(DefValueStack<TValue, double> stack)
    {
        var result = FlowResultStack<TValue>.Init(stack, FlowOperation.Remove);
        foreach (var value in stack)
        {
            var subResult = TryRemove(value.Def, value.Value);
            if (subResult)
            {
                result.AddResult(subResult);
            }
        }
        return result;
    }
    
    public FlowResultStack<TValue> TryInsert(DefValueStack<TValue, double> stack)
    {
        var result = FlowResultStack<TValue>.Init(stack, FlowOperation.Add);
        foreach (var value in stack)
        {
            var subResult = TryAdd(value.Def, value.Value);
            if (subResult)
            {
                result.AddResult(subResult);
            }
        }
        return result;
    }
    
    //This stack stuff is tricky and mainly just for the FlowSystem
    public DefValueStack<TValue, double> RemoveContent(double moveAmount)
    {
        moveAmount = Math.Abs(moveAmount);
        if (moveAmount == 0) return DefValueStack<TValue, double>.Empty;
        if (_mainStack.IsEmpty) return DefValueStack<TValue, double>.Empty;

        var total = _mainStack.TotalValue;
        var finalStack = new DefValueStack<TValue, double>();
        foreach (var value in _mainStack.Values)
        {
            var rem = value.Value * moveAmount / total;
            finalStack += (value.Def, rem);
            _mainStack -= (value.Def, rem);
        }

        return finalStack;
    }
    
    public void AddContent(DefValueStack<TValue, double> fullDiff)
    {
        foreach (var value in fullDiff)
        {
            var step = TryAdd(value.Def, value.Value);
        }
    }

    #endregion

    public void SetDirect(DefValueStack<TValue, double> stack)
    {
        _mainStack = stack;
        RegenColorState();
    }
    
    public void LoadFromStack(DefValueStack<TValue, double> stack)
    {
        Clear();
        foreach (var defVal in stack)
        {
            _ = TryAdd(defVal.Def, defVal.Value);
        }
    }

    /// <summary>
    /// Clears all values inside the container.
    /// </summary>
    public void Clear()
    {
        _mainStack = DefValueStack<TValue, double>.Empty;
        _totalColor = Color.white;
    }

    /// <summary>
    /// Clears all values inside the container.
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

    public bool AllowedByFilter(TValue def)
    {
        //TODO: re-add filter
        return AllowedValues.Contains(def);
        //return filter.CanReceive(valueType);
    }
    
    private bool ValueOperationCheck(FlowOperation operation, DefValue<TValue, double> value, out FlowFailureReason reason)
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

    private static bool CanTransferTo(FlowVolumeBase<TValue> other, TValue def, double value, out FlowFailureReason flowFailureReason)
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
    
    public FlowResult<TValue, double> TryAdd(FlowResult<TValue, double> prevResult)
    {
        return TryAdd(prevResult.Def, prevResult.Actual);
    }
    
    public bool TryAdd(TValue def, double value, out FlowResult<TValue, double> result)
    {
        result = TryAdd(def, value);
        return result;
    }
    
    /// <summary>
    /// Tries to add as much as possible from a value.
    /// </summary>
    public FlowResult<TValue, double> TryAdd(TValue def, double amount)
    {
        if(_config.infiniteSource) 
            return new FlowResult<TValue, double>(def, amount, amount);
        
        //Lazy sanity checks for failure
        if (!ValueOperationCheck(FlowOperation.Add, (def, amount), out var reason))
            return FlowResult<TValue, double>.InitFailed(def, amount, reason);

        var actual = amount - ExcessFor(def, amount);

        //Note: Technically never possible as this implies a full container
        if (actual <= 0) 
            return FlowResult<TValue, double>.InitFailed(def, amount, FlowFailureReason.IllegalState);

        //Otherwise continue to add the value
        _mainStack += new DefValue<TValue, double>(def, actual);

        Notify_AddedValue(def, amount, actual); //Notify internal logic updates

        //On the result, set actual added value and resolve completion status
        return new FlowResult<TValue, double>(def, amount, actual);
    }

    //##################################################################################################################
    
    public bool TryRemove(TValue def, double value, out FlowResult<TValue, double> result)
    {
        result = TryRemove(def, value);
        return result;
    }
    
    public FlowResult<TValue, double> TryRemove(TValue def, double amount) //TValue valueDef, int value
    {
        if(_config.infiniteSource) 
            return new FlowResult<TValue, double>(def, amount, Math.Min(amount, StoredValueOf(def)));
        
        //Lazy sanity checks for failure
        if (!ValueOperationCheck(FlowOperation.Remove, (def, amount), out var reason))
            return FlowResult<TValue, double>.InitFailed(def, amount, reason);


        var available = _mainStack[def];
        //Calculate the actual removeable value
        var actual = Math.Min(available.Value, amount);
        
        //Remove the value from the dictionary or update the value if there is still some left
        _mainStack -= (def, actual);

        //Notify internal cached data
        Notify_RemovedValue(def, amount, actual);

        //On the result, set actual removed value and resolve completion status
        return new FlowResult<TValue, double>(def, amount, actual);
    }

    /// <summary>
    /// Tries to transfer a fixed DefValue, fails when the full amount cannot be transfered.
    /// </summary>
    public FlowResult<TValue, double> TryTransferOrFail(FlowVolumeBase<TValue> other, DefValue<TValue, double> value) //ALL OR NOTHING
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
        return FlowResult<TValue, double>.InitFailed(value.Def, value.Value, reason);
    }

    /// <summary>
    /// Tries to transfer as much as possible.
    /// </summary>
    public FlowResult<TValue, double> TryTransfer(FlowVolumeBase<TValue> other, DefValue<TValue, double> value) //AS MUCH AS POSSIBLE
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
    public FlowResult<TValue, double> TryConsumeOrFail(TValue def, double amount) //ALL OR NOTHING
    {
        if (StoredValueOf(def) >= amount) 
            return TryRemove(def, amount);
        return FlowResult<TValue, double>.InitFailed(def, amount, FlowFailureReason.TriedToConsumeMoreThanExists); //value.Value
    }
    
    /// <summary>
    /// Tries to consume as much as possible of the required amount.
    /// </summary>
    public FlowResult<TValue, double> TryConsume(TValue def, double amount) //AS MUCH AS POSSIBLE
    {
        return TryRemove(def, amount);
    }
    
    #endregion

    public string AsStringUnits(TValue def)
    {
        var value = Math.Round(StoredValueOf(def), 2);
        return $"{value}{def.valueUnit}";
    }
    
    public override string ToString()
    {
        return $"[{TotalValue}/{MaxCapacity}][{Stack.Values.Count}/{AllowedValues.Count}]";
    }
}

public class FlowVolume<T> : FlowVolumeBase<T> where T : FlowValueDef
{
    
    public FlowVolume() : base()
    {
    }
    
    public FlowVolume(FlowVolumeConfig<T> config) : base(config)
    {
    }
}

public class FlowVolumeShared<T> : FlowVolumeBase<T> where T : FlowValueDef
{
    public override double MaxCapacity => _config.capacity * AllowedValues.Count;
    
    public override double CapacityOf(T? def)
    {
        return _config.capacity;
    }
    
    public override bool IsFull(T def)
    {
        return StoredValueOf(def) >= CapacityOf(def);
    }

    protected override double ExcessFor(T def, double amount)
    {
        return Math.Max(StoredValueOf(def) + amount - CapacityOf(def), 0);;
    }

    public FlowVolumeShared(FlowVolumeConfig<T> config) : base(config)
    {
    }
}
